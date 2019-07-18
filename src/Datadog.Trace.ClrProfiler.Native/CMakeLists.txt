cmake_minimum_required (VERSION 3.8)
cmake_policy(SET CMP0015 NEW)

project("Datadog.Trace.ClrProfiler.Native" VERSION 1.6.1)

add_compile_options(-std=c++11 -fPIC -fms-extensions)
add_compile_options(-DBIT64 -DPAL_STDCPP_COMPAT -DPLATFORM_UNIX -DUNICODE)
add_compile_options(-Wno-invalid-noreturn -Wno-macro-redefined -Wno-pragma-pack)

find_package(nlohmann_json CONFIG REQUIRED)
find_package(re2 CONFIG REQUIRED)

add_library("Datadog.Trace.ClrProfiler.Native.static" STATIC
    class_factory.cpp
    clr_helpers.cpp
    cor_profiler_base.cpp
    cor_profiler.cpp
    il_rewriter_wrapper.cpp
    il_rewriter.cpp
    integration_loader.cpp
    integration.cpp
    logging.cpp
    metadata_builder.cpp
    miniutf.cpp
    string.cpp
    util.cpp
)
set_target_properties("Datadog.Trace.ClrProfiler.Native.static" PROPERTIES PREFIX "")
target_include_directories("Datadog.Trace.ClrProfiler.Native.static"
    PUBLIC ../../lib/coreclr/src/pal/inc/rt
    PUBLIC ../../lib/coreclr/src/pal/prebuilt/inc
    PUBLIC ../../lib/coreclr/src/pal/inc
    PUBLIC ../../lib/coreclr/src/inc
)
target_link_libraries("Datadog.Trace.ClrProfiler.Native.static"
    -static-libgcc
    -static-libstdc++
    nlohmann_json nlohmann_json::nlohmann_json
    re2::re2
)

add_library("Datadog.Trace.ClrProfiler.Native" SHARED
    dllmain.cpp
    interop.cpp
)
set_target_properties("Datadog.Trace.ClrProfiler.Native" PROPERTIES PREFIX "")
target_link_libraries("Datadog.Trace.ClrProfiler.Native" "Datadog.Trace.ClrProfiler.Native.static")
