namespace LUAnity
{
	using System;
	using System.Collections.Generic;
	using System.Reflection;
	using UnityEngine;

	public class Lua : IDisposable
	{
		const string INIT_LUANET = @"
local metatable = {}
local rawget = rawget
local import_type = luanet.import_type
local load_assembly = luanet.load_assembly
luanet.error, luanet.type = error, type

-- Lookup a .NET identifier component
function metatable:__index(key) -- key is e.g. 'Form'
	-- Get the fully-qualified name, e.g. 'System.Windows.Forms.Form'
	local fqn = rawget(self,'.fqn')
	fqn = ((fqn and fqn .. '.') or '') .. key

	-- Try to find either a luanet function or a CLR type
	local obj = rawget(luanet,key) or import_type(fqn)

	-- If key is neither a luanet function or a CLR type, then it is simply
	-- an identifier component.
	if obj == nil then
		-- It might be an assembly, so we load it too.
		pcall(load_assembly,fqn)
		obj = { ['.fqn'] = fqn }
		setmetatable(obj, metatable)
	end

	-- Cache this lookup
	rawset(self, key, obj)
	return obj
end

-- A non-type has been called; e.g. foo = System.Foo()
function metatable:__call(...)
	error('No such type: ' .. rawget(self,'.fqn'), 2)
end

-- This is the root of the .NET namespace
luanet['.fqn'] = false
setmetatable(luanet, metatable)

-- Preload the mscorlib assembly
luanet.load_assembly('mscorlib')
		";

		// [luaState] => _translator
		static Dictionary<IntPtr, ObjectTranslator> _translators = new Dictionary<IntPtr, ObjectTranslator>();

		#region Globals auto-complete
		readonly List<string> _globals = new List<string> ();
		#endregion

		IntPtr _luaState;
		ObjectTranslator _translator;

		LuaCSFunction _panicCallback;
		LuaCSFunction _printFunction;

		public static ObjectTranslator GetObjectTranslator( IntPtr luaState )
		{
			return ( _translators.ContainsKey( luaState ) ) ? _translators[luaState] : null;
		}

		public Lua()
		{
			_luaState = LuaLib.luaL_newstate();
			if( _luaState == IntPtr.Zero )
			{
				Debug.LogError( "Failed to create Lua state!" );
				return;
			}

			LuaLib.luaL_openlibs( _luaState );

			LuaLib.lua_pushstring( _luaState, "LUAINTERFACE LOADED" );		// s
			LuaLib.lua_pushboolean( _luaState, true );						// s|b
			LuaLib.lua_settable( _luaState, LuaIndexes.LUA_REGISTRYINDEX ); // Set ["LUAINTERFACE LOADED"]=true in registry
			LuaLib.lua_newtable( _luaState );								// t
			LuaLib.lua_setglobal( _luaState, "luanet" );					// Set ["luanet"]=t in _G
			LuaLib.lua_pushvalue( _luaState, LuaIndexes.LUA_GLOBALSINDEX ); // _G
			LuaLib.lua_getglobal( _luaState, "luanet" );					// _G|t
			LuaLib.lua_pushstring( _luaState, "getmetatable" );				// _G|t|s
			LuaLib.lua_getglobal( _luaState, "getmetatable" );				// _G|t|s|f
			LuaLib.lua_settable( _luaState, -3 );                           // _G|t

			// Set luanet as global for object _translator
			LuaLib.lua_replace( _luaState, LuaIndexes.LUA_GLOBALSINDEX );
			_translator = new ObjectTranslator( this, _luaState );
			LuaLib.lua_replace( _luaState, LuaIndexes.LUA_GLOBALSINDEX );

			_translators[_luaState] = _translator;

			// We need to keep this in a managed reference so the delegate doesn't get garbage collected
			_panicCallback = new LuaCSFunction( _PanicCallback );
			LuaLib.lua_atpanic( _luaState, _panicCallback );

			_printFunction = new LuaCSFunction( _Print );
			LuaLib.lua_pushstdcallcfunction( _luaState, _printFunction );
			LuaLib.lua_setfield( _luaState, LuaIndexes.LUA_GLOBALSINDEX, "print" );

			LuaLib.luaL_dostring( _luaState, INIT_LUANET );
		}

