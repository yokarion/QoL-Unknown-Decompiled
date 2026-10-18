using System;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace QoL_Unknown;

internal static class MultiplayerHoverArbitrationPatcher
{
	private sealed class HoverCandidate : IComparable<HoverCandidate>
	{
		private readonly Item item;

		private readonly BuildingEntity building;

		private readonly SpriteRenderer renderer;

		private readonly int layerValue;

		private readonly int sortingOrder;

		private readonly float zOrder;

		private readonly int stableId;

		public bool IsBody { get; }

		private HoverCandidate(Item item, BuildingEntity building, bool isBody, SpriteRenderer renderer, int stableId)
		{
			//IL_0082: Unknown result type (might be due to invalid IL or missing references)
			this.item = item;
			this.building = building;
			this.renderer = renderer;
			IsBody = isBody;
			this.stableId = stableId;
			layerValue = int.MinValue;
			sortingOrder = int.MinValue;
			zOrder = float.NegativeInfinity;
			if ((Object)(object)renderer != (Object)null)
			{
				layerValue = SortingLayer.GetLayerValueFromID(((Renderer)renderer).sortingLayerID);
				sortingOrder = ((Renderer)renderer).sortingOrder;
				zOrder = 0f - ((Component)renderer).transform.position.z;
			}
		}

		public static HoverCandidate FromCollider(Collider2D collider, Body localBody)
		{
			if ((Object)(object)collider == (Object)null)
			{
				return null;
			}
			Item component = ((Component)collider).GetComponent<Item>();
			if ((Object)(object)component != (Object)null)
			{
				return new HoverCandidate(component, null, isBody: false, ((Component)component).GetComponent<SpriteRenderer>(), ((Object)component).GetInstanceID());
			}
			BuildingEntity component2 = ((Component)collider).GetComponent<BuildingEntity>();
			if ((Object)(object)component2 != (Object)null)
			{
				return new HoverCandidate(null, component2, isBody: false, ((Component)component2).GetComponent<SpriteRenderer>(), ((Object)component2).GetInstanceID());
			}
			Limb component3 = ((Component)collider).GetComponent<Limb>();
			if ((Object)(object)component3 != (Object)null && (Object)(object)component3.body != (Object)null)
			{
				return new HoverCandidate(null, null, isBody: true, ((Component)component3).GetComponent<SpriteRenderer>(), ((Object)component3).GetInstanceID());
			}
			Body component4 = ((Component)collider).GetComponent<Body>();
			if ((Object)(object)component4 != (Object)null)
			{
				return new HoverCandidate(null, null, isBody: true, ((Component)component4).GetComponent<SpriteRenderer>(), ((Object)component4).GetInstanceID());
			}
			return null;
		}

		public int CompareTo(HoverCandidate other)
		{
			if (other == null)
			{
				return 1;
			}
			int num = layerValue;
			int num2 = num.CompareTo(other.layerValue);
			if (num2 != 0)
			{
				return num2;
			}
			num = sortingOrder;
			int num3 = num.CompareTo(other.sortingOrder);
			if (num3 != 0)
			{
				return num3;
			}
			float num4 = zOrder;
			int num5 = num4.CompareTo(other.zOrder);
			if (num5 != 0)
			{
				return num5;
			}
			num = stableId;
			return num.CompareTo(other.stableId);
		}

		public bool Contains(Vector2 worldPos)
		{
			//IL_0055: Unknown result type (might be due to invalid IL or missing references)
			//IL_005a: Unknown result type (might be due to invalid IL or missing references)
			//IL_006b: Unknown result type (might be due to invalid IL or missing references)
			//IL_006c: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
			if (IsBody)
			{
				return false;
			}
			if ((Object)(object)item == (Object)null && (Object)(object)building == (Object)null)
			{
				return false;
			}
			if ((Object)(object)renderer != (Object)null)
			{
				Bounds bounds = ((Renderer)renderer).bounds;
				((Bounds)(ref bounds)).Expand(0.25f);
				return ((Bounds)(ref bounds)).Contains(Vector2.op_Implicit(worldPos));
			}
			Transform val = (((Object)(object)item != (Object)null) ? ((Component)item).transform : ((Component)building).transform);
			return Vector2.Distance(Vector2.op_Implicit(val.position), worldPos) <= 0.75f;
		}

