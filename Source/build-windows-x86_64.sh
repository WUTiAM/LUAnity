#!/bin/bash

LUAJIT_VER="LuaJIT-2.0.4"

# Build libluajit.a
make -C $LUAJIT_VER clean
make -j4 -C $LUAJIT_VER BUILDMODE=static CC="gcc -m64"
# Build luanity.dll
gcc lua_wrap.c pb.c -I$LUAJIT_VER/src -m64 -shared -static-libgcc \
	-Wl,--whole-archive $LUAJIT_VER/src/libluajit.a -Wl,--no-whole-archive \
	-o ../Assets/LUAnity/Plugins/x86_64/luanity64.dll

make -C $LUAJIT_VER clean

echo "==== Successfully built Assets/LUAnity/Plugins/x86_64/luanity64.dll ===="
