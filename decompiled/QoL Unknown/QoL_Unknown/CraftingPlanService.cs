using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace QoL_Unknown;

internal static class CraftingPlanService
{
	internal sealed class CraftingPlanOperations
	{
		public readonly List<CraftingCombineOperation> CombineOperations = new List<CraftingCombineOperation>();

		public readonly List<CraftingPlanOperation> IntermediateOperations = new List<CraftingPlanOperation>();
	}

	internal sealed class CraftingInventoryContext
	{
		private readonly InventorySimulation snapshot;

		internal InventorySimulation MutableSimulation => snapshot;

		internal CraftingInventoryContext(InventorySimulation snapshot)
		{
			this.snapshot = snapshot;
		}

		internal InventorySimulation CreateSimulation()
		{
			return snapshot?.Clone();
		}
	}

	internal sealed class InventorySimulation
	{
		private readonly List<SimulatedItem> items = new List<SimulatedItem>();

		public static InventorySimulation Capture(Body body)
		{
			//IL_005e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0063: Unknown result type (might be due to invalid IL or missing references)
			InventorySimulation inventorySimulation = new InventorySimulation();
			if ((Object)(object)body == (Object)null)
			{
				return inventorySimulation;
			}
			foreach (Item item in body.GetAllItemsThorough())
			{
				inventorySimulation.AddItem(item);
			}
			Collider2D[] array = Physics2D.OverlapCircleAll(Vector2.op_Implicit(((Component)body).transform.position), 10f, LayerMask.GetMask(new string[1] { "Item" }));
			Collider2D[] array2 = array;
			Item val2 = default(Item);
			foreach (Collider2D val in array2)
			{
				if ((Object)(object)val != (Object)null && ((Component)val).TryGetComponent<Item>(ref val2) && body.DoPickupCheck(val2, true))
				{
					inventorySimulation.AddItem(val2);
				}
			}
			return inventorySimulation;
		}

		public InventorySimulation Clone()
		{
			InventorySimulation inventorySimulation = new InventorySimulation();
			foreach (SimulatedItem item in items)
			{
				inventorySimulation.items.Add(item.Clone());
			}
			return inventorySimulation;
		}

		internal SimulatedItem FindAndUse(RecipeItem ingredient)
		{
			if (ingredient == null)
			{
				return null;
			}
			SimulatedItem simulatedItem = FindMatchingItem(ingredient);
			if (simulatedItem == null)
			{
				return null;
			}
			UseItem(ingredient, simulatedItem);
			return simulatedItem;
		}

		public bool TryCombineForIngredient(RecipeItem ingredient, CraftingPlanOperations operations)
		{
			if (ingredient == null || ingredient.isLiquid)
			{
				return false;
			}
			SimulatedItem simulatedItem = FindBestCombineTarget(ingredient);
			if (simulatedItem == null)
			{
				return false;
			}
			foreach (SimulatedItem item in items)
			{
				if (item == simulatedItem || item.Destroyed || item.Favourited || item.ContainerItemCount > 0 || (Object)(object)item.Actual == (Object)null || !CanCombineSolids(simulatedItem, item))
				{
					continue;
				}
				float num = 1f - simulatedItem.Condition;
				if (num <= 0f)
				{
					break;
				}
				float num2 = Mathf.Min(num, item.Condition);
				if (!(num2 <= 0f))
				{
					simulatedItem.Condition += num2;
					item.Condition -= num2;
					operations?.CombineOperations.Add(new CraftingCombineOperation
					{
						Target = simulatedItem.Actual,
						Source = item.Actual
					});
					if (IngredientMatches(ingredient, simulatedItem))
					{
						return true;
					}
				}
			}
			return IngredientMatches(ingredient, simulatedItem);
		}

		public void AddRecipeResult(Recipe recipe)
		{
			if (recipe?.result == null)
			{
				return;
			}
			if (recipe.result.isLiquid)
			{
				items.Add(SimulatedItem.FromLiquidResult(recipe.result.id, recipe.result.resultCondition));
				return;
			}
			int num = Mathf.Max(1, recipe.result.amount);
			for (int i = 0; i < num; i++)
			{
				items.Add(SimulatedItem.FromResult(recipe.result.id, recipe.result.resultCondition));
			}
		}

