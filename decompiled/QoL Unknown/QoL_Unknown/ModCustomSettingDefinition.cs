using System;
using UnityEngine;

namespace QoL_Unknown;

public sealed class ModCustomSettingDefinition
{
	public string Id;

	public string Label;

	public string Description;

	public SettingCategory Category;

	public string CustomCategory;

	public string TemplateResourcePath;

	public Action<GameObject> Binder;

	public Func<bool> IsVisible;
}
