// TUMBLER
// Copyright (c) 2020 Ted Brown

// https://vanderpelomundo.blogspot.com/2018/05/steam-vr-interactionsystem-unity-3d_7.html

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tumbler
{
	public class TumbleObject : MonoBehaviour 
	{
		#region EVENTS
		public Action<TumbleObject> OnAttach;
		public Action<TumbleObject> OnDetach;
		#endregion

		#region STATICS
		private static PhysicMaterial s_heldObjectPhysicMaterial;
		#endregion

		#region PUBLIC PROPERTIES
		public bool IsTouchingCollider
		{
			get { return _touchedColliders.Count > 0; }
		}

		public bool UseFixedOffset
		{
			get { return _useFixedOffset; }
		}

		public TumbleTarget TumbleTarget
		{
			get { return _tumbleTarget; }
		}

		public Vector3 HandlePosition
		{
			get { return _activeHandle == null ? transform.position : _activeHandle.position; }
		}

		public Vector3 OffsetPosition
		{
			get { return _offsetPosition; }
		}

		public Vector3 OffsetRotation
		{
			get { return _offsetRotation; }
		}
		#endregion

		#region INSPECTOR FIELDS
		#pragma warning disable 0649
		[Tooltip("Tumbler uses a standard physic material for held objects. It can be overriden here.")]
		[SerializeField] private Collider _customPhysicMaterial;
		[SerializeField] private bool _grabHitPoint;
		[Tooltip("If true, target location is consistently relative to move target (e.g. a hand-held tool). If false, target location offset is set on grab.")]
		[SerializeField] private bool _useFixedOffset;
		[SerializeField] private Vector3 _offsetPosition;
		[SerializeField] private Vector3 _offsetRotation;
		[Tooltip("If there are transforms in this array, they will be used as 'handles' instead of the object pivot.")]
		[SerializeField] private Transform[] _customHandles;
		#pragma warning restore 0649
		#endregion

		#region PRIVATE FIELDS
		private bool _hitColliderThisFrame;
		private bool _isUsingPhysicsMotion;
		private bool _wasUsingGravity;
		private Collider[] _childColliders;
		private Dictionary<Collider, PhysicMaterial> _originalPhysicMaterials;
		private float _previousDeltaAngle;
		private float _lastClearedNullColliders;
		private HashSet<Collider> _touchedColliders;
		private Rigidbody _rigidbody;
		private Transform _activeHandle;
		private Transform _hitPointHandle;
		private Transform _previousParent;
		private TumbleTarget _tumbleTarget;
		private Vector3 _previousCenterOfMass;
		private VelocityTracker _velocityTracker;
		#endregion

		#region PUBLIC METHODS
		public Transform GetHandleForPointer (TumblePointer pointer)
		{
			if (_grabHitPoint)
			{
				_hitPointHandle.position = pointer.HitPoint;
				return _hitPointHandle;
			}

			// If there are no custom handles, return this object's transform.
			if (_customHandles.Length == 0)
			{
				return transform;
			}

			// Find the handle that is most in line with the pointer by using dot product.
			float bestDotProduct = -1;
			Transform bestTransform = null;
			Vector3 pointerForward = pointer.Forward;

			foreach (Transform handle in _customHandles)
			{
				Vector3 dir = (handle.position - pointer.transform.position).normalized;
				float dot = Vector3.Dot(dir, pointerForward);
				if (dot > bestDotProduct)
				{
					bestDotProduct = dot;
					bestTransform = handle;
				}
			}

			return bestTransform;
		}

		public void HandleGrab (TumbleTarget tumbleTarget, Transform handle)
		{
			_previousParent = transform.parent;
			_tumbleTarget = tumbleTarget;
			_activeHandle = handle;

			_wasUsingGravity = _rigidbody.useGravity;
			_rigidbody.useGravity = false;

			_previousCenterOfMass = _rigidbody.centerOfMass;
			_rigidbody.centerOfMass = transform.InverseTransformPoint(handle.position);

			if (IsTouchingCollider)
			{
				// clearing the parent enables physics movement based on logic in FixedUpdate
				transform.parent = null;
			}
			else
			{
				StartPhysicsMotion();
			}

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
			transform.parent = newParent == null ? _previousParent : newParent;

			_rigidbody.centerOfMass = _previousCenterOfMass;
			_rigidbody.useGravity = _wasUsingGravity;

			// Restore the physic material of all child colliders
			foreach (Collider collider in _childColliders)
			{
				collider.sharedMaterial = _originalPhysicMaterials[collider];
			}

			Vector3 newVelocity = Vector3.ClampMagnitude(_velocityTracker.TrackedLinearVelocity, TumbleConfig.MaxVelocityNoContacts);

			// If they are "held" in a single position, boost their velocity to match user expectations
			if (_useFixedOffset && _isUsingPhysicsMotion == false)
			{
				newVelocity *= TumbleConfig.ThrowSpeedMultiplier;
			}

			_rigidbody.velocity = newVelocity;
			_rigidbody.angularVelocity = _velocityTracker.TrackedAngularVelocity;

			_velocityTracker.Deactivate();

			_tumbleTarget.HandleDetach();
			enabled = false;

			if (OnDetach != null)
			{
				OnDetach(this);
			}
		}

		public static Vector3 ScalePosition (Vector3 p, Vector3 s)
		{
			p.x *= s.x;
			p.y *= s.y;
			p.z *= s.z;
			return p;
		}
		#endregion

		#region PRIVATE METHODS
		/// <summary>Run during FixedUpdate. Applies physics forces to reach target location.</summary>
		private void MoveTowardsLocation (Vector3 targetPosition, Quaternion targetRotation)
		{
			// Rotation and angular velocity

			// If we're applying force, see how far we need to go.
			Quaternion deltaRotation = _tumbleTarget.TargetRotation * Quaternion.Inverse(_activeHandle.rotation);
			float angle;
			Vector3 axis;
			deltaRotation.ToAngleAxis(out angle, out axis);

			while (angle > 180)
			{
				angle -= 360;
			}

			angle *= Mathf.Deg2Rad;

			if (angle != 0 && Mathf.Abs(angle - _previousDeltaAngle) > TumbleConfig.MinDeltaRotationAngle)
			{
				Vector3 angularTarget = angle * axis;
				angularTarget = angularTarget * 1.0f / Time.fixedDeltaTime;

				if (float.IsNaN(angularTarget.x) == false)
				{
					if (_touchedColliders.Count > 0)
					{
						_rigidbody.angularVelocity = Vector3.MoveTowards(_rigidbody.angularVelocity, angularTarget, TumbleConfig.MaxTorqueAgainstObjects);
					}
					else
					{
						_rigidbody.angularVelocity = Vector3.MoveTowards(_rigidbody.angularVelocity, angularTarget, TumbleConfig.MaxTorqueNoContacts);
					}
				}
			}
			else
			{
				_rigidbody.angularVelocity = Vector3.zero;
			}

			_previousDeltaAngle = angle;


			// Position and velocity
			Vector3 velocity = _rigidbody.velocity;
			Vector3 velocityTarget = (targetPosition - _activeHandle.position) / Time.fixedDeltaTime;

			//if (float.IsNaN(velocityTarget.x) == false)
			{
				if (_touchedColliders.Count > 0)
				{
					velocity = Vector3.MoveTowards(velocity, velocityTarget, TumbleConfig.MaxAccelerationAgainstObjects * Time.fixedDeltaTime);
					velocity = Vector3.ClampMagnitude(velocity, TumbleConfig.MaxVelocityAgainstObjects);
				}
				else
				{
					velocity = Vector3.MoveTowards(velocity, velocityTarget, TumbleConfig.MaxAccelerationNoContacts * Time.fixedDeltaTime);
					velocity = Vector3.ClampMagnitude(velocity, TumbleConfig.MaxVelocityNoContacts);
				}
			}

			_rigidbody.velocity = velocity;
		}

		private void StartPhysicsMotion ()
		{
			_isUsingPhysicsMotion = true;
			transform.parent = null;
		}

		private void StopPhysicsMotion ()
		{
			_isUsingPhysicsMotion = false;
			transform.parent = _tumbleTarget.transform;
			_rigidbody.velocity = Vector3.zero;
			_rigidbody.angularVelocity = Vector3.zero;

			// If a fixed offset is used, the TumbleTarget -- our parent -- is already at the target location.
			if (_useFixedOffset)
			{
				if (_activeHandle)
				{
					transform.localRotation = _activeHandle.localRotation;

					// for position, take object scale into account.
					Vector3 offset = _activeHandle.localPosition * -1;

					Transform tScaler = _activeHandle.transform;
					offset = ScalePosition(offset, tScaler.localScale);

					while (tScaler != transform)
					{
						tScaler = tScaler.parent;
						offset = ScalePosition(offset, tScaler.localScale);

						if (tScaler == transform.root)
						{
							Debug.LogWarning("TumbleObject handle appears to be outside of gameobject", gameObject);
							break;
						}
					}

					transform.localPosition = offset;
				}
				else
				{
					transform.localPosition = Vector3.zero;
					transform.localRotation = Quaternion.identity;
				}
			}

			if (OnAttach != null)
			{
				OnAttach(this);
			}
		}
		#endregion

		#region MONOBEHAVIOUR EVENTS
		protected void Awake ()
		{
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

			if (_grabHitPoint)
			{
				_hitPointHandle = new GameObject("Hit Point Handle").transform;
				_hitPointHandle.parent = transform;
			}

			enabled = false;
		}

		protected virtual bool HasArrivedAtPosition (Vector3 center, Vector3 targetPosition, float arriveDistance)
		{
			Vector3 delta = targetPosition - center;
			return delta.magnitude < arriveDistance;
		}

		protected virtual bool HasArrivedAtRotation (Quaternion center, Quaternion targetRotation, float arriveAngle)
		{
			Quaternion deltaQuaternion = targetRotation * Quaternion.Inverse(center);
			float deltaAngle;
			Vector3 axis;
			deltaQuaternion.ToAngleAxis(out deltaAngle, out axis);
			if (deltaAngle > 180) deltaAngle -= 360;
			return Mathf.Abs(deltaAngle) < arriveAngle;
		}

		protected virtual void MoveTowardsPosition (Vector3 center, Vector3 targetPosition, float arriveDistance)
		{
			Vector3 delta = targetPosition - center;
			Vector3 velocity = _rigidbody.velocity;
			Vector3 velocityTarget = delta / Time.fixedDeltaTime;

			if (IsTouchingCollider)
			{
				velocity = Vector3.MoveTowards(velocity, velocityTarget, TumbleConfig.MaxAccelerationAgainstObjects * Time.fixedDeltaTime);
				velocity = Vector3.ClampMagnitude(velocity, TumbleConfig.MaxVelocityAgainstObjects);
			}
			else
			{
				velocity = Vector3.MoveTowards(velocity, velocityTarget, TumbleConfig.MaxAccelerationNoContacts * Time.fixedDeltaTime);
				velocity = Vector3.ClampMagnitude(velocity, TumbleConfig.MaxVelocityNoContacts);
			}

			_rigidbody.velocity = velocity;
		}

		protected virtual bool RotateTowardsRotation (Quaternion center, Quaternion targetRotation, float arriveAngle)
		{
			Quaternion deltaQuaternion = targetRotation * Quaternion.Inverse(center);
			float deltaAngle;
			Vector3 axis;
			deltaQuaternion.ToAngleAxis(out deltaAngle, out axis);
			if (deltaAngle > 180) deltaAngle -= 360;

			Vector3 angularTarget = deltaAngle * Mathf.Deg2Rad * axis;
			angularTarget = angularTarget / Time.fixedDeltaTime;

			if (float.IsNaN(angularTarget.x) == false)
			{
				if (_touchedColliders.Count > 0)
				{
					_rigidbody.angularVelocity = Vector3.MoveTowards(_rigidbody.angularVelocity, angularTarget, TumbleConfig.MaxTorqueAgainstObjects);
				}
				else
				{
					_rigidbody.angularVelocity = Vector3.MoveTowards(_rigidbody.angularVelocity, angularTarget, TumbleConfig.MaxTorqueNoContacts);
				}
			}

			return false;
		}

		protected void FixedUpdate ()
		{
			if (_tumbleTarget == null)
			{
				return;
			}

			// Determine if the object is at the target position
			float arriveDistance = _useFixedOffset ? TumbleConfig.ArriveDistanceFixedOffset : TumbleConfig.ArriveDistance;
			bool hasArrivedAtPosition = HasArrivedAtPosition(_activeHandle.position, _tumbleTarget.TargetPosition, arriveDistance);

			// Determine if the object is at the target rotation
			float arriveAngle = _useFixedOffset ? TumbleConfig.ArriveAngleFixedOffset : TumbleConfig.ArriveAngle;
			bool hasArrivedAtRotation = HasArrivedAtRotation(_activeHandle.rotation, _tumbleTarget.TargetRotation, arriveAngle);

			if (hasArrivedAtPosition && hasArrivedAtRotation)
			{
				if (_isUsingPhysicsMotion)
				{
					StopPhysicsMotion();
				}
				// otherwise... nothing to do
			}
			else
			{
				if (_isUsingPhysicsMotion == false)
				{
					StartPhysicsMotion();
				}
				RotateTowardsRotation(_activeHandle.rotation, _tumbleTarget.TargetRotation, TumbleConfig.ArriveAngle);
				MoveTowardsPosition(_activeHandle.position, _tumbleTarget.TargetPosition, arriveDistance);
			}
		}

		protected void OnCollisionEnter(Collision collision)
		{
			_hitColliderThisFrame = true;
			// Add to a hashset. If it already exists, it will be rejected.
			_touchedColliders.Add(collision.collider);
		}

		protected void OnCollisionStay (Collision collision)
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
			_lastClearedNullColliders += Time.deltaTime;
			// Remove null colliders from _touchedColliders every CLEAR_NULL_COLLIDERS_DELAY seconds
			if (_lastClearedNullColliders > TumbleConfig.ClearNullCollidersDelay)
			{
				_touchedColliders.RemoveWhere(x => x == null);
				_lastClearedNullColliders = 0;
			}
		}
		#endregion
	}
}
