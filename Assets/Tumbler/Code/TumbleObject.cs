// TUMBLER
// Copyright (c) 2020 Ted Brown

using System.Collections.Generic;
using UnityEngine;

namespace Tumbler
{
	public class TumbleObject : MonoBehaviour 
	{
		#region CONSTANTS
		/// <summary>Determines when it's safe to return to fixed motion after flying back to the target location</summary>
		private const float ARRIVE_DISTANCE = 0.1f; // 0.01 is 1cm

		/// <summary>Determines when it's safe to return to fixed motion after flying back to the target location</summary>
		private const float ARRIVE_ANGLE = 1;

		// Constants: Velocity
		/// <summary></summary>
		private const float MAX_VELOCITY_AGAINST_OBJECTS = 15;
		/// <summary></summary>
		private const float MAX_VELOCITY_NO_CONTACTS = 50;
		/// <summary></summary>
		private const float MAX_ACCELERATION_AGAINST_OBJECTS = 1;
		/// <summary></summary>
		private const float MAX_ACCELERATION_NO_CONTACTS = 20;

		// Constants: Angular Velocity
		/// <summary></summary>
		private const float MIN_DELTA_ROTATION_ANGLE = 0.002f;
		/// <summary></summary>
		private const float MAX_TORQUE_AGAINST_OBJECTS = 10;
		/// <summary></summary>
		private const float MAX_TORQUE_NO_CONTACTS = 360;

		/// <summary>Every x seconds, clear null objects from the touched colliders hashset. Helpful if colliders might be destroyed while this object is held.</summary>
		private const float CLEAR_NULL_COLLIDERS_DELAY = 0.5f;
		#endregion

		private static PhysicMaterial s_heldObjectPhysicMaterial;

		#region PUBLIC PROPERTIES
		public bool IsTouchingCollider
		{
			get { return _touchedColliders.Count > 0; }
		}

		public bool UseFixedOffset
		{
			get { return _useFixedOffset; }
		}
		#endregion

		#region INSPECTOR FIELDS
		#pragma warning disable 0649
		[Tooltip("A Box, Sphere, or Capsule collider that covers the entire object")]
		[SerializeField] private Collider _boundsCollider;
		[Tooltip("Tumbler uses a standard physic material for held objects. It can be overriden here.")]
		[SerializeField] private Collider _customPhysicMaterial;
		[Tooltip("If true, target location is consistently relative to move target (e.g. a hand-held tool). If false, target location offset is set on grab.")]
		[SerializeField] private bool _useFixedOffset;
		[SerializeField] private Vector3 _offsetPosition;
		[SerializeField] private Vector3 _offsetRotation;
		#pragma warning restore 0649
		#endregion

		#region PRIVATE FIELDS
		private bool _isAttached;
		private bool _wasUsingGravity;
		private Collider[] _childColliders;
		private Dictionary<Collider, PhysicMaterial> _originalPhysicMaterials;
		private float _previousRotationAngle;
		private float _lastClearedNullColliders;
		private HashSet<Collider> _touchedColliders;
		private Transform _moveTarget;
		private Quaternion _rotationOffset;
		private Rigidbody _rigidbody;
		private VelocityTracker _velocityTracker;
		#endregion

		#region PUBLIC METHODS
		public void Grab (Transform moveTarget)
		{
			_rotationOffset = Quaternion.Inverse(transform.rotation) * moveTarget.rotation;
			_wasUsingGravity = _rigidbody.useGravity;
			_rigidbody.useGravity = false;
			_moveTarget = moveTarget;

			// Change the physic material of all child colliders
			foreach (Collider collider in _childColliders)
			{
				collider.sharedMaterial = s_heldObjectPhysicMaterial;
			}

			_velocityTracker.Activate();

			enabled = true;
		}

		public void Release (Transform newParent = null)
		{
			// You can store the previous parent and restore it here, if you want.
			transform.parent = newParent;
			_rigidbody.useGravity = _wasUsingGravity;

			// Restore the physic material of all child colliders
			foreach (Collider collider in _childColliders)
			{
				collider.sharedMaterial = _originalPhysicMaterials[collider];
			}

			if (_isAttached)
			{
				_rigidbody.velocity = _velocityTracker.FrameLinearVelocity;
				_rigidbody.angularVelocity = _velocityTracker.FrameAngularVelocity;
			}

			_velocityTracker.Deactivate();

			enabled = false;
		}
		#endregion

		#region PRIVATE METHODS
		private void AttachToMoveTarget ()
		{
			transform.parent = _moveTarget;
			_rigidbody.velocity = Vector3.zero;
			_rigidbody.angularVelocity = Vector3.zero;

			if (_useFixedOffset)
			{
				transform.localPosition = _offsetPosition;
				transform.localRotation = Quaternion.Euler(_offsetRotation);
			}

			_isAttached = true;
		}

