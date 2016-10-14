using LUAnity;
using System.Text;
using UnityEngine;

public class CreateGameObject : MonoBehaviour
{
	private string script = @"
		luanet.load_assembly( 'Assembly-CSharp' )
		Type = luanet.import_type( 'System.Type' )

		luanet.load_assembly( 'UnityEngine' )
		GameObject = luanet.import_type( 'UnityEngine.GameObject' )

		local newGameObj = GameObject( 'NewObj' )
		newGameObj:AddComponent( Type.GetType( 'UnityEngine.ParticleSystem, UnityEngine' ) )
	";

	void Start()
	{
		Lua l = new Lua();
		l.DoString( Encoding.UTF8.GetBytes( script ) );
	}
}
