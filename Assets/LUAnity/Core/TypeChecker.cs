namespace LUAnity
{
	using System;
	using System.Collections.Generic;

	// Type checking and conversion functions.	
	public sealed class TypeChecker
	{
		ObjectTranslator _translator;

		Dictionary<Type, ValueExtractor> _extractValues = new Dictionary<Type, ValueExtractor>();
		ValueExtractor _extractNetObject;

		public TypeChecker( ObjectTranslator translator )
		{
			_translator = translator;

			_extractValues.Add( typeof( bool ), new ValueExtractor( _GetAsBoolean ) );
			_extractValues.Add( typeof( byte ), new ValueExtractor( _GetAsByte ) );
			_extractValues.Add( typeof( char ), new ValueExtractor( _GetAsChar ) );
			_extractValues.Add( typeof( char[] ), new ValueExtractor( _GetAsCharArray ) );
			_extractValues.Add( typeof( decimal ), new ValueExtractor( _GetAsDecimal ) );
			_extractValues.Add( typeof( double ), new ValueExtractor( _GetAsDouble ) );
			_extractValues.Add( typeof( float ), new ValueExtractor( _GetAsFloat ) );
			_extractValues.Add( typeof( int ), new ValueExtractor( _GetAsInt ) );
			_extractValues.Add( typeof( long ), new ValueExtractor( _GetAsLong ) );
			_extractValues.Add( typeof( object ), new ValueExtractor( _GetAsObject ) );
			_extractValues.Add( typeof( sbyte ), new ValueExtractor( _GetAsSbyte ) );
			_extractValues.Add( typeof( short ), new ValueExtractor( _GetAsShort ) );
			_extractValues.Add( typeof( string ), new ValueExtractor( _GetAsString ) );
			_extractValues.Add( typeof( uint ), new ValueExtractor( _GetAsUInt ) );
			_extractValues.Add( typeof( ulong ), new ValueExtractor( _GetAsULong ) );
			_extractValues.Add( typeof( ushort ), new ValueExtractor( _GetAsUShort ) );
			_extractValues.Add( typeof( LuaFunction ), new ValueExtractor( _GetAsFunction ) );
			_extractValues.Add( typeof( LuaTable ), new ValueExtractor( _GetAsTable ) );
			_extractValues.Add( typeof( LuaUserData ), new ValueExtractor( _GetAsUserdata ) );

			_extractNetObject = new ValueExtractor( _GetAsNetObject );
		}

		// Checks if the value at Lua stack index stackPos matches paramType, returning a conversion function if it does and null otherwise.
		internal ValueExtractor GetExtractor( ProxyType paramType )
		{
			return GetExtractor( paramType.UnderlyingSystemType );
		}

		internal ValueExtractor GetExtractor( Type paramType )
		{
			if( paramType.IsByRef )
			{
				paramType = paramType.GetElementType();
			}

			if( _extractValues.ContainsKey( paramType ))
			{
				return _extractValues[paramType];
			}
			return _extractNetObject;
		}

		internal ValueExtractor CheckLuaType( IntPtr luaState, int stackPos, Type paramType )
		{
			LuaTypes luaType = LuaLib.lua_type( luaState, stackPos );

			if( paramType.IsByRef )
			{
				paramType = paramType.GetElementType();
			}

			Type underlyingType = Nullable.GetUnderlyingType( paramType );
			if( underlyingType != null )
			{
				paramType = underlyingType; // Silently convert nullable types to their non null requics
			}

			bool isNetParamNumeric = ( paramType == typeof( byte )
									   || paramType == typeof( decimal )
									   || paramType == typeof( double )
									   || paramType == typeof( float )
									   || paramType == typeof( int )
									   || paramType == typeof( long )
									   || paramType == typeof( short )
									   || paramType == typeof( uint )
									   || paramType == typeof( ulong )
									   || paramType == typeof( ushort ) );

			// If it is a nullable
			if( underlyingType != null )
			{
				// null can always be assigned to nullable
				if( luaType == LuaTypes.LUA_TNIL )
				{
					// Return the correct extractor anyways
					return ( isNetParamNumeric || paramType == typeof( bool ) ) ? _extractValues[paramType] : _extractNetObject;
				}
			}

			if( paramType.Equals( typeof( object ) ) )
			{
				return _extractValues[paramType];
			}

			// Support for generic parameters
			if( paramType.IsGenericParameter )
			{
				if( luaType == LuaTypes.LUA_TBOOLEAN )
				{
					return _extractValues[typeof( bool )];
				}
				else if( luaType == LuaTypes.LUA_TFUNCTION )
				{
					return _extractValues[typeof( LuaFunction )];
				}
				else if( luaType == LuaTypes.LUA_TNUMBER )
				{
					return _extractValues[typeof( double )];
				}
				else if( luaType == LuaTypes.LUA_TSTRING )
				{
					return _extractValues[typeof( string )];
				}
				else if( luaType == LuaTypes.LUA_TTABLE )
				{
					return _extractValues[typeof( LuaTable )];
				}
				else if( luaType == LuaTypes.LUA_TUSERDATA )
				{
					return _extractValues[typeof( object )];
				}
			}

			bool isNetParamString = ( paramType == typeof( string ) || paramType == typeof( char[] ) );

			if( isNetParamNumeric )
			{
				if( LuaLib.lua_isnumber( luaState, stackPos ) && !isNetParamString )
				{
					return _extractValues[paramType];
				}
			}
			else if( paramType == typeof( bool ) )
			{
				if( LuaLib.lua_isboolean( luaState, stackPos ) )
				{
					return _extractValues[paramType];
				}
			}
			else if( paramType == typeof( LuaFunction ) )
			{
				if( luaType == LuaTypes.LUA_TFUNCTION || luaType == LuaTypes.LUA_TNIL )
				{
					return _extractValues[paramType];
				}
			}
			else if( isNetParamString )
			{
				if( LuaLib.lua_type( luaState, stackPos ) == LuaTypes.LUA_TSTRING )
				{
					return _extractValues[paramType];
				}
				else if( luaType == LuaTypes.LUA_TNIL )
				{
					return _extractNetObject; // Silently convert nil to a null string pointer
				}
			}
			else if( paramType == typeof( LuaTable ) )
			{
				if( luaType == LuaTypes.LUA_TTABLE || luaType == LuaTypes.LUA_TNIL )
				{
					return _extractValues[paramType];
				}
			}
			else if( paramType == typeof( LuaUserData ) )
			{
				if( luaType == LuaTypes.LUA_TUSERDATA || luaType == LuaTypes.LUA_TNIL )
				{
					return _extractValues[paramType];
				}
			}
			else if( typeof( Delegate ).IsAssignableFrom( paramType ) && luaType == LuaTypes.LUA_TFUNCTION )
			{
				_translator.ThrowError( luaState, "Delegates not implemnented" );
			}
			else if( paramType.IsInterface() && luaType == LuaTypes.LUA_TTABLE )
			{
				_translator.ThrowError( luaState, "Interfaces not implemnented" );
			}
			else if( ( paramType.IsInterface() || paramType.IsClass() ) && luaType == LuaTypes.LUA_TNIL )
			{
				// Allow nil to be silently converted to null - _extractNetObject will return null when the item ain't found
				return _extractNetObject;
			}
			else if( LuaLib.lua_type( luaState, stackPos ) == LuaTypes.LUA_TTABLE )
			{
				if( LuaLib.luaL_getmetafield( luaState, stackPos, "__index" ) )
				{
					object obj = _translator.GetNetObject( luaState, -1 );

					LuaLib.lua_settop( luaState, -2 );

					if( obj != null && paramType.IsAssignableFrom( obj.GetType() ) )
					{
						return _extractNetObject;
					}
				}
				else
				{
					return null;
				}
			}
			else
			{
				object obj = _translator.GetNetObject( luaState, stackPos );
				if( obj != null && paramType.IsAssignableFrom( obj.GetType() ) )
				{
					return _extractNetObject;
				}
			}

			return null;
		}

		//
		// The following functions return the value in the Lua stack index stackPos as the desired type if it can, or null otherwise
		//

		object _GetAsBoolean( IntPtr luaState, int stackPos )
		{
			return LuaLib.lua_toboolean( luaState, stackPos );
		}

		object _GetAsByte( IntPtr luaState, int stackPos )
		{
			byte retVal = (byte)LuaLib.lua_tonumber( luaState, stackPos );
			if( retVal == 0 && !LuaLib.lua_isnumber( luaState, stackPos ) )
				return null;

			return retVal;
		}

		object _GetAsChar( IntPtr luaState, int stackPos )
		{
			char retVal = (char)LuaLib.lua_tonumber( luaState, stackPos );
			if( retVal == 0 && !LuaLib.lua_isnumber( luaState, stackPos ) )
				return null;

			return retVal;
		}

		object _GetAsCharArray( IntPtr luaState, int stackPos )
		{
			if( LuaLib.lua_type( luaState, stackPos ) != LuaTypes.LUA_TSTRING )
				return null;

			string retVal = LuaLib.lua_tostring( luaState, stackPos ).ToString();
			return retVal.ToCharArray();
		}

		object _GetAsDecimal( IntPtr luaState, int stackPos )
		{
			decimal retVal = (decimal)LuaLib.lua_tonumber( luaState, stackPos );
			if( retVal == 0 && !LuaLib.lua_isnumber( luaState, stackPos ) )
				return null;

			return retVal;
		}

		object _GetAsDouble( IntPtr luaState, int stackPos )
		{
			double retVal = LuaLib.lua_tonumber( luaState, stackPos );
			if( retVal == 0 && !LuaLib.lua_isnumber( luaState, stackPos ) )
				return null;

			return retVal;
		}

		object _GetAsFloat( IntPtr luaState, int stackPos )
		{
			float retVal = (float)LuaLib.lua_tonumber( luaState, stackPos );
			if( retVal == 0 && !LuaLib.lua_isnumber( luaState, stackPos ) )
				return null;

			return retVal;
		}

		object _GetAsInt( IntPtr luaState, int stackPos )
		{
			if( !LuaLib.lua_isnumber( luaState, stackPos ) )
				return null;

			int retVal = (int)LuaLib.lua_tonumber( luaState, stackPos );
			return retVal;
		}

		object _GetAsLong( IntPtr luaState, int stackPos )
		{
			long retVal = (long)LuaLib.lua_tonumber( luaState, stackPos );
			if( retVal == 0 && !LuaLib.lua_isnumber( luaState, stackPos ) )
				return null;

			return retVal;
		}

		public object _GetAsObject( IntPtr luaState, int stackPos )
		{
			if( LuaLib.lua_type( luaState, stackPos ) == LuaTypes.LUA_TTABLE )
			{
				if( LuaLib.luaL_getmetafield( luaState, stackPos, "__index" ) )
				{
					if( LuaLib.luaL_checkmetatable( luaState, -1 ) )
					{
						LuaLib.lua_insert( luaState, stackPos );
						LuaLib.lua_remove( luaState, stackPos + 1 );
					}
					else
					{
						LuaLib.lua_settop( luaState, -2 );
					}
				}
			}

			return _translator.GetObject( luaState, stackPos );
		}

		object _GetAsSbyte( IntPtr luaState, int stackPos )
		{
			sbyte retVal = (sbyte)LuaLib.lua_tonumber( luaState, stackPos );
			if( retVal == 0 && !LuaLib.lua_isnumber( luaState, stackPos ) )
				return null;

			return retVal;
		}

		object _GetAsShort( IntPtr luaState, int stackPos )
		{
			short retVal = (short)LuaLib.lua_tonumber( luaState, stackPos );
			if( retVal == 0 && !LuaLib.lua_isnumber( luaState, stackPos ) )
				return null;

			return retVal;
		}

		object _GetAsString( IntPtr luaState, int stackPos )
		{
			if( LuaLib.lua_type( luaState, stackPos ) != LuaTypes.LUA_TSTRING )
				return null;

			string retVal = LuaLib.lua_tostring( luaState, stackPos ).ToString();
			return retVal;
		}

		object _GetAsUInt( IntPtr luaState, int stackPos )
		{
			uint retVal = (uint)LuaLib.lua_tonumber( luaState, stackPos );
			if( retVal == 0 && !LuaLib.lua_isnumber( luaState, stackPos ) )
				return null;

			return retVal;
		}

		object _GetAsULong( IntPtr luaState, int stackPos )
		{
			ulong retVal = (ulong)LuaLib.lua_tonumber( luaState, stackPos );
			if( retVal == 0 && !LuaLib.lua_isnumber( luaState, stackPos ) )
				return null;

			return retVal;
		}

		object _GetAsUShort( IntPtr luaState, int stackPos )
		{
			ushort retVal = (ushort)LuaLib.lua_tonumber( luaState, stackPos );
			if( retVal == 0 && !LuaLib.lua_isnumber( luaState, stackPos ) )
				return null;

			return retVal;
		}

		object _GetAsFunction( IntPtr luaState, int stackPos )
		{
			return _translator.GetFunction( luaState, stackPos );
		}

		object _GetAsTable( IntPtr luaState, int stackPos )
		{
			return _translator.GetTable( luaState, stackPos );
		}

		object _GetAsUserdata( IntPtr luaState, int stackPos )
		{
			return _translator.GetUserData( luaState, stackPos );
		}

		object _GetAsNetObject( IntPtr luaState, int stackPos )
		{
			object obj = _translator.GetNetObject( luaState, stackPos );

			if( obj == null && LuaLib.lua_type( luaState, stackPos ) == LuaTypes.LUA_TTABLE )
			{
				if( LuaLib.luaL_getmetafield( luaState, stackPos, "__index" ) )
				{
					if( LuaLib.luaL_checkmetatable( luaState, -1 ) )
					{
						LuaLib.lua_insert( luaState, stackPos );
						LuaLib.lua_remove( luaState, stackPos + 1 );
						obj = _translator.GetNetObject( luaState, stackPos );
					}
					else
					{
						LuaLib.lua_settop( luaState, -2 );
					}
				}
			}

			return obj;
		}
	}
}