		[MonoPInvokeCallback( typeof( LuaCSFunction ) )]
		static int _PanicCallback( IntPtr luaState )
		{
			string reason = string.Format( "Unprotected error in call to Lua API ({0})", LuaLib.lua_tostring( luaState, -1 ) );
			throw new LuaException( reason );
		}

		[MonoPInvokeCallback( typeof( LuaCSFunction ) )]
		static int _Print( IntPtr luaState )
		{
			// For each argument we'll 'tostring' it
			int n = LuaLib.lua_gettop( luaState );			// ...
			string s = string.Empty;

			LuaLib.lua_getglobal( luaState, "tostring" );	// ...|f

			for( int i = 1; i <= n; ++i )
			{
				LuaLib.lua_pushvalue( luaState, -1 );       // ...|f|f
				LuaLib.lua_pushvalue( luaState, i );        // ...|f|f|o
				LuaLib.lua_call( luaState, 1, 1 );          // ...|f|s
				s += LuaLib.lua_tostring( luaState, -1 );

				if( i > 1 )
				{
					s += "\t";
				}

				LuaLib.lua_pop( luaState, 1 );				// ...

				Debug.Log( "[LOG:Lua] " + s );
			}

			return 0;
		}

		// Indexer for global variables from the LuaInterpreter
		// Supports navigation of tables by using . operator
		public object this[string fullPath]
		{
			get
			{
				int oldTop = LuaLib.lua_gettop( _luaState );

				object returnValue = null;
				string[] path = fullPath.Split( new char[] { '.' } );
				LuaLib.lua_getglobal( _luaState, path[0] );
				returnValue = _translator.GetObject( _luaState, -1 );

				if( path.Length > 1 )
				{
					LuaObjectBase dispose = returnValue as LuaObjectBase;

					string[] remainingPath = new string[path.Length - 1];
					Array.Copy( path, 1, remainingPath, 0, path.Length - 1 );
					returnValue = GetObject( remainingPath );

					if( dispose != null )
					{
						dispose.Dispose();
					}
				}

				LuaLib.lua_settop( _luaState, oldTop );

				return returnValue;
			}
			set
			{
				int oldTop = LuaLib.lua_gettop( _luaState );

				string[] path = fullPath.Split( new char[] { '.' } );
				if( path.Length == 1 )
				{
					_translator.Push( _luaState, value );
					LuaLib.lua_setglobal( _luaState, fullPath );
				}
				else
				{
					LuaLib.lua_getglobal( _luaState, path[0] );
					string[] remainingPath = new string[path.Length - 1];
					Array.Copy( path, 1, remainingPath, 0, path.Length - 1 );
					SetObject( remainingPath, value );
				}

				LuaLib.lua_settop( _luaState, oldTop );

				// Globals auto-complete
				if( value == null )
				{
					// Remove now obsolete entries
					_globals.Remove( fullPath );
				}
				else
				{
					// Add new entries
					if( !_globals.Contains( fullPath ) )
					{
						RegisterGlobal( fullPath, value.GetType(), 0 );
					}
				}
			}
		}

