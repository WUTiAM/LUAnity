using LUAnity;
using System.Text;
using UnityEngine;

public class HelloWorld : MonoBehaviour
{
	void Start()
	{
		Lua l = new Lua();
		l.DoString( Encoding.UTF8.GetBytes( "print('Hello world 世界')" ) );
	}
}
