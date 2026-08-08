using System;
using UnityEngine;

namespace QoL_Unknown;

[Serializable]
public struct SerialVec3
{
	public float x;

	public float y;

	public float z;

	public SerialVec3(Vector3 v)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		x = v.x;
		y = v.y;
		z = v.z;
	}

	public Vector3 ToVector3()
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		return new Vector3(x, y, z);
	}
}
