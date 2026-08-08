using UnityEngine;

namespace QoL_Unknown;

internal sealed class ModKeybindDefinition
{
	public string Action { get; }

	public string DisplayNameKey { get; }

	public string DescriptionKey { get; }

	public KeyCode DefaultKey { get; }

	public string DisplayName => ModLocale.Get(DisplayNameKey, Action);

	public string Description => ModLocale.Get(DescriptionKey, string.Empty);

	public ModKeybindDefinition(string action, string displayNameKey, string descriptionKey, KeyCode defaultKey)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		Action = action;
		DisplayNameKey = displayNameKey;
		DescriptionKey = descriptionKey;
		DefaultKey = defaultKey;
	}
}
