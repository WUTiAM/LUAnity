namespace LUAnity
{
	public class LuaUserData : LuaObjectBase
	{
		public LuaUserData( int reference, Lua interpreter )
		{
			_reference = reference;
			_interpreter = interpreter;
		}

		// Indexer for string fields of the userdata
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

		// Indexer for numeric fields of the userdata
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

		// Calls the userdata and returns its return values inside an array
		public object[] Call( params object[] args )
		{
			return _interpreter.CallFunction( this, args );
		}

		public override string ToString()
		{
			return "userdata";
		}
	}
}
