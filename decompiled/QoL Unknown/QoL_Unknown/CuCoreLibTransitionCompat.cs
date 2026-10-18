using System;
using System.Reflection;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using HarmonyLib;

namespace QoL_Unknown;

internal static class CuCoreLibTransitionCompat
{
	private const string CuCoreLibPluginGuid = "net.cucorelib";

	private static ManualLogSource Logger;

	private static bool patchAttempted;

	public static bool SkipWorldRestore { get; internal set; }

	public static void Initialize(Harmony harmony, ManualLogSource logger)
	{
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Expected O, but got Unknown
		Logger = logger;
		if (patchAttempted)
		{
			return;
		}
		patchAttempted = true;
		if (!Chainloader.PluginInfos.ContainsKey("net.cucorelib"))
		{
			return;
		}
		try
		{
			Type type = AccessTools.TypeByName("CUCoreLib.Saving.SaveCoordinator");
			MethodInfo methodInfo = AccessTools.Method(type, "ApplyWorldProviders", (Type[])null, (Type[])null);
			if (methodInfo == null)
			{
				Logger.LogWarning((object)"[LayerTransition] CUCoreLib SaveCoordinator.ApplyWorldProviders not found; world restore guard inactive.");
				return;
			}
			MethodInfo method = typeof(CuCoreLibTransitionCompat).GetMethod("ApplyWorldProviders_Prefix", BindingFlags.Static | BindingFlags.NonPublic);
			harmony.Patch((MethodBase)methodInfo, new HarmonyMethod(method), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
		}
		catch (Exception ex)
		{
			Logger.LogWarning((object)("[LayerTransition] Failed to install CUCoreLib world-restore guard: " + ex.Message));
		}
	}

	private static bool ApplyWorldProviders_Prefix()
	{
		if (!SkipWorldRestore)
		{
			return true;
		}
		ManualLogSource logger = Logger;
		if (logger != null)
		{
			logger.LogInfo((object)"[LayerTransition] Skipping CUCoreLib world-provider restore after layer transition.");
		}
		return false;
	}
}
