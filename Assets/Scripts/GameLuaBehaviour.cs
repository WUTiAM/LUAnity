using LUAnity;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class GameLuaBehaviour : LuaBehaviour
{
	public static GameLuaBehaviour Instance { get; private set; }

	void Awake()
	{
		Instance = this;
	}

	void Update()
	{
	}
}
