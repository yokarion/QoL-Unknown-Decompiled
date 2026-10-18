using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace QoL_Unknown;

internal class InventorySortPatcher
{
	private sealed class InventoryModel
	{
		private sealed class Placement
		{
			public Item Item;

			public Container SourceContainer;

			public int SourceSlot = -1;

			public Container TargetContainer;

			public int TargetSlot = -1;

			public bool Assigned;

			public float Weight => ((Object)(object)Item != (Object)null) ? Item.totalWeight : 0f;
		}

		private sealed class ContainerInfo
		{
			private float plannedDelta;

			public Container Container { get; }

			public int Index { get; }

			public float Mult => Container.encumberanceMult;

			public bool IsSpecialized => Container.tagRestriction != null && Container.tagRestriction.Length != 0;

			public float RemainingWeight => Container.maxWeight - ProjectedWeight;

			private float ProjectedWeight => Container.GetHoldingWeight() + plannedDelta;

			public ContainerInfo(Container container, int index)
			{
				Container = container;
				Index = index;
			}

			public bool CanFit(Item item)
			{
				if ((Object)(object)item == (Object)null)
				{
					return false;
				}
				float totalWeight = item.totalWeight;
				return totalWeight <= Container.maxWeightPerItem && ProjectedWeight + totalWeight <= Container.maxWeight && IsAllowedByTags(item);
			}

			public void Reserve(Item item)
			{
				plannedDelta += item.totalWeight;
			}

			public void Release(Item item)
			{
				plannedDelta -= item.totalWeight;
			}

			private bool IsAllowedByTags(Item item)
			{
				if (!IsSpecialized)
				{
					return true;
				}
				IEnumerable<string> tags = item.Stats.GetTags();
				return tags != null && Container.tagRestriction.Intersect(tags).Any();
			}
		}

		private sealed class ContainerOption
		{
			public Placement Placement { get; }

			public ContainerInfo Info { get; }

			public float Gain { get; }

			public ContainerOption(Placement placement, ContainerInfo info, float gain)
			{
				Placement = placement;
				Info = info;
				Gain = gain;
			}
		}

		private readonly Body body;

		private readonly bool includeHandsAndMouth;

		private readonly List<Placement> placements = new List<Placement>();

		private readonly Placement[] slotOwner;

		public List<Item> AllItems { get; } = new List<Item>();

		private List<ContainerInfo> Containers { get; } = new List<ContainerInfo>();

		public InventoryModel(Body body, bool includeHandsAndMouth)
		{
			this.body = body;
			this.includeHandsAndMouth = includeHandsAndMouth;
			slotOwner = new Placement[body.slots.Length];
			HashSet<Item> hashSet = new HashSet<Item>();
			foreach (Item item in body.GetAllItemsThorough())
			{
				if ((Object)(object)item != (Object)null && hashSet.Add(item))
				{
					AllItems.Add(item);
				}
			}
			Container val = default(Container);
			foreach (Item allItem in AllItems)
			{
				Container component = ((Component)allItem).GetComponent<Container>();
				if (!((Object)(object)component == (Object)null) && !allItem.TryGetParentContainer(ref val))
				{
					Containers.Add(new ContainerInfo(component, Containers.Count));
				}
			}
			HashSet<Item> hashSet2 = new HashSet<Item>(from item in body.GetAllWearables()
				where (Object)(object)item != (Object)null
				select item);
			Container val2 = default(Container);
			foreach (Item allItem2 in AllItems)
			{
				allItem2.TryGetParentContainer(ref val2);
				int slotIndex = GetSlotIndex(body, allItem2);
				if (!((Object)(object)((Component)allItem2).GetComponent<Container>() != (Object)null) && !hashSet2.Contains(allItem2) && !allItem2.favourited && (!((Object)(object)val2 != (Object)null) || InfoFor(val2) != null) && (slotIndex < 0 || body.slots[slotIndex].canPickUp))
				{
					Placement placement = new Placement
					{
						Item = allItem2,
						SourceContainer = val2,
						SourceSlot = slotIndex,
						TargetContainer = val2,
						TargetSlot = slotIndex
					};
					placements.Add(placement);
					if (slotIndex >= 0)
					{
						slotOwner[slotIndex] = placement;
					}
				}
			}
		}