		private void AddItem(Item item)
		{
			if (!((Object)(object)item == (Object)null) && !items.Any((SimulatedItem existing) => (Object)(object)existing.Actual == (Object)(object)item))
			{
				items.Add(SimulatedItem.FromItem(item));
			}
		}

		private SimulatedItem FindMatchingItem(RecipeItem ingredient)
		{
			bool flag = ShouldPrioritizeBestToolQuality(ingredient);
			SimulatedItem result = null;
			float num = float.MinValue;
			float num2 = float.MinValue;
			foreach (SimulatedItem item in items)
			{
				if (item.Destroyed || item.Favourited)
				{
					continue;
				}
				if (ingredient.isLiquid)
				{
					if (MatchesLiquid(ingredient, item))
					{
						return item;
					}
				}
				else
				{
					if (item.ContainerItemCount > 0)
					{
						continue;
					}
					if (ingredient.specific)
					{
						if (string.Equals(item.Id, ingredient.specificId, StringComparison.OrdinalIgnoreCase) && item.Condition >= ingredient.minimumCondition)
						{
							return item;
						}
					}
					else if (item.Condition >= ingredient.minimumCondition && GetQualityThatMeetsCriteria(ingredient.quality, item.Qualities) != null)
					{
						if (!flag)
						{
							return item;
						}
						float matchedQualityAmount = GetMatchedQualityAmount(ingredient, item);
						if (matchedQualityAmount > num || (Mathf.Approximately(matchedQualityAmount, num) && item.Condition > num2))
						{
							result = item;
							num = matchedQualityAmount;
							num2 = item.Condition;
						}
					}
				}
			}
			return result;
		}

		private SimulatedItem FindBestCombineTarget(RecipeItem ingredient)
		{
			SimulatedItem simulatedItem = null;
			foreach (SimulatedItem item in items)
			{
				if (!item.Destroyed && !item.Favourited && item.ContainerItemCount <= 0 && !((Object)(object)item.Actual == (Object)null) && CanUseSolidAsCombineTarget(ingredient, item) && (simulatedItem == null || item.Condition > simulatedItem.Condition))
				{
					simulatedItem = item;
				}
			}
			return simulatedItem;
		}

		private static bool CanUseSolidAsCombineTarget(RecipeItem ingredient, SimulatedItem item)
		{
			if (ingredient == null || item == null || ingredient.isLiquid || item.IsLiquidContainer || item.Condition <= 0f)
			{
				return false;
			}
			if (ingredient.specific)
			{
				return string.Equals(item.Id, ingredient.specificId, StringComparison.OrdinalIgnoreCase) && item.Condition < ingredient.minimumCondition;
			}
			return item.Condition < ingredient.minimumCondition && GetQualityThatMeetsCriteria(ingredient.quality, item.Qualities) != null;
		}

