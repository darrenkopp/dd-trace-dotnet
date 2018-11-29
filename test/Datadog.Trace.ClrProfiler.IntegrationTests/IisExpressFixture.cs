using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Datadog.Trace.TestHelpers;
using Xunit.Abstractions;

namespace Datadog.Trace.ClrProfiler.IntegrationTests
{
    // a single fixture instance is shared among all tests in a class,
    // and they never run in parallel
    public sealed class IisExpressFixture : IDisposable
    {
        // start handing out ports at 9500 and keep going up
        private static int _nextPort = 9500;

        private IisExpress _iisExpress;

        public int AgentPort { get; private set; }

        public int HttpPort { get; private set; }

        public ITestOutputHelper Output { get; set; }

        public bool StartCalled { get; private set; }

        public bool StartedSuccessfully { get; private set; }

        public void StartIis(string sampleAppName)
        {
            if (sampleAppName == null) { throw new ArgumentNullException(nameof(sampleAppName)); }

            if (StartCalled)
            {
                // StartIis() can only be called once per Fixture instance
                throw new InvalidOperationException("IIS Express was already started on this Fixture instance.");
            }

            StartCalled = true;
            AgentPort = Interlocked.Increment(ref _nextPort);
            HttpPort = Interlocked.Increment(ref _nextPort);

            // get full paths to integration definitions
            IEnumerable<string> integrationPaths = Directory.EnumerateFiles(".", "*integrations.json").Select(Path.GetFullPath);

            IDictionary<string, string> environmentVariables = ProfilerHelper.GetProfilerEnvironmentVariables(
                Instrumentation.ProfilerClsid,
                TestHelper.GetProfilerDllPath(),
                integrationPaths,
                AgentPort);

            var waitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
            var stopwatch = Stopwatch.StartNew();
            _iisExpress = new IisExpress();

            _iisExpress.Message += (sender, e) =>
            {
                if (Output != null && !string.IsNullOrEmpty(e.Data))
                {
                    Output.WriteLine($"[webserver] {e.Data}");
                }
            };

            _iisExpress.OutputDataReceived += (sender, e) =>
            {
                if (Output != null && !string.IsNullOrEmpty(e.Data))
                {
                    Output.WriteLine($"[webserver][stdout] {e.Data}");

                    if (e.Data.Contains("IIS Express is running"))
                    {
                        Output.WriteLine($"[webserver] IIS Express started after {stopwatch.Elapsed}");
                        stopwatch.Stop();
                        waitHandle.Set();
                    }
                }
            };

            _iisExpress.ErrorDataReceived += (sender, e) =>
            {
                if (Output != null && !string.IsNullOrEmpty(e.Data))
                {
                    Output.WriteLine($"[webserver][stderr] {e.Data}");
                }
            };

            var sampleAppDirectory = Path.Combine(TestHelper.GetSolutionDirectory(), "samples", $"Samples.{sampleAppName}");
            _iisExpress.Start(sampleAppDirectory, Environment.Is64BitProcess, HttpPort, environmentVariables);

            // give IIS Express a few seconds to boot up in slow environments,
            // stop waiting when it outputs "IIS Express is running" or after timeout
            waitHandle.WaitOne(TimeSpan.FromSeconds(10));

            StartedSuccessfully = true;
        }

        // called after all test methods in a class are finished
        public void Dispose()
        {
            // disconnect the output after all tests are done
            // since it can't be used outside of the context of a test
            Output = null;

            if (_iisExpress != null)
            {
                _iisExpress.Stop();
                _iisExpress.Dispose();
            }
        }
    }
}
