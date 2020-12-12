// TUMBLER
// Copyright (c) 2020 Ted Brown

using System.Collections.Generic;
using Tumbler;
using UnityEngine;

namespace Tumbler
{
	public class TumblePointer : MonoBehaviour 
	{
		/// <summary>Determines how short the distance from the parent can be. Can be negative if you want.</summary>
		public static float MinDistance = 0;

		/// <summary>Determines how far this target can be from the parent</summary>
		public static float MaxDistance = 10; 

		/// <summary>Determines Lerp to target location when Rigidity is 0 and Flex is enabled</summary>
		public static float DefaultMaxFlexAmount = 0.6f;

		public AttachPoint AttachPoint
		{
			get { return _attachPoint; }
		}

		/// <summary>If true, the pointer is pointing at a Tumble Object, but not holding it.</summary>
		public bool HasTarget
		{
			get { return _isHoldingObject == false && _tumbleObject != null; }
		}

		/// <summary>If true, the pointer is holding an object that is not null.</summary>
		public bool IsHoldingObject
		{
			get { return _isHoldingObject && _tumbleObject != null; }
		}

		public float Distance
		{
			get { return _distance; }
			set { _distance = Mathf.Clamp(value, MinDistance, MaxDistance); }
		}

		public float FlexLerp
		{
			get
			{
				if (_tumbleObject && _tumbleObject.UseFixedOffset) return 1;
				if (_distance < MinFlexDistance) return 1;
				if (_distance > MaxFlexDistance) return _maxFlexAmount;
				float t = (_distance - MinFlexDistance) / (MaxFlexDistance - MinFlexDistance);
				return Mathf.Lerp(1, _maxFlexAmount, t);
			}
		}

		public float MinFlexDistance = 0.5f;
		public float MaxFlexDistance = 3;

		public float MaxFlexAmount
		{
			get { return _maxFlexAmount; }
			set { _maxFlexAmount = Mathf.Clamp01(value); }
		}

		public TumbleObject TumbleObject
		{
			get { return _tumbleObject; }
		}

		public TumbleTarget TumbleTarget
		{
			get { return _tumbleTarget; }
		}

		public Vector3 Forward
		{
			get 
			{ 
				if (IsHoldingObject && _tumbleObject.UseFixedOffset == false)
				{
					return transform.TransformDirection(_localRaycastDirection);
				}
				else
				{
					return transform.forward;
				}
			}
		}

		public Vector3 HitPoint
		{
			get { return _hitPoint; }
		}

		#pragma warning disable 0649
		[SerializeField] private AttachPoint _attachPoint;
		[SerializeField] private float _maxRaycastDistance = 10;
		[SerializeField] private float _noTargetLineDistance = 1;
		[SerializeField] private LayerMask _layerMask = 1;
		[SerializeField] private Transform _handTransform;
		#pragma warning restore 0649

		private static Dictionary<AttachPoint, TumblePointer> s_attachedPointers;

		private BendyLine _bendyLine;
		private bool _isHoldingObject;
		private Collider _hoveredCollider;
		private float _distance;
		private float _maxFlexAmount;
		private float _spin;
		private TumbleObject _tumbleObject;
		private TumbleTarget _tumbleTarget;
		private Vector3 _hitPoint;
		private Vector3 _localRaycastDirection;

		public static TumblePointer GetPointer (AttachPoint attachPoint)
		{
			TumblePointer pointer = null;
			if (s_attachedPointers.TryGetValue(attachPoint, out pointer) == false)
			{
				Debug.LogWarning("No TumblePointer found for AttachPoint." + attachPoint);
			}
			return pointer;
		}

		public bool GrabObject ()
		{
			if (HasTarget)
			{
				// Get the handle we'll be attaching to
				Transform handle = _tumbleObject.GetHandleForPointer(this);

				// Exit early if there's an error
				if (handle == null)
				{
					Debug.LogError("TumbleObject.GetHandleForPointer on '" + _tumbleObject.name + "' returned null. This is unexpected. Ignoring grab request.");
					return false;
				}

				_isHoldingObject = true;

				// Reset spin and flex
				_spin = 0;
				_maxFlexAmount = DefaultMaxFlexAmount;

				_distance = Vector3.Distance(transform.position, handle.position);

				// Draw a vertical line indicating where forward * distance is
				Debug.DrawRay(transform.position + Forward * _distance, Vector3.up * .3f, Color.red, 1);

				// If the target object doesn't used a fixed offset, change the raycast direction so it points to the center of the object.
				// If we don't do this, the object will immediately try and move to a position set by the raycast.
				// There are other options here, including storing the offset.
				if (_tumbleObject.UseFixedOffset == false)
				{
					_localRaycastDirection = (handle.position - transform.position).normalized;

					// Make it local so we can change it each frame to world
					_localRaycastDirection = transform.InverseTransformDirection(_localRaycastDirection);
				}

				_tumbleTarget.HandleAttachToObject(_tumbleObject, handle);

				return true;
			}

			return false;
		}

