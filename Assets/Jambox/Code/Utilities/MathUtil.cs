// JAMBOX
// General purpose game code for Unity
// Copyright (c) 2020 Ted Brown

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Jambox
{
	public class MathUtil : MonoBehaviour 
	{
		public static float ClampAngle360(float angle)
		{
			while (angle < -360f) angle += 360f;
			while (angle > 360f) angle -= 360f;
			return angle;
		}

		public static float ClampAngle(float angle, float min, float max)
		{
			while (angle < -360F) angle += 360F;
			while (angle > 360F) angle -= 360F;
			return Mathf.Clamp(angle, min, max);
		}
	}
}
