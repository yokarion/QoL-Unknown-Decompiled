using System;
using System.Collections.Generic;
using BepInEx.Logging;
using HarmonyLib;

namespace QoL_Unknown;

internal static class RuntimePatchRegistry
{
	private static readonly object SyncRoot = new object();

	private static readonly HashSet<string> AppliedPatchIds = new HashSet<string>(StringComparer.Ordinal);

	public static bool PatchAllOnce(string harmonyId, Type patchType, ManualLogSource logger)
	{
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		if (string.IsNullOrEmpty(harmonyId))
		{
			throw new ArgumentException("A Harmony ID is required.", "harmonyId");
		}
		if (patchType == null)
		{
			throw new ArgumentNullException("patchType");
		}
		lock (SyncRoot)
		{
			if (!AppliedPatchIds.Add(harmonyId))
			{
				return false;
			}
		}
		try
		{
			new Harmony(harmonyId).PatchAll(patchType);
			return true;
		}
		catch (Exception arg)
		{
			lock (SyncRoot)
			{
				AppliedPatchIds.Remove(harmonyId);
			}
			if (logger != null)
			{
				logger.LogError((object)$"Failed to apply Harmony patches for {patchType.Name} ({harmonyId}).\n{arg}");
			}
			throw;
		}
	}
}
