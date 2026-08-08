using System.Collections;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace QoL_Unknown;

internal class EarthquakePatcher : MonoBehaviour
{
	private static ManualLogSource Logger;

	private static EarthquakePatcher _instance;

	public float minDelay = 500f;

	public float maxDelay = 1500f;

	public float minDuration = 10f;

	public float maxDuration = 22f;

	public float warningTime = 6f;

	public int shiftRadius = 40;

	public int shiftsPerTick = 20;

	public float highHpCutoff = 700f;

	private float _delay;

	private float _quakeTime;

	private bool _busy;

	private Vector2Int _plateDir;

	private float _plateDirTimer;

	public float Intensity { get; private set; }

	public bool IsQuaking => _quakeTime > 0f;

	public static void Initialize(ManualLogSource logger)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		Logger = logger;
		new Harmony("org.bepinex.plugins.qol.earthquake").PatchAll(typeof(EarthquakePatcher));
		Logger.LogInfo((object)"EarthquakePatcher initialized");
	}

	[HarmonyPatch(typeof(WorldGeneration), "Start")]
	[HarmonyPostfix]
	private static void WorldGen_Start_Postfix()
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Expected O, but got Unknown
		if ((Object)(object)_instance != (Object)null)
		{
			Object.Destroy((Object)(object)((Component)_instance).gameObject);
		}
		GameObject val = new GameObject("EarthquakePatcher");
		Object.DontDestroyOnLoad((Object)(object)val);
		_instance = val.AddComponent<EarthquakePatcher>();
	}

	[HarmonyPatch(typeof(WorldGeneration), "OnDestroy")]
	[HarmonyPostfix]
	private static void WorldGen_OnDestroy_Postfix()
	{
		if (!((Object)(object)_instance == (Object)null))
		{
			Object.Destroy((Object)(object)((Component)_instance).gameObject);
			_instance = null;
		}
	}

	[HarmonyPatch(typeof(WorldGeneration), "Update")]
	[HarmonyPrefix]
	private static void WorldGen_Update_Prefix(WorldGeneration __instance)
	{
		__instance.earthquakeIntensity = 0f;
		__instance.earthquakeDelay = 99999f;
		__instance.earthquakeTime = 0f;
		__instance.earthquakeSource.volume = 0f;
	}

	private void Awake()
	{
		_delay = Random.Range(minDelay, maxDelay);
	}

	private void Update()
	{
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)WorldGeneration.world == (Object)null) && !((Object)(object)PlayerCamera.main?.body == (Object)null) && WorldGeneration.world.worldExists)
		{
			if (Input.GetKeyDown((KeyCode)107))
			{
				_delay = 0f;
			}
			_delay -= Time.deltaTime;
			if (_delay <= 0f && !_busy && (int)WorldGeneration.world.biomeOverride == 0 && !PlayerCamera.main.body.sleeping)
			{
				((MonoBehaviour)this).StartCoroutine(QuakeSequence());
			}
			Intensity = Mathf.MoveTowards(Intensity, (_quakeTime > 0f) ? 1f : 0f, Time.deltaTime * 0.12f);
			_quakeTime = Mathf.Max(0f, _quakeTime - Time.deltaTime);
			if (Intensity > 0f)
			{
				ApplyQuakeEffects();
			}
			WorldGeneration.world.earthquakeSource.volume = Intensity;
		}
	}

	private IEnumerator QuakeSequence()
	{
		_busy = true;
		float elapsed = 0f;
		while (elapsed < warningTime)
		{
			elapsed += Time.deltaTime;
			float t = elapsed / warningTime;
			if (Random.value / Time.deltaTime < 3f)
			{
				PlayerCamera.main.shaker.Shake(t * 5f);
			}
			yield return null;
		}
		_quakeTime = Random.Range(minDuration, maxDuration);
		Time.timeScale = 1f;
		_delay = Random.Range(minDelay, maxDelay);
		_busy = false;
	}

	private void ApplyQuakeEffects()
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0151: Unknown result type (might be due to invalid IL or missing references)
		//IL_0156: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_018b: Unknown result type (might be due to invalid IL or missing references)
		Body body = PlayerCamera.main.body;
		Vector2 origin = Vector2.op_Implicit(((Component)body).transform.position);
		if (Random.value / Time.deltaTime < 8f)
		{
			PlayerCamera.main.shaker.Shake(Intensity * 20f);
			if (body.standing)
			{
				Rigidbody2D rb = body.rb;
				rb.velocity += Random.insideUnitCircle * Intensity * 10f * (body.grounded ? 2.5f : 1f);
			}
			else
			{
				Vector2 val = Random.insideUnitCircle * Intensity * 10f;
				Limb[] limbs = body.limbs;
				foreach (Limb val2 in limbs)
				{
					Rigidbody2D rb2 = val2.rb;
					rb2.velocity += val;
				}
			}
		}
		if (Intensity > 0.5f)
		{
			body.eyeScareTime = 1f;
		}
		_plateDirTimer -= Time.deltaTime;
		if (_plateDirTimer <= 0f)
		{
			_plateDir = PickPlateDir();
			_plateDirTimer = Random.Range(0.3f, 0.6f);
		}
		int num = Mathf.RoundToInt((float)shiftsPerTick * Intensity);
		for (int j = 0; j < num; j++)
		{
			DoBlockOp(origin);
		}
	}

	private void DoBlockOp(Vector2 origin)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
		WorldGeneration world = WorldGeneration.world;
		Vector2Int val = world.WorldToBlockPos(origin + Random.insideUnitCircle * Random.Range(3f, (float)shiftRadius));
		if (!InBounds(world, val))
		{
			return;
		}
		ushort block = world.GetBlock(val);
		if (block == 0 || world.GetBlockInfo(block).health > highHpCutoff)
		{
			return;
		}
		float value = Random.value;
		if (value < 0.93f)
		{
			Vector2Int val2 = val + _plateDir;
			if (InBounds(world, val2) && world.GetBlock(val2) == 0)
			{
				world.SetBlock(val, (ushort)0);
				world.SetBlock(val2, block);
				return;
			}
			Vector2Int val3 = default(Vector2Int);
			((Vector2Int)(ref val3))._002Ector(-((Vector2Int)(ref _plateDir)).y, ((Vector2Int)(ref _plateDir)).x);
			Vector2Int val4 = val + val3;
			if (InBounds(world, val4) && world.GetBlock(val4) == 0)
			{
				world.SetBlock(val, (ushort)0);
				world.SetBlock(val4, block);
			}
		}
		else
		{
			world.SetBlock(val, (ushort)0);
		}
	}

	private static bool InBounds(WorldGeneration w, Vector2Int p)
	{
		return ((Vector2Int)(ref p)).x > 1 && ((Vector2Int)(ref p)).x < w.width - 2 && ((Vector2Int)(ref p)).y > 1 && ((Vector2Int)(ref p)).y < w.height - 2;
	}

	private static Vector2Int PickPlateDir()
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		float value = Random.value;
		if (value < 0.175f)
		{
			return new Vector2Int(1, 0);
		}
		if (value < 0.35f)
		{
			return new Vector2Int(-1, 0);
		}
		if (value < 0.575f)
		{
			return new Vector2Int(1, -1);
		}
		if (value < 0.8f)
		{
			return new Vector2Int(-1, -1);
		}
		if (value < 0.9f)
		{
			return new Vector2Int(0, -1);
		}
		return new Vector2Int(0, 1);
	}
}
