// TUMBLER
// Copyright (c) 2020 Ted Brown

using Jambox;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tumbler.Example
{
	public class PopGun : MonoBehaviour 
	{
		private bool AttackButtonDown
		{
			get 
			{
				if (_activeController == OVRInput.Controller.None) return Input.GetMouseButton(0);
				return (OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, _activeController) >= 0.9f);
			}
		}

		private bool AttackButtonUp
		{
			get 
			{
				if (_activeController == OVRInput.Controller.None) return Input.GetMouseButton(0) == false;
				return (OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, _activeController) <= 0.1f);
			}
		}

		public float _ballSpeed = 20;
		public GameObject _popBallPrefab;
		public Transform _firePoint;

		private static MaterialPropertyBlock s_propertyBlock;

		private bool _isAttached;
		private bool _waitingToReset;
		private bool _wasTouchingCollider;
		private OVRInput.Controller _activeController;
		private Renderer[] _renderers;
		private TumbleObject _tumbleObject;
		private TumblePointer _activePointer;

		public void Fire ()
		{
			GameObject popBall = Instantiate(_popBallPrefab, _firePoint.position, _firePoint.rotation);
			popBall.GetComponent<Rigidbody>().velocity = _firePoint.forward * _ballSpeed;
			_waitingToReset = true;
		}

		private void HandleAttach (TumbleObject tumbleObject)
		{
			if (tumbleObject != _tumbleObject) return;

			_activePointer = _tumbleObject.TumbleTarget.TumblePointer;

			if (_activePointer.AttachPoint == AttachPoint.LeftHand)
			{
				_activeController = OVRInput.Controller.LTouch;
				OvrAvatar.Instance.ShowLeftController(false);
			}
			else if (_activePointer.AttachPoint == AttachPoint.RightHand)
			{
				_activeController = OVRInput.Controller.RTouch;
				OvrAvatar.Instance.ShowRightController(false);
			}
			else // probably editor
			{
				_activeController = OVRInput.Controller.None;
			}

			_isAttached = true;
		}

		private void HandleDetach (TumbleObject tumbleObject)
		{
			if (_activeController == OVRInput.Controller.LTouch)
			{
				OvrAvatar.Instance.ShowLeftController(true);
			}
			else if (_activeController == OVRInput.Controller.RTouch)
			{
				OvrAvatar.Instance.ShowRightController(true);
			}

			_activePointer = null;
			_activeController = OVRInput.Controller.None;
			_isAttached = false;
		}

		protected void Awake ()
		{
			_tumbleObject = GetComponent<TumbleObject>();
			_tumbleObject.OnAttach += HandleAttach;
			_tumbleObject.OnDetach += HandleDetach;
			_renderers = GetComponentsInChildren<Renderer>();

			if (s_propertyBlock == null)
			{
				s_propertyBlock = new MaterialPropertyBlock();
			}
		}

		protected void OnDestroy ()
		{
			if (_tumbleObject)
			{
				_tumbleObject.OnAttach += HandleAttach;
				_tumbleObject.OnDetach += HandleDetach;
			}
		}

		protected void Update ()
		{
			if (_waitingToReset && AttackButtonUp)
			{
				_waitingToReset = false;
			}

			if (_waitingToReset == false && _isAttached && AttackButtonDown)
			{
				Fire();
			}

			bool touchingCollider = _tumbleObject.IsTouchingCollider;
			if (touchingCollider != _wasTouchingCollider)
			{
				Color c = touchingCollider ? Color.red : Color.white;
				foreach (Renderer r in _renderers)
				{
					r.GetPropertyBlock(s_propertyBlock);
					s_propertyBlock.SetColor("_Color", c);
					r.SetPropertyBlock(s_propertyBlock);
				}
			}
			_wasTouchingCollider = touchingCollider;
		}
	}
}
