// TUMBLER
// Copyright (c) 2020 Ted Brown

using Jambox;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tumbler
{
	public class ResetLevel : MonoBehaviour 
	{
		protected void Update ()
		{
			if (OVRManager.instance)
			{
				if (OVRInput.GetUp(OVRInput.Button.One))
				{
					SceneManager.LoadScene(gameObject.scene.path, LoadSceneMode.Single);
				}
			}
			else
			{
				if (Input.GetKeyDown(KeyCode.Space))
				{
					SceneManager.LoadScene(gameObject.scene.path, LoadSceneMode.Single);
				}
			}
		}
	}
}
