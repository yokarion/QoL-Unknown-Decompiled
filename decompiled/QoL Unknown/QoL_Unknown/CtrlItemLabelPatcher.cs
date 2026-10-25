using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace QoL_Unknown;

internal class CtrlItemLabelPatcher
{
	private readonly struct ItemCandidate
	{
		internal readonly Item Item;

		internal readonly bool Contained;

		internal readonly float Distance;

		internal ItemCandidate(Item item, bool contained, float distance)
		{
			Item = item;
			Contained = contained;
			Distance = distance;
		}
	}

	internal const string ModuleId = "qol_ui_ctrlitemlabels";

	private const int LabelPoolSize = 64;

	private const float ScanRadius = 14f;

	private const int MaxScanHits = 256;

	private const float RowGap = 4f;

	private const float MaxRise = 5000f;

	private static readonly FieldInfo SpawnedLabelsField = AccessTools.Field(typeof(AltHoverScript), "spawnedLabels");

	private static readonly FieldInfo IsActiveField = AccessTools.Field(typeof(AltHoverScript), "active");

	private static readonly Collider2D[] scanHits = (Collider2D[])(object)new Collider2D[256];

	private static readonly HashSet<Item> scannedItems = new HashSet<Item>();

	private static readonly List<ItemCandidate> candidates = new List<ItemCandidate>(256);

	private static readonly List<ItemLabel> shownLabels = new List<ItemLabel>(64);

	private static readonly List<Rect> placedRects = new List<Rect>(64);

	private static AltHoverScript poolInstance;

	private static List<ItemLabel> pool;

	private static int itemLayerMask = -1;

	public static void Initialize(ManualLogSource pluginLogger)
	{
		RuntimePatchRegistry.PatchAllOnce("org.bepinex.plugins.qol.ctrlitemlabels", typeof(CtrlItemLabelPatcher), pluginLogger);
	}

	[HarmonyPatch(typeof(AltHoverScript), "Start")]
	[HarmonyPrefix]
	private static void Start_Prefix()
	{
		if (QoLModuleManager.IsEnabled("qol_ui_ctrlitemlabels"))
		{
			AltHoverScript.allocSize = 64;
		}
	}

	[HarmonyPatch(typeof(AltHoverScript), "Update")]
	[HarmonyPrefix]
	private static bool Update_Prefix(AltHoverScript __instance)
	{
		if (!QoLModuleManager.IsEnabled("qol_ui_ctrlitemlabels"))
		{
			pool = null;
			poolInstance = null;
			return true;
		}
		Refresh(__instance);
		return false;
	}

	private static void Refresh(AltHoverScript instance)
	{
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_015e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0163: Unknown result type (might be due to invalid IL or missing references)
		//IL_0168: Unknown result type (might be due to invalid IL or missing references)
		//IL_027c: Unknown result type (might be due to invalid IL or missing references)
		//IL_029e: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_030c: Unknown result type (might be due to invalid IL or missing references)
		List<ItemLabel> list = GetPool(instance);
		PlayerCamera main = PlayerCamera.main;
		Camera main2 = Camera.main;
		if (list == null || list.Count == 0 || (Object)(object)main == (Object)null || (Object)(object)main.body == (Object)null || (Object)(object)main2 == (Object)null)
		{
			return;
		}
		for (int i = 0; i < list.Count; i++)
		{
			((Component)list[i]).gameObject.SetActive(false);
		}
		if (!UpdateDisplayState(instance))
		{
			return;
		}
		Vector2 val = Vector2.op_Implicit(((Component)main.body).transform.position);
		int num = Physics2D.OverlapAreaNonAlloc(val - Vector2.one * 14f, val + Vector2.one * 14f, scanHits, GetItemLayerMask());
		candidates.Clear();
		scannedItems.Clear();
		for (int j = 0; j < num; j++)
		{
			Collider2D val2 = scanHits[j];
			scanHits[j] = null;
			if ((Object)(object)val2 == (Object)null)
			{
				continue;
			}
			Item component = ((Component)val2).GetComponent<Item>();
			if (!((Object)(object)component == (Object)null) && scannedItems.Add(component))
			{
				float num2 = Vector2.Distance(Vector2.op_Implicit(((Component)val2).transform.position), val);
				if (!(num2 >= 14f))
				{
					candidates.Add(new ItemCandidate(component, (Object)(object)((Component)component).transform.parent != (Object)null, num2));
				}
			}
		}
		candidates.Sort(CompareCandidates);
		shownLabels.Clear();
		float num3 = (Object.op_Implicit((Object)(object)main.dragItem) ? 0.2f : 1f);
		Vector2 val4 = default(Vector2);
		for (int k = 0; k < candidates.Count; k++)
		{
			if (shownLabels.Count >= list.Count)
			{
				break;
			}
			Item item = candidates[k].Item;
			SpriteRenderer component2 = ((Component)item).GetComponent<SpriteRenderer>();
			if (!((Object)(object)component2 == (Object)null) && !((Object)(object)component2.sprite == (Object)null))
			{
				ItemLabel val3 = list[shownLabels.Count];
				((Component)val3).gameObject.SetActive(true);
				val3.SetItem(item);
				((Vector2)(ref val4))._002Ector(Mathf.Round(((Component)item).transform.position.x * 8f) / 8f, Mathf.Round(((Component)item).transform.position.y * 8f) / 8f);
				((Component)val3).transform.position = main2.WorldToScreenPoint(Vector2.op_Implicit(val4));
				Color color = Utils.WithAlpha(val3.hovered ? Color.gray : Color.white, num3);
				((Graphic)val3.image).color = color;
				((Graphic)val3.icon).color = color;
				val3.hovered = false;
				shownLabels.Add(val3);
			}
		}
		LayoutLabels();
	}

