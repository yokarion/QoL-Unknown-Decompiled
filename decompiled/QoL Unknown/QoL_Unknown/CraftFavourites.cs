using System.Collections.Generic;
using BepInEx.Logging;
using HarmonyLib;
using QoL_Unknown.Images;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace QoL_Unknown;

internal static class CraftFavourites
{
	private const string PrefsKey = "qol_craft_favourites";

	private const string ButtonName = "QoL_FavouriteButton";

	private const float PinButtonGap = 12f;

	internal static readonly Color BookmarkColor = new Color(1f, 0.92f, 0.3f);

	private static HashSet<string> favourites;

	private static ManualLogSource Logger;

	public static HashSet<string> Favourites
	{
		get
		{
			if (favourites == null)
			{
				Load();
			}
			return favourites;
		}
	}

	public static void Initialize(ManualLogSource logger)
	{
		Logger = logger;
		RuntimePatchRegistry.PatchAllOnce("org.bepinex.plugins.qol.craftfavourites", typeof(CraftFavourites), logger);
	}

	public static bool IsFavourited(string simpleName)
	{
		return !string.IsNullOrEmpty(simpleName) && Favourites.Contains(simpleName);
	}

	public static bool IsFavourited(Recipe recipe)
	{
		return recipe != null && IsFavourited(recipe.simpleName);
	}

	public static void Toggle(string simpleName)
	{
		if (!string.IsNullOrEmpty(simpleName))
		{
			if (!Favourites.Remove(simpleName))
			{
				Favourites.Add(simpleName);
			}
			Save();
		}
	}

	public static void Toggle(Recipe recipe)
	{
		if (recipe != null)
		{
			Toggle(recipe.simpleName);
		}
	}

	internal static Button CreatePinStyleButton(PlayerCamera camera, string name, string spriteName, int slot, string tipName, string tipDesc)
	{
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0112: Unknown result type (might be due to invalid IL or missing references)
		//IL_0153: Unknown result type (might be due to invalid IL or missing references)
		//IL_015d: Expected O, but got Unknown
		if ((Object)(object)camera == (Object)null || (Object)(object)camera.craftingPanel == (Object)null)
		{
			return null;
		}
		Transform transform = camera.craftingPanel.transform;
		if ((Object)(object)transform.Find(name) != (Object)null)
		{
			return null;
		}
		Transform obj = transform.Find("PinButton");
		RectTransform val = (RectTransform)(object)((obj is RectTransform) ? obj : null);
		if ((Object)(object)val == (Object)null || (Object)(object)((Component)val).GetComponent<Button>() == (Object)null || (Object)(object)((Component)val).GetComponent<Image>() == (Object)null)
		{
			ManualLogSource logger = Logger;
			if (logger != null)
			{
				logger.LogWarning((object)(tipName + " button could not be placed because the crafting pin button changed."));
			}
			return null;
		}
		GameObject val2 = Object.Instantiate<GameObject>(((Component)val).gameObject, transform);
		((Object)val2).name = name;
		val2.GetComponent<RectTransform>().anchoredPosition = val.anchoredPosition - new Vector2((float)slot * (val.sizeDelta.x + 12f), 0f);
		Image component = val2.GetComponent<Image>();
		component.sprite = Helper.LoadEmbeddedSprite(spriteName);
		((Graphic)component).color = Color.white;
		UITooltip val3 = val2.GetComponent<UITooltip>() ?? val2.AddComponent<UITooltip>();
		val3.skipLocale = true;
		val3.tipName = tipName;
		val3.tipDesc = tipDesc;
		Button component2 = val2.GetComponent<Button>();
		component2.onClick = new ButtonClickedEvent();
		return component2;
	}

	[HarmonyPatch(typeof(PlayerCamera), "Start")]
	[HarmonyPostfix]
	private static void PlayerCamera_Start_Postfix(PlayerCamera __instance)
	{
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Expected O, but got Unknown
		if (!QoLModuleManager.IsEnabled("qol_craft_uncrafted"))
		{
			return;
		}
		Button val = CreatePinStyleButton(__instance, "QoL_FavouriteButton", "favourite_craft", 1, "Bookmark", "");
		if (!((Object)(object)val == (Object)null))
		{
			((UnityEvent)val.onClick).AddListener((UnityAction)delegate
			{
				Toggle(Recipes.recipes[__instance.selectedRecipe]);
				UpdateButton(__instance);
				__instance.PlayUISound((UISoundType)1, 1f);
				__instance.RefreshRecipeList();
			});
			UpdateButton(__instance);
		}
	}

	[HarmonyPatch(typeof(PlayerCamera), "RefreshCurrentlySelectedRecipe")]
	[HarmonyPostfix]
	private static void RefreshCurrentlySelectedRecipe_Postfix(PlayerCamera __instance)
	{
		UpdateButton(__instance);
	}

	private static void UpdateButton(PlayerCamera camera)
	{
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		Transform val = camera.craftingPanel.transform.Find("QoL_FavouriteButton");
		if (!((Object)(object)val == (Object)null))
		{
			bool flag = IsFavourited(Recipes.recipes[camera.selectedRecipe]);
			((Graphic)((Component)val).GetComponent<Image>()).color = (flag ? BookmarkColor : Color.white);
			((Component)val).GetComponent<UITooltip>().tipDesc = (flag ? "Click to unbookmark this recipe." : "Click to bookmark this recipe.") + " Bookmarked recipes appear at the top of the list.";
		}
	}

	private static void Load()
	{
		favourites = new HashSet<string>();
		string text = PlayerPrefs.GetString("qol_craft_favourites", "");
		if (string.IsNullOrEmpty(text))
		{
			return;
		}
		string[] array = text.Split(new char[1] { ',' });
		for (int i = 0; i < array.Length; i++)
		{
			string text2 = array[i].Trim();
			if (!string.IsNullOrEmpty(text2))
			{
				favourites.Add(text2);
			}
		}
	}

	private static void Save()
	{
		string text = string.Join(",", Favourites);
		PlayerPrefs.SetString("qol_craft_favourites", text);
		PlayerPrefs.Save();
	}
}
