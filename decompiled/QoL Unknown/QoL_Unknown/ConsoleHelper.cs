using System.Reflection;
using UnityEngine;

namespace QoL_Unknown;

internal static class ConsoleHelper
{
	private static MethodInfo logToConsoleMethod;

	private static MethodInfo runCommandStringMethod;

	private static MethodInfo checkForWorldMethod;

	public static void LogToConsole(ConsoleScript instance, string message)
	{
		if (!((Object)(object)instance == (Object)null))
		{
			logToConsoleMethod = typeof(ConsoleScript).GetMethod("LogToConsole", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			logToConsoleMethod.Invoke(instance, new object[1] { message });
		}
	}

	public static void RunCommandString(ConsoleScript instance, string commandString)
	{
		if (!((Object)(object)instance == (Object)null))
		{
			if (runCommandStringMethod == null)
			{
				runCommandStringMethod = typeof(ConsoleScript).GetMethod("RunCommandString", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			}
			if (runCommandStringMethod != null)
			{
				runCommandStringMethod.Invoke(instance, new object[1] { commandString });
			}
		}
	}

	public static void CheckForWorld(ConsoleScript instance)
	{
		if (!((Object)(object)instance == (Object)null))
		{
			if (checkForWorldMethod == null)
			{
				checkForWorldMethod = typeof(ConsoleScript).GetMethod("CheckForWorld", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			}
			if (checkForWorldMethod != null)
			{
				checkForWorldMethod.Invoke(instance, null);
			}
		}
	}
}