		private static bool CanCombineSolids(SimulatedItem target, SimulatedItem source)
		{
			if (target == null || source == null || (Object)(object)target.Actual == (Object)null || (Object)(object)source.Actual == (Object)null || target.IsLiquidContainer || source.IsLiquidContainer || target.Condition <= 0f || source.Condition <= 0f || !string.Equals(target.Id, source.Id, StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}
			return target.Actual.Stats.combineable && source.Actual.Stats.combineable;
		}

		private static bool IngredientMatches(RecipeItem ingredient, SimulatedItem item)
		{
			if (ingredient == null || item == null || item.Destroyed || item.Favourited)
			{
				return false;
			}
			if (ingredient.isLiquid)
			{
				return MatchesLiquid(ingredient, item);
			}
			if (item.ContainerItemCount > 0)
			{
				return false;
			}
			if (ingredient.specific)
			{
				return string.Equals(item.Id, ingredient.specificId, StringComparison.OrdinalIgnoreCase) && item.Condition >= ingredient.minimumCondition;
			}
			return item.Condition >= ingredient.minimumCondition && GetQualityThatMeetsCriteria(ingredient.quality, item.Qualities) != null;
		}

		private static bool MatchesLiquid(RecipeItem ingredient, SimulatedItem item)
		{
			if (!item.IsLiquidContainer)
			{
				return false;
			}
			if (ingredient.specific)
			{
				return item.AmountOfLiquid(ingredient.specificId) >= ingredient.minimumCondition;
			}
			foreach (SimulatedLiquid liquid in item.Liquids)
			{
				if (!Liquids.Registry.TryGetValue(liquid.Id, out var value) || GetQualityThatMeetsCriteria(ingredient.quality, value.GetScaledQualities(liquid.Amount)) == null)
				{
					continue;
				}
				return true;
			}
			return false;
		}

		private static CraftingQuality GetQualityThatMeetsCriteria(CraftingQuality target, List<CraftingQuality> qualities)
		{
			if (target == null || qualities == null)
			{
				return null;
			}
			return qualities.FirstOrDefault((CraftingQuality quality) => quality.id == target.id && quality.amount >= target.amount);
		}

		private static bool ShouldPrioritizeBestToolQuality(RecipeItem ingredient)
		{
			string text = ingredient?.quality?.id;
			return text == "hammering" || text == "cutting";
		}

		private static float GetMatchedQualityAmount(RecipeItem ingredient, SimulatedItem item)
		{
			return GetQualityThatMeetsCriteria(ingredient?.quality, item?.Qualities)?.amount ?? float.MinValue;
		}

		private static void UseItem(RecipeItem ingredient, SimulatedItem item)
		{
			if (ingredient.isLiquid)
			{
				UseLiquid(ingredient, item);
				return;
			}
			if (ingredient.specific)
			{
				item.Condition -= ingredient.minimumCondition;
			}
			else
			{
				CraftingQuality qualityThatMeetsCriteria = GetQualityThatMeetsCriteria(ingredient.quality, item.Qualities);
				if (qualityThatMeetsCriteria != null && qualityThatMeetsCriteria.amount > 0f)
				{
					item.Condition -= ingredient.quality.amount / qualityThatMeetsCriteria.amount;
				}
			}
			if (ingredient.destroyItem)
			{
				item.Destroyed = true;
			}
		}

		private static void UseLiquid(RecipeItem ingredient, SimulatedItem item)
		{
			if (ingredient.specific)
			{
				item.DrainLiquid(ingredient.specificId, ingredient.minimumCondition);
				return;
			}
			foreach (SimulatedLiquid liquid in item.Liquids)
			{
				if (Liquids.Registry.TryGetValue(liquid.Id, out var value))
				{
					CraftingQuality qualityThatMeetsCriteria = GetQualityThatMeetsCriteria(ingredient.quality, value.GetScaledQualities(liquid.Amount));
					if (qualityThatMeetsCriteria != null && !(qualityThatMeetsCriteria.amount <= 0f))
					{
						float amount = liquid.Amount * (ingredient.quality.amount / qualityThatMeetsCriteria.amount);
						item.DrainLiquid(liquid.Id, amount);
						break;
					}
				}
			}
		}
	}

	internal sealed class SimulatedItem
	{
		public Item Actual;

		public string Id;

		public float Condition;

		public bool Destroyed;

		public bool Favourited;

		public int ContainerItemCount;

		public bool IsLiquidContainer;

		public readonly List<CraftingQuality> Qualities = new List<CraftingQuality>();

		public readonly List<SimulatedLiquid> Liquids = new List<SimulatedLiquid>();

		public static SimulatedItem FromItem(Item item)
		{
			//IL_007f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0089: Expected O, but got Unknown
			SimulatedItem simulatedItem = new SimulatedItem
			{
				Actual = item,
				Id = item.id,
				Condition = item.condition,
				Favourited = item.favourited
			};
			if (item.Stats?.qualities != null)
			{
				foreach (CraftingQuality quality in item.Stats.qualities)
				{
					simulatedItem.Qualities.Add(new CraftingQuality(quality.id, quality.amount));
				}
			}
			Container val = default(Container);
			if (((Component)item).TryGetComponent<Container>(ref val))
			{
				simulatedItem.ContainerItemCount = val.itemCount;
			}
			WaterContainerItem val2 = default(WaterContainerItem);
			if (((Component)item).TryGetComponent<WaterContainerItem>(ref val2))
			{
				simulatedItem.IsLiquidContainer = true;
				foreach (LiquidStack item2 in val2.stack)
				{
					simulatedItem.Liquids.Add(new SimulatedLiquid(item2.liquidId, item2.amount));
				}
			}
			return simulatedItem;
		}

