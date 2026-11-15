using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace QoL_Unknown;

internal static class ContainerStackingPatcher
{
	internal const string ModuleId = "qol_inv_stacking";

	private static Vector2? containerItemSize;

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
			Unsquish(__instance);
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

	internal static void Unsquish(PlayerCamera camera)
	{
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		Container currentContainer = camera.currentContainer;
		if ((Object)(object)currentContainer == (Object)null || ((Component)currentContainer).transform.childCount <= 36)
		{
			return;
		}
		if (!containerItemSize.HasValue)
		{
			GameObject val = Resources.Load<GameObject>("ContainerItem");
			if ((Object)(object)val == (Object)null)
			{
				return;
			}
			containerItemSize = val.GetComponent<RectTransform>().sizeDelta;
		}
		int num = 0;
		foreach (GameObject containerUnloadButton in camera.containerUnloadButtons)
		{
			if (!((Object)(object)containerUnloadButton == (Object)null))
			{
				RectTransform component = containerUnloadButton.GetComponent<RectTransform>();
				if (!((Object)(object)component == (Object)null))
				{
					component.sizeDelta = containerItemSize.Value;
					component.anchoredPosition = new Vector2(34.5f + (float)(num % 3) * 66f, -34.5f - (float)(num / 3) * 66f);
					num++;
				}
			}
		}
	}
}
