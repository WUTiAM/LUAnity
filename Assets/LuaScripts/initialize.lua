--
-- Lua environment initialization
--

math.randomseed( os.time() )

NUMBER_EC = 0.0001 -- Floating number error correction

--
-- Redefinition of 'require', 'print', 'log', error' and 'assert'
--

__reqs = {}
function require( file )
	local r = __reqs[file]

	if r == nil then
		__reqs[file] = false

		r = RequireLua( file )
		if r ~= nil then
			__reqs[file] = r
		end
	end

	if r then
		return r
	end
end

local function _getCallStack( content )
	-- 如果已开启debug模式， 打印 call stack， 辅助调试
	local debugEnabled = false
	if getGlobal ~= nil then
		debugEnabled = getGlobal( "_DEBUG_" )
	end

	if debugEnabled then
		if content == nil then
			content = ""
		end
		
		local i = 3 -- start from 3 in the stack (1: the current function; 2: the print/log/error/assert function)
		local info = debug.getinfo(i)
		while info do
			i = i + 1
			content = string.format("%s\n-> %s (%s.lua: %d)",
				content,
				info.name or "<noname>",
				info.source,
				info.currentline)
			info = debug.getinfo(i)
		end
	end	

	return content
end

local function _getStringFromVariantParams( args )
	local msg = ""

	for i, v in ipairs( args ) do
		msg = msg .. " " .. tostring( v )
	end

	return msg
end

function print( ... )
	local msg = _getStringFromVariantParams( { ... } )
	msg = _getCallStack( msg )
	LuaPrint( msg )
end

function log( ... )
	local msg = _getStringFromVariantParams( { ... } )
	msg = _getCallStack( msg )
	LuaLog( msg )
end

function error( ... )
	local msg = _getStringFromVariantParams( { ... } )
	msg = _getCallStack( msg )
	LuaError( msg )
end

local _originalAssert = assert
function assert( v, message )
	local msg = message or "assertion failed!"
	msg = _getCallStack( msg )
	_originalAssert( v, msg )
end

--
-- Lua extensions
--

require( "enum" )
require( "string_ext" )
require( "table_ext" )
require( "type_check" )

--
-- Import assemblies, classes and struct from C# to Lua
--

luanet.load_assembly( "Assembly-CSharp" )
luanet.load_assembly( "UnityEngine" )

-- C# classes
DateTime = luanet.import_type( "System.DateTime" )

-- Unity classes
Animation = luanet.import_type( "UnityEngine.Animation" )
Application = luanet.import_type( "UnityEngine.Application" )
Camera = luanet.import_type( "UnityEngine.Camera" )
GameObject = luanet.import_type( "UnityEngine.GameObject" )
Input = luanet.import_type( "UnityEngine.Input" )
PlayerPrefs = luanet.import_type( "UnityEngine.PlayerPrefs" )
RenderSettings = luanet.import_type( "UnityEngine.RenderSettings" )
Screen = luanet.import_type( "UnityEngine.Screen" )
Time = luanet.import_type('UnityEngine.Time')
-- Unity structs
Color = luanet.import_type( "UnityEngine.Color" )
Quaternion = luanet.import_type( "UnityEngine.Quaternion" )
Rect = luanet.import_type( "UnityEngine.Rect" )
Vector2 = luanet.import_type( "UnityEngine.Vector2" )
Vector3 = luanet.import_type( "UnityEngine.Vector3" )
Vector4 = luanet.import_type( "UnityEngine.Vector4" )

-- Others classes
AssetLoader = luanet.import_type( "AssetLoader" )
L2U = luanet.import_type( "L2U" )

--
-- More Lua extensions
--

-- proto-gen-lua
--require( "pb/pb_initialize")

--
-- 从此往后，禁止隐式声明、访问全局变量，需要通过 declareGlobal、getGlobal 显式操作
--

require( "restrict_global_from_now_on" )
