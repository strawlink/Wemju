using System;
using UnityEngine;
using Wemju.Game.Base;

namespace Wemju.Console.Example
{
	public class ConsoleExample : MonoBehaviour, IBaseObject
	{
		private void Awake()
		{
			AddCommands();
		}


		// Use this for initialization
		private void Start()
		{
		}

		// Update is called once per frame
		private void Update()
		{
			IngameConsole.CheckInputPressed();
		}

		public void LoadCommands()
		{
			throw new NotImplementedException();
		}

		[ConsoleMethod(Method.ACTION_SHOOT)]
		public static void Shoot()
		{
			Debug.Log("Bang!");
		}

		[ConsoleMethod(Method.SERVER_CONNECT)]
		public static void Connect()
		{
			Debug.Log("Connect");
		}



		public void AddCommands()
		{
//			IngameConsole.GetVar(Command.SETTINGS_NAME).OnChange += ChangeName;
//			IngameConsole.GetVar(Command.SETTINGS_MAXFPS).OnChange += ChangeFps;

			//IngameConsole.RegisterVar(Command.SETTINGS_NAME, "defaultName", ChangeName);
			//IngameConsole.RegisterVar(Command.SETTINGS_MAXFPS, 125, ChangeFps);

			//IngameConsole.RegisterMethod(Method.ACTION_SHOOT, Shoot);
//			IngameConsole.RegisterMethod(Method.SERVER_CONNECT, Connect);

			//IngameConsole.StartLoadConfig();
		}
	}
}