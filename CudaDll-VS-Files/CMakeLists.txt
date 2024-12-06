cmake_minimum_required(VERSION 3.9 FATAL_ERROR)
# Set CMake policy CMP0074
if (POLICY CMP0074)
    cmake_policy(SET CMP0074 NEW)
endif()

project(MRPNN LANGUAGES CXX CUDA)

enable_language(CUDA)

IF (WIN32)

MESSAGE(STATUS "Is windows")
SET(CMAKE_CXX_FLAGS "/openmp /Ot")

ELSE ()

MESSAGE(STATUS "Is linux")
add_definitions(-DLINUX)

ENDIF ()

Option(GUI OFF)
Option(RTX30XX ON)
Option(TENSORCORE OFF)
Option(CompileRPNN OFF)
Option(DISABLE_FAST_MATH OFF)

if (CompileRPNN)
    add_definitions(-DCRPNN)
endif()

if (TENSORCORE)
    add_definitions(-DENBALE_TENSOR)
endif()


file(GLOB CORE_HEADERS core/*.h *core/*.hpp core/*.cuh)
file(GLOB CORE_SOURCES core/*.cpp core/*.cu)

source_group("Core Include" FILES ${CORE_HEADERS}) 
source_group("Core Source" FILES ${CORE_SOURCES})

include_directories(${PROJECT_SOURCE_DIR}/core)
  

add_library(VolumeRender SHARED ${CORE_HEADERS} ${CORE_SOURCES})
set_target_properties(VolumeRender PROPERTIES
CUDA_SEPARABLE_COMPILATION ON
CXX_STANDARD_REQUIRED ON
CXX_STANDARD 17)
target_compile_definitions(VolumeRender PRIVATE _HAS_STD_BYTE=0)

if (DISABLE_FAST_MATH)
    if (RTX30XX)
    target_compile_options(VolumeRender PRIVATE $<$<COMPILE_LANGUAGE:CUDA>:-arch=sm_86 -maxrregcount=168>)
    else()
    target_compile_options(VolumeRender PRIVATE $<$<COMPILE_LANGUAGE:CUDA>:-arch=sm_75 -maxrregcount=168>)
    endif()
else()
    if (RTX30XX)
    target_compile_options(VolumeRender PRIVATE $<$<COMPILE_LANGUAGE:CUDA>:-use_fast_math -arch=sm_86 -maxrregcount=168>)
    else()
    target_compile_options(VolumeRender PRIVATE $<$<COMPILE_LANGUAGE:CUDA>:-use_fast_math -arch=sm_75 -maxrregcount=168>)
    endif()
endif()