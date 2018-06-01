//============= Copyright (c) Reto Spoerri, All rights reserved. ==============
//
// Purpose: 
//
//=============================================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NetXr {
	public class CanvasAboveParent : MonoBehaviour {
		// Update is called once per frame
		void Update () {
			transform.position = transform.parent.transform.position + new Vector3 (0, 0.05f, 0);
			transform.rotation = Quaternion.LookRotation (transform.position - PlayerSettingsController.Instance.headTransform.position);
		}
	}
}