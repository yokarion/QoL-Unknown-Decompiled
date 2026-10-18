using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace QoL_Unknown;

internal class SpawnAutofill
{
	private static ManualLogSource Logger;

	private static List<string> spawnableItems = new List<string>();

	private static readonly Dictionary<string, Sprite> itemSprites = new Dictionary<string, Sprite>();

	private static readonly Dictionary<string, GameObject> prefabsByName = new Dictionary<string, GameObject>();

	private static GameObject previewImageObject;

	private static Image previewImage;

	private static bool isInitialized = false;

	private static Type cuCoreItemRegistryType;

	private static Type cuCoreBuildingRegistryType;

	private static MethodInfo cuCoreGetRegisteredItemIdsMethod;

	private static MethodInfo cuCoreGetRegisteredBuildingIdsMethod;

	private static MethodInfo cuCoreTryGetIconMethod;

	private static bool cuCoreReflectionInitialized = false;

	public static void Initialize(ManualLogSource logger)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Expected O, but got Unknown
		Logger = logger;
		Harmony val = new Harmony("org.bepinex.plugins.qol.spawnautofill");
		val.PatchAll(typeof(SpawnAutofill));
	}

	private static void EnsurePrefabLookup()
	{
		if (prefabsByName.Count > 0)
		{
			return;
		}
		try
		{
			GameObject[] array = Resources.LoadAll<GameObject>("");
			foreach (GameObject val in array)
			{
				if ((Object)(object)val != (Object)null && !prefabsByName.ContainsKey(((Object)val).name))
				{
					prefabsByName[((Object)val).name] = val;
				}
			}
		}
		catch (Exception)
		{
		}
	}

	private static void EnsureCuCoreReflection()
	{
		if (cuCoreReflectionInitialized)
		{
			return;
		}
		cuCoreReflectionInitialized = true;
		try
		{
			cuCoreItemRegistryType = AccessTools.TypeByName("CUCoreLib.Registries.ItemRegistry");
			cuCoreBuildingRegistryType = AccessTools.TypeByName("CUCoreLib.Registries.BuildingEntityRegistry");
			if (!(cuCoreItemRegistryType == null))
			{
				cuCoreGetRegisteredItemIdsMethod = AccessTools.Method(cuCoreItemRegistryType, "GetRegisteredItemIds", (Type[])null, (Type[])null);
				cuCoreTryGetIconMethod = AccessTools.Method(cuCoreItemRegistryType, "TryGetIcon", new Type[2]
				{
					typeof(string),
					typeof(Sprite).MakeByRefType()
				}, (Type[])null);
				if (cuCoreBuildingRegistryType != null)
				{
					cuCoreGetRegisteredBuildingIdsMethod = AccessTools.Method(cuCoreBuildingRegistryType, "GetRegisteredIds", (Type[])null, (Type[])null);
				}
			}
		}
		catch (Exception ex)
		{
			Logger.LogWarning((object)("SpawnAutofill could not bind CUCoreLib item APIs: " + ex.Message));
		}
	}

	private static void AppendDistinctIds(List<string> autofillList, MethodInfo getIdsMethod)
	{
		EnsureCuCoreReflection();
		if (autofillList == null || getIdsMethod == null)
		{
			return;
		}
		try
		{
			if (!(getIdsMethod.Invoke(null, null) is IEnumerable<string> enumerable))
			{
				return;
			}
			foreach (string item in enumerable)
			{
				if (!string.IsNullOrWhiteSpace(item) && !autofillList.Contains<string>(item, StringComparer.OrdinalIgnoreCase))
				{
					autofillList.Add(item);
				}
			}
		}
		catch (Exception ex)
		{
			Logger.LogWarning((object)("SpawnAutofill failed to append CUCoreLib IDs: " + ex.Message));
		}
	}

	private static bool TryGetCuCoreSprite(string itemId, out Sprite sprite)
	{
		sprite = null;
		EnsureCuCoreReflection();
		if (string.IsNullOrWhiteSpace(itemId) || cuCoreTryGetIconMethod == null)
		{
			return false;
		}
		try
		{
			object[] array = new object[2] { itemId, null };
			if (!(cuCoreTryGetIconMethod.Invoke(null, array) is bool flag) || !flag)
			{
				return false;
			}
			object obj = array[1];
			sprite = (Sprite)((obj is Sprite) ? obj : null);
			return (Object)(object)sprite != (Object)null;
		}
		catch (Exception ex)
		{
			Logger.LogWarning((object)("SpawnAutofill failed to resolve CUCoreLib sprite for '" + itemId + "': " + ex.Message));
			return false;
		}
	}

	private static void EnsureSpawnAutofillFromVanilla(ConsoleScript console)
	{
		if (isInitialized || (Object)(object)console == (Object)null)
		{
			return;
		}
		try
		{
			console.RegisterSpawnEntities();
			Command val = ConsoleScript.SearchExact("spawn");
			if (val == null || val.argAutofill == null || !val.argAutofill.TryGetValue(0, out var value) || value == null)
			{
				return;
			}
			AppendDistinctIds(value, cuCoreGetRegisteredItemIdsMethod);
			AppendDistinctIds(value, cuCoreGetRegisteredBuildingIdsMethod);
			spawnableItems = value;
			EnsurePrefabLookup();
			for (int i = 0; i < spawnableItems.Count; i++)
			{
				string text = spawnableItems[i];
				if (string.IsNullOrEmpty(text) || itemSprites.ContainsKey(text))
				{
					continue;
				}
				if (TryGetCuCoreSprite(text, out var sprite))
				{
					itemSprites[text] = sprite;
				}
				else
				{
					if (!prefabsByName.TryGetValue(text, out var value2) || (Object)(object)value2 == (Object)null)
					{
						continue;
					}
					SpriteRenderer component = value2.GetComponent<SpriteRenderer>();
					if ((Object)(object)component != (Object)null && (Object)(object)component.sprite != (Object)null)
					{
						itemSprites[text] = component.sprite;
						continue;
					}
					SpriteRenderer componentInChildren = value2.GetComponentInChildren<SpriteRenderer>();
					if ((Object)(object)componentInChildren != (Object)null && (Object)(object)componentInChildren.sprite != (Object)null)
					{
						itemSprites[text] = componentInChildren.sprite;
					}
				}
			}
			isInitialized = true;
		}
		catch (Exception ex)
		{
			Logger.LogError((object)("Failed to initialize spawn autofill from vanilla data: " + ex.Message));
		}
	}

	private static void CreatePreviewImage(ConsoleScript console)
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Expected O, but got Unknown
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Expected O, but got Unknown
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Expected O, but got Unknown
		//IL_0165: Unknown result type (might be due to invalid IL or missing references)
		//IL_017c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0189: Unknown result type (might be due to invalid IL or missing references)
		//IL_0196: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)previewImageObject != (Object)null)
		{
			return;
		}
		try
		{
			previewImageObject = new GameObject("SpawnPreviewImage");
			previewImageObject.transform.SetParent(console.descriptionBack.transform.parent, false);
			RectTransform val = previewImageObject.AddComponent<RectTransform>();
			val.sizeDelta = new Vector2(64f, 64f);
			val.anchorMin = new Vector2(0f, 0f);
			val.anchorMax = new Vector2(0f, 0f);
			val.pivot = new Vector2(0f, 1f);
			GameObject val2 = new GameObject("Background");
			val2.transform.SetParent(previewImageObject.transform, false);
			RectTransform val3 = val2.AddComponent<RectTransform>();
			val3.anchorMin = Vector2.zero;
			val3.anchorMax = Vector2.one;
			val3.offsetMin = Vector2.zero;
			val3.offsetMax = Vector2.zero;
			Image val4 = val2.AddComponent<Image>();
			((Graphic)val4).color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
			GameObject val5 = new GameObject("ItemSprite");
			val5.transform.SetParent(previewImageObject.transform, false);
			RectTransform val6 = val5.AddComponent<RectTransform>();
			val6.anchorMin = new Vector2(0.1f, 0.1f);
			val6.anchorMax = new Vector2(0.9f, 0.9f);
			val6.offsetMin = Vector2.zero;
			val6.offsetMax = Vector2.zero;
			previewImage = val5.AddComponent<Image>();
			previewImage.preserveAspect = true;
			previewImageObject.SetActive(false);
		}
		catch (Exception ex)
		{
			Logger.LogError((object)("Failed to create preview image: " + ex.Message));
		}
	}

	private static void UpdatePreviewImage(string itemName, Vector3 position)
	{
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)previewImageObject == (Object)null) && !((Object)(object)previewImage == (Object)null))
		{
			if (string.IsNullOrEmpty(itemName) || !itemSprites.ContainsKey(itemName))
			{
				previewImageObject.SetActive(false);
				return;
			}
			previewImage.sprite = itemSprites[itemName];
			previewImageObject.SetActive(true);
			RectTransform component = previewImageObject.GetComponent<RectTransform>();
			((Transform)component).position = position + Vector3.right * 200f + Vector3.down * 32f;
		}
	}

	[HarmonyPatch(typeof(ConsoleScript), "RegisterAllCommands")]
	[HarmonyPostfix]
	private static void RegisterAllCommands_Postfix(ConsoleScript __instance)
	{
		try
		{
			EnsureSpawnAutofillFromVanilla(__instance);
			CreatePreviewImage(__instance);
		}
		catch (Exception ex)
		{
			Logger.LogError((object)("Failed to add spawn autofill: " + ex.Message));
		}
	}

	[HarmonyPatch(typeof(ConsoleScript), "HandleDescriptionText")]
	[HarmonyPostfix]
	private static void HandleDescriptionText_Postfix(ConsoleScript __instance, string[] args)
	{
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if ((Object)(object)previewImageObject == (Object)null)
			{
				return;
			}
			if (args.Length >= 2 && (args[0].Equals("spawn", StringComparison.OrdinalIgnoreCase) || args[0].Equals("cuspawn", StringComparison.OrdinalIgnoreCase)))
			{
				string text = args[1];
				Command val = ConsoleScript.SearchExact(args[0]);
				if (val != null && val.argAutofill != null && val.argAutofill.TryGetValue(0, out var value))
				{
					List<string> list = ConsoleScript.SearchArgumentAutofill(text, value);
					if (list.Count > 0)
					{
						int index = Mathf.Clamp(ConsoleScriptPatcher.SelectedAutofillIndex, 0, list.Count - 1);
						string itemName = list[index];
						Vector3 position = ((TMP_Text)__instance.descriptionText).transform.position;
						UpdatePreviewImage(itemName, position);
						return;
					}
				}
			}
			previewImageObject.SetActive(false);
		}
		catch (Exception)
		{
		}
	}

	[HarmonyPatch(typeof(ConsoleScript), "HandleConsoleActiveState")]
	[HarmonyPostfix]
	private static void HandleConsoleActiveState_Postfix(ConsoleScript __instance)
	{
		if ((Object)(object)previewImageObject != (Object)null && !__instance.active)
		{
			previewImageObject.SetActive(false);
		}
	}
}
