using System.Collections.Generic;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;

namespace QoL_Unknown;

internal class CtrlDragPatcher
{
	private enum LocationKind
	{
		BodySlot,
		Wearable,
		Container,
		Unknown
	}

	private struct ItemLocation
	{
		public LocationKind kind;

		public int slotIndex;

		public Container container;

		public string wearSlotId;
	}

	private static ManualLogSource Logger;

	private static bool initialized;

	public static void Initialize(ManualLogSource logger)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Expected O, but got Unknown
		if (!initialized)
		{
			initialized = true;
			Logger = logger;
			Harmony val = new Harmony("org.bepinex.plugins.qol.liquidquicktransfer");
			val.PatchAll(typeof(CtrlDragPatcher));
			Logger.LogInfo((object)"CtrlDragPatcher initialized");
		}
	}

	[HarmonyPatch(typeof(PlayerCamera), "TryPerformInventoryAction")]
	[HarmonyPrefix]
	private static bool ShiftDrag_Prefix(PlayerCamera __instance, RaycastResult hit, List<RaycastResult> uiCasts, ref bool __result)
	{
		//IL_0189: Unknown result type (might be due to invalid IL or missing references)
		//IL_0190: Expected O, but got Unknown
		if (!Input.GetKey((KeyCode)304) && !Input.GetKey((KeyCode)303))
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
		if ((Object)(object)dragItem == (Object)null || (Object)(object)item == (Object)null || (Object)(object)dragItem == (Object)(object)item)
		{
			return true;
		}
		WaterContainerItem component2 = ((Component)dragItem).GetComponent<WaterContainerItem>();
		WaterContainerItem component3 = ((Component)item).GetComponent<WaterContainerItem>();
		Container component4 = ((Component)dragItem).GetComponent<Container>();
		Container component5 = ((Component)item).GetComponent<Container>();
		if ((Object)(object)component2 != (Object)null && (Object)(object)component3 != (Object)null && component2.CurrentTotal > 0f && component3.SpaceLeft > 0f)
		{
			float num = Mathf.Min(component2.CurrentTotal, component3.SpaceLeft);
			__instance.body.CombineLiquids(component3, component2, num);
			if (!KrokoshaMpCompat.TryAnnounceLiquidTransfer(__instance.body, item, dragItem, num))
			{
				__instance.PlayUISound((UISoundType)6, 1f);
			}
			component.UpdateGraphic();
			__result = true;
			return false;
		}
		if ((Object)(object)component4 != (Object)null && (Object)(object)component5 != (Object)null)
		{
			Item val2 = default(Item);
			foreach (Transform item2 in ((Component)component4).transform)
			{
				Transform val = item2;
				if (!((Component)val).TryGetComponent<Item>(ref val2) || !component5.CanHoldItem(val2))
				{
					continue;
				}
				return true;
			}
			__result = true;
			return false;
		}
		return true;
	}

	[HarmonyPatch(typeof(PlayerCamera), "TryPerformInventoryAction")]
	[HarmonyPrefix]
	private static bool CtrlDrag_Prefix(PlayerCamera __instance, RaycastResult hit, List<RaycastResult> uiCasts, ref bool __result)
	{
		if (!ModKeybinds.GetAction("inventoryinteraction"))
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
		if ((Object)(object)dragItem == (Object)null || (Object)(object)item == (Object)null || (Object)(object)dragItem == (Object)(object)item)
		{
			return true;
		}
		Body body = __instance.body;
		bool flag = (Object)(object)((Component)dragItem).GetComponent<Container>() != (Object)null || (Object)(object)((Component)dragItem).GetComponent<WaterContainerItem>() != (Object)null;
		bool flag2 = (Object)(object)((Component)item).GetComponent<Container>() != (Object)null || (Object)(object)((Component)item).GetComponent<WaterContainerItem>() != (Object)null;
		Container component2 = ((Component)item).GetComponent<Container>();
		if (flag && flag2)
		{
			ItemLocation itemLocation = GetItemLocation(body, dragItem);
			ItemLocation itemLocation2 = GetItemLocation(body, item);
			if (itemLocation.kind == LocationKind.Unknown || itemLocation2.kind == LocationKind.Unknown)
			{
				__instance.PlayUISound((UISoundType)6, 1f);
				__result = false;
				return false;
			}
			if (!CanPlaceAtLocation(dragItem, itemLocation2) || !CanPlaceAtLocation(item, itemLocation))
			{
				__instance.PlayUISound((UISoundType)6, 1f);
				__result = false;
				return false;
			}
			if (itemLocation.kind == LocationKind.BodySlot && itemLocation2.kind == LocationKind.BodySlot)
			{
				body.SwapSlots(itemLocation.slotIndex, itemLocation2.slotIndex);
			}
			else
			{
				DetachItem(body, dragItem);
				DetachItem(body, item);
				PlaceAtLocation(body, dragItem, itemLocation2);
				PlaceAtLocation(body, item, itemLocation);
			}
			__instance.PlayBackpackSound();
			RefreshUI(__instance);
			__result = true;
			return false;
		}
		if ((Object)(object)component2 != (Object)null && component2.CanHoldItem(dragItem))
		{
			if (GetItemLocation(body, dragItem).kind == LocationKind.Unknown)
			{
				__instance.PlayUISound((UISoundType)6, 1f);
				__result = false;
				return false;
			}
			DetachItem(body, dragItem);
			component2.LoadItem(dragItem);
			__instance.PlayBackpackSound();
			RefreshUI(__instance);
			__result = true;
			return false;
		}
		return true;
	}

	[HarmonyPatch(typeof(PlayerCamera), "TryPerformSpecialUIAction")]
	[HarmonyPrefix]
	private static bool TryPerformSpecialUIAction_Prefix(PlayerCamera __instance, RaycastResult hit, ref bool __result)
	{
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		if (!IsLiquidDrainTarget(__instance, ((RaycastResult)(ref hit)).gameObject))
		{
			return true;
		}
		Item dragItem = __instance.dragItem;
		if ((Object)(object)dragItem == (Object)null)
		{
			return true;
		}
		if ((Object)(object)WorldGeneration.world == (Object)null || (Object)(object)FluidManager.main == (Object)null)
		{
			return true;
		}
		if (FluidManager.main.HasLiquid(WorldGeneration.world.WorldToBlockPos(Vector2.op_Implicit(((Component)dragItem).transform.position))))
		{
			return true;
		}
		if (!__instance.body.DoPickupCheck(dragItem, true))
		{
			return true;
		}
		WaterContainerItem component = ((Component)dragItem).GetComponent<WaterContainerItem>();
		if ((Object)(object)component == (Object)null || component.CurrentTotal <= 0f)
		{
			return true;
		}
		List<float> list = component.CalculateDrain(component.CurrentTotal);
		if (list == null || list.Count == 0)
		{
			return true;
		}
		using (KrokoshaMpCompat.BeginInventoryMutation(__instance.body))
		{
			KrokoshaMpCompat.TryEnsureItemSync(dragItem);
			component.Drain(list);
			KrokoshaMpCompat.MarkItemMoved(dragItem, containerChanged: false);
		}
		RefreshUI(__instance);
		__result = true;
		return false;
	}

	private static void RefreshUI(PlayerCamera cam)
	{
		if (cam.containerMenu.activeSelf && (Object)(object)cam.currentContainer != (Object)null)
		{
			cam.RepopulateContainer();
		}
		cam.UpdateWearables();
	}

	private static bool IsLiquidDrainTarget(PlayerCamera cam, GameObject hitObject)
	{
		GameObject val = (((Object)(object)cam != (Object)null) ? cam.liquidDrainObject : null);
		if ((Object)(object)val == (Object)null || (Object)(object)hitObject == (Object)null)
		{
			return false;
		}
		Transform transform = hitObject.transform;
		Transform transform2 = val.transform;
		return (Object)(object)hitObject == (Object)(object)val || (Object)(object)transform == (Object)(object)transform2 || transform.IsChildOf(transform2);
	}

	private static ItemLocation GetItemLocation(Body body, Item item)
	{
		for (int i = 0; i < body.slots.Length; i++)
		{
			if ((Object)(object)body.GetItem(i) == (Object)(object)item)
			{
				return new ItemLocation
				{
					kind = LocationKind.BodySlot,
					slotIndex = i
				};
			}
		}
		if (item.Stats.wearable && (Object)(object)body.GetWearableBySlotID(item.Stats.wearSlotId) == (Object)(object)item)
		{
			return new ItemLocation
			{
				kind = LocationKind.Wearable,
				wearSlotId = item.Stats.wearSlotId
			};
		}
		if ((Object)(object)((Component)item).transform.parent != (Object)null)
		{
			Container component = ((Component)((Component)item).transform.parent).GetComponent<Container>();
			if ((Object)(object)component != (Object)null)
			{
				return new ItemLocation
				{
					kind = LocationKind.Container,
					container = component
				};
			}
		}
		return new ItemLocation
		{
			kind = LocationKind.Unknown
		};
	}

	private static bool CanPlaceAtLocation(Item item, ItemLocation loc)
	{
		LocationKind kind = loc.kind;
		if (1 == 0)
		{
		}
		bool result = kind switch
		{
			LocationKind.BodySlot => true, 
			LocationKind.Wearable => item.Stats.wearable && item.Stats.wearSlotId == loc.wearSlotId, 
			LocationKind.Container => loc.container.CanHoldItem(item), 
			_ => false, 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	private static void DetachItem(Body body, Item item)
	{
		if ((Object)(object)((Component)item).transform.parent != (Object)null)
		{
			Container component = ((Component)((Component)item).transform.parent).GetComponent<Container>();
			if ((Object)(object)component != (Object)null)
			{
				component.UnloadItem(item, (Body)null);
				return;
			}
		}
		if (body.HoldingItem(item))
		{
			body.DropItem(item);
		}
		else if (item.Stats.wearable && (Object)(object)body.GetWearableBySlotID(item.Stats.wearSlotId) == (Object)(object)item)
		{
			body.DropWearable(item);
		}
	}

	private static void PlaceAtLocation(Body body, Item item, ItemLocation loc)
	{
		switch (loc.kind)
		{
		case LocationKind.BodySlot:
			body.PickUpItem(item, loc.slotIndex, true);
			break;
		case LocationKind.Wearable:
			body.WearWearable(item);
			break;
		case LocationKind.Container:
			loc.container.LoadItem(item);
			break;
		}
	}
}
