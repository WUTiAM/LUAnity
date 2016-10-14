namespace LUAnity
{
	using System;
	using System.Collections.Generic;
	using System.Reflection;
	using System.Runtime.InteropServices;

	// Functions used in the metatables of userdata representing CLR objects	
	public class MetaFunctions
	{
		// __index metafunction for CLR objects
		public static string LuaIndexFunction
		{
			get
			{
				return @"
local function index( obj, name )
	local meta = getmetatable( obj )
	local cached = meta.cache[name]

	if cached ~= nil then
		return cached
	else
		local value, isFunc = get_object_member( obj, name )
				   
		if isFunc then
		meta.cache[name] = value
		end

		return value
	end
end

return index
				";
			}
		}

		public LuaCSFunction IndexFunction { get; set; }
		public LuaCSFunction BaseIndexFunction { get; set; }
		public LuaCSFunction NewIndexFunction { get; set; }
		public LuaCSFunction ClassIndexFunction { get; set; }
		public LuaCSFunction ClassNewindexFunction { get; set; }
		public LuaCSFunction CallConstructorFunction { get; set; }
		public LuaCSFunction ExecuteDelegateFunction { get; set; }
		public LuaCSFunction ToStringFunction { get; set; }
		public LuaCSFunction GcFunction { get; set; }

		public LuaCSFunction AddFunction { get; set; }
		public LuaCSFunction SubtractFunction { get; set; }
		public LuaCSFunction MultiplyFunction { get; set; }
		public LuaCSFunction DivisionFunction { get; set; }
		public LuaCSFunction ModulosFunction { get; set; }
		public LuaCSFunction UnaryNegationFunction { get; set; }
		public LuaCSFunction EqualFunction { get; set; }
		public LuaCSFunction LessThanFunction { get; set; }
		public LuaCSFunction LessThanOrEqualFunction { get; set; }

		ObjectTranslator _translator;
		Dictionary<object, object> _memberCache;

		public MetaFunctions( ObjectTranslator translator )
		{
			_translator = translator;
			_memberCache = new Dictionary<object, object>();

			IndexFunction = new LuaCSFunction( _GetMethod );
			BaseIndexFunction = new LuaCSFunction( _GetBaseMethod );
			NewIndexFunction = new LuaCSFunction( _SetFieldOrProperty );
			ClassIndexFunction = new LuaCSFunction( _GetClassMethod );
			ClassNewindexFunction = new LuaCSFunction( _SetClassFieldOrProperty );
			CallConstructorFunction = new LuaCSFunction( _CallConstructor );
			ExecuteDelegateFunction = new LuaCSFunction( _RunFunctionDelegate );
			ToStringFunction = new LuaCSFunction( _ToStringLua );
			GcFunction = new LuaCSFunction( _CollectObject );

			AddFunction = new LuaCSFunction( _AddLua );
			SubtractFunction = new LuaCSFunction( _SubtractLua );
			MultiplyFunction = new LuaCSFunction( _MultiplyLua );
			DivisionFunction = new LuaCSFunction( _DivideLua );
			ModulosFunction = new LuaCSFunction( _ModLua );
			UnaryNegationFunction = new LuaCSFunction( _UnaryNegationLua );
			EqualFunction = new LuaCSFunction( _EqualLua );
			LessThanFunction = new LuaCSFunction( _LessThanLua );
			LessThanOrEqualFunction = new LuaCSFunction( _LessThanOrEqualLua );
		}

		// Called by the __index metafunction of CLR objects in case the method is not cached or it is a 
		// field/property/event
		// Receives the object and the member name as arguments and returns either the value of the member or a delegate
		// to call it
		// If the member does not exist returns nil
		[MonoPInvokeCallback( typeof( LuaCSFunction ) )]
		static int _GetMethod( IntPtr luaState )
		{
			ObjectTranslator translator = Lua.GetObjectTranslator( luaState );
			MetaFunctions metaFunctions = translator.MetaFunctions;

			object obj = translator.GetRawNetObject( luaState, 1 );
			if( obj == null )
			{
				translator.ThrowError( luaState, "Trying to index an invalid object reference" );
				LuaLib.lua_pushnil( luaState );

				return 1;
			}

			object index = translator.GetObject( luaState, 2 );
			string methodName = index as string; // Will be null if not a string arg
			Type objType = obj.GetType();
			ProxyType proxyType = new ProxyType( objType );

			// Handle the most common case, looking up the method by name
			// CP: This will fail when using indexers and attempting to get a value with the same name as a property of the object, 
			// ie: xmlelement['item'] <- item is a property of xmlelement
			try
			{
				if( !string.IsNullOrEmpty( methodName ) && metaFunctions._IsMemberPresent( proxyType, methodName ) )
				{
					return metaFunctions._GetMember( luaState, proxyType, obj, methodName, BindingFlags.Instance );
				}
			}
			catch
			{
			}

			bool failed = true;
			// Try to access by array if the type is right and index is an int (lua numbers always come across as double)
			if( objType.IsArray && index is double )
			{
				int intIndex = (int)( (double)index );
				Array objArray = obj as Array;
				if( intIndex >= objArray.Length )
				{
					return translator.PushError( luaState, "Array index out of bounds: " + intIndex + " " + objArray.Length );
				}

				object val = objArray.GetValue( intIndex );
				translator.Push( luaState, val );

				failed = false;
			}
			else
			{
				// Try to use get_Item to index into this .net object
				var methods = objType.GetMethods();
				foreach( var mInfo in methods )
				{
					if( mInfo.Name == "get_Item" )
					{
						// Check if the signature matches the input
						if( mInfo.GetParameters().Length == 1 )
						{
							var getter = mInfo;
							var actualParms = ( getter != null ) ? getter.GetParameters() : null;
							if( actualParms == null || actualParms.Length != 1 )
							{
								return translator.PushError( luaState, "Method not found (or no indexer): " + index );
							}
							else
							{
								// Get the index in a form acceptable to the getter
								index = translator.GetAsType( luaState, 2, actualParms[0].ParameterType );
								// Just call the indexer - if out of bounds an exception will happen
								object[] args = new object[1] { index };

								try
								{
									object result = getter.Invoke( obj, args );
									translator.Push( luaState, result );

									failed = false;
								}
								catch( TargetInvocationException e )
								{
									// Provide a more readable description for the common case of key not found
									if( e.InnerException is KeyNotFoundException )
									{
										return translator.PushError( luaState, "Key '" + index + "' not found " );
									}
									else
									{
										translator.PushError( luaState, "Exception indexing '" + index + "' " + e.Message );
									}
								}
							}
						}
					}
				}
			}
			if( failed )
			{
				return translator.PushError( luaState, "Cannot find " + index );
			}

			LuaLib.lua_pushboolean( luaState, false );

			return 2;
		}

		// __index metafunction of base classes (the base field of Lua tables)
		// Adds a prefix to the method name to call the base version of the method
		[MonoPInvokeCallback( typeof( LuaCSFunction ) )]
		static int _GetBaseMethod( IntPtr luaState )
		{
			ObjectTranslator translator = Lua.GetObjectTranslator( luaState );
			MetaFunctions metaFunctions = translator.MetaFunctions;

			object obj = translator.GetRawNetObject( luaState, 1 );
			if( obj == null )
			{
				translator.ThrowError( luaState, "trying to index an invalid object reference" );
				LuaLib.lua_pushnil( luaState );
				LuaLib.lua_pushboolean( luaState, false );

				return 2;
			}

			string methodName = LuaLib.lua_tostring( luaState, 2 ).ToString();
			if( string.IsNullOrEmpty( methodName ) )
			{
				LuaLib.lua_pushnil( luaState );
				LuaLib.lua_pushboolean( luaState, false );

				return 2;
			}

			metaFunctions._GetMember( luaState, new ProxyType( obj.GetType() ), obj, "__luaInterface_base_" + methodName, BindingFlags.Instance );
			LuaLib.lua_settop( luaState, -2 );

			if( LuaLib.lua_type( luaState, -1 ) == LuaTypes.LUA_TNIL )
			{
				LuaLib.lua_settop( luaState, -2 );
				return metaFunctions._GetMember( luaState, new ProxyType( obj.GetType() ), obj, methodName, BindingFlags.Instance );
			}

			LuaLib.lua_pushboolean( luaState, false );

			return 2;
		}

		// __newindex metafunction of CLR objects
		// Receives the object, the member name and the value to be stored as arguments
		// Throws and error if the assignment is invalid
		[MonoPInvokeCallback( typeof( LuaCSFunction ) )]
		static int _SetFieldOrProperty( IntPtr luaState )
		{
			ObjectTranslator translator = Lua.GetObjectTranslator( luaState );
			MetaFunctions metaFunctions = translator.MetaFunctions;

			object target = translator.GetRawNetObject( luaState, 1 );
			if( target == null )
			{
				translator.ThrowError( luaState, "trying to index and invalid object reference" );
				return 0;
			}

			Type type = target.GetType();

			// First try to look up the parameter as a property name
			string detailMessage;
			bool didMember = metaFunctions._TrySetMember( luaState, new ProxyType( type ), target, BindingFlags.Instance, out detailMessage );
			if( didMember )
				return 0; // Must have found the property name

			// We didn't find a property name, now see if we can use a [] style this accessor to set array contents
			try
			{
				if( type.IsArray && LuaLib.lua_isnumber( luaState, 2 ) )
				{
					int index = (int)LuaLib.lua_tonumber( luaState, 2 );
					var arr = (Array)target;
					object val = translator.GetAsType( luaState, 3, arr.GetType().GetElementType() );
					arr.SetValue( val, index );
				}
				else
				{
					// Try to see if we have a this[] accessor
					MethodInfo setter = type.GetMethod( "set_Item" );
					if( setter != null )
					{
						var args = setter.GetParameters();
						Type valueType = args[1].ParameterType;
						// The new val ue the user specified 
						object val = translator.GetAsType( luaState, 3, valueType );
						var indexType = args[0].ParameterType;
						object index = translator.GetAsType( luaState, 2, indexType );

						object[] methodArgs = new object[2];
						// Just call the indexer - if out of bounds an exception will happen
						methodArgs[0] = index;
						methodArgs[1] = val;

						setter.Invoke( target, methodArgs );
					}
					else
					{
						// Pass the original message from trySetMember because it is probably best
						translator.ThrowError( luaState, detailMessage );
					}
				}
			}
			catch( SEHException )
			{
				// If we are seeing a C++ exception - this must actually be for Lua's private use.  Let it handle it
				throw;
			}
			catch( Exception e )
			{
				translator.ThrowError( luaState, e );
			}

			return 0;
		}

		// __index metafunction of type references, works on static members
		[MonoPInvokeCallback( typeof( LuaCSFunction ) )]
		static int _GetClassMethod( IntPtr luaState )
		{
			ObjectTranslator translator = Lua.GetObjectTranslator( luaState );
			MetaFunctions metaFunctions = translator.MetaFunctions;

			ProxyType klass;
			object obj = translator.GetRawNetObject( luaState, 1 );
			if( obj == null || !( obj is ProxyType ) )
			{
				translator.ThrowError( luaState, "Trying to index an invalid type reference" );
				LuaLib.lua_pushnil( luaState );

				return 1;
			}
			else
			{
				klass = (ProxyType)obj;
			}

			if( LuaLib.lua_isnumber( luaState, 2 ) )
			{
				int size = (int)LuaLib.lua_tonumber( luaState, 2 );
				translator.Push( luaState, Array.CreateInstance( klass.UnderlyingSystemType, size ) );

				return 1;
			}
			else
			{
				string methodName = LuaLib.lua_tostring( luaState, 2 ).ToString();
				if( string.IsNullOrEmpty( methodName ) )
				{
					LuaLib.lua_pushnil( luaState );

					return 1;
				}

				return metaFunctions._GetMember( luaState, klass, null, methodName, BindingFlags.FlattenHierarchy | BindingFlags.Static );
			}
		}

		// __newindex function of type references, works on static members.
		[MonoPInvokeCallback( typeof( LuaCSFunction ) )]
		static int _SetClassFieldOrProperty( IntPtr luaState )
		{
			ObjectTranslator translator = Lua.GetObjectTranslator( luaState );
			MetaFunctions metaFunctions = translator.MetaFunctions;

			object obj = translator.GetRawNetObject( luaState, 1 );
			if( obj == null || !( obj is ProxyType ) )
			{
				translator.ThrowError( luaState, "trying to index an invalid type reference" );

				return 0;
			}

			ProxyType target = (ProxyType)obj;

			return metaFunctions._SetMember( luaState, target, null, BindingFlags.FlattenHierarchy | BindingFlags.Static );
		}

		// __call metafunction of type references
		// Searches for and calls a constructor for the type
		// Returns nil if the constructor is not found or if the arguments are invalid
		// Throws an error if the constructor generates an exception
		[MonoPInvokeCallback( typeof( LuaCSFunction ) )]
		static int _CallConstructor( IntPtr luaState )
		{
			ObjectTranslator translator = Lua.GetObjectTranslator( luaState );
			MetaFunctions metaFunctions = translator.MetaFunctions;

			object obj = translator.GetRawNetObject( luaState, 1 );
			if( obj == null || !( obj is ProxyType ) )
			{
				translator.ThrowError( luaState, "Trying to call constructor on an invalid type reference" );
				LuaLib.lua_pushnil( luaState );

				return 1;
			}

			LuaLib.lua_remove( luaState, 1 );

			MethodCache validConstructor = new MethodCache();
			ProxyType klass = (ProxyType)obj;
			var constructors = klass.UnderlyingSystemType.GetConstructors();
			foreach( var constructor in constructors )
			{
				bool isConstructor = translator.MatchParameters( luaState, constructor, ref validConstructor );
				if( isConstructor )
				{
					try
					{
						translator.Push( luaState, constructor.Invoke( validConstructor.Args ) );
					}
					catch( TargetInvocationException e )
					{
						metaFunctions._ThrowError( luaState, e );
						LuaLib.lua_pushnil( luaState );
					}
					catch
					{
						LuaLib.lua_pushnil( luaState );
					}

					return 1;
				}
			}

			// Structs initialized via the default constructor
			if( klass.UnderlyingSystemType.IsValueType )
			{
				int numLuaParams = LuaLib.lua_gettop( luaState );
				if( numLuaParams == 0 )
				{
					translator.Push( luaState, Activator.CreateInstance( klass.UnderlyingSystemType ) );

					return 1;
				}
			}

			string constructorName = ( constructors.Length == 0 ) ? "unknown" : constructors[0].Name;
			translator.ThrowError( luaState,
				string.Format( "{0} does not contain constructor({1}) argument match", klass.UnderlyingSystemType, constructorName ) );
			LuaLib.lua_pushnil( luaState );

			return 1;
		}

		// __call metafunction of CLR delegates, retrieves and calls the delegate
		[MonoPInvokeCallback( typeof( LuaCSFunction ) )]
		static int _RunFunctionDelegate( IntPtr luaState )
		{
			ObjectTranslator translator = Lua.GetObjectTranslator( luaState );

			LuaCSFunction func = (LuaCSFunction)translator.GetRawNetObject( luaState, 1 );
			LuaLib.lua_remove( luaState, 1 );

			return func( luaState );
		}

		// __tostring metafunction of CLR objects
		[MonoPInvokeCallback( typeof( LuaCSFunction ) )]
		static int _ToStringLua( IntPtr luaState )
		{
			ObjectTranslator translator = Lua.GetObjectTranslator( luaState );

			object obj = translator.GetRawNetObject( luaState, 1 );
			if( obj != null )
			{
				translator.Push( luaState, obj.ToString() + ": " + obj.GetHashCode().ToString() );
			}
			else
			{
				LuaLib.lua_pushnil( luaState );
			}

			return 1;
		}

		// __gc metafunction of CLR objects
		[MonoPInvokeCallback( typeof( LuaCSFunction ) )]
		static int _CollectObject( IntPtr luaState )
		{
			int udata = LuaLib.luanet_rawnetobj( luaState, 1 );
			if( udata != -1 )
			{
				ObjectTranslator translator = Lua.GetObjectTranslator( luaState );
				translator.CollectObject( udata );
			}

			return 0;
		}

		// __add metafunction of CLR objects
		[MonoPInvokeCallback( typeof( LuaCSFunction ) )]
		static int _AddLua( IntPtr luaState )
		{
			ObjectTranslator translator = Lua.GetObjectTranslator( luaState );
			return _MatchOperator( luaState, "op_Addition", translator );
		}

		// __sub metafunction of CLR objects
		[MonoPInvokeCallback( typeof( LuaCSFunction ) )]
		static int _SubtractLua( IntPtr luaState )
		{
			ObjectTranslator translator = Lua.GetObjectTranslator( luaState );
			return _MatchOperator( luaState, "op_Subtraction", translator );
		}

		// __mul metafunction of CLR objects
		[MonoPInvokeCallback( typeof( LuaCSFunction ) )]
		static int _MultiplyLua( IntPtr luaState )
		{
			ObjectTranslator translator = Lua.GetObjectTranslator( luaState );
			return _MatchOperator( luaState, "op_Multiply", translator );
		}

		// __div metafunction of CLR objects
		[MonoPInvokeCallback( typeof( LuaCSFunction ) )]
		static int _DivideLua( IntPtr luaState )
		{
			ObjectTranslator translator = Lua.GetObjectTranslator( luaState );
			return _MatchOperator( luaState, "op_Division", translator );
		}

		// __mod metafunction of CLR objects
		[MonoPInvokeCallback( typeof( LuaCSFunction ) )]
		static int _ModLua( IntPtr luaState )
		{
			ObjectTranslator translator = Lua.GetObjectTranslator( luaState );
			return _MatchOperator( luaState, "op_Modulus", translator );
		}

		// __unm metafunction of CLR objects
		[MonoPInvokeCallback( typeof( LuaCSFunction ) )]
		static int _UnaryNegationLua( IntPtr luaState )
		{
			ObjectTranslator translator = Lua.GetObjectTranslator( luaState );

			object obj = translator.GetRawNetObject( luaState, 1 );
			if( obj == null )
			{
				translator.ThrowError( luaState, "Cannot negate a nil object" );
				LuaLib.lua_pushnil( luaState );

				return 1;
			}

			Type type = obj.GetType();
			MethodInfo opUnaryNegation = type.GetMethod( "op_UnaryNegation" );
			if( opUnaryNegation == null )
			{
				translator.ThrowError( luaState, "Cannot negate object (" + type.Name + " does not overload the operator -)" );
				LuaLib.lua_pushnil( luaState );

				return 1;
			}

			obj = opUnaryNegation.Invoke( obj, new object[] { obj } );
			translator.Push( luaState, obj );

			return 1;
		}

		// __eq metafunction of CLR objects
		[MonoPInvokeCallback( typeof( LuaCSFunction ) )]
		static int _EqualLua( IntPtr luaState )
		{
			ObjectTranslator translator = Lua.GetObjectTranslator( luaState );
			return _MatchOperator( luaState, "op_Equality", translator );
		}

		// __lt metafunction of CLR objects
		[MonoPInvokeCallback( typeof( LuaCSFunction ) )]
		static int _LessThanLua( IntPtr luaState )
		{
			ObjectTranslator translator = Lua.GetObjectTranslator( luaState );
			return _MatchOperator( luaState, "op_LessThan", translator );
		}

		// __le metafunction of CLR objects
		[MonoPInvokeCallback( typeof( LuaCSFunction ) )]
		static int _LessThanOrEqualLua( IntPtr luaState )
		{
			ObjectTranslator translator = Lua.GetObjectTranslator( luaState );
			return _MatchOperator( luaState, "op_LessThanOrEqual", translator );
		}

		static int _MatchOperator( IntPtr luaState, string operation, ObjectTranslator translator )
		{
			object target = _GetTargetObject( luaState, operation, translator );
			if( target == null )
			{
				translator.ThrowError( luaState, "Cannot call " + operation + " on a nil object" );
				LuaLib.lua_pushnil( luaState );

				return 1;
			}

			var validOperator = new MethodCache();
			Type type = target.GetType();
			var operators = type.GetMethods( operation, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static );
			foreach( var op in operators )
			{
				bool isOk = translator.MatchParameters( luaState, op, ref validOperator );
				if( !isOk )
					continue;

				object result;
				if( op.IsStatic )
				{
					result = op.Invoke( null, validOperator.Args );
				}
				else
				{
					result = op.Invoke( target, validOperator.Args );
				}
				translator.Push( luaState, result );

				return 1;
			}

			translator.ThrowError( luaState, "Cannot call (" + operation + ") on object type " + type.Name );
			LuaLib.lua_pushnil( luaState );

			return 1;
		}

		static object _GetTargetObject( IntPtr luaState, string operation, ObjectTranslator translator )
		{
			object target = translator.GetRawNetObject( luaState, 1 );
			if( target != null )
			{
				Type type = target.GetType();
				if( type.HasMethod( operation ) )
				{
					return target;
				}
			}

			target = translator.GetRawNetObject( luaState, 2 );
			if( target != null )
			{
				Type type = target.GetType();
				if( type.HasMethod( operation ) )
				{
					return target;
				}
			}

			return null;
		}

		// Does this method exist as either an instance or static?
		bool _IsMemberPresent( ProxyType objType, string methodName )
		{
			object cachedMember = _CheckMemberCache( _memberCache, objType, methodName );
			if( cachedMember != null )
				return true;

			var members = objType.GetMember( methodName, BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public );
			return ( members.Length > 0 );
		}

		// Pushes the value of a member or a delegate to call it, depending on the type of the member. Works with static
		// or instance members
		// Uses reflection to find members, and stores the reflected MemberInfo object in a cache (indexed by the type
		// of the object and the name of the member)
		int _GetMember( IntPtr luaState, ProxyType objType, object obj, string methodName, BindingFlags bindingType )
		{
			bool implicitStatic = false;
			MemberInfo member = null;
			object cachedMember = _CheckMemberCache( _memberCache, objType, methodName );

			if( cachedMember is LuaCSFunction )
			{
				_translator.PushFunction( luaState, (LuaCSFunction)cachedMember );
				_translator.Push( luaState, true );

				return 2;
			}
			else if( cachedMember != null )
			{
				member = (MemberInfo)cachedMember;
			}
			else
			{
				var members = objType.GetMember( methodName, bindingType | BindingFlags.Public );
				if( members.Length > 0 )
				{
					member = members[0];
				}
				else
				{
					// If we can't find any suitable instance members, try to find them as statics - but we only want to
					// allow implicit static
					members = objType.GetMember( methodName, bindingType | BindingFlags.Static | BindingFlags.Public );
					if( members.Length > 0 )
					{
						member = members[0];
						implicitStatic = true;
					}
				}
			}

			if( member != null )
			{
				if( member.MemberType == MemberTypes.Field )
				{
					var field = (FieldInfo)member;

					if( cachedMember == null )
					{
						_SetMemberCache( _memberCache, objType, methodName, member );
					}

					try
					{
						object value = field.GetValue( obj );
						_translator.Push( luaState, value );
					}
					catch
					{
						LuaLib.lua_pushnil( luaState );
					}
				}
				else if( member.MemberType == MemberTypes.Property )
				{
					PropertyInfo property = (PropertyInfo)member;

					if( cachedMember == null )
					{
						_SetMemberCache( _memberCache, objType, methodName, member );
					}

					try
					{
						object value = property.GetValue( obj, null );
						_translator.Push( luaState, value );

					}
					catch( ArgumentException )
					{
						// If we can't find the getter in our class, recurse up to the base class and see if they can help
						if( objType.UnderlyingSystemType != typeof( object ) )
						{
							return _GetMember( luaState, new ProxyType( objType.UnderlyingSystemType.BaseType ), obj, methodName, bindingType );
						}
						else
						{
							LuaLib.lua_pushnil( luaState );
						}
					}
					catch( TargetInvocationException e )
					{
						// Convert this exception into a Lua error
						_ThrowError( luaState, e );
						LuaLib.lua_pushnil( luaState );
					}
				}
				else if( member.MemberType == MemberTypes.Event )
				{
					_translator.ThrowError( luaState, "Event is not supported " + methodName );
					LuaLib.lua_pushnil( luaState );
				}
				else if( !implicitStatic )
				{
					if( member.MemberType == MemberTypes.NestedType )
					{
						if( cachedMember == null )
						{
							_SetMemberCache( _memberCache, objType, methodName, member );
						}

						// Find the name of our class
						Type dectype = member.DeclaringType;
						string name = member.Name;
						// Build a new long name and try to find the type by name
						string longname = dectype.FullName + "+" + name;
						var nestedType = _translator.FindType( longname );
						_translator.PushType( luaState, nestedType );
					}
					else
					{
						// Member type must be 'method'
						var wrapper = new LuaCSFunction( ( new LuaMethodWrapper( _translator, objType, methodName, bindingType ) ).InvokeFunction );

						if( cachedMember == null )
						{
							_SetMemberCache( _memberCache, objType, methodName, wrapper );
						}

						_translator.PushFunction( luaState, wrapper );
						_translator.Push( luaState, true );

						return 2;
					}
				}
				else
				{
					// If we reach this point we found a static method, but can't use it in this context because the
					// user passed in an instance
					_translator.ThrowError( luaState, "Can't pass instance to static method " + methodName );
					LuaLib.lua_pushnil( luaState );
				}
			}
			else
			{
				if( objType.UnderlyingSystemType != typeof( object ) )
				{
					return _GetMember( luaState, new ProxyType( objType.UnderlyingSystemType.BaseType ), obj, methodName, bindingType );
				}

				// We want to throw an exception because meerly returning 'nil' in this case is not sufficient
				// Valid data members may return nil and therefore there must be some way to know the member just
				// doesn't exist
				_translator.ThrowError( luaState, "Unknown member name " + methodName );
				LuaLib.lua_pushnil( luaState );
			}

			// Push false because we are NOT returning a function (see LuaIndexFunction)
			_translator.Push( luaState, false );

			return 2;
		}

		// Writes to fields or properties, either static or instance
		// Throws an error if the operation is invalid
		int _SetMember( IntPtr luaState, ProxyType targetType, object target, BindingFlags bindingType )
		{
			string detail;
			bool success = _TrySetMember( luaState, targetType, target, bindingType, out detail );
			if( !success )
			{
				_translator.ThrowError( luaState, detail );
			}

			return 0;
		}

		// Tries to set a named property or field
		// Return false if unable to find the named member
		bool _TrySetMember( IntPtr luaState, ProxyType targetType, object target, BindingFlags bindingType,
							out string detailMessage )
		{
			detailMessage = null;

			// If not already a string just return - we don't want to call tostring - which has the side effect of 
			// changing the lua typecode to string
			// Note: We don't use isstring because the standard lua C isstring considers either strings or numbers to
			// be true for isstring
			if( LuaLib.lua_type( luaState, 2 ) != LuaTypes.LUA_TSTRING )
			{
				detailMessage = "Property names must be strings";

				return false;
			}

			// We only look up property names by string
			string fieldName = LuaLib.lua_tostring( luaState, 2 ).ToString();
			if( fieldName == null || fieldName.Length < 1 || !( char.IsLetter( fieldName[0] ) || fieldName[0] == '_' ) )
			{
				detailMessage = "Invalid property name";

				return false;
			}

			// Find our member via reflection or the cache
			var member = (MemberInfo)_CheckMemberCache( _memberCache, targetType, fieldName );
			if( member == null )
			{
				var members = targetType.GetMember( fieldName, bindingType | BindingFlags.Public );
				if( members.Length > 0 )
				{
					member = members[0];
					_SetMemberCache( _memberCache, targetType, fieldName, member );
				}
				else
				{
					detailMessage = "Field or property '" + fieldName + "' does not exist";

					return false;
				}
			}

			if( member.MemberType == MemberTypes.Field )
			{

				FieldInfo field = (FieldInfo)member;
				object val = _translator.GetAsType( luaState, 3, field.FieldType );

				try
				{
					field.SetValue( target, val );
				}
				catch( Exception e )
				{
					_ThrowError( luaState, e );
				}

				// We did a call
				return true;
			}
			else if( member.MemberType == MemberTypes.Property )
			{
				PropertyInfo property = (PropertyInfo)member;
				object val = _translator.GetAsType( luaState, 3, property.PropertyType );

				try
				{
					property.SetValue( target, val, null );
				}
				catch( Exception e )
				{
					_ThrowError( luaState, e );
				}

				// We did a call
				return true;
			}

			detailMessage = "'" + fieldName + "' is not a .net field or property";

			return false;
		}

		// Checks if a MemberInfo object is cached, returning it or null
		object _CheckMemberCache( Dictionary<object, object> memberCache, ProxyType objType, string memberName )
		{
			object members = null;
			if( memberCache.TryGetValue( objType, out members ) )
			{
				if( members != null )
				{
					var membersDict = members as Dictionary<object, object>;
					object memberValue = null;
					if( membersDict.TryGetValue( memberName, out memberValue ) )
					{
						return memberValue;
					}
				}
			}

			return null;
		}

		// Stores a MemberInfo object in the member cache
		void _SetMemberCache( Dictionary<object, object> memberCache, ProxyType objType, string memberName, object member )
		{
			Dictionary<object, object> members = null;
			object memberCacheValue = null;
			if( memberCache.TryGetValue( objType, out memberCacheValue ) )
			{
				members = (Dictionary<object, object>)memberCacheValue;
			}
			else
			{
				members = new Dictionary<object, object>();
				memberCache[objType] = members;
			}

			members[memberName] = member;
		}

		// Convert a C# exception into a Lua error
		// We try to look into the exception to give the most meaningful description
		void _ThrowError( IntPtr luaState, Exception e )
		{
			// If we got inside a reflection show what really happened
			var te = e as TargetInvocationException;

			if( te != null )
				e = te.InnerException;

			_translator.ThrowError( luaState, e );
		}
	}
}