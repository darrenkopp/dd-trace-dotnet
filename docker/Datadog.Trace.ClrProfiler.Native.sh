#!/bin/bash
set -euxo pipefail

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null && pwd )"

cd "$DIR/.."

cd src/Datadog.Trace.ClrProfiler.Native
mkdir -p obj/Debug/x64
(cd obj/Debug/x64 && \
 rm -rf *
 cmake -DCMAKE_TOOLCHAIN_FILE=/opt/vcpkg/scripts/buildsystems/vcpkg.cmake ../../.. && \
 make)

mkdir -p bin/Debug/x64
cp -f obj/Debug/x64/Datadog.Trace.ClrProfiler.Native.so bin/Debug/x64/Datadog.Trace.ClrProfiler.Native.so
