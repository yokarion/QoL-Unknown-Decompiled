using System.Collections.Generic;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace QoL_Unknown;

internal static class UncraftedCraftFilter
{
	public const string ModuleId = "qol_craft_uncrafted";

	private const string StateKey = "qol_craft_uncrafted_view";

	private const string ButtonName = "QoL_UncraftedButton";

	private static readonly Color ActiveColor = new Color(0.65f, 0.42f, 0.2f);

	public static bool Enabled
	{
		get
		{
			return PlayerPrefs.GetInt("qol_craft_uncrafted_view", 0) == 1;
		}
		set
		{
			PlayerPrefs.SetInt("qol_craft_uncrafted_view", value ? 1 : 0);
			PlayerPrefs.Save();
		}
	}

	public static void Initialize(ManualLogSource logger)
	{
		RuntimePatchRegistry.PatchAllOnce("org.bepinex.plugins.qol.uncraftedfilter", typeof(UncraftedCraftFilter), logger);
	}

	[HarmonyPatch(typeof(Recipes), "GetVisibleRecipes")]
	[HarmonyPostfix]
	private static void GetVisibleRecipes_Postfix(ref List<Recipe> __result)
	{
		if (__result != null && Enabled)
		{
			__result.RemoveAll((Recipe recipe) => recipe?.hasMadeBefore ?? true);
		}
	}

	[HarmonyPatch(typeof(PlayerCamera), "Start")]
	[HarmonyPostfix]
	private static void PlayerCamera_Start_Postfix(PlayerCamera __instance)
	{
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Expected O, but got Unknown
		Button val = CraftFavourites.CreatePinStyleButton(__instance, "QoL_UncraftedButton", "uncrafted_craft", 2, "Uncrafted only", "Displays only items that have not been crafted before.");
		if (!((Object)(object)val == (Object)null))
		{
			Image image = ((Component)val).GetComponent<Image>();
			((Graphic)image).color = (Enabled ? ActiveColor : Color.white);
			((UnityEvent)val.onClick).AddListener((UnityAction)delegate
			{
				//IL_0023: Unknown result type (might be due to invalid IL or missing references)
				//IL_001c: Unknown result type (might be due to invalid IL or missing references)
				Enabled = !Enabled;
				((Graphic)image).color = (Enabled ? ActiveColor : Color.white);
				__instance.PlayUISound((UISoundType)1, 1f);
				__instance.RefreshRecipeList();
			});
		}
	}
}
