namespace LUAnity
{
	using System;
	using System.Reflection;

	public class ProxyType
	{
		public Type UnderlyingSystemType { get { return _proxyType; } }

		Type _proxyType;

		public ProxyType( Type proxyType )
		{
			_proxyType = proxyType;
		}

		// Provide human readable short hand for this proxy object
		public override string ToString()
		{
			return "ProxyType(" + UnderlyingSystemType + ")";
		}

		public override bool Equals( object obj )
		{
			return _proxyType.Equals( obj );
		}

		public override int GetHashCode()
		{
			return _proxyType.GetHashCode();
		}

		public MemberInfo[] GetMember( string name, BindingFlags bindingAttr )
		{
			return _proxyType.GetMember( name, bindingAttr );
		}

		public MemberInfo[] GetMembers( BindingFlags bindingAttr )
		{
			return _proxyType.GetMembers( bindingAttr );
		}

		public MethodInfo GetMethod( string name, BindingFlags bindingAttr )
		{
			return _proxyType.GetMethod( name, bindingAttr );
		}

		public MethodInfo GetMethod( string name, BindingFlags bindingAttr, Type[] types )
		{
			return _proxyType.GetMethod( name, bindingAttr, null, types, null );
		}

		public MethodInfo[] GetMethods( BindingFlags bindingAttr )
		{
			return _proxyType.GetMethods( bindingAttr );
		}
	}
}
