using UnityEngine;

namespace QoL_Unknown;

internal static class UIScrollInputRouter
{
	private static int consumedFrame = -1;

	public static void ResetFrame()
	{
		if (consumedFrame != Time.frameCount)
		{
			consumedFrame = -1;
		}
	}

	public static bool IsConsumed()
	{
		ResetFrame();
		return consumedFrame == Time.frameCount;
	}

	public static bool TryConsume()
	{
		ResetFrame();
		if (consumedFrame == Time.frameCount)
		{
			return false;
		}
		consumedFrame = Time.frameCount;
		return true;
	}
}
