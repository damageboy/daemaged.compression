@call b\unset.bat
@call b\clean.bat

@set CFI=-Iinclude -I. -Isrc
@set CFASM=-DLZO_USE_ASM
@set BNAME=lzo2
@set BLIB=liblzo2.lib
@set BDLL=liblzo2.dll

@echo Compiling, please be patient...
