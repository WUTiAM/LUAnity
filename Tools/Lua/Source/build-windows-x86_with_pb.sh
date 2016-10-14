#!/bin/bash

LUAJIT_VER="LuaJIT-2.0.4"

# Build libluajit.a
make -C $LUAJIT_VER clean
make -j4 -C $LUAJIT_VER BUILDMODE=static CC="gcc -m32"
# Build ulua.dll
gcc lua_wrap.c pb.c -I$LUAJIT_VER/src -m32 -march=i686 -shared -static-libgcc \
	-Wl,--whole-archive $LUAJIT_VER/src/libluajit.a -Wl,--no-whole-archive \
	-o Plugins/x86/ulua.dll

make -C $LUAJIT_VER clean

echo "==== Successfully built Plugins/x86/ulua.dll ===="
