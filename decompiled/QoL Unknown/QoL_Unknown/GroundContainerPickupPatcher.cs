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
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Expected O, but got Unknown
		logger = pluginLogger;
		if (!initialized)
		{
			initialized = true;
			Harmony val = new Harmony("org.bepinex.plugins.qol.groundcontainerpickup");
			val.PatchAll(typeof(GroundContainerPickupPatcher));
			logger.LogInfo((object)"GroundContainerPickupPatcher initialized");
		}
	}

	[HarmonyPatch(typeof(Body), "DoPickupCheck")]
	[HarmonyPrefix]
	private static bool DoPickupCheck_Prefix(Body __instance, Item item, bool noAlerts, ref bool __result)
	{
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		if (!TryGetOpenedGroundContainerItem(__instance, item, out var containerItem))
		{
			return true;
		}
		Vector2 val;
		if (!Object.op_Implicit((Object)(object)((Component)containerItem).transform.parent))
		{
			Bounds bounds = ((Component)containerItem).GetComponent<Collider2D>().bounds;
			val = Vector2.op_Implicit(((Bounds)(ref bounds)).center);
		}
		else
		{
			val = Vector2.op_Implicit(((Component)containerItem).transform.position);
		}
		Vector2 val2 = val;
		bool flag = RaycastHit2D.op_Implicit(Physics2D.Linecast(Vector2.op_Implicit(((Component)__instance).transform.position), val2, LayerMask.GetMask(new string[1] { "Ground" })));
		bool flag2 = Vector2.Distance(Vector2.op_Implicit(((Component)containerItem).transform.position), Vector2.op_Implicit(((Component)__instance).transform.position)) < 10f;
		if (flag && Object.op_Implicit((Object)(object)Physics2D.OverlapPoint(val2, LayerMask.GetMask(new string[1] { "Ground" }))))
		{
			flag = false;
		}
		if (!noAlerts)
		{
			if (flag)
			{
				GameObject val3 = Object.Instantiate<GameObject>(Resources.Load<GameObject>("Special/PickupLine"), Vector2.op_Implicit(Vector2.zero), Quaternion.identity);
				val3.GetComponent<LineRenderer>().SetPosition(0, ((Component)__instance).transform.position);
				val3.GetComponent<LineRenderer>().SetPosition(1, Vector2.op_Implicit(val2));
				PlayerCamera.main.DoAlert(Locale.GetOther("alertitemblocked"), false);
				Object.Destroy((Object)(object)val3, 0.5f);
			}
			else if (!flag2)
			{
				PlayerCamera.main.DoAlert(Locale.GetOther("alertitemfar"), false);
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
}
