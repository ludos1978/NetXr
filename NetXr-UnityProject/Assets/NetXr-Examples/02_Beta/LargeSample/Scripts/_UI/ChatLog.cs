//============= Copyright (c) Reto Spoerri, All rights reserved. ==============
//
// Purpose: 
//
//=============================================================================

using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class ChatLog : MonoBehaviour {
    public int maxEntries = 50;
    private int maxDisplay = 10;
    public List<string> chatHistory = new List<string>();
    private Text chatText;

    void Awake () {
        //chatHistory = new List<string>();
        chatText = GetComponent<Text>();
    }

    public void Add (string message, string sender) {
        string chatEntry = "<color=grey>" + sender + "</color> <color=black>:</color> <color=white>" + message + "</color>";
        chatHistory.Add(chatEntry);
        if (chatHistory.Count > maxEntries) {
            int count = chatHistory.Count - 50;
            Debug.Log("ChatLog.Add: removing " + count + "  old entries of " + chatHistory.Count);
            chatHistory.RemoveRange(0, count);
        }
        OnChange();
    }

    public void OnChange () {
        StringBuilder sb = new StringBuilder();
        for (int i = Mathf.Max(chatHistory.Count - maxDisplay, 0); i < chatHistory.Count; i++) {
            sb.Append(chatHistory[i]);
            sb.Append("\n");
        }
        if (chatText != null) { 
            chatText.text = sb.ToString();
        }
    }
}
