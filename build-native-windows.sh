#!/bin/bash
PROJECT_ROOT=$(readlink -f $(dirname $0))

CMAKE_PATH=/c/dev/cmake/bin

vs=$(/c/Program\ Files\ \(x86\)/Microsoft\ Visual\ Studio/Installer/vswhere.exe -latest | grep installationPath | cut -f 2- -d : -d " ") 
MSBuildExePath="$vs/MSBuild/15.0/Bin/MSBuild.exe"


export PATH=$PATH:$CMAKE_PATH

LIBLZO2DIR=extsrc/lzo2
LIBLZO2OUT=liblzo2
(cd $LIBLZO2DIR && git clean -fdx && \
 cp $PROJECT_ROOT/native-cmakes/CMakeLists.lzo2.txt CMakeLists.txt && \
 mkdir build && cd build && \
 cmake .. -DCMAKE_BUILD_TYPE=RELEASE -G "Visual Studio 15 2017" && $MSBuildExePath lzo_shared.vcxproj -p:Configuration=Release &&
 cp Release/lzo2.dll $PROJECT_ROOT/$LIBLZO2OUT/x86)
(cd $LIBLZO2DIR && git clean -fdx && \
 mkdir build && cd build && \
 cp $PROJECT_ROOT/native-cmakes/CMakeLists.lzo2.txt CMakeLists.txt && \
 cmake .. -DCMAKE_BUILD_TYPE=RELEASE -G "Visual Studio 15 2017 Win64" && $MSBuildExePath lzo_shared.vcxproj -p:Configuration=Release &&
 cp Release/lzo2.dll  $PROJECT_ROOT/$LIBLZO2OUT/x64)
 
exit

LIBBZ2DIR=extsrc/bzip2
LIBBZ2OUT=libbz2
(cd $LIBBZ2DIR && git clean -fdx && \
 cp $PROJECT_ROOT/native-cmakes/CMakeLists.bzip2.txt CMakeLists.txt && \
 CC="$DEFAULT_CC $X86_SFX" cmake . -DCMAKE_BUILD_TYPE=RELEASE -DCMAKE_C_FLAGS_RELEASE="$DEFAULT_FLAGS" && make -j4 &&
 cp libbz2.so $PROJECT_ROOT/$LIBBZ2OUT/x86)
(cd $LIBBZ2DIR && git clean -fdx && \
 cp $PROJECT_ROOT/native-cmakes/CMakeLists.bzip2.txt CMakeLists.txt && \
 CC="$DEFAULT_CC $X64_SFX" cmake . -DCMAKE_BUILD_TYPE=RELEASE -DCMAKE_C_FLAGS_RELEASE="$DEFAULT_FLAGS" && make -j4 &&
 cp libbz2.so $PROJECT_ROOT/$LIBBZ2OUT/x64)

LZ4DIR=extsrc/lz4/cmake_unofficial
LZ4OUT=liblz4
(cd $LZ4DIR && git clean -fdx && \
 cp $PROJECT_ROOT/native-cmakes/CMakeLists.lz4.txt CMakeLists.txt && \
 CC="$DEFAULT_CC $X86_SFX" cmake . -DCMAKE_BUILD_TYPE=RELEASE -DCMAKE_C_FLAGS_RELEASE="$DEFAULT_FLAGS" && make -j4 && \
 cp liblz4.so $PROJECT_ROOT/$LZ4OUT/x86)
(cd $LZ4DIR && git clean -fdx && \
 cp $PROJECT_ROOT/native-cmakes/CMakeLists.lz4.txt CMakeLists.txt && \
 CC="$DEFAULT_CC $X64_SFX" cmake . -DCMAKE_BUILD_TYPE=RELEASE -DCMAKE_C_FLAGS_RELEASE="$DEFAULT_FLAGS" && make -j4 && \
 cp liblz4.so $PROJECT_ROOT/$LZ4OUT/x64)

LIBZDIR=extsrc/zlib-ng
LIBZOUT=libz
(cd $LIBZDIR && git clean -fdx && \
 CC="$DEFAULT_CC $X86_SFX" cmake . -DCMAKE_BUILD_TYPE=RELEASE -DCMAKE_C_FLAGS_RELEASE="$DEFAULT_FLAGS" && make -j4 && \
 cp libz.so $PROJECT_ROOT/$LIBZOUT/x86)
(cd $LIBZDIR && git clean -fdx && \
 CC="$DEFAULT_CC $X64_SFX" cmake . -DCMAKE_BUILD_TYPE=RELEASE -DCMAKE_C_FLAGS_RELEASE="$DEFAULT_FLAGS" && make -j4 && \
 cp libz.so $PROJECT_ROOT/$LIBZOUT/x64)

LIBLZMADIR=extsrc/xz
LIBLZMAOUT=liblzma
(cd $LIBLZMADIR && git clean -fdx && \
 ./autogen.sh && CC="$DEFAULT_CC $X86_SFX" CFLAGS="$DEFAULT_FLAGS" ./configure && make V=0 -j4 && \
 cp src/liblzma/.libs/liblzma.so $PROJECT_ROOT/$LIBLZMAOUT/x86)
(cd $LIBLZMADIR && git clean -fdx && \
 ./autogen.sh && CC="$DEFAULT_CC $X64_SFX" CFLAGS="$DEFAULT_FLAGS" ./configure && make V=0 -j4 && \
 cp src/liblzma/.libs/liblzma.so $PROJECT_ROOT/$LIBLZMAOUT/x64)

for o in $LIBLZO2OUT $LZ4OUT $LIBZOUT $LIBLZMAOUT $LIBBZ2OUT; do strip --strip-unneeded $o/{x86,x64}/$o.so; done
