using BepInEx.Logging;
using HarmonyLib;

namespace QoL_Unknown;

internal static class AltItemHighlightPatcher
{
	private static readonly bool FeatureEnabled;

	private static ManualLogSource Logger;

	public static void Initialize(ManualLogSource logger)
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Expected O, but got Unknown
		Logger = logger;
		if (FeatureEnabled)
		{
			AltItemHighlightManager.EnsureInstance("startup");
			Harmony val = new Harmony("org.bepinex.plugins.qol.altitemhighlight");
			val.PatchAll(typeof(AltItemHighlightPatcher));
			Logger.LogInfo((object)"AltItemHighlightPatcher initialized");
		}
	}

	public static void DebugLog(string message)
	{
	}

	[HarmonyPatch(typeof(PlayerCamera), "Update")]
	[HarmonyPostfix]
	private static void PlayerCamera_Update_Postfix()
	{
		if (FeatureEnabled)
		{
			AltItemHighlightManager.EnsureInstance("PlayerCamera.Update postfix")?.Tick();
		}
	}
}
