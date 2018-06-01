//============= Copyright (c) Reto Spoerri, All rights reserved. ==============
//
// Purpose: 
//
//=============================================================================

using UnityEngine;
using System;

public class Screencapture : MonoBehaviour {

	public KeyCode screenshotButton = KeyCode.Print;
	public int superSize = 4;
	
	// Update is called once per frame
	void Update () {
		if (Input.GetKeyDown(screenshotButton)) {
			string filename = "Screenshot-"+DateTime.Now.ToString("yyyy-MM-dd-hh-mm-ss-ff")+".png";
			ScreenCapture.CaptureScreenshot(filename, superSize);
			Debug.Log("Taken Screenshot and Saved as "+filename);
		}
	}
}
