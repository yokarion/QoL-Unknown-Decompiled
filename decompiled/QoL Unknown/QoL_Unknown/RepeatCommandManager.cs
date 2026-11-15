using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace QoL_Unknown;

internal static class RepeatCommandManager
{
	private class RepeatEntry
	{
		public string Name;

		public string Commands;

		public float Delay;

		public int TimesTotal;

		public int TimesRemaining;

		public float Elapsed;

		public bool Paused;
	}

	private static readonly List<RepeatEntry> entries = new List<RepeatEntry>();

	private static int autoNameCounter;

	private static bool driverRunning;

	public static void StartRepeat(string[] args)
	{
		if (args.Length <= 3)
		{
			throw new Exception(string.Format("Expected at least {0} argument{1}, but got {2}.", 3, "s", args.Length - 1));
		}
		if (!int.TryParse(args[1], out var result))
		{
			throw new Exception("\"" + args[1] + "\" is not a valid integer value! (0, 1, 5, etc)");
		}
		if (!float.TryParse(args[2], NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var result2))
		{
			throw new Exception("\"" + args[2] + "\" is not a valid float value! (2, 0.7, 14.1, etc)");
		}
		if (result >= 1)
		{
			string text = ((args.Length > 4) ? string.Join(" ", args.Skip(4)).Trim() : "");
			if (text.Length == 0)
			{
				text = GenerateName();
			}
			else if (Find(text) != null)
			{
				throw new Exception("A repeat command named \"" + text + "\" is already active!");
			}
			ConsoleHelper.RunCommandString(ConsoleScript.instance, args[3]);
			if (result != 1)
			{
				entries.Add(new RepeatEntry
				{
					Name = text,
					Commands = args[3],
					Delay = result2,
					TimesTotal = result,
					TimesRemaining = result - 1,
					Elapsed = 0f,
					Paused = false
				});
				RefreshAutofill();
				EnsureDriver();
				ConsoleHelper.LogToConsole(ConsoleScript.instance, "<color=green>Repeat \"" + text + "\" started: " + result + "x \"" + args[3] + "\" every " + result2 + "s.</color>");
			}
		}
	}

	public static void Control(string[] args)
	{
		if (args.Length < 2)
		{
			throw new Exception("Expected at least 1 argument, but got " + (args.Length - 1) + ".");
		}
		string text = args[1].ToLowerInvariant();
		if (text == "list")
		{
			ListAll();
			return;
		}
		if (text != "pause" && text != "unpause" && text != "stop")
		{
			throw new Exception("\"" + args[1] + "\" is not a valid controlrepeat action! (pause, unpause, stop, list)");
		}
		if (args.Length < 3)
		{
			throw new Exception("Expected a repeat command name after \"" + text + "\".");
		}
		string text2 = string.Join(" ", args.Skip(2)).Trim();
		RepeatEntry repeatEntry = Find(text2);
		if (repeatEntry == null)
		{
			throw new Exception("No active repeat command named \"" + text2 + "\". Use controlrepeat list to see active repeats.");
		}
		switch (text)
		{
		case "pause":
			repeatEntry.Paused = true;
			ConsoleHelper.LogToConsole(ConsoleScript.instance, "<color=yellow>Paused repeat \"" + repeatEntry.Name + "\".</color>");
			break;
		case "unpause":
			repeatEntry.Paused = false;
			ConsoleHelper.LogToConsole(ConsoleScript.instance, "<color=green>Unpaused repeat \"" + repeatEntry.Name + "\".</color>");
			break;
		case "stop":
			entries.Remove(repeatEntry);
			RefreshAutofill();
			ConsoleHelper.LogToConsole(ConsoleScript.instance, "<color=orange>Stopped repeat \"" + repeatEntry.Name + "\".</color>");
			break;
		}
	}

	public static void RefreshAutofill()
	{
		Command val = ConsoleScript.SearchExact("controlrepeat");
		if (val != null && val.argAutofill != null)
		{
			val.argAutofill[1] = entries.Select((RepeatEntry e) => e.Name).ToList();
		}
	}

	private static void ListAll()
	{
		if (entries.Count == 0)
		{
			ConsoleHelper.LogToConsole(ConsoleScript.instance, "No active repeat commands.");
			return;
		}
		string text = "<color=green>Active repeat commands (" + entries.Count + "):</color>";
		foreach (RepeatEntry entry in entries)
		{
			int num = entry.TimesTotal - entry.TimesRemaining;
			text = text + "\n<color=yellow>" + entry.Name + "</color> - " + (entry.Paused ? "<color=orange>paused</color>" : "<color=green>running</color>") + ", " + num + "/" + entry.TimesTotal + " repeats done, every " + entry.Delay + "s <color=#888888>[" + entry.Commands + "]</color>";
		}
		ConsoleHelper.LogToConsole(ConsoleScript.instance, text);
	}

	private static RepeatEntry Find(string name)
	{
		return entries.FirstOrDefault((RepeatEntry e) => e.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
	}

	private static string GenerateName()
	{
		string text;
		do
		{
			autoNameCounter++;
			text = "repeat" + autoNameCounter;
		}
		while (Find(text) != null);
		return text;
	}

	private static void EnsureDriver()
	{
		if (!driverRunning && !((Object)(object)ConsoleScript.instance == (Object)null))
		{
			driverRunning = true;
			((MonoBehaviour)ConsoleScript.instance).StartCoroutine(Driver());
		}
	}

	private static IEnumerator Driver()
	{
		while (entries.Count > 0)
		{
			float unscaledDeltaTime = Time.unscaledDeltaTime;
			for (int num = entries.Count - 1; num >= 0; num--)
			{
				RepeatEntry repeatEntry = entries[num];
				if (!repeatEntry.Paused)
				{
					repeatEntry.Elapsed += unscaledDeltaTime;
					if (!(repeatEntry.Elapsed < repeatEntry.Delay))
					{
						repeatEntry.Elapsed -= repeatEntry.Delay;
						ConsoleHelper.RunCommandString(ConsoleScript.instance, repeatEntry.Commands);
						repeatEntry.TimesRemaining--;
						if (repeatEntry.TimesRemaining <= 0)
						{
							entries.RemoveAt(num);
							RefreshAutofill();
						}
					}
				}
			}
			yield return null;
		}
		driverRunning = false;
	}
}
