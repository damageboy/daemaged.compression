#!/bin/bash
PROJECT_ROOT=$(readlink -f $(dirname $0))

DEFAULT_CC=gcc-5
X86_SFX="-m32"
X64_SFX="-m64"

DEFAULT_FLAGS="-O3 -flto"

LZ4DIR=extsrc/lz4-r129/cmake_unofficial
(cd $LZ4DIR && git clean -fdx && CC="$DEFAULT_CC $X86_SFX" cmake . -DCMAKE_BUILD_TYPE=RELEASE -DCMAKE_C_FLAGS_RELEASE="$DEFAULT_FLAGS" && make && cp liblz4.so $PROJECT_ROOT/liblz4/x86)
(cd $LZ4DIR && git clean -fdx && CC="$DEFAULT_CC $X64_SFX" cmake . -DCMAKE_BUILD_TYPE=RELEASE -DCMAKE_C_FLAGS_RELEASE="$DEFAULT_FLAGS" && make && cp liblz4.so $PROJECT_ROOT/liblz4/x64)

LIBZDIR=extsrc/zlib-ng
(cd $LIBZDIR && git clean -fdx && CC="$DEFAULT_CC $X86_SFX" cmake . -DCMAKE_BUILD_TYPE=RELEASE -DCMAKE_C_FLAGS_RELEASE="$DEFAULT_FLAGS" && make && cp libz.so $PROJECT_ROOT/libz/x86)
(cd $LIBZDIR && git clean -fdx && CC="$DEFAULT_CC $X64_SFX" cmake . -DCMAKE_BUILD_TYPE=RELEASE -DCMAKE_C_FLAGS_RELEASE="$DEFAULT_FLAGS" && make && cp libz.so $PROJECT_ROOT/libz/x64)