		public static SimulatedItem FromResult(string id, float condition)
		{
			//IL_0062: Unknown result type (might be due to invalid IL or missing references)
			//IL_006c: Expected O, but got Unknown
			SimulatedItem simulatedItem = new SimulatedItem
			{
				Id = id,
				Condition = condition
			};
			if (Item.GlobalItems.TryGetValue(id, out var value) && value.qualities != null)
			{
				foreach (CraftingQuality quality in value.qualities)
				{
					simulatedItem.Qualities.Add(new CraftingQuality(quality.id, quality.amount));
				}
			}
			return simulatedItem;
		}

		public static SimulatedItem FromLiquidResult(string id, float amount)
		{
			SimulatedItem simulatedItem = new SimulatedItem
			{
				Id = id,
				Condition = 1f,
				IsLiquidContainer = true
			};
			if (amount > 0f)
			{
				simulatedItem.Liquids.Add(new SimulatedLiquid(id, amount));
			}
			return simulatedItem;
		}

		public SimulatedItem Clone()
		{
			//IL_0085: Unknown result type (might be due to invalid IL or missing references)
			//IL_008f: Expected O, but got Unknown
			SimulatedItem simulatedItem = new SimulatedItem
			{
				Actual = Actual,
				Id = Id,
				Condition = Condition,
				Destroyed = Destroyed,
				Favourited = Favourited,
				ContainerItemCount = ContainerItemCount,
				IsLiquidContainer = IsLiquidContainer
			};
			foreach (CraftingQuality quality in Qualities)
			{
				simulatedItem.Qualities.Add(new CraftingQuality(quality.id, quality.amount));
			}
			foreach (SimulatedLiquid liquid in Liquids)
			{
				simulatedItem.Liquids.Add(new SimulatedLiquid(liquid.Id, liquid.Amount));
			}
			return simulatedItem;
		}

		public float AmountOfLiquid(string id)
		{
			return Liquids.FirstOrDefault((SimulatedLiquid stack) => string.Equals(stack.Id, id, StringComparison.OrdinalIgnoreCase))?.Amount ?? 0f;
		}

		public void DrainLiquid(string id, float amount)
		{
			if (amount <= 0f)
			{
				return;
			}
			SimulatedLiquid simulatedLiquid = Liquids.FirstOrDefault((SimulatedLiquid stack) => string.Equals(stack.Id, id, StringComparison.OrdinalIgnoreCase));
			if (simulatedLiquid != null)
			{
				simulatedLiquid.Amount -= Mathf.Min(simulatedLiquid.Amount, amount);
				Liquids.RemoveAll((SimulatedLiquid stack) => stack.Amount < 0.5f);
				UpdateLiquidCondition();
			}
		}

		private void UpdateLiquidCondition()
		{
			if (!IsLiquidContainer || (Object)(object)Actual == (Object)null)
			{
				return;
			}
			ItemInfo stats = Actual.Stats;
			LiquidItemInfo val = (LiquidItemInfo)(object)((stats is LiquidItemInfo) ? stats : null);
			if (val != null && !(val.capacity <= 0f))
			{
				Condition = Liquids.Sum((SimulatedLiquid stack) => stack.Amount) / val.capacity;
			}
		}
	}

	internal sealed class SimulatedLiquid
	{
		public readonly string Id;

		public float Amount;

		public SimulatedLiquid(string id, float amount)
		{
			Id = id;
			Amount = amount;
		}
	}

	private const int MaxCraftingAmount = 99;

	private const int MaxRecursionDepth = 8;

	private static readonly Dictionary<string, Recipe> RecipeByResultId = new Dictionary<string, Recipe>(StringComparer.OrdinalIgnoreCase);

	private static int recipeIndexCacheCount = -1;

