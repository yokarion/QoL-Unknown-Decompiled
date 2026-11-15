using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace QoL_Unknown;

internal class GroundContainerPickupPatcher
{
	private static ManualLogSource logger;

	private static bool initialized;

	public static void Initialize(ManualLogSource pluginLogger)
	{
		logger = pluginLogger;
		if (!initialized && RuntimePatchRegistry.PatchAllOnce("org.bepinex.plugins.qol.groundcontainerpickup", typeof(GroundContainerPickupPatcher), logger))
		{
			initialized = true;
		}
	}

	[HarmonyPatch(typeof(Body), "DoPickupCheck")]
	[HarmonyPrefix]
	private static bool DoPickupCheck_Prefix(Body __instance, Item item, bool noAlerts, ref bool __result)
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_011c: Unknown result type (might be due to invalid IL or missing references)
		if (!TryGetOpenedGroundContainerItem(__instance, item, out var containerItem))
		{
			return true;
		}
		Vector2 val = (Object.op_Implicit((Object)(object)((Component)containerItem).transform.parent) ? Vector2.op_Implicit(((Component)containerItem).transform.position) : GetPickupTarget(containerItem));
		bool flag = RaycastHit2D.op_Implicit(Physics2D.Linecast(Vector2.op_Implicit(((Component)__instance).transform.position), val, LayerMask.GetMask(new string[1] { "Ground" })));
		bool flag2 = Vector2.Distance(Vector2.op_Implicit(((Component)containerItem).transform.position), Vector2.op_Implicit(((Component)__instance).transform.position)) < 10f;
		if (flag && Object.op_Implicit((Object)(object)Physics2D.OverlapPoint(val, LayerMask.GetMask(new string[1] { "Ground" }))))
		{
			flag = false;
		}
		if (!noAlerts)
		{
			if (flag)
			{
				GameObject val2 = Object.Instantiate<GameObject>(Resources.Load<GameObject>("Special/PickupLine"), Vector2.op_Implicit(Vector2.zero), Quaternion.identity);
				LineRenderer val3 = (((Object)(object)val2 != (Object)null) ? val2.GetComponent<LineRenderer>() : null);
				if ((Object)(object)val3 != (Object)null)
				{
					val3.SetPosition(0, ((Component)__instance).transform.position);
					val3.SetPosition(1, Vector2.op_Implicit(val));
				}
				PlayerCamera main = PlayerCamera.main;
				if (main != null)
				{
					main.DoAlert(Locale.GetOther("alertitemblocked"), false);
				}
				if ((Object)(object)val2 != (Object)null)
				{
					Object.Destroy((Object)(object)val2, 0.5f);
				}
			}
			else if (!flag2)
			{
				PlayerCamera main2 = PlayerCamera.main;
				if (main2 != null)
				{
					main2.DoAlert(Locale.GetOther("alertitemfar"), false);
				}
			}
		}
		__result = !flag && flag2;
		return false;
	}

	private static bool TryGetOpenedGroundContainerItem(Body body, Item item, out Item containerItem)
	{
		containerItem = null;
		if ((Object)(object)body == (Object)null || (Object)(object)item == (Object)null || (Object)(object)PlayerCamera.main == (Object)null)
		{
			return false;
		}
		Container val = item.ParentContainer();
		if ((Object)(object)val == (Object)null || (Object)(object)PlayerCamera.main.currentContainer != (Object)(object)val)
		{
			return false;
		}
		containerItem = ((Component)val).GetComponent<Item>();
		if ((Object)(object)containerItem == (Object)null)
		{
			return false;
		}
		if (body.HoldingItem(containerItem) || body.HasWearable(containerItem))
		{
			return false;
		}
		return true;
	}

	private static Vector2 GetPickupTarget(Item item)
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		Collider2D val = (((Object)(object)item != (Object)null) ? ((Component)item).GetComponent<Collider2D>() : null);
		if (!((Object)(object)val != (Object)null))
		{
			return Vector2.op_Implicit(((Component)item).transform.position);
		}
		Bounds bounds = val.bounds;
		return Vector2.op_Implicit(((Bounds)(ref bounds)).center);
	}
}
