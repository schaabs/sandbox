set(CMAKE_LEGACY_CYGWIN_WIN32 0)
if(WIN32)
  # On Windows we require at least boost 1.64 and that in turn
  # requires cmake 3.9 or later. (Actually, vcpkg seems to have updated
  # to boost 1.65 which appears to actually require cmake v3.9.2 to run
  # warning free.  v3.9.2 isn't GA yet.)
  cmake_minimum_required(VERSION 3.9.0)
else()
  cmake_minimum_required(VERSION 3.0.0)
endif()

if(POLICY CMP0042)
  cmake_policy(SET CMP0042 NEW) # use MACOSX_RPATH
endif()

set(BUILD_SHARED_LIBS OFF CACHE INTERNAL "This should always be off.")
set(OPENSSL_USE_STATIC_LIBS ON CACHE INTERNAL "This should always be on.")
# The usual list starts with shared lib suffixes.  We prefer static libs.


message(STATUS "BUILD_MAJOR_VERSION=${BUILD_MAJOR_VERSION}")
message(STATUS "BUILD_MINOR_VERSION=${BUILD_MINOR_VERSION}")
message(STATUS "BUILD_PATCH_VERSION=${BUILD_PATCH_VERSION}")
message(STATUS "BUILD_NUM=${BUILD_NUM}")

if(NOT ((DEFINED BUILD_MAJOR_VERSION) AND (DEFINED BUILD_MINOR_VERSION) AND (DEFINED BUILD_PATCH_VERSION) AND (DEFINED BUILD_NUM)))
  set(BUILD_MAJOR_VERSION 0)
  set(BUILD_MINOR_VERSION 0)
  set(BUILD_PATCH_VERSION 0)
  set(BUILD_NUM 0)
endif()
set(AKVVM_VERSION ${BUILD_MAJOR_VERSION}.${BUILD_MINOR_VERSION}.${BUILD_PATCH_VERSION}.${BUILD_NUM})
message(STATUS "AKVVM_VERSION=${AKVVM_VERSION}")
project(akvvmext VERSION ${AKVVM_VERSION})

# Dump some variables so that we have a vague idea of what is going on
message(STATUS "CMAKE_BUILD_TYPE=${CMAKE_BUILD_TYPE}")
message(STATUS "CMAKE_PREFIX_PATH=${CMAKE_PREFIX_PATH}")
message(STATUS "BUILD_SHARED_LIBS=${BUILD_SHARED_LIBS}")
message(STATUS "PROJECT_VERSION_MAJOR=${PROJECT_VERSION_MAJOR}")
message(STATUS "PROJECT_VERSION_MINOR=${PROJECT_VERSION_MINOR}")
message(STATUS "PROJECT_VERSION_PATCH=${PROJECT_VERSION_PATCH}")

include( CMakeToolsHelpers OPTIONAL )

# Add our own modules to the module path
set(CMAKE_MODULE_PATH ${CMAKE_MODULE_PATH} "${CMAKE_SOURCE_DIR}/cmake/modules/")

include(${CMAKE_SOURCE_DIR}/cmake/find_boost.cmake)
include(${CMAKE_SOURCE_DIR}/cmake/find_openssl.cmake)
include(${CMAKE_SOURCE_DIR}/cmake/modules/FindCasablanca.cmake)
include(${CMAKE_SOURCE_DIR}/cmake/ucm.cmake)

# Platform specific settings
if(WIN32)     # Windows
  message(STATUS "Building for Windows")
  set(AKVVM_PLATFORM win32)

  # Windows does not need Boost, OpenSSL or PThreads
  set(AKVVM_USES_BOOST ON)
  set(AKVVM_USES_OPENSSL ON)
  set(AKVVM_USES_PTHREADS OFF)
  set(AKVVM_USES_CRYPTO OFF)

  # Platform specific compiler settings
  add_definitions(-DTARGET_OS_WIN32)
  add_definitions(-DUNICODE -D_UNICODE -D_WIN32_WINNT=0x0602)
elseif(APPLE) # MacOS
  message(STATUS "Building for MacOS")
  set(AKVVM_PLATFORM macos)

  # MacOS needs Boost and OpenSSL (today)
  set(AKVVM_USES_BOOST ON)
  set(AKVVM_USES_OPENSSL ON)
  set(AKVVM_USES_PTHREADS OFF)
  set(AKVVM_USES_CRYPTO ON)

  find_library(COREFOUNDATION_LIBRARY CoreFoundation REQUIRED)
  find_library(SECURITYFRAMEWORK_LIBRARY Security REQUIRED)

  # Platform specific compiler settings
  add_definitions(-DTARGET_OS_MACOS)

elseif(UNIX)  # Other Unix
  message(STATUS "Building for Unix")
  set(AKVVM_PLATFORM unix)

  # Linux needs Boost and OpenSSL (today)
  set(AKVVM_USES_BOOST ON)
  set(AKVVM_USES_OPENSSL ON)
  set(AKVVM_USES_PTHREADS ON)
  set(AKVVM_USES_CRYPTO ON)

  # Platform specific compiler settings
  add_definitions(-DTARGET_OS_UNIX)
else()

  message(FATAL_ERROR "-- Unsupported Build Platform.")
endif()

if (MSVC)
  set(OS_PLATFORM "win")
  set(PLATFORM_DIR ${OS_PLATFORM})
else()
  set(OS_PLATFORM "unix")
  set(PLATFORM_DIR ${OS_PLATFORM})
endif()

set(AKVVM_PLATFORM_DIR ${PLATFORM_DIR} CACHE INTERNAL "Platform specific subdirectory")

