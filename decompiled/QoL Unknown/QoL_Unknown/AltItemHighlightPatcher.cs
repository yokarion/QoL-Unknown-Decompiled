using BepInEx.Logging;
using HarmonyLib;

namespace QoL_Unknown;

internal static class AltItemHighlightPatcher
{
	private static readonly bool FeatureEnabled;

	private static ManualLogSource Logger;

	public static void Initialize(ManualLogSource logger)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		Logger = logger;
		if (FeatureEnabled)
		{
			AltItemHighlightManager.EnsureInstance("startup");
			new Harmony("org.bepinex.plugins.qol.altitemhighlight").PatchAll(typeof(AltItemHighlightPatcher));
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
