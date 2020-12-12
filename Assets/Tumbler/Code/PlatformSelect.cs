// TUMBLER
// Copyright (c) 2020 Ted Brown

using Jambox;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tumbler
{
	[DefaultExecutionOrder(-999)]
	public class PlatformSelect : MonoBehaviour 
	{
		public enum SupportedPlatform { Both, EditorOnly, BuildOnly }

		public SupportedPlatform _supportedPlatform;

		protected void Awake ()
		{
			if (Application.isEditor && _supportedPlatform == SupportedPlatform.BuildOnly)
			{
				DestroyImmediate(gameObject);
				return;
			}

			if (Application.isEditor == false && _supportedPlatform == SupportedPlatform.EditorOnly)
			{
				DestroyImmediate(gameObject);
				return;
			}
		}
	}
}
