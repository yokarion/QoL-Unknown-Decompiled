using BepInEx.Logging;
using HarmonyLib;

namespace QoL_Unknown;

internal class SavePanelPatcher
{
	private static ManualLogSource Logger;

	public static void Initialize(ManualLogSource logger)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Expected O, but got Unknown
		Logger = logger;
		Harmony val = new Harmony("org.bepinex.plugins.qol.savepanel");
		val.PatchAll(typeof(SavePanelPatcher));
	}
}
