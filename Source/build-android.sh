#!/bin/bash

LUAJIT_VER="LuaJIT-2.0.4"

if [[ "$OSTYPE" == "darwin"* ]]; then
	# macOS
	# !! TODO: Change to your own path !!
	NDK=/Users/jo3l/Development/android-ndk-r13
	NDKVER=$NDK/toolchains/arm-linux-androideabi-4.9
	NDKP=$NDKVER/prebuilt/darwin-x86_64/bin/arm-linux-androideabi-
else
	# Windows
	# !! TODO: Change to your own path !!
	NDK=/f/Applications/android-ndk-r13
	NDKVER=$NDK/toolchains/arm-linux-androideabi-4.9
	NDKP=$NDKVER/prebuilt/windows-x86_64/bin/arm-linux-androideabi-
fi

# Android/ARM, armeabi-v7a (ARMv7 VFP), Android 4.0+ (ICS)
NDKABI=14
NDKF="--sysroot $NDK/platforms/android-$NDKABI/arch-arm"
NDKARCH="-march=armv7-a -mfloat-abi=softfp -Wl,--fix-cortex-a8"
# Build libluajit.a
make -C $LUAJIT_VER clean
make -j4 -C $LUAJIT_VER HOST_CC="gcc -m32" CC="gcc -fPIC" CROSS=$NDKP TARGET_FLAGS="$NDKF $NDKARCH" TARGET_SYS=Linux
# Build lua_wrap.o
mkdir -p android
${NDKP}gcc $NDKF -c lua_wrap.c -o android/lua_wrap.o -I$LUAJIT_VER/src
# Build libluanity.so
${NDKP}gcc -fno-stack-protector -I$LUAJIT_VER/src $NDKF -fPIC -shared \
	-Wl,--whole-archive $LUAJIT_VER/src/libluajit.a android/lua_wrap.o -Wl,--no-whole-archive -lm \
	-o ../Assets/LUAnity/Plugins/Android/libluanity.so

make -C $LUAJIT_VER clean

echo "==== Successfully built Assets/LUAnity/Plugins/Android/libluanity.so ===="
