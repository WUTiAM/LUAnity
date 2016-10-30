#!/bin/bash

LUAJIT_VER="lua-5.1.5"

# Build liblua.a for x86_64
make -C $LUAJIT_VER clean
make macosx -j2 -C $LUAJIT_VER BUILDMODE=static
cp $LUAJIT_VER/src/liblua.a osx/liblua-x86_64.a
# Build luanity.bundle
cd osx/
xcodebuild
cd ..
# Copy to target folder
mv -r osx/Build/Release/luanity.bundle ../Plugins/

make -C $LUAJIT_VER clean

echo "==== Successfully built Plugins/luanity.bundle ===="
