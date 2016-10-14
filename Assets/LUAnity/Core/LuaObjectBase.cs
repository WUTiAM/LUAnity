namespace LUAnity
{
	using System;

	// Base class to provide consistent disposal flow across lua objects. 
	public abstract class LuaObjectBase : IDisposable
	{
		protected int _reference;
		protected Lua _interpreter;

		private bool _disposed;

		~LuaObjectBase()
		{
			Dispose( false );
		}

		#region IDisposable members
		public void Dispose()
		{
			Dispose( true );
			GC.SuppressFinalize( this );
		}

		public virtual void Dispose( bool disposeManagedResources )
		{
			if( !_disposed )
			{
				if( disposeManagedResources )
				{
					if( _reference != 0 )
					{
						_interpreter.Dispose( _reference );
					}
				}

				_interpreter = null;
				_disposed = true;
			}
		}
		#endregion

		#region Object members
		public override bool Equals( object o )
		{
			if( o is LuaObjectBase )
			{
				var luaObject = (LuaObjectBase)o;
				return _interpreter.CompareRef( luaObject._reference, _reference );
			}

			return false;
		}

		public override int GetHashCode()
		{
			return _reference;
		}
		#endregion
	}
}