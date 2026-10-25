using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace QoL_Unknown;

internal sealed class ContainerStackingView : MonoBehaviour
{
	private sealed class StackGroup
	{
		internal string Id;

		internal string BaseId;

		internal readonly List<Slot> Slots = new List<Slot>();

		internal bool Expanded;

		internal bool PinnedOpen;

		internal float LastHovered;

		internal TextMeshProUGUI Count;
	}

	private sealed class Slot
	{
		internal Item Item;

		internal InvButton Button;

		internal RectTransform Rect;

		internal CanvasGroup Canvas;

		internal Vector2 OriginalPosition;

		internal Vector2 Position;

		internal Vector2 Target;

		internal float Alpha;
	}

	private const int Columns = 3;

	private const float SlotSpacing = 66f;

	private const float CollapseDelay = 0.5f;

	private const float AnimationSpeed = 25f;

	private static readonly Vector2 Origin = new Vector2(34.5f, -34.5f);

	private PlayerCamera camera;

	private Container container;

	private readonly List<StackGroup> groups = new List<StackGroup>();

	private readonly Dictionary<string, StackGroup> groupsById = new Dictionary<string, StackGroup>(StringComparer.Ordinal);

	private readonly Dictionary<Item, Slot> slotsByItem = new Dictionary<Item, Slot>();

	private readonly Dictionary<InvButton, StackGroup> groupsByButton = new Dictionary<InvButton, StackGroup>();

	private readonly List<RaycastResult> raycasts = new List<RaycastResult>();

	private Transform[] children = Array.Empty<Transform>();

	private PointerEventData pointer;

	private EventSystem pointerEventSystem;

	private float outsideSince = -1f;

	internal float MaxScroll { get; private set; }

	internal bool IsShowing(Container value)
	{
		return (Object)(object)container != (Object)null && (Object)(object)container == (Object)(object)value;
	}

	internal void Rebuild(PlayerCamera owner)
	{
		//IL_0270: Unknown result type (might be due to invalid IL or missing references)
		//IL_0275: Unknown result type (might be due to invalid IL or missing references)
		//IL_027d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0282: Unknown result type (might be due to invalid IL or missing references)
		//IL_02af: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f5: Unknown result type (might be due to invalid IL or missing references)
		bool flag = (Object)(object)container != (Object)null && (Object)(object)container == (Object)(object)owner.currentContainer;
		Dictionary<string, StackGroup> dictionary = new Dictionary<string, StackGroup>(groupsById, StringComparer.Ordinal);
		Dictionary<Item, Slot> dictionary2 = new Dictionary<Item, Slot>(slotsByItem);
		float num = outsideSince;
		ClearState();
		if (flag)
		{
			outsideSince = num;
		}
		camera = owner;
		container = owner.currentContainer;
		((Behaviour)this).enabled = true;
		children = (Transform[])(object)new Transform[((Component)container).transform.childCount];
		for (int i = 0; i < children.Length; i++)
		{
			children[i] = ((Component)container).transform.GetChild(i);
		}
		foreach (GameObject containerUnloadButton in owner.containerUnloadButtons)
		{
			if ((Object)(object)containerUnloadButton == (Object)null)
			{
				continue;
			}
			InvButton component = containerUnloadButton.GetComponent<InvButton>();
			if ((Object)(object)component == (Object)null || component.isBody || (Object)(object)component.refItem == (Object)null)
			{
				continue;
			}
			Item refItem = component.refItem;
			if ((Object)(object)((Component)refItem).transform.parent != (Object)(object)((Component)container).transform)
			{
				continue;
			}
			string text = refItem.id ?? string.Empty;
			string baseId = text;
			if ((Object)(object)((Component)refItem).GetComponent<WaterContainerItem>() != (Object)null)
			{
				text = text + "#" + ((Object)refItem).GetInstanceID();
			}
			if (!groupsById.TryGetValue(text, out var value))
			{
				value = new StackGroup
				{
					Id = text,
					BaseId = baseId
				};
				if (flag && dictionary.TryGetValue(text, out var value2))
				{
					value.Expanded = value2.Expanded;
					value.PinnedOpen = value2.PinnedOpen;
					value.LastHovered = value2.LastHovered;
				}
				groups.Add(value);
				groupsById.Add(text, value);
			}
			RectTransform component2 = ((Component)component).GetComponent<RectTransform>();
			Slot slot = new Slot
			{
				Item = refItem,
				Button = component,
				Rect = component2,
				Canvas = containerUnloadButton.AddComponent<CanvasGroup>(),
				OriginalPosition = component2.anchoredPosition,
				Position = component2.anchoredPosition,
				Alpha = 1f
			};
			StackGroup value4;
			if (flag && dictionary2.TryGetValue(refItem, out var value3))
			{
				slot.Position = value3.Position;
				slot.Alpha = value3.Alpha;
			}
			else if (flag && dictionary.TryGetValue(text, out value4))
			{
				slot.Position = value4.Slots[0].Position;
				slot.Alpha = ((value.Slots.Count == 0) ? 1f : 0f);
			}
			value.Slots.Add(slot);
			slotsByItem.Add(refItem, slot);
			groupsByButton.Add(component, value);
		}
		foreach (StackGroup group in groups)
		{
			if (group.Slots.Count > 1)
			{
				group.Count = CreateCount(group);
			}
		}
		Layout(!flag);
	}

