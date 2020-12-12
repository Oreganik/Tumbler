// TUMBLER
// Copyright (c) 2020 Ted Brown

using Jambox;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tumbler
{
	public class TitleCard : MonoBehaviour 
	{
		protected void Update ()
		{
			if (Time.timeSinceLevelLoad > 2) Destroy(gameObject);
		}
	}
}