		public void Apply(PlayerCamera camera, ref UsableObject usableObject)
		{
			if ((Object)(object)item != (Object)null)
			{
				usableObject = null;
				ApplyItem(camera, item);
			}
			else if ((Object)(object)building != (Object)null)
			{
				ApplyBuilding(camera, building, ref usableObject);
			}
		}

		private static void ApplyItem(PlayerCamera camera, Item hoveredItem)
		{
			//IL_0032: Unknown result type (might be due to invalid IL or missing references)
			//IL_006c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0088: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
			SetCursor(camera, 1);
			GlobalDark.main.SetTooltip(PlayerCamera.ItemHoverDescription(hoveredItem));
			SpriteRenderer component = ((Component)hoveredItem).GetComponent<SpriteRenderer>();
			((Component)camera.hoverSquare).transform.position = ((Component)hoveredItem).transform.position;
			camera.hoverSquare.sprite = (((Object)(object)component != (Object)null) ? component.sprite : null);
			((Component)camera.hoverSquare).transform.rotation = ((Component)hoveredItem).transform.rotation;
			((Component)camera.hoverSquare).transform.localScale = ((Component)hoveredItem).transform.localScale;
			camera.hoverSquare.drawMode = (SpriteDrawMode)0;
			camera.hoverSquare.size = Vector2.one;
			camera.hoverSquare.flipX = false;
			((Renderer)camera.hoverSquare).enabled = true;
		}

		private static void ApplyBuilding(PlayerCamera camera, BuildingEntity hoveredBuilding, ref UsableObject usableObject)
		{
			//IL_0033: Unknown result type (might be due to invalid IL or missing references)
			//IL_006f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0091: Unknown result type (might be due to invalid IL or missing references)
			//IL_0089: Unknown result type (might be due to invalid IL or missing references)
			//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
			//IL_0113: Unknown result type (might be due to invalid IL or missing references)
			//IL_0118: Unknown result type (might be due to invalid IL or missing references)
			//IL_0124: Unknown result type (might be due to invalid IL or missing references)
			//IL_0129: Unknown result type (might be due to invalid IL or missing references)
			SetCursor(camera, 2);
			(string, string) tooltip = (hoveredBuilding.fullNameDisplay, hoveredBuilding.description);
			SpriteRenderer component = ((Component)hoveredBuilding).GetComponent<SpriteRenderer>();
			((Component)camera.hoverSquare).transform.position = ((Component)hoveredBuilding).transform.position;
			camera.hoverSquare.sprite = (((Object)(object)component != (Object)null) ? component.sprite : null);
			camera.hoverSquare.drawMode = (SpriteDrawMode)(((Object)(object)component != (Object)null) ? ((int)component.drawMode) : 0);
			camera.hoverSquare.size = (((Object)(object)component != (Object)null) ? component.size : Vector2.one);
			camera.hoverSquare.flipX = (Object)(object)component != (Object)null && component.flipX;
			((Component)camera.hoverSquare).transform.rotation = ((Component)hoveredBuilding).transform.rotation;
			((Component)camera.hoverSquare).transform.localScale = ((Component)hoveredBuilding).transform.localScale;
			((Renderer)camera.hoverSquare).enabled = true;
			if (((Component)hoveredBuilding).TryGetComponent<UsableObject>(ref usableObject) && Vector2.Distance(Vector2.op_Implicit(((Component)camera.body).transform.position), Vector2.op_Implicit(((Component)usableObject).transform.position)) < 10f * usableObject.rangeMultiplier)
			{
				tooltip.Item2 = tooltip.Item2 + "\n\n<color=#FFFFFF><sprite index=20>" + usableObject.toggleString;
				SetCursor(camera, 4);
			}
			GlobalDark.main.SetTooltip(tooltip);
		}
	}