		/// <summary>Run during FixedUpdate. Applies physics forces to reach target location.</summary>
		private void FlexibleMove (Vector3 targetPosition, Quaternion targetRotation)
		{
			// Move to root of scene
			if (transform.parent)
			{
				transform.parent = null;
			}

			// Position and velocity
			Vector3 positionDelta = targetPosition - transform.position;
			Vector3 velocityTarget = (positionDelta) * 1.0f / Time.fixedDeltaTime;

			Vector3 newVelocity = _rigidbody.velocity;

			if (float.IsNaN(velocityTarget.x) == false)
			{
				if (_touchedColliders.Count > 0)
				{
					newVelocity = Vector3.MoveTowards(newVelocity, velocityTarget, MAX_ACCELERATION_AGAINST_OBJECTS);
					newVelocity = Vector3.ClampMagnitude(newVelocity, MAX_VELOCITY_AGAINST_OBJECTS);
				}
				else
				{
					newVelocity = Vector3.MoveTowards(newVelocity, velocityTarget, MAX_ACCELERATION_NO_CONTACTS);
					newVelocity = Vector3.ClampMagnitude(newVelocity, MAX_VELOCITY_NO_CONTACTS);
				}
			}

			_rigidbody.velocity = newVelocity;

			// Rotation and angular velocity

			// If we're applying force, see how far we need to go.
			targetRotation = targetRotation * Quaternion.Inverse(_rotationOffset);
			Quaternion deltaRotation = targetRotation * Quaternion.Inverse(transform.rotation);
			float angle;
			Vector3 axis;
			deltaRotation.ToAngleAxis(out angle, out axis);

			if (angle > 180)
			{
				angle -= 360;
			}

			angle *= Mathf.Deg2Rad;

			if (angle != 0 && Mathf.Abs(angle - _previousRotationAngle) > MIN_DELTA_ROTATION_ANGLE)
			{
				Vector3 angularTarget = angle * axis;
				angularTarget = angularTarget * 1.0f / Time.fixedDeltaTime;

				if (float.IsNaN(angularTarget.x) == false)
				{
					if (_touchedColliders.Count > 0)
					{
						_rigidbody.angularVelocity = Vector3.MoveTowards(_rigidbody.angularVelocity, angularTarget, MAX_TORQUE_AGAINST_OBJECTS);
					}
					else
					{
						_rigidbody.angularVelocity = Vector3.MoveTowards(_rigidbody.angularVelocity, angularTarget, MAX_TORQUE_NO_CONTACTS);
					}
				}
			}
			else
			{
				_rigidbody.angularVelocity = Vector3.zero;
			}

			_previousRotationAngle = angle;
		}

		private void StartFlexibleMove ()
		{
			transform.parent = null;
			_isAttached = false;
		}
		#endregion

		#region MONOBEHAVIOUR EVENTS
		protected void Awake ()
		{
			if (_boundsCollider == null)
			{
				_boundsCollider = GetComponent<Collider>();
			}

			if (_boundsCollider == null)
			{
				Debug.LogWarning("TumbleObject on " + gameObject.name + " does not have a bounds collider. Disabling.", gameObject);
				Destroy(this);
				return;
			}

			_rigidbody = GetComponentInParent<Rigidbody>();

			if (_rigidbody == null)
			{
				Debug.LogWarning("TumbleObject requires a rigidbody component. Disabling.", gameObject);
				Destroy(this);
				return;
			}

			if (s_heldObjectPhysicMaterial == null)
			{
				s_heldObjectPhysicMaterial = (PhysicMaterial) Resources.Load("HeldObjectPhysicMaterial");
			}

			// Store references to the physics materials of the object. We change them while it's held.
			// Note that this uses the shared material: changes to the instances will be ignored. Modify as necessary.
			_childColliders = GetComponentsInChildren<Collider>();
			_originalPhysicMaterials = new Dictionary<Collider, PhysicMaterial>(_childColliders.Length);
			foreach (Collider collider in _childColliders)
			{
				_originalPhysicMaterials.Add(collider, collider.sharedMaterial);
			}

			// Find (or add) the velocity tracker. This lets us throw objects that were attached to the move target.
			_velocityTracker = GetComponent<VelocityTracker>();
			if (_velocityTracker == null)
			{
				_velocityTracker = gameObject.AddComponent<VelocityTracker>();
			}
			_velocityTracker.Deactivate();

			_touchedColliders = new HashSet<Collider>();

			enabled = false;
		}

		protected void FixedUpdate ()
		{
			if (_isAttached)
			{
				if (IsTouchingCollider)
				{
					StartFlexibleMove();
				}
			}
			else
			{
				// Push the object so it tries to reach its target location
				FlexibleMove(_moveTarget.position, _moveTarget.rotation);

				// If it's free of colliders, we might be able to return to attached mode...
				if (IsTouchingCollider == false)
				{
					// Is it close enough by distance?
					if (Vector3.Distance(transform.position, _moveTarget.position) < ARRIVE_DISTANCE)
					{
						// Is it close enough by rotation?
						Quaternion targetRotation = _moveTarget.rotation * Quaternion.Inverse(_rotationOffset);
						Quaternion deltaRotation = targetRotation * Quaternion.Inverse(transform.rotation);
						float angle;
						Vector3 axis;
						deltaRotation.ToAngleAxis(out angle, out axis);

						if (angle > 180)
						{
							angle -= 360;
						}

						if (Mathf.Abs(angle) < ARRIVE_ANGLE)
						{
							AttachToMoveTarget();
						}
					}
				}
			}
		}

		protected void OnCollisionEnter(Collision collision)
		{
			// Add to a hashset. If it already exists, it will be rejected.
			_touchedColliders.Add(collision.collider);
		}

		protected void OnCollisionExit(Collision collision)
		{
			// Remove from a hashset. If it is not found, it will be ignored.
			_touchedColliders.Remove(collision.collider);
		}

		protected void Update ()
		{
			// Remove null colliders from _touchedColliders every CLEAR_NULL_COLLIDERS_DELAY seconds
			if (Time.time > _lastClearedNullColliders + CLEAR_NULL_COLLIDERS_DELAY)
			{
				_touchedColliders.RemoveWhere(x => x == null);
				_lastClearedNullColliders = Time.time;
			}
		}
		#endregion
	}
}
