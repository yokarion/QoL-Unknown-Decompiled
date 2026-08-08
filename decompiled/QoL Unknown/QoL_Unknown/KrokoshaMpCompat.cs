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

internal static class KrokoshaMpCompat
{
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

	private sealed class NullScope : IDisposable
	{
		public static readonly NullScope Instance = new NullScope();

		public void Dispose()
		{
		}
	}

	private sealed class KrokoshaCraftSuppressionScope : IDisposable
	{
		private readonly bool previous;

		private bool disposed;

		public KrokoshaCraftSuppressionScope()
		{
			previous = suppressKrokoshaTryCraftPrefix;
			suppressKrokoshaTryCraftPrefix = true;
			suppressKrokoshaTryCraftPrefixThroughFrame = Mathf.Max(suppressKrokoshaTryCraftPrefixThroughFrame, Time.frameCount);
		}

		public void Dispose()
		{
			if (!disposed)
			{
				suppressKrokoshaTryCraftPrefix = previous;
				disposed = true;
			}
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

	internal const string PluginGuid = "KrokoshaCasualtiesMP";

	private const ushort QoLSeedSyncMessageId = 30990;

	private const ushort QoLSeedRequestMessageId = 30991;

	private const ushort QoLBulkCraftMessageId = 30992;

	private const ushort QoLClientInventoryRestoreRequestMessageId = 30993;

	private const ushort QoLClientInventoryRestoreResultMessageId = 30994;

	private const ushort QoLClientSpawnRequestMessageId = 30995;

	private const ushort QoLClientSpawnResultMessageId = 30996;

	private const ushort QoLClientHealRequestMessageId = 30997;

	private const ushort QoLClientHealResultMessageId = 30998;

	private const ushort QoLClientPrepareReconnectForHostLoadMessageId = 30999;

	private static ManualLogSource Logger;

	private static Harmony harmony;

	private static Type krokoshaScavType;

	private static Type mpPluginType;

	private static Type itemSyncType;

	private static Type netObjectRegistryType;

	private static Type inventoryActionPatchType;

	private static Type saveSystemPatchType;

	private static Type saveGamePatchType;

	private static Type soundCannonTrackerType;

	private static Type playerCameraTryCraftPatchType;

	private static Type bodyCombineItemsPatchType;

	private static Type netPlayerType;

	private static Type netBodyType;

	private static Type mpRulesType;

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

	private static Type networkDelegateType;

	private static FieldInfo inventoryActionFlagField;

	private static FieldInfo saveDataPathReplacementField;

	private static FieldInfo saveGameForceField;

	private static FieldInfo soundCannonTrackerChargingField;

	private static FieldInfo vanillaSoundCannonChargingField;

	private static FieldInfo containerStatusChangedField;

	private static FieldInfo mpLastRecipeItemsField;

	private static MethodInfo mpRecipeTryMakeMethod;

	private static MethodInfo mpRecipeGetItemsForRecipeMethod;

	private static MethodInfo mpTryCraftPrefixMethod;

	private static MethodInfo mpLiquidCombineAnnounceMethod;

	private static MethodInfo netPlayerLookupMethod;

	private static MethodInfo netPlayerLookupNetBodyMethod;

	private static MethodInfo netServerKickMethod;

	private static MethodInfo serverSendRelayMessageToClientsMethod;

	private static MethodInfo forceDisconnectMethod;

	private static MethodInfo uiMainMenuSetOpenMethod;

	private static MethodInfo transportLiteConnectMethod;

	private static MethodInfo transportSteamJoinLobbyMethod;

	private static MethodInfo multiplayerStatusLogMethod;

	private static MethodInfo serverAnnounceGameStartMethod;

	private static MethodInfo krokoshaOnSceneLoadedMethod;

	private static MethodInfo serverFinishedWorldgenReceiverMethod;

	private static FieldInfo netPlayerLocalPlayerField;

	private static FieldInfo netPlayerBodyToPlayerDictField;

	private static FieldInfo netPlayerClientIdToPlayerDictField;

	private static FieldInfo mpRulesField;

	private static FieldInfo firstWorldgenParamsField;

	private static FieldInfo showMultiplayerMenuField;

	private static FieldInfo uiCurrentTabField;

	private static FieldInfo uiDirectConnectTabField;

	private static FieldInfo serverLastPlayerStatesField;

	private static MethodInfo lastBeforeGenerationStateApplyMethod;

	private static MethodInfo netPlayerTeleportMethod;

	private static PropertyInfo serverAllClientIdsExceptHostProperty;

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

	private static bool bulkCraftRegistered;

	private static bool clientInventoryRestoreRequestRegistered;

	private static bool clientInventoryRestoreResultRegistered;

	private static bool clientSpawnRequestRegistered;

	private static bool clientSpawnResultRegistered;

	private static bool clientHealRequestRegistered;

	private static bool clientHealResultRegistered;

	private static bool clientPrepareReconnectForHostLoadRegistered;

	private static bool runtimeHooksInstalled;

	private static bool seedAnnouncementHooksPatched;

	private static bool pingSuppressionHooksPatched;

	private static bool craftSuppressionHooksPatched;

	private static bool craftTryMakeGuardPatched;

	private static bool hostSaveLoadHooksPatched;

	private static bool suppressKrokoshaTryCraftPrefix;

	private static int suppressKrokoshaTryCraftPrefixThroughFrame = -1;

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

	public static bool IsPluginPresent
	{
		get
		{
			EnsureResolved();
			return pluginPresent;
		}
	}

	public static bool IsActive => IsNetworkRunning;

	public static bool ShouldUseKrokoshaWorldgenAuthority => IsNetworkRunning && IsServer;

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
		harmony = new Harmony("org.bepinex.plugins.qol.krokosha_mp_compat");
		EnsureResolved();
		if (pluginPresent)
		{
			EnsureRuntimeHooksInstalled();
		}
	}

	public static void AnnounceCurrentSeedToClients()
	{
		if (IsNetworkRunning && IsServer && SeedManager.IsSeeded)
		{
			string payload = BuildSeedPayload(SeedManager.CurrentSeed, SeedManager.InputString);
			SendOrderedStringToClients(30990, payload);
		}
	}

