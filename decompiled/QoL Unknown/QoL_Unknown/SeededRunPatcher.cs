using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace QoL_Unknown;

internal class SeededRunPatcher
{
	private struct RngScopeState
	{
		public bool Active;

		public State PreviousState;

		public Random PreviousWorldGenRng;

		public string PreviousGenerationPhase;
	}

	private sealed class SeededCoroutine : IEnumerator, IDisposable
	{
		private readonly IEnumerator inner;

		private readonly Action onCompleted;

		private State phaseState;

		private Random phaseRng;

		private readonly string phase;

		private bool completed;

		public object Current => inner.Current;

		public SeededCoroutine(IEnumerator inner, int seed, string phase, Action onCompleted)
		{
			//IL_0056: Unknown result type (might be due to invalid IL or missing references)
			//IL_002d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0032: Unknown result type (might be due to invalid IL or missing references)
			//IL_003c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0041: Unknown result type (might be due to invalid IL or missing references)
			this.inner = inner ?? throw new ArgumentNullException("inner");
			this.phase = phase;
			this.onCompleted = onCompleted;
			State state = Random.state;
			try
			{
				Random.InitState(seed);
				phaseState = Random.state;
				phaseRng = new Random(seed);
			}
			finally
			{
				Random.state = state;
			}
		}

		public bool MoveNext()
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			//IL_0079: Unknown result type (might be due to invalid IL or missing references)
			//IL_007e: Unknown result type (might be due to invalid IL or missing references)
			//IL_008e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0015: Unknown result type (might be due to invalid IL or missing references)
			State state = Random.state;
			Random worldGenRng = _worldGenRng;
			string currentGenerationPhase = _currentGenerationPhase;
			try
			{
				Random.state = phaseState;
				_worldGenRng = phaseRng;
				_currentGenerationPhase = phase;
				bool flag = inner.MoveNext();
				if (!flag && !completed)
				{
					completed = true;
					onCompleted?.Invoke();
				}
				return flag;
			}
			finally
			{
				phaseState = Random.state;
				phaseRng = _worldGenRng;
				Random.state = state;
				_worldGenRng = worldGenRng;
				_currentGenerationPhase = currentGenerationPhase;
			}
		}

		public void Reset()
		{
			throw new NotSupportedException();
		}

