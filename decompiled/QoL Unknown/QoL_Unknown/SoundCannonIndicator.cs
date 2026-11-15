using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using QoL_Unknown.Images;
using UnityEngine;

namespace QoL_Unknown;

internal class SoundCannonIndicator
{
	private enum PlayerSoundState
	{
		Unchipped,
		HearingDamaged,
		Normal
	}

	private sealed class CannonTracker
	{
		public SoundCannon Cannon;

		public bool WasCharging;

		public GameObject ArrowObject;

		public SpriteRenderer ArrowRenderer;
	}

	private sealed class RuntimeCleaner : MonoBehaviour
	{
		private void Update()
		{
			if (!QoLModuleManager.IsEnabled("qol_game_soundcannon_indicator"))
			{
				CleanupAllIndicators();
			}
			else
			{
				PruneDestroyedCannons();
			}
		}
	}

	private const string ModuleId = "qol_game_soundcannon_indicator";

	private const string WarningText = "..what's that noise?";

	private const float SpeechCooldown = 1f;

	private const float BodyOffsetY = 2.2f;

	private const float ForwardOffset = 1.6f;

	private const float LateralOffset = 1.25f;

	private const float ArrowScale = 0.5f;

	private const float ArrowPulseSpeed = 2.6f;

	private const float ArrowPulseAmount = 0.25f;

	private const float ArrowPixelsPerUnit = 8f;

	private static readonly Color ArrowColor = new Color(1f, 0.93f, 0.2f, 0.98f);

	private static ManualLogSource Logger;

	private static readonly Dictionary<int, CannonTracker> ActiveCannons = new Dictionary<int, CannonTracker>();

	private static readonly List<int> RemovalBuffer = new List<int>();

	private static readonly FieldInfo ChargingField = AccessTools.Field(typeof(SoundCannon), "charging");

	private static float lastSpokenAt = -999f;

	private static bool initialized;

	private static Sprite indicatorSprite;

	public static void Initialize(ManualLogSource logger)
	{
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Expected O, but got Unknown
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		if (initialized)
		{
			return;
		}
		initialized = true;
		Logger = logger;
		indicatorSprite = Helper.LoadEmbeddedSprite("statusarrow", 8f, (FilterMode)0);
		if ((Object)(object)indicatorSprite == (Object)null)
		{
			ManualLogSource logger2 = Logger;
			if (logger2 != null)
			{
				logger2.LogWarning((object)"SoundCannonIndicator: statusarrow sprite failed to load.");
			}
		}
		GameObject val = new GameObject("SoundCannonIndicatorRuntime");
		Object.DontDestroyOnLoad((Object)val);
		val.AddComponent<RuntimeCleaner>();
		new Harmony("org.bepinex.plugins.qol.soundcannonindicator").PatchAll(typeof(SoundCannonIndicator));
	}

	[HarmonyPatch(typeof(SoundCannon), "Update")]
	[HarmonyPostfix]
	private static void SoundCannon_Update_Postfix(SoundCannon __instance)
	{
		if (!QoLModuleManager.IsEnabled("qol_game_soundcannon_indicator"))
		{
			CleanupAllIndicators();
		}
		else
		{
			if ((Object)(object)__instance == (Object)null)
			{
				return;
			}
			bool chargingState = GetChargingState(__instance);
			int instanceID = ((Object)__instance).GetInstanceID();
			CannonTracker value;
			bool flag = ActiveCannons.TryGetValue(instanceID, out value);
			if (!chargingState)
			{
				if (flag)
				{
					value.WasCharging = false;
					DestroyArrow(value);
					ActiveCannons.Remove(instanceID);
				}
				return;
			}
			if (!flag)
			{
				value = new CannonTracker
				{
					Cannon = __instance,
					WasCharging = false
				};
				ActiveCannons[instanceID] = value;
			}
			else
			{
				value.Cannon = __instance;
			}
			if (chargingState && !value.WasCharging)
			{
				TrySpeakWarning();
			}
			value.WasCharging = true;
			RefreshArrow(value);
		}
	}

	private static bool GetChargingState(SoundCannon cannon)
	{
		if (MultiplayerCompat.TryGetSoundCannonCharging(cannon, out var charging))
		{
			return charging;
		}
		if (ChargingField == null || (Object)(object)cannon == (Object)null)
		{
			return false;
		}
		try
		{
			object value = ChargingField.GetValue(cannon);
			bool flag = default(bool);
			int num;
			if (value is bool)
			{
				flag = (bool)value;
				num = 1;
			}
			else
			{
				num = 0;
			}
			return (byte)((uint)num & (flag ? 1u : 0u)) != 0;
		}
		catch
		{
			return false;
		}
	}

	private static void TrySpeakWarning()
	{
		Body val = PlayerCamera.main?.body;
		if (!((Object)(object)val == (Object)null) && !((Object)(object)val.talker == (Object)null) && !(Time.unscaledTime - lastSpokenAt < 1f))
		{
			val.talker.Talk("..what's that noise?", (Limb)null, true, true);
			lastSpokenAt = Time.unscaledTime;
		}
	}

