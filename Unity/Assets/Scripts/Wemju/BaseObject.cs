using System.Collections;
using System.Collections.Generic;
using UnityEditor.MemoryProfiler;
using UnityEngine;

namespace Wemju.Game.Base
{
	public interface IBaseObject
	{
		void LoadCommands();
		void AddCommands();
	}
}
