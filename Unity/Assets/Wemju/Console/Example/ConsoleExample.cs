using UnityEngine;

namespace Wemju.Console.Example
{
	public class ConsoleExample : MonoBehaviour
	{
		private void Awake()
		{
			IngameConsole.Initialize();
		}

		private void Update()
		{
			IngameConsole.CheckInputPressed();
		}
	}
}