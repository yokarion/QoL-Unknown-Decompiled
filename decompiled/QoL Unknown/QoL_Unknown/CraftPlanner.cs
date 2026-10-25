using System;
using System.Collections.Generic;
using UnityEngine;

namespace QoL_Unknown;

internal static class CraftPlanner
{
	public const int MaxBulkAmount = 99;

	private const int MaxChainDepth = 8;

	private const string NoAutoCraftTag = "noautocraft";

	private static readonly Dictionary<string, List<Recipe>> RecipesByResultId = new Dictionary<string, List<Recipe>>(StringComparer.OrdinalIgnoreCase);

	private static readonly HashSet<string> NonAutoCraftableResults = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	private static readonly Dictionary<int, bool> CraftableByRecipe = new Dictionary<int, bool>();

	private static readonly Dictionary<int, int> MaxByRecipe = new Dictionary<int, int>();

	private static int cachedRevision = -1;

	private static int recipeIndexCacheCount = -1;

	public static CraftPlan Build(Recipe recipe, int requestedAmount, CraftInventory inventory)
	{
		return Build(recipe, requestedAmount, inventory, scanMax: true);
	}

	public static CraftPlan Build(Recipe recipe, int requestedAmount, CraftInventory inventory, bool scanMax)
	{
		CraftPlan craftPlan = new CraftPlan
		{
			FinalRecipe = recipe,
			RequestedAmount = Mathf.Max(1, requestedAmount)
		};
		if (recipe == null || inventory == null)
		{
			return craftPlan;
		}
		int num = Mathf.Clamp(requestedAmount, 1, 99);
		int num2 = (scanMax ? 99 : num);
		CraftInventory working = inventory.Clone();
		List<CraftStep> list = new List<CraftStep>();
		List<CraftCombine> list2 = new List<CraftCombine>();
		int num3 = 0;
		for (int i = 0; i < num2; i++)
		{
			list.Clear();
			list2.Clear();
			bool flag = i < num;
			if (!TryPlanOne(recipe, working, new HashSet<string>(StringComparer.OrdinalIgnoreCase), 0, flag ? list : null, flag ? list2 : null))
			{
				break;
			}
			if (flag)
			{
				craftPlan.Intermediates.AddRange(list);
				craftPlan.Combines.AddRange(list2);
			}
			num3++;
		}
		craftPlan.MaxCraftable = num3;
		if (num3 <= 0)
		{
			return craftPlan;
		}
		craftPlan.RequestedAmount = Mathf.Min(num, num3);
		craftPlan.CanCraft = true;
		return craftPlan;
	}

	public static bool IsCraftable(Recipe recipe, CraftInventory inventory)
	{
		if (recipe == null || inventory == null)
		{
			return false;
		}
		EnsureCacheGeneration(inventory.Revision);
		if (CraftableByRecipe.TryGetValue(recipe.index, out var value))
		{
			return value;
		}
		bool flag = EvaluateCraftable(recipe, inventory);
		CraftableByRecipe[recipe.index] = flag;
		return flag;
	}

	public static int MaxCraftable(Recipe recipe, CraftInventory inventory)
	{
		if (recipe == null || inventory == null)
		{
			return 0;
		}
		EnsureCacheGeneration(inventory.Revision);
		if (MaxByRecipe.TryGetValue(recipe.index, out var value))
		{
			return value;
		}
		CraftPlan craftPlan = Build(recipe, 99, inventory);
		MaxByRecipe[recipe.index] = craftPlan.MaxCraftable;
		return craftPlan.MaxCraftable;
	}

	public static CraftIngredientStatus InspectIngredient(RecipeItem ingredient, CraftInventory working, HashSet<string> coveredAutoCrafts)
	{
		if (ingredient == null || working == null)
		{
			return CraftIngredientStatus.Missing;
		}
		if (working.TryConsume(ingredient))
		{
			return CraftIngredientStatus.Available;
		}
		if (working.TryRepairInto(ingredient, null) && working.TryConsume(ingredient))
		{
			return CraftIngredientStatus.Available;
		}
		string text = AutoCraftKey(ingredient);
		if (string.IsNullOrEmpty(text))
		{
			return CraftIngredientStatus.Missing;
		}
		if (!TryPlanSource(ingredient, text, working, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { text }, 1, null, null))
		{
			return CraftIngredientStatus.AutoCraftableButBlocked;
		}
		coveredAutoCrafts?.Add(text);
		return CraftIngredientStatus.AutoCraftable;
	}

	public static string AutoCraftKey(RecipeItem ingredient)
	{
		if (ingredient == null)
		{
			return null;
		}
		string text = (ingredient.specific ? ingredient.specificId : ingredient.quality?.id);
		if (string.IsNullOrEmpty(text) || NonAutoCraftableResults.Contains(text))
		{
			return null;
		}
		return IsExcludedFromAutoCraft(text) ? null : text;
	}

