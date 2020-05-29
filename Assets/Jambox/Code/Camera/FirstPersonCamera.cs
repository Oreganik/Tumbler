// JAMBOX
// General purpose game code for Unity
// Copyright (c) 2020 Ted Brown

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Jambox
{
	/// <summary>
	/// Super-simple camera controller that handles pitch and yaw.
	/// Restricts height to a base value. Does not collide with world.
	public class FirstPersonCamera : MonoBehaviour 
	{
		const float MAX_PITCH = 90;
		const float MIN_PITCH = -90;
		const float HEIGHT = 1.8f;

		public float _pitchSpeed = 3;
		public float _yawSpeed = 3;
		public float _moveSpeed = 5;

		private float _pitch;
		private float _yaw;

		protected void Update ()
		{
			Vector2 look = PInput.GetLook();
			_pitch += -look.y * _pitchSpeed;
			_yaw = MathUtil.ClampAngle360(_yaw + look.x * _yawSpeed);
			transform.rotation = Quaternion.Euler(new Vector3(_pitch, _yaw, 0));

			Vector3 move = PInput.GetWorldMove();
			move = Quaternion.Euler(new Vector3(0, _yaw, 0)) * move;
			Vector3 position = transform.position + move * _moveSpeed * Time.deltaTime;
			position.y = HEIGHT;
			transform.position = position;
		}
	}
}