		public bool ReleaseObject ()
		{
			if (IsHoldingObject)
			{
				_tumbleObject.Release();
				_tumbleObject = null;
				_isHoldingObject = false;
				return true;
			}
			return false;
		}

		public void Rotate (Vector3 axis, float amount)
		{
			while (amount > 360) amount -= 360;
			while (amount < -360) amount += 360;
			_tumbleTarget.Rotate(axis * amount);
		}

		protected virtual void AttachToTransform (Transform targetTransform)
		{
			transform.parent = targetTransform;
			transform.localPosition = Vector3.zero;
			transform.localRotation = Quaternion.identity;
		}

		protected virtual Collider GetHoveredCollider () 
		{
			// Use the mouse pointer by default. Can also be camera center for reticle.
			RaycastHit hit;
			if (Physics.Raycast(transform.position, Forward, out hit, _maxRaycastDistance, _layerMask, QueryTriggerInteraction.Ignore))
			{
				_hitPoint = hit.point;
				return hit.collider;
			}
			return null; 
		}

		protected void Awake ()
		{
			_bendyLine = GetComponent<BendyLine>();
			_tumbleTarget = new GameObject("Tumble Target").AddComponent<TumbleTarget>();
			_tumbleTarget.Initialize(this);

			if (s_attachedPointers == null)
			{
				s_attachedPointers = new Dictionary<AttachPoint, TumblePointer>();
			}
			else
			{
				s_attachedPointers.Clear();
			}

			switch (_attachPoint)
			{
				case AttachPoint.MainCamera:
					AttachToTransform(Camera.main.transform);
					Cursor.lockState = CursorLockMode.Locked;
					break;

				case AttachPoint.LeftHand:
				case AttachPoint.RightHand:
					if (OVRManager.instance)
					{
						// TODO: Find the hand!
						AttachToTransform(_handTransform);
					}
					else
					{
						Debug.Log("Disabling " + gameObject.name + " as OVRManager is not detected");
					}
					break;
			}

			if (s_attachedPointers.ContainsKey(_attachPoint))
			{
				Debug.LogError("Multiple TumblePointers assigned to AttachPoint." + _attachPoint);
				Debug.Break();
			}
			else
			{
				s_attachedPointers.Add(_attachPoint, this);
			}
		}

		protected void Update ()
		{
			if (_isHoldingObject)
			{
				// Null ref check
				if (_tumbleObject == null)
				{
					_isHoldingObject = false;
				}
				else
				{
					// Draw a ray from our origin
					Vector3 lineStart = transform.position;

					// Default line end is the current object's handle
					Vector3 lineEnd = _tumbleObject.HandlePosition;

					_bendyLine.DrawCurvedLine(lineStart, _tumbleTarget.TargetPosition, lineEnd);

					// Return to main thread. This allows the code to fall through if tumble object was null this frame.
					return;
				}
			}

			// We are not holding an object, or we were, but _tumbleObject was null.
			{
				// See if the pointer is hovering a collider
				_hoveredCollider = GetHoveredCollider();

				// Draw a ray from our origin
				Vector3 lineStart = transform.position;

				// Default line end is max distance away
				Vector3 lineEnd = transform.position + Forward * _noTargetLineDistance;

				// If we hit an object...
				if (_hoveredCollider)
				{
					// ... change the end point to the hit point
					lineEnd = _hitPoint;

					// Since we're hovering a collider, get its PointerTarget (if it exists)
					_tumbleObject = _hoveredCollider.GetComponentInParent<TumbleObject>();

					// If the user can grab this object, change the gradient
					if (_tumbleObject)
					{
						_bendyLine.HandleValidTarget();
					}
					else
					{
						_bendyLine.HandleNoTarget();
					}
				}
				// Otherwise ...
				else
				{
					_tumbleObject = null;
					_bendyLine.HandleNoTarget();
				}

				_bendyLine.DrawStraightLine(lineStart, lineEnd);
			}
		}
	}
}