	private static void LayoutLabels()
	{
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		shownLabels.Sort(CompareLabels);
		placedRects.Clear();
		for (int i = 0; i < shownLabels.Count; i++)
		{
			ItemLabel val = shownLabels[i];
			Vector2 anchoredPosition = val.rect.anchoredPosition;
			anchoredPosition.x = Mathf.Round(anchoredPosition.x);
			anchoredPosition.y = Mathf.Round(anchoredPosition.y);
			float num = anchoredPosition.y - 5000f;
			Rect rect = val.GetRect(anchoredPosition);
			bool flag;
			do
			{
				flag = false;
				for (int j = 0; j < placedRects.Count; j++)
				{
					Rect val2 = placedRects[j];
					if (((Rect)(ref rect)).Overlaps(val2))
					{
						flag = true;
						anchoredPosition.y = Mathf.Max(((Rect)(ref val2)).yMin - ((Rect)(ref rect)).height - 4f + ((Rect)(ref rect)).height * 0.5f, num);
						rect = val.GetRect(anchoredPosition);
					}
				}
			}
			while (flag);
			val.rect.anchoredPosition = anchoredPosition;
			placedRects.Add(rect);
		}
	}

	private static int CompareCandidates(ItemCandidate left, ItemCandidate right)
	{
		if (left.Contained != right.Contained)
		{
			return left.Contained ? 1 : (-1);
		}
		float distance = left.Distance;
		int num = distance.CompareTo(right.Distance);
		if (num != 0)
		{
			return num;
		}
		return ((Object)left.Item).GetInstanceID().CompareTo(((Object)right.Item).GetInstanceID());
	}

	private static int CompareLabels(ItemLabel left, ItemLabel right)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		Vector2 anchoredPosition = left.rect.anchoredPosition;
		Vector2 anchoredPosition2 = right.rect.anchoredPosition;
		int num = anchoredPosition2.y.CompareTo(anchoredPosition.y);
		if (num != 0)
		{
			return num;
		}
		int num2 = anchoredPosition.x.CompareTo(anchoredPosition2.x);
		if (num2 != 0)
		{
			return num2;
		}
		return GetItemId(left).CompareTo(GetItemId(right));
	}

	private static int GetItemId(ItemLabel label)
	{
		return ((Object)(object)label.refItem != (Object)null) ? ((Object)label.refItem).GetInstanceID() : 0;
	}

	private static bool UpdateDisplayState(AltHoverScript instance)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		KeyCode bind = KeyBinds.GetBind("altview");
		bool flag = IsActiveField != null && (bool)IsActiveField.GetValue(instance);
		if (AltHoverScript.toggleActivated)
		{
			if (Input.GetKeyDown(bind))
			{
				flag = !flag;
			}
		}
		else
		{
			flag = Input.GetKey(bind);
		}
		if (IsActiveField != null)
		{
			IsActiveField.SetValue(instance, flag);
		}
		return flag;
	}

	private static List<ItemLabel> GetPool(AltHoverScript instance)
	{
		if (pool != null && (Object)(object)poolInstance == (Object)(object)instance && SpawnedLabelsField != null)
		{
			return pool;
		}
		pool = ((SpawnedLabelsField != null) ? (SpawnedLabelsField.GetValue(instance) as List<ItemLabel>) : null);
		poolInstance = instance;
		return pool;
	}

	private static int GetItemLayerMask()
	{
		if (itemLayerMask < 0)
		{
			itemLayerMask = LayerMask.GetMask(new string[1] { "Item" });
		}
		return itemLayerMask;
	}
}