	public static string SelfCheck()
	{
		Body val = PlayerCamera.main?.body;
		if ((Object)(object)val == (Object)null || Recipes.recipes == null)
		{
			return "Craft self-check: no player body loaded.";
		}
		CraftInventory inventory = CraftInventory.Capture(val);
		CraftInventory inventory2 = CraftInventory.Capture(val);
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		for (int i = 0; i < Recipes.recipes.Count; i++)
		{
			Recipe val2 = Recipes.recipes[i];
			if (val2 != null && val2.visible)
			{
				num++;
				bool flag = IsCraftable(val2, inventory);
				int num4 = MaxCraftable(val2, inventory);
				bool flag2 = IsCraftable(val2, inventory);
				bool flag3 = IsCraftable(val2, inventory2);
				CraftPlan craftPlan = Build(val2, num4 + 5, inventory);
				if (flag != num4 >= 1 || flag2 != flag || flag3 != flag || craftPlan.CanCraft != flag || (flag && craftPlan.RequestedAmount != num4))
				{
					num3++;
					Debug.Log((object)$"[CraftCheck] {val2.simpleName}: craftable={flag} cacheHit={flag2} fresh={flag3} max={num4} overbidCanCraft={craftPlan.CanCraft} overbidAmount={craftPlan.RequestedAmount}");
				}
				if (flag)
				{
					num2++;
				}
			}
		}
		return $"Craft self-check: {num} recipes, {num2} craftable, {num3} inconsistent.";
	}

	private static bool EvaluateCraftable(Recipe recipe, CraftInventory inventory)
	{
		if (inventory.MatchesDirectly(recipe))
		{
			return true;
		}
		CraftInventory working = inventory.Clone();
		return TryPlanOne(recipe, working, new HashSet<string>(StringComparer.OrdinalIgnoreCase), 0, null, null);
	}

	private static bool TryPlanOne(Recipe recipe, CraftInventory working, HashSet<string> chain, int depth, List<CraftStep> steps, List<CraftCombine> combines)
	{
		if (recipe == null || depth > 8)
		{
			return false;
		}
		for (int i = 0; i < recipe.items.Count; i++)
		{
			RecipeItem val = recipe.items[i];
			if (!working.TryConsume(val) && (!working.TryRepairInto(val, combines) || !working.TryConsume(val)) && !TryAutoCraft(val, working, chain, depth, steps, combines))
			{
				return false;
			}
		}
		working.Produce(recipe);
		return true;
	}

	private static bool TryAutoCraft(RecipeItem missingIngredient, CraftInventory working, HashSet<string> chain, int depth, List<CraftStep> steps, List<CraftCombine> combines)
	{
		string text = AutoCraftKey(missingIngredient);
		if (string.IsNullOrEmpty(text) || chain.Contains(text))
		{
			return false;
		}
		chain.Add(text);
		bool result = TryPlanSource(missingIngredient, text, working, chain, depth + 1, steps, combines);
		chain.Remove(text);
		return result;
	}

	private static bool TryPlanSource(RecipeItem ingredient, string resultId, CraftInventory working, HashSet<string> chain, int depth, List<CraftStep> steps, List<CraftCombine> combines)
	{
		if (Recipes.recipes == null)
		{
			return false;
		}
		RebuildRecipeIndexIfNeeded();
		if (!RecipesByResultId.TryGetValue(resultId, out var value))
		{
			return false;
		}
		int num = value.Count - 1;
		while (num >= 0 && !value[num].visible)
		{
			num--;
		}
		for (int i = 0; i <= num; i++)
		{
			Recipe val = value[i];
			if (!val.visible)
			{
				continue;
			}
			bool flag = i == num;
			CraftInventory craftInventory = (flag ? working : working.Clone());
			List<CraftStep> list = ((flag || steps == null) ? steps : new List<CraftStep>());
			List<CraftCombine> list2 = ((flag || combines == null) ? combines : new List<CraftCombine>());
			if (!TryPlanOne(val, craftInventory, chain, depth, list, list2) || !craftInventory.TryConsume(ingredient))
			{
				continue;
			}
			if (!flag)
			{
				working.AdoptFrom(craftInventory);
				if (list != null)
				{
					for (int j = 0; j < list.Count; j++)
					{
						AddStep(steps, list[j].Recipe, list[j].Count);
					}
				}
				combines?.AddRange(list2);
			}
			if (steps != null)
			{
				AddStep(steps, val, 1);
			}
			return true;
		}
		return false;
	}

	private static void AddStep(List<CraftStep> steps, Recipe recipe, int count)
	{
		for (int num = steps.Count - 1; num >= 0; num--)
		{
			if (steps[num].Recipe == recipe)
			{
				steps[num].Count += count;
				return;
			}
		}
		steps.Add(new CraftStep
		{
			Recipe = recipe,
			Count = count
		});
	}

	private static bool IsExcludedFromAutoCraft(string resultId)
	{
		if (Item.GlobalItems == null || !Item.GlobalItems.TryGetValue(resultId, out var value) || value == null)
		{
			return false;
		}
		return value.HasTag("noautocraft");
	}

	private static void RebuildRecipeIndexIfNeeded()
	{
		int num = Recipes.recipes?.Count ?? 0;
		if (recipeIndexCacheCount == num)
		{
			return;
		}
		RecipesByResultId.Clear();
		recipeIndexCacheCount = num;
		if (Recipes.recipes == null)
		{
			return;
		}
		for (int i = 0; i < Recipes.recipes.Count; i++)
		{
			Recipe val = Recipes.recipes[i];
			if (val?.result != null && !string.IsNullOrEmpty(val.result.id))
			{
				if (!RecipesByResultId.TryGetValue(val.result.id, out var value))
				{
					value = new List<Recipe>();
					RecipesByResultId.Add(val.result.id, value);
				}
				value.Add(val);
			}
		}
	}

	private static void EnsureCacheGeneration(int revision)
	{
		if (cachedRevision != revision)
		{
			cachedRevision = revision;
			CraftableByRecipe.Clear();
			MaxByRecipe.Clear();
		}
	}
}
