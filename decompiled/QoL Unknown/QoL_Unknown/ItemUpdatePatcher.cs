using System;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace QoL_Unknown;

internal class ItemUpdatePatcher
{
	private static ManualLogSource Logger;

	public static void Initialize(ManualLogSource logger)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Expected O, but got Unknown
		Logger = logger;
		Harmony val = new Harmony("org.bepinex.plugins.qol.itemupdate");
		val.PatchAll(typeof(ItemUpdatePatcher));
		Logger.LogInfo((object)"ItemUpdatePatcher initialized");
	}

	[HarmonyPatch(typeof(Item), "Update")]
	[HarmonyPrefix]
	private static bool Update_Prefix(Item __instance)
	{
		try
		{
			if ((Object)(object)__instance == (Object)null)
			{
				return false;
			}
			if ((Object)(object)((Component)__instance).transform == (Object)null)
			{
				return false;
			}
			if ((Object)(object)WorldGeneration.world == (Object)null)
			{
				return false;
			}
			return true;
		}
		catch (Exception)
		{
			return false;
		}
	}

	[HarmonyPatch(typeof(Item), "Update")]
	[HarmonyFinalizer]
	private static Exception Update_Finalizer(Exception __exception)
	{
		if (__exception != null)
		{
			return null;
		}
		return __exception;
	}
}
