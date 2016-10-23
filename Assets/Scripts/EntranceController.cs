using UnityEngine;
using UnityEngine.SceneManagement;

public class EntranceController : MonoBehaviour
{
	void Awake()
	{
		if( GameLuaBehaviour.Instance != null )
		{
			GameObject.Destroy( GameLuaBehaviour.Instance.gameObject );
		}

		_InitializeUnity();

		_InitializeLua();

		Object o = Resources.Load( "Prefabs/GameController" );
		GameObject go = MonoBehaviour.Instantiate( o ) as GameObject;
		go.name = "Game";
		DontDestroyOnLoad( go );

		SceneManager.LoadScene( "Main" );
	}

	void _InitializeUnity()
	{
		Application.runInBackground = true;
		Screen.sleepTimeout = SleepTimeout.NeverSleep;		
	}

	void _InitializeLua()
	{
		LuaSystem.Initialize();
	}
}