		#region Globals auto-complete
		private void RegisterGlobal( string path, Type type, int recursionCounter )
		{
			// If the type is a global method, list it directly
			if( type == typeof( LuaCSFunction ) )
			{
				// Format for easy method invocation
				_globals.Add( path + "(" );
			}
			// If the type is a class or an interface and recursion hasn't been running too long, list the members
			else if( ( type.IsClass() || type.IsInterface() ) && type != typeof( string ) && recursionCounter < 2 )
			{
				#region Methods
				foreach( var method in type.GetMethods( BindingFlags.Public | BindingFlags.Instance ) )
				{
					string name = method.Name;
					if(
						// Check that the LuaHideAttribute and LuaGlobalAttribute were not applied
						( method.GetCustomAttributes( typeof( LuaHideAttribute ), false ).Length == 0 ) &&
						( method.GetCustomAttributes( typeof( LuaGlobalAttribute ), false ).Length == 0 ) &&
						// Exclude some generic .NET methods that wouldn't be very usefull in Lua
						name != "GetType" && name != "GetHashCode" && name != "Equals" &&
						name != "ToString" && name != "Clone" && name != "Dispose" &&
						name != "GetEnumerator" && name != "CopyTo" &&
						!name.StartsWith( "get_", StringComparison.Ordinal ) &&
						!name.StartsWith( "set_", StringComparison.Ordinal ) &&
						!name.StartsWith( "add_", StringComparison.Ordinal ) &&
						!name.StartsWith( "remove_", StringComparison.Ordinal ) )
					{
						// Format for easy method invocation
						string command = path + ":" + name + "(";

						if( method.GetParameters().Length == 0 )
							command += ")";
						_globals.Add( command );
					}
				}
				#endregion

				#region Fields
				foreach( var field in type.GetFields( BindingFlags.Public | BindingFlags.Instance ) )
				{
					if(
						// Check that the LuaHideAttribute and LuaGlobalAttribute were not applied
						( field.GetCustomAttributes( typeof( LuaHideAttribute ), false ).Length == 0 ) &&
						( field.GetCustomAttributes( typeof( LuaGlobalAttribute ), false ).Length == 0 ) )
					{
						// Go into recursion for members
						RegisterGlobal( path + "." + field.Name, field.FieldType, recursionCounter + 1 );
					}
				}
				#endregion

				#region Properties
				foreach( var property in type.GetProperties( BindingFlags.Public | BindingFlags.Instance ) )
				{
					if(
						// Check that the LuaHideAttribute and LuaGlobalAttribute were not applied
						( property.GetCustomAttributes( typeof( LuaHideAttribute ), false ).Length == 0 ) &&
						( property.GetCustomAttributes( typeof( LuaGlobalAttribute ), false ).Length == 0 )
						// Exclude some generic .NET properties that wouldn't be very useful in Lua
						&& property.Name != "Item" )
					{
						// Go into recursion for members
						RegisterGlobal( path + "." + property.Name, property.PropertyType, recursionCounter + 1 );
					}
				}
				#endregion
			}
			else
			{
				_globals.Add( path ); // Otherwise simply add the element to the list
			}
		}
		#endregion

		// Executes a Lua chunk and returns all the chunk's return values in an array.
		public object[] DoString( byte[] chunk, string chunkName = "chunk" )
		{
			int oldTop = LuaLib.lua_gettop( _luaState );

			if( LuaLib.luaL_loadbuffer( _luaState, chunk, chunk.Length, chunkName ) == 0 )
			{
				if( LuaLib.lua_pcall( _luaState, 0, -1, 0 ) == 0 )
				{
					return _translator.PopValues( _luaState, oldTop );
				}
			}

			ThrowExceptionFromError( oldTop );

			return null; // Never reached - keeps compiler happy
		}

		// Gets a field of the table corresponding to the provided reference using rawget (do not use metatables)
		internal object RawGetObject( int reference, string field )
		{
			int oldTop = LuaLib.lua_gettop( _luaState );

			LuaLib.lua_rawgeti( _luaState, LuaIndexes.LUA_REGISTRYINDEX, reference );
			LuaLib.lua_pushstring( _luaState, field );
			LuaLib.lua_rawget( _luaState, -2 );
			object obj = _translator.GetObject( _luaState, -1 );

			LuaLib.lua_settop( _luaState, oldTop );

			return obj;
		}

		// Gets a field of the table or userdata corresponding to the provided reference
		internal object GetObject( int reference, string field )
		{
			int oldTop = LuaLib.lua_gettop( _luaState );

			LuaLib.lua_rawgeti( _luaState, LuaIndexes.LUA_REGISTRYINDEX, reference );
			object returnValue = GetObject( field.Split( new char[] { '.' } ) );

			LuaLib.lua_settop( _luaState, oldTop );

			return returnValue;
		}

		// Gets a numeric field of the table or userdata corresponding the the provided reference
		internal object GetObject( int reference, object field )
		{
			int oldTop = LuaLib.lua_gettop( _luaState );

