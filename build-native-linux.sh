#!/bin/bash
PROJECT_ROOT=$(readlink -f $(dirname $0))

DEFAULT_CC=gcc
X86_SFX="-m32"
X64_SFX="-m64"

X64_OUT=$PROJECT_ROOT/runtimes/ubuntu.14.04-x64/native/
X86_OUT=$PROJECT_ROOT/runtimes/ubuntu.14.04-x86/native/

rm -f $X86_OUT/*.so $X64_OUT/*.so

git submodule foreach git clean -fdx
git submodule foreach git reset --hard

DEFAULT_FLAGS="-O3 -flto"

LIBLZO2DIR=extsrc/lzo2
(cd $LIBLZO2DIR && 
 cp $PROJECT_ROOT/native-cmakes/CMakeLists.lzo2.txt CMakeLists.txt &&
 mkdir build && cd build &&
 CC="$DEFAULT_CC $X86_SFX" cmake .. -DCMAKE_BUILD_TYPE=RELEASE -DCMAKE_C_FLAGS_RELEASE="$DEFAULT_FLAGS" && make -j8 &&
 cp liblzo2.so $X86_OUT)
(cd $LIBLZO2DIR &&
 mkdir build && cd build &&
 cp $PROJECT_ROOT/native-cmakes/CMakeLists.lzo2.txt CMakeLists.txt &&
 CC="$DEFAULT_CC $X64_SFX" cmake .. -DCMAKE_BUILD_TYPE=RELEASE -DCMAKE_C_FLAGS_RELEASE="$DEFAULT_FLAGS" && make -j8 &&
 cp liblzo2.so $X64_OUT)

LIBBZ2DIR=extsrc/bzip2
(cd $LIBBZ2DIR &&
 cp $PROJECT_ROOT/native-cmakes/CMakeLists.bzip2.txt CMakeLists.txt &&
 CC="$DEFAULT_CC $X86_SFX" cmake . -DCMAKE_BUILD_TYPE=RELEASE -DCMAKE_C_FLAGS_RELEASE="$DEFAULT_FLAGS" && make -j8 &&
 cp libbz2.so $X86_OUT)
(cd $LIBBZ2DIR &&
 cp $PROJECT_ROOT/native-cmakes/CMakeLists.bzip2.txt CMakeLists.txt &&
 CC="$DEFAULT_CC $X64_SFX" cmake . -DCMAKE_BUILD_TYPE=RELEASE -DCMAKE_C_FLAGS_RELEASE="$DEFAULT_FLAGS" && make -j8 &&
 cp libbz2.so $X64_OUT)

LZ4DIR=extsrc/lz4/contrib/cmake_unofficial
(cd $LZ4DIR &&
 cp $PROJECT_ROOT/native-cmakes/CMakeLists.lz4.txt CMakeLists.txt &&
 CC="$DEFAULT_CC $X86_SFX" cmake . -DBUILD_SHARED_LIBS=ON -DBUILD_STATIC_LIBS=OFF -DCMAKE_BUILD_TYPE=RELEASE -DCMAKE_C_FLAGS_RELEASE="$DEFAULT_FLAGS" && make -j8 &&
 cp liblz4.so $X86_OUT)
(cd $LZ4DIR &&
 cp $PROJECT_ROOT/native-cmakes/CMakeLists.lz4.txt CMakeLists.txt &&
 CC="$DEFAULT_CC $X64_SFX" cmake . -DBUILD_SHARED_LIBS=ON -DBUILD_STATIC_LIBS=OFF -DCMAKE_BUILD_TYPE=RELEASE -DCMAKE_C_FLAGS_RELEASE="$DEFAULT_FLAGS" && make -j8 &&
 cp liblz4.so $X64_OUT)

LIBZDIR=extsrc/zlib-ng
(cd $LIBZDIR &&
 cp $PROJECT_ROOT/native-cmakes/CMakeLists.libz.txt CMakeLists.txt &&
 CC="$DEFAULT_CC $X86_SFX" cmake . -DCMAKE_BUILD_TYPE=RELEASE -DCMAKE_C_FLAGS_RELEASE="$DEFAULT_FLAGS" && make -j8 &&
 cp libz.so $X86_OUT)
(cd $LIBZDIR &&
 cp $PROJECT_ROOT/native-cmakes/CMakeLists.libz.txt CMakeLists.txt &&
 CC="$DEFAULT_CC $X64_SFX" cmake . -DCMAKE_BUILD_TYPE=RELEASE -DCMAKE_C_FLAGS_RELEASE="$DEFAULT_FLAGS" && make -j8 &&
 cp libz.so $X64_OUT)

LIBLZMADIR=extsrc/xz
(cd $LIBLZMADIR &&
 ./autogen.sh && CC="$DEFAULT_CC $X86_SFX" CFLAGS="$DEFAULT_FLAGS" ./configure --host=i686-pc-linux-gnu && make V=0 -j8 &&
 cp src/liblzma/.libs/liblzma.so $X86_OUT)
(cd $LIBLZMADIR &&
 ./autogen.sh && CC="$DEFAULT_CC $X64_SFX" CFLAGS="$DEFAULT_FLAGS" ./configure && make V=0 -j8 &&
 cp src/liblzma/.libs/liblzma.so $X64_OUT)

for so in $X86_OUT/*.so $X64_OUT/*.so; do strip --strip-unneeded $so; done
