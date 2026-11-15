using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace QoL_Unknown;

internal static class PauseMenuDialoguePatcher
{
	private const string HarmonyId = "org.bepinex.plugins.qol_unknown.pause_menu_dialogue";

	private static readonly FieldInfo PauseQuotesField = AccessTools.Field(typeof(Language), "pauseQuotes");

	private static readonly List<string> customQuotes = new List<string> { "There's really no hard limit to how long these pause menu names can be and to be quite honest I'm rather curious to see how far we can go. Adolphus W. Green (1844–1917) started as the Principal of the Groton School in 1864. By 1865, he became second assistant librarian - oh wait nevermind there' a limit.", "Everything will be okay.", "Pause and hydrate. It'll help.", "You're almost there. Just a couple more steps." };

	private static ManualLogSource Logger;

	private static bool initialized;

	public static void Initialize(ManualLogSource logger)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		if (!initialized)
		{
			initialized = true;
			Logger = logger;
			new Harmony("org.bepinex.plugins.qol_unknown.pause_menu_dialogue").PatchAll(typeof(PauseMenuDialoguePatcher));
		}
	}

	[HarmonyPatch(typeof(Locale), "GetPauseQuote", new Type[] { typeof(string) })]
	[HarmonyPostfix]
	private static void GetPauseQuote_Postfix(string type, ref string __result)
	{
		if (customQuotes.Count == 0 || Locale.currentLang == null)
		{
			return;
		}
		List<string> value;
		int num = (((Dictionary<string, List<string>>)PauseQuotesField.GetValue(Locale.currentLang)).TryGetValue(type, out value) ? value.Count : 0);
		int num2 = num + customQuotes.Count;
		if (num2 > 0)
		{
			int num3 = Random.Range(0, num2);
			if (num3 >= num)
			{
				__result = customQuotes[num3 - num];
			}
		}
	}
}
