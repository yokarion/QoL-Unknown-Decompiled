using System.Collections.Generic;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;

namespace QoL_Unknown;

internal class ContainerTransferPatcher
{
	private static ManualLogSource Logger;

	public static void Initialize(ManualLogSource logger)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Expected O, but got Unknown
		Logger = logger;
		Harmony val = new Harmony("org.bepinex.plugins.qol.containertransfer");
		val.PatchAll(typeof(ContainerTransferPatcher));
	}

	[HarmonyPatch(typeof(PlayerCamera), "TryPerformInventoryAction")]
	[HarmonyPrefix]
	private static bool TryPerformInventoryAction_Prefix(PlayerCamera __instance, RaycastResult hit, List<RaycastResult> uiCasts, ref bool __result)
	{
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Expected O, but got Unknown
		if (!ModKeybinds.VanillaBindHeld("expanddesc"))
		{
			return true;
		}
		InvButton component = ((RaycastResult)(ref hit)).gameObject.GetComponent<InvButton>();
		if ((Object)(object)component == (Object)null || !component.Overlaps(uiCasts))
		{
			return true;
		}
		Item dragItem = __instance.dragItem;
		Item item = component.GetItem();
		if ((Object)(object)dragItem == (Object)(object)item)
		{
			return true;
		}
		Container component2 = ((Component)dragItem).GetComponent<Container>();
		Container component3 = ((Component)item).GetComponent<Container>();
		if ((Object)(object)component2 != (Object)null && (Object)(object)component3 != (Object)null)
		{
			__result = true;
			List<Item> list = new List<Item>();
			foreach (Transform item2 in ((Component)component2).transform)
			{
				Transform val = item2;
				Item component4 = ((Component)val).GetComponent<Item>();
				if ((Object)(object)component4 != (Object)null)
				{
					list.Add(component4);
				}
			}
			bool flag = false;
			foreach (Item item3 in list)
			{
				if (component3.CanHoldItem(item3))
				{
					component2.UnloadItem(item3, (Body)null);
					component3.LoadItem(item3);
					flag = true;
				}
			}
			if (flag)
			{
				__instance.PlayBackpackSound();
				if ((Object)(object)__instance.currentContainer == (Object)(object)component2 || (Object)(object)__instance.currentContainer == (Object)(object)component3)
				{
					__instance.RepopulateContainer();
				}
				component.UpdateGraphic();
			}
			else
			{
				__instance.PlayUISound((UISoundType)6, 1f);
			}
			return false;
		}
		return true;
	}
}