		public void Dispose()
		{
			(inner as IDisposable)?.Dispose();
		}
	}

	private sealed class CompletionCoroutine : IEnumerator, IDisposable
	{
		private readonly IEnumerator inner;

		private readonly Action onCompleted;

		private bool completed;

		public object Current => inner.Current;

		public CompletionCoroutine(IEnumerator inner, Action onCompleted)
		{
			this.inner = inner ?? throw new ArgumentNullException("inner");
			this.onCompleted = onCompleted;
		}

		public bool MoveNext()
		{
			bool flag = inner.MoveNext();
			if (!flag && !completed)
			{
				completed = true;
				onCompleted?.Invoke();
			}
			return flag;
		}

		public void Reset()
		{
			throw new NotSupportedException();
		}

		public void Dispose()
		{
			(inner as IDisposable)?.Dispose();
		}
	}

	private static ManualLogSource Logger;

	private const float ElderSpawnSafeRadius = 45f;

	private const float TrapSpawnVerticalBuffer = 5f;

	private const float TrapClearRadius = 12f;

	private const float SpawnLiquidClearRadius = 10f;

	private static readonly HashSet<string> TrapEntityIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
	{
		"antirad", "barbedwirefence", "beartrap", "caveticks", "coil", "drillpod", "grabberplant", "grabbershroom", "gunmine", "jumppad",
		"landmine", "overgrowntick", "radbarrel", "shadecrawler", "sidestabber", "sidestabberflip", "snowstrider", "soundcannon", "spentfuel", "spikestabber",
		"thornbackyoung", "wallbiter"
	};

	private static Random _worldGenRng;

	private static int _noiseGenStep = 0;

	private static int _distributeEntitiesCallIndex = 0;

	private static string _currentGenerationPhase = "";

	public static void Initialize(ManualLogSource logger)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Expected O, but got Unknown
		Logger = logger;
		Harmony val = new Harmony("org.bepinex.plugins.qol.seededruns");
		val.PatchAll(typeof(SeededRunPatcher));
		if (Chainloader.PluginInfos.ContainsKey("com.Jimmyking.morestructures"))
		{
			PatchCustomStructures(val);
		}
	}

	private static int GetLayerSeed()
	{
		int num = WorldGeneration.world.totalTraveled * 265443576;
		return SeedManager.CurrentSeed + num;
	}

	public static int GetCurrentLayerSeed()
	{
		return GetLayerSeed();
	}

	private static void InitializeWorldGenRng(int baseSeed, string phase)
	{
		_worldGenRng = new Random(baseSeed);
		_currentGenerationPhase = phase;
	}

	private static RngScopeState BeginRngScope(int seed, string phase, bool resetIsolatedRng = true)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		RngScopeState result = new RngScopeState
		{
			Active = true,
			PreviousState = Random.state,
			PreviousWorldGenRng = _worldGenRng,
			PreviousGenerationPhase = _currentGenerationPhase
		};
		Random.InitState(seed);
		if (resetIsolatedRng)
		{
			InitializeWorldGenRng(seed, phase);
		}
		return result;
	}

	private static RngScopeState BeginGenerationRng(int seed, string phase, bool resetIsolatedRng = true)
	{
		if (!ShouldUseScopedRng())
		{
			Random.InitState(seed);
			if (resetIsolatedRng)
			{
				InitializeWorldGenRng(seed, phase);
			}
			return default(RngScopeState);
		}
		return BeginRngScope(seed, phase, resetIsolatedRng);
	}

	private static Exception EndRngScope(Exception exception, RngScopeState state)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		if (state.Active)
		{
			try
			{
				Random.state = state.PreviousState;
				_worldGenRng = state.PreviousWorldGenRng;
				_currentGenerationPhase = state.PreviousGenerationPhase;
			}
			catch (Exception arg)
			{
				ManualLogSource logger = Logger;
				if (logger != null)
				{
					logger.LogError((object)$"[SeededRun] Failed to restore Unity RNG state: {arg}");
				}
			}
		}
		return exception;
	}

	private static void WrapSeededCoroutine(ref IEnumerator result, int seed, string phase, Action onCompleted = null)
	{
		if (result == null)
		{
			return;
		}
		if (!ShouldUseScopedRng())
		{
			Random.InitState(seed);
			InitializeWorldGenRng(seed, phase);
			if (onCompleted != null)
			{
				result = new CompletionCoroutine(result, onCompleted);
			}
		}
		else
		{
			result = new SeededCoroutine(result, seed, phase, onCompleted);
		}
	}

	private static bool ShouldUseQoLSeededWorldgen()
	{
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Invalid comparison between Unknown and I4
		if (!SeedManager.IsSeeded)
		{
			return false;
		}
		if (MultiplayerCompat.IsActive)
		{
			return false;
		}
		if ((Object)(object)WorldGeneration.world != (Object)null && (int)WorldGeneration.world.biomeOverride == 2)
		{
			return false;
		}
		return true;
	}

	private static bool ShouldUseScopedRng()
	{
		return SeedManager.SeedFormatVersion >= 2;
	}

	private static int ComposeDistributeEntitySeed(GameObject basObj, float minPerChunk, float maxPerChunk, int callIndex)
	{
		int layerSeed = GetLayerSeed();
		int stableHash = SeedManager.GetStableHash(((Object)(object)basObj != (Object)null) ? ((Object)basObj).name : string.Empty);
		int num = (int)(minPerChunk * 1000f);
		int num2 = (int)(maxPerChunk * 100f);
		return layerSeed + stableHash + num + num2 + callIndex * 486187739;
	}

	public static float GetSeededValue()
	{
		if (!ShouldUseQoLSeededWorldgen())
		{
			return Random.value;
		}
		if (_worldGenRng == null)
		{
			return Random.value;
		}
		return (float)_worldGenRng.NextDouble();
	}

	public static int GetSeededRange(int min, int max)
	{
		if (!ShouldUseQoLSeededWorldgen())
		{
			return Random.Range(min, max);
		}
		if (_worldGenRng == null)
		{
			return Random.Range(min, max);
		}
		return _worldGenRng.Next(min, max);
	}

	public static float GetSeededRange(float min, float max)
	{
		if (!ShouldUseQoLSeededWorldgen())
		{
			return Random.Range(min, max);
		}
		if (_worldGenRng == null)
		{
			return Random.Range(min, max);
		}
		return min + (float)_worldGenRng.NextDouble() * (max - min);
	}

	[HarmonyPatch(typeof(WorldGeneration), "Start")]
	[HarmonyPrefix]
	private static void WorldGen_Start_Prefix()
	{
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Invalid comparison between Unknown and I4
		if (MultiplayerCompat.IsActive)
		{
			_worldGenRng = null;
			_noiseGenStep = 0;
			_distributeEntitiesCallIndex = 0;
			return;
		}
		if ((Object)(object)WorldGeneration.world != (Object)null && (int)WorldGeneration.world.biomeOverride == 2)
		{
			_worldGenRng = null;
			_noiseGenStep = 0;
			_distributeEntitiesCallIndex = 0;
			return;
		}
		if (SeedManager.EnsureNonZeroSeedForLoadedRun())
		{
			Logger.LogInfo((object)$"[SeededRun] Migrated save had seed 0, assigned random seed {SeedManager.CurrentSeed}");
		}
		if (ShouldUseQoLSeededWorldgen() && !ShouldUseScopedRng())
		{
			Random.InitState(GetLayerSeed());
			InitializeWorldGenRng(GetLayerSeed(), "Start");
		}
	}

	[HarmonyPatch(typeof(WorldGeneration), "GenerateWorld")]
	[HarmonyPostfix]
	private static void GenerateWorld_Postfix(ref IEnumerator __result)
	{
		if (ShouldUseQoLSeededWorldgen())
		{
			_noiseGenStep = 0;
			_distributeEntitiesCallIndex = 0;
			int seed = GetLayerSeed() + 1000;
			WrapSeededCoroutine(ref __result, seed, "GenerateWorld");
		}
	}

	[HarmonyPatch(/*Could not decode attribute arguments.*/)]
	[HarmonyPrefix]
	private static void FastNoise_Ctor_Prefix(ref int seed)
	{
		if (ShouldUseQoLSeededWorldgen() && (Object)(object)WorldGeneration.world != (Object)null && WorldGeneration.world.generatingWorld)
		{
			seed = GetLayerSeed() + _noiseGenStep * 8121;
			_noiseGenStep++;
		}
	}

	[HarmonyPatch(typeof(WorldGeneration), "WorldPreprocess")]
	[HarmonyPostfix]
	private static void WorldPreprocess_Postfix(ref IEnumerator __result)
	{
		if (ShouldUseQoLSeededWorldgen())
		{
			WrapSeededCoroutine(ref __result, GetLayerSeed() + 2000, "WorldPreprocess");
		}
	}

	[HarmonyPatch(typeof(WorldGeneration), "WorldCreateBackground")]
	[HarmonyPostfix]
	private static void WorldCreateBackground_Postfix(ref IEnumerator __result)
	{
		if (ShouldUseQoLSeededWorldgen())
		{
			WrapSeededCoroutine(ref __result, GetLayerSeed() + 3000, "WorldCreateBackground");
		}
	}

	[HarmonyPatch(typeof(WorldGeneration), "WorldGenerateTerrain")]
	[HarmonyPostfix]
	private static void WorldGenerateTerrain_Postfix(ref IEnumerator __result)
	{
		if (ShouldUseQoLSeededWorldgen())
		{
			_noiseGenStep = 0;
			WrapSeededCoroutine(ref __result, GetLayerSeed() + 4000, "WorldGenerateTerrain");
		}
	}

	[HarmonyPatch(typeof(WorldGeneration), "WorldGenerateWorldBorders")]
	[HarmonyPostfix]
	private static void WorldGenerateWorldBorders_Postfix(ref IEnumerator __result)
	{
		if (ShouldUseQoLSeededWorldgen())
		{
			WrapSeededCoroutine(ref __result, GetLayerSeed() + 4500, "WorldGenerateWorldBorders");
		}
	}

	[HarmonyPatch(typeof(WorldGeneration), "WorldGenerateStructures")]
	[HarmonyPostfix]
	private static void WorldGenerateStructures_Postfix(ref IEnumerator __result)
	{
		if (ShouldUseQoLSeededWorldgen())
		{
			WrapSeededCoroutine(ref __result, GetLayerSeed() + 5000, "WorldGenerateStructures");
		}
	}

	[HarmonyPatch(typeof(WorldGeneration), "WorldPlacePlayer")]
	[HarmonyPostfix]
	private static void WorldPlacePlayer_Postfix(ref IEnumerator __result)
	{
		if (ShouldUseQoLSeededWorldgen())
		{
			WrapSeededCoroutine(ref __result, GetLayerSeed() + 6000, "WorldPlacePlayer");
		}
	}

	[HarmonyPatch(typeof(WorldGeneration), "WorldPlaceEntities")]
	[HarmonyPostfix]
	private static void WorldPlaceEntities_Postfix(ref IEnumerator __result)
	{
		if (ShouldUseQoLSeededWorldgen())
		{
			WrapSeededCoroutine(ref __result, GetLayerSeed() + 7000, "WorldPlaceEntities");
		}
	}

	[HarmonyPatch(typeof(WorldGeneration), "FinishWorldGeneration")]
	[HarmonyPostfix]
	private static void FinishWorldGeneration_RngPostfix(ref IEnumerator __result)
	{
		if (ShouldUseQoLSeededWorldgen())
		{
			WrapSeededCoroutine(ref __result, GetLayerSeed() + 8000, "FinishWorldGeneration", FinishWorldGeneration_Completed);
		}
	}

	private static void FinishWorldGeneration_Completed()
	{
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)WorldGeneration.world == (Object)null) && !((Object)(object)PlayerCamera.main == (Object)null) && !((Object)(object)PlayerCamera.main.body == (Object)null) && !ShouldSkipSpawnSafetyForFreshRunStart())
		{
			Vector2 spawnPos = Vector2.op_Implicit(((Component)PlayerCamera.main.body).transform.position);
			KeepEldersAwayFromSpawn(spawnPos, 45f);
			EnsurePlayerOnSolidGroundLikeVanilla();
			ClearNearbyLiquidsAtSpawn();
			RemoveNearbyTrapsLikeVanilla();
		}
	}

	private static bool ShouldSkipSpawnSafetyForFreshRunStart()
	{
		if (SaveSystem.loadedRun)
		{
			return false;
		}
		if ((Object)(object)WorldGeneration.world == (Object)null)
		{
			return false;
		}
		return WorldGeneration.world.totalTraveled < 50;
	}

	[HarmonyPatch(typeof(WorldGeneration), "GenerateLifePods")]
	[HarmonyPrefix]
	private static void GenerateLifePods_Prefix(float amt, out RngScopeState __state)
	{
		__state = default(RngScopeState);
		if (ShouldUseQoLSeededWorldgen())
		{
			int seed = GetLayerSeed() + 5100 + (int)(amt * 1000f);
			__state = BeginGenerationRng(seed, "GenerateLifePods");
		}
	}

	[HarmonyPatch(typeof(WorldGeneration), "GenerateLifePods")]
	[HarmonyFinalizer]
	private static Exception GenerateLifePods_Finalizer(Exception __exception, RngScopeState __state)
	{
		return EndRngScope(__exception, __state);
	}

	[HarmonyPatch(typeof(WorldGeneration), "GenerateDropCapsules")]
	[HarmonyPrefix]
	private static void GenerateDropCapsules_Prefix(float amt, out RngScopeState __state)
	{
		__state = default(RngScopeState);
		if (ShouldUseQoLSeededWorldgen())
		{
			int seed = GetLayerSeed() + 5200 + (int)(amt * 1000f);
			__state = BeginGenerationRng(seed, "GenerateDropCapsules");
		}
	}

	[HarmonyPatch(typeof(WorldGeneration), "GenerateDropCapsules")]
	[HarmonyFinalizer]
	private static Exception GenerateDropCapsules_Finalizer(Exception __exception, RngScopeState __state)
	{
		return EndRngScope(__exception, __state);
	}

	[HarmonyPatch(typeof(WorldGeneration), "GenerateCollapsedPods")]
	[HarmonyPrefix]
	private static void GenerateCollapsedPods_Prefix(float amt, out RngScopeState __state)
	{
		__state = default(RngScopeState);
		if (ShouldUseQoLSeededWorldgen())
		{
			int seed = GetLayerSeed() + 5300 + (int)(amt * 1000f);
			__state = BeginGenerationRng(seed, "GenerateCollapsedPods");
		}
	}

	[HarmonyPatch(typeof(WorldGeneration), "GenerateCollapsedPods")]
	[HarmonyFinalizer]
	private static Exception GenerateCollapsedPods_Finalizer(Exception __exception, RngScopeState __state)
	{
		return EndRngScope(__exception, __state);
	}

	[HarmonyPatch(typeof(WorldGeneration), "GenerateOres")]
	[HarmonyPrefix]
	private static void GenerateOres_Prefix(out RngScopeState __state)
	{
		__state = default(RngScopeState);
		if (ShouldUseQoLSeededWorldgen())
		{
			__state = BeginGenerationRng(GetLayerSeed() + 5400, "GenerateOres");
		}
	}

	[HarmonyPatch(typeof(WorldGeneration), "GenerateOres")]
	[HarmonyFinalizer]
	private static Exception GenerateOres_Finalizer(Exception __exception, RngScopeState __state)
	{
		return EndRngScope(__exception, __state);
	}

	[HarmonyPatch(typeof(WorldGeneration), "GenerateTree")]
	[HarmonyPrefix]
	private static void GenerateTree_Prefix(Vector2Int pos, out RngScopeState __state)
	{
		__state = default(RngScopeState);
		if (ShouldUseQoLSeededWorldgen())
		{
			int seed = GetLayerSeed() + 5500 + ((Vector2Int)(ref pos)).x * 31 + ((Vector2Int)(ref pos)).y * 17;
			__state = BeginGenerationRng(seed, "GenerateTree", resetIsolatedRng: false);
		}
	}

	[HarmonyPatch(typeof(WorldGeneration), "GenerateTree")]
	[HarmonyFinalizer]
	private static Exception GenerateTree_Finalizer(Exception __exception, RngScopeState __state)
	{
		return EndRngScope(__exception, __state);
	}

	[HarmonyPatch(typeof(WorldGeneration), "GenerateBigMushroom")]
	[HarmonyPrefix]
	private static void GenerateBigMushroom_Prefix(Vector2Int pos, out RngScopeState __state)
	{
		__state = default(RngScopeState);
		if (ShouldUseQoLSeededWorldgen())
		{
			int seed = GetLayerSeed() + 5600 + ((Vector2Int)(ref pos)).x * 31 + ((Vector2Int)(ref pos)).y * 17;
			__state = BeginGenerationRng(seed, "GenerateBigMushroom", resetIsolatedRng: false);
		}
	}

	[HarmonyPatch(typeof(WorldGeneration), "GenerateBigMushroom")]
	[HarmonyFinalizer]
	private static Exception GenerateBigMushroom_Finalizer(Exception __exception, RngScopeState __state)
	{
		return EndRngScope(__exception, __state);
	}

	[HarmonyPatch(typeof(WorldGeneration), "ApplyLayerModifiers")]
	[HarmonyPrefix]
	private static void ApplyLayerModifiers_Prefix(out RngScopeState __state)
	{
		__state = default(RngScopeState);
		if (ShouldUseQoLSeededWorldgen())
		{
			__state = BeginGenerationRng(GetLayerSeed() + 10000, "ApplyLayerModifiers");
		}
	}

	[HarmonyPatch(typeof(WorldGeneration), "ApplyLayerModifiers")]
	[HarmonyFinalizer]
	private static Exception ApplyLayerModifiers_Finalizer(Exception __exception, RngScopeState __state)
	{
		return EndRngScope(__exception, __state);
	}

	[HarmonyPatch(typeof(WorldGeneration), "DistributeEntities")]
	[HarmonyPrefix]
	private static void DistributeEntities_Prefix(GameObject basObj, float minPerChunk, float maxPerChunk, out RngScopeState __state)
	{
		__state = default(RngScopeState);
		if (ShouldUseQoLSeededWorldgen() && !((Object)(object)basObj == (Object)null))
		{
			int seed = ComposeDistributeEntitySeed(basObj, minPerChunk, maxPerChunk, _distributeEntitiesCallIndex++);
			__state = BeginGenerationRng(seed, "DistributeEntities_" + ((Object)basObj).name);
		}
	}

	[HarmonyPatch(typeof(WorldGeneration), "DistributeEntities")]
	[HarmonyFinalizer]
	private static Exception DistributeEntities_Finalizer(Exception __exception, RngScopeState __state)
	{
		return EndRngScope(__exception, __state);
	}

	[HarmonyPatch(typeof(WorldGeneration), "PlaceLiquids")]
	[HarmonyPrefix]
	private static void PlaceLiquids_Prefix(float perChunk, byte type, int maxFill, out RngScopeState __state)
	{
		__state = default(RngScopeState);
		if (ShouldUseQoLSeededWorldgen())
		{
			int seed = GetLayerSeed() + 9000 + type * 100 + maxFill;
			__state = BeginGenerationRng(seed, $"PlaceLiquids_Type{type}");
		}
	}

	[HarmonyPatch(typeof(WorldGeneration), "PlaceLiquids")]
	[HarmonyFinalizer]
	private static Exception PlaceLiquids_Finalizer(Exception __exception, RngScopeState __state)
	{
		return EndRngScope(__exception, __state);
	}

	[HarmonyPatch(typeof(TraderScript), "GenerateInventory")]
	[HarmonyPrefix]
	private static void Trader_GenerateInv_Prefix(TraderScript __instance, out RngScopeState __state)
	{
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		__state = default(RngScopeState);
		if (ShouldUseQoLSeededWorldgen() && !((Object)(object)__instance == (Object)null))
		{
			int seed = GetLayerSeed() + __instance.character * 1000 + (int)(((Component)__instance).transform.position.x * 10f) + (int)(((Component)__instance).transform.position.y * 10f);
			__state = BeginGenerationRng(seed, "TraderInventory", resetIsolatedRng: false);
		}
	}

	[HarmonyPatch(typeof(TraderScript), "GenerateInventory")]
	[HarmonyFinalizer]
	private static Exception Trader_GenerateInv_Finalizer(Exception __exception, RngScopeState __state)
	{
		return EndRngScope(__exception, __state);
	}

	[HarmonyPatch(typeof(Openable), "OnUse")]
	[HarmonyPrefix]
	private static void Openable_OnUse_Prefix(Openable __instance, out RngScopeState __state)
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		__state = default(RngScopeState);
		if (ShouldUseQoLSeededWorldgen() && !((Object)(object)__instance == (Object)null))
		{
			int num = (int)(((Component)__instance).transform.position.x * 100f) ^ (int)(((Component)__instance).transform.position.y * 100f);
			int seed = GetLayerSeed() ^ num;
			__state = BeginGenerationRng(seed, "Openable", resetIsolatedRng: false);
		}
	}

	[HarmonyPatch(typeof(WorldGeneration), "PlaceCrystals")]
	[HarmonyPrefix]
	private static void PlaceCrystals_Prefix(out RngScopeState __state)
	{
		__state = default(RngScopeState);
		if (ShouldUseQoLSeededWorldgen())
		{
			__state = BeginGenerationRng(GetLayerSeed() + 9100, "PlaceCrystals");
		}
	}

	[HarmonyPatch(typeof(WorldGeneration), "PlaceCrystals")]
	[HarmonyFinalizer]
	private static Exception PlaceCrystals_Finalizer(Exception __exception, RngScopeState __state)
	{
		return EndRngScope(__exception, __state);
	}

	[HarmonyPatch(typeof(WorldGeneration), "DistributeMiniBarrels")]
	[HarmonyPrefix]
	private static void DistributeMiniBarrels_Prefix(out RngScopeState __state)
	{
		__state = default(RngScopeState);
		if (ShouldUseQoLSeededWorldgen())
		{
			__state = BeginGenerationRng(GetLayerSeed() + 9200, "DistributeMiniBarrels");
		}
	}

	[HarmonyPatch(typeof(WorldGeneration), "DistributeMiniBarrels")]
	[HarmonyFinalizer]
	private static Exception DistributeMiniBarrels_Finalizer(Exception __exception, RngScopeState __state)
	{
		return EndRngScope(__exception, __state);
	}

	[HarmonyPatch(typeof(Openable), "OnUse")]
	[HarmonyFinalizer]
	private static Exception Openable_OnUse_Finalizer(Exception __exception, RngScopeState __state)
	{
		return EndRngScope(__exception, __state);
	}

	[HarmonyPatch(typeof(WorldGeneration), "ContinueRun")]
	[HarmonyPrefix]
	private static void ContinueRun_Prefix(out RngScopeState __state)
	{
		__state = default(RngScopeState);
		if (ShouldUseQoLSeededWorldgen())
		{
			_distributeEntitiesCallIndex = 0;
			__state = BeginGenerationRng(GetLayerSeed(), "ContinueRun");
		}
	}

	[HarmonyPatch(typeof(WorldGeneration), "ContinueRun")]
	[HarmonyFinalizer]
	private static Exception ContinueRun_Finalizer(Exception __exception, RngScopeState __state)
	{
		__exception = EndRngScope(__exception, __state);
		if (__exception == null)
		{
			return null;
		}
		if (__exception is ArgumentNullException)
		{
			return null;
		}
		if (__exception is NullReferenceException && !string.IsNullOrEmpty(__exception.Message) && __exception.Message.IndexOf("routine is null", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return null;
		}
		return __exception;
	}

	[HarmonyPatch(typeof(WorldGeneration), "RegenerateWorld")]
	[HarmonyPrefix]
	private static void RegenerateWorld_Prefix()
	{
		if (ShouldUseQoLSeededWorldgen())
		{
			_noiseGenStep = 0;
			_distributeEntitiesCallIndex = 0;
			_worldGenRng = null;
		}
	}

	[HarmonyPatch(typeof(WorldGeneration), "Clear")]
	[HarmonyPostfix]
	private static void Clear_Postfix()
	{
		if (ShouldUseQoLSeededWorldgen())
		{
			_noiseGenStep = 0;
			_distributeEntitiesCallIndex = 0;
		}
	}

	[HarmonyPatch(typeof(WorldGeneration), "InstantiateWorld")]
	[HarmonyPostfix]
	private static void InstantiateWorld_Postfix(ref IEnumerator __result)
	{
		if (ShouldUseQoLSeededWorldgen())
		{
			WrapSeededCoroutine(ref __result, GetLayerSeed() + 500, "InstantiateWorld");
		}
	}

	private static void PatchCustomStructures(Harmony harmony)
	{
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Expected O, but got Unknown
		//IL_007b: Expected O, but got Unknown
		try
		{
			Type type = AccessTools.TypeByName("More_Structures.WorldGenerationPatcher");
			if (type != null)
			{
				MethodInfo methodInfo = AccessTools.Method(type, "GenerateStructures_Postfix", (Type[])null, (Type[])null);
				if (methodInfo != null)
				{
					MethodInfo method = typeof(SeededRunPatcher).GetMethod("CustomStructure_Prefix", BindingFlags.Static | BindingFlags.NonPublic);
					MethodInfo method2 = typeof(SeededRunPatcher).GetMethod("CustomStructure_Finalizer", BindingFlags.Static | BindingFlags.NonPublic);
					harmony.Patch((MethodBase)methodInfo, new HarmonyMethod(method), (HarmonyMethod)null, (HarmonyMethod)null, new HarmonyMethod(method2), (HarmonyMethod)null);
				}
			}
		}
		catch (Exception)
		{
		}
	}

	private static void CustomStructure_Prefix(out RngScopeState __state)
	{
		__state = default(RngScopeState);
		if (ShouldUseQoLSeededWorldgen())
		{
			int seed = GetLayerSeed() + 99999;
			__state = BeginGenerationRng(seed, "CustomStructures");
		}
	}

	private static Exception CustomStructure_Finalizer(Exception __exception, RngScopeState __state)
	{
		return EndRngScope(__exception, __state);
	}

	private static void KeepEldersAwayFromSpawn(Vector2 spawnPos, float minRadius)
	{
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		Transform[] array = Object.FindObjectsOfType<Transform>();
		foreach (Transform val in array)
		{
			if ((Object)(object)val == (Object)null || (Object)(object)((Component)val).gameObject == (Object)null)
			{
				continue;
			}
			string name = ((Object)((Component)val).gameObject).name;
			if (name == "thornbackelder" || name == "thornbackelder(Clone)" || name.Contains("thornbackelder"))
			{
				Vector2 val2 = Vector2.op_Implicit(val.position);
				Vector2 val3 = val2 - spawnPos;
				if (!(((Vector2)(ref val3)).sqrMagnitude >= minRadius * minRadius) && TryFindValidElderPositionAwayFromSpawn(spawnPos, minRadius, out var pos))
				{
					val.position = new Vector3(pos.x, pos.y, val.position.z);
				}
			}
		}
	}

	private static bool TryFindValidElderPositionAwayFromSpawn(Vector2 spawnPos, float minRadius, out Vector2 pos)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		pos = spawnPos;
		WorldGeneration world = WorldGeneration.world;
		if ((Object)(object)world == (Object)null)
		{
			return false;
		}
		int num = (int)world.width / 2;
		int num2 = (int)world.height / 2;
		for (int i = 0; i < 96; i++)
		{
			float num3 = GetSeededRange(0f, 360f) * (MathF.PI / 180f);
			float seededRange = GetSeededRange(minRadius + 10f, Mathf.Min((float)world.width, (float)world.height) * 0.45f);
			int num4 = Mathf.RoundToInt(spawnPos.x + Mathf.Cos(num3) * seededRange);
			int num5 = Mathf.RoundToInt(spawnPos.y + Mathf.Sin(num3) * seededRange);
			if (num4 >= -num + 2 && num4 <= num - 2 && num5 >= -num2 + 2 && num5 <= num2 - 2 && IsAirAtWorld(num4, num5) && !IsAirAtWorld(num4, num5 - 1))
			{
				pos = new Vector2((float)num4, (float)num5);
				return true;
			}
		}
		return false;
	}

	private static void EnsurePlayerOnSolidGroundLikeVanilla()
	{
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)PlayerCamera.main == (Object)null || (Object)(object)PlayerCamera.main.body == (Object)null || (Object)(object)WorldGeneration.world == (Object)null)
		{
			return;
		}
		Vector3 position = ((Component)PlayerCamera.main.body).transform.position;
		int x = Mathf.RoundToInt(position.x);
		int y = Mathf.RoundToInt(position.y);
		if (!IsPlayerStandLocationValid(x, y))
		{
			PlayerCamera.main.body.PlaceBody();
			position = ((Component)PlayerCamera.main.body).transform.position;
			x = Mathf.RoundToInt(position.x);
			y = Mathf.RoundToInt(position.y);
			if (!IsPlayerStandLocationValid(x, y))
			{
				MovePlayerToNearestOpenLandIfEmbedded();
			}
		}
	}

	private static void RemoveNearbyTrapsLikeVanilla()
	{
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)PlayerCamera.main == (Object)null || (Object)(object)PlayerCamera.main.body == (Object)null)
		{
			return;
		}
		Vector2 val = Vector2.op_Implicit(((Component)PlayerCamera.main.body).transform.position);
		float num = 144f;
		Transform[] array = Object.FindObjectsOfType<Transform>();
		foreach (Transform val2 in array)
		{
			if (!((Object)(object)val2 == (Object)null) && !((Object)(object)((Component)val2).gameObject == (Object)null) && IsTrapObject(((Component)val2).gameObject) && !ShouldPreserveTrapCandidate(((Component)val2).gameObject))
			{
				Vector2 val3 = Vector2.op_Implicit(val2.position) - val;
				bool flag = ((Vector2)(ref val3)).sqrMagnitude <= num;
				bool flag2 = val2.position.y > val.y - 5f;
				if (flag || flag2)
				{
					Object.Destroy((Object)(object)((Component)val2).gameObject);
				}
			}
		}
	}

	private static bool ShouldPreserveTrapCandidate(GameObject obj)
	{
		Item val = (((Object)(object)obj != (Object)null) ? obj.GetComponent<Item>() : null);
		if ((Object)(object)val == (Object)null)
		{
			return false;
		}
		Body val2 = (((Object)(object)PlayerCamera.main != (Object)null) ? PlayerCamera.main.body : null);
		if ((Object)(object)val2 == (Object)null)
		{
			return false;
		}
		if (IsHeldOrWorn(val2, val))
		{
			return true;
		}
		Container val3 = val.ParentContainer();
		if ((Object)(object)val3 == (Object)null)
		{
			return false;
		}
		Item component = ((Component)val3).GetComponent<Item>();
		return IsHeldOrWorn(val2, component);
	}

	private static bool IsHeldOrWorn(Body body, Item item)
	{
		if ((Object)(object)body == (Object)null || (Object)(object)item == (Object)null)
		{
			return false;
		}
		if (body.HoldingItem(item))
		{
			return true;
		}
		ItemInfo value;
		return Item.GlobalItems.TryGetValue(item.id, out value) && value.wearable && (Object)(object)body.LimbByName(value.desiredWearLimb) != (Object)null && body.HasWearable(item);
	}

	private static void ClearNearbyLiquidsAtSpawn()
	{
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)PlayerCamera.main == (Object)null || (Object)(object)PlayerCamera.main.body == (Object)null || (Object)(object)WorldGeneration.world == (Object)null || (Object)(object)FluidManager.main == (Object)null)
		{
			return;
		}
		Vector2 val = Vector2.op_Implicit(((Component)PlayerCamera.main.body).transform.position);
		Vector2Int val2 = WorldGeneration.world.WorldToBlockPos(val);
		int num = Mathf.CeilToInt(10f);
		float num2 = 100f;
		Vector2Int val3 = default(Vector2Int);
		for (int i = ((Vector2Int)(ref val2)).x - num; i <= ((Vector2Int)(ref val2)).x + num; i++)
		{
			for (int j = ((Vector2Int)(ref val2)).y - num; j <= ((Vector2Int)(ref val2)).y + num; j++)
			{
				((Vector2Int)(ref val3))._002Ector(i, j);
				Vector2 val4 = WorldGeneration.world.BlockToWorldPos(val3);
				Vector2 val5 = val4 - val;
				if (!(((Vector2)(ref val5)).sqrMagnitude > num2) && FluidManager.main.HasLiquid(val3))
				{
					FluidManager.main.SetLiquid(i, j, (byte)0);
				}
			}
		}
	}

	private static bool IsTrapObject(GameObject obj)
	{
		if ((Object)(object)obj == (Object)null)
		{
			return false;
		}
		string item = NormalizeEntityId(((Object)obj).name);
		if (TrapEntityIds.Contains(item))
		{
			return true;
		}
		BuildingEntity component = obj.GetComponent<BuildingEntity>();
		if ((Object)(object)component == (Object)null || string.IsNullOrEmpty(component.id))
		{
			return false;
		}
		string item2 = NormalizeEntityId(component.id);
		return TrapEntityIds.Contains(item2);
	}

	private static string NormalizeEntityId(string value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return string.Empty;
		}
		string text = value.Replace("(Clone)", "").Trim();
		int num = text.LastIndexOf('/');
		if (num >= 0 && num < text.Length - 1)
		{
			text = text.Substring(num + 1);
		}
		return text;
	}

	private static void MovePlayerToNearestOpenLandIfEmbedded()
	{
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)PlayerCamera.main == (Object)null || (Object)(object)PlayerCamera.main.body == (Object)null || (Object)(object)WorldGeneration.world == (Object)null)
		{
			return;
		}
		Vector3 position = ((Component)PlayerCamera.main.body).transform.position;
		Vector2Int start = default(Vector2Int);
		((Vector2Int)(ref start))._002Ector(Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.y));
		if (!IsPlayerStandLocationValid(((Vector2Int)(ref start)).x, ((Vector2Int)(ref start)).y) && TryFindNearestOpenLand(start, 80, out var found))
		{
			Vector3 val = default(Vector3);
			((Vector3)(ref val))._002Ector((float)((Vector2Int)(ref found)).x, (float)((Vector2Int)(ref found)).y, position.z);
			((Component)PlayerCamera.main.body).transform.position = val;
			((Component)PlayerCamera.main).transform.position = new Vector3(val.x, val.y, -10f);
			if ((Object)(object)PlayerCamera.main.body.rb != (Object)null)
			{
				PlayerCamera.main.body.rb.velocity = Vector2.zero;
			}
		}
	}

	private static bool TryFindNearestOpenLand(Vector2Int start, int maxRadius, out Vector2Int found)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		found = start;
		HashSet<Vector2Int> hashSet = new HashSet<Vector2Int>();
		Queue<Vector2Int> queue = new Queue<Vector2Int>();
		queue.Enqueue(start);
		hashSet.Add(start);
		Vector2Int[] array = (Vector2Int[])(object)new Vector2Int[4]
		{
			new Vector2Int(-1, 0),
			new Vector2Int(1, 0),
			new Vector2Int(0, 1),
			new Vector2Int(0, -1)
		};
		while (queue.Count > 0)
		{
			Vector2Int val = queue.Dequeue();
			if (Mathf.Abs(((Vector2Int)(ref val)).x - ((Vector2Int)(ref start)).x) + Mathf.Abs(((Vector2Int)(ref val)).y - ((Vector2Int)(ref start)).y) > maxRadius)
			{
				continue;
			}
			if (IsPlayerStandLocationValid(((Vector2Int)(ref val)).x, ((Vector2Int)(ref val)).y))
			{
				found = val;
				return true;
			}
			for (int i = 0; i < array.Length; i++)
			{
				Vector2Int item = val + array[i];
				if (!hashSet.Contains(item) && IsInsideWorld(((Vector2Int)(ref item)).x, ((Vector2Int)(ref item)).y))
				{
					hashSet.Add(item);
					queue.Enqueue(item);
				}
			}
		}
		return false;
	}

	private static bool IsPlayerStandLocationValid(int x, int y)
	{
		if (!IsInsideWorld(x, y))
		{
			return false;
		}
		if (!IsInsideWorld(x, y + 1))
		{
			return false;
		}
		if (!IsInsideWorld(x, y - 1))
		{
			return false;
		}
		bool flag = IsAirAtWorld(x, y);
		bool flag2 = IsAirAtWorld(x, y + 1);
		bool flag3 = !IsAirAtWorld(x, y - 1);
		return flag && flag2 && flag3;
	}

	private static bool IsAirAtWorld(int x, int y)
	{
		ushort worldBlock = GetWorldBlock(x, y);
		return worldBlock == 0;
	}

	private static ushort GetWorldBlock(int x, int y)
	{
		WorldGeneration world = WorldGeneration.world;
		if ((Object)(object)world == (Object)null)
		{
			return 0;
		}
		if (!IsInsideWorld(x, y))
		{
			return 0;
		}
		int num = (int)world.width / 2;
		int num2 = (int)world.height / 2;
		int num3 = x + num;
		int num4 = y + num2;
		if (num3 < 0 || num4 < 0 || num3 >= (int)world.width || num4 >= (int)world.height)
		{
			return 0;
		}
		ushort[,] value = Traverse.Create((object)world).Field("worldBlocks").GetValue<ushort[,]>();
		if (value == null)
		{
			return 0;
		}
		return value[num3, num4];
	}

	private static bool IsInsideWorld(int x, int y)
	{
		WorldGeneration world = WorldGeneration.world;
		if ((Object)(object)world == (Object)null)
		{
			return false;
		}
		int num = (int)world.width / 2;
		int num2 = (int)world.height / 2;
		return x >= -num && x < num && y >= -num2 && y < num2;
	}
}