	private static readonly HashSet<string> AutoCraftableResults = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
	{
		"woodscraps", "woodcube", "flexiglass", "casing", "flammablepowder", "woodpanel", "canvas", "rope", "stick", "string",
		"scrapcube", "scrappanel", "scraptube", "nails", "processedcopper", "bundleofwires", "titaniumslab", "titaniumsheet", "titaniumrod", "plasticchunk",
		"biochem", "opium", "fat", "bundleofwires"
	};

	public static CraftingPlan BuildPlan(Recipe recipe, int requestedAmount)
	{
		return BuildPlan(recipe, requestedAmount, PlayerCamera.main?.body);
	}

	public static CraftingPlan BuildPlan(Recipe recipe, int requestedAmount, Body crafter)
	{
		return BuildPlan(recipe, requestedAmount, CaptureContext(crafter));
	}

	public static CraftingPlan BuildPlan(Recipe recipe, int requestedAmount, CraftingInventoryContext context)
	{
		CraftingPlan craftingPlan = new CraftingPlan
		{
			RequestedAmount = Mathf.Max(1, requestedAmount)
		};
		if (recipe == null || context == null)
		{
			return craftingPlan;
		}
		InventorySimulation simulation = context.CreateSimulation();
		int num = 0;
		for (int i = 0; i < 99; i++)
		{
			if (!TryPlanSingleCraft(recipe, simulation, new HashSet<string>(StringComparer.OrdinalIgnoreCase), 0, null))
			{
				break;
			}
			num++;
		}
		craftingPlan.MaxCraftable = num;
		if (num <= 0)
		{
			return craftingPlan;
		}
		int num2 = (craftingPlan.RequestedAmount = Mathf.Clamp(requestedAmount, 1, num));
		InventorySimulation simulation2 = context.CreateSimulation();
		CraftingPlanOperations craftingPlanOperations = new CraftingPlanOperations();
		for (int j = 0; j < num2; j++)
		{
			if (!TryPlanSingleCraft(recipe, simulation2, new HashSet<string>(StringComparer.OrdinalIgnoreCase), 0, craftingPlanOperations))
			{
				return craftingPlan;
			}
		}
		craftingPlan.CanCraft = true;
		craftingPlan.CombineOperations.AddRange(craftingPlanOperations.CombineOperations);
		craftingPlan.IntermediateOperations.AddRange(craftingPlanOperations.IntermediateOperations);
		return craftingPlan;
	}

	public static CraftingPlan BuildExecutionPlan(Recipe recipe, int requestedAmount)
	{
		return BuildExecutionPlan(recipe, requestedAmount, PlayerCamera.main?.body);
	}

	public static CraftingPlan BuildExecutionPlan(Recipe recipe, int requestedAmount, Body crafter)
	{
		return BuildExecutionPlan(recipe, requestedAmount, CaptureContext(crafter));
	}

	public static CraftingPlan BuildExecutionPlan(Recipe recipe, int requestedAmount, CraftingInventoryContext context)
	{
		CraftingPlan craftingPlan = new CraftingPlan
		{
			RequestedAmount = Mathf.Clamp(requestedAmount, 1, 99)
		};
		if (recipe == null || context == null)
		{
			return craftingPlan;
		}
		InventorySimulation simulation = context.CreateSimulation();
		CraftingPlanOperations craftingPlanOperations = new CraftingPlanOperations();
		for (int i = 0; i < craftingPlan.RequestedAmount; i++)
		{
			if (!TryPlanSingleCraft(recipe, simulation, new HashSet<string>(StringComparer.OrdinalIgnoreCase), 0, craftingPlanOperations))
			{
				return craftingPlan;
			}
		}
		craftingPlan.CanCraft = true;
		craftingPlan.MaxCraftable = craftingPlan.RequestedAmount;
		craftingPlan.CombineOperations.AddRange(craftingPlanOperations.CombineOperations);
		craftingPlan.IntermediateOperations.AddRange(craftingPlanOperations.IntermediateOperations);
		return craftingPlan;
	}

	public static bool CanCraftRecipe(Recipe recipe, Body crafter = null)
	{
		return CanCraftRecipe(recipe, CaptureContext(crafter ?? PlayerCamera.main?.body));
	}