		public List<Container> TrackedContainers()
		{
			return Containers.Select((ContainerInfo info) => info.Container).ToList();
		}

		public void BuildPlan()
		{
			if (PrefersItemCapacity())
			{
				PlanForItemCapacity();
			}
			else
			{
				PlanForWeightReduction();
			}
		}

		public int Execute()
		{
			List<Placement> list = placements.Where((Placement placement) => (Object)(object)placement.TargetContainer != (Object)(object)placement.SourceContainer || placement.TargetSlot != placement.SourceSlot).ToList();
			if (list.Count == 0)
			{
				return 0;
			}
			List<Placement> list2 = new List<Placement>();
			int num = 0;
			try
			{
				foreach (Placement item in list)
				{
					if (Detach(item))
					{
						list2.Add(item);
					}
				}
				bool flag = true;
				while (list2.Count > 0 && flag)
				{
					flag = false;
					int num2 = 0;
					while (num2 < list2.Count)
					{
						if (PlaceAtTarget(list2[num2]))
						{
							num++;
							list2.RemoveAt(num2);
							flag = true;
						}
						else
						{
							num2++;
						}
					}
				}
			}
			catch (Exception ex)
			{
				ManualLogSource logger = Logger;
				if (logger != null)
				{
					logger.LogWarning((object)("Inventory sort stopped early: " + ex.Message));
				}
			}
			finally
			{
				foreach (Placement item2 in list2)
				{
					ReturnToSource(item2);
				}
			}
			return num;
		}

		private bool PrefersItemCapacity()
		{
			int num = 6 + Containers.Count * 3;
			return AllItems.Count < num && body.GetTotalEncumberance() < 10f;
		}

		private void PlanForItemCapacity()
		{
			foreach (Placement item in from item in placements
				where (Object)(object)item.SourceContainer != (Object)null || !IsSlotBlocked(item.SourceSlot)
				orderby item.Weight
				select item)
			{
				ContainerInfo containerInfo = FindContainerHome(item);
				if (containerInfo != null)
				{
					Assign(item, containerInfo, -1);
				}
			}
		}

		private void PlanForWeightReduction()
		{
			List<Placement> list = placements.Where((Placement placement2) => (Object)(object)placement2.SourceContainer != (Object)null || !IsSlotBlocked(placement2.SourceSlot)).ToList();
			foreach (Placement item in list)
			{
				ContainerInfo containerInfo = InfoFor(item.SourceContainer);
				if (containerInfo != null && containerInfo.IsSpecialized)
				{
					item.Assigned = true;
				}
			}
			for (int num = 0; num < 2; num++)
			{
				bool wantSpecialized = num == 0;
				List<ContainerOption> list2 = new List<ContainerOption>();
				foreach (Placement placement in list.Where((Placement item) => !item.Assigned))
				{
					ContainerInfo containerInfo2 = InfoFor(placement.SourceContainer);
					foreach (ContainerInfo item2 in Containers.Where((ContainerInfo candidate) => candidate.IsSpecialized == wantSpecialized && candidate.CanFit(placement.Item)))
					{
						float num2 = placement.Weight * (CurrentFactor(placement) - item2.Mult);
						bool flag = wantSpecialized && containerInfo2 != null && !containerInfo2.IsSpecialized;
						if (num2 > 0.0001f || flag)
						{
							list2.Add(new ContainerOption(placement, item2, num2));
						}
					}
				}
				foreach (ContainerOption item3 in from candidate in list2
					orderby candidate.Gain descending, candidate.Placement.Weight descending, candidate.Info.Mult, candidate.Info.Index
					select candidate)
				{
					if (!item3.Placement.Assigned && item3.Info.CanFit(item3.Placement.Item))
					{
						Assign(item3.Placement, item3.Info, -1);
					}
				}
			}
		}

		private float CurrentFactor(Placement placement)
		{
			return InfoFor(placement.SourceContainer)?.Mult ?? 1f;
		}

