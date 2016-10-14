namespace LUAnity
{
	using System;
	using System.Runtime.InteropServices;

#pragma warning disable 414
	public class MonoPInvokeCallbackAttribute : Attribute
	{
		private Type type;

		public MonoPInvokeCallbackAttribute( Type t )
		{
			type = t;
		}
	}
#pragma warning restore 414

	public enum LuaTypes
	{
		LUA_TNONE = -1,
		LUA_TNIL = 0,
		LUA_TBOOLEAN = 1,
		LUA_TLIGHTUSERDATA = 2,
		LUA_TNUMBER = 3,
		LUA_TSTRING = 4,
		LUA_TTABLE = 5,
		LUA_TFUNCTION = 6,
		LUA_TUSERDATA = 7,
		LUA_TTHREAD = 8,
	}

	public enum LuaGCOptions
	{
		LUA_GCSTOP = 0,
		LUA_GCRESTART = 1,
		LUA_GCCOLLECT = 2,
		LUA_GCCOUNT = 3,
		LUA_GCCOUNTB = 4,
		LUA_GCSTEP = 5,
		LUA_GCSETPAUSE = 6,
		LUA_GCSETSTEPMUL = 7,
	}

	sealed class LuaIndexes
	{
		public static int LUA_REGISTRYINDEX = -10000;
		public static int LUA_ENVIRONINDEX = -10001;
		public static int LUA_GLOBALSINDEX = -10002;
	}

	[StructLayout( LayoutKind.Sequential )]
	public struct ReaderInfo
	{
		public string chunkData;
		public bool finished;
	}

	public delegate int LuaCSFunction( IntPtr luaState );
	public delegate string LuaChunkReader( IntPtr luaState, ref ReaderInfo data, ref uint size );

	public class LuaLib
	{
		public const int LUA_MULTRET = -1;

#if UNITY_EDITOR
		const string LIBNAME = "lua";
#elif UNITY_IOS
		const string LIBNAME = "__Internal";
#else
		const string LIBNAME = "lua";
#endif

		//
		// APIs
		//
		[DllImport( LIBNAME, CallingConvention = CallingConvention.Cdecl )]
		public static extern void lua_atpanic( IntPtr luaState, LuaCSFunction panicFunc );
		[DllImport( LIBNAME, CallingConvention = CallingConvention.Cdecl )]
		public static extern int lua_call( IntPtr luaState, int nArgs, int nResults );
		[DllImport( LIBNAME, CallingConvention = CallingConvention.Cdecl )]
		public static extern bool lua_checkstack( IntPtr luaState, int extra );
		[DllImport( LIBNAME, CallingConvention = CallingConvention.Cdecl )]
		public static extern void lua_close( IntPtr luaState );
		[DllImport( LIBNAME, CallingConvention = CallingConvention.Cdecl )]
		public static extern void lua_createtable( IntPtr luaState, int nArr, int nRec );
		[DllImport( LIBNAME, CallingConvention = CallingConvention.Cdecl )]
		public static extern int lua_equal( IntPtr luaState, int stackPos1, int stackPos2 );
		[DllImport( LIBNAME, CallingConvention = CallingConvention.Cdecl )]
		public static extern void lua_error( IntPtr luaState );
		[DllImport( LIBNAME, CallingConvention = CallingConvention.Cdecl )]
		public static extern int lua_gc( IntPtr luaState, LuaGCOptions what, int data );
		[DllImport( LIBNAME, CallingConvention = CallingConvention.Cdecl )]
		public static extern void lua_getfield( IntPtr luaState, int stackPos, string key );
		public static void lua_getglobal( IntPtr luaState, string name )
		{
			lua_getfield( luaState, LuaIndexes.LUA_GLOBALSINDEX, name );
		}
		[DllImport( LIBNAME, CallingConvention = CallingConvention.Cdecl )]
		public static extern int lua_getmetatable( IntPtr luaState, int stackPos );
		[DllImport( LIBNAME, CallingConvention = CallingConvention.Cdecl )]
		public static extern void lua_gettable( IntPtr luaState, int stackPos );
		[DllImport( LIBNAME, CallingConvention = CallingConvention.Cdecl )]
		public static extern int lua_gettop( IntPtr luaState );
		[DllImport( LIBNAME, CallingConvention = CallingConvention.Cdecl )]
		public static extern void lua_insert( IntPtr luaState, int newTop );
		public static bool lua_isboolean( IntPtr luaState, int stackPos )
		{
			return lua_type( luaState, stackPos ) == LuaTypes.LUA_TBOOLEAN;
		}
		[DllImport( LIBNAME, CallingConvention = CallingConvention.Cdecl )]
		public static extern bool lua_iscfunction( IntPtr luaState, int stackPos );
		public static bool lua_isfunction( IntPtr luaState, int stackPos )
		{
			return lua_type( luaState, stackPos ) == LuaTypes.LUA_TFUNCTION;
		}
		public static bool lua_islightuserdata( IntPtr luaState, int stackPos )
		{
			return lua_type( luaState, stackPos ) == LuaTypes.LUA_TLIGHTUSERDATA;
		}
		public static bool lua_isnil( IntPtr luaState, int stackPos )
		{
			return ( lua_type( luaState, stackPos ) == LuaTypes.LUA_TNIL );
		}
		public static bool lua_isnoneornil( IntPtr luaState, int stackPos )
		{
			return ( lua_type( luaState, stackPos ) <= 0 );
		}
		[DllImport( LIBNAME, CallingConvention = CallingConvention.Cdecl )]
		public static extern bool lua_isnumber( IntPtr luaState, int stackPos );
		[DllImport( LIBNAME, CallingConvention = CallingConvention.Cdecl )]
		public static extern bool lua_isstring( IntPtr luaState, int stackPos );
		public static bool lua_istable( IntPtr luaState, int stackPos )
		{
			return ( lua_type( luaState, stackPos ) == LuaTypes.LUA_TTABLE );
		}
		public static bool lua_isthread( IntPtr luaState, int stackPos )
		{
			return ( lua_type( luaState, stackPos ) == LuaTypes.LUA_TTHREAD );
		}
		[DllImport( LIBNAME, CallingConvention = CallingConvention.Cdecl )]
		public static extern int lua_isuserdata( IntPtr luaState, int stackPos );
		[DllImport( LIBNAME, CallingConvention = CallingConvention.Cdecl )]
		public static extern int lua_lessthan( IntPtr luaState, int stackPos1, int stackPos2 );
		[DllImport( LIBNAME, CallingConvention = CallingConvention.Cdecl )]
		public static extern int lua_load( IntPtr luaState, LuaChunkReader chunkReader, ref ReaderInfo data, string chunkName );
		public static void lua_newtable( IntPtr luaState )
		{
			lua_createtable( luaState, 0, 0 );
		}
		[DllImport( LIBNAME, CallingConvention = CallingConvention.Cdecl )]
		public static extern IntPtr lua_newthread( IntPtr luaState );
		[DllImport( LIBNAME, CallingConvention = CallingConvention.Cdecl )]
		public static extern IntPtr lua_newuserdata( IntPtr luaState, int size );
		[DllImport( LIBNAME, CallingConvention = CallingConvention.Cdecl )]
		public static extern int lua_next( IntPtr luaState, int stackPos );
		[DllImport( LIBNAME, CallingConvention = CallingConvention.Cdecl )]
		public static extern int lua_objlen( IntPtr luaState, int stackPos );
		[DllImport( LIBNAME, CallingConvention = CallingConvention.Cdecl )]
		public static extern int lua_pcall( IntPtr luaState, int nArgs, int nResults, int errFunc );
		public static void lua_pop( IntPtr luaState, int n )
		{
			LuaLib.lua_settop( luaState, -( n ) - 1 );
		}
		[DllImport( LIBNAME, CallingConvention = CallingConvention.Cdecl )]
		public static extern void lua_pushboolean( IntPtr luaState, bool value );
		[DllImport( LIBNAME, CallingConvention = CallingConvention.Cdecl )]
		public static extern void lua_pushinteger( IntPtr luaState, int value );
		[DllImport( LIBNAME, CallingConvention = CallingConvention.Cdecl )]
		public static extern void lua_pushlightuserdata( IntPtr luaState, IntPtr udata );
		[DllImport( LIBNAME, CallingConvention = CallingConvention.Cdecl )]
		public static extern void lua_pushlstring( IntPtr luaState, string str, int len );
		[DllImport( LIBNAME, CallingConvention = CallingConvention.Cdecl )]
		public static extern void lua_pushnil( IntPtr luaState );
		[DllImport( LIBNAME, CallingConvention = CallingConvention.Cdecl )]
		public static extern void lua_pushnumber( IntPtr luaState, double number );
		[DllImport( LIBNAME, CallingConvention = CallingConvention.Cdecl )]
		public static extern void lua_pushstring( IntPtr luaState, string str );
		[DllImport( LIBNAME, CallingConvention = CallingConvention.Cdecl )]
		public static extern int lua_pushthread( IntPtr L );
		// Use lua_pushstdcallcfunction instead of lua_pushcclosure/lua_pushcfunction/lua_register directly
		// since Marshaling uses stdcall but C uses cdecl
		[DllImport( LIBNAME, CallingConvention = CallingConvention.Cdecl )]
		public static extern void lua_pushstdcallcfunction( IntPtr luaState, IntPtr wrapper );
		public static void lua_pushstdcallcfunction( IntPtr luaState, LuaCSFunction func )
		{
			lua_pushstdcallcfunction( luaState, Marshal.GetFunctionPointerForDelegate( func ) );
		}
		[DllImport( LIBNAME, CallingConvention = CallingConvention.Cdecl )]
		public static extern void lua_pushvalue( IntPtr luaState, int stackPos );
		[DllImport( LIBNAME, CallingConvention = CallingConvention.Cdecl )]
		public static extern int lua_rawequal( IntPtr luaState, int stackPos1, int stackPos2 );
		[DllImport( LIBNAME, CallingConvention = CallingConvention.Cdecl )]
		public static extern void lua_rawget( IntPtr luaState, int stackPos );
		[DllImport( LIBNAME, CallingConvention = CallingConvention.Cdecl )]
		public static extern void lua_rawgeti( IntPtr luaState, int tableIndex, int stackPos );
		[DllImport( LIBNAME, CallingConvention = CallingConvention.Cdecl )]
		public static extern void lua_rawset( IntPtr luaState, int stackPos );
		[DllImport( LIBNAME, CallingConvention = CallingConvention.Cdecl )]
		public static extern void lua_rawseti( IntPtr luaState, int tableIndex, int stackPos );
		[DllImport( LIBNAME, CallingConvention = CallingConvention.Cdecl )]
		public static extern void lua_remove( IntPtr luaState, int stackPos );
		[DllImport( LIBNAME, CallingConvention = CallingConvention.Cdecl )]
		public static extern void lua_replace( IntPtr luaState, int stackPos );
		[DllImport( LIBNAME, CallingConvention = CallingConvention.Cdecl )]
		public static extern int lua_resume( IntPtr luaState, int nArg );
		[DllImport( LIBNAME, CallingConvention = CallingConvention.Cdecl )]
		public static extern void lua_setfield( IntPtr luaState, int stackPos, string key );
		public static void lua_setglobal( IntPtr luaState, string name )
		{
			lua_setfield( luaState, LuaIndexes.LUA_GLOBALSINDEX, name );
		}
		[DllImport( LIBNAME, CallingConvention = CallingConvention.Cdecl )]
		public static extern void lua_setmetatable( IntPtr luaState, int stackPos );
		[DllImport( LIBNAME, CallingConvention = CallingConvention.Cdecl )]
		public static extern void lua_settable( IntPtr luaState, int stackPos );
		[DllImport( LIBNAME, CallingConvention = CallingConvention.Cdecl )]
		public static extern void lua_settop( IntPtr luaState, int newTop );
		[DllImport( LIBNAME, CallingConvention = CallingConvention.Cdecl )]
		public static extern int lua_status( IntPtr luaState );
		[DllImport( LIBNAME, CallingConvention = CallingConvention.Cdecl )]
		public static extern bool lua_toboolean( IntPtr luaState, int stackPos );
		[DllImport( LIBNAME, CallingConvention = CallingConvention.Cdecl )]
		public static extern IntPtr lua_tocfunction( IntPtr luaState, int stackPos );
		[DllImport( LIBNAME, CallingConvention = CallingConvention.Cdecl )]
		public static extern IntPtr lua_tolstring( IntPtr luaState, int stackPos, out IntPtr strLen );
		[DllImport( LIBNAME, CallingConvention = CallingConvention.Cdecl )]
		public static extern double lua_tonumber( IntPtr luaState, int stackPos );
		public static string lua_tostring( IntPtr luaState, int stackPos )
		{
			IntPtr len;
			IntPtr str = lua_tolstring( luaState, stackPos, out len );
			int strLen = len.ToInt32();
			if( str != IntPtr.Zero )
			{
				return Marshal.PtrToStringAnsi( str, strLen );
			}
			else
			{
				return null;
			}
		}
		[DllImport( LIBNAME, CallingConvention = CallingConvention.Cdecl )]
		public static extern int lua_tothread( IntPtr luaState, int stackPos );
		[DllImport( LIBNAME, CallingConvention = CallingConvention.Cdecl )]
		public static extern IntPtr lua_touserdata( IntPtr luaState, int stackPos );
		[DllImport( LIBNAME, CallingConvention = CallingConvention.Cdecl )]
		public static extern LuaTypes lua_type( IntPtr luaState, int stackPos );
		[DllImport( LIBNAME, CallingConvention = CallingConvention.Cdecl )]
		public static extern string lua_typename( IntPtr luaState, LuaTypes type );
		[DllImport( LIBNAME, CallingConvention = CallingConvention.Cdecl )]
		public static extern int lua_xmove( IntPtr fromLuaState, IntPtr toLuaState, int n );
		[DllImport( LIBNAME, CallingConvention = CallingConvention.Cdecl )]
		public static extern int lua_yield( IntPtr luaState, int nResults );

		//
		// Auxiliary library
		//
		[DllImport( LIBNAME, CallingConvention = CallingConvention.Cdecl )]
		public static extern int luaL_callmeta( IntPtr luaState, int stackPos, string methodName );
		[DllImport( LIBNAME, CallingConvention = CallingConvention.Cdecl )]
		public static extern bool luaL_checkmetatable( IntPtr luaState, int obj );
		[DllImport( LIBNAME, CallingConvention = CallingConvention.Cdecl )]
		public static extern IntPtr luaL_checkudata( IntPtr luaState, int stackPos, string meta );
		public static int luaL_dofile( IntPtr luaState, string filename )
		{
			int result = luaL_loadfile( luaState, filename );
			if( result == 0 )
			{
				result = lua_pcall( luaState, 0, LUA_MULTRET, 0 );
			}

			return result;
		}
		public static int luaL_dostring( IntPtr luaState, string chunk )
		{
			int result = luaL_loadstring( luaState, chunk );
			if( result == 0 )
			{
				result = lua_pcall( luaState, 0, -1, 0 );
			}

			return result;
		}
		[DllImport( LIBNAME, CallingConvention = CallingConvention.Cdecl )]
		public static extern void luaL_error( IntPtr luaState, string message );
		[DllImport( LIBNAME, CallingConvention = CallingConvention.Cdecl )]
		public static extern bool luaL_getmetafield( IntPtr luaState, int stackPos, string key );
		public static void luaL_getmetatable( IntPtr luaState, string tableName )
		{
			lua_getfield( luaState, LuaIndexes.LUA_REGISTRYINDEX, tableName );
		}
		[DllImport( LIBNAME, CallingConvention = CallingConvention.Cdecl )]
		public static extern string luaL_gsub( IntPtr luaState, string str, string pattern, string replacement );
		[DllImport( LIBNAME, CallingConvention = CallingConvention.Cdecl )]
		public static extern int luaL_loadbuffer( IntPtr luaState, byte[] buff, int size, string chunkName );
		[DllImport( LIBNAME, CallingConvention = CallingConvention.Cdecl )]
		public static extern int luaL_loadfile( IntPtr luaState, string filename );
		[DllImport( LIBNAME, CallingConvention = CallingConvention.Cdecl )]
		public static extern int luaL_loadstring( IntPtr luaState, string chunk );
		[DllImport( LIBNAME, CallingConvention = CallingConvention.Cdecl )]
		public static extern int luaL_newmetatable( IntPtr luaState, string tableName );
		[DllImport( LIBNAME, CallingConvention = CallingConvention.Cdecl )]
		public static extern IntPtr luaL_newstate();
		[DllImport( LIBNAME, CallingConvention = CallingConvention.Cdecl )]
		public static extern void luaL_openlibs( IntPtr luaState );
		[DllImport( LIBNAME, CallingConvention = CallingConvention.Cdecl )]
		public static extern int luaL_ref( IntPtr luaState, int stackPos );
		public static string luaL_typename( IntPtr luaState, int stackPos )
		{
			return lua_typename( luaState, lua_type( luaState, stackPos ) );
		}
		[DllImport( LIBNAME, CallingConvention = CallingConvention.Cdecl )]
		public static extern void luaL_unref( IntPtr luaState, int stackPos, int reference );
		[DllImport( LIBNAME, CallingConvention = CallingConvention.Cdecl )]
		public static extern void luaL_where( IntPtr luaState, int level );

		//
		// LuaNet
		//
		[DllImport( LIBNAME, CallingConvention = CallingConvention.Cdecl )]
		public static extern int luanet_checkudata( IntPtr luaState, int obj, string meta );
		[DllImport( LIBNAME, CallingConvention = CallingConvention.Cdecl )]
		public static extern IntPtr luanet_gettag();
		[DllImport( LIBNAME, CallingConvention = CallingConvention.Cdecl )]
		public static extern int luanet_newudata( IntPtr luaState, int val );
		[DllImport( LIBNAME, CallingConvention = CallingConvention.Cdecl )]
		public static extern int luanet_rawnetobj( IntPtr luaState, int obj );
		[DllImport( LIBNAME, CallingConvention = CallingConvention.Cdecl )]
		public static extern int luanet_tonetobject( IntPtr luaState, int obj );
	}
}
