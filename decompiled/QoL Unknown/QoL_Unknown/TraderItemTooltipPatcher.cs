using System;
using System.Collections.Generic;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;

namespace QoL_Unknown;

internal class TraderItemTooltipPatcher
{
	public static void Initialize(ManualLogSource pluginLogger)
	{
		RuntimePatchRegistry.PatchAllOnce("org.bepinex.plugins.qol.tradertooltips", typeof(TraderItemTooltipPatcher), pluginLogger);
	}

	[HarmonyPatch(typeof(UIUtil), "GetUITooltip")]
	[HarmonyPostfix]
	private static void GetUITooltip_Postfix(List<RaycastResult> UICasts, ref (string, string) __result)
	{
		PlayerCamera main = PlayerCamera.main;
		if ((Object)(object)main == (Object)null || (Object)(object)main.currentTrader == (Object)null || (Object)(object)main.traderInventory == (Object)null || (Object)(object)main.tradeMenu == (Object)null || !main.tradeMenu.activeSelf)
		{
			return;
		}
		UITooltip hoveredTooltip = GetHoveredTooltip(UICasts);
		if (!((Object)(object)hoveredTooltip == (Object)null) && ((Component)hoveredTooltip).transform.IsChildOf(main.traderInventory))
		{
			ItemInfo val = FindTraderItem(main.currentTrader, hoveredTooltip.tipName);
			if (val != null)
			{
				bool expanded = PlayerCamera.alwaysExpandDescriptions || ModKeybinds.VanillaBindHeld("expanddesc");
				__result = (hoveredTooltip.tipName, BuildTooltip(val, expanded));
			}
		}
	}

