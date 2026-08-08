using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace QoL_Unknown;

[HarmonyPatch(typeof(Talker), "Talk", new Type[]
{
	typeof(List<string>),
	typeof(Limb),
	typeof(bool),
	typeof(bool)
})]
internal class DiscordRPCHelper
{
	public static string LastSpokenSentence = "...";

	private static bool initialized = false;

	public static void Initialize(ManualLogSource logger)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Expected O, but got Unknown
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Expected O, but got Unknown
		if (!initialized)
		{
			initialized = true;
			Harmony val = new Harmony("org.bepinex.plugins.qol.discordrpc");
			val.PatchAll(typeof(DiscordRPCHelper));
			GameObject val2 = new GameObject("DiscordManager_Persistent");
			((Object)val2).hideFlags = (HideFlags)61;
			Object.DontDestroyOnLoad((Object)(object)val2);
			DiscordManager discordManager = val2.AddComponent<DiscordManager>();
		}
	}

	[HarmonyPostfix]
	private static void Postfix(Talker __instance)
	{
		string value = Traverse.Create((object)__instance).Field("currentString").GetValue<string>();
		if (!string.IsNullOrEmpty(value))
		{
			LastSpokenSentence = Regex.Replace(value, "<.*?>", string.Empty);
		}
	}
}