		private ContainerInfo FindContainerHome(Placement placement)
		{
			if ((Object)(object)placement.SourceContainer != (Object)null)
			{
				ContainerInfo containerInfo = InfoFor(placement.SourceContainer);
				if (containerInfo != null)
				{
					return containerInfo;
				}
			}
			return (from info in Containers
				where info.CanFit(placement.Item)
				orderby info.RemainingWeight - placement.Item.totalWeight descending, info.Index
				select info).FirstOrDefault();
		}

		private void Assign(Placement placement, ContainerInfo target, int targetSlot)
		{
			if ((target == null && targetSlot == placement.SourceSlot) || (target != null && (Object)(object)target.Container == (Object)(object)placement.SourceContainer))
			{
				placement.TargetContainer = placement.SourceContainer;
				placement.TargetSlot = placement.SourceSlot;
				placement.Assigned = true;
				return;
			}
			InfoFor(placement.SourceContainer)?.Release(placement.Item);
			if (placement.SourceSlot >= 0 && slotOwner[placement.SourceSlot] == placement)
			{
				slotOwner[placement.SourceSlot] = null;
			}
			target?.Reserve(placement.Item);
			placement.TargetContainer = target?.Container;
			placement.TargetSlot = targetSlot;
			placement.Assigned = true;
			if (targetSlot >= 0)
			{
				slotOwner[targetSlot] = placement;
			}
		}

		private bool IsSlotBlocked(int slotIndex)
		{
			return !includeHandsAndMouth && IsHandOrHeadSlot(slotIndex);
		}

		private bool IsHandOrHeadSlot(int slotIndex)
		{
			if (slotIndex < 0 || slotIndex >= body.slots.Length)
			{
				return false;
			}
			InventorySlot val = body.slots[slotIndex];
			return (Object)(object)val != (Object)null && (val.isHand || IsHeadSlot(slotIndex));
		}

		private bool IsHeadSlot(int slotIndex)
		{
			if (slotIndex < 0 || slotIndex >= body.slots.Length)
			{
				return false;
			}
			InventorySlot val = body.slots[slotIndex];
			return (Object)(object)val != (Object)null && (Object)(object)val.limb != (Object)null && val.limb.isHead;
		}

		private ContainerInfo InfoFor(Container container)
		{
			if ((Object)(object)container == (Object)null)
			{
				return null;
			}
			return Containers.FirstOrDefault((ContainerInfo info) => (Object)(object)info.Container == (Object)(object)container);
		}

		private bool Detach(Placement placement)
		{
			Item item = placement.Item;
			if ((Object)(object)item == (Object)null)
			{
				return false;
			}
			Container val = default(Container);
			if (item.TryGetParentContainer(ref val))
			{
				val.UnloadItem(item, (Body)null);
			}
			else if (body.HoldingItem(item))
			{
				body.DropItem(item);
			}
			else if ((Object)(object)((Component)item).transform.parent != (Object)null)
			{
				((Component)item).transform.SetParent((Transform)null);
			}
			if ((Object)(object)((Component)item).transform.parent != (Object)null)
			{
				return false;
			}
			PrepareDetachedItem(item);
			return true;
		}

		private bool PlaceAtTarget(Placement placement)
		{
			//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
			Item item = placement.Item;
			if (placement.TargetSlot >= 0)
			{
				if (!CanUseSlot(body, placement.TargetSlot, item) || body.HoldingItem(placement.TargetSlot))
				{
					return false;
				}
				body.PickUpItem(item, placement.TargetSlot, true);
				return (Object)(object)body.GetItem(placement.TargetSlot) == (Object)(object)item;
			}
			Container targetContainer = placement.TargetContainer;
			if ((Object)(object)targetContainer == (Object)null || !targetContainer.CanHoldItem(item))
			{
				return false;
			}
			((Component)item).transform.position = ((Component)targetContainer).transform.position;
			targetContainer.LoadItem(item);
			Container val = default(Container);
			return item.TryGetParentContainer(ref val) && (Object)(object)val == (Object)(object)targetContainer;
		}