	public static void RequestSeedResend()
	{
		if (IsNetworkRunning && IsClient && !(Time.realtimeSinceStartup - lastSeedRequestTime < 2f))
		{
			lastSeedRequestTime = Time.realtimeSinceStartup;
			SendSimpleMessageToServer(30991);
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
			InvokeStatic(itemSyncType, "Client_GetReadyToSyncInventoryState", body);
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
			if (IsServer)
			{
				InvokeStatic(netObjectRegistryType, "ForceAddFastSync", syncInfo);
			}
			if (containerChanged)
			{
				AddContainerStatusChanged(syncInfo);
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

	public static bool SendSimpleMessageToServer(ushort msgId, bool reliable = true)
	{
		return InvokeSendSimple(new Type[2]
		{
			typeof(ushort),
			typeof(bool)
		}, msgId, reliable);
	}

	public static bool SendSimpleUintToServer(ushort msgId, uint data, bool reliable = true)
	{
		return InvokeSendSimple(new Type[3]
		{
			typeof(ushort),
			typeof(uint),
			typeof(bool)
		}, msgId, data, reliable);
	}

	public static bool SendSimpleUintUintToServer(ushort msgId, uint data, uint data2, bool reliable = true)
	{
		return InvokeSendSimple(new Type[4]
		{
			typeof(ushort),
			typeof(uint),
			typeof(uint),
			typeof(bool)
		}, msgId, data, data2, reliable);
	}

	public static bool SendSimpleUintByteToServer(ushort msgId, uint data, byte data2, bool reliable = true)
	{
		return InvokeSendSimple(new Type[4]
		{
			typeof(ushort),
			typeof(uint),
			typeof(byte),
			typeof(bool)
		}, msgId, data, data2, reliable);
	}

	public static bool SendBulkCraftRequest(Recipe recipe, int amount)
	{
		if (!IsNetworkRunning || !IsClient || recipe == null)
		{
			return false;
		}
		amount = Mathf.Clamp(amount, 1, 255);
		return SendSimpleUintByteToServer(30992, (uint)recipe.index, (byte)amount);
	}

	public static bool SendClientInventoryRestoreRequest(string payload)
	{
		if (!IsNetworkRunning || !IsClient || string.IsNullOrWhiteSpace(payload))
		{
			return false;
		}
		return SendOrderedStringToServer(30993, payload);
	}

	public static bool SendClientSpawnRequest(string commandString)
	{
		if (!IsNetworkRunning || !IsClient || string.IsNullOrWhiteSpace(commandString))
		{
			return false;
		}
		EnsureRuntimeHooksInstalled();
		return SendOrderedStringToServer(30995, commandString);
	}

	public static bool SendClientHealRequest()
	{
		if (!IsNetworkRunning || !IsClient)
		{
			return false;
		}
		EnsureRuntimeHooksInstalled();
		return SendOrderedStringToServer(30997, "1");
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
			obj = netPlayerLocalPlayerField?.GetValue(null);
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
		if (krokoshaScavType == null)
		{
			return null;
		}
		try
		{
			string text2 = krokoshaScavType.GetField("INPUT_USERNAME", BindingFlags.Static | BindingFlags.Public)?.GetValue(null) as string;
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
				logger.LogWarning((object)("Failed to capture Krokosha worldgen state: " + ex.Message));
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
		if (netPlayerBodyToPlayerDictField == null || netPlayerTeleportMethod == null)
		{
			return;
		}
		try
		{
			if (!(netPlayerBodyToPlayerDictField.GetValue(null) is IDictionary dictionary))
			{
				return;
			}
			Vector2 val = Vector2.op_Implicit(((Component)PlayerCamera.main.body).transform.position);
			foreach (DictionaryEntry item in dictionary)
			{
				object value = item.Value;
				if (value != null && !IsLocalNetPlayer(value))
				{
					netPlayerTeleportMethod.Invoke(value, new object[1] { val });
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

	public static bool TryExecuteBulkCraftViaKrokosha(PlayerCamera camera, Recipe finalRecipe, CraftingPlan plan)
	{
		Body val = camera?.body;
		if (!IsActive || (Object)(object)val == (Object)null || finalRecipe == null || plan == null || !plan.CanCraft)
		{
			return false;
		}
		EnsureResolved();
		if (mpRecipeTryMakeMethod == null || mpRecipeGetItemsForRecipeMethod == null)
		{
			return false;
		}
		if (!TryExecuteCombineOperations(val, plan))
		{
			return false;
		}
		foreach (CraftingPlanOperation intermediateOperation in plan.IntermediateOperations)
		{
			for (int i = 0; i < intermediateOperation.Count; i++)
			{
				if (!TryExecuteMpRecipeAndAnnounce(camera, val, intermediateOperation.Recipe))
				{
					return false;
				}
			}
		}
		for (int j = 0; j < plan.RequestedAmount; j++)
		{
			if (!TryExecuteMpRecipeAndAnnounce(camera, val, finalRecipe))
			{
				return false;
			}
		}
		return true;
	}

	public static bool CanAnnounceKrokoshaCraft(Body crafter, Recipe recipe)
	{
		if (!IsActive || (Object)(object)crafter == (Object)null || recipe == null)
		{
			return false;
		}
		EnsureResolved();
		if (mpRecipeGetItemsForRecipeMethod == null)
		{
			return false;
		}
		try
		{
			IEnumerable items = mpRecipeGetItemsForRecipeMethod.Invoke(null, new object[2] { crafter, recipe }) as IEnumerable;
			return CraftItemsHaveSyncIds(items);
		}
		catch
		{
			return false;
		}
	}

	public static bool AreItemsSynced(params Item[] items)
	{
		if (!IsActive)
		{
			return true;
		}
		if (items == null || items.Length == 0)
		{
			return false;
		}
		EnsureResolved();
		foreach (Item val in items)
		{
			if ((Object)(object)val == (Object)null || !TryGetSyncInfo(val, out var syncInfo) || !TryGetSyncId(syncInfo, out var _))
			{
				return false;
			}
		}
		return true;
	}

	public static bool CanDirectCraftViaKrokosha(Body crafter, Recipe recipe)
	{
		if (!IsActive || (Object)(object)crafter == (Object)null || recipe == null)
		{
			return false;
		}
		EnsureResolved();
		if (mpRecipeGetItemsForRecipeMethod == null)
		{
			return false;
		}
		try
		{
			return mpRecipeGetItemsForRecipeMethod.Invoke(null, new object[2] { crafter, recipe }) is IEnumerable source && source.Cast<object>().Any();
		}
		catch
		{
			return false;
		}
	}

	public static bool TryExecuteSingleCraftViaKrokosha(PlayerCamera camera, Recipe recipe)
	{
		Body val = camera?.body;
		if (!IsActive || (Object)(object)val == (Object)null || recipe == null)
		{
			return false;
		}
		EnsureResolved();
		if (mpRecipeTryMakeMethod == null || mpRecipeGetItemsForRecipeMethod == null)
		{
			return false;
		}
		if (IsClient)
		{
			return TryExecuteClientCraftViaKrokoshaPrefix(camera, recipe);
		}
		return TryExecuteMpRecipeAndAnnounce(camera, val, recipe);
	}

	public static IDisposable SuppressKrokoshaTryCraftPrefix()
	{
		if (!IsActive)
		{
			return NullScope.Instance;
		}
		EnsureRuntimeHooksInstalled();
		return new KrokoshaCraftSuppressionScope();
	}

	public static bool TryAnnounceLiquidTransfer(Body body, Item targetItem, Item sourceItem, float ml)
	{
		if (!IsActive || (Object)(object)body == (Object)null || (Object)(object)targetItem == (Object)null || (Object)(object)sourceItem == (Object)null || ml <= 0f)
		{
			return true;
		}
		EnsureResolved();
		if (mpLiquidCombineAnnounceMethod == null)
		{
			return false;
		}
		try
		{
			object obj = mpLiquidCombineAnnounceMethod.Invoke(null, new object[5] { true, ml, body, targetItem, sourceItem });
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
		catch
		{
			return false;
		}
	}

	public static bool TryUseItemViaKrokosha(Body body, Item item)
	{
		if ((Object)(object)body == (Object)null || (Object)(object)item == (Object)null || !item.Stats.usable)
		{
			return false;
		}
		try
		{
			body.UseItem(item);
			return true;
		}
		catch
		{
			return false;
		}
	}

	public static bool TryExecuteBulkCraftOnServer(Body crafter, Recipe finalRecipe, int amount)
	{
		if ((Object)(object)crafter == (Object)null || finalRecipe == null)
		{
			return false;
		}
		EnsureResolved();
		if (mpRecipeTryMakeMethod == null || mpRecipeGetItemsForRecipeMethod == null)
		{
			return false;
		}
		CraftingPlan craftingPlan = CraftingPlanService.BuildExecutionPlan(finalRecipe, amount, crafter);
		if (!craftingPlan.CanCraft)
		{
			return false;
		}
		if (!TryExecuteCombineOperations(crafter, craftingPlan))
		{
			return false;
		}
		foreach (CraftingPlanOperation intermediateOperation in craftingPlan.IntermediateOperations)
		{
			for (int i = 0; i < intermediateOperation.Count; i++)
			{
				if (!TryExecuteMpRecipe(crafter, intermediateOperation.Recipe))
				{
					return false;
				}
			}
		}
		for (int j = 0; j < craftingPlan.RequestedAmount; j++)
		{
			if (!TryExecuteMpRecipe(crafter, finalRecipe))
			{
				return false;
			}
		}
		return true;
	}

	private static bool TryExecuteCombineOperations(Body crafter, CraftingPlan plan)
	{
		if ((Object)(object)crafter == (Object)null || plan == null)
		{
			return false;
		}
		foreach (CraftingCombineOperation combineOperation in plan.CombineOperations)
		{
			if ((Object)(object)combineOperation?.Target == (Object)null || (Object)(object)combineOperation.Source == (Object)null || !crafter.CanCombine(combineOperation.Target, combineOperation.Source))
			{
				return false;
			}
			crafter.CombineItems(combineOperation.Target, combineOperation.Source);
		}
		return true;
	}

	private static bool InvokeSendSimple(Type[] signature, params object[] args)
	{
		EnsureResolved();
		MethodInfo methodInfo = FindStaticMethod(krokoshaScavType, "Client_SendSimpleMessageToServer", signature);
		if (methodInfo == null)
		{
			return false;
		}
		try
		{
			methodInfo.Invoke(null, args);
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static void EnsureResolved()
	{
		if (resolutionAttempted && !pluginPresent)
		{
			return;
		}
		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
		if ((resolutionAttempted && !pluginPresent && assemblies.Length == resolvedAssemblyCount) || (resolutionAttempted && assemblies.Length == resolvedAssemblyCount))
		{
			return;
		}
		resolutionAttempted = true;
		resolvedAssemblyCount = assemblies.Length;
		krokoshaScavType = FindType(assemblies, "KrokoshaCasualtiesMP.KrokoshaScavMultiplayer");
		mpPluginType = FindType(assemblies, "KrokoshaCasualtiesMP.Plugin");
		itemSyncType = FindType(assemblies, "KrokoshaCasualtiesMP.ItemSync");
		netObjectRegistryType = FindType(assemblies, "KrokoshaCasualtiesMP.NetObjectRegistry");
		inventoryActionPatchType = FindType(assemblies, "KrokoshaCasualtiesMP.PlayerCamera_TryPerformInventoryAction_MultiplayerPatch");
		saveSystemPatchType = FindType(assemblies, "KrokoshaCasualtiesMP.SavesystemPatch");
		saveGamePatchType = FindType(assemblies, "KrokoshaCasualtiesMP.SaveSystem_SaveGame_MultiplayerPatch");
		soundCannonTrackerType = FindType(assemblies, "KrokoshaCasualtiesMP.KrokoshaSoundCannonNetworkTrackerComponent");
		playerCameraTryCraftPatchType = FindType(assemblies, "KrokoshaCasualtiesMP.PlayerCamera_TryCraft_MultiplayerPatch");
		bodyCombineItemsPatchType = FindType(assemblies, "KrokoshaCasualtiesMP.Body_CombineItems_MultiplayerPatch");
		netPlayerType = FindType(assemblies, "KrokoshaCasualtiesMP.NetPlayer");
		netBodyType = FindType(assemblies, "KrokoshaCasualtiesMP.NetBody");
		mpRulesType = FindType(assemblies, "KrokoshaCasualtiesMP.KrokoshaMultiplayerGameRules");
		clientMainType = FindType(assemblies, "KrokoshaCasualtiesMP.ClientMain");
		serverMainType = FindType(assemblies, "KrokoshaCasualtiesMP.ServerMain");
		netType = FindType(assemblies, "KrokoshaCasualtiesMP.Net");
		netNetTypeType = FindType(assemblies, "KrokoshaCasualtiesMP.Net+NetType");
		uiMainMenuType = FindType(assemblies, "KrokoshaCasualtiesMP.UIMainMenu");
		transportLiteNetLibType = FindType(assemblies, "KrokoshaCasualtiesMP.TransportLiteNetLib");
		transportSteamworksType = FindType(assemblies, "KrokoshaCasualtiesMP.TransportSteamworks");
		kSteamType = FindType(assemblies, "KrokoshaCasualtiesMP.KSteam");
		worldgenPatchType = FindType(assemblies, "KrokoshaCasualtiesMP.WorldGeneration_GenerateWorld_MultiplayerPatch");
		worldgenPatchesType = FindType(assemblies, "KrokoshaCasualtiesMP.WorldgenPatches");
		lastBeforeGenerationStateType = FindType(assemblies, "KrokoshaCasualtiesMP.LastBeforeGenerationState");
		netExtensionsType = FindType(assemblies, "KrokoshaCasualtiesMP.MyLiteNetLibExtensions");
		netClientIdType = FindType(assemblies, "KrokoshaCasualtiesMP.knetid");
		networkDelegateType = krokoshaScavType?.GetNestedType("KrokoshaHandleNamedMessageDelegate", BindingFlags.Public | BindingFlags.NonPublic);
		netDataReaderType = FindType(assemblies, "LiteNetLib.Utils.NetDataReader");
		netDataWriterType = FindType(assemblies, "LiteNetLib.Utils.NetDataWriter");
		deliveryMethodType = FindType(assemblies, "LiteNetLib.DeliveryMethod");
		InferNetworkApiTypes();
		inventoryActionFlagField = inventoryActionPatchType?.GetField("is_inside_TryPerformInventoryAction", BindingFlags.Static | BindingFlags.Public);
		saveDataPathReplacementField = saveSystemPatchType?.GetField("savedatapathreplacement", BindingFlags.Static | BindingFlags.Public);
		saveGameForceField = saveGamePatchType?.GetField("force", BindingFlags.Static | BindingFlags.NonPublic);
		soundCannonTrackerChargingField = soundCannonTrackerType?.GetField("charging", BindingFlags.Instance | BindingFlags.Public);
		vanillaSoundCannonChargingField = typeof(SoundCannon).GetField("charging", BindingFlags.Instance | BindingFlags.NonPublic);
		containerStatusChangedField = itemSyncType?.GetField("ContainerStatusChanged", BindingFlags.Static | BindingFlags.Public);
		mpLastRecipeItemsField = playerCameraTryCraftPatchType?.GetField("last_recipe_items", BindingFlags.Static | BindingFlags.Public);
		mpRecipeTryMakeMethod = playerCameraTryCraftPatchType?.GetMethod("Recipe_TryMake", BindingFlags.Static | BindingFlags.Public);
		mpRecipeGetItemsForRecipeMethod = playerCameraTryCraftPatchType?.GetMethod("Recipe_GetItemsForRecipe", BindingFlags.Static | BindingFlags.Public);
		mpTryCraftPrefixMethod = playerCameraTryCraftPatchType?.GetMethod("Prefix", BindingFlags.Static | BindingFlags.NonPublic);
		mpLiquidCombineAnnounceMethod = bodyCombineItemsPatchType?.GetMethod("DoTheCombineAnnouncement", BindingFlags.Static | BindingFlags.Public);
		netPlayerLookupMethod = netPlayerType?.GetMethod("TryGetNetPlayerAndBodyFromClientId", BindingFlags.Static | BindingFlags.Public);
		netPlayerLookupNetBodyMethod = netPlayerType?.GetMethod("TryGetNetPlayerAndNetBodyFromClientId", BindingFlags.Static | BindingFlags.Public);
		netServerKickMethod = netType?.GetMethod("Server_Kick", BindingFlags.Static | BindingFlags.Public);
		serverSendRelayMessageToClientsMethod = krokoshaScavType?.GetMethods(BindingFlags.Static | BindingFlags.Public).FirstOrDefault(delegate(MethodInfo method)
		{
			if (method.Name != "Server_SendRelayMessageToClients")
			{
				return false;
			}
			ParameterInfo[] parameters = method.GetParameters();
			return parameters.Length == 4 && UnwrapByRef(parameters[0].ParameterType) == typeof(ushort) && parameters[1].ParameterType == typeof(ushort) && parameters[2].ParameterType == typeof(bool) && parameters[3].ParameterType == typeof(bool);
		});
		forceDisconnectMethod = krokoshaScavType?.GetMethod("_JustDisconnect", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		uiMainMenuSetOpenMethod = uiMainMenuType?.GetMethod("SetOpen", BindingFlags.Static | BindingFlags.Public);
		transportLiteConnectMethod = transportLiteNetLibType?.GetMethod("OnWantToConnect", BindingFlags.Static | BindingFlags.Public);
		transportSteamJoinLobbyMethod = transportSteamworksType?.GetMethod("OnWantToJoinLobby", BindingFlags.Static | BindingFlags.Public);
		multiplayerStatusLogMethod = krokoshaScavType?.GetMethod("DoMultiplayerStatusMessageLog", BindingFlags.Static | BindingFlags.Public);
		serverAnnounceGameStartMethod = serverMainType?.GetMethod("Server_Announce_GAME_START", BindingFlags.Static | BindingFlags.Public);
		krokoshaOnSceneLoadedMethod = krokoshaScavType?.GetMethod("OnSceneLoaded", BindingFlags.Static | BindingFlags.Public);
		serverFinishedWorldgenReceiverMethod = worldgenPatchesType?.GetMethod("ServerReceiver_FinishedWorldgen", BindingFlags.Static | BindingFlags.NonPublic);
		netPlayerLocalPlayerField = netPlayerType?.GetField("LOCAL_PLAYER", BindingFlags.Static | BindingFlags.Public);
		netPlayerBodyToPlayerDictField = netPlayerType?.GetField("BodyToPlayerDict", BindingFlags.Static | BindingFlags.Public);
		netPlayerClientIdToPlayerDictField = netPlayerType?.GetField("ClientIdToPlayerDict", BindingFlags.Static | BindingFlags.Public);
		netPlayerTeleportMethod = netPlayerType?.GetMethod("Server_TeleportCharacter", BindingFlags.Instance | BindingFlags.Public);
		showMultiplayerMenuField = krokoshaScavType?.GetField("showMultiplayerMenu", BindingFlags.Static | BindingFlags.Public);
		uiCurrentTabField = uiMainMenuType?.GetField("current_tab", BindingFlags.Static | BindingFlags.Public);
		uiDirectConnectTabField = uiMainMenuType?.GetField("menutab_directconnect", BindingFlags.Static | BindingFlags.Public);
		serverLastPlayerStatesField = serverMainType?.GetField("server_lastplayerstates", BindingFlags.Static | BindingFlags.Public);
		serverAllClientIdsExceptHostProperty = serverMainType?.GetProperty("AllClientIdsExceptHost", BindingFlags.Static | BindingFlags.Public);
		mpRulesField = krokoshaScavType?.GetField("rules", BindingFlags.Static | BindingFlags.Public);
		firstWorldgenParamsField = worldgenPatchType?.GetField("firstworldgenparams", BindingFlags.Static | BindingFlags.Public);
		lastBeforeGenerationStateApplyMethod = lastBeforeGenerationStateType?.GetMethod("Apply", BindingFlags.Instance | BindingFlags.Public);
		pluginPresent = krokoshaScavType != null || mpPluginType != null;
		networkStateFrame = -1;
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
			return 0;
		}
		if (!SendClientsBackToMainMenuForHostLoad())
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
			IDictionary dictionary2 = netPlayerClientIdToPlayerDictField?.GetValue(null) as IDictionary;
			object obj = netPlayerLocalPlayerField?.GetValue(null);
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
					if (!string.IsNullOrWhiteSpace(text) && KrokoshaMpSaveBundle.TryReadLiveMpPlayerInventorySnapshot(text, out var payload))
					{
						dictionary[text] = payload;
					}
				}
			}
			pendingClientInventoryRestorePayloadsByPersistentId = ((dictionary.Count > 0) ? dictionary : null);
		}
		catch (Exception)
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
		catch (Exception)
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
		catch (Exception)
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

	private static bool SendRelayMessageToClients(ushort messageId)
	{
		EnsureResolved();
		if (serverSendRelayMessageToClientsMethod == null)
		{
			return false;
		}
		try
		{
			serverSendRelayMessageToClientsMethod.Invoke(null, new object[4]
			{
				messageId,
				(ushort)0,
				true,
				false
			});
			return true;
		}
		catch (Exception)
		{
			return false;
		}
	}

	private static bool SendClientsBackToMainMenuForHostLoad()
	{
		EnsureResolved();
		if (serverSendRelayMessageToClientsMethod == null)
		{
			return false;
		}
		try
		{
			serverSendRelayMessageToClientsMethod.Invoke(null, new object[4]
			{
				(ushort)10011,
				(ushort)0,
				true,
				false
			});
			return true;
		}
		catch (Exception)
		{
			return false;
		}
	}

	private static bool SendOrderedEmptyMessageToClients(ushort msgId)
	{
		EnsureResolved();
		if (serverMainType == null || netType == null || netDataWriterType == null || deliveryMethodType == null)
		{
			return false;
		}
		try
		{
			object targets = serverAllClientIdsExceptHostProperty?.GetValue(null, null);
			if (targets == null)
			{
				return false;
			}
			MethodInfo methodInfo = FindStaticMethod(netType, "CreateWriter", new Type[1] { typeof(ushort) });
			MethodInfo methodInfo2 = netType.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).FirstOrDefault(delegate(MethodInfo method)
			{
				if (method.Name != "Server_SendToClients")
				{
					return false;
				}
				ParameterInfo[] parameters = method.GetParameters();
				return parameters.Length == 3 && parameters[0].ParameterType.IsAssignableFrom(deliveryMethodType) && parameters[1].ParameterType.IsAssignableFrom(netDataWriterType) && parameters[2].ParameterType.IsInstanceOfType(targets);
			});
			if (methodInfo == null || methodInfo2 == null)
			{
				return false;
			}
			object obj = methodInfo.Invoke(null, new object[1] { msgId });
			object obj2 = Enum.Parse(deliveryMethodType, "ReliableOrdered");
			methodInfo2.Invoke(null, new object[3] { obj2, obj, targets });
			return true;
		}
		catch (Exception)
		{
			return false;
		}
	}

	private static void ResetRemotePlayerBodiesForConnectedHostSaveLoad()
	{
		EnsureResolved();
		if (netPlayerBodyToPlayerDictField == null || netPlayerClientIdToPlayerDictField == null || netPlayerLocalPlayerField == null)
		{
			return;
		}
		try
		{
			IDictionary dictionary = netPlayerBodyToPlayerDictField.GetValue(null) as IDictionary;
			IDictionary dictionary2 = netPlayerClientIdToPlayerDictField.GetValue(null) as IDictionary;
			object value = netPlayerLocalPlayerField.GetValue(null);
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
					catch (Exception)
					{
					}
				}
			}
		}
		catch (Exception)
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
			cachedNetworkRunning = ReadStaticBool(krokoshaScavType, "network_system_is_running");
			if (!cachedNetworkRunning)
			{
				cachedIsClient = false;
				cachedIsServer = false;
				cachedWorldGenerated = false;
				nextInactiveNetworkRefreshTime = Time.unscaledTime + 0.5f;
			}
			else
			{
				cachedIsClient = cachedNetworkRunning && ReadStaticBool(krokoshaScavType, "is_client");
				cachedIsServer = cachedNetworkRunning && ReadStaticBool(krokoshaScavType, "is_server");
				cachedWorldGenerated = cachedNetworkRunning && InvokeStaticBool(krokoshaScavType, "IsNetworkActiveAndIsWorldGenerated");
				nextInactiveNetworkRefreshTime = 0f;
				EnsureRuntimeHooksInstalled();
			}
		}
	}

	private static void EnsureRuntimeHooksInstalled()
	{
		if (pluginPresent && !runtimeHooksInstalled)
		{
			RegisterSeedSyncReceiver();
			RegisterSeedRequestReceiver();
			RegisterBulkCraftReceiver();
			RegisterClientInventoryRestoreRequestReceiver();
			RegisterClientInventoryRestoreResultReceiver();
			RegisterClientPrepareReconnectForHostLoadReceiver();
			PatchSeedAnnouncementHooks();
			PatchHostSaveLoadHooks();
			PatchPingSuppressionHooks();
			PatchCraftSuppressionHooks();
			PatchCraftTryMakeGuard();
			runtimeHooksInstalled = true;
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
		if (netDataReaderType == null && networkDelegateType != null)
		{
			ParameterInfo[] array = networkDelegateType.GetMethod("Invoke")?.GetParameters();
			if (array != null && array.Length == 2)
			{
				netDataReaderType = UnwrapByRef(array[1].ParameterType);
			}
		}
		if (netDataWriterType == null && netType != null)
		{
			netDataWriterType = FindStaticMethod(netType, "CreateWriter", new Type[1] { typeof(ushort) })?.ReturnType;
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

	private static MethodInfo FindClientRegisterReceiverMethod(bool includeReliableFlag)
	{
		EnsureResolved();
		if (networkDelegateType == null)
		{
			return null;
		}
		Type[] signature = ((!includeReliableFlag) ? new Type[2]
		{
			typeof(ushort),
			networkDelegateType
		} : new Type[3]
		{
			typeof(ushort),
			networkDelegateType,
			typeof(bool)
		});
		return FindStaticMethod(netType, "RegisterClientReceiver", signature) ?? FindStaticMethod(netType, "RegisterClientReciever", signature) ?? FindStaticMethod(clientMainType, "RegisterClientReceiver", signature) ?? FindStaticMethod(clientMainType, "RegisterClientReciever", signature);
	}

	private static MethodInfo FindServerRegisterReceiverMethod()
	{
		EnsureResolved();
		if (networkDelegateType == null)
		{
			return null;
		}
		Type[] signature = new Type[2]
		{
			typeof(ushort),
			networkDelegateType
		};
		return FindStaticMethod(netType, "RegisterServerReceiver", signature) ?? FindStaticMethod(netType, "RegisterServerReciever", signature) ?? FindStaticMethod(serverMainType, "RegisterServerReceiver", signature) ?? FindStaticMethod(serverMainType, "RegisterServerReciever", signature);
	}

	private static void RegisterSeedSyncReceiver()
	{
		if (seedSyncRegistered)
		{
			return;
		}
		EnsureResolved();
		if (clientMainType == null || networkDelegateType == null || netDataReaderType == null)
		{
			return;
		}
		MethodInfo methodInfo = FindClientRegisterReceiverMethod(includeReliableFlag: true);
		if (methodInfo == null)
		{
			return;
		}
		try
		{
			object obj = CreateSeedSyncReceiverDelegate();
			methodInfo.Invoke(null, new object[3]
			{
				(ushort)30990,
				obj,
				true
			});
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
		if (serverMainType == null || networkDelegateType == null || netDataReaderType == null)
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
			methodInfo.Invoke(null, new object[2]
			{
				(ushort)30991,
				obj
			});
			seedRequestRegistered = true;
		}
		catch
		{
		}
	}

	private static void RegisterBulkCraftReceiver()
	{
		if (bulkCraftRegistered)
		{
			return;
		}
		EnsureResolved();
		if (serverMainType == null || networkDelegateType == null || netDataReaderType == null)
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
			object obj = CreateBulkCraftReceiverDelegate();
			methodInfo.Invoke(null, new object[2]
			{
				(ushort)30992,
				obj
			});
			bulkCraftRegistered = true;
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
		if (serverMainType == null || networkDelegateType == null || netDataReaderType == null)
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
			methodInfo.Invoke(null, new object[2]
			{
				(ushort)30993,
				obj
			});
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
		if (clientMainType == null || networkDelegateType == null || netDataReaderType == null)
		{
			return;
		}
		MethodInfo methodInfo = FindClientRegisterReceiverMethod(includeReliableFlag: true);
		if (methodInfo == null)
		{
			return;
		}
		try
		{
			object obj = CreateClientInventoryRestoreResultReceiverDelegate();
			methodInfo.Invoke(null, new object[3]
			{
				(ushort)30994,
				obj,
				false
			});
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
		if (serverMainType == null || networkDelegateType == null || netDataReaderType == null)
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
				methodInfo.Invoke(null, new object[2]
				{
					(ushort)30995,
					obj
				});
				clientSpawnRequestRegistered = true;
			}
		}
		catch (Exception)
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
		if (clientMainType == null || networkDelegateType == null || netDataReaderType == null)
		{
			return;
		}
		MethodInfo methodInfo = FindClientRegisterReceiverMethod(includeReliableFlag: true);
		if (methodInfo == null)
		{
			return;
		}
		try
		{
			object obj = CreateClientSpawnResultReceiverDelegate();
			methodInfo.Invoke(null, new object[3]
			{
				(ushort)30996,
				obj,
				false
			});
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
		if (serverMainType == null || networkDelegateType == null)
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
				methodInfo.Invoke(null, new object[2]
				{
					(ushort)30997,
					obj
				});
				clientHealRequestRegistered = true;
			}
		}
		catch (Exception)
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
		if (clientMainType == null || networkDelegateType == null || netDataReaderType == null)
		{
			return;
		}
		MethodInfo methodInfo = FindClientRegisterReceiverMethod(includeReliableFlag: true);
		if (methodInfo == null)
		{
			return;
		}
		try
		{
			object obj = CreateClientHealResultReceiverDelegate();
			methodInfo.Invoke(null, new object[3]
			{
				(ushort)30998,
				obj,
				false
			});
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
		if (clientMainType == null || networkDelegateType == null)
		{
			return;
		}
		MethodInfo methodInfo = FindClientRegisterReceiverMethod(includeReliableFlag: true);
		if (methodInfo == null)
		{
			return;
		}
		try
		{
			object obj = CreateClientPrepareReconnectForHostLoadReceiverDelegate();
			methodInfo.Invoke(null, new object[3]
			{
				(ushort)30999,
				obj,
				true
			});
			clientPrepareReconnectForHostLoadRegistered = true;
		}
		catch
		{
		}
	}

	private static object CreateSeedSyncReceiverDelegate()
	{
		return CreateReceiverDelegateWithReader("QoL_KrokoshaSeedSyncReceiver", "HandleSeedSyncMessage");
	}

	private static object CreateSeedRequestReceiverDelegate()
	{
		return CreateReceiverDelegateWithoutReader("QoL_KrokoshaSeedRequestReceiver", "HandleSeedRequestMessage");
	}

	private static object CreateBulkCraftReceiverDelegate()
	{
		return CreateReceiverDelegateWithReader("QoL_KrokoshaBulkCraftReceiver", "HandleBulkCraftMessage");
	}

	private static object CreateClientInventoryRestoreRequestReceiverDelegate()
	{
		return CreateReceiverDelegateWithReader("QoL_KrokoshaClientInventoryRestoreRequestReceiver", "HandleClientInventoryRestoreRequestMessage");
	}

	private static object CreateClientInventoryRestoreResultReceiverDelegate()
	{
		return CreateReceiverDelegateWithReader("QoL_KrokoshaClientInventoryRestoreResultReceiver", "HandleClientInventoryRestoreResultMessage");
	}

	private static object CreateClientSpawnRequestReceiverDelegate()
	{
		return CreateReceiverDelegateWithReader("QoL_KrokoshaClientSpawnRequestReceiver", "HandleClientSpawnRequestMessage");
	}

	private static object CreateClientSpawnResultReceiverDelegate()
	{
		return CreateReceiverDelegateWithReader("QoL_KrokoshaClientSpawnResultReceiver", "HandleClientSpawnResultMessage");
	}

	private static object CreateClientHealRequestReceiverDelegate()
	{
		return CreateReceiverDelegateWithoutReader("QoL_KrokoshaClientHealRequestReceiver", "HandleClientHealRequestMessage");
	}

	private static object CreateClientHealResultReceiverDelegate()
	{
		return CreateReceiverDelegateWithReader("QoL_KrokoshaClientHealResultReceiver", "HandleClientHealResultMessage");
	}

	private static object CreateClientPrepareReconnectForHostLoadReceiverDelegate()
	{
		return CreateReceiverDelegateWithoutReader("QoL_KrokoshaClientPrepareReconnectForHostLoadReceiver", "HandleClientPrepareReconnectForHostLoadMessage");
	}

	private static object CreateReceiverDelegateWithReader(string methodName, string handlerName)
	{
		EnsureResolved();
		MethodInfo methodInfo = networkDelegateType?.GetMethod("Invoke");
		MethodInfo method = typeof(KrokoshaMpCompat).GetMethod(handlerName, BindingFlags.Static | BindingFlags.NonPublic);
		if (methodInfo == null || method == null)
		{
			return null;
		}
		ParameterInfo[] parameters = methodInfo.GetParameters();
		if (parameters.Length < 2)
		{
			return null;
		}
		Type parameterType = parameters[0].ParameterType;
		Type parameterType2 = parameters[1].ParameterType;
		DynamicMethod dynamicMethod = new DynamicMethod(methodName, typeof(void), new Type[2] { parameterType, parameterType2 }, typeof(KrokoshaMpCompat).Module, skipVisibility: true);
		ILGenerator iLGenerator = dynamicMethod.GetILGenerator();
		EmitLoadNormalizedClientId(iLGenerator, parameterType);
		iLGenerator.Emit(OpCodes.Ldarg_1);
		iLGenerator.Emit(OpCodes.Ldind_Ref);
		iLGenerator.Emit(OpCodes.Call, method);
		iLGenerator.Emit(OpCodes.Ret);
		return dynamicMethod.CreateDelegate(networkDelegateType);
	}

	private static object CreateReceiverDelegateWithoutReader(string methodName, string handlerName)
	{
		EnsureResolved();
		MethodInfo methodInfo = networkDelegateType?.GetMethod("Invoke");
		MethodInfo method = typeof(KrokoshaMpCompat).GetMethod(handlerName, BindingFlags.Static | BindingFlags.NonPublic);
		if (methodInfo == null || method == null)
		{
			return null;
		}
		ParameterInfo[] parameters = methodInfo.GetParameters();
		if (parameters.Length < 1)
		{
			return null;
		}
		Type parameterType = parameters[0].ParameterType;
		Type type = ((parameters.Length > 1) ? parameters[1].ParameterType : netDataReaderType.MakeByRefType());
		DynamicMethod dynamicMethod = new DynamicMethod(methodName, typeof(void), new Type[2] { parameterType, type }, typeof(KrokoshaMpCompat).Module, skipVisibility: true);
		ILGenerator iLGenerator = dynamicMethod.GetILGenerator();
		EmitLoadNormalizedClientId(iLGenerator, parameterType);
		iLGenerator.Emit(OpCodes.Call, method);
		iLGenerator.Emit(OpCodes.Ret);
		return dynamicMethod.CreateDelegate(networkDelegateType);
	}

	private static void EmitLoadNormalizedClientId(ILGenerator il, Type clientIdType)
	{
		il.Emit(OpCodes.Ldarg_0);
		if (clientIdType.IsValueType)
		{
			il.Emit(OpCodes.Box, clientIdType);
		}
		il.Emit(OpCodes.Call, typeof(KrokoshaMpCompat).GetMethod("ConvertSenderIdToUInt", BindingFlags.Static | BindingFlags.NonPublic));
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
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Expected O, but got Unknown
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Expected O, but got Unknown
		//IL_0150: Unknown result type (might be due to invalid IL or missing references)
		//IL_015d: Expected O, but got Unknown
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
		MethodInfo method2 = typeof(KrokoshaMpCompat).GetMethod("ServerAnnounceGameStart_Prefix", BindingFlags.Static | BindingFlags.NonPublic);
		if (method == null || method2 == null)
		{
			return;
		}
		try
		{
			harmony.Patch((MethodBase)method, new HarmonyMethod(method2), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
			MethodInfo method3 = serverMainType.GetMethod("Server_AnnounceSeed", BindingFlags.Static | BindingFlags.Public);
			MethodInfo method4 = typeof(KrokoshaMpCompat).GetMethod("ServerAnnounceSeed_Prefix", BindingFlags.Static | BindingFlags.NonPublic);
			if (method3 != null && method4 != null)
			{
				harmony.Patch((MethodBase)method3, new HarmonyMethod(method4), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
			}
			MethodInfo methodInfo = clientMainType?.GetMethod("ClientReciever__GenerateWorld_SeedAnnounce", BindingFlags.Static | BindingFlags.NonPublic);
			MethodInfo method5 = typeof(KrokoshaMpCompat).GetMethod("ClientGenerateWorldSeedAnnounce_Postfix", BindingFlags.Static | BindingFlags.NonPublic);
			if (methodInfo != null && method5 != null)
			{
				harmony.Patch((MethodBase)methodInfo, (HarmonyMethod)null, new HarmonyMethod(method5), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
			}
			seedAnnouncementHooksPatched = true;
		}
		catch
		{
		}
	}

	private static void PatchPingSuppressionHooks()
	{
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Expected O, but got Unknown
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Expected O, but got Unknown
		if (pingSuppressionHooksPatched || !pluginPresent)
		{
			return;
		}
		EnsureResolved();
		MethodInfo methodInfo = netPlayerType?.GetMethod("PointFingerAt", BindingFlags.Instance | BindingFlags.Public);
		MethodInfo methodInfo2 = FindStaticMethod(netType, "Client_Send", new Type[2] { deliveryMethodType, netDataWriterType });
		MethodInfo method = typeof(KrokoshaMpCompat).GetMethod("PointFingerAtSuppressionPrefix", BindingFlags.Static | BindingFlags.NonPublic);
		MethodInfo method2 = typeof(KrokoshaMpCompat).GetMethod("ClientSendAfterPointFingerPrefix", BindingFlags.Static | BindingFlags.NonPublic);
		try
		{
			bool flag = false;
			if (methodInfo != null && method != null)
			{
				harmony.Patch((MethodBase)methodInfo, new HarmonyMethod(method), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
				flag = true;
			}
			if (methodInfo2 != null && method2 != null)
			{
				harmony.Patch((MethodBase)methodInfo2, new HarmonyMethod(method2), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
				flag = true;
			}
			pingSuppressionHooksPatched = flag;
		}
		catch
		{
		}
	}

	private static void PatchCraftSuppressionHooks()
	{
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Expected O, but got Unknown
		if (craftSuppressionHooksPatched || !pluginPresent)
		{
			return;
		}
		EnsureResolved();
		MethodInfo methodInfo = playerCameraTryCraftPatchType?.GetMethod("Prefix", BindingFlags.Static | BindingFlags.NonPublic);
		MethodInfo method = typeof(KrokoshaMpCompat).GetMethod("KrokoshaTryCraftPrefixGuard", BindingFlags.Static | BindingFlags.NonPublic);
		if (methodInfo == null || method == null)
		{
			return;
		}
		try
		{
			harmony.Patch((MethodBase)methodInfo, new HarmonyMethod(method), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
			craftSuppressionHooksPatched = true;
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
		if (krokoshaOnSceneLoadedMethod == null || serverFinishedWorldgenReceiverMethod == null)
		{
			return;
		}
		MethodInfo method = typeof(KrokoshaMpCompat).GetMethod("KrokoshaOnSceneLoaded_Prefix", BindingFlags.Static | BindingFlags.NonPublic);
		MethodInfo method2 = typeof(KrokoshaMpCompat).GetMethod("KrokoshaOnSceneLoaded_Postfix", BindingFlags.Static | BindingFlags.NonPublic);
		MethodInfo method3 = typeof(KrokoshaMpCompat).GetMethod("ServerReceiverFinishedWorldgen_Postfix", BindingFlags.Static | BindingFlags.NonPublic);
		if (method == null || method2 == null || method3 == null)
		{
			return;
		}
		try
		{
			harmony.Patch((MethodBase)krokoshaOnSceneLoadedMethod, new HarmonyMethod(method), new HarmonyMethod(method2), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
			harmony.Patch((MethodBase)serverFinishedWorldgenReceiverMethod, (HarmonyMethod)null, new HarmonyMethod(method3), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
			hostSaveLoadHooksPatched = true;
		}
		catch
		{
		}
	}

	private static void PatchCraftTryMakeGuard()
	{
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Expected O, but got Unknown
		if (craftTryMakeGuardPatched || !pluginPresent || mpRecipeTryMakeMethod == null)
		{
			return;
		}
		MethodInfo method = typeof(KrokoshaMpCompat).GetMethod("MpRecipeTryMakeGuardPrefix", BindingFlags.Static | BindingFlags.NonPublic);
		if (method == null)
		{
			return;
		}
		try
		{
			harmony.Patch((MethodBase)mpRecipeTryMakeMethod, new HarmonyMethod(method), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
			craftTryMakeGuardPatched = true;
		}
		catch
		{
		}
	}

	private static bool KrokoshaTryCraftPrefixGuard()
	{
		return !suppressKrokoshaTryCraftPrefix && Time.frameCount > suppressKrokoshaTryCraftPrefixThroughFrame;
	}

	private static bool MpRecipeTryMakeGuardPrefix(Body crafter, Recipe craftingtable, ref List<Item> items_to_craft_with, ref bool __result)
	{
		if ((Object)(object)crafter == (Object)null || craftingtable == null)
		{
			__result = false;
			return false;
		}
		if (items_to_craft_with == null)
		{
			try
			{
				items_to_craft_with = mpRecipeGetItemsForRecipeMethod?.Invoke(null, new object[2] { crafter, craftingtable }) as List<Item>;
			}
			catch
			{
				items_to_craft_with = null;
			}
		}
		if (items_to_craft_with == null || items_to_craft_with.Any((Item item) => (Object)(object)item == (Object)null))
		{
			__result = false;
			return false;
		}
		return true;
	}

	private static bool PointFingerAtSuppressionPrefix(object __instance)
	{
		if (!IsLocalNetPlayer(__instance))
		{
			return true;
		}
		if (QuickStashPatcher.TryHandleQuickStashBeforeKrokoshaPing())
		{
			suppressNextPointFingerClientSend = true;
			return false;
		}
		if (!QuickStashPatcher.ShouldSuppressKrokoshaPointPingNow())
		{
			return true;
		}
		suppressNextPointFingerClientSend = true;
		return false;
	}

	private static bool IsLocalNetPlayer(object player)
	{
		if (player == null || netPlayerLocalPlayerField == null)
		{
			return false;
		}
		try
		{
			return player == netPlayerLocalPlayerField.GetValue(null);
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

	private static void KrokoshaOnSceneLoaded_Prefix()
	{
		if (preserveServerPlayerStatesForHostSaveLoad)
		{
			CaptureServerLastPlayerStatesForHostSaveLoad();
		}
	}

	private static void KrokoshaOnSceneLoaded_Postfix()
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

	private static void ServerReceiverFinishedWorldgen_Postfix(object clientId)
	{
		if (!IsNetworkRunning || !IsServer || pendingClientInventoryRestorePayloadsByPersistentId == null || pendingClientInventoryRestorePayloadsByPersistentId.Count == 0)
		{
			return;
		}
		try
		{
			uint clientId2 = Convert.ToUInt32(clientId, CultureInfo.InvariantCulture);
			Body bodyFromClientId = GetBodyFromClientId(clientId2);
			if ((Object)(object)bodyFromClientId == (Object)null)
			{
				return;
			}
			object[] array = new object[3]
			{
				CoerceClientIdForMethod(netPlayerLookupMethod, clientId2),
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
					((MonoBehaviour)main).StartCoroutine(ApplyPostWorldgenClientInventoryRestoreCoroutine(clientId2, text, value));
				}
			}
		}
		catch (Exception)
		{
		}
	}

	private static IEnumerator ApplyPostWorldgenClientInventoryRestoreCoroutine(uint clientId, string persistentId, string payload)
	{
		yield return (object)new WaitForSeconds(0.5f);
		Body body = GetBodyFromClientId(clientId);
		if (!((Object)(object)body == (Object)null) && pendingClientInventoryRestorePayloadsByPersistentId.TryGetValue(persistentId, out var currentPayload) && string.Equals(currentPayload, payload, StringComparison.Ordinal) && KrokoshaMpSaveBundle.TryApplyClientInventoryPayload(body, payload, out var _))
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
			ApplyQoLSeedBeforeKrokoshaHostWorldgenCapture();
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

	private static bool ApplyQoLSeedBeforeKrokoshaHostWorldgenCapture()
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
		catch (Exception)
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
		catch (Exception)
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
		catch (Exception)
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
		catch (Exception)
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
			MethodInfo methodInfo = FindKrokoshaMemcpyMethod("Put", netDataWriterType, lastBeforeGenerationStateType);
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
			MethodInfo methodInfo = FindKrokoshaMemcpyMethod("Get", netDataReaderType, lastBeforeGenerationStateType.MakeByRefType());
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

	private static MethodInfo FindKrokoshaMemcpyMethod(string name, Type firstParameter, Type secondParameter)
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
			SendOrderedStringToTargets(30990, payload, targets);
		}
	}

	private static void HandleSeedRequestMessage(uint senderClientId)
	{
		try
		{
			if (IsNetworkRunning && IsServer && SeedManager.IsSeeded)
			{
				string payload = BuildSeedPayload(SeedManager.CurrentSeed, SeedManager.InputString);
				SendOrderedStringToClient(30990, payload, senderClientId);
			}
		}
		catch
		{
		}
	}

	private static void HandleBulkCraftMessage(uint senderClientId, object reader)
	{
		try
		{
			if (!IsNetworkRunning || !IsServer || reader == null)
			{
				return;
			}
			uint num = ReadUInt(reader);
			byte b = ReadByte(reader);
			if (Recipes.recipes == null || num >= Recipes.recipes.Count || b == 0)
			{
				return;
			}
			Body bodyFromClientId = GetBodyFromClientId(senderClientId);
			if (!((Object)(object)bodyFromClientId == (Object)null) && bodyFromClientId.conscious)
			{
				Recipe finalRecipe = Recipes.recipes[(int)num];
				if (TryExecuteBulkCraftOnServer(bodyFromClientId, finalRecipe, b))
				{
					AnnounceCraftSound(bodyFromClientId, senderClientId);
				}
			}
		}
		catch (Exception)
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
			string text = ReadOneByteString(reader);
			if (string.IsNullOrWhiteSpace(text))
			{
				SendClientInventoryRestoreResult(senderClientId, ok: false, "Inventory snapshot was empty.");
				return;
			}
			Body bodyFromClientId = GetBodyFromClientId(senderClientId);
			if ((Object)(object)bodyFromClientId == (Object)null)
			{
				SendClientInventoryRestoreResult(senderClientId, ok: false, "Host could not find your player body.");
				return;
			}
			if (!KrokoshaMpSaveBundle.TryApplyClientInventoryPayload(bodyFromClientId, text, out var message))
			{
				SendClientInventoryRestoreResult(senderClientId, ok: false, message ?? "Inventory restore failed.");
				return;
			}
			SyncBodyInventory(bodyFromClientId);
			SendClientInventoryRestoreResult(senderClientId, ok: true, "Restored inventory from QoL slot.");
		}
		catch (Exception)
		{
			SendClientInventoryRestoreResult(senderClientId, ok: false, "Inventory restore failed on host.");
		}
	}

	private static void HandleClientInventoryRestoreResultMessage(uint senderClientId, object reader)
	{
		try
		{
			string text = ReadOneByteString(reader);
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
			string text = ReadOneByteString(reader);
			string resultMessage;
			if (string.IsNullOrWhiteSpace(text))
			{
				SendClientSpawnResult(senderClientId, ok: false, "Spawn command was empty.");
			}
			else if (!text.TrimStart(Array.Empty<char>()).StartsWith("spawn ", StringComparison.OrdinalIgnoreCase))
			{
				SendClientSpawnResult(senderClientId, ok: false, "Only spawn commands can be forwarded.");
			}
			else if (!TryExecuteClientSpawnCommand(text, out resultMessage))
			{
				SendClientSpawnResult(senderClientId, ok: false, string.IsNullOrWhiteSpace(resultMessage) ? "Spawn request failed on host." : resultMessage);
			}
			else
			{
				SendClientSpawnResult(senderClientId, ok: true, resultMessage);
			}
		}
		catch (Exception)
		{
			SendClientSpawnResult(senderClientId, ok: false, "Spawn request failed on host.");
		}
	}

	private static void HandleClientSpawnResultMessage(uint senderClientId, object reader)
	{
		try
		{
			string text = ReadOneByteString(reader);
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
			string playerNameFromClientId = GetPlayerNameFromClientId(senderClientId);
			if (string.IsNullOrWhiteSpace(playerNameFromClientId))
			{
				SendClientHealResult(senderClientId, ok: false, "Host could not find your player name.");
				return;
			}
			Body bodyFromClientId = GetBodyFromClientId(senderClientId);
			if ((Object)(object)bodyFromClientId == (Object)null)
			{
				SendClientHealResult(senderClientId, ok: false, "Host could not find your body.");
				return;
			}
			ApplyHealToBody(bodyFromClientId);
			SendClientHealResult(senderClientId, ok: true, "Healed player on host.");
		}
		catch (Exception)
		{
			SendClientHealResult(senderClientId, ok: false, "Heal request failed on host.");
		}
	}

	private static void HandleClientHealResultMessage(uint senderClientId, object reader)
	{
		try
		{
			string text = ReadOneByteString(reader);
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

	private static void HandleClientPrepareReconnectForHostLoadMessage(uint senderClientId)
	{
		if (IsNetworkRunning && IsClient && TryCaptureCurrentReconnectTarget(out var useSteam, out var ipPort, out var lobbyId))
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
		useSteam = ReadStaticBool(netType, "is_playing_with_steam");
		if (useSteam)
		{
			object value = ReadStaticMemberValue(kSteamType, "lobbyId");
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
		catch (Exception)
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
		catch (Exception)
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
		catch (Exception)
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
			string payload = ReadOneByteString(reader);
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

	private static string ReadOneByteString(object reader)
	{
		EnsureResolved();
		if (reader == null || netExtensionsType == null || netDataReaderType == null)
		{
			return null;
		}
		MethodInfo methodInfo = FindStaticMethod(netExtensionsType, "Get", new Type[3]
		{
			netDataReaderType,
			typeof(string),
			typeof(bool)
		});
		if (methodInfo == null)
		{
			return null;
		}
		object[] array = new object[3] { reader, null, true };
		methodInfo.Invoke(null, array);
		return array[1] as string;
	}

	private static uint ReadUInt(object reader)
	{
		if (reader == null)
		{
			return 0u;
		}
		object[] array = new object[1] { 0u };
		reader.GetType().GetMethod("Get", new Type[1] { typeof(uint).MakeByRefType() })?.Invoke(reader, array);
		return (array[0] is uint num) ? num : 0u;
	}

	private static byte ReadByte(object reader)
	{
		if (reader == null)
		{
			return 0;
		}
		object[] array = new object[1] { (byte)0 };
		reader.GetType().GetMethod("Get", new Type[1] { typeof(byte).MakeByRefType() })?.Invoke(reader, array);
		return (byte)((array[0] is byte b) ? b : 0);
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
		if (mpRulesField == null)
		{
			return false;
		}
		try
		{
			object value = mpRulesField.GetValue(null);
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

	private static void SendClientInventoryRestoreResult(uint clientId, bool ok, string message)
	{
		string text = Convert.ToBase64String(Encoding.UTF8.GetBytes(message ?? ""));
		SendOrderedStringToClient(30994, (ok ? "1" : "0") + "|" + text, clientId);
	}

	private static void SendClientSpawnResult(uint clientId, bool ok, string message)
	{
		string text = Convert.ToBase64String(Encoding.UTF8.GetBytes(message ?? ""));
		SendOrderedStringToClient(30996, (ok ? "1" : "0") + "|" + text, clientId);
	}

	private static void SendClientHealResult(uint clientId, bool ok, string message)
	{
		string text = Convert.ToBase64String(Encoding.UTF8.GetBytes(message ?? ""));
		SendOrderedStringToClient(30998, (ok ? "1" : "0") + "|" + text, clientId);
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

	private static string GetPlayerNameFromClientId(uint clientId)
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
			if (((uint)num & (flag ? 1u : 0u)) == 0 || array[1] == null)
			{
				return null;
			}
			return array[1].GetType().GetField("playername", BindingFlags.Instance | BindingFlags.Public)?.GetValue(array[1]) as string;
		}
		catch
		{
			return null;
		}
	}

	private static void ApplyHealToBody(Body body)
	{
		if ((Object)(object)body == (Object)null)
		{
			return;
		}
		Limb[] limbs = body.limbs;
		if (limbs != null)
		{
			foreach (Limb val in limbs)
			{
				if (!((Object)(object)val == (Object)null))
				{
					val.muscleHealth = 100f;
					val.skinHealth = 100f;
					val.boneHealTimer = 0f;
					val.dislocationTimer = 0f;
					val.infectionAmount = 0f;
					val.bleedAmount = 0f;
					val.pain = 0f;
					val.shrapnel = 0;
					val.infected = false;
					val.dismembered = false;
					if (!((Component)val).gameObject.activeSelf)
					{
						((Component)val).gameObject.SetActive(true);
					}
				}
			}
		}
		body.brainHealth = 100f;
		SetBodyFloatCompat(body, 100f, "bloodAmount", "bloodVolume");
		SetBodyFloatCompat(body, 100f, "oxygenAmount", "bloodOxygen");
		body.hunger = 100f;
		body.thirst = 100f;
		body.septicShock = 0f;
		body.temperature = 37f;
		body.sicknessAmount = 0f;
		body.consciousness = 100f;
		body.stamina = 100f;
		body.energy = 100f;
		body.happiness = 0f;
		body.weightOffset = 0f;
		body.radiationSickness = 0f;
		body.disfigured = false;
		body.eyeGone = false;
		body.internalBleeding = 0f;
		body.hemothorax = 0f;
		body.traumaAmount = 0f;
		body.dirtyness = 0f;
		body.wetness = 0f;
		body.badSleepAmount = 0f;
		body.hearingLoss = 0f;
		body.antidepressantHappiness = 0f;
		body.opiateHappiness = 0f;
		body.antibioticImmunityTime = 0f;
		body.brainGrowSickness = 0f;
		body.triedRollingLastStand = false;
		body.succesfullyRolledLastStand = false;
		body.lastStandTime = -1000f;
		body.adrenaline = 0f;
		body.curAdrenaline = 0f;
		Painkillers val2 = default(Painkillers);
		if (((Component)body).TryGetComponent<Painkillers>(ref val2))
		{
			Object.Destroy((Object)(object)val2);
		}
		SleepingPills val3 = default(SleepingPills);
		if (((Component)body).TryGetComponent<SleepingPills>(ref val3))
		{
			Object.Destroy((Object)(object)val3);
		}
		Antidepressants val4 = default(Antidepressants);
		if (((Component)body).TryGetComponent<Antidepressants>(ref val4))
		{
			Object.Destroy((Object)(object)val4);
		}
		MindwipeScript[] components = ((Component)body).GetComponents<MindwipeScript>();
		for (int j = 0; j < components.Length; j++)
		{
			Object.Destroy((Object)(object)components[j]);
		}
		body.mindWipe = null;
		CoUtils instance = CoUtils.instance;
		if (instance != null)
		{
			instance.Stop("bleach");
		}
		CoUtils instance2 = CoUtils.instance;
		if (instance2 != null)
		{
			instance2.Stop("mercury");
		}
	}

	private static bool TryExecuteClientSpawnCommand(string commandString, out string resultMessage)
	{
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0177: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01df: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e5: Invalid comparison between Unknown and I4
		//IL_0228: Unknown result type (might be due to invalid IL or missing references)
		//IL_022d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0241: Unknown result type (might be due to invalid IL or missing references)
		//IL_0246: Unknown result type (might be due to invalid IL or missing references)
		resultMessage = null;
		if (string.IsNullOrWhiteSpace(commandString))
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
		if (array.Length < 2 || !array[0].Equals("spawn", StringComparison.OrdinalIgnoreCase))
		{
			resultMessage = "Only spawn commands can be forwarded.";
			return false;
		}
		string text = array[1];
		float result = 1f;
		int result2 = 1;
		Vector2 position = (((Object)(object)Camera.main != (Object)null) ? Vector2.op_Implicit(Camera.main.ScreenToWorldPoint(Input.mousePosition)) : Vector2.zero);
		if (array.Length > 2 && !TryParseSpawnPosition(array[2], out position, out resultMessage))
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
		result2 = Mathf.Max(1, result2);
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
				if ((Object)(object)PlayerCamera.main?.body != (Object)null && Vector2.Distance(Vector2.op_Implicit(val.transform.position), Vector2.op_Implicit(((Component)PlayerCamera.main.body).transform.position)) < 2f)
				{
					PlayerCamera.main.body.AutoPickUpItem(val2);
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
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_018c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0191: Unknown result type (might be due to invalid IL or missing references)
		position = Vector2.zero;
		errorMessage = null;
		switch (raw)
		{
		case "cursor":
			if ((Object)(object)Camera.main == (Object)null)
			{
				errorMessage = "Host camera is unavailable for cursor position.";
				return false;
			}
			position = Vector2.op_Implicit(Camera.main.ScreenToWorldPoint(Input.mousePosition));
			return true;
		case "player":
			if ((Object)(object)PlayerCamera.main?.body == (Object)null)
			{
				errorMessage = "Host player body is unavailable for player position.";
				return false;
			}
			position = Vector2.op_Implicit(((Component)PlayerCamera.main.body).transform.position);
			return true;
		case "random":
			position = new Vector2(Random.Range(-1f, 1f) * (float)WorldGeneration.world.halfWidth, Random.Range(-1f, 1f) * (float)WorldGeneration.world.halfHeight);
			return true;
		default:
		{
			string[] array = raw.Split(new char[1] { ',' });
			if (array.Length != 2 || !float.TryParse(array[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var result) || !float.TryParse(array[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var result2))
			{
				errorMessage = "\"" + raw + "\" is not a valid position! (cursor/player/random/#,#)";
				return false;
			}
			position = new Vector2(result, result2);
			return true;
		}
		}
	}

	private static void SetBodyFloatCompat(Body body, float value, params string[] fieldNames)
	{
		if ((Object)(object)body == (Object)null || fieldNames == null)
		{
			return;
		}
		Type type = ((object)body).GetType();
		foreach (string text in fieldNames)
		{
			if (!string.IsNullOrEmpty(text))
			{
				FieldInfo field = type.GetField(text, BindingFlags.Instance | BindingFlags.Public);
				if (field != null && field.FieldType == typeof(float))
				{
					field.SetValue(body, value);
					break;
				}
			}
		}
	}

	private static bool TryExecuteMpRecipe(Body crafter, Recipe recipe)
	{
		if ((Object)(object)crafter == (Object)null || recipe == null || mpRecipeTryMakeMethod == null || mpRecipeGetItemsForRecipeMethod == null)
		{
			return false;
		}
		try
		{
			object obj = mpRecipeGetItemsForRecipeMethod.Invoke(null, new object[2] { crafter, recipe });
			if (obj == null)
			{
				return false;
			}
			object obj2 = mpRecipeTryMakeMethod.Invoke(null, new object[3] { crafter, recipe, obj });
			bool flag = default(bool);
			int num;
			if (obj2 is bool)
			{
				flag = (bool)obj2;
				num = 1;
			}
			else
			{
				num = 0;
			}
			bool flag2 = (byte)((uint)num & (flag ? 1u : 0u)) != 0;
			if (flag2)
			{
				recipe.hasMadeBefore = true;
			}
			return flag2;
		}
		catch
		{
			return false;
		}
	}

	private static bool TryExecuteMpRecipeAndAnnounce(PlayerCamera camera, Body crafter, Recipe recipe)
	{
		if ((Object)(object)camera == (Object)null || (Object)(object)crafter == (Object)null || recipe == null)
		{
			return false;
		}
		int selectedRecipe = camera.selectedRecipe;
		try
		{
			camera.selectedRecipe = recipe.index;
			if (!TryExecuteMpRecipe(crafter, recipe))
			{
				return false;
			}
			if (IsClient && !TrySendKrokoshaCraftAnnouncement(recipe))
			{
				return false;
			}
			if (IsServer)
			{
				AnnounceCraftSound(crafter, 0u);
			}
			return true;
		}
		finally
		{
			camera.selectedRecipe = selectedRecipe;
		}
	}

	private static bool TryExecuteClientCraftViaKrokoshaPrefix(PlayerCamera camera, Recipe recipe)
	{
		if ((Object)(object)camera == (Object)null || recipe == null)
		{
			return false;
		}
		EnsureResolved();
		if (mpTryCraftPrefixMethod == null)
		{
			return false;
		}
		if (!CanDirectCraftViaKrokosha(camera.body, recipe))
		{
			return false;
		}
		int selectedRecipe = camera.selectedRecipe;
		try
		{
			camera.selectedRecipe = recipe.index;
			return !(mpTryCraftPrefixMethod.Invoke(null, new object[1] { camera }) is bool flag) || !flag;
		}
		catch
		{
			return false;
		}
		finally
		{
			camera.selectedRecipe = selectedRecipe;
		}
	}

	private static bool TrySendKrokoshaCraftAnnouncement(Recipe recipe)
	{
		EnsureResolved();
		if (recipe == null || netType == null || netDataWriterType == null || deliveryMethodType == null || mpLastRecipeItemsField == null)
		{
			return false;
		}
		try
		{
			if (!(mpLastRecipeItemsField.GetValue(null) is IEnumerable entries))
			{
				return false;
			}
			MethodInfo methodInfo = FindStaticMethod(netType, "CreateWriter", new Type[1] { typeof(ushort) });
			MethodInfo methodInfo2 = FindStaticMethod(netType, "Client_Send", new Type[2] { deliveryMethodType, netDataWriterType });
			if (methodInfo == null || methodInfo2 == null)
			{
				return false;
			}
			object obj = methodInfo.Invoke(null, new object[1] { (ushort)10053 });
			MethodInfo method = obj.GetType().GetMethod("Put", new Type[1] { typeof(ushort) });
			MethodInfo method2 = obj.GetType().GetMethod("PutArray", new Type[1] { typeof(ushort[]) });
			if (method == null || method2 == null)
			{
				return false;
			}
			ushort[] array = ExtractCraftSyncIds(entries);
			if (array == null)
			{
				return false;
			}
			method.Invoke(obj, new object[1] { (ushort)recipe.index });
			method2.Invoke(obj, new object[1] { array });
			object obj2 = Enum.Parse(deliveryMethodType, "ReliableOrdered");
			methodInfo2.Invoke(null, new object[2] { obj2, obj });
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static ushort[] ExtractCraftSyncIds(IEnumerable entries)
	{
		if (entries == null)
		{
			return null;
		}
		List<ushort> list = new List<ushort>();
		foreach (object entry in entries)
		{
			if (entry == null)
			{
				return null;
			}
			Item val = (Item)((entry is Item) ? entry : null);
			if (val != null)
			{
				if (!TryGetSyncInfo(val, out var syncInfo) || !TryGetSyncId(syncInfo, out var syncId) || syncId > 65535)
				{
					return null;
				}
				list.Add((ushort)syncId);
			}
			else
			{
				if (!TryConvertToUshort(entry, out var result))
				{
					return null;
				}
				list.Add(result);
			}
		}
		return list.ToArray();
	}

	private static bool TryConvertToUshort(object value, out ushort result)
	{
		try
		{
			if (!(value is ushort num))
			{
				if (!(value is byte b))
				{
					if (!(value is sbyte b2))
					{
						if (!(value is short num2))
						{
							if (!(value is uint num3))
							{
								if (!(value is int num4))
								{
									if (value is long num5 && num5 >= 0 && num5 <= 65535)
									{
										result = (ushort)num5;
										return true;
									}
								}
								else if (num4 >= 0 && num4 <= 65535)
								{
									result = (ushort)num4;
									return true;
								}
							}
							else if (num3 <= 65535)
							{
								result = (ushort)num3;
								return true;
							}
						}
						else if (num2 >= 0)
						{
							result = (ushort)num2;
							return true;
						}
					}
					else if (b2 >= 0)
					{
						result = (ushort)b2;
						return true;
					}
					result = Convert.ToUInt16(value, CultureInfo.InvariantCulture);
					return true;
				}
				result = b;
				return true;
			}
			result = num;
			return true;
		}
		catch
		{
			result = 0;
			return false;
		}
	}

	private static bool CraftItemsHaveSyncIds(IEnumerable items)
	{
		if (items == null)
		{
			return false;
		}
		foreach (object item in items)
		{
			Item val = (Item)((item is Item) ? item : null);
			if ((Object)(object)val == (Object)null || !TryGetSyncInfo(val, out var syncInfo) || !TryGetSyncId(syncInfo, out var _))
			{
				return false;
			}
		}
		return true;
	}

	private static void AnnounceCraftSound(Body crafter, uint senderClientId)
	{
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		EnsureResolved();
		if (serverMainType == null || (Object)(object)crafter == (Object)null)
		{
			return;
		}
		try
		{
			MethodInfo method = serverMainType.GetMethod("GetListOfClientIdsExceptThis", BindingFlags.Static | BindingFlags.Public);
			MethodInfo method2 = serverMainType.GetMethod("Server_AnnounceSound", BindingFlags.Static | BindingFlags.Public);
			object obj = method?.Invoke(null, new object[1] { senderClientId });
			method2?.Invoke(null, new object[3]
			{
				((Component)crafter).transform.position,
				"combine",
				obj
			});
		}
		catch
		{
		}
	}

	private static bool SendOrderedStringToClients(ushort msgId, string payload)
	{
		EnsureResolved();
		if (serverMainType == null)
		{
			return false;
		}
		object targets = serverMainType.GetProperty("AllClientIdsExceptHost", BindingFlags.Static | BindingFlags.Public)?.GetValue(null, null);
		return SendOrderedStringToTargets(msgId, payload, targets);
	}

	private static bool SendOrderedStringToClient(ushort msgId, string payload, uint clientId)
	{
		EnsureResolved();
		if (netType == null || netExtensionsType == null || netDataWriterType == null || deliveryMethodType == null)
		{
			return false;
		}
		try
		{
			MethodInfo methodInfo = FindStaticMethod(netType, "CreateWriter", new Type[1] { typeof(ushort) });
			MethodInfo methodInfo2 = FindStaticMethod(netExtensionsType, "Put", new Type[3]
			{
				netDataWriterType,
				typeof(string),
				typeof(bool)
			});
			object coercedClientId = CoerceClientIdObject(clientId);
			MethodInfo methodInfo3 = netType.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).FirstOrDefault(delegate(MethodInfo method)
			{
				if (method.Name != "Server_SendToClients")
				{
					return false;
				}
				ParameterInfo[] parameters = method.GetParameters();
				return parameters.Length == 3 && parameters[0].ParameterType.IsAssignableFrom(deliveryMethodType) && parameters[1].ParameterType.IsAssignableFrom(netDataWriterType) && coercedClientId != null && parameters[2].ParameterType.IsAssignableFrom(coercedClientId.GetType());
			});
			if (methodInfo == null || methodInfo2 == null || methodInfo3 == null || coercedClientId == null)
			{
				return false;
			}
			object obj = methodInfo.Invoke(null, new object[1] { msgId });
			methodInfo2.Invoke(null, new object[3] { obj, payload, true });
			object obj2 = Enum.Parse(deliveryMethodType, "ReliableOrdered");
			methodInfo3.Invoke(null, new object[3] { obj2, obj, coercedClientId });
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static bool SendOrderedStringToServer(ushort msgId, string payload)
	{
		EnsureResolved();
		if (netType == null || netExtensionsType == null || netDataWriterType == null || deliveryMethodType == null)
		{
			return false;
		}
		try
		{
			MethodInfo methodInfo = FindStaticMethod(netType, "CreateWriter", new Type[1] { typeof(ushort) });
			MethodInfo methodInfo2 = FindStaticMethod(netExtensionsType, "Put", new Type[3]
			{
				netDataWriterType,
				typeof(string),
				typeof(bool)
			});
			MethodInfo methodInfo3 = FindStaticMethod(netType, "Client_Send", new Type[2] { deliveryMethodType, netDataWriterType });
			if (methodInfo == null || methodInfo2 == null || methodInfo3 == null)
			{
				return false;
			}
			object obj = methodInfo.Invoke(null, new object[1] { msgId });
			methodInfo2.Invoke(null, new object[3] { obj, payload, true });
			object obj2 = Enum.Parse(deliveryMethodType, "ReliableOrdered");
			methodInfo3.Invoke(null, new object[2] { obj2, obj });
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
			MethodInfo methodInfo = FindStaticMethod(netObjectRegistryType, "TryGetSyncInfoOrRegister", new Type[2]
			{
				typeof(Component),
				null
			});
			object obj = null;
			if (methodInfo != null)
			{
				object obj2 = ((object)gameObject.GetComponent<Item>()) ?? ((object)gameObject.GetComponent<Component>());
				object[] array = new object[2] { obj2, null };
				methodInfo.Invoke(null, array);
				obj = array[1];
			}
			if (obj != null)
			{
				InvokeStatic(netObjectRegistryType, "ForceAddFastSync", obj);
				InvokeStatic(netObjectRegistryType, "Server_ObjectSyncSingle", obj, true);
			}
			else
			{
				FindStaticMethod(netObjectRegistryType, "NewGO", new Type[1] { typeof(GameObject) })?.Invoke(null, new object[1] { gameObject });
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

	private static bool SendOrderedStringToTargets(ushort msgId, string payload, object targets)
	{
		EnsureResolved();
		if (netType == null || serverMainType == null || netExtensionsType == null || netDataWriterType == null || deliveryMethodType == null)
		{
			return false;
		}
		try
		{
			MethodInfo methodInfo = FindStaticMethod(netType, "CreateWriter", new Type[1] { typeof(ushort) });
			MethodInfo methodInfo2 = FindStaticMethod(netExtensionsType, "Put", new Type[3]
			{
				netDataWriterType,
				typeof(string),
				typeof(bool)
			});
			MethodInfo methodInfo3 = netType.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).FirstOrDefault(delegate(MethodInfo method)
			{
				if (method.Name != "Server_SendToClients")
				{
					return false;
				}
				ParameterInfo[] parameters = method.GetParameters();
				return parameters.Length == 3 && parameters[0].ParameterType.IsAssignableFrom(deliveryMethodType) && parameters[1].ParameterType.IsAssignableFrom(netDataWriterType) && targets != null && parameters[2].ParameterType.IsAssignableFrom(targets.GetType());
			});
			if (methodInfo == null || methodInfo2 == null || methodInfo3 == null || targets == null)
			{
				return false;
			}
			object obj = methodInfo.Invoke(null, new object[1] { msgId });
			methodInfo2.Invoke(null, new object[3] { obj, payload, true });
			object obj2 = Enum.Parse(deliveryMethodType, "ReliableOrdered");
			methodInfo3.Invoke(null, new object[3] { obj2, obj, targets });
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
		catch (Exception)
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
			MethodInfo methodInfo = FindStaticMethod(netObjectRegistryType, "ObjectCanBeIgnoredForNetwork", new Type[1] { typeof(GameObject) });
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

	private static void AddContainerStatusChanged(object syncInfo)
	{
		if (containerStatusChangedField == null || syncInfo == null)
		{
			return;
		}
		try
		{
			object value = containerStatusChangedField.GetValue(null);
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
			FieldInfo field = syncInfo.GetType().GetField("syncid", BindingFlags.Instance | BindingFlags.Public);
			if (field != null && field.GetValue(syncInfo) is uint num)
			{
				syncId = num;
				return true;
			}
			PropertyInfo property = syncInfo.GetType().GetProperty("syncid", BindingFlags.Instance | BindingFlags.Public);
			if (property != null && property.GetValue(syncInfo, null) is uint num2)
			{
				syncId = num2;
				return true;
			}
		}
		catch
		{
		}
		return false;
	}
}
