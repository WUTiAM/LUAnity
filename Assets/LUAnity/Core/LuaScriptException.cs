namespace LUAnity
{
	using System;

	// Exceptions thrown by the Lua runtime because of errors in the script
	public class LuaScriptException : LuaException
	{
		// Returns true if the exception has occured as the result of a .NET exception in user code
		public bool IsNetException { get; private set; }
		// The position in the script where the exception was triggered.
		public override string Source { get { return _source; } }

		private readonly string _source;

		// Creates a new Lua-only exception.
		public LuaScriptException( string message, string source ) : base( message )
		{
			_source = source;
		}

		// Creates a new .NET wrapping exception.
		public LuaScriptException( Exception innerException, string source )
			: base( "A .NET exception occured in user-code", innerException )
		{
			IsNetException = true;

			_source = source;
		}

		public override string ToString()
		{
			// Prepend the error source
			return GetType().FullName + ": " + _source + Message;
		}
	}
}
