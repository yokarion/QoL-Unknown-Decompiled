using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace QoL_Unknown;

internal static class MultiplayerCompat
{
	internal enum NativeCraftState
	{
		Denied,
		Pending,
		Accepted
	}

	private sealed class InventoryMutationScope : IDisposable
	{
		private readonly bool hadFlag;

		private readonly object previous;

		private bool disposed;

		public InventoryMutationScope(bool hadFlag, object previous)
		{
			this.hadFlag = hadFlag;
			this.previous = previous;
		}

		public void Dispose()
		{
			if (disposed)
			{
				return;
			}
			disposed = true;
			try
			{
				if (hadFlag && inventoryActionFlagField != null)
				{
					inventoryActionFlagField.SetValue(null, previous);
				}
			}
			catch
			{
			}
		}
	}

	private sealed class InventoryRestoreChunkAssembly
	{
		public readonly string RequestId;

		public readonly byte[][] Chunks;

		public readonly int TotalLength;

		public int ReceivedChunkCount;

		public int ReceivedLength;

		public float LastReceivedTime;

		public InventoryRestoreChunkAssembly(string requestId, int chunkCount, int totalLength, float receivedTime)
		{
			RequestId = requestId;
			Chunks = new byte[chunkCount][];
			TotalLength = totalLength;
			LastReceivedTime = receivedTime;
		}
	}

	private sealed class NullScope : IDisposable
	{
		public static readonly NullScope Instance = new NullScope();

		public void Dispose()
		{
		}
	}

	private sealed class VanillaSaveSnapshotScope : IDisposable
	{
		private readonly bool hadPath;

		private readonly object previousPath;

		private readonly bool hadForce;

		private readonly object previousForce;

		private bool disposed;

		public VanillaSaveSnapshotScope(bool hadPath, object previousPath, bool hadForce, object previousForce)
		{
			this.hadPath = hadPath;
			this.previousPath = previousPath;
			this.hadForce = hadForce;
			this.previousForce = previousForce;
		}

		public void Dispose()
		{
			if (disposed)
			{
				return;
			}
			disposed = true;
			try
			{
				if (hadPath && saveDataPathReplacementField != null)
				{
					saveDataPathReplacementField.SetValue(null, previousPath);
				}
				if (hadForce && saveGameForceField != null)
				{
					saveGameForceField.SetValue(null, previousForce);
				}
			}
			catch
			{
			}
		}
	}

	internal const string PluginGuid = "CasualtiesMP";

	private const string SeedSyncMessageName = "QoLUnknown.v1.SeedSync";

	private const string SeedRequestMessageName = "QoLUnknown.v1.SeedRequest";

	private const string ClientInventoryRestoreRequestMessageName = "QoLUnknown.v1.InventoryRestoreRequest";

	private const string ClientInventoryRestoreResultMessageName = "QoLUnknown.v1.InventoryRestoreResult";

	private const string ClientSpawnRequestMessageName = "QoLUnknown.v1.SpawnRequest";

	private const string ClientSpawnResultMessageName = "QoLUnknown.v1.SpawnResult";

	private const string ClientHealRequestMessageName = "QoLUnknown.v1.HealRequest";

	private const string ClientHealResultMessageName = "QoLUnknown.v1.HealResult";

	private const string ClientPrepareReconnectForHostLoadMessageName = "QoLUnknown.v1.PrepareReconnectForHostLoad";

	private const int MaxMessagePayloadLength = 1048576;

	private const int InventoryRestoreChunkSize = 32768;

	private const float InventoryRestoreChunkTimeoutSeconds = 30f;

	private static ManualLogSource Logger;

	private static Harmony harmony;

	private static Type multiplayerType;

	private static Type mpPluginType;

	private static Type itemSyncType;

	private static Type netObjectRegistryType;

	private static Type inventoryActionPatchType;

	private static Type saveSystemPatchType;

	private static Type saveGamePatchType;

	private static Type soundCannonTrackerType;

	private static Type bodyCombineItemsPatchType;

	private static Type scavPlayerType;

	private static Type netBodyType;

	private static Type clientMainType;

	private static Type serverMainType;

	private static Type netType;

	private static Type netNetTypeType;

	private static Type uiMainMenuType;

	private static Type transportLiteNetLibType;

	private static Type transportSteamworksType;

	private static Type kSteamType;

	private static Type worldgenPatchType;

	private static Type worldgenPatchesType;

	private static Type lastBeforeGenerationStateType;

	private static Type netDataReaderType;

	private static Type netDataWriterType;

	private static Type deliveryMethodType;

	private static Type netClientIdType;

	private static Type netExtensionsType;

	private static Type customServerReceiverDelegateType;

	private static Type customClientReceiverDelegateType;

	private static FieldInfo inventoryActionFlagField;

	private static FieldInfo saveDataPathReplacementField;

	private static FieldInfo saveGameForceField;

	private static FieldInfo soundCannonTrackerChargingField;

	private static FieldInfo vanillaSoundCannonChargingField;

	private static FieldInfo itemContainerChangedField;

	private static Type craftCommitType;

	private static MethodInfo craftRequestMethod;

	private static MethodInfo craftTryClickMethod;

	private static FieldInfo craftStepOpenField;

	private static FieldInfo craftStepPendingField;

	private static FieldInfo craftStepAcceptedField;

	private static FieldInfo mpClientPingField;

	private static int handledCraftFrame = -1;

	private static bool craftClickGuardPatched;

	private static MethodInfo mpLiquidCombineAnnounceMethod;

	private static MethodInfo netPlayerLookupMethod;

	private static MethodInfo netServerKickMethod;

	private static MethodInfo forceDisconnectMethod;

	private static MethodInfo goBackToMainMenuMethod;

	private static MethodInfo uiMainMenuSetOpenMethod;

	private static MethodInfo uiMainMenuIsOpenMethod;

	private static MethodInfo transportLiteConnectMethod;

	private static MethodInfo transportSteamJoinLobbyMethod;

	private static MethodInfo multiplayerStatusLogMethod;

	private static MethodInfo serverAnnounceGameStartMethod;

	private static MethodInfo multiplayerOnSceneLoadedMethod;

	private static MethodInfo serverFinishedWorldgenReceiverMethod;

	private static FieldInfo localPlayerField;

	private static FieldInfo playerBodyToPlayerDictField;

	private static FieldInfo playerClientIdToPlayerDictField;

	private static FieldInfo multiplayerRulesField;

	private static FieldInfo firstWorldgenParamsField;

	private static FieldInfo showMultiplayerMenuField;

	private static FieldInfo uiCurrentTabField;

	private static FieldInfo uiDirectConnectTabField;

	private static FieldInfo serverLastPlayerStatesField;

	private static MethodInfo lastBeforeGenerationStateApplyMethod;

	private static MethodInfo playerTeleportMethod;

	private static PropertyInfo serverAllClientIdsExceptHostProperty;

	private static MethodInfo createNamedWriterMethod;

	private static MethodInfo registerCustomServerReceiverMethod;

	private static MethodInfo registerCustomClientReceiverMethod;

	private static MethodInfo clientSendMethod;

	private static MethodInfo serverSendToPlayerMethod;

	private static MethodInfo serverSendToClientsMethod;

	private static MethodInfo writerPutBytesMethod;

	private static MethodInfo writerPutUIntMethod;

	private static MethodInfo readerGetRemainingBytesMethod;

	private static PropertyInfo readerAvailableBytesProperty;

	private static MethodInfo readerGetUIntMethod;

	private static MethodInfo itemRecordInventoryStateMethod;

	private static FieldInfo itemLastInventoryStateField;

	private static EventInfo multiplayerInitFinishedEvent;

	private static Delegate multiplayerInitFinishedHandler;

	private static bool resolutionAttempted;

	private static int resolvedAssemblyCount = -1;

	private static bool pluginPresent;

	private static int networkStateFrame = -1;

	private static bool cachedNetworkRunning;

	private static bool cachedIsClient;

	private static bool cachedIsServer;

	private static bool cachedWorldGenerated;

	private static float nextInactiveNetworkRefreshTime;

	private static bool seedSyncRegistered;

	private static bool seedRequestRegistered;

	private static bool clientInventoryRestoreRequestRegistered;

	private static bool clientInventoryRestoreResultRegistered;

	private static bool clientSpawnRequestRegistered;

	private static bool clientSpawnResultRegistered;

	private static bool clientHealRequestRegistered;

	private static bool clientHealResultRegistered;

	private static bool clientPrepareReconnectForHostLoadRegistered;

	private static bool runtimeHooksInstalled;

	private static bool runtimeHooksAttempted;

	private static bool seedAnnouncementHooksPatched;

	private static bool pingSuppressionHooksPatched;

	private static bool hostSaveLoadHooksPatched;

	private static bool suppressNextPointFingerClientSend;

	private static float lastSeedRequestTime;

	private static string pendingHostWorldgenStatePayload;

	private static bool preserveServerPlayerStatesForHostSaveLoad;

	private static List<DictionaryEntry> preservedServerPlayerStatesForHostSaveLoad;

	private static bool autoReconnectAfterHostLoadArmed;

	private static bool autoReconnectAfterHostLoadUseSteam;

	private static string autoReconnectAfterHostLoadIpPort;

	private static ulong autoReconnectAfterHostLoadLobbyId;

	private static bool autoReconnectAfterHostLoadCoroutineRunning;

	private static int autoReconnectAfterHostLoadAttempts;

	private static float autoReconnectAfterHostLoadNextAttemptTime;

	private static bool autoReconnectAfterHostLoadAttemptInFlight;

	private static bool autoReconnectAfterHostLoadMainMenuLogged;

	private static Dictionary<string, string> pendingClientInventoryRestorePayloadsByPersistentId;

	private static readonly Dictionary<uint, InventoryRestoreChunkAssembly> incomingInventoryRestoreChunks = new Dictionary<uint, InventoryRestoreChunkAssembly>();

	public static bool IsPluginPresent
	{
		get
		{
			EnsureResolved();
			return pluginPresent;
		}
	}

	public static bool IsActive => IsNetworkRunning;

	public static bool IsMultiplayerMenuOpen
	{
		get
		{
			if (!IsPluginPresent || uiMainMenuIsOpenMethod == null)
			{
				return false;
			}
			object obj = uiMainMenuIsOpenMethod.Invoke(null, null);
			bool flag = default(bool);
			int num;
			if (obj is bool)
			{
				flag = (bool)obj;
				num = 1;
			}
			else
			{
				num = 0;
			}
			return (byte)((uint)num & (flag ? 1u : 0u)) != 0;
		}
	}

	public static bool ShouldUseMultiplayerWorldgenAuthority => IsNetworkRunning && IsServer;

	public static bool HasPendingHostWorldgenState => !string.IsNullOrEmpty(pendingHostWorldgenStatePayload);

	public static bool IsNetworkRunning
	{
		get
		{
			RefreshNetworkState();
			return cachedNetworkRunning;
		}
	}

	public static bool IsClient
	{
		get
		{
			RefreshNetworkState();
			return cachedIsClient;
		}
	}

	public static bool IsServer
	{
		get
		{
			RefreshNetworkState();
			return cachedIsServer;
		}
	}

	public static bool IsWorldGenerated
	{
		get
		{
			RefreshNetworkState();
			return cachedWorldGenerated;
		}
	}

	public static bool ShouldBlockQoLLoads
	{
		get
		{
			EnsureResolved();
			if (!pluginPresent)
			{
				return false;
			}
			if (ReadStaticBool(mpPluginType, "FORCE_DISABLE_MP_MOD"))
			{
				return false;
			}
			return IsNetworkRunning || ReadStaticBool(mpPluginType, "SUCCESFULLY_INITIALIZED");
		}
	}

	public static bool CanImportSingleplayerSlotAsHostStart => IsNetworkRunning && IsServer && !IsWorldGenerated;

	internal static bool NativeCraftAvailable
	{
		get
		{
			EnsureResolved();
			return craftRequestMethod != null && craftStepPendingField != null && craftStepAcceptedField != null;
		}
	}

	internal static bool NativeCraftOutstanding
	{
		get
		{
			EnsureResolved();
			object obj = craftStepOpenField?.GetValue(null);
			bool flag = default(bool);
			int num;
			if (obj is bool)
			{
				flag = (bool)obj;
				num = 1;
			}
			else
			{
				num = 0;
			}
			return (byte)((uint)num & (flag ? 1u : 0u)) != 0;
		}
	}

	internal static float CraftWaitSeconds
	{
		get
		{
			EnsureResolved();
			int num = ((mpClientPingField?.GetValue(null) is int val) ? Math.Max(0, val) : 0);
			return 15f + (float)num * 0.008f;
		}
	}

	internal static void EnsureInitializedForRuntime()
	{
		EnsureResolved();
		EnsureRuntimeHooksInstalled();
	}