	public static bool CanCraftRecipe(Recipe recipe, CraftingInventoryContext context)
	{
		if (recipe == null || context == null)
		{
			return false;
		}
		return TryPlanSingleCraft(recipe, context.CreateSimulation(), new HashSet<string>(StringComparer.OrdinalIgnoreCase), 0, null);
	}

	public static bool CanAutoCraftMissingIngredient(RecipeItem missingIngredient, Body crafter = null)
	{
		return CanAutoCraftMissingIngredient(missingIngredient, CaptureContext(crafter ?? PlayerCamera.main?.body));
	}

	public static bool CanAutoCraftMissingIngredient(RecipeItem missingIngredient, CraftingInventoryContext context)
	{
		string autoCraftResultId = GetAutoCraftResultId(missingIngredient);
		if (string.IsNullOrEmpty(autoCraftResultId))
		{
			return false;
		}
		Recipe val = FindRecipeForResult(autoCraftResultId);
		if (val == null)
		{
			return false;
		}
		return CanCraftRecipe(val, context);
	}

	public static string GetAutoCraftKey(RecipeItem missingIngredient)
	{
		return GetAutoCraftResultId(missingIngredient);
	}

	public static CraftingIngredientAvailability TryConsumeIngredientForDisplay(RecipeItem ingredient, CraftingInventoryContext context, HashSet<string> activeChain = null, int depth = 0)
	{
		if (ingredient == null || context == null)
		{
			return CraftingIngredientAvailability.Missing;
		}
		return TryConsumeIngredientForDisplay(ingredient, context.MutableSimulation, activeChain ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase), depth);
	}

	public static CraftingInventoryContext CreateMutableContext(Body body)
	{
		return ((Object)(object)body == (Object)null) ? null : new CraftingInventoryContext(InventorySimulation.Capture(body));
	}

	public static CraftingInventoryContext CaptureContext(Body body)
	{
		return ((Object)(object)body == (Object)null) ? null : new CraftingInventoryContext(InventorySimulation.Capture(body));
	}

	private static CraftingIngredientAvailability TryConsumeIngredientForDisplay(RecipeItem ingredient, InventorySimulation simulation, HashSet<string> activeChain, int depth)
	{
		if (ingredient == null || simulation == null)
		{
			return CraftingIngredientAvailability.Missing;
		}
		SimulatedItem simulatedItem = simulation.FindAndUse(ingredient);
		if (simulatedItem != null)
		{
			return CraftingIngredientAvailability.Available;
		}
		if (simulation.TryCombineForIngredient(ingredient, null))
		{
			simulatedItem = simulation.FindAndUse(ingredient);
			if (simulatedItem != null)
			{
				return CraftingIngredientAvailability.Available;
			}
		}
		string autoCraftResultId = GetAutoCraftResultId(ingredient);
		if (string.IsNullOrEmpty(autoCraftResultId))
		{
			return CraftingIngredientAvailability.Missing;
		}
		Recipe val = FindRecipeForResult(autoCraftResultId);
		if (val == null || activeChain.Contains(autoCraftResultId) || depth > 8)
		{
			return CraftingIngredientAvailability.AutoCraftableButMissingInputs;
		}
		activeChain.Add(autoCraftResultId);
		bool flag = TryPlanSingleCraft(val, simulation, activeChain, depth + 1, null);
		activeChain.Remove(autoCraftResultId);
		if (!flag)
		{
			return CraftingIngredientAvailability.AutoCraftableButMissingInputs;
		}
		simulatedItem = simulation.FindAndUse(ingredient);
		return (simulatedItem != null) ? CraftingIngredientAvailability.AutoCraftable : CraftingIngredientAvailability.AutoCraftableButMissingInputs;
	}

	public static string BuildIntermediateSummary(CraftingPlan plan)
	{
		if (plan == null || !plan.HasIntermediates)
		{
			return string.Empty;
		}
		List<string> list = new List<string>();
		foreach (CraftingPlanOperation item in CompressOperations(plan.IntermediateOperations))
		{
			string text = Locale.GetItem(item.ResultId);
			if (string.IsNullOrEmpty(text))
			{
				text = item.ResultId;
			}
			list.Add($"{text} x{item.Count}");
		}
		return ModLocale.Get("qol.craft.bulk.will_craft", "Will craft: {0}").Replace("{0}", string.Join(", ", list));
	}

