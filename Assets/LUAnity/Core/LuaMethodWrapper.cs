namespace LUAnity
{
	using System;
	using System.Collections.Generic;
	using System.Reflection;

	// Cached method
	public struct MethodCache
	{
		public MethodBase CachedMethod
		{
			get
			{
				return _cachedMethod;
			}
			set
			{
				_cachedMethod = value;

				var mi = value as MethodInfo;
				if( mi != null )
				{
					IsReturnVoid = mi.ReturnType == typeof( void );
				}
			}
		}

		public bool IsReturnVoid;

		public object[] Args; // List or arguments
		public int[] OutList; // Positions of out parameters
		public MethodArgs[] ArgTypes; // Types of parameters

		MethodBase _cachedMethod;
	}

	// Parameter information
	public struct MethodArgs
	{
		public int Index; // Position of parameter
		public ValueExtractor ExtractValue; // Type-conversion function
		public bool IsParamsArray;
		public Type ParamsArrayType;
	}

	// Argument extraction with type-conversion function
	public delegate object ValueExtractor( IntPtr luaState, int stackPos );

	// Wrapper class for methods/constructors accessed from Lua
	public class LuaMethodWrapper
	{
		public LuaCSFunction InvokeFunction;

		ObjectTranslator _translator;
		object _target;
		ValueExtractor _extractTarget;
		MethodBase _method;
		string _methodName;
		bool _isStatic;
		MemberInfo[] _members;

		MethodCache _lastCalledMethod = new MethodCache();

		// Constructs the wrapper for a known MethodBase instance
		public LuaMethodWrapper( ObjectTranslator translator, object target, ProxyType targetType, MethodBase method )
		{
			InvokeFunction = new LuaCSFunction( _Call );

			_translator = translator;
			_target = target;
			if( targetType != null )
			{
				_extractTarget = translator.TypeChecker.GetExtractor( targetType );
			}
			_method = method;
			_methodName = method.Name;
			_isStatic = method.IsStatic;
		}

		// Constructs the wrapper for a known method name
		public LuaMethodWrapper( ObjectTranslator translator, ProxyType targetType, string methodName, BindingFlags bindingType )
		{
			InvokeFunction = new LuaCSFunction( _Call );

			_translator = translator;
			if( targetType != null )
			{
				_extractTarget = translator.TypeChecker.GetExtractor( targetType );
			}
			_methodName = methodName;
			_isStatic = ( bindingType & BindingFlags.Static ) == BindingFlags.Static;
			_members = _GetMethodsRecursively( targetType.UnderlyingSystemType, methodName, bindingType | BindingFlags.Public );
		}

		// Calls the method. Receives the arguments from the Lua stack and returns values in it
		int _Call( IntPtr luaState )
		{
			object targetObject = _target;
			bool failedCall = true;

			if( !LuaLib.lua_checkstack( luaState, 5 ) )
				throw new LuaException( "Lua stack overflow" );

			bool isStatic = _isStatic;
			_SetPendingException( null );

			// Method from name
			if( _method == null )
			{
				targetObject = isStatic ? null : _extractTarget( luaState, 1 );

				// Cached?
				if( _lastCalledMethod.CachedMethod != null )
				{
					// If this is an instance invoke we will have an extra arg on the stack for the targetObject
					int numStackToSkip = isStatic ? 0 : 1; 
					int numArgsPassed = LuaLib.lua_gettop( luaState ) - numStackToSkip;
					// Number of args match?
					if( numArgsPassed == _lastCalledMethod.ArgTypes.Length )
					{
						if( !LuaLib.lua_checkstack( luaState, _lastCalledMethod.OutList.Length + 6 ) )
						{
							throw new LuaException( "Lua stack overflow" );
						}

						object[] args = _lastCalledMethod.Args;
						MethodBase method = _lastCalledMethod.CachedMethod;

						try
						{
							for( int i = 0; i < _lastCalledMethod.ArgTypes.Length; ++i )
							{
								MethodArgs type = _lastCalledMethod.ArgTypes[i];
								int index = i + 1 + numStackToSkip;
								Func<int, object> valueExtractor = ( currentParam ) => {
									return type.ExtractValue( luaState, currentParam );
								};

								if( type.IsParamsArray )
								{
									int count = index - _lastCalledMethod.ArgTypes.Length;
									args[type.Index] = _translator.TableToArray( valueExtractor, type.ParamsArrayType, index, count );
								}
								else
								{
									args[type.Index] = valueExtractor( index );
								}

								if( _lastCalledMethod.Args[type.Index] == null && !LuaLib.lua_isnil( luaState, i + 1 + numStackToSkip ) )
								{
									throw new LuaException( string.Format( "Argument number {0} is invalid", ( i + 1 ) ) );
								}
							}

							if( _isStatic )
							{
								object ret = method.Invoke( null, _lastCalledMethod.Args );
								_translator.Push( luaState, ret );
							}
							else
							{
								if( method.IsConstructor )
								{
									object ret = ( (ConstructorInfo)method ).Invoke( _lastCalledMethod.Args );
									_translator.Push( luaState, ret );
								}
								else
								{
									object ret = method.Invoke( targetObject, _lastCalledMethod.Args );
									_translator.Push( luaState, ret );
								}
							}

							failedCall = false;
						}
						catch( TargetInvocationException e )
						{
							// Failure of method invocation
							return _SetPendingException( e.GetBaseException() );
						}
						catch( Exception e )
						{
							// Is the method not overloaded?
							if( _members.Length == 1 )
							{
								return _SetPendingException( e );
							}
						}
					}
				}

				// Cache miss
				if( failedCall )
				{
					//Debug.LogDebug( "Cache miss on " + methodName );

					// If we are running an instance variable, we can now pop the targetObject from the stack
					if( !isStatic )
					{
						if( targetObject == null )
						{
							_translator.ThrowError( luaState, String.Format( "Instance method '{0}' requires a non null target object", _methodName ) );
							LuaLib.lua_pushnil( luaState );

							return 1;
						}

						LuaLib.lua_remove( luaState, 1 ); // Pops the receiver
					}

					bool hasMatch = false;
					string candidateName = null;
					foreach( var member in _members )
					{
						candidateName = member.ReflectedType.Name + "." + member.Name;

						if( _translator.MatchParameters( luaState, (MethodInfo)member, ref _lastCalledMethod ) )
						{
							hasMatch = true;
							break;
						}
					}
					if( !hasMatch )
					{
						_translator.ThrowError( luaState, 
							( candidateName == null ) ? "Invalid arguments to method call" : ( "Invalid arguments to method: " + candidateName ) );
						LuaLib.lua_pushnil( luaState );

						_ClearCachedArgs();

						return 1;
					}
				}
			}
			// Method from MethodBase instance
			else
			{
				if( _method.ContainsGenericParameters )
				{
					_translator.MatchParameters( luaState, _method, ref _lastCalledMethod );

					if( _method.IsGenericMethodDefinition )
					{
						// Need to make a concrete type of the generic method definition
						var typeArgs = new List<Type>();

						foreach( object arg in _lastCalledMethod.Args )
						{
							typeArgs.Add( arg.GetType() );
						}

						MethodInfo concreteMethod = ( _method as MethodInfo ).MakeGenericMethod( typeArgs.ToArray() );
						object ret = concreteMethod.Invoke( targetObject, _lastCalledMethod.Args );
						_translator.Push( luaState, ret );

						failedCall = false;
					}
					else if( _method.ContainsGenericParameters )
					{
						_translator.ThrowError( luaState, "Unable to invoke method on generic class as the current method is an open generic method" );
						LuaLib.lua_pushnil( luaState );

						_ClearCachedArgs();

						return 1;
					}
				}
				else
				{
					if( !_method.IsStatic && !_method.IsConstructor && targetObject == null )
					{
						targetObject = _extractTarget( luaState, 1 );
						LuaLib.lua_remove( luaState, 1 ); // Pops the receiver
					}

					if( !_translator.MatchParameters( luaState, _method, ref _lastCalledMethod ) )
					{
						_translator.ThrowError( luaState, "Invalid arguments to method call" );
						LuaLib.lua_pushnil( luaState );

						_ClearCachedArgs();

						return 1;
					}
				}
			}

			if( failedCall )
			{
				if( !LuaLib.lua_checkstack( luaState, _lastCalledMethod.OutList.Length + 6 ) )
				{
					_ClearCachedArgs();

					throw new LuaException( "Lua stack overflow" );
				}

				try
				{
					if( isStatic )
					{
						object ret = _lastCalledMethod.CachedMethod.Invoke( null, _lastCalledMethod.Args );
						_translator.Push( luaState, ret );
					}
					else
					{
						if( _lastCalledMethod.CachedMethod.IsConstructor )
						{
							object ret = ( (ConstructorInfo)_lastCalledMethod.CachedMethod ).Invoke( _lastCalledMethod.Args );
							_translator.Push( luaState, ret );
						}
						else
						{
							object ret = _lastCalledMethod.CachedMethod.Invoke( targetObject, _lastCalledMethod.Args );
							_translator.Push( luaState, ret );
						}
					}
				}
				catch( TargetInvocationException e )
				{
					_ClearCachedArgs();

					return _SetPendingException( e.GetBaseException() );
				}
				catch( Exception e )
				{
					_ClearCachedArgs();

					return _SetPendingException( e );
				}
			}

			int numReturnValues = 0;

			// Pushes out and ref return values
			for( int index = 0; index < _lastCalledMethod.OutList.Length; ++index )
			{
				++numReturnValues;
				_translator.Push( luaState, _lastCalledMethod.Args[_lastCalledMethod.OutList[index]] );
			}

			// If not return void, we need add 1, or we will lose the function's return value when call dotnet function
			// like "int foo(arg1, out arg2, out arg3)" in lua code 
			if( !_lastCalledMethod.IsReturnVoid && numReturnValues > 0 )
			{
				++numReturnValues;
			}

			_ClearCachedArgs();

			return numReturnValues < 1 ? 1 : numReturnValues;
		}

		MethodInfo[] _GetMethodsRecursively( Type type, string methodName, BindingFlags bindingType )
		{
			if( type == typeof( object ) )
			{
				return type.GetMethods( methodName, bindingType );
			}

			var methods = type.GetMethods( methodName, bindingType );
			var baseMethods = _GetMethodsRecursively( type.BaseType, methodName, bindingType );

			var allMethods = new MethodInfo[methods.Length + baseMethods.Length];
			methods.CopyTo( allMethods, 0 );
			baseMethods.CopyTo( allMethods, methods.Length );
			return allMethods;
		}

		// Convert C# exceptions into Lua errors
		// Returns num of things on stack, null for no pending exception
		int _SetPendingException( Exception e )
		{
			return _translator.Interpreter.SetPendingException( e );
		}

		void _ClearCachedArgs()
		{
			if( _lastCalledMethod.Args == null )
				return;

			for( int i = 0; i < _lastCalledMethod.Args.Length; ++i )
			{
				_lastCalledMethod.Args[i] = null;
			}
		}
	}
}