	public static void Initialize(ManualLogSource logger)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Expected O, but got Unknown
		Logger = logger;
		harmony = new Harmony("org.bepinex.plugins.qol.together_compat");
		EnsureResolved();
		if (pluginPresent)
		{
			AttachToMultiplayerInitialization();
			if (ReadStaticBool(mpPluginType, "SUCCESFULLY_INITIALIZED"))
			{
				EnsureRuntimeHooksInstalled();
			}
		}
	}

	public static void AnnounceCurrentSeedToClients()
	{
		if (IsNetworkRunning && IsServer && SeedManager.IsSeeded)
		{
			string payload = BuildSeedPayload(SeedManager.CurrentSeed, SeedManager.InputString);
			SendNamedStringToClients("QoLUnknown.v1.SeedSync", payload);
		}
	}

	public static void RequestSeedResend()
	{
		if (IsNetworkRunning && IsClient && !(Time.realtimeSinceStartup - lastSeedRequestTime < 2f))
		{
			lastSeedRequestTime = Time.realtimeSinceStartup;
			SendNamedStringToServer("QoLUnknown.v1.SeedRequest", string.Empty);
		}
	}

	public static IDisposable BeginInventoryMutation(Body body)
	{
		if (!IsActive || !IsWorldGenerated)
		{
			return NullScope.Instance;
		}
		object previous = null;
		bool hadFlag = false;
		try
		{
			if (IsClient && itemRecordInventoryStateMethod != null && itemLastInventoryStateField != null)
			{
				object obj = itemRecordInventoryStateMethod.Invoke(null, new object[1] { body });
				if (obj != null)
				{
					itemLastInventoryStateField.SetValue(null, obj);
				}
			}
			if (inventoryActionFlagField != null)
			{
				previous = inventoryActionFlagField.GetValue(null);
				hadFlag = true;
				inventoryActionFlagField.SetValue(null, true);
			}
		}
		catch
		{
		}
		return new InventoryMutationScope(hadFlag, previous);
	}

	public static IDisposable BeginVanillaSaveSnapshot()
	{
		if (!IsActive)
		{
			return NullScope.Instance;
		}
		EnsureResolved();
		object previousPath = null;
		object previousForce = null;
		bool hadPath = false;
		bool hadForce = false;
		try
		{
			if (saveDataPathReplacementField != null)
			{
				previousPath = saveDataPathReplacementField.GetValue(null);
				hadPath = true;
				saveDataPathReplacementField.SetValue(null, Application.persistentDataPath);
			}
			if (saveGameForceField != null)
			{
				previousForce = saveGameForceField.GetValue(null);
				hadForce = true;
				saveGameForceField.SetValue(null, true);
			}
		}
		catch
		{
		}
		return new VanillaSaveSnapshotScope(hadPath, previousPath, hadForce, previousForce);
	}

	public static bool CanMoveItemNow(Item item)
	{
		if (!IsActive || !IsWorldGenerated || (Object)(object)item == (Object)null)
		{
			return true;
		}
		return TryEnsureItemSync(item);
	}

	public static bool TryEnsureItemSync(Item item)
	{
		if (!IsActive || (Object)(object)item == (Object)null)
		{
			return true;
		}
		if (ObjectCanBeIgnoredForNetwork(((Component)item).gameObject))
		{
			return true;
		}
		if (TryGetSyncInfo(item, out var syncInfo))
		{
			return true;
		}
		if (!IsServer)
		{
			return false;
		}
		InvokeTryGetSyncInfoOrRegister(item);
		return TryGetSyncInfo(item, out syncInfo);
	}

	public static void MarkItemMoved(Item item, bool containerChanged)
	{
		if (!IsActive || (Object)(object)item == (Object)null)
		{
			return;
		}
		if (!TryGetSyncInfo(item, out var syncInfo) || syncInfo == null)
		{
			TryEnsureItemSync(item);
			TryGetSyncInfo(item, out syncInfo);
		}
		if (syncInfo != null)
		{
			InvokeInstance(syncInfo, "SetIgnoreTimeForRoundTrip");
			if (containerChanged)
			{
				AddItemContainerChanged(syncInfo);
			}
		}
	}

	public static bool TryGetSoundCannonCharging(SoundCannon cannon, out bool charging)
	{
		charging = false;
		if ((Object)(object)cannon == (Object)null)
		{
			return false;
		}
		EnsureResolved();
		if (IsActive && soundCannonTrackerType != null)
		{
			try
			{
				Component component = ((Component)cannon).GetComponent(soundCannonTrackerType);
				if ((Object)(object)component != (Object)null && soundCannonTrackerChargingField != null)
				{
					object value = soundCannonTrackerChargingField.GetValue(component);
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
					charging = (byte)((uint)num & (flag ? 1u : 0u)) != 0;
					return true;
				}
			}
			catch
			{
				return false;
			}
		}
		try
		{
			if (vanillaSoundCannonChargingField != null)
			{
				object value = vanillaSoundCannonChargingField.GetValue(cannon);
				bool flag2 = default(bool);
				int num2;
				if (value is bool)
				{
					flag2 = (bool)value;
					num2 = 1;
				}
				else
				{
					num2 = 0;
				}
				charging = (byte)((uint)num2 & (flag2 ? 1u : 0u)) != 0;
				return true;
			}
		}
		catch
		{
			return false;
		}
		return false;
	}

	public static bool TryGetNetBodyTooltip(Body body, out string tooltipName, out string tooltipDescription)
	{
		tooltipName = "";
		tooltipDescription = "";
		if (!IsActive || (Object)(object)body == (Object)null)
		{
			return false;
		}
		EnsureResolved();
		if (netBodyType == null)
		{
			return false;
		}
		try
		{
			Component component = ((Component)body).GetComponent(netBodyType);
			MethodInfo method = netBodyType.GetMethod("DoMouseHoverTooltip", BindingFlags.Instance | BindingFlags.Public);
			if ((Object)(object)component == (Object)null || method == null)
			{
				return false;
			}
			object[] array = new object[2] { tooltipName, tooltipDescription };
			method.Invoke(component, array);
			tooltipName = (array[0] as string) ?? "";
			tooltipDescription = (array[1] as string) ?? "";
			return !string.IsNullOrEmpty(tooltipName);
		}
		catch
		{
			tooltipName = "";
			tooltipDescription = "";
			return false;
		}
	}

	public static void NotifyLoadBlocked()
	{
		string text = "Disable multiplayer before loading QoL saves.";
		if ((Object)(object)PlayerCamera.main != (Object)null)
		{
			PlayerCamera.main.DoAlert(text, false);
		}
		ManualLogSource logger = Logger;
		if (logger != null)
		{
			logger.LogWarning((object)text);
		}
	}

	public static bool SendClientInventoryRestoreRequest(string payload)
	{
		if (!IsNetworkRunning || !IsClient || string.IsNullOrWhiteSpace(payload))
		{
			return false;
		}
		EnsureRuntimeHooksInstalled();
		byte[] bytes = Encoding.UTF8.GetBytes(payload);
		if (bytes.Length == 0 || bytes.Length > 1048576)
		{
			return false;
		}
		string text = Guid.NewGuid().ToString("N");
		int num = (bytes.Length + 32768 - 1) / 32768;
		for (int i = 0; i < num; i++)
		{
			int num2 = i * 32768;
			int length = Math.Min(32768, bytes.Length - num2);
			string text2 = Convert.ToBase64String(bytes, num2, length);
			string payload2 = string.Join("|", "1", text, i.ToString(CultureInfo.InvariantCulture), num.ToString(CultureInfo.InvariantCulture), bytes.Length.ToString(CultureInfo.InvariantCulture), text2);
			if (!SendNamedStringToServer("QoLUnknown.v1.InventoryRestoreRequest", payload2))
			{
				return false;
			}
		}
		return true;
	}

	public static bool SendClientSpawnRequest(string commandString)
	{
		if (!IsNetworkRunning || !IsClient || string.IsNullOrWhiteSpace(commandString))
		{
			return false;
		}
		EnsureRuntimeHooksInstalled();
		return SendNamedStringToServer("QoLUnknown.v1.SpawnRequest", commandString);
	}

	public static bool SendClientHealRequest()
	{
		if (!IsNetworkRunning || !IsClient)
		{
			return false;
		}
		EnsureRuntimeHooksInstalled();
		return SendNamedStringToServer("QoLUnknown.v1.HealRequest", "1");
	}

	public static bool AreClientCheatCommandsAllowed()
	{
		return ClientCheatCommandsAllowed();
	}

	public static string TryGetLocalPersistentPlayerId()
	{
		EnsureResolved();
		object obj = null;
		try
		{
			obj = localPlayerField?.GetValue(null);
		}
		catch
		{
		}
		if (obj == null)
		{
			return null;
		}
		try
		{
			return obj.GetType().GetMethod("GetPersistentId", BindingFlags.Instance | BindingFlags.Public)?.Invoke(obj, null) as string;
		}
		catch
		{
			return null;
		}
	}

	public static string TryGetExpectedHostPersistentPlayerId()
	{
		string text = TryGetLocalPersistentPlayerId();
		if (!string.IsNullOrEmpty(text))
		{
			return text;
		}
		EnsureResolved();
		if (uiMainMenuType == null)
		{
			return null;
		}
		try
		{
			string text2 = uiMainMenuType.GetField("USERINPUT_NAME", BindingFlags.Static | BindingFlags.Public)?.GetValue(null) as string;
			if (string.IsNullOrEmpty(text2))
			{
				return null;
			}
			return "NAME_" + BitConverter.ToString(Encoding.UTF8.GetBytes(text2)).Replace("-", string.Empty);
		}
		catch
		{
			return null;
		}
	}

	public static bool TryCaptureHostWorldgenState(out string payload)
	{
		payload = null;
		if (!IsNetworkRunning || !IsServer)
		{
			return false;
		}
		EnsureResolved();
		if (firstWorldgenParamsField == null || lastBeforeGenerationStateType == null)
		{
			return false;
		}
		try
		{
			object value = firstWorldgenParamsField.GetValue(null);
			return TrySerializeLastBeforeGenerationState(value, out payload);
		}
		catch (Exception ex)
		{
			ManualLogSource logger = Logger;
			if (logger != null)
			{
				logger.LogWarning((object)("Failed to capture Together worldgen state: " + ex.Message));
			}
			payload = null;
			return false;
		}
	}

	public static void SetPendingHostWorldgenStatePayload(string payload)
	{
		pendingHostWorldgenStatePayload = (string.IsNullOrWhiteSpace(payload) ? null : payload.Trim());
	}

	public static void ClearPendingHostWorldgenStatePayload()
	{
		pendingHostWorldgenStatePayload = null;
	}

	public static void RegisterAndSyncItemTree(Item item)
	{
		if (IsNetworkRunning && IsServer && !((Object)(object)item == (Object)null))
		{
			Item[] componentsInChildren = ((Component)item).GetComponentsInChildren<Item>(true);
			foreach (Item val in componentsInChildren)
			{
				RegisterAndSyncGameObject(((Component)val).gameObject);
			}
		}
	}

	public static void TeleportClientsToHostAfterDelay(float delaySeconds)
	{
		if (IsNetworkRunning && IsServer && !((Object)(object)PlayerCamera.main == (Object)null))
		{
			((MonoBehaviour)PlayerCamera.main).StartCoroutine(TeleportClientsToHostCoroutine(delaySeconds));
		}
	}

	private static IEnumerator TeleportClientsToHostCoroutine(float delaySeconds)
	{
		if (delaySeconds > 0f)
		{
			yield return (object)new WaitForSeconds(delaySeconds);
		}
		TryTeleportClientsToHost();
	}

	private static void TryTeleportClientsToHost()
	{
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		if (!IsNetworkRunning || !IsServer || (Object)(object)PlayerCamera.main == (Object)null || (Object)(object)PlayerCamera.main.body == (Object)null)
		{
			return;
		}
		EnsureResolved();
		if (playerBodyToPlayerDictField == null || playerTeleportMethod == null)
		{
			return;
		}
		try
		{
			if (!(playerBodyToPlayerDictField.GetValue(null) is IDictionary dictionary))
			{
				return;
			}
			Vector2 val = Vector2.op_Implicit(((Component)PlayerCamera.main.body).transform.position);
			foreach (DictionaryEntry item in dictionary)
			{
				object value = item.Value;
				if (value != null && !IsLocalNetPlayer(value))
				{
					playerTeleportMethod.Invoke(value, new object[1] { val });
				}
			}
		}
		catch (Exception ex)
		{
			ManualLogSource logger = Logger;
			if (logger != null)
			{
				logger.LogWarning((object)("Failed to teleport clients to host after MP load: " + ex.Message));
			}
		}
	}

	internal static void MarkCraftHandled()
	{
		handledCraftFrame = Time.frameCount;
	}

	internal static NativeCraftState RequestNativeCraft(PlayerCamera camera, Recipe recipe)
	{
		if ((Object)(object)camera == (Object)null || recipe == null || !NativeCraftAvailable)
		{
			return NativeCraftState.Denied;
		}
		int selectedRecipe = camera.selectedRecipe;
		try
		{
			object obj = craftRequestMethod.Invoke(null, new object[2] { camera, recipe });
			if (obj == null)
			{
				return NativeCraftState.Denied;
			}
			object value = craftStepPendingField.GetValue(obj);
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
			if (((uint)num & (flag ? 1u : 0u)) != 0)
			{
				return NativeCraftState.Pending;
			}
			value = craftStepAcceptedField.GetValue(obj);
			bool flag2 = default(bool);
			int num2;
			if (value is bool)
			{
				flag2 = (bool)value;
				num2 = 1;
			}
			else
			{
				num2 = 0;
			}
			return (((uint)num2 & (flag2 ? 1u : 0u)) != 0) ? NativeCraftState.Accepted : NativeCraftState.Denied;
		}
		catch (Exception ex)
		{
			ManualLogSource logger = Logger;
			if (logger != null)
			{
				logger.LogWarning((object)$"[Crafting] Together craft request failed for recipe {recipe.index}: {ex.GetBaseException().Message}");
			}
			return NativeCraftState.Denied;
		}
		finally
		{
			camera.selectedRecipe = selectedRecipe;
		}
	}

	public static bool TryAnnounceLiquidTransfer(Body body, Item targetItem, Item sourceItem, float ml)
	{
		if (!IsActive || (Object)(object)body == (Object)null || (Object)(object)targetItem == (Object)null || (Object)(object)sourceItem == (Object)null || ml <= 0f)
		{
			return true;
		}
		EnsureResolved();
		if (mpLiquidCombineAnnounceMethod == null || !TryEnsureItemSync(targetItem) || !TryEnsureItemSync(sourceItem))
		{
			return false;
		}
		try
		{
			mpLiquidCombineAnnounceMethod.Invoke(null, new object[5] { true, ml, body, targetItem, sourceItem });
			return true;
		}
		catch
		{
			return false;
		}
	}

	public static bool TryAnnounceLiquidDrain(Item containerItem, float newTotal)
	{
		if (!IsActive || (Object)(object)containerItem == (Object)null)
		{
			return true;
		}
		if (!IsClient || IsServer)
		{
			return true;
		}
		EnsureResolved();
		if (multiplayerType == null || !TryGetSyncInfo(containerItem, out var syncInfo) || syncInfo == null || !TryGetSyncId(syncInfo, out var syncId))
		{
			return false;
		}
		MethodInfo methodInfo = FindStaticMethod(multiplayerType, "Client_SendSimpleMessageToServer", new Type[4]
		{
			typeof(byte),
			typeof(ushort),
			typeof(float),
			typeof(bool)
		});
		if (methodInfo == null)
		{
			return false;
		}
		try
		{
			methodInfo.Invoke(null, new object[4]
			{
				(byte)75,
				(ushort)syncId,
				newTotal,
				true
			});
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static void EnsureResolved()
	{
		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
		if (!resolutionAttempted || assemblies.Length != resolvedAssemblyCount)
		{
			resolutionAttempted = true;
			resolvedAssemblyCount = assemblies.Length;
			multiplayerType = FindType(assemblies, "Together.Multiplayer");
			mpPluginType = FindType(assemblies, "Together.Plugin");
			itemSyncType = FindType(assemblies, "Together.ItemSync");
			netObjectRegistryType = FindType(assemblies, "Together.NetObjectRegistry");
			inventoryActionPatchType = FindType(assemblies, "Together.PlayerCamera_TryPerformInventoryAction_MultiplayerPatch");
			saveSystemPatchType = FindType(assemblies, "Together.SavesystemPatch");
			saveGamePatchType = FindType(assemblies, "Together.SaveSystem_SaveGame_MultiplayerPatch");
			soundCannonTrackerType = FindType(assemblies, "Together.TrackerSoundCannon");
			bodyCombineItemsPatchType = FindType(assemblies, "Together.Body_CombineItems_MultiplayerPatch");
			scavPlayerType = FindType(assemblies, "Together.ScavPlayer");
			netBodyType = FindType(assemblies, "Together.NetBody");
			clientMainType = FindType(assemblies, "Together.ClientMain");
			serverMainType = FindType(assemblies, "Together.ServerMain");
			netType = FindType(assemblies, "Together.Net");
			netNetTypeType = FindType(assemblies, "Together.Net+NetType");
			uiMainMenuType = FindType(assemblies, "Together.UIMainMenu");
			transportLiteNetLibType = FindType(assemblies, "Together.TransportLiteNetLib");
			transportSteamworksType = FindType(assemblies, "Together.TransportSteamworks");
			kSteamType = FindType(assemblies, "Together.KSteam");
			worldgenPatchType = FindType(assemblies, "Together.WorldGeneration_GenerateWorld_MultiplayerPatch");
			worldgenPatchesType = FindType(assemblies, "Together.WorldMP");
			lastBeforeGenerationStateType = FindType(assemblies, "Together.LastBeforeGenerationState");
			netExtensionsType = FindType(assemblies, "Together.MyLiteNetLibExtensions");
			netClientIdType = FindType(assemblies, "Together.knetid");
			netDataReaderType = FindType(assemblies, "LiteNetLib.Utils.NetDataReader");
			netDataWriterType = FindType(assemblies, "LiteNetLib.Utils.NetDataWriter");
			deliveryMethodType = FindType(assemblies, "LiteNetLib.DeliveryMethod");
			InferNetworkApiTypes();
			createNamedWriterMethod = multiplayerType?.GetMethod("CreateNamedWriter", BindingFlags.Static | BindingFlags.Public, null, new Type[1] { typeof(string) }, null);
			registerCustomServerReceiverMethod = multiplayerType?.GetMethod("RegisterCustomServerReceiver", BindingFlags.Static | BindingFlags.Public);
			registerCustomClientReceiverMethod = multiplayerType?.GetMethod("RegisterCustomClientReceiver", BindingFlags.Static | BindingFlags.Public);
			customServerReceiverDelegateType = registerCustomServerReceiverMethod?.GetParameters().ElementAtOrDefault(1)?.ParameterType;
			customClientReceiverDelegateType = registerCustomClientReceiverMethod?.GetParameters().ElementAtOrDefault(1)?.ParameterType;
			clientSendMethod = FindStaticMethod(netType, "Client_Send", new Type[2] { deliveryMethodType, netDataWriterType });
			serverSendToPlayerMethod = FindStaticMethod(netType, "Server_SendTo", new Type[3] { deliveryMethodType, netDataWriterType, scavPlayerType });
			serverSendToClientsMethod = netType?.GetMethods(BindingFlags.Static | BindingFlags.Public).FirstOrDefault(delegate(MethodInfo method)
			{
				ParameterInfo[] parameters = method.GetParameters();
				Type type = ((parameters.Length == 3) ? UnwrapByRef(parameters[2].ParameterType) : null);
				return method.Name == "Server_SendToClients" && parameters.Length == 3 && UnwrapByRef(parameters[0].ParameterType) == deliveryMethodType && UnwrapByRef(parameters[1].ParameterType) == netDataWriterType && type != null && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>) && type.GetGenericArguments()[0] == netClientIdType;
			});
			writerPutBytesMethod = netDataWriterType?.GetMethod("Put", new Type[1] { typeof(byte[]) });
			writerPutUIntMethod = netDataWriterType?.GetMethod("Put", new Type[1] { typeof(uint) });
			readerGetRemainingBytesMethod = netDataReaderType?.GetMethod("GetRemainingBytes", Type.EmptyTypes);
			readerAvailableBytesProperty = netDataReaderType?.GetProperty("AvailableBytes", BindingFlags.Instance | BindingFlags.Public);
			readerGetUIntMethod = netDataReaderType?.GetMethod("GetUInt", Type.EmptyTypes);
			inventoryActionFlagField = inventoryActionPatchType?.GetField("is_inside_TryPerformInventoryAction", BindingFlags.Static | BindingFlags.Public);
			saveDataPathReplacementField = saveSystemPatchType?.GetField("savedatapathreplacement", BindingFlags.Static | BindingFlags.Public);
			saveGameForceField = saveGamePatchType?.GetField("force", BindingFlags.Static | BindingFlags.NonPublic);
			soundCannonTrackerChargingField = soundCannonTrackerType?.GetField("charging", BindingFlags.Instance | BindingFlags.Public);
			vanillaSoundCannonChargingField = typeof(SoundCannon).GetField("charging", BindingFlags.Instance | BindingFlags.NonPublic);
			itemContainerChangedField = itemSyncType?.GetField("ItemContainerChanged", BindingFlags.Static | BindingFlags.Public);
			itemRecordInventoryStateMethod = itemSyncType?.GetMethod("RecordInvState", BindingFlags.Static | BindingFlags.Public);
			itemLastInventoryStateField = itemSyncType?.GetField("Client_last_inventory_state", BindingFlags.Static | BindingFlags.NonPublic);
			craftCommitType = FindType(assemblies, "Together.CraftCommit");
			craftRequestMethod = craftCommitType?.GetMethod("Request", BindingFlags.Static | BindingFlags.Public, null, new Type[2]
			{
				typeof(PlayerCamera),
				typeof(Recipe)
			}, null);
			craftTryClickMethod = craftCommitType?.GetMethod("TryClick", BindingFlags.Static | BindingFlags.NonPublic, null, new Type[1] { typeof(PlayerCamera) }, null);
			craftStepOpenField = craftCommitType?.GetField("_stepOpen", BindingFlags.Static | BindingFlags.NonPublic);
			craftStepPendingField = craftRequestMethod?.ReturnType.GetField("Pending", BindingFlags.Instance | BindingFlags.Public);
			craftStepAcceptedField = craftRequestMethod?.ReturnType.GetField("Accepted", BindingFlags.Instance | BindingFlags.Public);
			mpClientPingField = clientMainType?.GetField("LOCAL_PING", BindingFlags.Static | BindingFlags.Public);
			mpLiquidCombineAnnounceMethod = bodyCombineItemsPatchType?.GetMethod("DoTheCombineAnnouncement", BindingFlags.Static | BindingFlags.Public);
			netPlayerLookupMethod = scavPlayerType?.GetMethod("TryGetPlayerAndBodyFromClientId", BindingFlags.Static | BindingFlags.Public);
			netServerKickMethod = netType?.GetMethod("Server_Kick", BindingFlags.Static | BindingFlags.Public);
			forceDisconnectMethod = multiplayerType?.GetMethod("_JustDisconnect", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			goBackToMainMenuMethod = multiplayerType?.GetMethod("GoBackToMainMenuIfImClientAndInGame", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			uiMainMenuSetOpenMethod = uiMainMenuType?.GetMethod("SetOpen", BindingFlags.Static | BindingFlags.Public);
			uiMainMenuIsOpenMethod = uiMainMenuType?.GetMethod("IsOpen", BindingFlags.Static | BindingFlags.Public);
			transportLiteConnectMethod = transportLiteNetLibType?.GetMethod("OnWantToConnect", BindingFlags.Static | BindingFlags.Public);
			transportSteamJoinLobbyMethod = transportSteamworksType?.GetMethod("OnWantToJoinLobby", BindingFlags.Static | BindingFlags.Public);
			multiplayerStatusLogMethod = multiplayerType?.GetMethod("DoMultiplayerStatusMessageLog", BindingFlags.Static | BindingFlags.Public);
			serverAnnounceGameStartMethod = serverMainType?.GetMethod("Server_Announce_GAME_START", BindingFlags.Static | BindingFlags.Public);
			multiplayerOnSceneLoadedMethod = multiplayerType?.GetMethod("OnSceneLoaded", BindingFlags.Static | BindingFlags.Public);
			serverFinishedWorldgenReceiverMethod = worldgenPatchesType?.GetMethod("ServerReceiver_FinishedWorldgen", BindingFlags.Static | BindingFlags.NonPublic);
			localPlayerField = scavPlayerType?.GetField("LocalPlayer", BindingFlags.Static | BindingFlags.Public);
			playerBodyToPlayerDictField = scavPlayerType?.GetField("BodyToPlayerDict", BindingFlags.Static | BindingFlags.Public);
			playerClientIdToPlayerDictField = scavPlayerType?.GetField("ClientIdToPlayerDict", BindingFlags.Static | BindingFlags.Public);
			playerTeleportMethod = scavPlayerType?.GetMethod("Server_TeleportCharacter", BindingFlags.Instance | BindingFlags.Public);
			showMultiplayerMenuField = multiplayerType?.GetField("showMultiplayerMenu", BindingFlags.Static | BindingFlags.Public);
			uiCurrentTabField = uiMainMenuType?.GetField("current_tab", BindingFlags.Static | BindingFlags.Public);
			uiDirectConnectTabField = uiMainMenuType?.GetField("menutab_directconnect", BindingFlags.Static | BindingFlags.Public);
			serverLastPlayerStatesField = serverMainType?.GetField("Server_LastPlayerStates", BindingFlags.Static | BindingFlags.Public);
			serverAllClientIdsExceptHostProperty = serverMainType?.GetProperty("AllClientIdsExceptHost", BindingFlags.Static | BindingFlags.Public);
			multiplayerRulesField = multiplayerType?.GetField("rules", BindingFlags.Static | BindingFlags.Public);
			firstWorldgenParamsField = worldgenPatchType?.GetField("firstworldgenparams", BindingFlags.Static | BindingFlags.Public);
			lastBeforeGenerationStateApplyMethod = lastBeforeGenerationStateType?.GetMethod("Apply", BindingFlags.Instance | BindingFlags.Public);
			multiplayerInitFinishedEvent = mpPluginType?.GetEvent("OnInitFinish", BindingFlags.Static | BindingFlags.Public);
			pluginPresent = multiplayerType != null && mpPluginType != null;
			networkStateFrame = -1;
			if (pluginPresent)
			{
				LogCompatibilityStatus();
			}
		}
	}

	private static void AttachToMultiplayerInitialization()
	{
		if (!pluginPresent || multiplayerInitFinishedEvent == null || (object)multiplayerInitFinishedHandler != null)
		{
			return;
		}
		try
		{
			MethodInfo method = typeof(MultiplayerCompat).GetMethod("OnMultiplayerInitializationFinished", BindingFlags.Static | BindingFlags.NonPublic);
			multiplayerInitFinishedHandler = Delegate.CreateDelegate(multiplayerInitFinishedEvent.EventHandlerType, method);
			multiplayerInitFinishedEvent.AddEventHandler(null, multiplayerInitFinishedHandler);
		}
		catch (Exception ex)
		{
			ManualLogSource logger = Logger;
			if (logger != null)
			{
				logger.LogWarning((object)("[Together] Could not subscribe to Plugin.OnInitFinish: " + ex.Message));
			}
		}
	}

	private static void OnMultiplayerInitializationFinished()
	{
		EnsureResolved();
		EnsureRuntimeHooksInstalled();
	}

	private static void LogCompatibilityStatus()
	{
		bool flag = createNamedWriterMethod != null && registerCustomServerReceiverMethod != null && registerCustomClientReceiverMethod != null && clientSendMethod != null && serverSendToPlayerMethod != null && serverSendToClientsMethod != null && writerPutBytesMethod != null && readerGetRemainingBytesMethod != null && readerAvailableBytesProperty != null;
		bool flag2 = itemSyncType != null && netObjectRegistryType != null && itemContainerChangedField != null;
		bool flag3 = saveSystemPatchType != null && saveGamePatchType != null && lastBeforeGenerationStateType != null;
	}

	public static int PrepareClientsForHostSaveLoad()
	{
		if (!IsNetworkRunning || !IsServer)
		{
			return 0;
		}
		EnsureResolved();
		List<object> nonHostClientIds = GetNonHostClientIds();
		if (nonHostClientIds.Count == 0)
		{
			ManualLogSource logger = Logger;
			if (logger != null)
			{
				logger.LogInfo((object)"[MultiplayerCompat] Host save load: no connected clients to return to the main menu.");
			}
			return 0;
		}
		bool flag = SendClientsBackToMainMenuForHostLoad();
		ManualLogSource logger2 = Logger;
		if (logger2 != null)
		{
			logger2.LogInfo((object)string.Format("[MultiplayerCompat] Host save load: returning {0} client(s) to the main menu (prepare-reconnect send {1}).", nonHostClientIds.Count, flag ? "ok" : "failed"));
		}
		if (!flag)
		{
			DisconnectNonHostClientsForHostSaveLoad("Host is loading a save. Please rejoin after the load finishes.", nonHostClientIds);
			return nonHostClientIds.Count;
		}
		return nonHostClientIds.Count;
	}

	public static void BeginConnectedHostSaveLoad()
	{
		if (IsNetworkRunning && IsServer)
		{
			preserveServerPlayerStatesForHostSaveLoad = false;
			preservedServerPlayerStatesForHostSaveLoad = null;
			ResetRemotePlayerBodiesForConnectedHostSaveLoad();
		}
	}

	public static void PreparePostWorldgenClientInventoryRestores()
	{
		if (!IsNetworkRunning || !IsServer)
		{
			pendingClientInventoryRestorePayloadsByPersistentId = null;
			return;
		}
		try
		{
			Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.Ordinal);
			IDictionary dictionary2 = playerClientIdToPlayerDictField?.GetValue(null) as IDictionary;
			object obj = localPlayerField?.GetValue(null);
			if (dictionary2 == null)
			{
				pendingClientInventoryRestorePayloadsByPersistentId = null;
				return;
			}
			foreach (DictionaryEntry item in dictionary2)
			{
				object value = item.Value;
				if (value != null && value != obj)
				{
					string text = value.GetType().GetMethod("GetPersistentId", BindingFlags.Instance | BindingFlags.Public)?.Invoke(value, null) as string;
					if (!string.IsNullOrWhiteSpace(text) && MultiplayerSaveBundle.TryReadLiveMpPlayerInventorySnapshot(text, out var payload))
					{
						dictionary[text] = payload;
					}
				}
			}
			pendingClientInventoryRestorePayloadsByPersistentId = ((dictionary.Count > 0) ? dictionary : null);
		}
		catch
		{
			pendingClientInventoryRestorePayloadsByPersistentId = null;
		}
	}

	public static void AnnounceHostSaveLoadRestart()
	{
		if (!IsNetworkRunning || !IsServer)
		{
			return;
		}
		EnsureResolved();
		try
		{
			serverAnnounceGameStartMethod?.Invoke(null, null);
		}
		catch
		{
		}
	}

	public static void CancelConnectedHostSaveLoad()
	{
		preserveServerPlayerStatesForHostSaveLoad = false;
		preservedServerPlayerStatesForHostSaveLoad = null;
		pendingClientInventoryRestorePayloadsByPersistentId = null;
	}

	public static int DisconnectNonHostClientsForHostSaveLoad(string reason = null)
	{
		return DisconnectNonHostClientsForHostSaveLoad(reason, null);
	}

	private static int DisconnectNonHostClientsForHostSaveLoad(string reason, List<object> pendingClientIds)
	{
		if (!IsNetworkRunning || !IsServer)
		{
			return 0;
		}
		EnsureResolved();
		if (netServerKickMethod == null)
		{
			return 0;
		}
		try
		{
			List<object> list = pendingClientIds ?? GetNonHostClientIds();
			if (list.Count == 0)
			{
				return 0;
			}
			string text = (string.IsNullOrWhiteSpace(reason) ? "Host is loading a save. Please rejoin after the load finishes." : reason);
			for (int i = 0; i < list.Count; i++)
			{
				netServerKickMethod.Invoke(null, new object[2]
				{
					list[i],
					text
				});
			}
			return list.Count;
		}
		catch
		{
			return 0;
		}
	}

	private static List<object> GetNonHostClientIds()
	{
		EnsureResolved();
		List<object> list = new List<object>();
		if (serverAllClientIdsExceptHostProperty == null)
		{
			return list;
		}
		try
		{
			if (!(serverAllClientIdsExceptHostProperty.GetValue(null, null) is IEnumerable enumerable))
			{
				return list;
			}
			foreach (object item in enumerable)
			{
				if (item != null)
				{
					list.Add(item);
				}
			}
		}
		catch
		{
		}
		return list;
	}

	private static bool SendClientsBackToMainMenuForHostLoad()
	{
		return SendNamedStringToClients("QoLUnknown.v1.PrepareReconnectForHostLoad", string.Empty);
	}

	private static void ResetRemotePlayerBodiesForConnectedHostSaveLoad()
	{
		EnsureResolved();
		if (playerBodyToPlayerDictField == null || playerClientIdToPlayerDictField == null || localPlayerField == null)
		{
			return;
		}
		try
		{
			IDictionary dictionary = playerBodyToPlayerDictField.GetValue(null) as IDictionary;
			IDictionary dictionary2 = playerClientIdToPlayerDictField.GetValue(null) as IDictionary;
			object value = localPlayerField.GetValue(null);
			if (dictionary == null || dictionary2 == null)
			{
				return;
			}
			List<Body> list = new List<Body>();
			List<object> list2 = new List<object>();
			foreach (DictionaryEntry item in dictionary)
			{
				object key = item.Key;
				Body val = (Body)((key is Body) ? key : null);
				object value2 = item.Value;
				if (!((Object)(object)val == (Object)null) && value2 != null && value2 != value)
				{
					list.Add(val);
					list2.Add(value2);
				}
			}
			for (int i = 0; i < list.Count; i++)
			{
				dictionary.Remove(list[i]);
			}
			for (int j = 0; j < list2.Count; j++)
			{
				list2[j].GetType().GetField("body", BindingFlags.Instance | BindingFlags.Public)?.SetValue(list2[j], null);
			}
			for (int k = 0; k < list.Count; k++)
			{
				Body val2 = list[k];
				if (!((Object)(object)val2 == (Object)null))
				{
					try
					{
						Object.Destroy((Object)(object)((Component)((Component)val2).transform.root).gameObject);
					}
					catch
					{
					}
				}
			}
		}
		catch
		{
		}
	}

	private static void RefreshNetworkState()
	{
		EnsureResolved();
		if (!pluginPresent)
		{
			cachedNetworkRunning = false;
			cachedIsClient = false;
			cachedIsServer = false;
			cachedWorldGenerated = false;
		}
		else if ((cachedNetworkRunning || !(Time.unscaledTime < nextInactiveNetworkRefreshTime)) && networkStateFrame != Time.frameCount)
		{
			networkStateFrame = Time.frameCount;
			cachedNetworkRunning = ReadStaticBool(netType, "IsRunning");
			if (!cachedNetworkRunning)
			{
				cachedIsClient = false;
				cachedIsServer = false;
				cachedWorldGenerated = false;
				nextInactiveNetworkRefreshTime = Time.unscaledTime + 0.5f;
			}
			else
			{
				cachedIsClient = cachedNetworkRunning && ReadStaticBool(netType, "IsClient");
				cachedIsServer = cachedNetworkRunning && ReadStaticBool(netType, "IsServer");
				cachedWorldGenerated = cachedNetworkRunning && InvokeStaticBool(multiplayerType, "IsNetworkActiveAndIsWorldGenerated");
				nextInactiveNetworkRefreshTime = 0f;
				EnsureRuntimeHooksInstalled();
			}
		}
	}

	private static void EnsureRuntimeHooksInstalled()
	{
		if (!pluginPresent || runtimeHooksAttempted)
		{
			return;
		}
		if (!ReadStaticBool(mpPluginType, "SUCCESFULLY_INITIALIZED"))
		{
			AttachToMultiplayerInitialization();
			return;
		}
		runtimeHooksAttempted = true;
		RegisterSeedSyncReceiver();
		RegisterSeedRequestReceiver();
		RegisterClientInventoryRestoreRequestReceiver();
		RegisterClientInventoryRestoreResultReceiver();
		RegisterClientSpawnRequestReceiver();
		RegisterClientSpawnResultReceiver();
		RegisterClientHealRequestReceiver();
		RegisterClientHealResultReceiver();
		RegisterClientPrepareReconnectForHostLoadReceiver();
		PatchSeedAnnouncementHooks();
		PatchHostSaveLoadHooks();
		PatchPingSuppressionHooks();
		PatchCraftClickGuard();
		runtimeHooksInstalled = seedSyncRegistered && seedRequestRegistered && clientInventoryRestoreRequestRegistered && clientInventoryRestoreResultRegistered && clientSpawnRequestRegistered && clientSpawnResultRegistered && clientHealRequestRegistered && clientHealResultRegistered && clientPrepareReconnectForHostLoadRegistered && seedAnnouncementHooksPatched && hostSaveLoadHooksPatched && pingSuppressionHooksPatched;
		if (!runtimeHooksInstalled)
		{
			ManualLogSource logger = Logger;
			if (logger != null)
			{
				logger.LogWarning((object)"[Together] One or more QoL named-message routes could not be registered; affected multiplayer features will fail closed.");
			}
			ManualLogSource logger2 = Logger;
			if (logger2 != null)
			{
				logger2.LogDebug((object)($"[Together] Hook status: seedSync={seedSyncRegistered} seedReq={seedRequestRegistered} " + $"invRestoreReq={clientInventoryRestoreRequestRegistered} invRestoreRes={clientInventoryRestoreResultRegistered} " + $"spawnReq={clientSpawnRequestRegistered} spawnRes={clientSpawnResultRegistered} " + $"healReq={clientHealRequestRegistered} healRes={clientHealResultRegistered} " + $"prepReconn={clientPrepareReconnectForHostLoadRegistered} seedAnnounce={seedAnnouncementHooksPatched} " + $"hostSaveLoad={hostSaveLoadHooksPatched} pingSuppress={pingSuppressionHooksPatched}"));
			}
		}
	}

	private static Type FindType(Assembly[] assemblies, string fullName)
	{
		return assemblies.Select(delegate(Assembly assembly)
		{
			try
			{
				return assembly.GetType(fullName, throwOnError: false);
			}
			catch
			{
				return (Type)null;
			}
		}).FirstOrDefault((Type type) => type != null);
	}

	private static void InferNetworkApiTypes()
	{
		if (netDataReaderType == null && customServerReceiverDelegateType != null)
		{
			ParameterInfo[] array = customServerReceiverDelegateType.GetMethod("Invoke")?.GetParameters();
			if (array != null && array.Length == 2)
			{
				netDataReaderType = UnwrapByRef(array[1].ParameterType);
			}
		}
		if (netDataWriterType == null && netType != null)
		{
			netDataWriterType = multiplayerType?.GetMethod("CreateNamedWriter", BindingFlags.Static | BindingFlags.Public, null, new Type[1] { typeof(string) }, null)?.ReturnType;
		}
		if ((deliveryMethodType == null || netDataWriterType == null) && netType != null)
		{
			ParameterInfo[] array2 = netType.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).FirstOrDefault((MethodInfo method) => method.Name == "Server_SendToClients" && method.GetParameters().Length == 3)?.GetParameters();
			if (array2 != null)
			{
				deliveryMethodType = deliveryMethodType ?? UnwrapByRef(array2[0].ParameterType);
				netDataWriterType = netDataWriterType ?? UnwrapByRef(array2[1].ParameterType);
			}
		}
	}

	private static Type UnwrapByRef(Type type)
	{
		return (type != null && type.IsByRef) ? type.GetElementType() : type;
	}

	private static MethodInfo FindClientRegisterReceiverMethod()
	{
		EnsureResolved();
		return registerCustomClientReceiverMethod;
	}

	private static MethodInfo FindServerRegisterReceiverMethod()
	{
		EnsureResolved();
		return registerCustomServerReceiverMethod;
	}

	private static void RegisterSeedSyncReceiver()
	{
		if (seedSyncRegistered)
		{
			return;
		}
		EnsureResolved();
		if (customClientReceiverDelegateType == null || netDataReaderType == null)
		{
			return;
		}
		MethodInfo methodInfo = FindClientRegisterReceiverMethod();
		if (methodInfo == null)
		{
			return;
		}
		try
		{
			object obj = CreateSeedSyncReceiverDelegate();
			methodInfo.Invoke(null, new object[2] { "QoLUnknown.v1.SeedSync", obj });
			seedSyncRegistered = true;
		}
		catch
		{
		}
	}

	private static void RegisterSeedRequestReceiver()
	{
		if (seedRequestRegistered)
		{
			return;
		}
		EnsureResolved();
		if (customServerReceiverDelegateType == null || netDataReaderType == null)
		{
			return;
		}
		MethodInfo methodInfo = FindServerRegisterReceiverMethod();
		if (methodInfo == null)
		{
			return;
		}
		try
		{
			object obj = CreateSeedRequestReceiverDelegate();
			methodInfo.Invoke(null, new object[2] { "QoLUnknown.v1.SeedRequest", obj });
			seedRequestRegistered = true;
		}
		catch
		{
		}
	}

	private static void RegisterClientInventoryRestoreRequestReceiver()
	{
		if (clientInventoryRestoreRequestRegistered)
		{
			return;
		}
		EnsureResolved();
		if (customServerReceiverDelegateType == null || netDataReaderType == null)
		{
			return;
		}
		MethodInfo methodInfo = FindServerRegisterReceiverMethod();
		if (methodInfo == null)
		{
			return;
		}
		try
		{
			object obj = CreateClientInventoryRestoreRequestReceiverDelegate();
			methodInfo.Invoke(null, new object[2] { "QoLUnknown.v1.InventoryRestoreRequest", obj });
			clientInventoryRestoreRequestRegistered = true;
		}
		catch
		{
		}
	}

	private static void RegisterClientInventoryRestoreResultReceiver()
	{
		if (clientInventoryRestoreResultRegistered)
		{
			return;
		}
		EnsureResolved();
		if (customClientReceiverDelegateType == null || netDataReaderType == null)
		{
			return;
		}
		MethodInfo methodInfo = FindClientRegisterReceiverMethod();
		if (methodInfo == null)
		{
			return;
		}
		try
		{
			object obj = CreateClientInventoryRestoreResultReceiverDelegate();
			methodInfo.Invoke(null, new object[2] { "QoLUnknown.v1.InventoryRestoreResult", obj });
			clientInventoryRestoreResultRegistered = true;
		}
		catch
		{
		}
	}

	private static void RegisterClientSpawnRequestReceiver()
	{
		if (clientSpawnRequestRegistered)
		{
			return;
		}
		EnsureResolved();
		if (customServerReceiverDelegateType == null || netDataReaderType == null)
		{
			return;
		}
		MethodInfo methodInfo = FindServerRegisterReceiverMethod();
		if (methodInfo == null)
		{
			return;
		}
		try
		{
			object obj = CreateClientSpawnRequestReceiverDelegate();
			if (obj != null)
			{
				methodInfo.Invoke(null, new object[2] { "QoLUnknown.v1.SpawnRequest", obj });
				clientSpawnRequestRegistered = true;
			}
		}
		catch
		{
		}
	}

	private static void RegisterClientSpawnResultReceiver()
	{
		if (clientSpawnResultRegistered)
		{
			return;
		}
		EnsureResolved();
		if (customClientReceiverDelegateType == null || netDataReaderType == null)
		{
			return;
		}
		MethodInfo methodInfo = FindClientRegisterReceiverMethod();
		if (methodInfo == null)
		{
			return;
		}
		try
		{
			object obj = CreateClientSpawnResultReceiverDelegate();
			methodInfo.Invoke(null, new object[2] { "QoLUnknown.v1.SpawnResult", obj });
			clientSpawnResultRegistered = true;
		}
		catch
		{
		}
	}

	private static void RegisterClientHealRequestReceiver()
	{
		if (clientHealRequestRegistered)
		{
			return;
		}
		EnsureResolved();
		if (customServerReceiverDelegateType == null || netDataReaderType == null)
		{
			return;
		}
		MethodInfo methodInfo = FindServerRegisterReceiverMethod();
		if (methodInfo == null)
		{
			return;
		}
		try
		{
			object obj = CreateClientHealRequestReceiverDelegate();
			if (obj != null)
			{
				methodInfo.Invoke(null, new object[2] { "QoLUnknown.v1.HealRequest", obj });
				clientHealRequestRegistered = true;
			}
		}
		catch
		{
		}
	}

	private static void RegisterClientHealResultReceiver()
	{
		if (clientHealResultRegistered)
		{
			return;
		}
		EnsureResolved();
		if (customClientReceiverDelegateType == null || netDataReaderType == null)
		{
			return;
		}
		MethodInfo methodInfo = FindClientRegisterReceiverMethod();
		if (methodInfo == null)
		{
			return;
		}
		try
		{
			object obj = CreateClientHealResultReceiverDelegate();
			methodInfo.Invoke(null, new object[2] { "QoLUnknown.v1.HealResult", obj });
			clientHealResultRegistered = true;
		}
		catch
		{
		}
	}

	private static void RegisterClientPrepareReconnectForHostLoadReceiver()
	{
		if (clientPrepareReconnectForHostLoadRegistered)
		{
			return;
		}
		EnsureResolved();
		if (customClientReceiverDelegateType == null || netDataReaderType == null)
		{
			return;
		}
		MethodInfo methodInfo = FindClientRegisterReceiverMethod();
		if (methodInfo == null)
		{
			return;
		}
		try
		{
			object obj = CreateClientPrepareReconnectForHostLoadReceiverDelegate();
			methodInfo.Invoke(null, new object[2] { "QoLUnknown.v1.PrepareReconnectForHostLoad", obj });
			clientPrepareReconnectForHostLoadRegistered = true;
		}
		catch
		{
		}
	}

	private static object CreateSeedSyncReceiverDelegate()
	{
		return CreateClientReceiverDelegate("QoL_TogetherSeedSyncReceiver", "HandleSeedSyncMessage");
	}

	private static object CreateSeedRequestReceiverDelegate()
	{
		return CreateServerReceiverDelegate("QoL_TogetherSeedRequestReceiver", "HandleSeedRequestMessage", includeReader: false);
	}

	private static object CreateClientInventoryRestoreRequestReceiverDelegate()
	{
		return CreateServerReceiverDelegate("QoL_TogetherClientInventoryRestoreRequestReceiver", "HandleClientInventoryRestoreRequestMessage", includeReader: true);
	}

	private static object CreateClientInventoryRestoreResultReceiverDelegate()
	{
		return CreateClientReceiverDelegate("QoL_TogetherClientInventoryRestoreResultReceiver", "HandleClientInventoryRestoreResultMessage");
	}

	private static object CreateClientSpawnRequestReceiverDelegate()
	{
		return CreateServerReceiverDelegate("QoL_TogetherClientSpawnRequestReceiver", "HandleClientSpawnRequestMessage", includeReader: true);
	}

	private static object CreateClientSpawnResultReceiverDelegate()
	{
		return CreateClientReceiverDelegate("QoL_TogetherClientSpawnResultReceiver", "HandleClientSpawnResultMessage");
	}

	private static object CreateClientHealRequestReceiverDelegate()
	{
		return CreateServerReceiverDelegate("QoL_TogetherClientHealRequestReceiver", "HandleClientHealRequestMessage", includeReader: false);
	}

	private static object CreateClientHealResultReceiverDelegate()
	{
		return CreateClientReceiverDelegate("QoL_TogetherClientHealResultReceiver", "HandleClientHealResultMessage");
	}

	private static object CreateClientPrepareReconnectForHostLoadReceiverDelegate()
	{
		return CreateClientReceiverDelegate("QoL_TogetherClientPrepareReconnectForHostLoadReceiver", "HandleClientPrepareReconnectForHostLoadMessage");
	}

	private static object CreateClientReceiverDelegate(string methodName, string handlerName)
	{
		EnsureResolved();
		MethodInfo methodInfo = customClientReceiverDelegateType?.GetMethod("Invoke");
		MethodInfo method = typeof(MultiplayerCompat).GetMethod(handlerName, BindingFlags.Static | BindingFlags.NonPublic);
		if (methodInfo == null || method == null)
		{
			return null;
		}
		ParameterInfo[] parameters = methodInfo.GetParameters();
		if (parameters.Length != 1)
		{
			return null;
		}
		Type type = UnwrapByRef(parameters[0].ParameterType);
		DynamicMethod dynamicMethod = new DynamicMethod(methodName, typeof(void), new Type[1] { type }, typeof(MultiplayerCompat).Module, skipVisibility: true);
		ILGenerator iLGenerator = dynamicMethod.GetILGenerator();
		iLGenerator.Emit(OpCodes.Ldc_I4_0);
		iLGenerator.Emit(OpCodes.Ldarg_0);
		iLGenerator.Emit(OpCodes.Call, method);
		iLGenerator.Emit(OpCodes.Ret);
		return dynamicMethod.CreateDelegate(customClientReceiverDelegateType);
	}

	private static object CreateServerReceiverDelegate(string methodName, string handlerName, bool includeReader)
	{
		EnsureResolved();
		MethodInfo methodInfo = customServerReceiverDelegateType?.GetMethod("Invoke");
		MethodInfo method = typeof(MultiplayerCompat).GetMethod(handlerName, BindingFlags.Static | BindingFlags.NonPublic);
		if (methodInfo == null || method == null)
		{
			return null;
		}
		ParameterInfo[] parameters = methodInfo.GetParameters();
		if (parameters.Length != 2)
		{
			return null;
		}
		Type type = UnwrapByRef(parameters[0].ParameterType);
		Type type2 = UnwrapByRef(parameters[1].ParameterType);
		DynamicMethod dynamicMethod = new DynamicMethod(methodName, typeof(void), new Type[2] { type, type2 }, typeof(MultiplayerCompat).Module, skipVisibility: true);
		ILGenerator iLGenerator = dynamicMethod.GetILGenerator();
		iLGenerator.Emit(OpCodes.Ldarg_0);
		iLGenerator.Emit(OpCodes.Call, typeof(MultiplayerCompat).GetMethod("GetPlayerClientId", BindingFlags.Static | BindingFlags.NonPublic));
		if (includeReader)
		{
			iLGenerator.Emit(OpCodes.Ldarg_1);
		}
		iLGenerator.Emit(OpCodes.Call, method);
		iLGenerator.Emit(OpCodes.Ret);
		return dynamicMethod.CreateDelegate(customServerReceiverDelegateType);
	}

	private static uint GetPlayerClientId(object player)
	{
		if (player == null)
		{
			return 0u;
		}
		object senderClientId = ReadInstanceMemberValue(player, "playerId");
		return ConvertSenderIdToUInt(senderClientId);
	}

	private static uint ConvertSenderIdToUInt(object senderClientId)
	{
		if (senderClientId == null)
		{
			return 0u;
		}
		try
		{
			Type type = senderClientId.GetType();
			if (type.IsEnum)
			{
				return Convert.ToUInt32(senderClientId, CultureInfo.InvariantCulture);
			}
			FieldInfo field = type.GetField("id", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (field != null)
			{
				object value = field.GetValue(senderClientId);
				if (value != null)
				{
					return Convert.ToUInt32(value, CultureInfo.InvariantCulture);
				}
			}
			PropertyInfo property = type.GetProperty("id", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (property != null)
			{
				object value2 = property.GetValue(senderClientId, null);
				if (value2 != null)
				{
					return Convert.ToUInt32(value2, CultureInfo.InvariantCulture);
				}
			}
			return Convert.ToUInt32(senderClientId, CultureInfo.InvariantCulture);
		}
		catch
		{
			return 0u;
		}
	}

	private static void PatchSeedAnnouncementHooks()
	{
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Expected O, but got Unknown
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Expected O, but got Unknown
		//IL_013a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0147: Expected O, but got Unknown
		if (seedAnnouncementHooksPatched)
		{
			return;
		}
		EnsureResolved();
		if (serverMainType == null || harmony == null)
		{
			return;
		}
		MethodInfo method = serverMainType.GetMethod("Server_Announce_GAME_START", BindingFlags.Static | BindingFlags.Public);
		MethodInfo method2 = typeof(MultiplayerCompat).GetMethod("ServerAnnounceGameStart_Prefix", BindingFlags.Static | BindingFlags.NonPublic);
		MethodInfo method3 = serverMainType.GetMethod("Server_AnnounceSeed", BindingFlags.Static | BindingFlags.Public);
		MethodInfo method4 = typeof(MultiplayerCompat).GetMethod("ServerAnnounceSeed_Prefix", BindingFlags.Static | BindingFlags.NonPublic);
		MethodInfo methodInfo = clientMainType?.GetMethod("ClientReceiver__GenerateWorld_SeedAnnounce", BindingFlags.Static | BindingFlags.NonPublic);
		MethodInfo method5 = typeof(MultiplayerCompat).GetMethod("ClientGenerateWorldSeedAnnounce_Postfix", BindingFlags.Static | BindingFlags.NonPublic);
		if (method == null || method2 == null || method3 == null || method4 == null || methodInfo == null || method5 == null)
		{
			return;
		}
		try
		{
			harmony.Patch((MethodBase)method, new HarmonyMethod(method2), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
			harmony.Patch((MethodBase)method3, new HarmonyMethod(method4), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
			harmony.Patch((MethodBase)methodInfo, (HarmonyMethod)null, new HarmonyMethod(method5), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
			seedAnnouncementHooksPatched = true;
		}
		catch
		{
		}
	}

	private static void PatchPingSuppressionHooks()
	{
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Expected O, but got Unknown
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Expected O, but got Unknown
		if (pingSuppressionHooksPatched || !pluginPresent)
		{
			return;
		}
		EnsureResolved();
		MethodInfo methodInfo = scavPlayerType?.GetMethod("PointFingerAt", BindingFlags.Instance | BindingFlags.Public);
		MethodInfo methodInfo2 = FindStaticMethod(netType, "Client_Send", new Type[2] { deliveryMethodType, netDataWriterType });
		MethodInfo method = typeof(MultiplayerCompat).GetMethod("PointFingerAtSuppressionPrefix", BindingFlags.Static | BindingFlags.NonPublic);
		MethodInfo method2 = typeof(MultiplayerCompat).GetMethod("ClientSendAfterPointFingerPrefix", BindingFlags.Static | BindingFlags.NonPublic);
		if (methodInfo == null || methodInfo2 == null || method == null || method2 == null)
		{
			return;
		}
		try
		{
			harmony.Patch((MethodBase)methodInfo, new HarmonyMethod(method), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
			harmony.Patch((MethodBase)methodInfo2, new HarmonyMethod(method2), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
			pingSuppressionHooksPatched = true;
		}
		catch
		{
		}
	}

	private static void PatchHostSaveLoadHooks()
	{
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Expected O, but got Unknown
		//IL_00d4: Expected O, but got Unknown
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Expected O, but got Unknown
		if (hostSaveLoadHooksPatched || harmony == null)
		{
			return;
		}
		EnsureResolved();
		if (multiplayerOnSceneLoadedMethod == null || serverFinishedWorldgenReceiverMethod == null)
		{
			return;
		}
		MethodInfo method = typeof(MultiplayerCompat).GetMethod("MultiplayerOnSceneLoaded_Prefix", BindingFlags.Static | BindingFlags.NonPublic);
		MethodInfo method2 = typeof(MultiplayerCompat).GetMethod("MultiplayerOnSceneLoaded_Postfix", BindingFlags.Static | BindingFlags.NonPublic);
		MethodInfo method3 = typeof(MultiplayerCompat).GetMethod("ServerReceiverFinishedWorldgen_Postfix", BindingFlags.Static | BindingFlags.NonPublic);
		if (method == null || method2 == null || method3 == null)
		{
			return;
		}
		try
		{
			harmony.Patch((MethodBase)multiplayerOnSceneLoadedMethod, new HarmonyMethod(method), new HarmonyMethod(method2), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
			harmony.Patch((MethodBase)serverFinishedWorldgenReceiverMethod, (HarmonyMethod)null, new HarmonyMethod(method3), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
			hostSaveLoadHooksPatched = true;
		}
		catch
		{
		}
	}

	private static void PatchCraftClickGuard()
	{
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Expected O, but got Unknown
		if (craftClickGuardPatched || !pluginPresent || craftTryClickMethod == null)
		{
			return;
		}
		MethodInfo method = typeof(MultiplayerCompat).GetMethod("CraftTryClickGuardPrefix", BindingFlags.Static | BindingFlags.NonPublic);
		if (method == null)
		{
			return;
		}
		try
		{
			harmony.Patch((MethodBase)craftTryClickMethod, new HarmonyMethod(method), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
			craftClickGuardPatched = true;
		}
		catch (Exception ex)
		{
			ManualLogSource logger = Logger;
			if (logger != null)
			{
				logger.LogWarning((object)("Failed to install the crafting click guard: " + ex.Message));
			}
		}
	}

	private static bool CraftTryClickGuardPrefix()
	{
		return handledCraftFrame != Time.frameCount;
	}

	private static bool PointFingerAtSuppressionPrefix(object __instance)
	{
		if (!IsLocalNetPlayer(__instance))
		{
			return true;
		}
		if (QuickStashPatcher.TryHandleQuickStashBeforeMultiplayerPing())
		{
			suppressNextPointFingerClientSend = true;
			return false;
		}
		if (!QuickStashPatcher.ShouldSuppressMultiplayerPointPingNow())
		{
			return true;
		}
		suppressNextPointFingerClientSend = true;
		return false;
	}

	private static bool IsLocalNetPlayer(object player)
	{
		if (player == null || localPlayerField == null)
		{
			return false;
		}
		try
		{
			return player == localPlayerField.GetValue(null);
		}
		catch
		{
			return false;
		}
	}

	private static bool ClientSendAfterPointFingerPrefix()
	{
		if (!suppressNextPointFingerClientSend)
		{
			return true;
		}
		suppressNextPointFingerClientSend = false;
		return false;
	}

	private static void MultiplayerOnSceneLoaded_Prefix()
	{
		if (preserveServerPlayerStatesForHostSaveLoad)
		{
			CaptureServerLastPlayerStatesForHostSaveLoad();
		}
	}

	private static void MultiplayerOnSceneLoaded_Postfix()
	{
		if (preservedServerPlayerStatesForHostSaveLoad == null || preservedServerPlayerStatesForHostSaveLoad.Count == 0)
		{
			preserveServerPlayerStatesForHostSaveLoad = false;
			preservedServerPlayerStatesForHostSaveLoad = null;
		}
		else
		{
			RestoreServerLastPlayerStatesAfterHostSaveLoad();
		}
	}

	private static void ServerReceiverFinishedWorldgen_Postfix(object __0)
	{
		if (!IsNetworkRunning || !IsServer || pendingClientInventoryRestorePayloadsByPersistentId == null || pendingClientInventoryRestorePayloadsByPersistentId.Count == 0)
		{
			return;
		}
		try
		{
			uint clientId = ConvertSenderIdToUInt(__0);
			Body bodyFromClientId = GetBodyFromClientId(clientId);
			if ((Object)(object)bodyFromClientId == (Object)null)
			{
				return;
			}
			object[] array = new object[3]
			{
				CoerceClientIdForMethod(netPlayerLookupMethod, clientId),
				null,
				null
			};
			object obj = netPlayerLookupMethod.Invoke(null, array);
			bool flag = default(bool);
			int num;
			if (obj is bool)
			{
				flag = (bool)obj;
				num = 1;
			}
			else
			{
				num = 0;
			}
			object obj2 = ((((uint)num & (flag ? 1u : 0u)) != 0) ? array[1] : null);
			string text = obj2?.GetType().GetMethod("GetPersistentId", BindingFlags.Instance | BindingFlags.Public)?.Invoke(obj2, null) as string;
			if (!string.IsNullOrWhiteSpace(text) && pendingClientInventoryRestorePayloadsByPersistentId.TryGetValue(text, out var value))
			{
				PlayerCamera main = PlayerCamera.main;
				if (main != null)
				{
					((MonoBehaviour)main).StartCoroutine(ApplyPostWorldgenClientInventoryRestoreCoroutine(clientId, text, value));
				}
			}
		}
		catch
		{
		}
	}

	private static IEnumerator ApplyPostWorldgenClientInventoryRestoreCoroutine(uint clientId, string persistentId, string payload)
	{
		yield return (object)new WaitForSeconds(0.5f);
		Body body = GetBodyFromClientId(clientId);
		if (!((Object)(object)body == (Object)null) && pendingClientInventoryRestorePayloadsByPersistentId.TryGetValue(persistentId, out var currentPayload) && string.Equals(currentPayload, payload, StringComparison.Ordinal) && MultiplayerSaveBundle.TryApplyClientInventoryPayload(body, payload, out var _))
		{
			SyncBodyInventory(body);
			pendingClientInventoryRestorePayloadsByPersistentId.Remove(persistentId);
			if (pendingClientInventoryRestorePayloadsByPersistentId.Count == 0)
			{
				pendingClientInventoryRestorePayloadsByPersistentId = null;
			}
		}
	}

	private static void ServerAnnounceGameStart_Prefix()
	{
		AnnounceCurrentSeedToClients();
	}

	private static void ServerAnnounceSeed_Prefix(object __0)
	{
		if (!ApplyPendingHostWorldgenState())
		{
			ApplyQoLSeedBeforeMultiplayerHostWorldgenCapture();
		}
		AnnounceCurrentSeedToTargets(__0);
	}

	private static void ClientGenerateWorldSeedAnnounce_Postfix()
	{
		if (!SeedManager.IsSeeded)
		{
			RequestSeedResend();
		}
	}

	private static bool ApplyQoLSeedBeforeMultiplayerHostWorldgenCapture()
	{
		if (!IsNetworkRunning || !IsServer || !SeedManager.IsSeeded || HasPendingHostWorldgenState)
		{
			return false;
		}
		try
		{
			int currentLayerSeed = SeededRunPatcher.GetCurrentLayerSeed();
			Random.InitState(currentLayerSeed);
			if (firstWorldgenParamsField != null && lastBeforeGenerationStateType != null)
			{
				object obj = Activator.CreateInstance(lastBeforeGenerationStateType);
				firstWorldgenParamsField.SetValue(null, obj);
				lastBeforeGenerationStateApplyMethod?.Invoke(obj, null);
			}
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static void CaptureServerLastPlayerStatesForHostSaveLoad()
	{
		EnsureResolved();
		if (serverLastPlayerStatesField == null)
		{
			preserveServerPlayerStatesForHostSaveLoad = false;
			return;
		}
		try
		{
			if (!(serverLastPlayerStatesField.GetValue(null) is IDictionary dictionary))
			{
				preserveServerPlayerStatesForHostSaveLoad = false;
				return;
			}
			preservedServerPlayerStatesForHostSaveLoad = new List<DictionaryEntry>(dictionary.Count);
			foreach (DictionaryEntry item in dictionary)
			{
				preservedServerPlayerStatesForHostSaveLoad.Add(item);
			}
		}
		catch
		{
			preserveServerPlayerStatesForHostSaveLoad = false;
			preservedServerPlayerStatesForHostSaveLoad = null;
		}
	}

	private static void RestoreServerLastPlayerStatesAfterHostSaveLoad()
	{
		EnsureResolved();
		try
		{
			if (!(serverLastPlayerStatesField?.GetValue(null) is IDictionary dictionary))
			{
				return;
			}
			foreach (DictionaryEntry item in preservedServerPlayerStatesForHostSaveLoad)
			{
				dictionary[item.Key] = item.Value;
			}
		}
		catch
		{
		}
		finally
		{
			preserveServerPlayerStatesForHostSaveLoad = false;
			preservedServerPlayerStatesForHostSaveLoad = null;
		}
	}

	private static bool ApplyPendingHostWorldgenState()
	{
		if (string.IsNullOrEmpty(pendingHostWorldgenStatePayload) || !IsNetworkRunning || !IsServer)
		{
			return false;
		}
		EnsureResolved();
		if (firstWorldgenParamsField == null || lastBeforeGenerationStateType == null)
		{
			return false;
		}
		string payload = pendingHostWorldgenStatePayload;
		pendingHostWorldgenStatePayload = null;
		try
		{
			if (!TryDeserializeLastBeforeGenerationState(payload, out var state) || state == null)
			{
				return false;
			}
			firstWorldgenParamsField.SetValue(null, state);
			lastBeforeGenerationStateApplyMethod?.Invoke(state, null);
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static bool TrySerializeLastBeforeGenerationState(object state, out string payload)
	{
		payload = null;
		EnsureResolved();
		if (state == null || lastBeforeGenerationStateType == null || netDataWriterType == null || netExtensionsType == null)
		{
			return false;
		}
		try
		{
			object obj = Activator.CreateInstance(netDataWriterType);
			MethodInfo methodInfo = FindNetworkMemcpyMethod("Put", netDataWriterType, lastBeforeGenerationStateType);
			MethodInfo methodInfo2 = FindStaticMethod(netExtensionsType, "CopyData", new Type[2]
			{
				netDataWriterType,
				typeof(int)
			});
			if (methodInfo == null || methodInfo2 == null)
			{
				return false;
			}
			methodInfo.Invoke(null, new object[2] { obj, state });
			if (!(methodInfo2.Invoke(null, new object[2] { obj, 0 }) is byte[] array) || array.Length == 0)
			{
				return false;
			}
			payload = Convert.ToBase64String(array);
			return true;
		}
		catch
		{
			payload = null;
			return false;
		}
	}

	private static bool TryDeserializeLastBeforeGenerationState(string payload, out object state)
	{
		state = null;
		EnsureResolved();
		if (string.IsNullOrWhiteSpace(payload) || lastBeforeGenerationStateType == null || netDataReaderType == null || netExtensionsType == null)
		{
			return false;
		}
		try
		{
			byte[] array = Convert.FromBase64String(payload.Trim());
			object obj = Activator.CreateInstance(netDataReaderType, array);
			MethodInfo methodInfo = FindNetworkMemcpyMethod("Get", netDataReaderType, lastBeforeGenerationStateType.MakeByRefType());
			if (methodInfo == null)
			{
				return false;
			}
			object[] array2 = new object[2]
			{
				obj,
				Activator.CreateInstance(lastBeforeGenerationStateType)
			};
			methodInfo.Invoke(null, array2);
			state = array2[1];
			return state != null;
		}
		catch
		{
			state = null;
			return false;
		}
	}

	private static MethodInfo FindNetworkMemcpyMethod(string name, Type firstParameter, Type secondParameter)
	{
		if (netExtensionsType == null || firstParameter == null || secondParameter == null)
		{
			return null;
		}
		MethodInfo[] methods = netExtensionsType.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		foreach (MethodInfo methodInfo in methods)
		{
			if (methodInfo.Name != name)
			{
				continue;
			}
			ParameterInfo[] parameters = methodInfo.GetParameters();
			if (parameters.Length != 2 || parameters[0].ParameterType != firstParameter)
			{
				continue;
			}
			if (parameters[1].ParameterType == secondParameter)
			{
				return methodInfo;
			}
			if (!methodInfo.IsGenericMethodDefinition || methodInfo.GetGenericArguments().Length != 1)
			{
				continue;
			}
			try
			{
				MethodInfo methodInfo2 = methodInfo.MakeGenericMethod(lastBeforeGenerationStateType);
				ParameterInfo[] parameters2 = methodInfo2.GetParameters();
				if (parameters2.Length == 2 && parameters2[1].ParameterType == secondParameter)
				{
					return methodInfo2;
				}
			}
			catch
			{
			}
		}
		return null;
	}

	private static void AnnounceCurrentSeedToTargets(object targets)
	{
		if (IsNetworkRunning && IsServer && SeedManager.IsSeeded)
		{
			string payload = BuildSeedPayload(SeedManager.CurrentSeed, SeedManager.InputString);
			SendNamedStringToTargets("QoLUnknown.v1.SeedSync", payload, targets);
		}
	}

	private static void HandleSeedRequestMessage(uint senderClientId)
	{
		try
		{
			if (IsNetworkRunning && IsServer && SeedManager.IsSeeded)
			{
				string payload = BuildSeedPayload(SeedManager.CurrentSeed, SeedManager.InputString);
				SendNamedStringToClient("QoLUnknown.v1.SeedSync", payload, senderClientId);
			}
		}
		catch
		{
		}
	}

	private static void HandleClientInventoryRestoreRequestMessage(uint senderClientId, object reader)
	{
		try
		{
			if (!IsNetworkRunning || !IsServer || reader == null)
			{
				return;
			}
			if (!ClientCheatCommandsAllowed())
			{
				SendClientInventoryRestoreResult(senderClientId, ok: false, "Host has sv_cheats or client commands disabled.");
				return;
			}
			string payload = ReadNamedUtf8Payload(reader);
			if (!TryAcceptInventoryRestoreChunk(senderClientId, payload, out var completedPayload, out var errorMessage))
			{
				SendClientInventoryRestoreResult(senderClientId, ok: false, errorMessage ?? "Inventory snapshot transfer was invalid.");
			}
			else if (completedPayload != null)
			{
				Body bodyFromClientId = GetBodyFromClientId(senderClientId);
				if ((Object)(object)bodyFromClientId == (Object)null)
				{
					SendClientInventoryRestoreResult(senderClientId, ok: false, "Host could not find your player body.");
					return;
				}
				if (!MultiplayerSaveBundle.TryApplyClientInventoryPayload(bodyFromClientId, completedPayload, out var message))
				{
					SendClientInventoryRestoreResult(senderClientId, ok: false, message ?? "Inventory restore failed.");
					return;
				}
				SyncBodyInventory(bodyFromClientId);
				SendClientInventoryRestoreResult(senderClientId, ok: true, "Restored inventory from QoL slot.");
			}
		}
		catch
		{
			SendClientInventoryRestoreResult(senderClientId, ok: false, "Inventory restore failed on host.");
		}
	}

	private static void HandleClientInventoryRestoreResultMessage(uint senderClientId, object reader)
	{
		try
		{
			string text = ReadNamedUtf8Payload(reader);
			if (!string.IsNullOrWhiteSpace(text))
			{
				string[] array = text.Split(new char[1] { '|' }, 2);
				bool flag = array.Length != 0 && array[0] == "1";
				string text2 = ((array.Length > 1) ? Encoding.UTF8.GetString(Convert.FromBase64String(array[1])) : (flag ? "Inventory restored." : "Inventory restore failed."));
				if ((Object)(object)PlayerCamera.main != (Object)null)
				{
					PlayerCamera.main.DoAlert(text2, false);
				}
			}
		}
		catch
		{
		}
	}

	private static void HandleClientSpawnRequestMessage(uint senderClientId, object reader)
	{
		try
		{
			if (!IsNetworkRunning || !IsServer || reader == null)
			{
				return;
			}
			if (!ClientCheatCommandsAllowed())
			{
				SendClientSpawnResult(senderClientId, ok: false, "Host has sv_cheats or client commands disabled.");
				return;
			}
			string text = ReadNamedUtf8Payload(reader);
			if (string.IsNullOrWhiteSpace(text))
			{
				SendClientSpawnResult(senderClientId, ok: false, "Spawn command was empty.");
				return;
			}
			if (!text.TrimStart(Array.Empty<char>()).StartsWith("spawn ", StringComparison.OrdinalIgnoreCase))
			{
				SendClientSpawnResult(senderClientId, ok: false, "Only spawn commands can be forwarded.");
				return;
			}
			Body bodyFromClientId = GetBodyFromClientId(senderClientId);
			string resultMessage;
			if ((Object)(object)bodyFromClientId == (Object)null || !bodyFromClientId.conscious)
			{
				SendClientSpawnResult(senderClientId, ok: false, "Host could not find your active player body.");
			}
			else if (!TryExecuteClientSpawnCommand(text, bodyFromClientId, out resultMessage))
			{
				SendClientSpawnResult(senderClientId, ok: false, string.IsNullOrWhiteSpace(resultMessage) ? "Spawn request failed on host." : resultMessage);
			}
			else
			{
				SendClientSpawnResult(senderClientId, ok: true, resultMessage);
			}
		}
		catch
		{
			SendClientSpawnResult(senderClientId, ok: false, "Spawn request failed on host.");
		}
	}

	private static void HandleClientSpawnResultMessage(uint senderClientId, object reader)
	{
		try
		{
			string text = ReadNamedUtf8Payload(reader);
			if (!string.IsNullOrWhiteSpace(text))
			{
				string[] array = text.Split(new char[1] { '|' }, 2);
				bool flag = array.Length != 0 && array[0] == "1";
				string text2 = ((array.Length > 1) ? Encoding.UTF8.GetString(Convert.FromBase64String(array[1])) : (flag ? "Spawn request completed." : "Spawn request failed."));
				if ((Object)(object)PlayerCamera.main != (Object)null)
				{
					PlayerCamera.main.DoAlert(text2, false);
				}
			}
		}
		catch
		{
		}
	}

	private static void HandleClientHealRequestMessage(uint senderClientId)
	{
		try
		{
			if (!IsNetworkRunning || !IsServer)
			{
				return;
			}
			if (!ClientCheatCommandsAllowed())
			{
				SendClientHealResult(senderClientId, ok: false, "Host has sv_cheats or client commands disabled.");
				return;
			}
			object playerFromClientId = GetPlayerFromClientId(senderClientId);
			string value = ReadInstanceMemberValue(playerFromClientId, "playerName") as string;
			if (playerFromClientId == null || string.IsNullOrWhiteSpace(value))
			{
				SendClientHealResult(senderClientId, ok: false, "Host could not find your player name.");
				return;
			}
			MethodInfo method = playerFromClientId.GetType().GetMethod("Server_HealCharacter", BindingFlags.Instance | BindingFlags.Public);
			if (method == null)
			{
				SendClientHealResult(senderClientId, ok: false, "Together's server heal operation is unavailable.");
				return;
			}
			method.Invoke(playerFromClientId, null);
			Body bodyFromClientId = GetBodyFromClientId(senderClientId);
			if ((Object)(object)bodyFromClientId != (Object)null)
			{
				TryRaiseCUCoreLibHealEvent(bodyFromClientId);
			}
			SendClientHealResult(senderClientId, ok: true, "Healed player on host.");
		}
		catch
		{
			SendClientHealResult(senderClientId, ok: false, "Heal request failed on host.");
		}
	}

	private static void HandleClientHealResultMessage(uint senderClientId, object reader)
	{
		try
		{
			string text = ReadNamedUtf8Payload(reader);
			if (!string.IsNullOrWhiteSpace(text))
			{
				string[] array = text.Split(new char[1] { '|' }, 2);
				bool flag = array.Length != 0 && array[0] == "1";
				string text2 = ((array.Length > 1) ? Encoding.UTF8.GetString(Convert.FromBase64String(array[1])) : (flag ? "Heal request completed." : "Heal request failed."));
				if ((Object)(object)PlayerCamera.main != (Object)null)
				{
					PlayerCamera.main.DoAlert(text2, false);
				}
			}
		}
		catch
		{
		}
	}

	private static void HandleClientPrepareReconnectForHostLoadMessage(uint senderClientId, object reader)
	{
		if (!IsNetworkRunning || !IsClient)
		{
			return;
		}
		ManualLogSource logger = Logger;
		if (logger != null)
		{
			logger.LogInfo((object)"[MultiplayerCompat] Client received host-load prepare-reconnect; leaving the world before the host re-announces.");
		}
		if (TryCaptureCurrentReconnectTarget(out var useSteam, out var ipPort, out var lobbyId))
		{
			autoReconnectAfterHostLoadArmed = true;
			autoReconnectAfterHostLoadUseSteam = useSteam;
			autoReconnectAfterHostLoadIpPort = ipPort;
			autoReconnectAfterHostLoadLobbyId = lobbyId;
			autoReconnectAfterHostLoadAttempts = 0;
			autoReconnectAfterHostLoadNextAttemptTime = Time.realtimeSinceStartup + 0.5f;
			autoReconnectAfterHostLoadAttemptInFlight = false;
			autoReconnectAfterHostLoadMainMenuLogged = false;
			if (!autoReconnectAfterHostLoadCoroutineRunning)
			{
				autoReconnectAfterHostLoadCoroutineRunning = true;
				SharedCoroutineStarter.StartCoroutine(AutoReconnectAfterHostLoadCoroutine());
			}
		}
		else
		{
			ManualLogSource logger2 = Logger;
			if (logger2 != null)
			{
				logger2.LogWarning((object)"[MultiplayerCompat] Could not capture reconnect target for host load; returning to menu without auto-rejoin.");
			}
		}
		TryGoBackToMainMenuAsClient();
	}

	private static void TryGoBackToMainMenuAsClient()
	{
		EnsureResolved();
		try
		{
			if (goBackToMainMenuMethod != null)
			{
				goBackToMainMenuMethod.Invoke(null, null);
				ManualLogSource logger = Logger;
				if (logger != null)
				{
					logger.LogInfo((object)"[MultiplayerCompat] Invoked Together GoBackToMainMenuIfImClientAndInGame on host load.");
				}
				return;
			}
			ManualLogSource logger2 = Logger;
			if (logger2 != null)
			{
				logger2.LogWarning((object)"[MultiplayerCompat] Together GoBackToMainMenuIfImClientAndInGame not found; falling back to ToMainMenu.");
			}
		}
		catch (Exception ex)
		{
			ManualLogSource logger3 = Logger;
			if (logger3 != null)
			{
				logger3.LogWarning((object)("[MultiplayerCompat] GoBackToMainMenuIfImClientAndInGame threw: " + ex.Message));
			}
		}
		if ((Object)(object)PlayerCamera.main != (Object)null)
		{
			showMultiplayerMenuField?.SetValue(null, true);
			PlayerCamera.main.ToMainMenu();
		}
	}

	private static bool TryCaptureCurrentReconnectTarget(out bool useSteam, out string ipPort, out ulong lobbyId)
	{
		useSteam = false;
		ipPort = null;
		lobbyId = 0uL;
		EnsureResolved();
		if (netType == null)
		{
			return false;
		}
		useSteam = ReadStaticBool(netType, "IsRunningSteam");
		if (useSteam)
		{
			object value = ReadStaticMemberValue(kSteamType, "LobbyId");
			if (TryConvertToUInt64(value, out lobbyId) && lobbyId != 0)
			{
				return true;
			}
			object obj = ReadStaticMemberValue(kSteamType, "CURRENT_LOBBY");
			object value2 = obj?.GetType().GetField("lobby_steamID", BindingFlags.Instance | BindingFlags.Public)?.GetValue(obj);
			return TryConvertToUInt64(value2, out lobbyId) && lobbyId != 0;
		}
		ipPort = ReadStaticMemberValue(uiMainMenuType, "USERINPUT_IPPORT") as string;
		return !string.IsNullOrWhiteSpace(ipPort);
	}

	private static IEnumerator AutoReconnectAfterHostLoadCoroutine()
	{
		while (autoReconnectAfterHostLoadArmed)
		{
			if (!IsMainMenuSceneActive())
			{
				yield return null;
				continue;
			}
			if (!autoReconnectAfterHostLoadMainMenuLogged)
			{
				autoReconnectAfterHostLoadMainMenuLogged = true;
			}
			if (IsNetworkRunning)
			{
				if (autoReconnectAfterHostLoadAttemptInFlight)
				{
					if (IsClient)
					{
						ClearAutoReconnectAfterHostLoad();
						break;
					}
					yield return null;
				}
				else if (IsClient)
				{
					TryForceClientDisconnect();
					autoReconnectAfterHostLoadNextAttemptTime = Time.realtimeSinceStartup + 1f;
					yield return (object)new WaitForSecondsRealtime(0.5f);
				}
				else if (Time.realtimeSinceStartup < autoReconnectAfterHostLoadNextAttemptTime)
				{
					yield return null;
				}
				else
				{
					autoReconnectAfterHostLoadNextAttemptTime = Time.realtimeSinceStartup + 1f;
					yield return (object)new WaitForSecondsRealtime(0.25f);
				}
				continue;
			}
			if (autoReconnectAfterHostLoadAttemptInFlight)
			{
				if (Time.realtimeSinceStartup < autoReconnectAfterHostLoadNextAttemptTime)
				{
					yield return null;
					continue;
				}
				autoReconnectAfterHostLoadAttemptInFlight = false;
			}
			if (Time.realtimeSinceStartup < autoReconnectAfterHostLoadNextAttemptTime)
			{
				yield return null;
				continue;
			}
			if (autoReconnectAfterHostLoadAttempts >= 10)
			{
				ClearAutoReconnectAfterHostLoad();
				break;
			}
			autoReconnectAfterHostLoadAttempts++;
			bool started = TryStartPreparedReconnect();
			autoReconnectAfterHostLoadNextAttemptTime = Time.realtimeSinceStartup + 3f;
			if (!started)
			{
				autoReconnectAfterHostLoadAttemptInFlight = false;
			}
			else
			{
				autoReconnectAfterHostLoadAttemptInFlight = true;
			}
			yield return (object)new WaitForSecondsRealtime(0.5f);
		}
		autoReconnectAfterHostLoadCoroutineRunning = false;
	}

	private static bool IsMainMenuSceneActive()
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			Scene activeScene = SceneManager.GetActiveScene();
			return ((Scene)(ref activeScene)).name == "PreGen";
		}
		catch
		{
			return false;
		}
	}

	private static void TryForceClientDisconnect()
	{
		EnsureResolved();
		try
		{
			forceDisconnectMethod?.Invoke(null, null);
		}
		catch
		{
		}
	}

	private static bool TryStartPreparedReconnect()
	{
		EnsureResolved();
		try
		{
			PrepareMultiplayerMenuForReconnect();
			object obj;
			if (autoReconnectAfterHostLoadUseSteam)
			{
				if (transportSteamJoinLobbyMethod == null || autoReconnectAfterHostLoadLobbyId == 0)
				{
					return false;
				}
				multiplayerStatusLogMethod?.Invoke(null, new object[1] { "Client reconnect clicked!" });
				obj = transportSteamJoinLobbyMethod.Invoke(null, new object[1] { autoReconnectAfterHostLoadLobbyId });
				bool flag = default(bool);
				int num;
				if (obj is bool)
				{
					flag = (bool)obj;
					num = 1;
				}
				else
				{
					num = 0;
				}
				bool flag2 = (byte)((uint)num & (flag ? 1u : 0u)) != 0;
				if (flag2)
				{
				}
				return flag2;
			}
			if (transportLiteConnectMethod == null || string.IsNullOrWhiteSpace(autoReconnectAfterHostLoadIpPort) || netType == null)
			{
				return false;
			}
			if (netNetTypeType == null)
			{
				return false;
			}
			object obj2 = Enum.Parse(netNetTypeType, "Client");
			multiplayerStatusLogMethod?.Invoke(null, new object[1] { "Client Connect clicked!" });
			obj = transportLiteConnectMethod.Invoke(null, new object[2] { autoReconnectAfterHostLoadIpPort, obj2 });
			bool flag3 = default(bool);
			int num2;
			if (obj is bool)
			{
				flag3 = (bool)obj;
				num2 = 1;
			}
			else
			{
				num2 = 0;
			}
			bool flag4 = (byte)((uint)num2 & (flag3 ? 1u : 0u)) != 0;
			if (flag4)
			{
			}
			return flag4;
		}
		catch
		{
			return false;
		}
	}

	private static void PrepareMultiplayerMenuForReconnect()
	{
		EnsureResolved();
		try
		{
			showMultiplayerMenuField?.SetValue(null, true);
			if (uiCurrentTabField != null && uiDirectConnectTabField != null)
			{
				object value = uiDirectConnectTabField.GetValue(null);
				if (value != null)
				{
					uiCurrentTabField.SetValue(null, value);
					if (value.GetType().GetField("on_open", BindingFlags.Instance | BindingFlags.Public)?.GetValue(value) is Action action)
					{
						action();
					}
				}
			}
			uiMainMenuSetOpenMethod?.Invoke(null, new object[1] { true });
		}
		catch
		{
		}
	}

	private static void ClearAutoReconnectAfterHostLoad()
	{
		autoReconnectAfterHostLoadArmed = false;
		autoReconnectAfterHostLoadUseSteam = false;
		autoReconnectAfterHostLoadIpPort = null;
		autoReconnectAfterHostLoadLobbyId = 0uL;
		autoReconnectAfterHostLoadAttempts = 0;
		autoReconnectAfterHostLoadNextAttemptTime = 0f;
		autoReconnectAfterHostLoadAttemptInFlight = false;
		autoReconnectAfterHostLoadMainMenuLogged = false;
	}

	private static void HandleSeedSyncMessage(uint senderClientId, object reader)
	{
		try
		{
			string payload = ReadNamedUtf8Payload(reader);
			if (TryParseSeedPayload(payload, out var seed, out var input))
			{
				SeedManager.SetNetworkSeed(seed, input);
			}
		}
		catch
		{
		}
	}

	private static string BuildSeedPayload(int seed, string input)
	{
		string text = Convert.ToBase64String(Encoding.UTF8.GetBytes(input ?? ""));
		return "1|" + seed.ToString(CultureInfo.InvariantCulture) + "|" + text;
	}

	private static bool TryParseSeedPayload(string payload, out int seed, out string input)
	{
		seed = 0;
		input = "";
		if (string.IsNullOrEmpty(payload))
		{
			return false;
		}
		string[] array = payload.Split(new char[1] { '|' }, 3);
		if (array.Length != 3 || array[0] != "1" || !int.TryParse(array[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out seed))
		{
			return false;
		}
		try
		{
			input = Encoding.UTF8.GetString(Convert.FromBase64String(array[2]));
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static string ReadNamedUtf8Payload(object reader)
	{
		EnsureResolved();
		if (reader == null || readerGetRemainingBytesMethod == null || readerAvailableBytesProperty == null)
		{
			return null;
		}
		try
		{
			if (!(readerAvailableBytesProperty.GetValue(reader, null) is int num) || num < 0 || num > 1048576)
			{
				return null;
			}
			return (!(readerGetRemainingBytesMethod.Invoke(reader, null) is byte[] bytes)) ? null : Encoding.UTF8.GetString(bytes);
		}
		catch
		{
			return null;
		}
	}

	private static uint ReadUInt(object reader)
	{
		if (reader == null)
		{
			return 0u;
		}
		try
		{
			return (readerGetUIntMethod?.Invoke(reader, null) is uint num) ? num : 0u;
		}
		catch
		{
			return 0u;
		}
	}

	private static Body GetBodyFromClientId(uint clientId)
	{
		EnsureResolved();
		if (netPlayerLookupMethod == null)
		{
			return null;
		}
		try
		{
			object[] array = new object[3]
			{
				CoerceClientIdForMethod(netPlayerLookupMethod, clientId),
				null,
				null
			};
			object obj = netPlayerLookupMethod.Invoke(null, array);
			bool flag = default(bool);
			int num;
			if (obj is bool)
			{
				flag = (bool)obj;
				num = 1;
			}
			else
			{
				num = 0;
			}
			object result;
			if (((uint)num & (flag ? 1u : 0u)) == 0)
			{
				result = null;
			}
			else
			{
				object obj2 = array[2];
				result = ((obj2 is Body) ? obj2 : null);
			}
			return (Body)result;
		}
		catch
		{
			return null;
		}
	}

	private static bool ClientCheatCommandsAllowed()
	{
		EnsureResolved();
		if (multiplayerRulesField == null)
		{
			return false;
		}
		try
		{
			object value = multiplayerRulesField.GetValue(null);
			if (value == null)
			{
				return false;
			}
			FieldInfo field = value.GetType().GetField("sv_cheats", BindingFlags.Instance | BindingFlags.Public);
			FieldInfo field2 = value.GetType().GetField("AllowClientCheatCommands", BindingFlags.Instance | BindingFlags.Public);
			bool flag = default(bool);
			int num;
			if (field != null && field2 != null)
			{
				object value2 = field.GetValue(value);
				if (value2 is bool)
				{
					flag = (bool)value2;
					num = 1;
				}
				else
				{
					num = 0;
				}
			}
			else
			{
				num = 0;
			}
			bool flag2 = default(bool);
			int num2;
			if (((uint)num & (flag ? 1u : 0u)) != 0)
			{
				object value2 = field2.GetValue(value);
				if (value2 is bool)
				{
					flag2 = (bool)value2;
					num2 = 1;
				}
				else
				{
					num2 = 0;
				}
			}
			else
			{
				num2 = 0;
			}
			return (byte)((uint)num2 & (flag2 ? 1u : 0u)) != 0;
		}
		catch
		{
			return false;
		}
	}

	private static void TryRaiseCUCoreLibHealEvent(Body body)
	{
		try
		{
			Type type = null;
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			foreach (Assembly assembly in assemblies)
			{
				type = assembly.GetType("CUCoreLib.Helpers.CUCoreUtils");
				if (type != null)
				{
					break;
				}
			}
			if (!(type == null))
			{
				MethodInfo method = type.GetMethod("RaiseOnHeal", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
				if (method != null)
				{
					method.Invoke(null, new object[1] { body });
				}
			}
		}
		catch
		{
		}
	}

	private static void SendClientInventoryRestoreResult(uint clientId, bool ok, string message)
	{
		string text = Convert.ToBase64String(Encoding.UTF8.GetBytes(message ?? ""));
		SendNamedStringToClient("QoLUnknown.v1.InventoryRestoreResult", (ok ? "1" : "0") + "|" + text, clientId);
	}

	private static bool TryAcceptInventoryRestoreChunk(uint senderClientId, string payload, out string completedPayload, out string errorMessage)
	{
		completedPayload = null;
		errorMessage = null;
		if (string.IsNullOrWhiteSpace(payload))
		{
			errorMessage = "Inventory snapshot transfer was empty.";
			return false;
		}
		string[] array = payload.Split(new char[1] { '|' }, 6);
		if (array.Length != 6 || array[0] != "1" || array[1].Length != 32 || !Guid.TryParseExact(array[1], "N", out var _) || !int.TryParse(array[2], NumberStyles.None, CultureInfo.InvariantCulture, out var result2) || !int.TryParse(array[3], NumberStyles.None, CultureInfo.InvariantCulture, out var result3) || !int.TryParse(array[4], NumberStyles.None, CultureInfo.InvariantCulture, out var result4))
		{
			errorMessage = "Inventory snapshot transfer header was invalid.";
			return false;
		}
		int num = 32;
		if (result3 < 1 || result3 > num || result2 < 0 || result2 >= result3 || result4 < 1 || result4 > 1048576 || result3 != (result4 + 32768 - 1) / 32768)
		{
			errorMessage = "Inventory snapshot transfer exceeded its limits.";
			return false;
		}
		byte[] array2;
		try
		{
			array2 = Convert.FromBase64String(array[5]);
		}
		catch
		{
			errorMessage = "Inventory snapshot transfer data was invalid.";
			return false;
		}
		int num2 = Math.Min(32768, result4 - result2 * 32768);
		if (array2.Length != num2)
		{
			errorMessage = "Inventory snapshot transfer chunk length was invalid.";
			return false;
		}
		float now = Time.realtimeSinceStartup;
		uint[] array3 = (from entry in incomingInventoryRestoreChunks
			where now - entry.Value.LastReceivedTime > 30f
			select entry.Key).ToArray();
		foreach (uint key in array3)
		{
			incomingInventoryRestoreChunks.Remove(key);
		}
		if (!incomingInventoryRestoreChunks.TryGetValue(senderClientId, out var value) || value.RequestId != array[1] || value.TotalLength != result4 || value.Chunks.Length != result3 || now - value.LastReceivedTime > 30f)
		{
			value = new InventoryRestoreChunkAssembly(array[1], result3, result4, now);
			incomingInventoryRestoreChunks[senderClientId] = value;
		}
		value.LastReceivedTime = now;
		byte[] array4 = value.Chunks[result2];
		if (array4 != null)
		{
			if (!array4.SequenceEqual(array2))
			{
				incomingInventoryRestoreChunks.Remove(senderClientId);
				errorMessage = "Inventory snapshot transfer contained a conflicting chunk.";
				return false;
			}
			return true;
		}
		value.Chunks[result2] = array2;
		value.ReceivedChunkCount++;
		value.ReceivedLength += array2.Length;
		if (value.ReceivedChunkCount != result3)
		{
			return true;
		}
		if (value.ReceivedLength != result4)
		{
			incomingInventoryRestoreChunks.Remove(senderClientId);
			errorMessage = "Inventory snapshot transfer length did not match.";
			return false;
		}
		byte[] array5 = new byte[result4];
		int num4 = 0;
		for (int num5 = 0; num5 < value.Chunks.Length; num5++)
		{
			byte[] array6 = value.Chunks[num5];
			if (array6 == null)
			{
				incomingInventoryRestoreChunks.Remove(senderClientId);
				errorMessage = "Inventory snapshot transfer was incomplete.";
				return false;
			}
			Buffer.BlockCopy(array6, 0, array5, num4, array6.Length);
			num4 += array6.Length;
		}
		incomingInventoryRestoreChunks.Remove(senderClientId);
		completedPayload = Encoding.UTF8.GetString(array5);
		return true;
	}

	private static void SendClientSpawnResult(uint clientId, bool ok, string message)
	{
		string text = Convert.ToBase64String(Encoding.UTF8.GetBytes(message ?? ""));
		SendNamedStringToClient("QoLUnknown.v1.SpawnResult", (ok ? "1" : "0") + "|" + text, clientId);
	}

	private static void SendClientHealResult(uint clientId, bool ok, string message)
	{
		string text = Convert.ToBase64String(Encoding.UTF8.GetBytes(message ?? ""));
		SendNamedStringToClient("QoLUnknown.v1.HealResult", (ok ? "1" : "0") + "|" + text, clientId);
	}

	private static void SyncBodyInventory(Body body)
	{
		if (!((Object)(object)body == (Object)null))
		{
			Item[] componentsInChildren = ((Component)body).GetComponentsInChildren<Item>(true);
			foreach (Item item in componentsInChildren)
			{
				RegisterAndSyncItemTree(item);
			}
		}
	}

	private static bool TryExecuteClientSpawnCommand(string commandString, Body requesterBody, out string resultMessage)
	{
		//IL_0167: Unknown result type (might be due to invalid IL or missing references)
		//IL_0175: Unknown result type (might be due to invalid IL or missing references)
		//IL_0183: Unknown result type (might be due to invalid IL or missing references)
		//IL_0191: Unknown result type (might be due to invalid IL or missing references)
		//IL_019f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0223: Unknown result type (might be due to invalid IL or missing references)
		//IL_0334: Unknown result type (might be due to invalid IL or missing references)
		//IL_0288: Unknown result type (might be due to invalid IL or missing references)
		//IL_028e: Invalid comparison between Unknown and I4
		//IL_02b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cd: Unknown result type (might be due to invalid IL or missing references)
		resultMessage = null;
		if (string.IsNullOrWhiteSpace(commandString) || (Object)(object)requesterBody == (Object)null)
		{
			resultMessage = "Spawn command was empty.";
			return false;
		}
		if ((Object)(object)WorldGeneration.world == (Object)null)
		{
			resultMessage = "No active world on host.";
			return false;
		}
		string[] array = commandString.Split(new char[1] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
		if (array.Length < 3 || !array[0].Equals("spawn", StringComparison.OrdinalIgnoreCase))
		{
			resultMessage = "Forwarded spawn commands require a resolved numeric position.";
			return false;
		}
		string text = array[1];
		if (array.Length > 5 || text.Length > 128)
		{
			resultMessage = "Spawn request contains too many or oversized arguments.";
			return false;
		}
		float result = 1f;
		int result2 = 1;
		if (!TryParseSpawnPosition(array[2], out var position, out resultMessage))
		{
			return false;
		}
		if (array.Length > 3 && !float.TryParse(array[3], NumberStyles.Float, CultureInfo.InvariantCulture, out result))
		{
			resultMessage = "\"" + array[3] + "\" is not a valid condition value.";
			return false;
		}
		if (array.Length > 4 && !int.TryParse(array[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out result2))
		{
			resultMessage = "\"" + array[4] + "\" is not a valid amount value.";
			return false;
		}
		if (float.IsNaN(position.x) || float.IsInfinity(position.x) || float.IsNaN(position.y) || float.IsInfinity(position.y) || Mathf.Abs(position.x) > 1000000f || Mathf.Abs(position.y) > 1000000f)
		{
			resultMessage = "Spawn position is outside the supported world range.";
			return false;
		}
		if (float.IsNaN(result) || float.IsInfinity(result))
		{
			resultMessage = "Spawn condition must be a finite number.";
			return false;
		}
		result = Mathf.Clamp01(result);
		result2 = Mathf.Clamp(result2, 1, 50);
		int num = 0;
		Item val2 = default(Item);
		AmmoScript val3 = default(AmmoScript);
		for (int i = 0; i < result2; i++)
		{
			GameObject val = Utils.Create(text, position, 0f);
			if ((Object)(object)val == (Object)null)
			{
				resultMessage = "Could not find entity \"" + text + "\".";
				return false;
			}
			RegisterAndSyncGameObjectHierarchy(val);
			if (val.TryGetComponent<Item>(ref val2))
			{
				val2.SetCondition(result);
				if (val.TryGetComponent<AmmoScript>(ref val3) && (int)val3.itemType == 1)
				{
					val3.rounds = Mathf.RoundToInt((float)val3.maxRounds * result);
				}
				if (Vector2.Distance(Vector2.op_Implicit(val.transform.position), Vector2.op_Implicit(((Component)requesterBody).transform.position)) < 2f)
				{
					requesterBody.AutoPickUpItem(val2);
				}
			}
			num++;
		}
		resultMessage = string.Format(CultureInfo.InvariantCulture, "Created \"{0}\" {1}at {2}.", text, (num > 1) ? $"(x{num}) " : string.Empty, position);
		return true;
	}

	private static bool TryParseSpawnPosition(string raw, out Vector2 position, out string errorMessage)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		position = Vector2.zero;
		errorMessage = null;
		string[] array = raw.Split(new char[1] { ',' });
		if (array.Length != 2 || !float.TryParse(array[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var result) || !float.TryParse(array[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var result2))
		{
			errorMessage = "\"" + raw + "\" is not a valid resolved spawn position.";
			return false;
		}
		position = new Vector2(result, result2);
		return true;
	}

	private static bool SendNamedStringToClients(string messageName, string payload)
	{
		EnsureResolved();
		if (serverAllClientIdsExceptHostProperty == null)
		{
			return false;
		}
		object value = serverAllClientIdsExceptHostProperty.GetValue(null, null);
		return SendNamedStringToTargets(messageName, payload, value);
	}

	private static bool SendNamedStringToClient(string messageName, string payload, uint clientId)
	{
		EnsureResolved();
		object playerFromClientId = GetPlayerFromClientId(clientId);
		if (playerFromClientId == null || serverSendToPlayerMethod == null)
		{
			return false;
		}
		try
		{
			object obj = CreateNamedUtf8Writer(messageName, payload);
			if (obj == null)
			{
				return false;
			}
			object obj2 = Enum.Parse(deliveryMethodType, "ReliableOrdered");
			serverSendToPlayerMethod.Invoke(null, new object[3] { obj2, obj, playerFromClientId });
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static bool SendNamedStringToServer(string messageName, string payload)
	{
		EnsureResolved();
		if (clientSendMethod == null)
		{
			return false;
		}
		try
		{
			object obj = CreateNamedUtf8Writer(messageName, payload);
			if (obj == null)
			{
				return false;
			}
			object obj2 = Enum.Parse(deliveryMethodType, "ReliableOrdered");
			clientSendMethod.Invoke(null, new object[2] { obj2, obj });
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static void RegisterAndSyncGameObject(GameObject gameObject)
	{
		EnsureResolved();
		if ((Object)(object)gameObject == (Object)null || netObjectRegistryType == null)
		{
			return;
		}
		try
		{
			if (IsServer)
			{
				FindStaticMethod(netObjectRegistryType, "Server_EnsureItemIsNetworkRegistered", new Type[1] { typeof(GameObject) })?.Invoke(null, new object[1] { gameObject });
			}
			else
			{
				FindStaticMethod(netObjectRegistryType, "SafeNewGO", new Type[1] { typeof(GameObject) })?.Invoke(null, new object[1] { gameObject });
			}
		}
		catch
		{
		}
	}

	private static void RegisterAndSyncGameObjectHierarchy(GameObject root)
	{
		if ((Object)(object)root == (Object)null)
		{
			return;
		}
		RegisterAndSyncGameObject(root);
		Item[] componentsInChildren = root.GetComponentsInChildren<Item>(true);
		foreach (Item val in componentsInChildren)
		{
			if ((Object)(object)val != (Object)null)
			{
				RegisterAndSyncGameObject(((Component)val).gameObject);
			}
		}
	}

	private static bool SendNamedStringToTargets(string messageName, string payload, object targets)
	{
		EnsureResolved();
		if (serverSendToClientsMethod == null || targets == null)
		{
			return false;
		}
		try
		{
			object obj = CreateNamedUtf8Writer(messageName, payload);
			if (obj == null)
			{
				return false;
			}
			object obj2 = Enum.Parse(deliveryMethodType, "ReliableOrdered");
			serverSendToClientsMethod.Invoke(null, new object[3] { obj2, obj, targets });
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static bool ReadStaticBool(Type type, string name)
	{
		EnsureResolved();
		if (type == null)
		{
			return false;
		}
		try
		{
			PropertyInfo property = type.GetProperty(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			if (property != null && property.GetValue(null, null) is bool result)
			{
				return result;
			}
			FieldInfo field = type.GetField(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			bool flag = default(bool);
			int num;
			if (field != null)
			{
				object value = field.GetValue(null);
				if (value is bool)
				{
					flag = (bool)value;
					num = 1;
				}
				else
				{
					num = 0;
				}
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

	private static object CreateNamedUtf8Writer(string messageName, string payload)
	{
		if (createNamedWriterMethod == null || writerPutBytesMethod == null || string.IsNullOrWhiteSpace(messageName))
		{
			return null;
		}
		payload = payload ?? string.Empty;
		byte[] bytes = Encoding.UTF8.GetBytes(payload);
		if (bytes.Length > 1048576)
		{
			ManualLogSource logger = Logger;
			if (logger != null)
			{
				logger.LogWarning((object)("[Together] Refused oversized '" + messageName + "' payload."));
			}
			return null;
		}
		object obj = createNamedWriterMethod.Invoke(null, new object[1] { messageName });
		writerPutBytesMethod.Invoke(obj, new object[1] { bytes });
		return obj;
	}

	private static object GetPlayerFromClientId(uint clientId)
	{
		if (!(playerClientIdToPlayerDictField?.GetValue(null) is IDictionary dictionary))
		{
			return null;
		}
		object obj = CoerceClientIdObject(clientId);
		return (obj != null && dictionary.Contains(obj)) ? dictionary[obj] : null;
	}

	private static object ReadStaticMemberValue(Type type, string name)
	{
		EnsureResolved();
		if (type == null)
		{
			return null;
		}
		try
		{
			PropertyInfo property = type.GetProperty(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			if (property != null)
			{
				return property.GetValue(null, null);
			}
			return type.GetField(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(null);
		}
		catch
		{
			return null;
		}
	}

	private static object ReadInstanceMemberValue(object instance, string name)
	{
		if (instance == null || string.IsNullOrEmpty(name))
		{
			return null;
		}
		try
		{
			Type type = instance.GetType();
			PropertyInfo property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (property != null)
			{
				return property.GetValue(instance, null);
			}
			return type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(instance);
		}
		catch
		{
			return null;
		}
	}

	private static bool TryConvertToUInt64(object value, out ulong result)
	{
		result = 0uL;
		if (value == null)
		{
			return false;
		}
		try
		{
			if (!(value is ulong num))
			{
				if (!(value is long num2))
				{
					if (value is uint num3)
					{
						result = num3;
						return true;
					}
					if (value is int num4 && num4 >= 0)
					{
						result = (ulong)num4;
						return true;
					}
				}
				else if (num2 >= 0)
				{
					result = (ulong)num2;
					return true;
				}
				FieldInfo field = value.GetType().GetField("m_SteamID", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				if (field != null)
				{
					object value2 = field.GetValue(value);
					if (value2 is ulong num5)
					{
						result = num5;
						return true;
					}
					if (value2 is long num6 && num6 >= 0)
					{
						result = (ulong)num6;
						return true;
					}
				}
				result = Convert.ToUInt64(value, CultureInfo.InvariantCulture);
				return result != 0;
			}
			result = num;
			return true;
		}
		catch
		{
			result = 0uL;
			return false;
		}
	}

	private static object CoerceClientIdForMethod(MethodInfo method, uint clientId)
	{
		ParameterInfo[] array = method?.GetParameters();
		Type targetType = ((array != null && array.Length != 0) ? array[0].ParameterType : null);
		return CoerceClientIdObject(clientId, targetType);
	}

	private static object CoerceClientIdObject(uint clientId, Type targetType = null)
	{
		EnsureResolved();
		Type type = targetType ?? netClientIdType ?? typeof(uint);
		try
		{
			if (type == typeof(uint))
			{
				return clientId;
			}
			checked
			{
				if (type == typeof(ushort))
				{
					return (ushort)clientId;
				}
				if (type == typeof(int))
				{
					return (int)clientId;
				}
			}
			if (type == typeof(long))
			{
				return (long)clientId;
			}
			if (type == typeof(ulong))
			{
				return (ulong)clientId;
			}
			if (type.IsEnum)
			{
				return Enum.ToObject(type, clientId);
			}
			ConstructorInfo constructor = type.GetConstructor(new Type[1] { typeof(ushort) });
			if (constructor != null)
			{
				return constructor.Invoke(new object[1] { checked((ushort)clientId) });
			}
			FieldInfo field = type.GetField("id", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (field != null && type.IsValueType)
			{
				object obj = Activator.CreateInstance(type);
				object value = Convert.ChangeType(clientId, field.FieldType, CultureInfo.InvariantCulture);
				field.SetValue(obj, value);
				return obj;
			}
			return Convert.ChangeType(clientId, type, CultureInfo.InvariantCulture);
		}
		catch
		{
			return null;
		}
	}

	private static bool InvokeStaticBool(Type type, string name)
	{
		EnsureResolved();
		if (type == null)
		{
			return false;
		}
		try
		{
			MethodInfo method = type.GetMethod(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
			bool flag = default(bool);
			int num;
			if (method != null)
			{
				object obj = method.Invoke(null, null);
				if (obj is bool)
				{
					flag = (bool)obj;
					num = 1;
				}
				else
				{
					num = 0;
				}
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

	private static object InvokeStatic(Type type, string name, params object[] args)
	{
		if (type == null)
		{
			return null;
		}
		return type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).FirstOrDefault((MethodInfo candidate) => candidate.Name == name && ParametersMatch(candidate, args.Select((object arg) => arg?.GetType()).ToArray()))?.Invoke(null, args);
	}

	private static object InvokeInstance(object instance, string name, params object[] args)
	{
		return instance?.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).FirstOrDefault((MethodInfo candidate) => candidate.Name == name && ParametersMatch(candidate, args.Select((object arg) => arg?.GetType()).ToArray()))?.Invoke(instance, args);
	}

	private static MethodInfo FindStaticMethod(Type type, string name, Type[] signature)
	{
		if (type == null)
		{
			return null;
		}
		return type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).FirstOrDefault((MethodInfo method) => method.Name == name && ParametersMatch(method, signature));
	}

	private static bool ParametersMatch(MethodBase method, Type[] signature)
	{
		ParameterInfo[] parameters = method.GetParameters();
		if (parameters.Length != signature.Length)
		{
			return false;
		}
		for (int i = 0; i < parameters.Length; i++)
		{
			Type type = parameters[i].ParameterType;
			if (type.IsByRef)
			{
				type = type.GetElementType();
			}
			Type type2 = signature[i];
			if (!(type2 == null) && !type.IsAssignableFrom(type2))
			{
				return false;
			}
		}
		return true;
	}

	private static bool TryGetSyncInfo(Item item, out object syncInfo)
	{
		syncInfo = null;
		if (itemSyncType == null || (Object)(object)item == (Object)null)
		{
			return false;
		}
		try
		{
			object[] array = new object[2] { item, null };
			MethodInfo methodInfo = FindStaticMethod(itemSyncType, "TryGetSyncInfo", new Type[2]
			{
				typeof(Item),
				null
			});
			if (methodInfo == null)
			{
				return false;
			}
			object obj = methodInfo.Invoke(null, array);
			bool flag = default(bool);
			int num;
			if (obj is bool)
			{
				flag = (bool)obj;
				num = 1;
			}
			else
			{
				num = 0;
			}
			bool result = (byte)((uint)num & (flag ? 1u : 0u)) != 0;
			syncInfo = array[1];
			return result;
		}
		catch
		{
			return false;
		}
	}

	private static void InvokeTryGetSyncInfoOrRegister(Item item)
	{
		if (netObjectRegistryType == null || (Object)(object)item == (Object)null)
		{
			return;
		}
		try
		{
			object[] parameters = new object[2] { item, null };
			FindStaticMethod(netObjectRegistryType, "TryGetSyncInfoOrRegister", new Type[2]
			{
				typeof(Component),
				null
			})?.Invoke(null, parameters);
		}
		catch
		{
		}
	}

	private static bool ObjectCanBeIgnoredForNetwork(GameObject obj)
	{
		if (netObjectRegistryType == null || (Object)(object)obj == (Object)null)
		{
			return false;
		}
		try
		{
			MethodInfo methodInfo = FindStaticMethod(netObjectRegistryType, "ObjectShouldNotBeSynced", new Type[1] { typeof(GameObject) });
			bool flag = default(bool);
			int num;
			if (methodInfo != null)
			{
				object obj2 = methodInfo.Invoke(null, new object[1] { obj });
				if (obj2 is bool)
				{
					flag = (bool)obj2;
					num = 1;
				}
				else
				{
					num = 0;
				}
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

	private static void AddItemContainerChanged(object syncInfo)
	{
		if (itemContainerChangedField == null || syncInfo == null)
		{
			return;
		}
		try
		{
			object value = itemContainerChangedField.GetValue(null);
			(value?.GetType().GetMethod("Add", BindingFlags.Instance | BindingFlags.Public))?.Invoke(value, new object[1] { syncInfo });
		}
		catch
		{
		}
	}

	private static bool TryGetSyncId(object syncInfo, out uint syncId)
	{
		syncId = 0u;
		if (syncInfo == null)
		{
			return false;
		}
		try
		{
			object obj = syncInfo.GetType().GetField("syncId", BindingFlags.Instance | BindingFlags.Public)?.GetValue(syncInfo);
			if (obj != null)
			{
				syncId = ConvertSenderIdToUInt(obj);
				return syncId <= 65535;
			}
			obj = syncInfo.GetType().GetProperty("syncId", BindingFlags.Instance | BindingFlags.Public)?.GetValue(syncInfo, null);
			if (obj != null)
			{
				syncId = ConvertSenderIdToUInt(obj);
				return syncId <= 65535;
			}
		}
		catch
		{
		}
		return false;
	}
}
