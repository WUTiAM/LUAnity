namespace LUAnity
{
	using System;
	using System.Collections;

	// Wrapper class for Lua tables
	public class LuaTable : LuaObjectBase
	{
		// Indexer for string fields of the table
		public object this[string field]
		{
			get
			{
				return _interpreter.GetObject( _reference, field );
			}
			set
			{
				_interpreter.SetObject( _reference, field, value );
			}
		}
		// Indexer for numeric fields of the table
		public object this[object field]
		{
			get
			{
				return _interpreter.GetObject( _reference, field );
			}
			set
			{
				_interpreter.SetObject( _reference, field, value );
			}
		}

		public ICollection Keys
		{
			get { return _interpreter.GetTableDict( this ).Keys; }
		}
		public ICollection Values
		{
			get { return _interpreter.GetTableDict( this ).Values; }
		}

		public LuaTable( int reference, Lua interpreter )
		{
			_reference = reference;
			_interpreter = interpreter;
		}

		public IDictionaryEnumerator GetEnumerator()
		{
			return _interpreter.GetTableDict( this ).GetEnumerator();
		}

		// Gets an string fields of a table ignoring its metatable, if it exists
		internal object RawGet( string field )
		{
			return _interpreter.RawGetObject( _reference, field );
		}

		// Pushes this table into the Lua stack
		internal void Push( IntPtr luaState )
		{
			LuaLib.lua_rawgeti( luaState, LuaIndexes.LUA_REGISTRYINDEX, _reference );
		}

		public override string ToString()
		{
			return "table";
		}
	}
}