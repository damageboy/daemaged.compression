#!/bin/bash
PROJECT_ROOT=$(readlink -f $(dirname $0))

CMAKE_PATH=/c/dev/cmake/bin

vs=$(/c/Program\ Files\ \(x86\)/Microsoft\ Visual\ Studio/Installer/vswhere.exe -latest | grep installationPath | cut -f 2- -d : -d " ") 
MSBuildExePath="$vs/MSBuild/15.0/Bin/MSBuild.exe"

export PATH=$PATH:$CMAKE_PATH

X86_OUT=$PROJECT_ROOT/runtimes/win7-x86/native/
X64_OUT=$PROJECT_ROOT/runtimes/win7-x64/native/

rm -f $X86_OUT/*.dll $X64_OUT/*.dll
mkdir -p $X86_OUT
mkdir -p $X64_OUT

git submodule foreach git clean -fdx
git submodule foreach git reset --hard


LIBLZO2DIR=extsrc/lzo2
(cd $LIBLZO2DIR && mkdir -p build && cd build &&
 cp $PROJECT_ROOT/native-cmakes/CMakeLists.lzo2.txt CMakeLists.txt &&
 cmake .. -DCMAKE_BUILD_TYPE=RELEASE -G "Visual Studio 15 2017" && $MSBuildExePath lzo_shared.vcxproj -p:Configuration=Release &&
 cp Release/liblzo2.dll $X86_OUT)
(cd $LIBLZO2DIR && mkdir -p build && cd build &&
 cp $PROJECT_ROOT/native-cmakes/CMakeLists.lzo2.txt CMakeLists.txt &&
 cmake .. -DCMAKE_BUILD_TYPE=RELEASE -G "Visual Studio 15 2017 Win64" && $MSBuildExePath lzo_shared.vcxproj -p:Configuration=Release &&
 cp Release/liblzo2.dll $X64_OUT)
 
LIBBZ2DIR=extsrc/bzip2
(cd $LIBBZ2DIR && 
 cp $PROJECT_ROOT/native-cmakes/CMakeLists.bzip2.txt CMakeLists.txt &&
 cmake . -DCMAKE_BUILD_TYPE=RELEASE -G "Visual Studio 15 2017" && $MSBuildExePath libbz2.vcxproj -p:Configuration=Release &&
 cp Release/libbz2.dll $X86_OUT)
(cd $LIBBZ2DIR && 
 cp $PROJECT_ROOT/native-cmakes/CMakeLists.bzip2.txt CMakeLists.txt &&
 cmake . -DCMAKE_BUILD_TYPE=RELEASE -G "Visual Studio 15 2017 Win64" && $MSBuildExePath libbz2.vcxproj -p:Configuration=Release &&
 cp Release/libbz2.dll $X64_OUT)
 
LZ4DIR=extsrc/lz4/contrib/cmake_unofficial
(cd $LZ4DIR && 
 cp $PROJECT_ROOT/native-cmakes/CMakeLists.lz4.txt CMakeLists.txt &&
 cmake . -DBUILD_SHARED_LIBS=ON -DBUILD_STATIC_LIBS=OFF -DCMAKE_BUILD_TYPE=RELEASE -G "Visual Studio 15 2017" && $MSBuildExePath lz4_shared.vcxproj -p:Configuration=Release &&
 cp Release/liblz4.dll $X86_OUT)
(cd $LZ4DIR && 
 cp $PROJECT_ROOT/native-cmakes/CMakeLists.lz4.txt CMakeLists.txt &&
 cmake . -DBUILD_SHARED_LIBS=ON -DBUILD_STATIC_LIBS=OFF -DCMAKE_BUILD_TYPE=RELEASE -G "Visual Studio 15 2017 Win64" && $MSBuildExePath lz4_shared.vcxproj -p:Configuration=Release &&
 cp Release/liblz4.dll $X64_OUT)
 
LIBZDIR=extsrc/zlib-ng
(cd $LIBZDIR && 
 cp $PROJECT_ROOT/native-cmakes/CMakeLists.libz.txt CMakeLists.txt &&
 cp $PROJECT_ROOT/native-cmakes/zlib1.rc win32/zlib1.rc &&
 cmake . -DBUILD_SHARED_LIBS=ON -DCMAKE_BUILD_TYPE=RELEASE -G "Visual Studio 15 2017" && $MSBuildExePath zlib.vcxproj -p:Configuration=Release &&
 cp Release/libz.dll $X86_OUT)
(cd $LIBZDIR && 
 cp $PROJECT_ROOT/native-cmakes/CMakeLists.libz.txt CMakeLists.txt &&
 cp $PROJECT_ROOT/native-cmakes/zlib1.rc win32/zlib1.rc &&
 cmake . -DBUILD_SHARED_LIBS=ON -DCMAKE_BUILD_TYPE=RELEASE -G "Visual Studio 15 2017 Win64" && $MSBuildExePath zlib.vcxproj -p:Configuration=Release &&
 cp Release/libz.dll $X64_OUT) 

LIBLZMADIR=extsrc/xz/windows
(cd $LIBLZMADIR && 
 cp $PROJECT_ROOT/native-cmakes/liblzma_dll.vcxproj liblzma_dll.vcxproj &&
 $MSBuildExePath liblzma_dll.vcxproj -p:Configuration=Release -p:Platform=Win32 &&
 cp Release/Win32/liblzma_dll/liblzma.dll $X86_OUT)
(cd $LIBLZMADIR && 
 cp $PROJECT_ROOT/native-cmakes/liblzma_dll.vcxproj liblzma_dll.vcxproj &&
 $MSBuildExePath liblzma_dll.vcxproj -p:Configuration=Release -p:Platform=x64 &&
 cp Release/x64/liblzma_dll/liblzma.dll $X64_OUT)