	private TextMeshProUGUI CreateCount(StackGroup group)
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Expected O, but got Unknown
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
		//IL_0143: Unknown result type (might be due to invalid IL or missing references)
		//IL_014b: Unknown result type (might be due to invalid IL or missing references)
		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
		//IL_014e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0156: Unknown result type (might be due to invalid IL or missing references)
		//IL_0169: Unknown result type (might be due to invalid IL or missing references)
		//IL_017f: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = new GameObject("StackCount", new Type[2]
		{
			typeof(RectTransform),
			typeof(TextMeshProUGUI)
		});
		val.transform.SetParent((Transform)(object)group.Slots[0].Rect, false);
		TextMeshProUGUI component = val.GetComponent<TextMeshProUGUI>();
		if ((Object)(object)camera.containerText != (Object)null)
		{
			((TMP_Text)component).font = ((TMP_Text)camera.containerText).font;
		}
		((TMP_Text)component).text = "(x" + group.Slots.Count + ")";
		((Graphic)component).color = Color.white;
		((TMP_Text)component).outlineColor = Color32.op_Implicit(Color.black);
		((TMP_Text)component).outlineWidth = 0.15f;
		((TMP_Text)component).fontSize = 14f;
		((TMP_Text)component).enableAutoSizing = true;
		((TMP_Text)component).fontSizeMin = 9f;
		((TMP_Text)component).fontSizeMax = 14f;
		((TMP_Text)component).enableWordWrapping = false;
		((TMP_Text)component).alignment = (TextAlignmentOptions)260;
		((Graphic)component).raycastTarget = false;
		((TMP_Text)component).alpha = (group.Expanded ? 0f : 1f);
		RectTransform rectTransform = ((TMP_Text)component).rectTransform;
		Vector2 val2 = (rectTransform.pivot = Vector2.one);
		Vector2 anchorMin = (rectTransform.anchorMax = val2);
		rectTransform.anchorMin = anchorMin;
		rectTransform.anchoredPosition = new Vector2(-2f, -2f);
		rectTransform.sizeDelta = new Vector2(60f, 20f);
		return component;
	}

	private void LateUpdate()
	{
		if ((Object)(object)camera == (Object)null || (Object)(object)container == (Object)null || (Object)(object)camera.currentContainer != (Object)(object)container || !camera.containerMenu.activeSelf)
		{
			return;
		}
		if (!QoLModuleManager.IsEnabled("qol_inv_stacking"))
		{
			Restore();
			return;
		}
		if (ContentsChanged())
		{
			camera.RepopulateContainer();
			return;
		}
		StackGroup stackGroup = HoveredGroup();
		if (stackGroup == null)
		{
			foreach (StackGroup group in groups)
			{
				if (group.Slots.Count > 1 && ContainsPointer(group))
				{
					stackGroup = group;
					break;
				}
			}
		}
		float unscaledTime = Time.unscaledTime;
		if (stackGroup != null || PointerInsideMenu())
		{
			outsideSince = -1f;
		}
		else if (outsideSince < 0f)
		{
			outsideSince = unscaledTime;
		}
		bool flag = outsideSince >= 0f && unscaledTime - outsideSince >= 0.5f;
		int num = groups.IndexOf(stackGroup);
		for (int i = 0; i < groups.Count; i++)
		{
			StackGroup stackGroup2 = groups[i];
			if (flag)
			{
				stackGroup2.Expanded = false;
				stackGroup2.PinnedOpen = false;
			}
			else
			{
				if (stackGroup2.Slots.Count < 2)
				{
					continue;
				}
				if (stackGroup2 == stackGroup)
				{
					if (!stackGroup2.Expanded)
					{
						stackGroup2.Slots[0].Button.UpdateGraphic();
					}
					stackGroup2.Expanded = true;
					stackGroup2.LastHovered = unscaledTime;
				}
				else if (stackGroup2.Expanded && i < num)
				{
					stackGroup2.PinnedOpen = true;
				}
				else if (!stackGroup2.PinnedOpen && unscaledTime - stackGroup2.LastHovered >= 0.5f)
				{
					stackGroup2.Expanded = false;
				}
			}
		}
		Layout(immediate: false);
	}

	private bool PointerInsideMenu()
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		if (raycasts.Count > 0)
		{
			RaycastResult val = raycasts[0];
			if (((RaycastResult)(ref val)).gameObject.transform.IsChildOf(camera.containerMenu.transform))
			{
				return true;
			}
		}
		RectTransform component = camera.containerMenu.GetComponent<RectTransform>();
		Canvas componentInParent = ((Component)this).GetComponentInParent<Canvas>();
		Camera val2 = (((Object)(object)componentInParent != (Object)null && (int)componentInParent.renderMode != 0) ? componentInParent.worldCamera : null);
		Vector2 val3 = default(Vector2);
		int result;
		if ((Object)(object)component != (Object)null && RectTransformUtility.ScreenPointToLocalPointInRectangle(component, Vector2.op_Implicit(Input.mousePosition), val2, ref val3))
		{
			Rect rect = component.rect;
			result = (((Rect)(ref rect)).Contains(val3) ? 1 : 0);
		}
		else
		{
			result = 0;
		}
		return (byte)result != 0;
	}

	private bool ContentsChanged()
	{
		if (((Component)container).transform.childCount != children.Length)
		{
			return true;
		}
		for (int i = 0; i < children.Length; i++)
		{
			if ((Object)(object)children[i] == (Object)null || (Object)(object)((Component)container).transform.GetChild(i) != (Object)(object)children[i])
			{
				return true;
			}
		}
		foreach (StackGroup group in groups)
		{
			foreach (Slot slot in group.Slots)
			{
				if ((Object)(object)slot.Item == (Object)null || (Object)(object)slot.Button == (Object)null || (slot.Item.id ?? string.Empty) != group.BaseId)
				{
					return true;
				}
			}
		}
		return false;
	}

	private StackGroup HoveredGroup()
	{
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Expected O, but got Unknown
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		EventSystem current = EventSystem.current;
		if ((Object)(object)current == (Object)null)
		{
			return null;
		}
		if (pointer == null || (Object)(object)pointerEventSystem != (Object)(object)current)
		{
			pointerEventSystem = current;
			pointer = new PointerEventData(current);
		}
		pointer.position = Vector2.op_Implicit(Input.mousePosition);
		raycasts.Clear();
		current.RaycastAll(pointer, raycasts);
		if (raycasts.Count == 0)
		{
			return null;
		}
		RaycastResult val = raycasts[0];
		InvButton componentInParent = ((RaycastResult)(ref val)).gameObject.GetComponentInParent<InvButton>();
		StackGroup value;
		return ((Object)(object)componentInParent != (Object)null && groupsByButton.TryGetValue(componentInParent, out value)) ? value : null;
	}

	private bool ContainsPointer(StackGroup group)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_0176: Unknown result type (might be due to invalid IL or missing references)
		if (raycasts.Count > 0)
		{
			RaycastResult val = raycasts[0];
			Transform transform = ((RaycastResult)(ref val)).gameObject.transform;
			Transform transform2 = camera.containerMenu.transform;
			if (!transform.IsChildOf(((Component)this).transform) && (Object)(object)transform != (Object)(object)transform2 && (Object)(object)transform != (Object)(object)transform2.Find("Image"))
			{
				return false;
			}
		}
		Canvas componentInParent = ((Component)this).GetComponentInParent<Canvas>();
		Camera val2 = (((Object)(object)componentInParent != (Object)null && (int)componentInParent.renderMode != 0) ? componentInParent.worldCamera : null);
		Vector2 val3 = default(Vector2);
		for (int i = ((!group.Expanded) ? 1 : 0); i < group.Slots.Count; i++)
		{
			Slot slot = group.Slots[i];
			if (!(slot.Alpha <= 0f) && ((Component)slot.Button).gameObject.activeSelf && RectTransformUtility.ScreenPointToLocalPointInRectangle(slot.Rect, Vector2.op_Implicit(Input.mousePosition), val2, ref val3))
			{
				Rect rect = slot.Rect.rect;
				((Rect)(ref rect)).xMin = ((Rect)(ref rect)).xMin - 3f;
				((Rect)(ref rect)).xMax = ((Rect)(ref rect)).xMax + 3f;
				((Rect)(ref rect)).yMin = ((Rect)(ref rect)).yMin - 3f;
				((Rect)(ref rect)).yMax = ((Rect)(ref rect)).yMax + 3f;
				if (((Rect)(ref rect)).Contains(val3))
				{
					return true;
				}
			}
		}
		return false;
	}

	private void Layout(bool immediate)
	{
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_032b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_0184: Unknown result type (might be due to invalid IL or missing references)
		float num = (immediate ? 1f : (1f - Mathf.Exp(-25f * Time.unscaledDeltaTime)));
		int index = 0;
		float num2 = 0f;
		foreach (StackGroup group in groups)
		{
			Vector2 val = PositionAt(index);
			float num3 = 0f;
			float num4 = float.PositiveInfinity;
			for (int i = 0; i < group.Slots.Count; i++)
			{
				Slot slot = group.Slots[i];
				num3 += slot.Item.condition;
				num4 = Mathf.Min(num4, slot.Item.condition);
				bool flag = i == 0 || group.Expanded;
				slot.Target = (flag ? PositionAt(index++) : val);
				slot.Position = Vector2.Lerp(slot.Position, slot.Target, num);
				Vector2 val2 = slot.Position - slot.Target;
				if (((Vector2)(ref val2)).sqrMagnitude < 0.01f)
				{
					slot.Position = slot.Target;
				}
				slot.Alpha = Mathf.Lerp(slot.Alpha, flag ? 1f : 0f, num);
				if (Mathf.Abs(slot.Alpha - (flag ? 1f : 0f)) < 0.01f)
				{
					slot.Alpha = (flag ? 1f : 0f);
				}
				slot.Rect.anchoredPosition = slot.Position;
				slot.Canvas.alpha = slot.Alpha;
				slot.Canvas.blocksRaycasts = ((!flag) ? (slot.Alpha > 0f) : (i == 0 || slot.Alpha >= 0.9f));
				slot.Canvas.interactable = slot.Canvas.blocksRaycasts;
				if (flag && !((Component)slot.Button).gameObject.activeSelf)
				{
					slot.Button.UpdateGraphic();
				}
				((Component)slot.Button).gameObject.SetActive(flag || slot.Alpha > 0f);
				if (flag || slot.Alpha > 0f)
				{
					num2 = Mathf.Max(num2, Origin.y - slot.Position.y);
				}
				if (flag)
				{
					num2 = Mathf.Max(num2, Origin.y - slot.Target.y);
				}
			}
			if ((Object)(object)group.Count != (Object)null)
			{
				if (!group.Expanded)
				{
					InvButton button = group.Slots[0].Button;
					button.itemCond.fillAmount = num3 / (float)group.Slots.Count;
					((Graphic)button.itemCond).color = PlayerCamera.ConditionToColor(num4);
				}
				((TMP_Text)group.Count).alpha = Mathf.Lerp(((TMP_Text)group.Count).alpha, group.Expanded ? 0f : 1f, num);
			}
		}
		MaxScroll = Mathf.Max(0f, num2 - 264f);
	}

	private static Vector2 PositionAt(int index)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		return Origin + new Vector2((float)(index % 3) * 66f, (float)(-(index / 3)) * 66f);
	}

	private void Restore()
	{
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		foreach (StackGroup group in groups)
		{
			if ((Object)(object)group.Count != (Object)null)
			{
				Object.Destroy((Object)(object)((Component)group.Count).gameObject);
			}
			foreach (Slot slot in group.Slots)
			{
				if (!((Object)(object)slot.Button == (Object)null))
				{
					slot.Rect.anchoredPosition = slot.OriginalPosition;
					slot.Canvas.alpha = 1f;
					slot.Canvas.blocksRaycasts = true;
					slot.Canvas.interactable = true;
					((Component)slot.Button).gameObject.SetActive(true);
					slot.Button.UpdateGraphic();
					Object.Destroy((Object)(object)slot.Canvas);
				}
			}
		}
		((Behaviour)this).enabled = false;
	}

	private void OnDisable()
	{
		if (!((Behaviour)this).enabled || (Object)(object)camera == (Object)null || (Object)(object)camera.currentContainer != (Object)(object)container || (Object)(object)camera.containerMenu == (Object)null || !camera.containerMenu.activeSelf)
		{
			ClearState();
		}
	}

	private void ClearState()
	{
		groups.Clear();
		groupsById.Clear();
		slotsByItem.Clear();
		groupsByButton.Clear();
		children = Array.Empty<Transform>();
		raycasts.Clear();
		container = null;
		outsideSince = -1f;
		MaxScroll = 0f;
	}
}
