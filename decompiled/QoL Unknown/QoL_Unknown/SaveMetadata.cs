using System;
using System.Collections.Generic;

namespace QoL_Unknown;

[Serializable]
public class SaveMetadata
{
	public string SeedInput;

	public int SeedValue;

	public int SeedFormatVersion;

	public string LocationName;

	public string Timestamp;

	public int BiomeIndex;

	public SerialVec3 PlayerPosition;

	public bool UseVanillaLayerSpawn;

	public float RadLinePosY;

	public bool RadLineActive;

	public float IGTSeconds;

	public float RTASeconds;

	public bool LoadedFromMenu;

	public bool SavedDuringMultiplayer;

	public string SaveKind;

	public string MultiplayerBundleName;

	public string MultiplayerSaveId;

	public string MultiplayerVersion;

	public string MultiplayerGameVersion;

	public string KrokoshaSaveId;

	public string KrokoshaMpVersion;

	public string KrokoshaGameVersion;

	public string LocalPersistentPlayerId;

	public List<SerialMod> Mods;
}
