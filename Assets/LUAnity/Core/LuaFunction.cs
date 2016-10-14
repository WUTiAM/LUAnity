namespace LUAnity
{
	using System;

	public class LuaFunction : LuaObjectBase
	{
		internal LuaCSFunction Function;

		public LuaFunction( int reference, Lua interpreter )
		{
			Function = null;

			_reference = reference;
			_interpreter = interpreter;
		}

		public LuaFunction( LuaCSFunction function, Lua interpreter )
		{
			Function = function;

			_reference = 0;
			_interpreter = interpreter;
		}

		// Calls the function casting return values to the types in returnTypes
		internal object[] Call( object[] args, Type[] returnTypes )
		{
			return _interpreter.CallFunction( this, args, returnTypes );
		}

		// Calls the function and returns its return values inside an array
		public object[] Call( params object[] args )
		{
			return _interpreter.CallFunction( this, args );
		}

		// Pushes the function into the Lua stack
		internal void Push( IntPtr luaState )
		{
			if( _reference != 0 )
			{
				LuaLib.lua_rawgeti( luaState, LuaIndexes.LUA_REGISTRYINDEX, _reference );
			}
			else
			{
				_interpreter.PushFunction( Function );
			}
		}

		public override string ToString()
		{
			return "function";
		}

		public override bool Equals( object o )
		{
			if( o is LuaFunction )
			{
				LuaFunction lf = (LuaFunction)o;

				if( _reference != 0 && lf._reference != 0 )
				{
					return _interpreter.CompareRef( lf._reference, this._reference );
				}
				else
				{
					return ( Function == lf.Function );
				}
			}
			else
			{
				return false;
			}
		}

		public override int GetHashCode()
		{
			return ( _reference != 0 ? _reference : Function.GetHashCode() );
		}
	}
}
