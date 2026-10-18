namespace QoL_Unknown;

internal static class MultiplayerCommandUtil
{
	public static void EnsureReceiversRegistered()
	{
		MultiplayerCompat.EnsureInitializedForRuntime();
	}

	public static bool SendClientSpawnRequest(string commandString)
	{
		return MultiplayerCompat.SendClientSpawnRequest(commandString);
	}

	public static bool SendClientHealRequest()
	{
		return MultiplayerCompat.SendClientHealRequest();
	}

	public static bool AreClientCheatCommandsAllowed()
	{
		return MultiplayerCompat.AreClientCheatCommandsAllowed();
	}
}
