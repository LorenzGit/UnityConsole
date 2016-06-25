using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class GameConsole : MonoBehaviour {
    private const int NumberOfLines = 26;
    private const int MaxCharsPerLine = 85;

    private readonly List<Text> _textGameObjects = new List<Text>();
    private readonly List<string> _textStrings = new List<string>();

    // Use this for initialization
    void Start () {
	    GameObject textObject = transform.Find("ScrollView/Viewport/Content/Text").gameObject;
        _textGameObjects.Add( textObject.GetComponent<Text>() );
	    for (int i = 1; i < NumberOfLines; i++) {
	        GameObject textObjectClone = Instantiate(textObject);
            textObjectClone.transform.SetParent(textObject.transform.parent);
            textObjectClone.transform.localPosition = new Vector3(5f, -20f*i, 0f);
            _textGameObjects.Add( textObjectClone.GetComponent<Text>() );
        }
	}

    private void Log(string s) {
        string[] lines = s.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
        for (int i = 0; i < _textStrings.Count; i++) {
            
        }
    }
	
	// Update is called once per frame
	void Update () {
	
	}

    public void OnSendButtonClicked() {
        Debug.Log(this);
    }

    public void OnCloseButtonClicked() {
        this.gameObject.SetActive(false);
    }
}
