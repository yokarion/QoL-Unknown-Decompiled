using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace QoL_Unknown;

internal static class MultiplayerSaveLoadPatcher
{
	public static bool IsLoadingMultiplayerSlot;

	public static void Initialize()
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Expected O, but got Unknown
		Harmony val = new Harmony("org.bepinex.plugins.qol.multiplayer_save_load");
		val.PatchAll(typeof(MultiplayerSaveLoadPatcher));
	}

	public static void MarkMultiplayerSlotLoad()
	{
		IsLoadingMultiplayerSlot = true;
	}

	public static void ClearMultiplayerSlotLoad()
	{
		IsLoadingMultiplayerSlot = false;
	}

	private static GameObject CreateUnlessLoadingMultiplayerSlot(string id, Vector2 position, float rotation)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		return ShouldSkipEmergencyLight(id) ? null : Utils.Create(id, position, rotation);
	}

	private static bool ShouldSkipEmergencyLight(string id)
	{
		if (id != "emergencylight" || !IsLoadingMultiplayerSlot)
		{
			return false;
		}
		if (KrokoshaMpCompat.IsNetworkRunning && KrokoshaMpCompat.IsClient && !SaveSystem.loadedRun)
		{
			return false;
		}
		return SaveSystem.loadedRun || KrokoshaMpCompat.IsServer;
	}

	private static Item GetItemUnlessNull(GameObject gameObject)
	{
		return ((Object)(object)gameObject == (Object)null) ? null : gameObject.GetComponent<Item>();
	}

	private static void PickUpUnlessNull(Body body, Item item, int slot, bool force)
	{
		if ((Object)(object)item != (Object)null)
		{
			body.PickUpItem(item, slot, force);
		}
	}

	[HarmonyPatch(typeof(WorldGeneration), "WorldPlacePlayer")]
	[HarmonyTranspiler]
	private static IEnumerable<CodeInstruction> WorldPlacePlayer_Transpiler(IEnumerable<CodeInstruction> instructions)
	{
		//IL_019b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a5: Expected O, but got Unknown
		//IL_01c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d2: Expected O, but got Unknown
		//IL_01f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0200: Expected O, but got Unknown
		List<CodeInstruction> list = instructions.ToList();
		MethodInfo methodInfo = AccessTools.Method(typeof(Utils), "Create", new Type[3]
		{
			typeof(string),
			typeof(Vector2),
			typeof(float)
		}, (Type[])null);
		MethodInfo methodInfo2 = AccessTools.Method(typeof(MultiplayerSaveLoadPatcher), "CreateUnlessLoadingMultiplayerSlot", (Type[])null, (Type[])null);
		MethodInfo methodInfo3 = typeof(GameObject).GetMethods(BindingFlags.Instance | BindingFlags.Public).FirstOrDefault((MethodInfo method) => method.Name == "GetComponent" && method.IsGenericMethodDefinition && method.GetParameters().Length == 0)?.MakeGenericMethod(typeof(Item));
		MethodInfo methodInfo4 = AccessTools.Method(typeof(MultiplayerSaveLoadPatcher), "GetItemUnlessNull", (Type[])null, (Type[])null);
		MethodInfo methodInfo5 = AccessTools.Method(typeof(Body), "PickUpItem", new Type[3]
		{
			typeof(Item),
			typeof(int),
			typeof(bool)
		}, (Type[])null);
		MethodInfo methodInfo6 = AccessTools.Method(typeof(MultiplayerSaveLoadPatcher), "PickUpUnlessNull", (Type[])null, (Type[])null);
		if (methodInfo == null || methodInfo2 == null || methodInfo3 == null || methodInfo4 == null || methodInfo5 == null || methodInfo6 == null)
		{
			return list;
		}
		for (int num = 0; num < list.Count; num++)
		{
			if (CodeInstructionExtensions.Calls(list[num], methodInfo))
			{
				list[num] = new CodeInstruction(OpCodes.Call, (object)methodInfo2);
			}
			else if (CodeInstructionExtensions.Calls(list[num], methodInfo3))
			{
				list[num] = new CodeInstruction(OpCodes.Call, (object)methodInfo4);
			}
			else if (CodeInstructionExtensions.Calls(list[num], methodInfo5))
			{
				list[num] = new CodeInstruction(OpCodes.Call, (object)methodInfo6);
			}
		}
		return list;
	}
}
