using System.Collections.Generic;

namespace QoL_Unknown;

internal sealed class DiscordPresenceContext
{
	public bool InWorld;

	public bool IsTutorial;

	public bool Alive;

	public int Depth;

	public int BiomeDepth;

	public string LayerName;

	public string Status;

	public string LastSpeech;

	public string Mode;

	public string Death;

	public string TutorialCourse;

	public float Health;

	public float Hunger;

	public float Thirst;

	public float Happiness;

	public string DefaultDetails;

	public string DefaultState;

	public string DefaultLargeImageText;

	public string DefaultSmallImageText;

	public Dictionary<string, DiscordPresenceRuleValue> RuleValues;
}
