//============= Copyright (c) Reto Spoerri, All rights reserved. ==============
//
// Purpose: 
//
//=============================================================================

using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;

public class VrFramerate : MonoBehaviour {
    Texture2D frameTex;
    Sprite frameSprite;
    public Image frameImage;

    List<float> frameHistory;
    int length = 256;

	// Use this for initialization
	void Start () {
        frameTex = new Texture2D(length, 1);
        frameSprite = Sprite.Create(frameTex, new Rect(Vector2.zero, new Vector2(length, 1)), Vector2.zero);
        frameImage.sprite = frameSprite;

        frameHistory = new List<float>();
        for (int i=0; i< length; i++) {
            frameHistory.Add(0);
        }
    }
	
	// Update is called once per frame
	void Update () {
        frameHistory.Add(Time.deltaTime);
        frameHistory.RemoveAt(0);
        for (int x = 0; x < frameTex.width; x++) {
            //float v = (float)x / 64f;
            //float c = (Mathf.Sin(v*2*Mathf.PI+Time.time)+1)/2f;
            // Color col = new Color(c,c,c,1);
            float dt = frameHistory[x];
            //Color col = Color.green;
            float v = (dt - (0.01f)) / (0.01f);
            Color col = Color.Lerp(Color.green, Color.red, v);
            /*if (dt > 0.01) {
                col = Color.yellow;
            }*/
            frameTex.SetPixel(x, 0, col);
        }
        frameTex.Apply();
    }
}
