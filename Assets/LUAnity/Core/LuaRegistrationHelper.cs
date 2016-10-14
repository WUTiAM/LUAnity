namespace LUAnity
{
	using System;
	using System.Diagnostics.CodeAnalysis;
	using System.Reflection;

	public static class LuaRegistrationHelper
	{
		#region Tagged instance methods
		// Registers all public instance methods in an object tagged with LuaGlobalAttribute as Lua global functions
		// lua: The Lua VM to add the methods to
		// o: The object to get the methods from
		public static void TaggedInstanceMethods( Lua lua, object o )
		{
			#region Sanity checks
			if( lua == null )
			{
				throw new ArgumentNullException( "lua" );
			}

			if( o == null )
			{
				throw new ArgumentNullException( "o" );
			}
			#endregion

			foreach( var method in o.GetType().GetMethods( BindingFlags.Instance | BindingFlags.Public ) )
			{
				foreach( LuaGlobalAttribute attribute in method.GetCustomAttributes( typeof( LuaGlobalAttribute ), true ) )
				{
					if( string.IsNullOrEmpty( attribute.Name ) )
					{
						lua.RegisterFunction( method.Name, o, method ); // CLR name
					}
					else
					{
						lua.RegisterFunction( attribute.Name, o, method ); // Custom name
					}
				}
			}
		}
		#endregion

		#region Tagged static methods
		// Registers all public static methods in a class tagged with LuaGlobalAttribute as Lua global functions
		// lua: The Lua VM to add the methods to
		// type: The class type to get the methods from
		public static void TaggedStaticMethods( Lua lua, Type type )
		{
			#region Sanity checks
			if( lua == null )
			{
				throw new ArgumentNullException( "lua" );
			}

			if( type == null )
			{
				throw new ArgumentNullException( "type" );
			}

			if( !type.IsClass() )
			{
				throw new ArgumentException( "The type must be a class!", "type" );
			}
			#endregion

			foreach( var method in type.GetMethods( BindingFlags.Static | BindingFlags.Public ) )
			{
				foreach( LuaGlobalAttribute attribute in method.GetCustomAttributes( typeof( LuaGlobalAttribute ), false ) )
				{
					if( string.IsNullOrEmpty( attribute.Name ) )
					{
						lua.RegisterFunction( method.Name, null, method ); // CLR name
					}
					else
					{
						lua.RegisterFunction( attribute.Name, null, method ); // Custom name
					}
				}
			}
		}
		#endregion

		#region Enumeration
		// Registers an enumeration's values for usage as a Lua variable table
		// T: The enum type to register
		// lua: The Lua VM to add the enum to
		[SuppressMessage( "Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Justification = "The type parameter is used to select an enum type" )]
		public static void Enumeration<T>( Lua lua )
		{
			#region Sanity checks
			if( lua == null )
			{
				throw new ArgumentNullException( "lua" );
			}
			#endregion

			var type = typeof( T );
			if( !type.IsEnum() )
			{
				throw new ArgumentException( "The type must be an enumeration!" );
			}

			string[] names = Enum.GetNames( type );
			var values = (T[])Enum.GetValues( type );

			lua.NewTable( type.Name );

			for( int i = 0; i < names.Length; i++ )
			{
				string path = type.Name + "." + names[i];
				lua[path] = values[i];
			}
		}
		#endregion
	}
}
