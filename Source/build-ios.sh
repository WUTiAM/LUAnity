#!/bin/bash

LUAJIT_VER="LuaJIT-2.1.0-beta2"

IXCODE=`xcode-select -print-path`
ISDK=$IXCODE/Platforms/iPhoneOS.platform/Developer
ISDKVER=iPhoneOS.sdk
TARGET_CC=`xcrun -find -sdk iphoneos gcc`
TARGET_AR="`xcrun -find -sdk iphoneos ar` rcus"
TARGET_STRIP=`xcrun -find -sdk iphoneos strip`

#
# armv7
#
# Build libluajit.a
ISDKF="-arch armv7 -isysroot $ISDK/SDKs/$ISDKVER"
make -C $LUAJIT_VER clean
make -j2 -C $LUAJIT_VER BUILDMODE=static CC="$TARGET_CC" HOST_CC="gcc -m32 -arch i386" \
	TARGET_FLAGS="$ISDKF" TARGET_SYS="iOS" TARGET_AR="$TARGET_AR" TARGET_STRIP="$TARGET_STRIP"
# Build lua_wrap.a
mkdir -p ios
$TARGET_CC $ISDKF -c lua_wrap.c -o ios/lua_wrap.o -I$LUAJIT_VER/src
$TARGET_AR ios/lua_wrap.a ios/lua_wrap.o
# Build libluanity-armv7.a
libtool -static -o ios/libluanity-armv7.a $LUAJIT_VER/src/libluajit.a ios/lua_wrap.a

#
# arm64
#
# Build libluajit.a
ISDKF="-arch arm64 -isysroot $ISDK/SDKs/$ISDKVER"
make -C $LUAJIT_VER clean
make -j2 -C $LUAJIT_VER BUILDMODE=static CC="$TARGET_CC" HOST_CC="gcc -m64 -arch x86_64" \
	TARGET_FLAGS="$ISDKF" TARGET_SYS="iOS" TARGET_AR="$TARGET_AR" TARGET_STRIP="$TARGET_STRIP"
# Build lua_wrap.a
$TARGET_CC $ISDKF -c lua_wrap.c -o ios/lua_wrap.o -I$LUAJIT_VER/src
$TARGET_AR ios/lua_wrap.a ios/lua_wrap.o
# Build libluanity-arm64.a
libtool -static -o ios/libluanity-arm64.a $LUAJIT_VER/src/libluajit.a ios/lua_wrap.a

# Build the universal libluanity.a
lipo -create ios/libluanity-armv7.a ios/libluanity-arm64.a -output ../Assets/LUAnity/Plugins/iOS/libluanity.a

make -C $LUAJIT_VER clean

echo "==== Successfully built Assets/LUAnity/Plugins/iOS/libluanity.a ===="
