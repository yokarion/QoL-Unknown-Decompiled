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
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Expected O, but got Unknown
		if (!initialized)
		{
			initialized = true;
			new Harmony("org.bepinex.plugins.qol.discordrpc").PatchAll(typeof(DiscordRPCHelper));
			GameObject val = new GameObject("DiscordManager_Persistent")
			{
				hideFlags = (HideFlags)61
			};
			Object.DontDestroyOnLoad((Object)val);
			val.AddComponent<DiscordManager>();
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