			LuaLib.lua_rawgeti( _luaState, LuaIndexes.LUA_REGISTRYINDEX, reference );
			_translator.Push( _luaState, field );
			LuaLib.lua_gettable( _luaState, -2 );
			object returnValue = _translator.GetObject( _luaState, -1 );

			LuaLib.lua_settop( _luaState, oldTop );

			return returnValue;
		}

		// Navigates a table in the top of the stack, returning the value of the specified field
		internal object GetObject( string[] remainingPath )
		{
			object returnValue = null;

			for( int i = 0; i < remainingPath.Length; ++i )
			{
				LuaLib.lua_pushstring( _luaState, remainingPath[i] );
				LuaLib.lua_gettable( _luaState, -2 );
				returnValue = _translator.GetObject( _luaState, -1 );

				if( returnValue == null )
					break;
			}

			return returnValue;
		}

		// Sets a field of the table or userdata corresponding the the provided reference to the provided value
		internal void SetObject( int reference, string field, object val )
		{
			int oldTop = LuaLib.lua_gettop( _luaState );

			LuaLib.lua_rawgeti( _luaState, LuaIndexes.LUA_REGISTRYINDEX, reference );
			SetObject( field.Split( new char[] { '.' } ), val );

			LuaLib.lua_settop( _luaState, oldTop );
		}

		// Sets a numeric field of the table or userdata corresponding the the provided reference to the provided value
		internal void SetObject( int reference, object field, object val )
		{
			int oldTop = LuaLib.lua_gettop( _luaState );

			LuaLib.lua_rawgeti( _luaState, LuaIndexes.LUA_REGISTRYINDEX, reference );
			_translator.Push( _luaState, field );
			_translator.Push( _luaState, val );
			LuaLib.lua_settable( _luaState, -3 );

			LuaLib.lua_settop( _luaState, oldTop );
		}

		// Navigates a table to set the value of one of its fields
		internal void SetObject( string[] remainingPath, object val )
		{
			for( int i = 0; i < remainingPath.Length - 1; ++i )
			{
				LuaLib.lua_pushstring( _luaState, remainingPath[i] );
				LuaLib.lua_gettable( _luaState, -2 );
			}
			LuaLib.lua_pushstring( _luaState, remainingPath[remainingPath.Length - 1] );
			_translator.Push( _luaState, val );
			LuaLib.lua_settable( _luaState, -3 );
		}

		 // Registers an object's method as a Lua function (global or table field)
		 // The method may have any signature
		public LuaFunction RegisterFunction( string path, object target, MethodBase function)
		{
			// We leave nothing on the stack when we are done
			int oldTop = LuaLib.lua_gettop( _luaState );

			LuaMethodWrapper wrapper = new LuaMethodWrapper( _translator, target, new ProxyType( function.DeclaringType ), function );
			_translator.Push( _luaState, wrapper.InvokeFunction );

			this[path] = _translator.GetObject( _luaState, -1 );
			LuaFunction f = GetFunction( path );

			LuaLib.lua_settop( _luaState, oldTop );

			return f;
		}

		// Gets a function global variable
		public LuaFunction GetFunction( string fullPath )
		{
			object obj = this[fullPath];
			return ( obj is LuaCSFunction ? new LuaFunction( (LuaCSFunction)obj, this ) : (LuaFunction)obj );
		}

		internal void PushFunction( LuaCSFunction function )
		{
			_translator.PushFunction( _luaState, function );
		}

		// Calls the object as a function with the provided arguments and casting returned values to the types in
		// returnTypes before returning them in an array
		internal object[] CallFunction( object function, object[] args, Type[] returnTypes = null )
		{
			int nArgs = 0;
			int oldTop = LuaLib.lua_gettop( _luaState );

			if( !LuaLib.lua_checkstack( _luaState, args.Length + 6 ) )
			{
				throw new LuaException( "Lua stack overflow" );
			}

			_translator.Push( _luaState, function );
			if( args != null )
			{
				nArgs = args.Length;
				for( int i = 0; i < args.Length; i++ )
				{
					_translator.Push( _luaState, args[i] );
				}
			}
			int error = LuaLib.lua_pcall( _luaState, nArgs, -1, 0 );
			if( error != 0 )
			{
				ThrowExceptionFromError( oldTop );
			}

