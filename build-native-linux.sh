#!/bin/bash -x
PROJECT_ROOT=$(readlink -f $(dirname $0))

CC=gcc
CC_X86="$CC -m32"
CC_X64="$CC -m64"

X86_OUT=$PROJECT_ROOT/runtimes/${RID}-x86/native/
X64_OUT=$PROJECT_ROOT/runtimes/${RID}-x64/native/

rm -f $X86_OUT/ $X64_OUT/
mkdir -p $X86_OUT/ $X64_OUT/

GCC_MAJOR=$(gcc --version | grep ^gcc | sed 's/^.* //g' | sed 's/\..*//g')
DEFAULT_FLAGS="-O3"
if [[ "$GCC_MAJOR" -gt "4" ]]; then
  DEFAULT_FLAGS="${DEFAULT_FLAGS} -flto"
fi

function compile_lzo2 {
  CC_PARAM=$1
  OUT_PARAM=$2

  (cd extsrc/lzo2 && git clean -fdx &&
   cp $PROJECT_ROOT/native-cmakes/CMakeLists.lzo2.txt CMakeLists.txt &&
   mkdir build && cd build &&
   CC="$CC_PARAM" cmake .. -DCMAKE_BUILD_TYPE=RELEASE -DCMAKE_C_FLAGS_RELEASE="$DEFAULT_FLAGS" && make -j8 &&
   cp liblzo2.so $OUT_PARAM)
}

function compile_bzip2 {
  CC_PARAM=$1
  OUT_PARAM=$2

  (cd extsrc/bzip2 && git clean -fdx &&
   cp $PROJECT_ROOT/native-cmakes/CMakeLists.bzip2.txt CMakeLists.txt &&
   CC="$CC_PARAM" cmake . -DCMAKE_BUILD_TYPE=RELEASE -DCMAKE_C_FLAGS_RELEASE="$DEFAULT_FLAGS" && make -j8 &&
   cp libbz2.so $OUT_PARAM)
}

function compile_lz4 {
  CC_PARAM=$1
  OUT_PARAM=$2

  (cd extsrc/lz4/contrib/cmake_unofficial && git clean -fdx &&
   cp $PROJECT_ROOT/native-cmakes/CMakeLists.lz4.txt CMakeLists.txt &&
   CC="$CC_PARAM" cmake . -DBUILD_SHARED_LIBS=ON -DBUILD_STATIC_LIBS=OFF -DCMAKE_BUILD_TYPE=RELEASE -DCMAKE_C_FLAGS_RELEASE="$DEFAULT_FLAGS" && make -j8 &&
   cp liblz4.so $OUT_PARAM)
}

function compile_zlib {
  CC_PARAM=$1
  OUT_PARAM=$2
 
  (cd extsrc/zlib-ng && git clean -fdx &&
   cp $PROJECT_ROOT/native-cmakes/CMakeLists.libz.txt CMakeLists.txt &&
   CC="$CC_PARAM" cmake . -DCMAKE_BUILD_TYPE=RELEASE -DCMAKE_C_FLAGS_RELEASE="$DEFAULT_FLAGS" -DZLIB_COMPAT=1 -DARCH=i686 && make -j8 &&
   cp libz.so $OUT_PARAM)
}

function compile_lzma {
  CC_PARAM=$1
  OUT_PARAM=$2
  
  (cd extsrc/xz && git clean -fdx &&
   ./autogen.sh && CC="$CC_PARAM" CFLAGS="$DEFAULT_FLAGS" ./configure --host=i686-pc-linux-gnu && make V=0 -j8 &&
   cp src/liblzma/.libs/liblzma.so $OUT_PARAM)
}


compile_lzo2 "$CC_X86" "$X86_OUT"
compile_lzo2 "$CC_X64" "$X64_OUT"

compile_bzip2 "$CC_X86" "$X86_OUT"
compile_bzip2 "$CC_X64" "$X64_OUT"

compile_lz4 "$CC_X86" "$X86_OUT"
compile_lz4 "$CC_X64" "$X64_OUT"

compile_zlib "$CC_X86" "$X86_OUT"
compile_zlib "$CC_X64" "$X64_OUT"

compile_lzma "$CC_X86" "$X86_OUT"
compile_lzma "$CC_X64" "$X64_OUT"

echo Stripping shared objects...
echo Before strip: $(du -chs $X86_OUT/ $X64_OUT/ | tail -1 | cut -f 1)
for so in $X86_OUT/*.so $X64_OUT/*.so; do strip --strip-unneeded $so; done
echo After strip: $(du -chs $X86_OUT/ $X64_OUT/ | tail -1 | cut -f 1)
