using System;
using System.Text;

namespace Extensions
{
	public static class ExtensionMethods
	{
		public static void SafeInvoke(this Action action)
		{
			var handler = action;
			if (handler != null)
			{
				handler();
			}
		}
		public static void SafeInvoke<T>(this Action<T> action, T arg1)
		{
			var handler = action;
			if (handler != null)
			{
				handler(arg1);
			}
		}

		public static byte[] GetBytes(this string message)
		{
			return Encoding.UTF8.GetBytes(message);
		}

		public static string GetString(this byte[] data)
		{
			return Encoding.UTF8.GetString(data);
		}
	}
}