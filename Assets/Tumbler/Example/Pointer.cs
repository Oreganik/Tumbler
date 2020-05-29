// TUMBLER
// Copyright (c) 2020 Ted Brown

using Tumbler;
using UnityEngine;

namespace Tumbler.Example
{
	public class Pointer : MonoBehaviour 
	{
		private const float MIN_DISTANCE = 0.5f;

		private Vector3 Forward
		{
			get 
			{ 
				if (_tumbleObject && _tumbleObject.UseFixedOffset == false)
				{
					return transform.TransformDirection(_localRaycastDirection);
				}
				else
				{
					return transform.forward;
				}
			}
		}

		public float _offset = -.1f;
		public float _maxRaycastDistance = 10;
		public float _noTargetLineDistance = 1;
		public float _scrollSpeed = 100;
		public LayerMask _layerMask = 1;

		// mostly transparent blue to full transparent
		public Gradient _noTargetGradient = new Gradient() 
		{
			colorKeys = new GradientColorKey[2] {
				// Add your colour and specify the stop point
				new GradientColorKey(new Color(0, 0.8f, 1), 0),
				new GradientColorKey(new Color(1, 1, 1), 1)
			},
			alphaKeys = new GradientAlphaKey[2] {
				new GradientAlphaKey(0.5f, 0),
				new GradientAlphaKey(0, 1)
			}
		};

		// blue to semi-transparent purple
		public Gradient _targetGradient = new Gradient() 
		{
			colorKeys = new GradientColorKey[2] {
				new GradientColorKey(new Color(0, 0.8f, 1), 0),
				new GradientColorKey(new Color(0.5f, 0.25f, 1), 1)
			},
			alphaKeys = new GradientAlphaKey[2] {
				new GradientAlphaKey(1, 0),
				new GradientAlphaKey(0.75f, 1)
			}
		};

		private Collider _hoveredCollider;
		private float _distance;
		private LineRenderer _lineRenderer;
		private Transform _moveTarget;
		private TumbleObject _tumbleObject;
		private Vector3 _hitPoint;
		private Vector3 _localRaycastDirection;

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
			_lineRenderer = GetComponent<LineRenderer>();
			_moveTarget = new GameObject("Move Target").transform;
			_moveTarget.parent = transform;
			_moveTarget.localPosition = Vector3.zero;
			_moveTarget.localRotation = Quaternion.identity;
			transform.parent = Camera.main.transform;
			transform.localRotation = Quaternion.identity;
			Cursor.lockState = CursorLockMode.Locked;
		}

		protected void Update ()
		{
			if (_tumbleObject)
			{
				// Draw a ray from our origin towards the center of the held object.
				_lineRenderer.SetPosition(0, transform.position);

				// Determine line's end point
				RaycastHit hit;
				if (Physics.Raycast(transform.position, Forward, out hit, _maxRaycastDistance, _layerMask, QueryTriggerInteraction.Ignore))
				{
					_lineRenderer.SetPosition(1, hit.point);
				}
				else
				{
					_lineRenderer.SetPosition(1, transform.position + Forward * _maxRaycastDistance);
				}

				if (Input.GetMouseButtonUp(0))
				{
					_tumbleObject.Release();
					_tumbleObject = null;
				}
				else
				{
					float scroll = Input.GetAxis("Mouse ScrollWheel");
					_distance = Mathf.Max(_distance + scroll * _scrollSpeed * Time.deltaTime, MIN_DISTANCE);
					_moveTarget.position = transform.position + Forward * _distance;
				}
			}
			else
			{
				// See if the pointer is hovering a collider
				_hoveredCollider = GetHoveredCollider();

				transform.localPosition = Vector3.up * _offset;

				// Draw a ray from our origin to a TBD point
				_lineRenderer.SetPosition(0, transform.position);

				// If we hit an object...
				if (_hoveredCollider)
				{
					// ... change the end point to the hit point
					_lineRenderer.SetPosition(1, _hitPoint);

					// Since we're hovering a collider, get its PointerTarget (if it exists)
					TumbleObject hoveredTumbleObject = _hoveredCollider.GetComponentInParent<TumbleObject>();

					// If the user can grab this object, change the gradient
					if (hoveredTumbleObject)
					{
						_lineRenderer.colorGradient = _targetGradient;

						// If the user presses the left mouse button, pick up the object
					 	if (Input.GetMouseButtonDown(0))
						{
							_tumbleObject = hoveredTumbleObject;
							
							// Find the distance to the origin of the object. (tumble object can be extended to support a custom center)
							_distance = Vector3.Distance(transform.position, _tumbleObject.transform.position);

							// If the target object doesn't used a fixed offset, change the raycast direction so it points to the center of the object.
							// If we don't do this, the object will immediately try and move to a position set by the raycast.
							// There are other options here, including storing the offset.
							if (_tumbleObject.UseFixedOffset == false)
							{
								_localRaycastDirection = (_tumbleObject.transform.position - transform.position).normalized;

								// Make it local so we can change it each frame to world
								_localRaycastDirection = transform.InverseTransformDirection(_localRaycastDirection);
							}

							// Move the move target to the appropriate location
							_moveTarget.position = transform.position + Forward * _distance;

							_tumbleObject.Grab(_moveTarget);
						}
					}
					else
					{
						_lineRenderer.colorGradient = _noTargetGradient;
					}
				}
				// Otherwise ...
				else
				{
					// ... change the line color and set the end point to our max line distance
					_lineRenderer.colorGradient = _noTargetGradient;
					_lineRenderer.SetPosition(1, transform.position + Forward * _noTargetLineDistance);
				}
			}
		}
	}
}
