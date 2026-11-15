using System.Reflection;
using System.Text;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace QoL_Unknown;

internal class MinigameQoLPatcher
{
	private const string MinigameQoLPrefKey = "Setting_Minigame_QoL";

	private const float LockpickDegreeMultiplier = 2f;

	private const float LockpickSpeedMultiplier = 1.2f;

	private const int KeypadMinDigits = 5;

	private const int KeypadMaxDigitsInclusive = 9;

	private static RectTransform raisedKeypadScreen;

	private static int keypadScreenBaseSiblingIndex;

	private static FieldInfo openableCodeField;

	public static void Initialize(ManualLogSource logger)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		new Harmony("org.bepinex.plugins.qol.minigame").PatchAll(typeof(MinigameQoLPatcher));
	}

	private static bool MinigameQoLEnabled()
	{
		return Settings.Get<SettingBool>("qol.game.minigameqol")?.value ?? (PlayerPrefs.GetInt("Setting_Minigame_QoL", 0) == 1);
	}

	[HarmonyPatch(/*Could not decode attribute arguments.*/)]
	[HarmonyPrefix]
	private static void LockpingMinigame_Ctor_Prefix(ref float anglePrecision)
	{
		if (MinigameQoLEnabled())
		{
			anglePrecision *= 2f;
		}
	}

	[HarmonyPatch(typeof(LockpingMinigame), "Update")]
	[HarmonyPriority(800)]
	[HarmonyBefore(new string[] { "CasualtiesMP" })]
	[HarmonyPrefix]
	private static void LockpingMinigame_Update_Prefix(LockpingMinigame __instance)
	{
		if (MinigameQoLEnabled() && __instance.clickingInside && !((Object)(object)__instance.toDestroy == (Object)null))
		{
			float num = __instance.MaxTurnProgress();
			if (!(__instance.lockProgress >= num))
			{
				float num2 = __instance.pickLevel + 1;
				float num3 = Time.deltaTime * (0.66f + num2 * 0.065f);
				float num4 = (Mathf.Min(__instance.lockProgress + num3, num) - __instance.lockProgress) * 0.20000005f;
				__instance.lockProgress = Mathf.Clamp(__instance.lockProgress + num4, 0f, num);
			}
		}
	}

	[HarmonyPatch(typeof(KeypadMinigame), "GenerateCode")]
	[HarmonyPrefix]
	private static bool KeypadMinigame_GenerateCode_Prefix(ref string __result)
	{
		if (!MinigameQoLEnabled())
		{
			return true;
		}
		int num = Random.Range(5, 10);
		StringBuilder stringBuilder = new StringBuilder(num);
		for (int i = 0; i < num; i++)
		{
			stringBuilder.Append(Random.Range(0, 10));
		}
		__result = stringBuilder.ToString();
		return false;
	}

	[HarmonyPatch(typeof(KeypadMinigame), "Start")]
	[HarmonyPostfix]
	private static void KeypadMinigame_Start_RaiseAboveSavePanel_Postfix()
	{
		MinigameBase main = MinigameBase.main;
		WorldGeneration world = WorldGeneration.world;
		if (!((Object)(object)main == (Object)null) && !((Object)(object)main.minigameScreen == (Object)null) && !((Object)(object)world == (Object)null) && !((Object)(object)world.savePanel == (Object)null))
		{
			Transform transform = ((Component)main.minigameScreen).transform;
			Transform transform2 = world.savePanel.transform;
			if (!((Object)(object)transform.parent == (Object)null) && !((Object)(object)transform.parent != (Object)(object)transform2.parent) && transform.GetSiblingIndex() <= transform2.GetSiblingIndex())
			{
				raisedKeypadScreen = main.minigameScreen;
				keypadScreenBaseSiblingIndex = transform.GetSiblingIndex();
				transform.SetAsLastSibling();
				transform.SetSiblingIndex(transform2.GetSiblingIndex() + 1);
			}
		}
	}

	[HarmonyPatch(typeof(MinigameBase), "EndMinigame")]
	[HarmonyPostfix]
	private static void MinigameBase_EndMinigame_RestoreScreenOrder_Postfix()
	{
		RectTransform val = raisedKeypadScreen;
		raisedKeypadScreen = null;
		if ((Object)(object)val != (Object)null)
		{
			((Component)val).transform.SetSiblingIndex(keypadScreenBaseSiblingIndex);
		}
	}

	[HarmonyPatch(typeof(Openable), "OnUse")]
	[HarmonyPrefix]
	private static bool Openable_OnUse_Prefix(Openable __instance)
	{
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Expected O, but got Unknown
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Expected O, but got Unknown
		if (__instance.instantOpen)
		{
			((Component)__instance).GetComponent<BuildingEntity>().health = 0f;
			return false;
		}
		if (__instance.isKeypad)
		{
			if (openableCodeField == null)
			{
				openableCodeField = AccessTools.Field(typeof(Openable), "code");
			}
			string text = (string)openableCodeField.GetValue(__instance);
			if (string.IsNullOrEmpty(text))
			{
				text = KeypadMinigame.GenerateCode();
				openableCodeField.SetValue(__instance, text);
			}
			MinigameBase.main.StartMinigame((Minigame)new KeypadMinigame(((Component)__instance).GetComponent<BuildingEntity>(), text), (Item)null);
			return false;
		}
		Item val = FindByIdEverywhere(PlayerCamera.main.body, "lockpickingkit");
		if (PlayerCamera.main.body.skills.INT < 10)
		{
			val = null;
		}
		MinigameBase.main.StartMinigame((Minigame)new LockpingMinigame(((Component)__instance).GetComponent<BuildingEntity>(), __instance.lockpickAnglePrecision * WorldGeneration.GetRunSettingFloat("lockpickprecision")), val);
		return false;
	}

	[HarmonyPatch(typeof(PlayerCamera), "WoundSpecialAction")]
	[HarmonyPrefix]
	private static bool PlayerCamera_WoundSpecialAction_Prefix(PlayerCamera __instance)
	{
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Expected O, but got Unknown
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Expected O, but got Unknown
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		if (!__instance.body.allowUseItem)
		{
			__instance.UseFailUnhappiness();
			return false;
		}
		if (!__instance.body.conscious)
		{
			return false;
		}
		Limb selectedLimb = __instance.selectedLimb;
		TourniquetScript val = default(TourniquetScript);
		if (((Component)selectedLimb).TryGetComponent<TourniquetScript>(ref val))
		{
			val.TakeOff();
		}
		else if (selectedLimb.hasShrapnel)
		{
			Item val2 = FindByIdEverywhere(__instance.body, "tweezers");
			bool flag = (Object)(object)val2 != (Object)null;
			MinigameBase.main.StartMinigame((Minigame)new ShrapnelMinigame(selectedLimb, flag), val2);
			if (flag)
			{
				val2.condition -= 0.01f;
				Sound.Play("tweezeruse", Vector2.op_Implicit(((Component)__instance.body).transform.position), false, true, (Transform)null, 1f, 1f, false, false);
			}
		}
		SplintLimb val3 = default(SplintLimb);
		if (((Component)selectedLimb).TryGetComponent<SplintLimb>(ref val3))
		{
			val3.TakeOff();
		}
		else if (selectedLimb.dislocated)
		{
			Item val4 = FindByIdEverywhere(__instance.body, "wrench");
			bool flag2 = (Object)(object)val4 != (Object)null;
			MinigameBase.main.StartMinigame((Minigame)new DislocationMinigame(selectedLimb, flag2), val4);
			if (flag2)
			{
				Sound.Play("wrenchhit", Vector2.op_Implicit(((Component)val4).transform.position), false, true, (Transform)null, 1f, 1f, false, false);
			}
		}
		return false;
	}

	[HarmonyPatch(typeof(MinigameBase), "StartMinigame")]
	[HarmonyPriority(800)]
	[HarmonyBefore(new string[] { "CasualtiesMP" })]
	[HarmonyPrefix]
	private static void MinigameBase_StartMinigame_Prefix(MinigameBase __instance, Minigame minigame, ref Item item)
	{
		if ((Object)(object)item != (Object)null || (Object)(object)__instance == (Object)null || (Object)(object)__instance.body == (Object)null)
		{
			return;
		}
		ShrapnelMinigame val = (ShrapnelMinigame)(object)((minigame is ShrapnelMinigame) ? minigame : null);
		if (val != null && !val.hasTweezers)
		{
			Item val2 = FindByIdEverywhere(__instance.body, "tweezers");
			if ((Object)(object)val2 != (Object)null)
			{
				val.hasTweezers = true;
				item = val2;
			}
			return;
		}
		DislocationMinigame val3 = (DislocationMinigame)(object)((minigame is DislocationMinigame) ? minigame : null);
		if (val3 != null && !val3.hasWrench)
		{
			Item val4 = FindByIdEverywhere(__instance.body, "wrench");
			if ((Object)(object)val4 != (Object)null)
			{
				val3.hasWrench = true;
				item = val4;
			}
		}
	}

	private static Item FindByIdEverywhere(Body body, string id)
	{
		for (int i = 0; i < body.slots.Length; i++)
		{
			Item val = FindInItemTree(body.GetItem(i), id);
			if ((Object)(object)val != (Object)null)
			{
				return val;
			}
		}
		foreach (Item allWearable in body.GetAllWearables())
		{
			Item val2 = FindInItemTree(allWearable, id);
			if ((Object)(object)val2 != (Object)null)
			{
				return val2;
			}
		}
		return null;
	}

	private static Item FindInItemTree(Item item, string id)
	{
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)item == (Object)null)
		{
			return null;
		}
		if (item.id == id)
		{
			return item;
		}
		if ((Object)(object)((Component)item).GetComponent<Container>() == (Object)null)
		{
			return null;
		}
		foreach (Transform item2 in ((Component)item).transform)
		{
			Item val = FindInItemTree(((Component)item2).GetComponent<Item>(), id);
			if ((Object)(object)val != (Object)null)
			{
				return val;
			}
		}
		return null;
	}
}