	private static PlayerSoundState GetPlayerState(Body body)
	{
		if ((Object)(object)WorldGeneration.world != (Object)null && WorldGeneration.unchipped)
		{
			return PlayerSoundState.Unchipped;
		}
		if (body.hearingLoss > 50f)
		{
			return PlayerSoundState.HearingDamaged;
		}
		return PlayerSoundState.Normal;
	}

	private static void RefreshArrow(CannonTracker tracker)
	{
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_0112: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		//IL_0153: Unknown result type (might be due to invalid IL or missing references)
		//IL_0168: Unknown result type (might be due to invalid IL or missing references)
		//IL_018f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		if (tracker == null || (Object)(object)tracker.Cannon == (Object)null)
		{
			return;
		}
		Body val = PlayerCamera.main?.body;
		if ((Object)(object)val == (Object)null)
		{
			DestroyArrow(tracker);
			return;
		}
		if (GetPlayerState(val) != PlayerSoundState.Normal)
		{
			DestroyArrow(tracker);
			return;
		}
		EnsureArrow(tracker);
		if (!((Object)(object)tracker.ArrowObject == (Object)null))
		{
			Vector2 val2 = Vector2.op_Implicit(((Component)val).transform.position);
			Vector2 val3 = Vector2.op_Implicit(((Component)tracker.Cannon).transform.position) - val2;
			if (((Vector2)(ref val3)).sqrMagnitude < 0.0001f)
			{
				val3 = Vector2.left;
			}
			Vector2 normalized = ((Vector2)(ref val3)).normalized;
			Vector2 val4 = default(Vector2);
			((Vector2)(ref val4))._002Ector(0f - normalized.y, normalized.x);
			float num = DeterministicSpread(((Object)tracker.Cannon).GetInstanceID());
			Vector2 val5 = val2 + Vector2.up * 2.2f + normalized * 1.6f + val4 * num * 1.25f;
			tracker.ArrowObject.transform.position = new Vector3(val5.x, val5.y, 0f);
			float num2 = Vector2.SignedAngle(Vector2.left, normalized);
			tracker.ArrowObject.transform.rotation = Quaternion.Euler(0f, 0f, num2);
			tracker.ArrowObject.transform.localScale = Vector3.one * (0.5f * (1f + Mathf.Sin(Time.unscaledTime * 2.6f) * 0.25f));
			SpriteRenderer componentInChildren = ((Component)val).GetComponentInChildren<SpriteRenderer>();
			if ((Object)(object)componentInChildren != (Object)null && (Object)(object)tracker.ArrowRenderer != (Object)null)
			{
				((Renderer)tracker.ArrowRenderer).sortingLayerID = ((Renderer)componentInChildren).sortingLayerID;
				((Renderer)tracker.ArrowRenderer).sortingOrder = ((Renderer)componentInChildren).sortingOrder + 12;
			}
		}
	}

	private static void EnsureArrow(CannonTracker tracker)
	{
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Expected O, but got Unknown
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)tracker.ArrowObject != (Object)null) && !((Object)(object)indicatorSprite == (Object)null))
		{
			GameObject val = new GameObject($"SoundCannonIndicator_{((Object)tracker.Cannon).GetInstanceID()}");
			SpriteRenderer val2 = val.AddComponent<SpriteRenderer>();
			val2.sprite = indicatorSprite;
			((Renderer)val2).sortingOrder = 6000;
			val2.color = ArrowColor;
			val.transform.localScale = Vector3.one * 0.5f;
			tracker.ArrowObject = val;
			tracker.ArrowRenderer = val2;
		}
	}

	private static float DeterministicSpread(int seed)
	{
		float num = (float)((seed * 1103515245 + 69420) & 0x3FF) / 1023f * 2f - 1f;
		if (Mathf.Abs(num) < 0.2f)
		{
			num = ((num >= 0f) ? 0.2f : (-0.2f));
		}
		return num;
	}

	private static void DestroyArrow(CannonTracker tracker)
	{
		if (tracker != null && !((Object)(object)tracker.ArrowObject == (Object)null))
		{
			Object.Destroy((Object)(object)tracker.ArrowObject);
			tracker.ArrowObject = null;
			tracker.ArrowRenderer = null;
		}
	}

	private static void CleanupAllIndicators()
	{
		foreach (KeyValuePair<int, CannonTracker> activeCannon in ActiveCannons)
		{
			DestroyArrow(activeCannon.Value);
		}
		ActiveCannons.Clear();
	}

	private static bool IsCannonAlive(CannonTracker tracker)
	{
		if (tracker == null || (Object)(object)tracker.Cannon == (Object)null)
		{
			return false;
		}
		GameObject gameObject = ((Component)tracker.Cannon).gameObject;
		if ((Object)(object)gameObject != (Object)null)
		{
			return gameObject.activeInHierarchy;
		}
		return false;
	}

	private static void PruneDestroyedCannons()
	{
		if (ActiveCannons.Count == 0)
		{
			return;
		}
		RemovalBuffer.Clear();
		foreach (KeyValuePair<int, CannonTracker> activeCannon in ActiveCannons)
		{
			CannonTracker value = activeCannon.Value;
			if (!IsCannonAlive(value))
			{
				DestroyArrow(value);
				RemovalBuffer.Add(activeCannon.Key);
			}
		}
		for (int i = 0; i < RemovalBuffer.Count; i++)
		{
			ActiveCannons.Remove(RemovalBuffer[i]);
		}
	}
}