	private static IEnumerable<CraftingPlanOperation> CompressOperations(IEnumerable<CraftingPlanOperation> operations)
	{
		Dictionary<Recipe, CraftingPlanOperation> dictionary = new Dictionary<Recipe, CraftingPlanOperation>();
		List<CraftingPlanOperation> list = new List<CraftingPlanOperation>();
		if (operations == null)
		{
			return list;
		}
		foreach (CraftingPlanOperation operation in operations)
		{
			if (operation?.Recipe != null)
			{
				if (dictionary.TryGetValue(operation.Recipe, out var value))
				{
					value.Count += operation.Count;
					continue;
				}
				CraftingPlanOperation craftingPlanOperation = new CraftingPlanOperation
				{
					Recipe = operation.Recipe,
					Count = operation.Count
				};
				dictionary[operation.Recipe] = craftingPlanOperation;
				list.Add(craftingPlanOperation);
			}
		}
		return list;
	}

	private static bool TryPlanSingleCraft(Recipe recipe, InventorySimulation simulation, HashSet<string> activeChain, int depth, CraftingPlanOperations operations)
	{
		if (recipe == null || simulation == null || depth > 8)
		{
			return false;
		}
		foreach (RecipeItem item in recipe.items)
		{
			SimulatedItem simulatedItem = simulation.FindAndUse(item);
			if (simulatedItem != null)
			{
				continue;
			}
			if (simulation.TryCombineForIngredient(item, operations))
			{
				simulatedItem = simulation.FindAndUse(item);
				if (simulatedItem != null)
				{
					continue;
				}
			}
			if (!TryCraftMissingIngredient(item, simulation, activeChain, depth + 1, operations))
			{
				return false;
			}
			simulatedItem = simulation.FindAndUse(item);
			if (simulatedItem != null)
			{
				continue;
			}
			return false;
		}
		simulation.AddRecipeResult(recipe);
		return true;
	}

	private static bool TryCraftMissingIngredient(RecipeItem missingIngredient, InventorySimulation simulation, HashSet<string> activeChain, int depth, CraftingPlanOperations operations)
	{
		string autoCraftResultId = GetAutoCraftResultId(missingIngredient);
		if (string.IsNullOrEmpty(autoCraftResultId) || activeChain.Contains(autoCraftResultId))
		{
			return false;
		}
		Recipe val = FindRecipeForResult(autoCraftResultId);
		if (val == null)
		{
			return false;
		}
		activeChain.Add(autoCraftResultId);
		bool flag = TryPlanSingleCraft(val, simulation, activeChain, depth, operations);
		activeChain.Remove(autoCraftResultId);
		if (!flag)
		{
			return false;
		}
		operations?.IntermediateOperations.Add(new CraftingPlanOperation
		{
			Recipe = val,
			Count = 1
		});
		return true;
	}

	private static string GetAutoCraftResultId(RecipeItem missingIngredient)
	{
		if (missingIngredient == null)
		{
			return null;
		}
		string text = (missingIngredient.specific ? missingIngredient.specificId : missingIngredient.quality?.id);
		if (string.IsNullOrEmpty(text) || !AutoCraftableResults.Contains(text))
		{
			return null;
		}
		return text;
	}

	private static Recipe FindRecipeForResult(string resultId)
	{
		if (Recipes.recipes == null)
		{
			return null;
		}
		RebuildRecipeIndexIfNeeded();
		Recipe value;
		return (RecipeByResultId.TryGetValue(resultId, out value) && value.visible) ? value : null;
	}

	private static void RebuildRecipeIndexIfNeeded()
	{
		int num = Recipes.recipes?.Count ?? 0;
		if (recipeIndexCacheCount == num)
		{
			return;
		}
		RecipeByResultId.Clear();
		recipeIndexCacheCount = num;
		if (Recipes.recipes == null)
		{
			return;
		}
		foreach (Recipe recipe in Recipes.recipes)
		{
			if (recipe?.result != null && !string.IsNullOrEmpty(recipe.result.id) && !RecipeByResultId.ContainsKey(recipe.result.id))
			{
				RecipeByResultId.Add(recipe.result.id, recipe);
			}
		}
	}
}
