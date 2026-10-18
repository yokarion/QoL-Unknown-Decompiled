using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace QoL_Unknown;

internal static class ContainerStackingPatcher
{
	internal const string ModuleId = "qol_inv_stacking";

	public static void Initialize(ManualLogSource logger)
	{
		RuntimePatchRegistry.PatchAllOnce("org.bepinex.plugins.qol.containerstacking", typeof(ContainerStackingPatcher), logger);
	}

	[HarmonyPatch(typeof(PlayerCamera), "ClearContainer")]
	[HarmonyPrefix]
	private static void ClearContainer_Prefix(PlayerCamera __instance)
	{
		foreach (GameObject containerUnloadButton in __instance.containerUnloadButtons)
		{
			if ((Object)(object)containerUnloadButton != (Object)null)
			{
				containerUnloadButton.SetActive(false);
			}
		}
	}

	[HarmonyPatch(typeof(PlayerCamera), "RepopulateContainer")]
	[HarmonyPostfix]
	[HarmonyPriority(800)]
	private static void RepopulateContainer_Postfix(PlayerCamera __instance)
	{
		if (QoLModuleManager.IsEnabled("qol_inv_stacking") && !((Object)(object)__instance.currentContainer == (Object)null) && !((Object)(object)__instance.contPopulate == (Object)null))
		{
			ContainerStackingView containerStackingView = __instance.contPopulate.GetComponent<ContainerStackingView>();
			if ((Object)(object)containerStackingView == (Object)null)
			{
				containerStackingView = __instance.contPopulate.AddComponent<ContainerStackingView>();
			}
			containerStackingView.Rebuild(__instance);
		}
	}

	internal static bool TryGetMaxScroll(PlayerCamera camera, out float maxScroll)
	{
		ContainerStackingView containerStackingView = (((Object)(object)camera.contPopulate != (Object)null) ? camera.contPopulate.GetComponent<ContainerStackingView>() : null);
		maxScroll = 0f;
		if ((Object)(object)containerStackingView == (Object)null || !((Behaviour)containerStackingView).enabled || !containerStackingView.IsShowing(camera.currentContainer))
		{
			return false;
		}
		maxScroll = containerStackingView.MaxScroll;
		return true;
	}
}