		private void ReturnToSource(Placement placement)
		{
			//IL_0116: Unknown result type (might be due to invalid IL or missing references)
			Item item = placement.Item;
			if ((Object)(object)item == (Object)null || (Object)(object)((Component)item).transform.parent != (Object)null || TryPlaceInContainer(item, placement.SourceContainer) || TryPlaceInSlot(item, placement.SourceSlot))
			{
				return;
			}
			foreach (ContainerInfo container in Containers)
			{
				if (TryPlaceInContainer(item, container.Container))
				{
					return;
				}
			}
			for (int i = 0; i < body.slots.Length; i++)
			{
				if (TryPlaceInSlot(item, i))
				{
					return;
				}
			}
			ManualLogSource logger = Logger;
			if (logger != null)
			{
				logger.LogWarning((object)("Inventory sort found no legal home for " + item.id + ", it was left at the player's feet."));
			}
			((Component)item).transform.position = ((Component)body).transform.position;
			if ((Object)(object)item.rb != (Object)null)
			{
				item.rb.simulated = true;
			}
		}

		private bool TryPlaceInContainer(Item item, Container container)
		{
			//IL_0034: Unknown result type (might be due to invalid IL or missing references)
			if ((Object)(object)container == (Object)null || !container.CanHoldItem(item))
			{
				return false;
			}
			PrepareDetachedItem(item);
			((Component)item).transform.position = ((Component)container).transform.position;
			container.LoadItem(item);
			Container val = default(Container);
			return item.TryGetParentContainer(ref val) && (Object)(object)val == (Object)(object)container;
		}

		private bool TryPlaceInSlot(Item item, int slotIndex)
		{
			if (!CanUseSlot(body, slotIndex, item) || body.HoldingItem(slotIndex))
			{
				return false;
			}
			PrepareDetachedItem(item);
			body.PickUpItem(item, slotIndex, true);
			return (Object)(object)body.GetItem(slotIndex) == (Object)(object)item;
		}

		private void PrepareDetachedItem(Item item)
		{
			//IL_0031: Unknown result type (might be due to invalid IL or missing references)
			if ((Object)(object)item.rb != (Object)null)
			{
				item.rb.simulated = false;
			}
			((Component)item).transform.position = ((Component)body).transform.position;
		}

		private static int GetSlotIndex(Body body, Item item)
		{
			for (int i = 0; i < body.slots.Length; i++)
			{
				if ((Object)(object)body.GetItem(i) == (Object)(object)item)
				{
					return i;
				}
			}
			return -1;
		}

		private static bool CanUseSlot(Body body, int slotIndex, Item item)
		{
			if ((Object)(object)body == (Object)null || (Object)(object)item == (Object)null || slotIndex < 0 || slotIndex >= body.slots.Length)
			{
				return false;
			}
			InventorySlot val = body.slots[slotIndex];
			return (Object)(object)val != (Object)null && val.canPickUp && (!item.Stats.onlyHoldInHands || val.isHand);
		}
	}

	private const int EarlyGameItemBudget = 6;

	private const int ItemBudgetPerContainer = 3;

	private const float EarlyGameEncumberanceCeiling = 10f;

	private const float WeightEpsilon = 0.0001f;

	private static ManualLogSource Logger;

	public static void Initialize(ManualLogSource logger)
	{
		Logger = logger;
	}

	public static void RunSort(bool includeHandsAndMouth = false)
	{
		PlayerCamera main = PlayerCamera.main;
		Body val = (((Object)(object)main != (Object)null) ? main.body : null);
		if ((Object)(object)val == (Object)null || val.slots == null)
		{
			return;
		}
		InventoryModel inventoryModel = new InventoryModel(val, includeHandsAndMouth);
		if (MultiplayerCompat.IsActive && inventoryModel.AllItems.Any((Item item) => (Object)(object)item != (Object)null && !MultiplayerCompat.CanMoveItemNow(item)))
		{
			main.PlayUISound((UISoundType)6, 1f);
			return;
		}
		inventoryModel.BuildPlan();
		int num;
		using (MultiplayerCompat.BeginInventoryMutation(val))
		{
			num = inventoryModel.Execute();
		}
		if (num == 0)
		{
			main.PlayUISound((UISoundType)6, 1f);
			return;
		}
		foreach (Item allItem in inventoryModel.AllItems)
		{
			MultiplayerCompat.MarkItemMoved(allItem, containerChanged: true);
		}
		ReorderContainerContentsByCraftingPriority(inventoryModel.TrackedContainers());
		RefreshInventoryUi(main);
	}

