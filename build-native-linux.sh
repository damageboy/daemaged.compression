#!/bin/bash -x
PROJECT_ROOT=$(readlink -f $(dirname $0))

DEFAULT_CC=gcc
X86_SFX="-m32"
X64_SFX="-m64"

X86_OUT=$PROJECT_ROOT/runtimes/${RID}-x86/native/
X64_OUT=$PROJECT_ROOT/runtimes/${RID}-x64/native/

rm -f $X86_OUT/ $X64_OUT/
mkdir -p $X86_OUT/ $X64_OUT/

GCC_MAJOR=$(gcc --version | grep ^gcc | sed 's/^.* //g' | sed 's/\..*//g')
DEFAULT_FLAGS="-O3"
if [[ "$GCC_MAJOR" -gt "4" ]]; then
  DEFAULT_FLAGS="${DEFAULT_FLAGS} -flto"
fi

LIBLZO2DIR=extsrc/lzo2
(cd $LIBLZO2DIR && git clean -fdx &&
 cp $PROJECT_ROOT/native-cmakes/CMakeLists.lzo2.txt CMakeLists.txt &&
 mkdir build && cd build &&
 CC="$DEFAULT_CC $X86_SFX" cmake .. -DCMAKE_BUILD_TYPE=RELEASE -DCMAKE_C_FLAGS_RELEASE="$DEFAULT_FLAGS" && make -j8 &&
 cp liblzo2.so $X86_OUT)
(cd $LIBLZO2DIR && git clean -fdx &&
 mkdir build && cd build &&
 cp $PROJECT_ROOT/native-cmakes/CMakeLists.lzo2.txt CMakeLists.txt &&
 CC="$DEFAULT_CC $X64_SFX" cmake .. -DCMAKE_BUILD_TYPE=RELEASE -DCMAKE_C_FLAGS_RELEASE="$DEFAULT_FLAGS" && make -j8 &&
 cp liblzo2.so $X64_OUT)

LIBBZ2DIR=extsrc/bzip2
(cd $LIBBZ2DIR && git clean -fdx &&
 cp $PROJECT_ROOT/native-cmakes/CMakeLists.bzip2.txt CMakeLists.txt &&
 CC="$DEFAULT_CC $X86_SFX" cmake . -DCMAKE_BUILD_TYPE=RELEASE -DCMAKE_C_FLAGS_RELEASE="$DEFAULT_FLAGS" && make -j8 &&
 cp libbz2.so $X86_OUT)
(cd $LIBBZ2DIR && git clean -fdx &&
 cp $PROJECT_ROOT/native-cmakes/CMakeLists.bzip2.txt CMakeLists.txt &&
 CC="$DEFAULT_CC $X64_SFX" cmake . -DCMAKE_BUILD_TYPE=RELEASE -DCMAKE_C_FLAGS_RELEASE="$DEFAULT_FLAGS" && make -j8 &&
 cp libbz2.so $X64_OUT)

LZ4DIR=extsrc/lz4/contrib/cmake_unofficial
(cd $LZ4DIR && git clean -fdx &&
 cp $PROJECT_ROOT/native-cmakes/CMakeLists.lz4.txt CMakeLists.txt &&
 CC="$DEFAULT_CC $X86_SFX" cmake . -DBUILD_SHARED_LIBS=ON -DBUILD_STATIC_LIBS=OFF -DCMAKE_BUILD_TYPE=RELEASE -DCMAKE_C_FLAGS_RELEASE="$DEFAULT_FLAGS" && make -j8 &&
 cp liblz4.so $X86_OUT)
(cd $LZ4DIR && git clean -fdx &&
 cp $PROJECT_ROOT/native-cmakes/CMakeLists.lz4.txt CMakeLists.txt &&
 CC="$DEFAULT_CC $X64_SFX" cmake . -DBUILD_SHARED_LIBS=ON -DBUILD_STATIC_LIBS=OFF -DCMAKE_BUILD_TYPE=RELEASE -DCMAKE_C_FLAGS_RELEASE="$DEFAULT_FLAGS" && make -j8 &&
 cp liblz4.so $X64_OUT)

LIBZDIR=extsrc/zlib-ng
(cd $LIBZDIR && git clean -fdx &&
 cp $PROJECT_ROOT/native-cmakes/CMakeLists.libz.txt CMakeLists.txt &&
 CC="$DEFAULT_CC $X86_SFX" cmake . -DCMAKE_BUILD_TYPE=RELEASE -DCMAKE_C_FLAGS_RELEASE="$DEFAULT_FLAGS" -DZLIB_COMPAT=1 -DARCH=i686 && make -j8 &&
 cp libz.so $X86_OUT)
(cd $LIBZDIR && git clean -fdx &&
 cp $PROJECT_ROOT/native-cmakes/CMakeLists.libz.txt CMakeLists.txt &&
 CC="$DEFAULT_CC $X64_SFX" cmake . -DCMAKE_BUILD_TYPE=RELEASE -DCMAKE_C_FLAGS_RELEASE="$DEFAULT_FLAGS" -DZLIB_COMPAT=1 -DARCH=amd64 && make -j8 &&
 cp libz.so $X64_OUT)

LIBLZMADIR=extsrc/xz
(cd $LIBLZMADIR && git clean -fdx &&
 ./autogen.sh && CC="$DEFAULT_CC $X86_SFX" CFLAGS="$DEFAULT_FLAGS" ./configure --host=i686-pc-linux-gnu && make V=0 -j8 &&
 cp src/liblzma/.libs/liblzma.so $X86_OUT)
(cd $LIBLZMADIR && git clean -fdx &&
 ./autogen.sh && CC="$DEFAULT_CC $X64_SFX" CFLAGS="$DEFAULT_FLAGS" ./configure && make V=0 -j8 &&
 cp src/liblzma/.libs/liblzma.so $X64_OUT)

echo Stripping shared objects...
echo Before strip: $(du -chs $X86_OUT/ $X64_OUT/ | tail -1 | cut -f 1)
for so in $X86_OUT/*.so $X64_OUT/*.so; do strip --strip-unneeded $so; done
echo After strip: $(du -chs $X86_OUT/ $X64_OUT/ | tail -1 | cut -f 1)
