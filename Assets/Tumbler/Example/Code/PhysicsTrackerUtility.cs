//
// Copyright (C) 2018 Magic Leap Inc.
// All Rights Reserved
//
// Author: Srinath Upadhyayula
//


using UnityEngine;

namespace Create
{
    /// <summary>
    /// This is a helper class that uses the Unity Labs' PhysicsTracker.cs to track 
    /// Linear and Angular velocities of a transform.
    /// </summary>
    public class PhysicsTrackerUtility : MonoBehaviour
	{
#pragma warning disable 0649
		[SerializeField] private bool _startTrackingImmediately;

#pragma warning restore 0649
		/// <summary>
		/// The transform of the object to track physics
		/// </summary>
#pragma warning disable 0649
		[SerializeField] private Transform _trackingTransform;

#pragma warning restore 0649
		/// <summary>
		/// This is set when the _trackingTransform is set. Used in FixedUpdate
		/// </summary>
		private bool _isTracking;

		/// <summary>
		/// Tracks and smooths Linear and Angular velocities of a given transform
		/// </summary>
		private Unity.Labs.SuperScience.PhysicsTracker _physicsTracker;

		#region Public Methods
		/// <summary>
		/// Returns the tracked Linear Velocity
		/// </summary>
		public Vector3 LinearVelocity
		{
			get
			{
				if (_isTracking)
				{
					return _physicsTracker.Velocity;
				}
				else
				{
					Debug.LogWarning("LinearVelocity is currently not being tracked. Make sure to call StartTracking before querying the Velocities");
				}
				return Vector3.zero;
			}
		}

		/// <summary>
		/// Returns the tracked Angular Velocity
		/// </summary>
		public Vector3 AngularVelocity
		{
			get
			{
				if (_isTracking)
				{
					return _physicsTracker.AngularVelocity;
				}
				else
				{
					Debug.LogWarning("AngularVelocity is currently not being tracked. Make sure to call StartTracking before querying the Velocities");
				}
				return Vector3.zero;
			}
		}

		/// <summary>
		/// Sets the _trackingTransform, calls Reset to clear any old samples, and sets _isTracking = true
		/// </summary>
		/// <param name="transformToTrack"></param>
		public void StartTracking(Transform transformToTrack)
		{
			if (_isTracking && _trackingTransform == transformToTrack)
			{
				return;
			}
			if (_isTracking && _trackingTransform != transformToTrack)
			{
				Debug.LogWarning("Tracking a new transform. Old Tracking information will be lost. Call StopTracking on old transform to stop seeing this warning.");
			}
			_trackingTransform = transformToTrack;
			if (_trackingTransform != null)
			{
				_isTracking = true;

				_physicsTracker.Reset(_trackingTransform.position, _trackingTransform.rotation, Vector3.zero, Vector3.zero);
			}
		}

		/// <summary>
		/// Stop Tracking 
		/// </summary>
		public void StopTracking()
		{
			_trackingTransform = null;
			_isTracking = false;
		}
		#endregion

		#region Monobehavior 
		private void Awake()
		{
			_physicsTracker = new Unity.Labs.SuperScience.PhysicsTracker();
		}

		private void Start()
		{
			if (_startTrackingImmediately)
			{
				StartTracking(_trackingTransform);
			}
		}

		private void FixedUpdate()
		{
			if (_isTracking && _trackingTransform != null)
			{
				_physicsTracker.Update(_trackingTransform.position, _trackingTransform.rotation, Time.smoothDeltaTime);
			}
		}
		#endregion
	}
}
