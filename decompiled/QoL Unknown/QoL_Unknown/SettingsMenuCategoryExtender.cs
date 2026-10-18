using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace QoL_Unknown;

internal sealed class SettingsMenuCategoryExtender : MonoBehaviour
{
	private const int BuiltInTabCount = 5;

	private const float MinimumInterButtonGap = 2f;

	private readonly List<Button> customButtons = new List<Button>();

	private readonly Dictionary<Button, int> buttonCategoryIndices = new Dictionary<Button, int>();

	private readonly List<Vector2> builtInAnchoredPositions = new List<Vector2>();

	private readonly List<Vector2> builtInSizes = new List<Vector2>();

	private SettingsMenu menu;

	private int activeCategoryIndex;

	private bool capturedBuiltInLayout;

	internal static void EnsureAttached(SettingsMenu menu)
	{
		if (!((Object)(object)menu == (Object)null))
		{
			SettingsMenuCategoryExtender settingsMenuCategoryExtender = ((Component)menu).GetComponent<SettingsMenuCategoryExtender>();
			if ((Object)(object)settingsMenuCategoryExtender == (Object)null)
			{
				settingsMenuCategoryExtender = ((Component)menu).gameObject.AddComponent<SettingsMenuCategoryExtender>();
			}
			settingsMenuCategoryExtender.Initialize(menu);
		}
	}

	internal static void RefreshLiveMenu()
	{
		if (!((Object)(object)SettingsMenu.instance == (Object)null))
		{
			EnsureAttached(SettingsMenu.instance);
			((Component)SettingsMenu.instance).GetComponent<SettingsMenuCategoryExtender>()?.RefreshVisibleTab();
		}
	}

	internal void Initialize(SettingsMenu settingsMenu)
	{
		menu = settingsMenu;
		if (!((Object)(object)menu == (Object)null))
		{
			if (menu.buttons == null)
			{
				menu.buttons = new List<Button>();
			}
			CaptureBuiltInLayoutIfNeeded();
			RegisterBuiltInButtons();
			RebuildButtons();
			ApplyButtonSprites();
		}
	}