	private static void RefreshInventoryUi(PlayerCamera camera)
	{
		if (!((Object)(object)camera == (Object)null))
		{
			camera.UpdateWearables();
			if (camera.radialOpen)
			{
				camera.RepopulateContainer();
			}
		}
	}

	private static void ReorderContainerContentsByCraftingPriority(List<Container> containers)
	{
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Expected O, but got Unknown
		foreach (Container container in containers)
		{
			if ((Object)(object)container == (Object)null || ((Component)container).transform.childCount < 2)
			{
				continue;
			}
			List<Item> currentOrder = new List<Item>();
			foreach (Transform item in ((Component)container).transform)
			{
				Transform val = item;
				Item component = ((Component)val).GetComponent<Item>();
				if ((Object)(object)component != (Object)null)
				{
					currentOrder.Add(component);
				}
			}
			List<Item> list = (from item in currentOrder
				where !item.favourited
				orderby GetCraftingQualityScore(item, "cutting") descending, GetCraftingQualityScore(item, "hammering") descending, item.totalWeight descending, currentOrder.IndexOf(item)
				select item).ToList();
			if (list.Count < 2)
			{
				continue;
			}
			List<Item> list2 = new List<Item>(currentOrder);
			int num = 0;
			for (int num2 = 0; num2 < list2.Count; num2++)
			{
				if (!list2[num2].favourited)
				{
					list2[num2] = list[num++];
				}
			}
			ApplySiblingOrder(container, list2);
		}
	}

	private static void ApplySiblingOrder(Container container, IList<Item> desiredOrder)
	{
		if ((Object)(object)container == (Object)null || desiredOrder == null)
		{
			return;
		}
		for (int i = 0; i < desiredOrder.Count; i++)
		{
			Item val = desiredOrder[i];
			if ((Object)(object)val != (Object)null && (Object)(object)((Component)val).transform.parent == (Object)(object)((Component)container).transform)
			{
				((Component)val).transform.SetSiblingIndex(i);
			}
		}
	}

	private static float GetCraftingQualityScore(Item item, string qualityId)
	{
		if ((Object)(object)item == (Object)null || item.Stats == null || item.Stats.qualities == null || string.IsNullOrEmpty(qualityId))
		{
			return 0f;
		}
		foreach (CraftingQuality quality in item.Stats.qualities)
		{
			if (quality != null)
			{
				string a = TryReadStringMember(quality, "id");
				if (string.Equals(a, qualityId, StringComparison.OrdinalIgnoreCase))
				{
					return (TryReadFloatMember(quality, "amount") ?? TryReadFloatMember(quality, "value") ?? TryReadFloatMember(quality, "num") ?? TryReadFloatMember(quality, "quality")).GetValueOrDefault();
				}
			}
		}
		return 0f;
	}

	private static string TryReadStringMember(object instance, string memberName)
	{
		Type type = instance.GetType();
		FieldInfo fieldInfo = AccessTools.Field(type, memberName);
		if (fieldInfo != null && fieldInfo.FieldType == typeof(string))
		{
			return fieldInfo.GetValue(instance) as string;
		}
		PropertyInfo propertyInfo = AccessTools.Property(type, memberName);
		if (propertyInfo != null && propertyInfo.PropertyType == typeof(string))
		{
			return propertyInfo.GetValue(instance) as string;
		}
		return null;
	}

	private static float? TryReadFloatMember(object instance, string memberName)
	{
		Type type = instance.GetType();
		FieldInfo fieldInfo = AccessTools.Field(type, memberName);
		if (fieldInfo != null)
		{
			return ConvertToFloat(fieldInfo.GetValue(instance));
		}
		PropertyInfo propertyInfo = AccessTools.Property(type, memberName);
		if (propertyInfo != null)
		{
			return ConvertToFloat(propertyInfo.GetValue(instance));
		}
		return null;
	}

	private static float? ConvertToFloat(object value)
	{
		if (value == null)
		{
			return null;
		}
		if (value is float value2)
		{
			return value2;
		}
		if (value is int num)
		{
			return num;
		}
		if (value is double num2)
		{
			return (float)num2;
		}
		return null;
	}
}
