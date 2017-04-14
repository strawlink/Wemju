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
			if (Input.GetKeyDown(KeyCode.F1))
			{
				Console.SetVar(Command.SETTINGS_NAME, "newName1");
			}
			if (Input.GetKeyDown(KeyCode.F2))
			{
				Console.SetVar(Command.SETTINGS_NAME, "newName2");
			}

			if (Input.GetKeyDown(KeyCode.F3))
			{
				Debug.Log("Current value is: " + Console.GetVar<string>(Command.SETTINGS_NAME));
			}

			if (Input.GetKeyDown(KeyCode.F4))
			{
				var val = Console.GetDefaultVar<string>(Command.SETTINGS_NAME);
				Debug.Log("Default value is: " + val);
			}

			if (Input.GetKeyDown(KeyCode.F5))
			{
				Console.ExecuteCommand("set_name test1");
			}

			if (Input.GetKeyDown(KeyCode.F6))
			{
				Console.ExecuteCommand("set_maxfps 100");
			}
			if (Input.GetKeyDown(KeyCode.F7))
			{
				Console.ExecuteCommand("set_maxfps 200");
			}
			if (Input.GetKeyDown(KeyCode.F8))
			{
				Console.ExecuteCommand("set_maxfps monkey");
			}



			if (Input.GetKeyDown(KeyCode.F9))
			{
				Console.ExecuteCommand("+bind lmb +shoot");
			}
			if (Input.GetKeyDown(KeyCode.F10))
			{
				Console.ExecuteCommand("+bind lmb +connect");
			}



			Console.CheckInputPressed();
		}

		public void LoadCommands()
		{
			throw new System.NotImplementedException();
		}

		public void ChangeName(object val)
		{
			Debug.Log("Change Name to: " + val as string);
		}

		public void ChangeFps(object val)
		{
			Debug.Log("Change fps to: " + Convert.ToInt32(val));
		}

		public void Shoot()
		{
			Debug.Log("Bang!");
		}
		public void Connect()
		{
			Debug.Log("Connect");
		}

		public void AddCommands()
		{
			Console.RegisterVar(Command.SETTINGS_NAME, "defaultName", ChangeName);
			Console.RegisterVar(Command.SETTINGS_MAXFPS, 125, ChangeFps);

			Console.RegisterMethod(Method.ACTION_SHOOT, Shoot);
			Console.RegisterMethod(Method.SERVER_CONNECT, Connect);

			Console.StartLoadConfig();
		}
	}
}