	private static UITooltip GetHoveredTooltip(List<RaycastResult> raycasts)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		UITooltip result = default(UITooltip);
		for (int i = 0; i < raycasts.Count; i++)
		{
			RaycastResult val = raycasts[i];
			if ((Object)(object)((RaycastResult)(ref val)).gameObject != (Object)null)
			{
				val = raycasts[i];
				if (((RaycastResult)(ref val)).gameObject.TryGetComponent<UITooltip>(ref result))
				{
					return result;
				}
			}
		}
		return null;
	}

	private static ItemInfo FindTraderItem(TraderScript trader, string displayName)
	{
		if (string.IsNullOrEmpty(displayName) || trader.items == null)
		{
			return null;
		}
		for (int i = 0; i < trader.items.Count; i++)
		{
			ItemInfo val = ((trader.items[i] != null) ? trader.items[i].info : null);
			if (val != null && val.fullName == displayName)
			{
				return val;
			}
		}
		return null;
	}

	private static string BuildTooltip(ItemInfo info, bool expanded)
	{
		string text = (info.description ?? string.Empty) + "\n\n";
		List<LiquidStack> contents = DefaultContents(info);
		text += ContentsBlock(info, contents, expanded);
		text = text + "<color=#ffffff><sprite index=0 tint=1>" + Locale.GetOther("weight") + ": " + info.weight.ToString("0.##") + "u\n";
		if (!expanded)
		{
			return text + "<color=#a2e8af><sprite index=2 tint=1><i>" + Locale.GetOther("shifttoexpand") + "</i>";
		}
		if (info.rotSpeed > 0f && info.decayMinutes > 0f)
		{
			string newValue = PlayerCamera.TimeToString(TimeSpan.FromMinutes(info.decayMinutes));
			text = text + "<color=#a2dec2><sprite index=5 tint=1>" + Locale.GetOther("decaysin").Replace("<>", newValue);
			text += DecayConditions(info);
			text += "\n";
		}
		if (info.onlyHoldInHands)
		{
			text = text + "<color=#ff8787><sprite index=6 tint=1>" + Locale.GetOther("itemonlyinhands") + "\n";
		}
		if (info.wearable)
		{
			text = text + "<color=#91d1ff><sprite index=7 tint=1>" + Locale.GetOther("itemwearable") + " \"" + info.wearSlotId + "\"\n";
			if (info.wearableArmor > 0f)
			{
				text = text + "<color=#91d1ff><sprite index=13 tint=1>" + Locale.GetOther("itemwearableprotection") + Mathf.RoundToInt(info.wearableArmor * 100f) + "%\n";
			}
			if (info.wearableIsolation > 0f)
			{
				text = text + "<color=#91d1ff><sprite index=14 tint=1>" + Locale.GetOther("itemwearableisolation") + info.wearableIsolation * 100f + "%\n";
			}
		}
		if (info.usable || IsUsableOnLimb(info, contents))
		{
			text = text + "<color=#a3a3a3><sprite index=11 tint=1>" + Locale.GetOther("itemusable");
			if (info.usable)
			{
				text = text + "<color=#a6ffaa><sprite index=6 tint=1>" + Locale.GetOther(info.usableWithLMB ? "itemusablehand" : "itemusableinventory");
			}
			if (info.usable && IsUsableOnLimb(info, contents))
			{
				text += "<color=#ffffff>/";
			}
			if (IsUsableOnLimb(info, contents))
			{
				text = text + "<color=#fffb91><sprite index=12 tint=1>" + Locale.GetOther("itemusablewound");
			}
			text += "\n";
		}
		if (info.qualities != null && info.qualities.Count > 0)
		{
			text = text + "<color=#ffc814><sprite index=26 tint=1>" + Locale.GetOther("itemqualities") + QualityList(info.qualities) + "\n";
		}
		if (info.DefaultValue() > 0)
		{
			text = text + "<color=#f6ff73><sprite index=9 tint=1>" + Locale.GetOther("itemvalue") + info.DefaultValue() + "\n";
		}
		return text.TrimEnd(new char[1] { '\n' });
	}

	private static List<LiquidStack> DefaultContents(ItemInfo info)
	{
		LiquidItemInfo val = (LiquidItemInfo)(object)((info is LiquidItemInfo) ? info : null);
		if (val != null && val.defaultContents != null && val.defaultContents.Count > 0)
		{
			return val.defaultContents;
		}
		return null;
	}

	private static string ContentsBlock(ItemInfo info, List<LiquidStack> contents, bool expanded)
	{
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_01df: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ee: Unknown result type (might be due to invalid IL or missing references)
		if (contents == null)
		{
			return string.Empty;
		}
		float num = 0f;
		for (int i = 0; i < contents.Count; i++)
		{
			num += contents[i].amount;
		}
		string text = string.Empty;
		if (Liquids.Registry.TryGetValue(contents[0].liquidId, out var value))
		{
			text = text + "<color=#" + ColorUtility.ToHtmlStringRGB(value.color) + "><sprite index=21 tint=1>";
		}
		text = text + Mathf.RoundToInt(num) + "/" + Mathf.RoundToInt(CapacityOf(info)) + "mL\n";
		List<string> list = new List<string>();
		List<string> list2 = new List<string>();
		for (int j = 0; j < contents.Count; j++)
		{
			LiquidStack val = contents[j];
			if (!Liquids.Registry.TryGetValue(val.liquidId, out var value2))
			{
				list.Add(val.liquidId + " " + Mathf.RoundToInt(val.amount) + "mL");
				list2.Add(val.liquidId + " " + Mathf.RoundToInt(val.amount) + "mL");
				continue;
			}
			string text2 = (value2.localeFromItem ? Locale.GetItem(val.liquidId) : Locale.GetOther(value2.localeName));
			string text3 = Mathf.RoundToInt(val.amount) + "mL";
			string text4 = "<color=#" + ColorUtility.ToHtmlStringRGB(value2.color) + ">" + text2 + " (" + text3 + ")<color=#" + ColorUtility.ToHtmlStringRGB(Color.Lerp(value2.color, Color.white, 0.5f)) + ">";
			list.Add(text4);
			string text5 = (value2.localeFromItem ? val.liquidId : value2.localeName) + "dsc";
			string text6 = text4 + "\n<alpha=#88>" + Locale.GetOther(text5) + "<alpha=#FF>\n";
			if (value2.qualities != null && value2.qualities.Count > 0)
			{
				text6 = text6 + "<sprite index=26 tint=1>" + Locale.GetOther("itemqualities") + QualityList(value2.GetScaledQualities(val.amount)) + "\n";
			}
			list2.Add(text6);
		}
		if (!expanded)
		{
			return string.Join(", ", list) + "\n";
		}
		return string.Join(string.Empty, list2);
	}

	private static float CapacityOf(ItemInfo info)
	{
		return ((LiquidItemInfo)(((info is LiquidItemInfo) ? info : null)?)).capacity ?? 0f;
	}

	private static string QualityList(List<CraftingQuality> qualities)
	{
		List<string> list = new List<string>();
		for (int i = 0; i < qualities.Count; i++)
		{
			list.Add(qualities[i].LocaleName + $" ({qualities[i].amount:0.#})");
		}
		return string.Join(", ", list);
	}

	private static bool IsUsableOnLimb(ItemInfo info, List<LiquidStack> contents)
	{
		if (info.usableOnLimb)
		{
			return true;
		}
		if (contents == null)
		{
			return false;
		}
		for (int i = 0; i < contents.Count; i++)
		{
			if (Liquids.Registry.TryGetValue(contents[i].liquidId, out var value) && value.healthUsable)
			{
				return true;
			}
		}
		return false;
	}

	private static string DecayConditions(ItemInfo info)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		DecayType val = (DecayType)info.decayInfo;
		bool flag = ((Enum)val).HasFlag((Enum)(object)(DecayType)1);
		bool flag2 = ((Enum)val).HasFlag((Enum)(object)(DecayType)4);
		bool flag3 = ((Enum)val).HasFlag((Enum)(object)(DecayType)2);
		if (!flag && !flag2 && !flag3)
		{
			return string.Empty;
		}
		List<string> list = new List<string>();
		if (flag3)
		{
			list.Add(Locale.GetOther("decayonlyworn"));
		}
		if (flag2)
		{
			list.Add(Locale.GetOther("decayonlymoving"));
		}
		if (flag)
		{
			list.Add(Locale.GetOther("decayonlyhasitems"));
		}
		return " (" + Locale.GetOther("decayonlyif") + string.Join(", ", list) + ")";
	}
}
