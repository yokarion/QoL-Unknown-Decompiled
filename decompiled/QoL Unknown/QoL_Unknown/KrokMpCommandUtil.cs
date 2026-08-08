using BepInEx.Logging;

namespace QoL_Unknown;

internal static class KrokMpCommandUtil
{
	public static void Initialize(ManualLogSource manualLogger)
	{
		KrokoshaMpCompat.Initialize(manualLogger);
	}

	public static void EnsureReceiversRegistered()
	{
		KrokoshaMpCompat.EnsureInitializedForRuntime();
	}

	public static bool SendClientSpawnRequest(string commandString)
	{
		return KrokoshaMpCompat.SendClientSpawnRequest(commandString);
	}

	public static bool SendClientHealRequest()
	{
		return KrokoshaMpCompat.SendClientHealRequest();
	}

	public static bool AreClientCheatCommandsAllowed()
	{
		return KrokoshaMpCompat.AreClientCheatCommandsAllowed();
	}
}
