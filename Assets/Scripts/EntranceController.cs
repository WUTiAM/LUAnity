using UnityEngine;
using UnityEngine.SceneManagement;

public class EntranceController : MonoBehaviour
{
	void Awake()
	{
		_InitializeUnity();
		_InitializeLua();
		_InitializeGame();
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

	void _InitializeGame()
	{
		Object o = Resources.Load( "Prefabs/GameController" );
		GameObject go = MonoBehaviour.Instantiate( o ) as GameObject;
		go.name = "Game";
		DontDestroyOnLoad( go );

		SceneManager.sceneLoaded += delegate( Scene scene, LoadSceneMode mode )
		{
			AssetLoader.OnSceneLoaded( scene, mode );
		};
	}
}
