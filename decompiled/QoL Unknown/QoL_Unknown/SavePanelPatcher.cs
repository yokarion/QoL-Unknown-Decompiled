using BepInEx.Logging;
using HarmonyLib;

namespace QoL_Unknown;

internal class SavePanelPatcher
{
	private static ManualLogSource Logger;

	public static void Initialize(ManualLogSource logger)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		Logger = logger;
		new Harmony("org.bepinex.plugins.qol.savepanel").PatchAll(typeof(SavePanelPatcher));
	}
}