			if( returnTypes != null )
			{
				return _translator.PopValues( _luaState, oldTop, returnTypes );
			}
			else
			{
				return _translator.PopValues( _luaState, oldTop );
			}
		}

		// Creates a new table as a global variable or as a field inside an existing table
		internal void NewTable( string fullPath )
		{
			int oldTop = LuaLib.lua_gettop( _luaState );

			string[] path = fullPath.Split( new char[] { '.' } );
			if( path.Length == 1 )
			{
				LuaLib.lua_newtable( _luaState );
				LuaLib.lua_setglobal( _luaState, fullPath );
			}
			else
			{
				LuaLib.lua_getglobal( _luaState, path[0] );

				for( int i = 1; i < path.Length - 1; i++ )
				{
					LuaLib.lua_pushstring( _luaState, path[i] );
					LuaLib.lua_gettable( _luaState, -2 );
				}

				LuaLib.lua_pushstring( _luaState, path[path.Length - 1] );
				LuaLib.lua_newtable( _luaState );
				LuaLib.lua_settable( _luaState, -3 );
			}

			LuaLib.lua_settop( _luaState, oldTop );
		}

		internal Dictionary<object, object> GetTableDict( LuaTable table )
		{
			var dict = new Dictionary<object, object>();

			int oldTop = LuaLib.lua_gettop( _luaState );
			_translator.Push( _luaState, table );
			LuaLib.lua_pushnil( _luaState );

			while( LuaLib.lua_next( _luaState, -2 ) != 0 )
			{
				dict[_translator.GetObject( _luaState, -2 )] = _translator.GetObject( _luaState, -1 );
				LuaLib.lua_settop( _luaState, -2 );
			}

			LuaLib.lua_settop( _luaState, oldTop );

			return dict;
		}

		// Assuming we have a Lua error string sitting on the stack, throw a C# exception out to the user's app
		// Thrown if the script caused an exception
		internal void ThrowExceptionFromError( int oldTop )
		{
			object err = _translator.GetObject( _luaState, -1 );
			LuaLib.lua_settop( _luaState, oldTop );

			// A pre-wrapped exception - just rethrow it (stack trace of InnerException will be preserved)
			LuaScriptException e = err as LuaScriptException;
			if( e != null )
				throw e;

			// A non-wrapped Lua error (best interpreted as a string) - wrap it and throw it
			if( err == null )
			{
				err = "Unknown Lua Error";
			}
			throw new LuaScriptException( err.ToString(), "" );
		}

		// Convert C# exceptions into Lua errors
		// Returns>num of things on stack, null for no pending exception
		internal int SetPendingException( Exception e )
		{
			if( e != null )
			{
				_translator.ThrowError( _luaState, e );
				LuaLib.lua_pushnil( _luaState );

				return 1;
			}

			return 0;
		}

		internal bool CompareRef( int reference1, int reference2 )
		{
			int top = LuaLib.lua_gettop( _luaState );

			LuaLib.lua_rawgeti( _luaState, LuaIndexes.LUA_REGISTRYINDEX, reference1 );
			LuaLib.lua_rawgeti( _luaState, LuaIndexes.LUA_REGISTRYINDEX, reference2 );
			int equal = LuaLib.lua_equal( _luaState, -1, -2 );

			LuaLib.lua_settop( _luaState, top );

			return ( equal != 0 );
		}

		// Lets go of a previously allocated reference to a table, function or userdata
		internal void Dispose( int reference )
		{
			if( _luaState != IntPtr.Zero )
			{
				LuaLib.luaL_unref( _luaState, LuaIndexes.LUA_REGISTRYINDEX, reference );
			}
		}

		public void Close()
		{
			if( _luaState != IntPtr.Zero )
			{
				_translators.Remove( _luaState );

				LuaLib.lua_close( _luaState );
			}
		}

		#region IDisposable members
		public void Dispose()
		{
			if( _translator != null )
			{
				_translator = null;
			}

			Close();

			GC.Collect();
			GC.WaitForPendingFinalizers();
		}
		#endregion
	}
}
