namespace LUAnity
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Reflection;
	using UnityEngine;

	// Passes objects from the CLR to Lua and vice-versa
	public class ObjectTranslator
	{
		internal Lua Interpreter;
		internal MetaFunctions MetaFunctions;
		internal TypeChecker TypeChecker;

		List<Assembly> _assemblies;

		LuaCSFunction _loadAssemblyFunction;
		LuaCSFunction _importTypeFunction;
		LuaCSFunction _getMethodSigFunction;
		LuaCSFunction _getConstructorSigFunction;
		LuaCSFunction _ctypeFunction;
		LuaCSFunction _enumFromIntFunction;

		// [object index]: object
		readonly Dictionary<int, object> _objects = new Dictionary<int, object>();
		// [object]: object index
		readonly Dictionary<object, int> _objectsBackward = new Dictionary<object, int>();
		int _nextObjectIndex = 0; // We want to ensure that objects always have a unique ID

		//internal EventHandlerContainer pendingEvents = new EventHandlerContainer();

		public ObjectTranslator( Lua interpreter, IntPtr luaState )
		{
			Interpreter = interpreter;
			MetaFunctions = new MetaFunctions( this );
			TypeChecker = new TypeChecker( this );

			_assemblies = new List<Assembly>();

			_CreateLuaObjectList( luaState );
			_CreateIndexingMetaFunction( luaState );
			_CreateBaseClassMetatable( luaState );
			_CreateClassMetatable( luaState );
			_CreateFunctionMetatable( luaState );
			_SetGlobalFunctions( luaState );
		}

		// Sets up the list of objects in the Lua side
		void _CreateLuaObjectList( IntPtr luaState )
		{
			LuaLib.lua_pushstring( luaState, "luaNet_objects" );			// s
			LuaLib.lua_newtable( luaState );								// s|t
			LuaLib.lua_newtable( luaState );								// s|t|mt
			// The values in the metatable are weak
			LuaLib.lua_pushstring( luaState, "__mode" );					// s|t|mt|s
			LuaLib.lua_pushstring( luaState, "v" );							// s|t|mt|s|s
			LuaLib.lua_settable( luaState, -3 );							// s|t|mt
			LuaLib.lua_setmetatable( luaState, -2 );						// s|t
			LuaLib.lua_settable( luaState, LuaIndexes.LUA_REGISTRYINDEX );
		}

		// Registers the indexing function of CLR objects passed to Lua
		void _CreateIndexingMetaFunction( IntPtr luaState )
		{
			LuaLib.lua_pushstring( luaState, "luaNet_indexfunction" );			// s
			LuaLib.luaL_dostring( luaState, MetaFunctions.LuaIndexFunction );	// s|f
			LuaLib.lua_rawset( luaState, LuaIndexes.LUA_REGISTRYINDEX );
		}

		// Creates the metatable for superclasses (the base field of registered tables)
		void _CreateBaseClassMetatable( IntPtr luaState )
		{
			LuaLib.luaL_newmetatable( luaState, "luaNet_searchbase" );						// mt
			LuaLib.lua_pushstring( luaState, "__gc" );										// mt|s
			LuaLib.lua_pushstdcallcfunction( luaState, MetaFunctions.GcFunction );			// mt|s|cf
			LuaLib.lua_settable( luaState, -3 );											// mt
			LuaLib.lua_pushstring( luaState, "__tostring" );								// mt|s
			LuaLib.lua_pushstdcallcfunction( luaState, MetaFunctions.ToStringFunction );	// mt|s|cf
			LuaLib.lua_settable( luaState, -3 );											// mt
			LuaLib.lua_pushstring( luaState, "__index" );									// mt|s
			LuaLib.lua_pushstdcallcfunction( luaState, MetaFunctions.BaseIndexFunction );	// mt|s|cf
			LuaLib.lua_settable( luaState, -3 );											// mt
			LuaLib.lua_pushstring( luaState, "__newindex" );								// mt|s
			LuaLib.lua_pushstdcallcfunction( luaState, MetaFunctions.NewIndexFunction );	// mt|s|cf
			LuaLib.lua_settable( luaState, -3 );											// mt
			LuaLib.lua_settop( luaState, -2 );
		}

		// Creates the metatable for type references
		void _CreateClassMetatable( IntPtr luaState )
		{
			LuaLib.luaL_newmetatable( luaState, "luaNet_class" );								// mt
			LuaLib.lua_pushstring( luaState, "__gc" );											// mt|s
			LuaLib.lua_pushstdcallcfunction( luaState, MetaFunctions.GcFunction );				// mt|s|cf
			LuaLib.lua_settable( luaState, -3 );												// mt
			LuaLib.lua_pushstring( luaState, "__tostring" );									// mt|s
			LuaLib.lua_pushstdcallcfunction( luaState, MetaFunctions.ToStringFunction );		// mt|s|cf
			LuaLib.lua_settable( luaState, -3 );												// mt
			LuaLib.lua_pushstring( luaState, "__index" );                                       // mt|s
			LuaLib.lua_pushstdcallcfunction( luaState, MetaFunctions.ClassIndexFunction );		// mt|s|cf
			LuaLib.lua_settable( luaState, -3 );												// mt
			LuaLib.lua_pushstring( luaState, "__newindex" );                                    // mt|s
			LuaLib.lua_pushstdcallcfunction( luaState, MetaFunctions.ClassNewindexFunction );	// mt|s|cf
			LuaLib.lua_settable( luaState, -3 );												// mt
			LuaLib.lua_pushstring( luaState, "__call" );                                        // mt|s
			LuaLib.lua_pushstdcallcfunction( luaState, MetaFunctions.CallConstructorFunction );	// mt|s|cf
			LuaLib.lua_settable( luaState, -3 );												// mt
			LuaLib.lua_settop( luaState, -2 );
		}

		// Creates the metatable for delegates
		void _CreateFunctionMetatable( IntPtr luaState )
		{
			LuaLib.luaL_newmetatable( luaState, "luaNet_function" );							// mt
			LuaLib.lua_pushstring( luaState, "__gc" );											// mt|s
			LuaLib.lua_pushstdcallcfunction( luaState, MetaFunctions.GcFunction );				// mt|s|cf
			LuaLib.lua_settable( luaState, -3 );												// mt
			LuaLib.lua_pushstring( luaState, "__call" );										// mt|s
			LuaLib.lua_pushstdcallcfunction( luaState, MetaFunctions.ExecuteDelegateFunction );	// mt|s|cf
			LuaLib.lua_settable( luaState, -3 );												// mt
			LuaLib.lua_settop( luaState, -2 );
		}

		// Registers the global functions used by LUAnity
		void _SetGlobalFunctions( IntPtr luaState )
		{
			_loadAssemblyFunction = new LuaCSFunction( _LoadAssembly );
			_importTypeFunction = new LuaCSFunction( _ImportType );
			_getMethodSigFunction = new LuaCSFunction( _GetMethodSignature );
			_getConstructorSigFunction = new LuaCSFunction( GetConstructorSignature );
			_ctypeFunction = new LuaCSFunction( _CType );
			_enumFromIntFunction = new LuaCSFunction( _EnumFromIntOrString );

			LuaLib.lua_pushstdcallcfunction( luaState, MetaFunctions.IndexFunction );
			LuaLib.lua_setglobal( luaState, "get_object_member" );
			LuaLib.lua_pushstdcallcfunction( luaState, _importTypeFunction );
			LuaLib.lua_setglobal( luaState, "import_type" );
			LuaLib.lua_pushstdcallcfunction( luaState, _loadAssemblyFunction );
			LuaLib.lua_setglobal( luaState, "load_assembly" );
			LuaLib.lua_pushstdcallcfunction( luaState, _getMethodSigFunction );
			LuaLib.lua_setglobal( luaState, "get_method_bysig" );
			LuaLib.lua_pushstdcallcfunction( luaState, _getConstructorSigFunction );
			LuaLib.lua_setglobal( luaState, "get_constructor_bysig" );
			LuaLib.lua_pushstdcallcfunction( luaState, _ctypeFunction );
			LuaLib.lua_setglobal( luaState, "ctype" );
			LuaLib.lua_pushstdcallcfunction( luaState, _enumFromIntFunction );
			LuaLib.lua_setglobal( luaState, "enum" );
		}

		// Implementation of load_assembly( "assemblyName" )
		// Throws an error if the assembly is not found
		[MonoPInvokeCallback( typeof( LuaCSFunction ) )]
		static int _LoadAssembly( IntPtr luaState )
		{
			ObjectTranslator translator = Lua.GetObjectTranslator( luaState );

			try
			{
				string assemblyName = LuaLib.lua_tostring( luaState, 1 );
				Assembly assembly = null;

				try
				{
					assembly = Assembly.Load( assemblyName );
				}
				catch( BadImageFormatException )
				{
					// The assemblyName was invalid, it is most likely a path
				}

				if( assembly == null )
				{
					assembly = Assembly.Load( AssemblyName.GetAssemblyName( assemblyName ) );
				}

				if( assembly != null && !translator._assemblies.Contains( assembly ) )
				{
					translator._assemblies.Add( assembly );
				}
			}
			catch( Exception e )
			{
				translator.ThrowError( luaState, e );
			}

			return 0;
		}

		// Implementation of import_type( "className" )
		// Returns nil if the type is not found
		[MonoPInvokeCallback( typeof( LuaCSFunction ) )]
		static int _ImportType( IntPtr luaState )
		{
			ObjectTranslator translator = Lua.GetObjectTranslator( luaState );

			string className = LuaLib.lua_tostring( luaState, 1 );
			Type klass = translator.FindType( className );
			if( klass != null )
			{
				translator.PushType( luaState, klass );
			}
			else
			{
				LuaLib.lua_pushnil( luaState );
			}

			return 1;
		}

		// Implementation of get_method_bysig( obj, "methodName", "paramType1", ... )
		// Returns nil if no matching method is not found
		[MonoPInvokeCallback( typeof( LuaCSFunction ) )]
		static int _GetMethodSignature( IntPtr luaState )
		{
			ObjectTranslator translator = Lua.GetObjectTranslator( luaState );

			ProxyType klass;
			object target;
			int udata = LuaLib.luanet_checkudata( luaState, 1, "luaNet_class" );
			if( udata != -1 )
			{
				klass = (ProxyType)translator._objects[udata];
				target = null;
			}
			else
			{
				target = translator.GetRawNetObject( luaState, 1 );
				if( target == null )
				{
					translator.ThrowError( luaState, "get_method_bysig: first arg is not type or object reference" );
					LuaLib.lua_pushnil( luaState );
					return 1;
				}

				klass = new ProxyType( target.GetType() );
			}

			string methodName = LuaLib.lua_tostring( luaState, 2 ).ToString();

			var signatures = new Type[LuaLib.lua_gettop( luaState ) - 2];
			for( int i = 0; i < signatures.Length; ++i )
			{
				signatures[i] = translator.FindType( LuaLib.lua_tostring( luaState, i + 3 ).ToString() );
			}

			try
			{
				MethodInfo method = klass.GetMethod( methodName,
						BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance, signatures );
				translator.PushFunction( luaState,
					new LuaCSFunction( ( new LuaMethodWrapper( translator, target, klass, method ) ).InvokeFunction ) );
			}
			catch( Exception e )
			{
				translator.ThrowError( luaState, e );
				LuaLib.lua_pushnil( luaState );
			}

			return 1;
		}

		// Implementation of get_constructor_bysig( obj, "paramType1", ... )
		// Returns nil if no matching constructor is found
		[MonoPInvokeCallback( typeof( LuaCSFunction ) )]
		static int GetConstructorSignature( IntPtr luaState )
		{
			ObjectTranslator translator = Lua.GetObjectTranslator( luaState );

			ProxyType klass = null;
			int udata = LuaLib.luanet_checkudata( luaState, 1, "luaNet_class" );
			if( udata != -1 )
			{
				klass = (ProxyType)translator._objects[udata];
			}

			if( klass == null )
			{
				translator.ThrowError( luaState, "get_constructor_bysig: first arg is invalid type reference" );
			}

			var signature = new Type[LuaLib.lua_gettop( luaState ) - 1];
			for( int i = 0; i < signature.Length; ++i )
			{
				signature[i] = translator.FindType( LuaLib.lua_tostring( luaState, i + 2 ).ToString() );
			}

			try
			{
				ConstructorInfo constructor = klass.UnderlyingSystemType.GetConstructor( signature );
				translator.PushFunction( luaState,
					new LuaCSFunction( ( new LuaMethodWrapper( translator, null, klass, constructor ) ).InvokeFunction ) );
			}
			catch( Exception e )
			{
				translator.ThrowError( luaState, e );
				LuaLib.lua_pushnil( luaState );
			}

			return 1;
		}

		// Implementation of ctype( obj )
		[MonoPInvokeCallback( typeof( LuaCSFunction ) )]
		static int _CType( IntPtr luaState )
		{
			var translator = Lua.GetObjectTranslator( luaState );

			Type t = translator._TypeOf( luaState, 1 );
			if( t == null )
			{
				return translator.PushError( luaState, "Not a CLR Class" );
			}

			translator.PushObject( luaState, t, "luaNet_metatable" );

			return 1;
		}

		// Implementation of enum( "enumType", "enumValue, ..." )
		[MonoPInvokeCallback( typeof( LuaCSFunction ) )]
		static int _EnumFromIntOrString( IntPtr luaState )
		{
			ObjectTranslator translator = Lua.GetObjectTranslator( luaState );

			Type t = translator._TypeOf( luaState, 1 );
			if( t == null || !t.IsEnum() )
			{
				return translator.PushError( luaState, "Not an Enum." );
			}

			object res = null;
			LuaTypes lt = LuaLib.lua_type( luaState, 2 );
			if( lt == LuaTypes.LUA_TNUMBER )
			{
				int ival = (int)LuaLib.lua_tonumber( luaState, 2 );
				res = Enum.ToObject( t, ival );
			}
			else if( lt == LuaTypes.LUA_TSTRING )
			{
				string sflags = LuaLib.lua_tostring( luaState, 2 );
				try
				{
					res = Enum.Parse( t, sflags, true );
				}
				catch( ArgumentException e )
				{
					return translator.PushError( luaState, e.Message );
				}
			}
			else
			{
				return translator.PushError( luaState, "Second argument must be a integer or a string." );
			}
			translator.PushObject( luaState, res, "luaNet_metatable" );

			return 1;
		}

		Type _TypeOf( IntPtr luaState, int idx )
		{
			int udata = LuaLib.luanet_checkudata( luaState, 1, "luaNet_class" );
			if( udata == -1 )
				return null;

			ProxyType pt = (ProxyType)_objects[udata];
			return pt.UnderlyingSystemType;
		}

		//--------------------------------------------------------------------------------------------------------------
		// FindType
		// PushType
		// PushFunction
		// PushObject
		// GetAsType
		// CollectObject
		//
		// GetObject
		// GetFunction
		// GetTable
		// GetUserData
		// GetNetObject
		// GetRawNetObject
		// PopValues
		// Push
		//
		// MatchParameters
		// TableToArray
		//--------------------------------------------------------------------------------------------------------------

		internal Type FindType( string className )
		{
			foreach( var assembly in _assemblies )
			{
				var klass = assembly.GetType( className );
				if( klass != null )
					return klass;
			}
			return null;
		}

		// Pushes a type reference into the stack
		internal void PushType( IntPtr luaState, Type t )
		{
			PushObject( luaState, new ProxyType( t ), "luaNet_class" );
		}

		// Pushes a delegate into the stack
		internal void PushFunction( IntPtr luaState, LuaCSFunction func )
		{
			PushObject( luaState, func, "luaNet_function" );
		}

		// Pushes a CLR object into the Lua stack as an userdata with the provided metatable
		internal void PushObject( IntPtr luaState, object o, string metatable )
		{
			// Pushes nil
			if( o == null )
			{
				LuaLib.lua_pushnil( luaState );
				return;
			}

			int index = -1;

			// Object already in the list of Lua objects? Push the stored reference
			bool found = ( !o.GetType().IsValueType || o.GetType().IsEnum ) && _objectsBackward.TryGetValue( o, out index );
			if( found )
			{
				LuaLib.luaL_getmetatable( luaState, "luaNet_objects" );
				LuaLib.lua_rawgeti( luaState, -1, index );

				// Note:
				// Starting with lua5.1 the garbage collector may remove weak reference items (such as our
				// luaNet_objects values) when the initial GC sweep occurs, but the actual call of the __gc finalizer
				// for that object may not happen until a little while later
				// During that window we might call this routine and find the element missing from luaNet_objects, but
				// collectObject() has not yet been called
				// In that case, we go ahead and call collect object here
				// Did we find a non nil object in our table? if not, we need to call collect object
				var type = LuaLib.lua_type( luaState, -1 );
				if( type != LuaTypes.LUA_TNIL )
				{
					LuaLib.lua_remove( luaState, -2 ); // Drop the metatable - we're going to leave our object on the stack
					return;
				}

				// MetaFunctions.dumpStack(this, luaState);
				LuaLib.lua_remove( luaState, -1 );	// Remove the nil object value
				LuaLib.lua_remove( luaState, -1 );	// Remove the metatable
				_CollectObject( o, index );			// Remove from both our tables and fall out to get a new ID
			}

			index = _AddObject( o );
			_PushNewObject( luaState, o, index, metatable );
		}

		int _AddObject( object obj )
		{
			// New object: inserts it in the list
			int index = _nextObjectIndex++;

			_objects[index] = obj;
			if( !obj.GetType().IsValueType || obj.GetType().IsValueType )
			{
				_objectsBackward[obj] = index;
			}

			return index;
		}

		// Pushes a new object into the Lua stack with the provided metatable
		void _PushNewObject( IntPtr luaState, object o, int index, string metatable )
		{
			if( metatable == "luaNet_metatable" )
			{
				// Gets or creates the metatable for the object's type
				LuaLib.luaL_getmetatable( luaState, o.GetType().AssemblyQualifiedName );			// mt

				if( LuaLib.lua_isnil( luaState, -1 ) )
				{
					LuaLib.lua_settop( luaState, -2 );												// 
					LuaLib.luaL_newmetatable( luaState, o.GetType().AssemblyQualifiedName );		// mt
					LuaLib.lua_pushstring( luaState, "cache" );										// mt|s
					LuaLib.lua_newtable( luaState );												// mt|s|t
					LuaLib.lua_rawset( luaState, -3 );												// mt
					LuaLib.lua_pushlightuserdata( luaState, LuaLib.luanet_gettag() );				// mt|lud
					LuaLib.lua_pushnumber( luaState, 1 );											// mt|lud|n
					LuaLib.lua_rawset( luaState, -3 );												// mt
					LuaLib.lua_pushstring( luaState, "__index" );									// mt|s
					LuaLib.lua_pushstring( luaState, "luaNet_indexfunction" );						// mt|s|s
					LuaLib.lua_rawget( luaState, LuaIndexes.LUA_REGISTRYINDEX );					// mt|s|cf
					LuaLib.lua_rawset( luaState, -3 );												// mt
					LuaLib.lua_pushstring( luaState, "__gc" );										// mt|s
					LuaLib.lua_pushstdcallcfunction( luaState, MetaFunctions.GcFunction );			// mt|s|cf
					LuaLib.lua_rawset( luaState, -3 );												// mt
					LuaLib.lua_pushstring( luaState, "__tostring" );                                // mt|s
					LuaLib.lua_pushstdcallcfunction( luaState, MetaFunctions.ToStringFunction );	// mt|s|cf
					LuaLib.lua_rawset( luaState, -3 );												// mt
					LuaLib.lua_pushstring( luaState, "__newindex" );                                // mt|s
					LuaLib.lua_pushstdcallcfunction( luaState, MetaFunctions.NewIndexFunction );	// mt|s|cf
					LuaLib.lua_rawset( luaState, -3 );												// mt

					// Bind C# operator with Lua metamethods (__add, __sub, __mul)
					_RegisterOperatorsFunctions( luaState, o.GetType() );
				}
			}
			else
			{
				LuaLib.luaL_getmetatable( luaState, metatable );									// mt
			}

			// Stores the object index in the Lua list and pushes the index into the Lua stack
			LuaLib.luaL_getmetatable( luaState, "luaNet_objects" );	// mt|mt2
			LuaLib.luanet_newudata( luaState, index );				// mt|mt2|ud
			LuaLib.lua_pushvalue( luaState, -3 );					// mt|mt2|ud|mt
			LuaLib.lua_remove( luaState, -4 );						// mt2|ud|mt
			LuaLib.lua_setmetatable( luaState, -2 );                // mt2|ud
			LuaLib.lua_pushvalue( luaState, -1 );                   // mt2|ud|ud
			LuaLib.lua_rawseti( luaState, -3, index );				// mt2|ud
			LuaLib.lua_remove( luaState, -2 );						// ud
		}

		void _RegisterOperatorsFunctions( IntPtr luaState, Type type )
		{
			if( type.HasAdditionOpertator() )
			{
				LuaLib.lua_pushstring( luaState, "__add" );
				LuaLib.lua_pushstdcallcfunction( luaState, MetaFunctions.AddFunction );
				LuaLib.lua_rawset( luaState, -3 );
			}
			if( type.HasSubtractionOpertator() )
			{
				LuaLib.lua_pushstring( luaState, "__sub" );
				LuaLib.lua_pushstdcallcfunction( luaState, MetaFunctions.SubtractFunction );
				LuaLib.lua_rawset( luaState, -3 );
			}
			if( type.HasMultiplyOpertator() )
			{
				LuaLib.lua_pushstring( luaState, "__mul" );
				LuaLib.lua_pushstdcallcfunction( luaState, MetaFunctions.MultiplyFunction );
				LuaLib.lua_rawset( luaState, -3 );
			}
			if( type.HasDivisionOpertator() )
			{
				LuaLib.lua_pushstring( luaState, "__div" );
				LuaLib.lua_pushstdcallcfunction( luaState, MetaFunctions.DivisionFunction );
				LuaLib.lua_rawset( luaState, -3 );
			}
			if( type.HasModulusOpertator() )
			{
				LuaLib.lua_pushstring( luaState, "__mod" );
				LuaLib.lua_pushstdcallcfunction( luaState, MetaFunctions.ModulosFunction );
				LuaLib.lua_rawset( luaState, -3 );
			}
			if( type.HasUnaryNegationOpertator() )
			{
				LuaLib.lua_pushstring( luaState, "__unm" );
				LuaLib.lua_pushstdcallcfunction( luaState, MetaFunctions.UnaryNegationFunction );
				LuaLib.lua_rawset( luaState, -3 );
			}
			if( type.HasEqualityOpertator() )
			{
				LuaLib.lua_pushstring( luaState, "__eq" );
				LuaLib.lua_pushstdcallcfunction( luaState, MetaFunctions.EqualFunction );
				LuaLib.lua_rawset( luaState, -3 );
			}
			if( type.HasLessThanOpertator() )
			{
				LuaLib.lua_pushstring( luaState, "__lt" );
				LuaLib.lua_pushstdcallcfunction( luaState, MetaFunctions.LessThanFunction );
				LuaLib.lua_rawset( luaState, -3 );
			}
			if( type.HasLessThanOrEqualOpertator() )
			{
				LuaLib.lua_pushstring( luaState, "__le" );
				LuaLib.lua_pushstdcallcfunction( luaState, MetaFunctions.LessThanOrEqualFunction );
				LuaLib.lua_rawset( luaState, -3 );
			}
		}

		// Gets an object from the Lua stack with the desired type if it matches, otherwise returns null
		internal object GetAsType( IntPtr luaState, int stackPos, Type paramType )
		{
			ValueExtractor extractor = TypeChecker.CheckLuaType( luaState, stackPos, paramType );
			return extractor != null ? extractor( luaState, stackPos ) : null;
		}

		// Given the Lua int ID for an object, remove it from our maps
		internal void CollectObject( int udata )
		{
			object o;
			bool found = _objects.TryGetValue( udata, out o );
			// The other variant of collectObject might have gotten here first, in that case we will silently ignore the missing entry
			if( found )
			{
				_CollectObject( o, udata );
			}
		}

		// Given an object reference, remove it from our maps
		void _CollectObject( object o, int udata )
		{
			_objects.Remove( udata );

			if( !o.GetType().IsValueType || o.GetType().IsEnum )
			{
				_objectsBackward.Remove( o );
			}
		}

		// Gets an object from the Lua stack according to its Lua type
		internal object GetObject( IntPtr luaState, int index )
		{
			var type = LuaLib.lua_type( luaState, index );

			switch( type )
			{
			case LuaTypes.LUA_TNUMBER:
				{
					return LuaLib.lua_tonumber( luaState, index );
				}
			case LuaTypes.LUA_TSTRING:
				{
					return LuaLib.lua_tostring( luaState, index );
				}
			case LuaTypes.LUA_TBOOLEAN:
				{
					return LuaLib.lua_toboolean( luaState, index );
				}
			case LuaTypes.LUA_TTABLE:
				{
					return GetTable( luaState, index );
				}
			case LuaTypes.LUA_TFUNCTION:
				{
					return GetFunction( luaState, index );
				}
			case LuaTypes.LUA_TUSERDATA:
				{
					int udata = LuaLib.luanet_tonetobject( luaState, index );
					return udata != -1 ? _objects[udata] : GetUserData( luaState, index );
				}
			default:
				return null;
			}
		}

		// Gets the function in the index positon of the Lua stack
		internal LuaFunction GetFunction( IntPtr luaState, int index )
		{
			LuaLib.lua_pushvalue( luaState, index );
			int reference = LuaLib.luaL_ref( luaState, LuaIndexes.LUA_REGISTRYINDEX );
			if( reference == -1 )
				return null;
			return new LuaFunction( reference, Interpreter );
		}

		// Gets the table in the index positon of the Lua stack
		internal LuaTable GetTable( IntPtr luaState, int index )
		{
			LuaLib.lua_pushvalue( luaState, index );
			int reference = LuaLib.luaL_ref( luaState, LuaIndexes.LUA_REGISTRYINDEX );
			if( reference == -1 )
				return null;
			return new LuaTable( reference, Interpreter );
		}

		// Gets the userdata in the index positon of the Lua stack
		internal LuaUserData GetUserData( IntPtr luaState, int index )
		{
			LuaLib.lua_pushvalue( luaState, index );
			int reference = LuaLib.luaL_ref( luaState, LuaIndexes.LUA_REGISTRYINDEX );
			if( reference == -1 )
				return null;
			return new LuaUserData( reference, Interpreter );
		}

		// Gets the CLR object in the index positon of the Lua stack
		// Returns delegates as Lua functions
		internal object GetNetObject( IntPtr luaState, int index )
		{
			int idx = LuaLib.luanet_tonetobject( luaState, index );
			return idx != -1 ? _objects[idx] : null;
		}

		// Gets the CLR object in the index position of the Lua stack
		// Returns delegates as is
		internal object GetRawNetObject( IntPtr luaState, int index )
		{
			int udata = LuaLib.luanet_rawnetobj( luaState, index );
			return udata != -1 ? _objects[udata] : null;
		}

		// Gets the values from the provided index to the top of the stack and returns them in an array
		internal object[] PopValues( IntPtr luaState, int oldTop )
		{
			int newTop = LuaLib.lua_gettop( luaState );
			if( oldTop == newTop )
			{
				return null;
			}
			else
			{
				var returnValues = new List<object>();

				for( int i = oldTop + 1; i <= newTop; ++i )
				{
					returnValues.Add( GetObject( luaState, i ) );
				}

				LuaLib.lua_settop( luaState, oldTop );

				return returnValues.ToArray();
			}
		}

		// Gets the values from the provided index to the top of the stack and returns them in an array,
		// casting them to the provided types
		internal object[] PopValues( IntPtr luaState, int oldTop, Type[] popTypes )
		{
			int newTop = LuaLib.lua_gettop( luaState );
			if( oldTop == newTop )
			{
				return null;
			}
			else
			{
				int iTypes;
				var returnValues = new List<object>();

				if( popTypes[0] == typeof( void ) )
					iTypes = 1;
				else
					iTypes = 0;

				for( int i = oldTop + 1; i <= newTop; ++i )
				{
					returnValues.Add( GetAsType( luaState, i, popTypes[iTypes] ) );
					++iTypes;
				}

				LuaLib.lua_settop( luaState, oldTop );

				return returnValues.ToArray();
			}
		}

		// Pushes the object into the Lua stack according to its type
		internal void Push( IntPtr luaState, object o )
		{
			if( o == null )
			{
				LuaLib.lua_pushnil( luaState );
			}
			else if( o is GameObject && (GameObject)o == null )
			{
				LuaLib.lua_pushnil( luaState );
			}
			else if( o is sbyte || o is byte || o is short || o is ushort
					 || o is int || o is uint || o is long || o is float
					 || o is ulong || o is decimal || o is double )
			{
				double d = Convert.ToDouble( o );
				LuaLib.lua_pushnumber( luaState, d );
			}
			else if( o is char )
			{
				double d = (char)o;
				LuaLib.lua_pushnumber( luaState, d );
			}
			else if( o is string )
			{
				string str = (string)o;
				LuaLib.lua_pushstring( luaState, str );
			}
			else if( o is bool )
			{
				bool b = (bool)o;
				LuaLib.lua_pushboolean( luaState, b );
			}
			else if( o is LuaTable )
			{
				( (LuaTable)o ).Push( luaState );
			}
			else if( o is LuaCSFunction )
			{
				PushFunction( luaState, (LuaCSFunction)o );
			}
			else if( o is LuaFunction )
			{
				( (LuaFunction)o ).Push( luaState );
			}
			else
			{
				PushObject( luaState, o, "luaNet_metatable" );
			}
		}

		// Checks if the method matches the arguments in the Lua stack, getting the arguments if it does
		internal bool MatchParameters( IntPtr luaState, MethodBase method, ref MethodCache methodCache )
		{
			ValueExtractor extractValue;
			bool isMethod = true;
			var paramInfo = method.GetParameters();
			int currentLuaParam = 1;
			int nLuaParams = LuaLib.lua_gettop( luaState );
			var paramList = new List<object>();
			var outList = new List<int>();
			var argTypes = new List<MethodArgs>();

			foreach( var currentNetParam in paramInfo )
			{
				// Skips out params
				if( !currentNetParam.IsIn && currentNetParam.IsOut )
				{
					int index = paramList.Count;
					paramList.Add( null );
					outList.Add( index );
				}
				// Type checking
				else if( _IsTypeCorrect( luaState, currentLuaParam, currentNetParam, out extractValue ) )
				{
					int index = paramList.Count;
					paramList.Add( extractValue( luaState, currentLuaParam ) );

					var methodArg = new MethodArgs();
					methodArg.Index = index;
					methodArg.ExtractValue = extractValue;
					argTypes.Add( methodArg );

					if( currentNetParam.ParameterType.IsByRef )
					{
						outList.Add( index );
					}

					++currentLuaParam;
				}
				// Type does not match, ignore if the parameter is optional
				else if( _IsParamsArray( luaState, nLuaParams, currentLuaParam, currentNetParam, out extractValue ) )
				{
					int count = ( nLuaParams - currentLuaParam ) + 1;
					Type paramArrayType = currentNetParam.ParameterType.GetElementType();

					Func<int, object> extractDelegate = ( currentParam ) =>
					{
						++currentLuaParam;
						return extractValue( luaState, currentParam );
					};

					int index = paramList.Count;
					Array paramArray = TableToArray( extractDelegate, paramArrayType, currentLuaParam, count );
					paramList.Add( paramArray );

					var methodArg = new MethodArgs();
					methodArg.Index = index;
					methodArg.ExtractValue = extractValue;
					methodArg.IsParamsArray = true;
					methodArg.ParamsArrayType = paramArrayType;
					argTypes.Add( methodArg );
				}
				// Adds optional parameters
				else if( currentLuaParam > nLuaParams )
				{
					if( currentNetParam.IsOptional )
					{
						paramList.Add( currentNetParam.DefaultValue );
					}
					else
					{
						isMethod = false;
						break;
					}
				}
				else if( currentNetParam.IsOptional )
				{
					paramList.Add( currentNetParam.DefaultValue );
				}
				// No match
				else
				{
					isMethod = false;
					break;
				}
			}

			// Number of parameters does not match
			if( currentLuaParam != nLuaParams + 1 )
			{
				isMethod = false;
			}

			if( isMethod )
			{
				methodCache.Args = paramList.ToArray();
				methodCache.CachedMethod = method;
				methodCache.OutList = outList.ToArray();
				methodCache.ArgTypes = argTypes.ToArray();
			}

			return isMethod;
		}

		// Returns true if the type is set and assigns the extract value
		bool _IsTypeCorrect( IntPtr luaState, int currentLuaParam, ParameterInfo currentNetParam,
							 out ValueExtractor extractValue )
		{
			try
			{
				extractValue = TypeChecker.CheckLuaType( luaState, currentLuaParam, currentNetParam.ParameterType );
				return ( extractValue != null );
			}
			catch
			{
				extractValue = null;
				Debug.LogError( "Type wasn't correct" );

				return false;
			}
		}

		bool _IsParamsArray( IntPtr luaState, int nLuaParams, int currentLuaParam, ParameterInfo currentNetParam,
							 out ValueExtractor extractValue )
		{
			bool isParamArray = false;
			extractValue = null;

			if( currentNetParam.GetCustomAttributes( typeof( ParamArrayAttribute ), false ).Length > 0 )
			{
				isParamArray = nLuaParams < currentLuaParam;

				LuaTypes luaType;
				try
				{
					luaType = LuaLib.lua_type( luaState, currentLuaParam );
				}
				catch( Exception ex )
				{
					Debug.LogError( "Could not retrieve lua type while attempting to determine params Array Status." );
					Debug.LogError( ex.Message );

					return false;
				}

				if( luaType == LuaTypes.LUA_TTABLE )
				{
					try
					{
						extractValue = TypeChecker.GetExtractor( typeof( LuaTable ) );
					}
					catch( Exception )
					{
						Debug.LogError( "An error occurred during an attempt to retrieve a LuaTable extractor while checking for params array status." );
					}

					if( extractValue != null )
					{
						return true;
					}
				}
				else
				{
					Type paramElementType = currentNetParam.ParameterType.GetElementType();

					try
					{
						extractValue = TypeChecker.CheckLuaType( luaState, currentLuaParam, paramElementType );
					}
					catch( Exception )
					{
						Debug.LogError( string.Format( 
							"An error occurred during an attempt to retrieve an extractor ({0}) while checking for params array status.",
							paramElementType.FullName ) );
					}

					if( extractValue != null )
					{
						return true;
					}
				}
			}

			return isParamArray;
		}

		internal Array TableToArray( Func<int, object> luaParamValueExtractor, Type paramArrayType, int startIndex, int count )
		{
			Array paramArray;

			if( count == 0 )
			{
				return Array.CreateInstance( paramArrayType, 0 );
			}

			object luaParamValue = luaParamValueExtractor( startIndex );
			if( luaParamValue is LuaTable )
			{
				LuaTable table = (LuaTable)luaParamValue;
				paramArray = Array.CreateInstance( paramArrayType, table.Values.Count );

				IDictionaryEnumerator tableEnumerator = table.GetEnumerator();
				tableEnumerator.Reset();

				int paramArrayIndex = 0;
				while( tableEnumerator.MoveNext() )
				{
					object value = tableEnumerator.Value;

					if( paramArrayType == typeof( object ) )
					{
						if( value != null && value.GetType() == typeof( double ) && _IsInteger( (double)value ) )
						{
							value = Convert.ToInt32( (double)value );
						}
					}
					paramArray.SetValue( Convert.ChangeType( value, paramArrayType ), paramArrayIndex );
					++paramArrayIndex;
				}
			}
			else
			{
				paramArray = Array.CreateInstance( paramArrayType, count );

				paramArray.SetValue( luaParamValue, 0 );
				for( int i = 1; i < count; ++i )
				{
					++startIndex;
					object value = luaParamValueExtractor( startIndex );
					paramArray.SetValue( value, i );
				}
			}

			return paramArray;
		}

		static bool _IsInteger( double x )
		{
			return Math.Ceiling( x ) == x;
		}

		// Passes errors (argument e) to the Lua interpreter
		internal void ThrowError( IntPtr luaState, object e )
		{
			// We use this to remove anything pushed by luaL_where
			int oldTop = LuaLib.lua_gettop( luaState );

			// Stack frame #1 is our C# wrapper, so not very interesting to the user
			// Stack frame #2 must be the lua code that called us, so that's what we want to use
			LuaLib.luaL_where( luaState, 1 );
			var curlev = PopValues( luaState, oldTop );
			// Determine the position in the script where the exception was triggered
			string errLocation = string.Empty;
			if( curlev.Length > 0 )
			{
				errLocation = curlev[0].ToString();
			}

			string message = e as string;
			if( message != null )
			{
				// Wrap Lua error (just a string) and store the error location
				e = new LuaScriptException( message, errLocation );
			}
			else
			{
				Exception ex = e as Exception;
				if( ex != null )
				{
					// Wrap generic .NET exception as an InnerException and store the error location
					e = new LuaScriptException( ex, errLocation );
				}
			}

			Push( luaState, e );
			LuaLib.lua_error( luaState );
		}

		internal int PushError( IntPtr luaState, string msg )
		{
			LuaLib.lua_pushnil( luaState );
			LuaLib.lua_pushstring( luaState, msg );

			return 2;
		}

		// Debug tool to dump the lua stack
		public static void DumpStack( ObjectTranslator translator, IntPtr luaState )
		{
			int depth = LuaLib.lua_gettop( luaState );

			Debug.Log( string.Format( "Lua stack depth: {0}", depth ) );

			for( int i = 1; i <= depth; ++i )
			{
				LuaTypes type = LuaLib.lua_type( luaState, i );
				// We dump stacks when deep in calls, calling typename while the stack is in flux can fail sometimes,
				// so manually check for key types
				string typestr = ( type == LuaTypes.LUA_TTABLE ) ? "table" : LuaLib.lua_typename( luaState, type );
				string strrep = LuaLib.lua_tostring( luaState, i ).ToString();
				if( type == LuaTypes.LUA_TUSERDATA )
				{
					object obj = translator.GetRawNetObject( luaState, i );
					strrep = obj.ToString();
				}

				Debug.Log( string.Format( "{0}: ({1}) {2}", i, typestr, strrep ) );
			}
		}
	}
}