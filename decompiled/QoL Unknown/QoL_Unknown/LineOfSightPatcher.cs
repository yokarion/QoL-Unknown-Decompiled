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

	private static readonly FieldRef<PlayerCamera, float> SoundCannonWarnTimeRef = AccessTools.FieldRefAccess<PlayerCamera, float>("soundCannonWarnTime");

	private static readonly FieldRef<PlayerCamera, Vector2> SoundCannonPosRef = AccessTools.FieldRefAccess<PlayerCamera, Vector2>("soundCannonPos");

	private static bool disableLineOfSight;

	private static bool maskClearedWhileSkipped;

	private static bool forceVanillaLineOfSightRefresh;

	private static bool initialized;

	internal static bool IsLineOfSightDisabled => disableLineOfSight;

	public static void Initialize(ManualLogSource logger)
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		Logger = logger;
		disableLineOfSight = PlayerPrefs.GetInt("DisableLineOfSight", 0) == 1;
		if (!initialized)
		{
			initialized = true;
			new Harmony("org.bepinex.plugins.qol.los.patch").PatchAll(typeof(LineOfSightPatcher));
		}
	}

	internal static void SetLineOfSightDisabled(bool disabled)
	{
		bool num = disableLineOfSight;
		disableLineOfSight = disabled;
		PlayerPrefs.SetInt("DisableLineOfSight", disabled ? 1 : 0);
		PlayerPrefs.Save();
		RefreshLineOfSightShader(num && !disabled);
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
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
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

	private static void UpdateSoundCannonWarn(PlayerCamera camera)
	{
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_0112: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)camera.soundCannonWarn == (Object)null) && !((Object)(object)camera.body == (Object)null))
		{
			float num = SoundCannonWarnTimeRef.Invoke(camera) - Time.deltaTime;
			SoundCannonWarnTimeRef.Invoke(camera) = num;
			if (num > 0f)
			{
				float num2 = num * 0.2f;
				camera.soundCannonWarn.color = Color.Lerp(camera.soundCannonWarn.color, new Color(1f, num2, num2, 0.3f + (1f - num2) * 0.7f), Time.deltaTime * 8f);
				Transform transform = ((Component)camera.soundCannonWarn).transform;
				Vector2 down = Vector2.down;
				Vector2 val = SoundCannonPosRef.Invoke(camera) - Vector2.op_Implicit(((Component)camera.body).transform.position);
				transform.eulerAngles = new Vector3(0f, 0f, Vector2.SignedAngle(down, ((Vector2)(ref val)).normalized));
			}
			else
			{
				camera.soundCannonWarn.color = Color.Lerp(camera.soundCannonWarn.color, Color.clear, Time.deltaTime * 8f);
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
			UpdateSoundCannonWarn(__instance);
			return false;
		}
		maskClearedWhileSkipped = false;
		if (Time.frameCount % 3 != 0)
		{
			UpdateSoundCannonWarn(__instance);
			return false;
		}
		ClearLineOfSightHazardsAbovePlayer(__instance);
		return true;
	}
}
