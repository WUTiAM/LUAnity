namespace LUAnity
{
	using System;
	using System.Linq;
	using System.Reflection;

	static class TypeExtensions
	{
		public static bool HasMethod( this Type t, string name )
		{
			var op = t.GetMethods( BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static );
			return op.Any( m => m.Name == name );
		}

		public static bool HasAdditionOpertator( this Type t )
		{
			if( t.IsPrimitive() )
				return true;

			return t.HasMethod( "op_Addition" );
		}

		public static bool HasSubtractionOpertator( this Type t )
		{
			if( t.IsPrimitive() )
				return true;

			return t.HasMethod( "op_Subtraction" );
		}

		public static bool HasMultiplyOpertator( this Type t )
		{
			if( t.IsPrimitive() )
				return true;

			return t.HasMethod( "op_Multiply" );
		}

		public static bool HasDivisionOpertator( this Type t )
		{
			if( t.IsPrimitive() )
				return true;

			return t.HasMethod( "op_Division" );
		}

		public static bool HasModulusOpertator( this Type t )
		{
			if( t.IsPrimitive() )
				return true;

			return t.HasMethod( "op_Modulus" );
		}

		public static bool HasUnaryNegationOpertator( this Type t )
		{
			if( t.IsPrimitive() )
				return true;

			// Unary - will always have only one version
			var op = t.GetMethod( "op_UnaryNegation", BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static );
			return op != null;
		}

		public static bool HasEqualityOpertator( this Type t )
		{
			if( t.IsPrimitive() )
				return true;

			return t.HasMethod( "op_Equality" );
		}

		public static bool HasLessThanOpertator( this Type t )
		{
			if( t.IsPrimitive() )
				return true;

			return t.HasMethod( "op_LessThan" );
		}

		public static bool HasLessThanOrEqualOpertator( this Type t )
		{
			if( t.IsPrimitive() )
				return true;

			return t.HasMethod( "op_LessThanOrEqual" );
		}

		public static MethodInfo[] GetMethods( this Type t, string name, BindingFlags flags )
		{
			return t.GetMethods( flags ).Where( m => m.Name == name ).ToArray();
		}

		public static bool IsPrimitive( this Type t )
		{
			return t.IsPrimitive;
		}

		public static bool IsClass( this Type t )
		{
			return t.IsClass;
		}

		public static bool IsEnum( this Type t )
		{
			return t.IsEnum;
		}

		public static bool IsPublic( this Type t )
		{
			return t.IsPublic;
		}

		public static bool IsSealed( this Type t )
		{
			return t.IsSealed;
		}

		public static bool IsGenericType( this Type t )
		{
			return t.IsGenericType;
		}

		public static bool IsInterface( this Type t )
		{
			return t.IsInterface;
		}

		public static Assembly GetAssembly( this Type t )
		{
			return t.Assembly;
		}
	}
}