	internal void OnTabSelected(SettingCategory category)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected I4, but got Unknown
		activeCategoryIndex = (int)category;
		ApplyButtonSprites();
	}

	internal void RefreshVisibleTab()
	{
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)menu == (Object)null))
		{
			Vector2 anchoredPosition = (((Object)(object)menu.content != (Object)null) ? menu.content.anchoredPosition : Vector2.zero);
			RebuildButtons();
			menu.SelectTab(activeCategoryIndex);
			if ((Object)(object)menu.content != (Object)null)
			{
				menu.content.anchoredPosition = anchoredPosition;
			}
		}
	}

	private void RebuildButtons()
	{
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_0144: Unknown result type (might be due to invalid IL or missing references)
		//IL_0152: Unknown result type (might be due to invalid IL or missing references)
		//IL_0157: Unknown result type (might be due to invalid IL or missing references)
		//IL_0198: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a2: Expected O, but got Unknown
		//IL_01b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bc: Expected O, but got Unknown
		RemoveCustomButtons();
		buttonCategoryIndices.Clear();
		RestoreBuiltInLayout();
		RegisterBuiltInButtons();
		List<ModOptionCategoryEntry> customCategories = ModOptionsMenu.GetCustomCategories();
		if ((Object)(object)menu == (Object)null || menu.buttons == null || menu.buttons.Count == 0 || customCategories.Count == 0)
		{
			return;
		}
		Button val = menu.buttons.LastOrDefault();
		object obj;
		if (!((Object)(object)val != (Object)null))
		{
			obj = null;
		}
		else
		{
			Transform transform = ((Component)val).transform;
			obj = ((transform is RectTransform) ? transform : null);
		}
		RectTransform val2 = (RectTransform)obj;
		if ((Object)(object)val == (Object)null || (Object)(object)val2 == (Object)null)
		{
			return;
		}
		float builtInSpacing = GetBuiltInSpacing();
		Transform parent = ((Component)val).transform.parent;
		Vector2 anchoredPosition = val2.anchoredPosition;
		for (int i = 0; i < customCategories.Count; i++)
		{
			ModOptionCategoryEntry modOptionCategoryEntry = customCategories[i];
			GameObject val3 = Object.Instantiate<GameObject>(((Component)val).gameObject, parent, false);
			((Object)val3).name = "SettingsTab_" + modOptionCategoryEntry.DisplayName.Replace(' ', '_');
			Transform transform2 = val3.transform;
			RectTransform val4 = (RectTransform)(object)((transform2 is RectTransform) ? transform2 : null);
			if ((Object)(object)val4 != (Object)null)
			{
				val4.anchoredPosition = anchoredPosition + new Vector2(builtInSpacing * (float)(i + 1), 0f);
			}
			Button component = val3.GetComponent<Button>();
			if ((Object)(object)component == (Object)null)
			{
				Object.Destroy((Object)(object)val3);
				continue;
			}
			int categoryIndex = modOptionCategoryEntry.CategoryIndex;
			component.onClick = new ButtonClickedEvent();
			((UnityEvent)component.onClick).AddListener((UnityAction)delegate
			{
				menu.SelectTab(categoryIndex);
			});
			TextMeshProUGUI componentInChildren = val3.GetComponentInChildren<TextMeshProUGUI>(true);
			if ((Object)(object)componentInChildren != (Object)null)
			{
				((TMP_Text)componentInChildren).text = modOptionCategoryEntry.DisplayName;
				Component component2 = ((Component)componentInChildren).GetComponent("UILocalizer");
				if ((Object)(object)component2 != (Object)null)
				{
					Object.Destroy((Object)(object)component2);
				}
				NormalizeTabLabel((TMP_Text)(object)componentInChildren);
			}
			menu.buttons.Add(component);
			customButtons.Add(component);
			buttonCategoryIndices[component] = categoryIndex;
		}
		ReflowButtonsIntoOriginalBand();
	}

	private void RemoveCustomButtons()
	{
		if ((Object)(object)menu != (Object)null && menu.buttons != null)
		{
			for (int i = 0; i < customButtons.Count; i++)
			{
				menu.buttons.Remove(customButtons[i]);
			}
		}
		for (int j = 0; j < customButtons.Count; j++)
		{
			Button val = customButtons[j];
			if ((Object)(object)val != (Object)null)
			{
				buttonCategoryIndices.Remove(val);
				Object.Destroy((Object)(object)((Component)val).gameObject);
			}
		}
		customButtons.Clear();
	}

	private void RegisterBuiltInButtons()
	{
		int num = Mathf.Min(5, menu.buttons.Count);
		for (int i = 0; i < num; i++)
		{
			Button val = menu.buttons[i];
			if ((Object)(object)val != (Object)null)
			{
				buttonCategoryIndices[val] = i;
			}
		}
	}

	private float GetBuiltInSpacing()
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		if (builtInAnchoredPositions.Count < 2)
		{
			return 116f;
		}
		float num = builtInAnchoredPositions[1].x - builtInAnchoredPositions[0].x;
		return (Mathf.Abs(num) > 0.01f) ? num : 116f;
	}

	private void ApplyButtonSprites()
	{
		if ((Object)(object)menu == (Object)null || menu.buttons == null)
		{
			return;
		}
		for (int i = 0; i < menu.buttons.Count; i++)
		{
			Button val = menu.buttons[i];
			if (!((Object)(object)val == (Object)null))
			{
				Image component = ((Component)val).GetComponent<Image>();
				if ((Object)(object)component != (Object)null)
				{
					int value;
					bool flag = buttonCategoryIndices.TryGetValue(val, out value) && value == activeCategoryIndex;
					component.sprite = (flag ? menu.buttonOpen : menu.buttonClosed);
				}
			}
		}
	}

	private void CaptureBuiltInLayoutIfNeeded()
	{
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		if (capturedBuiltInLayout || (Object)(object)menu == (Object)null || menu.buttons == null)
		{
			return;
		}
		builtInAnchoredPositions.Clear();
		builtInSizes.Clear();
		int num = Mathf.Min(5, menu.buttons.Count);
		for (int i = 0; i < num; i++)
		{
			Button val = menu.buttons[i];
			object obj;
			if (!((Object)(object)val != (Object)null))
			{
				obj = null;
			}
			else
			{
				Transform transform = ((Component)val).transform;
				obj = ((transform is RectTransform) ? transform : null);
			}
			RectTransform val2 = (RectTransform)obj;
			if (!((Object)(object)val2 == (Object)null))
			{
				builtInAnchoredPositions.Add(val2.anchoredPosition);
				builtInSizes.Add(val2.sizeDelta);
			}
		}
		capturedBuiltInLayout = builtInAnchoredPositions.Count > 0;
	}

	private void RestoreBuiltInLayout()
	{
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		if (!capturedBuiltInLayout || (Object)(object)menu == (Object)null || menu.buttons == null)
		{
			return;
		}
		int num = Mathf.Min(Mathf.Min(5, menu.buttons.Count), builtInAnchoredPositions.Count);
		for (int i = 0; i < num; i++)
		{
			object obj;
			if (!((Object)(object)menu.buttons[i] != (Object)null))
			{
				obj = null;
			}
			else
			{
				Transform transform = ((Component)menu.buttons[i]).transform;
				obj = ((transform is RectTransform) ? transform : null);
			}
			RectTransform val = (RectTransform)obj;
			if (!((Object)(object)val == (Object)null))
			{
				val.anchoredPosition = builtInAnchoredPositions[i];
				val.sizeDelta = builtInSizes[i];
			}
		}
	}

	private void ReflowButtonsIntoOriginalBand()
	{
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_0163: Unknown result type (might be due to invalid IL or missing references)
		//IL_0154: Unknown result type (might be due to invalid IL or missing references)
		//IL_0168: Unknown result type (might be due to invalid IL or missing references)
		//IL_016e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0175: Unknown result type (might be due to invalid IL or missing references)
		//IL_018f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0199: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)menu == (Object)null || menu.buttons == null || menu.buttons.Count == 0 || builtInAnchoredPositions.Count == 0)
		{
			return;
		}
		int builtInCount = Mathf.Min(5, builtInAnchoredPositions.Count);
		int count = menu.buttons.Count;
		float num = builtInAnchoredPositions[0].x - builtInSizes[0].x * 0.5f;
		float tabStripRightEdge = GetTabStripRightEdge(num, builtInCount);
		float num2 = Mathf.Max(0f, tabStripRightEdge - num);
		float num3 = ((count > 1) ? 2f : 0f);
		float num4 = ((count > 0) ? Mathf.Max(1f, (num2 - num3 * (float)(count - 1)) / (float)count) : num2);
		float num5 = num;
		for (int i = 0; i < count; i++)
		{
			Button val = menu.buttons[i];
			object obj;
			if (!((Object)(object)val != (Object)null))
			{
				obj = null;
			}
			else
			{
				Transform transform = ((Component)val).transform;
				obj = ((transform is RectTransform) ? transform : null);
			}
			RectTransform val2 = (RectTransform)obj;
			if (!((Object)(object)val2 == (Object)null))
			{
				Vector2 val3 = ((i < builtInSizes.Count) ? builtInSizes[i] : builtInSizes[builtInSizes.Count - 1]);
				val2.sizeDelta = new Vector2(num4, val3.y);
				val2.anchoredPosition = new Vector2(num5 + num4 * 0.5f, val2.anchoredPosition.y);
				num5 += num4 + num3;
				TextMeshProUGUI componentInChildren = ((Component)val).GetComponentInChildren<TextMeshProUGUI>(true);
				if ((Object)(object)componentInChildren != (Object)null)
				{
					NormalizeTabLabel((TMP_Text)(object)componentInChildren);
				}
			}
		}
	}

	private float GetTabStripRightEdge(float firstLeft, int builtInCount)
	{
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		object obj;
		if (!((Object)(object)menu.buttons[0] != (Object)null))
		{
			obj = null;
		}
		else
		{
			Transform parent = ((Component)menu.buttons[0]).transform.parent;
			obj = ((parent is RectTransform) ? parent : null);
		}
		RectTransform val = (RectTransform)obj;
		if ((Object)(object)val == (Object)null)
		{
			return builtInAnchoredPositions[builtInCount - 1].x + builtInSizes[builtInCount - 1].x * 0.5f;
		}
		Rect rect = val.rect;
		float num = firstLeft - ((Rect)(ref rect)).xMin;
		rect = val.rect;
		return ((Rect)(ref rect)).xMax - num;
	}

	private void NormalizeTabLabel(TMP_Text label)
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)label == (Object)null))
		{
			Transform transform = label.transform;
			RectTransform val = (RectTransform)(object)((transform is RectTransform) ? transform : null);
			if ((Object)(object)val != (Object)null)
			{
				val.anchorMin = Vector2.zero;
				val.anchorMax = Vector2.one;
				val.pivot = new Vector2(0.5f, 0.5f);
				val.offsetMin = Vector2.zero;
				val.offsetMax = Vector2.zero;
				val.anchoredPosition = Vector2.zero;
			}
			label.alignment = (TextAlignmentOptions)514;
			label.enableAutoSizing = false;
		}
	}
}
