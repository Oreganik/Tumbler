// TUMBLER
// Copyright (c) 2020 Ted Brown

using Jambox;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tumbler
{
	public class TumbleConfig : MonoBehaviour 
	{
		/// <summary>There is a gap in perception between how fast objects are thrown, and how fast users THINK they are thrown.
		/// This number can be tuned to make the user feel more powerful, which matches expectations.
		/// It's only applied to objects with fixed offsets that weren't moving towards their target.</summary>
		public static float ThrowSpeedMultiplier = 1.5f;

		/// <summary>Determines when it's safe to return to fixed motion after flying back to the target location</summary>
		public static float ArriveDistance = 0.1f; // 0.1  0.01 is 1cm

		/// <summary>Determines when it's safe to return to fixed motion after flying back to the target location</summary>
		public static float ArriveDistanceFixedOffset = 0.1f;

		/// <summary>Determines when it's safe to return to fixed motion after flying back to the target location</summary>
		public static float ArriveAngle = 1;

		public static float ArriveAngleFixedOffset = 5;

		// Constants: Velocity
		/// <summary></summary>
		public static float MaxVelocityAgainstObjects = 10; // 10
		/// <summary></summary>
		public static float MaxVelocityNoContacts = 20;//20;
		/// <summary></summary>
		public static float MaxAccelerationAgainstObjects = 100;
		/// <summary></summary>
		public static float MaxAccelerationNoContacts = 1000;

		// Constants: Angular Velocity
		/// <summary></summary>
		public static float MinDeltaRotationAngle = 0.002f;
		/// <summary></summary>
		public static float MaxTorqueAgainstObjects = 1;
		/// <summary></summary>
		public static float MaxTorqueNoContacts = 360;

		/// <summary>Every x seconds, clear null objects from the touched colliders hashset. Helpful if colliders might be destroyed while this object is held.</summary>
		public static float ClearNullCollidersDelay = 0.5f;
	}
}
