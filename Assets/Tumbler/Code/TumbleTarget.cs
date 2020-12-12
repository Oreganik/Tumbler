// TUMBLER
// Copyright (c) 2020 Ted Brown

using Jambox;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tumbler
{
	public class TumbleTarget : MonoBehaviour 
	{
		public float FlexPercentage
		{
			get { return _flexPercenage; }
			set { _flexPercenage = Mathf.Clamp01(value); }
		}

		public Quaternion TargetRotation
		{
			get 
			{
				Quaternion rotation = _tumblePointer.transform.rotation;

				// used fixed rotation offset if the held object is configured that way
				if (_tumbleObject && _tumbleObject.UseFixedOffset)
				{
					rotation *= Quaternion.Euler(_tumbleObject.OffsetRotation);
				}
				// apply rotation offset if it's valid (a w of 0 is invalid)
				else if (Mathf.Approximately(_rotationOffset.w, 0) == false)
				{
					rotation *= Quaternion.Inverse(_rotationOffset);
				}
				return rotation;
			}
		}

		public TumblePointer TumblePointer
		{
			get { return _tumblePointer; }
		}

		public Vector3 TargetPosition
		{
			get 
			{
				Vector3 position = _tumblePointer.transform.position;
				if (_tumbleObject && _tumbleObject.UseFixedOffset)
				{
					position += _tumblePointer.transform.rotation * _tumbleObject.OffsetPosition;
				}
				else
				{
					position += _tumblePointer.Forward * _tumblePointer.Distance;
				}
				return position;
			}
		}

		private float _flexPercenage;
		private TumbleObject _tumbleObject;
		private TumblePointer _tumblePointer;
		private Quaternion _rotationOffset;

		public void HandleAttachToObject (TumbleObject tumbleObject, Transform handle)
		{
			_tumbleObject = tumbleObject;
			_rotationOffset = Quaternion.Inverse(handle.rotation) * _tumblePointer.transform.rotation;
			transform.position = TargetPosition;
			transform.rotation = TargetRotation;
			tumbleObject.HandleGrab(this, handle);
		}

		public void HandleDetach ()
		{
			_tumbleObject = null;
		}

		public void Initialize (TumblePointer tumblePointer)
		{
			_tumblePointer = tumblePointer;
			FlexPercentage = 0.5f;
		}

		public void Rotate (Vector3 rotation)
		{
			_rotationOffset *= Quaternion.Euler(rotation);
			//transform.Rotate(rotation, Space.World);
		}

		protected void LateUpdate ()
		{
			float lerpAmount = _tumblePointer.FlexLerp;
			transform.position = Vector3.Lerp(transform.position, TargetPosition, lerpAmount);
			transform.rotation = Quaternion.Lerp(transform.rotation, TargetRotation, lerpAmount);
		}

		protected void OnDrawGizmos()
		{
			// Draw a larger blue cube indicating where we are going
			Gizmos.color = Color.cyan;
			Gizmos.matrix = Matrix4x4.TRS(TargetPosition, TargetRotation, Vector3.one);
			Gizmos.DrawWireCube(Vector3.zero, Vector3.one * 0.15f);

			// Draw a small yellow cube indicating where this currently at
			Gizmos.color = Color.yellow;
			Gizmos.matrix = transform.localToWorldMatrix;
			Gizmos.DrawWireCube(Vector3.zero, Vector3.one * 0.1f);
		}
	}
}
