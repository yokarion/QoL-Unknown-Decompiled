using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;

namespace QoL_Unknown;

internal class QuickStashPatcher
{
	private enum MiddleClickTargetState
	{
		Clear,
		Item,
		LineOfSightBlocked
	}

	private const string LightbulbItemId = "lightbulb";

	private static readonly string[] StashPriorityIds = new string[5] { "bandolier", "belt", "traumarig", "medkit", "materialpouch" };

	private const string MultiplayerHarmonyId = "CasualtiesMP";

	private static ManualLogSource Logger;

	private static int suppressRadialThroughFrame = -1;

	private static int suppressMultiplayerPointPingThroughFrame = -1;

	private static int handledQuickStashInputFrame = -1;

	public static void Initialize(ManualLogSource logger)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		Logger = logger;
		new Harmony("org.bepinex.plugins.qol.quickstash").PatchAll(typeof(QuickStashPatcher));
	}

	[HarmonyPatch(typeof(PlayerCamera), "Update")]
	[HarmonyPostfix]
	[HarmonyPriority(800)]
	private static void Update_Postfix(PlayerCamera __instance)
	{
		if (!((Object)(object)__instance.body == (Object)null) && __instance.body.alive && Time.timeScale != 0f && !((Object)(object)__instance.dragItem != (Object)null) && ModKeybinds.GetActionDown("quickstash") && handledQuickStashInputFrame != Time.frameCount)
		{
			TryHandleQuickStashInput(__instance, suppressPingOnSuccess: true);
		}
	}

	[HarmonyPatch(typeof(PlayerCamera), "Update")]
	[HarmonyPostfix]
	[HarmonyAfter(new string[] { "CasualtiesMP" })]
	[HarmonyPriority(0)]
	private static void Update_Postfix_AfterMp(PlayerCamera __instance)
	{
		if (IsRadialSuppressionActive())
		{
			ForceCloseRadialPing(__instance);
		}
	}

	[HarmonyPatch(typeof(PlayerCamera), "HandleWhileDragging")]
	[HarmonyPostfix]
	[HarmonyAfter(new string[] { "CasualtiesMP" })]
	[HarmonyPriority(0)]
	private static void HandleWhileDragging_Postfix(PlayerCamera __instance)
	{
		if (IsRadialSuppressionActive())
		{
			ForceCloseRadialPing(__instance);
		}
	}

	[HarmonyPatch(typeof(PlayerCamera), "LateUpdate")]
	[HarmonyPostfix]
	[HarmonyAfter(new string[] { "CasualtiesMP" })]
	[HarmonyPriority(0)]
	private static void LateUpdate_Postfix(PlayerCamera __instance)
	{
		if (IsRadialSuppressionActive())
		{
			ForceCloseRadialPing(__instance);
		}
	}

	internal static bool TryHandleQuickStashBeforeMultiplayerPing()
	{
		MultiplayerCompat.EnsureInitializedForRuntime();
		if (!MultiplayerCompat.IsActive || (Object)(object)PlayerCamera.main == (Object)null || !ModKeybinds.GetActionDown("quickstash"))
		{
			return false;
		}
		if (handledQuickStashInputFrame == Time.frameCount)
		{
			return true;
		}
		return TryHandleQuickStashInput(PlayerCamera.main, suppressPingOnSuccess: true);
	}

	private static bool TryHandleQuickStashInput(PlayerCamera cam, bool suppressPingOnSuccess)
	{
		if ((Object)(object)cam == (Object)null || (Object)(object)cam.body == (Object)null || !cam.body.alive || Time.timeScale == 0f || (Object)(object)cam.dragItem != (Object)null)
		{
			return false;
		}
		MiddleClickTargetState targetState;
		Item itemUnderMouse = GetItemUnderMouse(cam, out targetState);
		if ((Object)(object)itemUnderMouse == (Object)null)
		{
			return false;
		}
		if (!MultiplayerCompat.CanMoveItemNow(itemUnderMouse))
		{
			cam.PlayUISound((UISoundType)6, 1f);
			return false;
		}
		bool flag = Input.GetKey((KeyCode)304) || Input.GetKey((KeyCode)303);
		bool flag2;
		using (MultiplayerCompat.BeginInventoryMutation(cam.body))
		{
			flag2 = ((flag && !itemUnderMouse.Stats.wearable) ? TryQuickStashToSlot(cam, itemUnderMouse) : TryQuickStash(cam, itemUnderMouse));
		}
		if (flag2)
		{
			handledQuickStashInputFrame = Time.frameCount;
			if (suppressPingOnSuccess)
			{
				if (!cam.radialOpen && !cam.containerMenu.activeSelf)
				{
					SuppressMpRadialPing(cam);
				}
				suppressMultiplayerPointPingThroughFrame = Mathf.Max(suppressMultiplayerPointPingThroughFrame, Time.frameCount + 1);
			}
		}
		return flag2;
	}

	private static bool TryQuickStashToSlot(PlayerCamera cam, Item item)
	{
		if ((Object)(object)item == (Object)null)
		{
			return false;
		}
		Body body = cam.body;
		int[] array = new int[6] { 0, 1, 2, 3, 4, 5 };
		foreach (int num in array)
		{
			if (num >= body.slots.Length || ShouldSkipSlotForQuickStash(item, num) || (Object)(object)body.GetItem(num) != (Object)null)
			{
				continue;
			}
			if ((Object)(object)((Component)item).transform.parent != (Object)null)
			{
				Container component = ((Component)((Component)item).transform.parent).GetComponent<Container>();
				if (Object.op_Implicit((Object)(object)component))
				{
					component.UnloadItem(item, (Body)null);
				}
				else if (body.HoldingItem(item))
				{
					body.DropItem(item);
				}
				else if (item.Stats.wearable && (Object)(object)body.GetWearableBySlotID(item.Stats.wearSlotId) == (Object)(object)item)
				{
					body.DropWearable(item);
				}
			}
			body.PickUpItem(item, num, true);
			MultiplayerCompat.MarkItemMoved(item, containerChanged: true);
			if (cam.containerMenu.activeSelf && (Object)(object)cam.currentContainer != (Object)null)
			{
				cam.RepopulateContainer();
			}
			cam.UpdateWearables();
			cam.PlayUISound((UISoundType)0, 1f);
			return true;
		}
		cam.PlayUISound((UISoundType)6, 1f);
		return false;
	}

	private static Item GetItemUnderMouse(PlayerCamera cam, out MiddleClickTargetState targetState)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Expected O, but got Unknown
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		targetState = MiddleClickTargetState.Clear;
		PointerEventData val = new PointerEventData(EventSystem.current)
		{
			position = Vector2.op_Implicit(Input.mousePosition)
		};
		List<RaycastResult> list = new List<RaycastResult>();
		EventSystem.current.RaycastAll(val, list);
		ItemLabel val2 = default(ItemLabel);
		InvButton val3 = default(InvButton);
		foreach (RaycastResult item2 in list)
		{
			RaycastResult current = item2;
			if (((RaycastResult)(ref current)).gameObject.TryGetComponent<ItemLabel>(ref val2))
			{
				Item refItem = val2.refItem;
				if ((Object)(object)refItem == (Object)null)
				{
					return null;
				}
				targetState = MiddleClickTargetState.Item;
				bool flag = cam.body.DoPickupCheck(refItem, false);
				if (IsLineOfSightBlocked(cam.body, refItem))
				{
					targetState = MiddleClickTargetState.LineOfSightBlocked;
					return null;
				}
				if (!flag)
				{
					return null;
				}
				return refItem;
			}
			if (((RaycastResult)(ref current)).gameObject.TryGetComponent<InvButton>(ref val3))
			{
				Item item = val3.GetItem();
				if ((Object)(object)item != (Object)null)
				{
					targetState = MiddleClickTargetState.Item;
					return item;
				}
			}
		}
		Vector3 val4 = Camera.main.ScreenToWorldPoint(Input.mousePosition);
		Collider2D val5 = Physics2D.OverlapPoint(new Vector2(val4.x, val4.y), LayerMask.GetMask(new string[1] { "Item" }));
		if ((Object)(object)val5 != (Object)null)
		{
			Item val6 = ((Component)val5).GetComponent<Item>() ?? ((Component)val5).GetComponentInParent<Item>();
			if ((Object)(object)val6 != (Object)null)
			{
				targetState = MiddleClickTargetState.Item;
				bool flag2 = cam.body.DoPickupCheck(val6, false);
				if (IsLineOfSightBlocked(cam.body, val6))
				{
					targetState = MiddleClickTargetState.LineOfSightBlocked;
					return null;
				}
				if (flag2)
				{
					return val6;
				}
			}
		}
		return null;
	}

	internal static bool ShouldSuppressMultiplayerPointPingNow()
	{
		if (MultiplayerCompat.IsActive)
		{
			return Time.frameCount <= suppressMultiplayerPointPingThroughFrame;
		}
		return false;
	}

	internal static bool TryGetPickupableWorldItemUnderMouse(PlayerCamera cam, out Item item)
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		item = null;
		if ((Object)(object)cam == (Object)null || (Object)(object)cam.body == (Object)null || (Object)(object)Camera.main == (Object)null)
		{
			return false;
		}
		Collider2D val = Physics2D.OverlapPoint(Vector2.op_Implicit(Camera.main.ScreenToWorldPoint(Input.mousePosition)), LayerMask.GetMask(new string[1] { "Item" }));
		item = (((Object)(object)val != (Object)null) ? (((Component)val).GetComponent<Item>() ?? ((Component)val).GetComponentInParent<Item>()) : null);
		if ((Object)(object)item != (Object)null && !IsLineOfSightBlocked(cam.body, item))
		{
			return cam.body.DoPickupCheck(item, true);
		}
		return false;
	}

	private static bool IsLineOfSightBlocked(Body body, Item item)
	{
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)body == (Object)null || (Object)(object)item == (Object)null)
		{
			return false;
		}
		if (body.HoldingItem(item) || (Object.op_Implicit((Object)(object)item.ParentContainer()) && body.HoldingItem(((Component)item.ParentContainer()).GetComponent<Item>())))
		{
			return false;
		}
		Vector2 val;
		if (!Object.op_Implicit((Object)(object)((Component)item).transform.parent))
		{
			Bounds bounds = ((Component)item).GetComponent<Collider2D>().bounds;
			val = Vector2.op_Implicit(((Bounds)(ref bounds)).center);
		}
		else
		{
			val = Vector2.op_Implicit(((Component)item).transform.position);
		}
		Vector2 val2 = val;
		bool flag = RaycastHit2D.op_Implicit(Physics2D.Linecast(Vector2.op_Implicit(((Component)body).transform.position), val2, LayerMask.GetMask(new string[1] { "Ground" })));
		if (flag && Object.op_Implicit((Object)(object)Physics2D.OverlapPoint(val2, LayerMask.GetMask(new string[1] { "Ground" }))))
		{
			flag = false;
		}
		return flag;
	}

	private static bool TryQuickStash(PlayerCamera cam, Item item)
	{
		if ((Object)(object)item == (Object)null)
		{
			return false;
		}
		Body body = cam.body;
		if ((Object)(object)((Component)item).GetComponent<Container>() != (Object)null && (Object)(object)cam.currentContainer == (Object)(object)((Component)item).GetComponent<Container>())
		{
			return false;
		}
		Container component = ((Component)item).GetComponent<Container>();
		if (item.Stats.wearable)
		{
			if (TryEquipWearable(cam, item))
			{
				cam.PlayUISound((UISoundType)0, 1f);
				return true;
			}
			if (((Object)(object)component == (Object)null || component.itemCount == 0) && TryMoveToBestContainer(cam, item))
			{
				return true;
			}
			cam.PlayUISound((UISoundType)6, 1f);
			return false;
		}
		if ((Object)(object)component != (Object)null && component.itemCount > 0)
		{
			if (TryMoveToSlot(cam, item))
			{
				cam.PlayUISound((UISoundType)0, 1f);
				return true;
			}
			cam.PlayUISound((UISoundType)6, 1f);
			return false;
		}
		if (TryMoveToBestContainer(cam, item))
		{
			return true;
		}
		int? num = FirstEmptyQuickStashSlot(body, item);
		if (num.HasValue)
		{
			body.PickUpItem(item, num.Value, true);
			MultiplayerCompat.MarkItemMoved(item, containerChanged: true);
			cam.PlayUISound((UISoundType)0, 1f);
			return true;
		}
		cam.PlayUISound((UISoundType)6, 1f);
		return false;
	}

	private static bool TryEquipWearable(PlayerCamera cam, Item item)
	{
		Body body = cam.body;
		if (!item.Stats.wearable || (Object)(object)body.GetWearableBySlotID(item.Stats.wearSlotId) != (Object)null)
		{
			return false;
		}
		body.WearWearable(item);
		if ((Object)(object)body.GetWearableBySlotID(item.Stats.wearSlotId) != (Object)(object)item)
		{
			return false;
		}
		MultiplayerCompat.MarkItemMoved(item, containerChanged: true);
		cam.UpdateWearables();
		if (cam.containerMenu.activeSelf && (Object)(object)cam.currentContainer != (Object)null)
		{
			cam.RepopulateContainer();
		}
		return true;
	}

	private static bool TryMoveToBestContainer(PlayerCamera cam, Item item)
	{
		//IL_01c0: Unknown result type (might be due to invalid IL or missing references)
		Body body = cam.body;
		List<Container> list = new List<Container>();
		for (int i = 0; i < body.slots.Length; i++)
		{
			Item item2 = body.GetItem(i);
			if ((Object)(object)item2 != (Object)null && (Object)(object)item2 != (Object)(object)item)
			{
				Container component = ((Component)item2).GetComponent<Container>();
				if (Object.op_Implicit((Object)(object)component) && component.CanHoldItem(item) && CanUseContainerForMp(component))
				{
					list.Add(component);
				}
			}
		}
		foreach (Item allWearable in body.GetAllWearables())
		{
			if ((Object)(object)allWearable != (Object)(object)item)
			{
				Container component2 = ((Component)allWearable).GetComponent<Container>();
				if (Object.op_Implicit((Object)(object)component2) && component2.CanHoldItem(item) && CanUseContainerForMp(component2))
				{
					list.Add(component2);
				}
			}
		}
		if (list.Count == 0)
		{
			return false;
		}
		Container val = list.OrderBy(StashPriority).ThenBy((Container c) => c.encumberanceMult).ThenBy((Container c) => c.maxWeightPerItem)
			.First();
		if ((Object)(object)((Component)item).transform.parent != (Object)null)
		{
			Container component3 = ((Component)((Component)item).transform.parent).GetComponent<Container>();
			if (Object.op_Implicit((Object)(object)component3))
			{
				component3.UnloadItem(item, (Body)null);
			}
			else if (body.HoldingItem(item))
			{
				body.DropItem(item);
			}
			else if (item.Stats.wearable && (Object)(object)body.GetWearableBySlotID(item.Stats.wearSlotId) == (Object)(object)item)
			{
				body.DropWearable(item);
			}
		}
		((Component)item).transform.position = ((Component)val).transform.position;
		val.LoadItem(item);
		MultiplayerCompat.MarkItemMoved(item, containerChanged: true);
		cam.PlayBackpackSound();
		if (cam.containerMenu.activeSelf && (Object)(object)cam.currentContainer != (Object)null)
		{
			cam.RepopulateContainer();
		}
		cam.UpdateWearables();
		return true;
	}

	private static int StashPriority(Container container)
	{
		Item component = ((Component)container).GetComponent<Item>();
		int num = (((Object)(object)component != (Object)null) ? Array.IndexOf(StashPriorityIds, component.id) : (-1));
		if (num < 0)
		{
			return StashPriorityIds.Length;
		}
		return num;
	}

	private static bool TryMoveToSlot(PlayerCamera cam, Item item)
	{
		Body body = cam.body;
		int? num = FirstEmptyQuickStashSlot(body, item);
		if (num.HasValue)
		{
			body.PickUpItem(item, num.Value, true);
			MultiplayerCompat.MarkItemMoved(item, containerChanged: true);
			if (cam.containerMenu.activeSelf && (Object)(object)cam.currentContainer != (Object)null)
			{
				cam.RepopulateContainer();
			}
			cam.UpdateWearables();
			return true;
		}
		return false;
	}

	private static int? FirstEmptyQuickStashSlot(Body body, Item item)
	{
		for (int i = 0; i < body.slots.Length; i++)
		{
			if (!ShouldSkipSlotForQuickStash(item, i) && !body.HoldingItem(i) && body.slots[i].canPickUp && (!item.Stats.onlyHoldInHands || body.slots[i].isHand))
			{
				return i;
			}
		}
		return null;
	}

	private static bool ShouldSkipSlotForQuickStash(Item item, int slot)
	{
		if (slot == 0 && (Object)(object)item != (Object)null)
		{
			return item.id == "lightbulb";
		}
		return false;
	}

	private static bool CanUseContainerForMp(Container container)
	{
		if ((Object)(object)container == (Object)null)
		{
			return false;
		}
		Item component = ((Component)container).GetComponent<Item>();
		if (!((Object)(object)component == (Object)null))
		{
			return MultiplayerCompat.CanMoveItemNow(component);
		}
		return true;
	}

	private static void SuppressMpRadialPing(PlayerCamera cam)
	{
		if (!((Object)(object)cam == (Object)null) && MultiplayerCompat.IsActive)
		{
			suppressRadialThroughFrame = Mathf.Max(suppressRadialThroughFrame, Time.frameCount + 1);
			ForceCloseRadialPing(cam);
		}
	}

	private static bool IsRadialSuppressionActive()
	{
		if (Time.frameCount <= suppressRadialThroughFrame)
		{
			return MultiplayerCompat.IsActive;
		}
		return false;
	}

	private static void ForceCloseRadialPing(PlayerCamera cam)
	{
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)cam == (Object)null))
		{
			cam.radialOpen = false;
			if ((Object)(object)cam.radialCircle != (Object)null)
			{
				((Behaviour)cam.radialCircle).enabled = false;
			}
			if ((Object)(object)cam.radialMenu != (Object)null)
			{
				((Transform)cam.radialMenu).localScale = Vector3.one * 0.01f;
				((Component)cam.radialMenu).gameObject.SetActive(false);
			}
		}
	}
}
