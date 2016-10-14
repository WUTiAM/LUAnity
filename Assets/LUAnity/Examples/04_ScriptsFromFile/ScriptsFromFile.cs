using LUAnity;
using System.Text;
using UnityEngine;

public class ScriptsFromFile : MonoBehaviour
{
	public TextAsset scriptFile;

	void Start()
	{
		Lua l = new Lua();
		l.DoString( Encoding.UTF8.GetBytes( scriptFile.text ) );
	}
}
