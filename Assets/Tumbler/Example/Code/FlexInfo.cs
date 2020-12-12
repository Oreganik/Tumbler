// TUMBLER
// Copyright (c) 2020 Ted Brown

using Jambox;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Tumbler
{
	public class FlexInfo : MonoBehaviour 
	{
		const string TEXT = "Distance: {0}\nFlex Distance: Min {1} / Max {2}\nMax Flex Lerp: {3}\nFlex Lerp: {4}";

		public TumblePointer _tumblePointer;

		private Text _text;

		protected void Awake ()
		{
			_text = GetComponent<Text>();
			_text.text = string.Empty;
		}

		protected void Update ()
		{
			_text.text = string.Format(TEXT, 
				_tumblePointer.Distance.ToString("F2"),
				_tumblePointer.MinFlexDistance.ToString("F2"),
				_tumblePointer.MaxFlexDistance.ToString("F2"),
				_tumblePointer.MaxFlexAmount.ToString("F2"),
				_tumblePointer.FlexLerp.ToString("F2")
			);
		}
	}
}