# Clear warnings
set(WARNINGS)
set(WERROR TRUE CACHE BOOL "Treat Warnings as Errors.")

# Compiler specific settings
if ( CMAKE_CXX_COMPILER_ID MATCHES "Clang" )
  message(STATUS "Setting clang options")

  set(WARNINGS -Wall -Wextra -Wc++11-compat -Wcast-qual -Wconversion -Wformat=2 -Winit-self -Winvalid-pch -Wmissing-format-attribute -Wmissing-include-dirs -Wpacked -Wredundant-decls -Wcast-align -Wunreachable-code)
  set(SUPPRESSIONS -Wno-sign-conversion -Wno-sign-compare -Wno-unused-parameter -Wno-char-subscripts -Wno-shorten-64-to-32 -Wno-deprecated-declarations)
  set(WARNINGS ${WARNINGS} ${SUPPRESSIONS})

  set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -stdlib=libc++ -Wno-return-type-c-linkage -Wno-unneeded-internal-declaration")
  set(CMAKE_XCODE_ATTRIBUTE_CLANG_CXX_LIBRARY "libc++")
  set(CMAKE_XCODE_ATTRIBUTE_CLANG_CXX_LANGUAGE_STANDARD "c++11")

  set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -std=c++11 -fno-strict-aliasing")

elseif ( CMAKE_CXX_COMPILER_ID MATCHES "GNU" )
  message(STATUS "Setting gcc options")

  set(WARNINGS -Wall -Wextra -Wc++11-compat -Wcast-qual -Wconversion -Wformat=2 -Winit-self -Winvalid-pch -Wmissing-format-attribute -Wmissing-include-dirs -Wpacked -Wredundant-decls -Wcast-align -Wunreachable-code)
  set(SUPPRESSIONS -Wno-sign-conversion -Wno-sign-compare -Wno-unused-parameter -Wno-char-subscripts)
  set(WARNINGS ${WARNINGS} ${SUPPRESSIONS})
  set(LD_FLAGS "${LD_FLAGS} -Wl,-z,defs")

  set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -std=c++11 -fno-strict-aliasing")
elseif ( CMAKE_CXX_COMPILER_ID MATCHES "MSVC" )
  message(STATUS "Setting msvc options")
  # This warnings are related to C++ types being exposed from an object, or something...
  set(WARNINGS /bigobj /wd4290 /wd4251 /wd4275)
  set(CMAKE_STATIC_LINKER_FLAGS "${CMAKE_STATIC_LINKER_FLAGS} /ignore:4264")
  add_compile_options(/MP)
else()
  message(STATUS "Unknown compiler, success is doubtful.")
  message(STATUS "CMAKE_CXX_COMPILER_ID=${CMAKE_CXX_COMPILER_ID}")
endif()


# Find our dependencies
# Static first
message(STATUS "Finding Key Vault SDK")
find_package(KeyVault REQUIRED)

# Required shared libs
message(STATUS "Finding Casablanca (C++ REST SDK)...")
find_casablanca()

if (AKVVM_USES_BOOST)
  # We need Boost
  message(STATUS "Finding Boost...")
  if (NOT WIN32)
    # VCPKG renames the boost libs in a way that doesn't conform to the convention
    # that the find_package(boost) routines expect.  See:
    # https://github.com/Microsoft/vcpkg/issues/1338
    set(Boost_USE_STATIC_LIBS ON CACHE INTERNAL "use static libraries from Boost")
  endif()
  set(Boost_USE_MULTITHREADED ON CACHE INTERNAL "use multithreaded libraries from Boost")
  find_boost()
  if ( ( Boost_MAJOR_VERSION GREATER 0 ) AND ( Boost_MINOR_VERSION GREATER 60 ) )
    add_definitions(-DBOOST_MAX_FILES_SUPPORT)
  endif()
endif()

if (AKVVM_USES_OPENSSL)
  # We need OpenSSL
  message(STATUS "Finding OpenSSL...")
  find_openssl()
endif()

if (AKVVM_USES_PTHREADS)
  # We need Threads
  message(STATUS "Finding Threads...")
  find_package(Threads REQUIRED)
endif()

if (AKVVM_USES_CRYPTO)
  # We need libcrypto++ / libcryptopp
  message(STATUS "Finding Crypto++...")
  find_package(Cryptopp REQUIRED)
  message(STATUS "CRYPTOPP_INCLUDE_DIR=${CRYPTOPP_INCLUDE_DIR}")
  message(STATUS "CRYPTOPP_LIBRARY=${CRYPTOPP_LIBRARY}")
endif()

set(AKVVM_BUILD_TESTS ON CACHE BOOL "When set to OFF building of tests will be skipped.")
message(STATUS "AKVVM_BUILD_TEST is ${AKVVM_BUILD_TEST}")

set(AKVVM_COMMON_DIR "${CMAKE_CURRENT_SOURCE_DIR}/common")
set(AKVVM_COMMON_SRC_DIR "${AKVVM_COMMON_DIR}")
set(AKVVM_PRECOMP_CPP "../common/precomp/precomp.cpp" CACHE INTERNAL "Precompiled header cpp file path")

file( GLOB AKKVM_COMMON_SRC_FILES ${AKVVM_COMMON_SRC_DIR}/*.cpp )
set( AKVVM_COMMON_SRC ${AKKVM_COMMON_SRC_FILES} CACHE INTERNAL "Common C++ files")

include_directories( "${AKVVM_COMMON_DIR}/include" )

add_subdirectory(service)
if (AKVVM_BUILD_TESTS)
  #include test
  add_subdirectory(test)
endif()
add_subdirectory(package)

enable_testing()
