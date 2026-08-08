using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace QoL_Unknown;

internal class LineOfSightPatcher
{
	private const string DisableLineOfSightPrefKey = "DisableLineOfSight";

	private const string LineOfSightFixModuleId = "qol_vis_los";

	private const int LineOfSightUpdateIntervalFrames = 3;

	private const float HazardClearSideDistance = 2f;

	private const float HazardClearHeight = 30f;

	private static ManualLogSource Logger;

	private static bool disableLineOfSight;

	private static bool maskClearedWhileSkipped;

	private static bool forceVanillaLineOfSightRefresh;

	private static bool initialized;

	internal static bool IsLineOfSightDisabled => disableLineOfSight;

	public static void Initialize(ManualLogSource logger)
	{
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Expected O, but got Unknown
		Logger = logger;
		disableLineOfSight = PlayerPrefs.GetInt("DisableLineOfSight", 0) == 1;
		if (!initialized)
		{
			initialized = true;
			Harmony val = new Harmony("org.bepinex.plugins.qol.los.patch");
			val.PatchAll(typeof(LineOfSightPatcher));
		}
	}

	internal static void SetLineOfSightDisabled(bool disabled)
	{
		bool flag = disableLineOfSight;
		disableLineOfSight = disabled;
		PlayerPrefs.SetInt("DisableLineOfSight", disabled ? 1 : 0);
		PlayerPrefs.Save();
		RefreshLineOfSightShader(flag && !disabled);
	}

	internal static void RefreshLineOfSightShader(bool rebuildLineOfSightMask)
	{
		if (!((Object)(object)WorldGeneration.world == (Object)null) && !((Object)(object)PlayerCamera.main == (Object)null))
		{
			bool unchipped = WorldGeneration.unchipped;
			WorldGeneration.world.SetUnchipped(!unchipped);
			WorldGeneration.world.SetUnchipped(unchipped);
			if (rebuildLineOfSightMask)
			{
				forceVanillaLineOfSightRefresh = true;
				PlayerCamera.main.HandleLineOfSight();
				forceVanillaLineOfSightRefresh = false;
			}
		}
	}

	private static void ClearLineOfSightHazardsAbovePlayer(PlayerCamera camera)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)camera?.body == (Object)null)
		{
			return;
		}
		Vector2 val = Vector2.op_Implicit(((Component)camera.body).transform.position);
		float num = val.x - 2f;
		float num2 = val.x + 2f;
		float y = val.y;
		float num3 = val.y + 30f;
		for (int num4 = Item.allItems.Count - 1; num4 >= 0; num4--)
		{
			Item val2 = Item.allItems[num4];
			if (!((Object)(object)val2 == (Object)null) && !(val2.id != "skullcrusher"))
			{
				Vector2 val3 = Vector2.op_Implicit(((Component)val2).transform.position);
				if (!(val3.x < num) && !(val3.x > num2) && !(val3.y < y) && !(val3.y > num3))
				{
					Object.Destroy((Object)(object)((Component)val2).gameObject);
				}
			}
		}
	}

	[HarmonyPatch(typeof(PlayerCamera), "HandleLineOfSight")]
	[HarmonyPrefix]
	private static bool HandleLineOfSight_Prefix(PlayerCamera __instance)
	{
		if (forceVanillaLineOfSightRefresh)
		{
			maskClearedWhileSkipped = false;
			return true;
		}
		bool flag = QoLModuleManager.IsEnabled("qol_vis_los");
		if (disableLineOfSight || (flag && !WorldGeneration.lineOfSightEnabled))
		{
			VisionMask val = default(VisionMask);
			if (!maskClearedWhileSkipped && (Object)(object)__instance.LOSHole != (Object)null && __instance.LOSHole.TryGetComponent<VisionMask>(ref val))
			{
				val.ClearMask();
				maskClearedWhileSkipped = true;
			}
			return false;
		}
		maskClearedWhileSkipped = false;
		if (Time.frameCount % 3 != 0)
		{
			return false;
		}
		ClearLineOfSightHazardsAbovePlayer(__instance);
		return true;
	}
}
