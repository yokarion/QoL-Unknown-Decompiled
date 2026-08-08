using BepInEx.Logging;
using HarmonyLib;

namespace QoL_Unknown;

internal class KeybindsPatcher
{
	private static ManualLogSource Logger;

	public static void Initialize(ManualLogSource logger)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Expected O, but got Unknown
		Logger = logger;
		ModKeybinds.Initialize(logger);
		Harmony val = new Harmony("org.bepinex.plugins.qol.keybinds");
		val.PatchAll(typeof(KeybindsPatcher));
		Logger.LogInfo((object)"KeybindsPatcher initialized");
	}

	[HarmonyPatch(typeof(Settings), "LoadSettings")]
	[HarmonyPostfix]
	private static void LoadSettings_Postfix()
	{
		ModKeybinds.EnsureDefaultsPresent();
	}
}