	private static readonly FieldInfo CursorField = AccessTools.Field(typeof(PlayerCamera), "cursor");

	private static ManualLogSource Logger;

	private static HoverCandidate latchedObject;

	private static int latchedObjectThroughFrame = -1;

	public static void Initialize(ManualLogSource logger)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Expected O, but got Unknown
		Logger = logger;
		Harmony val = new Harmony("org.bepinex.plugins.qol.multiplayer_hover_arbitration");
		val.PatchAll(typeof(MultiplayerHoverArbitrationPatcher));
	}

	[HarmonyPatch(typeof(PlayerCamera), "HandleWorldHover")]
	[HarmonyPostfix]
	[HarmonyAfter(new string[] { "CasualtiesMP" })]
	[HarmonyPriority(0)]
	private static void HandleWorldHover_Postfix(PlayerCamera __instance, ref UsableObject usableObject)
	{
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		if (!MultiplayerCompat.IsActive || (Object)(object)__instance == (Object)null || (Object)(object)__instance.body == (Object)null || !__instance.body.conscious || __instance.radialOpen || (Object)(object)GlobalDark.main == (Object)null || (Object)(object)Camera.main == (Object)null)
		{
			return;
		}
		Vector2 val = Vector2.op_Implicit(Camera.main.ScreenToWorldPoint(Input.mousePosition));
		Collider2D[] array = Physics2D.OverlapPointAll(val, LayerMask.GetMask(new string[5] { "Item", "Ground", "Descriptor", "Limb", "Body" }));
		HoverCandidate hoverCandidate = null;
		HoverCandidate hoverCandidate2 = null;
		for (int i = 0; i < array.Length; i++)
		{
			HoverCandidate hoverCandidate3 = HoverCandidate.FromCollider(array[i], __instance.body);
			if (hoverCandidate3 == null)
			{
				continue;
			}
			if (hoverCandidate3.IsBody)
			{
				if (hoverCandidate2 == null || hoverCandidate3.CompareTo(hoverCandidate2) > 0)
				{
					hoverCandidate2 = hoverCandidate3;
				}
			}
			else if (hoverCandidate == null || hoverCandidate3.CompareTo(hoverCandidate) > 0)
			{
				hoverCandidate = hoverCandidate3;
			}
		}
		if (hoverCandidate == null)
		{
			if (!TryApplyLatchedObject(__instance, ref usableObject))
			{
			}
		}
		else if (hoverCandidate2 != null && hoverCandidate2.CompareTo(hoverCandidate) > 0)
		{
			ClearLatchedObject();
		}
		else
		{
			hoverCandidate.Apply(__instance, ref usableObject);
			LatchObject(hoverCandidate);
		}
	}

	private static void SetCursor(PlayerCamera camera, int value)
	{
		CursorField?.SetValue(camera, value);
	}

	private static void LatchObject(HoverCandidate candidate)
	{
		latchedObject = candidate;
		latchedObjectThroughFrame = Time.frameCount + 4;
	}

	private static void ClearLatchedObject()
	{
		latchedObject = null;
		latchedObjectThroughFrame = -1;
	}

	private static bool TryApplyLatchedObject(PlayerCamera camera, ref UsableObject usableObject)
	{
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		if (latchedObject == null || Time.frameCount > latchedObjectThroughFrame || (Object)(object)Camera.main == (Object)null)
		{
			ClearLatchedObject();
			return false;
		}
		Vector2 worldPos = Vector2.op_Implicit(Camera.main.ScreenToWorldPoint(Input.mousePosition));
		if (!latchedObject.Contains(worldPos))
		{
			ClearLatchedObject();
			return false;
		}
		latchedObject.Apply(camera, ref usableObject);
		return true;
	}
}
