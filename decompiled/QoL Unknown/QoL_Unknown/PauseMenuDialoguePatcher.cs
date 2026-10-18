using System.Collections.Generic;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace QoL_Unknown;

internal static class PauseMenuDialoguePatcher
{
	private const string HarmonyId = "org.bepinex.plugins.qol_unknown.pause_menu_dialogue";

	private static readonly List<string> customQuotes = new List<string> { "There's really no hard limit to how long these pause menu names can be and to be quite honest I'm rather curious to see how far we can go. Adolphus W. Green (1844–1917) started as the Principal of the Groton School in 1864. By 1865, he became second assistant librarian - oh wait nevermind there' a limit.", "Everything will be okay.", "Pause and hydrate. It'll help.", "You're almost there. Just a couple more steps." };

	private static ManualLogSource Logger;

	private static bool initialized;

	public static void Initialize(ManualLogSource logger)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Expected O, but got Unknown
		if (!initialized)
		{
			initialized = true;
			Logger = logger;
			Harmony val = new Harmony("org.bepinex.plugins.qol_unknown.pause_menu_dialogue");
			val.PatchAll(typeof(PauseMenuDialoguePatcher));
		}
	}

	[HarmonyPatch(typeof(Locale), "GetPauseQuote")]
	[HarmonyPostfix]
	private static void GetPauseQuote_Postfix(ref string __result)
	{
		if (customQuotes.Count == 0)
		{
			return;
		}
		int num = ((Locale.currentLang != null && Locale.currentLang.pauseQuotes != null) ? Locale.currentLang.pauseQuotes.Count : 0);
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
