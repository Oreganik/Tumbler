// TUMBLER
// Copyright (c) 2020 Ted Brown

using Jambox;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tumbler
{
	public class TumbleInput : MonoBehaviour 
	{
		// Prioritize scroll (Y) over swipe (X) through deadzone control
		public static float DeadzoneX = 0.2f;
		public static float DeadzoneY = 0.1f;

		public virtual bool GrabObject
		{
			get 
			{
				if (_tumblePointer.AttachPoint == AttachPoint.MainCamera)
					return Input.GetMouseButtonDown(1); 

				return (OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, _ovrController) >= 0.9f);
			}
		}

		public virtual bool ReleaseObject
		{
			get 
			{
				if (_tumblePointer.AttachPoint == AttachPoint.MainCamera)
					return Input.GetMouseButtonUp(1); 

				return (OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, _ovrController) <= 0.1f);

				// if (_tumblePointer.AttachPoint == AttachPoint.LeftHand)
				// 	return (OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, OVRInput.Controller.LTouch) <= 0.1f);

				// if (_tumblePointer.AttachPoint == AttachPoint.RightHand)
				// 	return (OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, OVRInput.Controller.RTouch) <= 0.1f);

				// Debug.LogWarning("TumbleInput: Support for AttachPoint." + _tumblePointer.AttachPoint + " has not been implemented");
				// return false;
			}
		}

		public virtual float ScrollAmount
		{
			get
			{
				if (_tumblePointer.AttachPoint == AttachPoint.MainCamera)
					// Shift key toggles swipe not scroll
					if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) return 0;
					else return Input.GetAxis("Mouse ScrollWheel") * _scrollSpeed * Time.deltaTime;

				float amount = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, _ovrController).y;
				if (Mathf.Abs(amount) < DeadzoneY) return 0;
				return amount * _scrollSpeed * Time.deltaTime;

				// if (_tumblePointer.AttachPoint == AttachPoint.LeftHand)
				// {
				// 	float amount = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.LTouch).y;
				// 	if (Mathf.Abs(amount) < DeadzoneY) return 0;
				// 	return amount * _scrollSpeed * Time.deltaTime;
				// }

				// if (_tumblePointer.AttachPoint == AttachPoint.RightHand)
				// {
				// 	float amount = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.RTouch).y;
				// 	if (Mathf.Abs(amount) < DeadzoneY) return 0;
				// 	return amount * _scrollSpeed * Time.deltaTime;
				// }

				// Debug.LogWarning("TumbleInput: Support for AttachPoint." + _tumblePointer.AttachPoint + " has not been implemented");
				// return 0;
			}
		}

		public virtual float SwipeAmount
		{
			get
			{
				if (_tumblePointer.AttachPoint == AttachPoint.MainCamera)
					// Shift key toggles swipe not scroll
					if (Input.GetKey(KeyCode.LeftShift) == false && Input.GetKey(KeyCode.RightShift) == false) return 0;
					else return Input.GetAxis("Mouse ScrollWheel") * _swipeSpeed * Time.deltaTime;

				float amount = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, _ovrController).x;
				if (Mathf.Abs(amount) < DeadzoneX) return 0;
				return amount * _swipeSpeed * Time.deltaTime;

				// if (_tumblePointer.AttachPoint == AttachPoint.LeftHand)
				// {
				// 	float amount = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.LTouch).x;
				// 	if (Mathf.Abs(amount) < DeadzoneX) return 0;
				// 	return amount * _swipeSpeed * Time.deltaTime;
				// }

				// if (_tumblePointer.AttachPoint == AttachPoint.RightHand)
				// {
				// 	float amount = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.RTouch).x;
				// 	if (Mathf.Abs(amount) < DeadzoneX) return 0;
				// 	return amount * _swipeSpeed * Time.deltaTime;
				// }

				// Debug.LogWarning("TumbleInput: Support for AttachPoint." + _tumblePointer.AttachPoint + " has not been implemented");
				// return 0;
			}
		}

		public float _scrollSpeed = 100;
		public float _swipeSpeed = 100;

		private OVRInput.Controller _ovrController;
		private TumblePointer _tumblePointer;
		private TumbleTarget _tumbleTarget;

		private float GetJoystickInputAfterDeadzone (float value)
		{
			if (Mathf.Abs(value) < DeadzoneX) return 0;
			return value;
		}

		protected void Start ()
		{
			_tumblePointer = GetComponent<TumblePointer>();
			_tumbleTarget = _tumblePointer.TumbleTarget;

			if (_tumblePointer.AttachPoint == AttachPoint.LeftHand)
			{
				_ovrController = OVRInput.Controller.LTouch;
			}
			else if (_tumblePointer.AttachPoint == AttachPoint.RightHand)
			{
				_ovrController = OVRInput.Controller.RTouch;
			}
			else
			{
				_ovrController = OVRInput.Controller.None;
			}
		}

		protected void Update ()
		{

			// Test TumbleTarget flex amounts
			if (Input.GetKeyDown(KeyCode.Alpha0)) _tumbleTarget.FlexPercentage = 0.0f;
			if (Input.GetKeyDown(KeyCode.Alpha1)) _tumbleTarget.FlexPercentage = 0.1f;
			if (Input.GetKeyDown(KeyCode.Alpha2)) _tumbleTarget.FlexPercentage = 0.2f;
			if (Input.GetKeyDown(KeyCode.Alpha3)) _tumbleTarget.FlexPercentage = 0.3f;
			if (Input.GetKeyDown(KeyCode.Alpha4)) _tumbleTarget.FlexPercentage = 0.4f;
			if (Input.GetKeyDown(KeyCode.Alpha5)) _tumbleTarget.FlexPercentage = 0.5f;
			if (Input.GetKeyDown(KeyCode.Alpha6)) _tumbleTarget.FlexPercentage = 0.6f;
			if (Input.GetKeyDown(KeyCode.Alpha7)) _tumbleTarget.FlexPercentage = 0.7f;
			if (Input.GetKeyDown(KeyCode.Alpha8)) _tumbleTarget.FlexPercentage = 0.8f;
			if (Input.GetKeyDown(KeyCode.Alpha9)) _tumbleTarget.FlexPercentage = 0.9f;

			if (_tumblePointer.IsHoldingObject)
			{
				if (ReleaseObject)
				{
					_tumblePointer.ReleaseObject();
				}
				else
				{
					_tumblePointer.Distance += ScrollAmount;
					// this is not working properly
					_tumblePointer.Rotate(Vector3.up, SwipeAmount);
				}
			}
			else if (_tumblePointer.HasTarget)
			{
				if (GrabObject)
				{
					_tumblePointer.GrabObject();
				}
			}
		}
	}
}
