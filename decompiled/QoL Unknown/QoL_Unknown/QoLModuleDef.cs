using System;
using BepInEx.Logging;

namespace QoL_Unknown;

public class QoLModuleDef
{
	public string ID;

	public string Name;

	public string Description;

	public Action<ManualLogSource> InitAction;

	public Action DisableAction;

	public bool EnabledByDefault;

	public bool IsCore;
}
