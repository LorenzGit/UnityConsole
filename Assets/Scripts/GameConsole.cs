using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class GameConsole : MonoBehaviour {
    public static GameConsole instance;

    private const int MaxNumberOfLines = 14;
    private const int MaxCharsPerLine = 70;
    private const float LineHieght = 40f;

    private Color[] _colors = new Color[2]
    {
        Color.white,
        Color.cyan
    };
    private int _currentColor = 0;

    private struct ConsoleString {
        public string text;
        public Color color;
        // Constructor:
        public ConsoleString( string text, Color color ) {
            this.text = text;
            this.color = color;
        }
    }

    private readonly List<Text> _textGameObjects = new List<Text>();
    private List<ConsoleString> _consoleStrings = new List<ConsoleString>();

    private int _currentLine = 0;
    private RectTransform _contentRectTranform;
    private ScrollRect _scrollRect;
    private InputField _inputTextField;
    private Dictionary<string, Action> _callbacks = new Dictionary<string, Action>();
    private Dictionary<string, string> _callbackDescriptions = new Dictionary<string, string>();

    void Awake () {
        instance = this;
        GameObject textObject = transform.Find("ScrollView/Viewport/Content/Text").gameObject;
        _textGameObjects.Add( textObject.GetComponent<Text>() );
	    for (int i = 1; i < MaxNumberOfLines; i++) {
	        GameObject textObjectClone = Instantiate(textObject);
            textObjectClone.transform.SetParent(textObject.transform.parent, false);
            textObjectClone.transform.localPosition = new Vector3(5f, -LineHieght * i, 0f);
            _textGameObjects.Add( textObjectClone.GetComponent<Text>() );
        }

        _contentRectTranform = this.transform.Find( "ScrollView/Viewport/Content" ).GetComponent<RectTransform>();
        _scrollRect = this.transform.Find("ScrollView").GetComponent<ScrollRect>();
        _scrollRect.onValueChanged.AddListener( delegate { UpdateScroller(); } );
        _inputTextField = this.gameObject.transform.Find("InputField").GetComponent<InputField>();
        _inputTextField.onEndEdit.AddListener( delegate { OnSendButtonClicked(); } );

        Application.logMessageReceived += HandleLog;
        
        AddCallback( "help", ShowAllCallbacks, "Show all recorded commands" );
        AddCallback( "clear", ClearConsole, "Clears the console" );

        Log( "Write 'help' for a list of commands..." );

        UpdateScroller();
    }

    private void UpdateScroller() {
        float percentage = 1 - _scrollRect.verticalScrollbar.value;
        _currentLine = (int)Math.Max(0, Mathf.Ceil( percentage*(float)(_consoleStrings.Count- MaxNumberOfLines ) ));
        //Debug.Log( "GameConsoleDebug " + percentage+", "+ _currentLine );

        for (int i = 0; i < MaxNumberOfLines; i++) {
            if (_consoleStrings.Count > _currentLine + i) {
                _textGameObjects[i].transform.localPosition = new Vector3(5, -LineHieght*(_currentLine + i));
                _textGameObjects[i].text = _consoleStrings[_currentLine + i].text;
                _textGameObjects[i].color = _consoleStrings[_currentLine + i].color;
            }
        }
    }

    private void HandleLog( string logString, string stackTrace, LogType type ) {
        if (logString.StartsWith("GameConsoleDebug") == false) {
            _currentColor++;
            if (_currentColor >= _colors.Length) { _currentColor = 0;}
            Color color = _colors[_currentColor];
            if (type == LogType.Warning) { color = Color.yellow; }
            else if (type == LogType.Error) { color = Color.red; }

            string s = logString;
            string[] lines = s.Split(new string[] {"\r\n", "\n"}, StringSplitOptions.None);
            for (int i = 0; i < lines.Length; i++) {
                string currentLine = lines[i];
                while (currentLine.Length > 0) {
                    if (currentLine.Length > MaxCharsPerLine) {
                        _consoleStrings.Add(new ConsoleString(currentLine.Substring(0, MaxCharsPerLine), color ));
                        currentLine = currentLine.Remove(0, MaxCharsPerLine);
                    }
                    else {
                        _consoleStrings.Add(new ConsoleString(currentLine, color ));
                        break;
                    }
                }
            }

            SetContentPosition();
        }
    }

    private void SetContentPosition() {
        _contentRectTranform.sizeDelta = new Vector2( _contentRectTranform.sizeDelta.x, LineHieght * _consoleStrings.Count );
        float position = Mathf.Max(0f, _contentRectTranform.sizeDelta.y - this.GetComponent<RectTransform>().sizeDelta.y + 55f);
        _contentRectTranform.localPosition = new Vector3( _contentRectTranform.localPosition.x, position, 0f );
        UpdateScroller();
    }

    public void OnSendButtonClicked() {
        if (_inputTextField.text != "") {
            string s = _inputTextField.text;
            _inputTextField.text = "";
            Log(s);
        }
        _inputTextField.Select();
        _inputTextField.ActivateInputField();
    }

    public void Log(string s) {
        Debug.Log( s );
        foreach (KeyValuePair<string, Action> keyValue in _callbacks) {
            if (keyValue.Key == s) {
                keyValue.Value();
            }
        }
    }

    public void OnCloseButtonClicked() {
        this.gameObject.SetActive(false);
    }

    public void OpenConsole() {
        if (this.gameObject.activeSelf == false) {
            this.gameObject.SetActive(true);
        }
    }

    public void AddCallback( string s, Action callBack, string description = "" ) {			
		_callbacks[s] = callBack;
        _callbackDescriptions[s] = description;
	}

    public void RemoveCallback( string s) {			
	    _callbacks.Remove(s);
        _callbackDescriptions.Remove( s );
    }

    //callbacks
    private void ShowAllCallbacks(){
        Log("Writing all callbacks recorded:");
        foreach (KeyValuePair<string, string> keyValue in _callbackDescriptions) {
            Log(keyValue.Key + " - "+keyValue.Value);
        }
	}

    private void ClearConsole() {
        _currentLine = 0;
        _consoleStrings = new List<ConsoleString>();
        for( int i = 0; i < MaxNumberOfLines; i++ ) {
            _textGameObjects[i].transform.localPosition = new Vector3( 5, -LineHieght * ( _currentLine + i ) );
            _textGameObjects[i].text = "";
        }
        SetContentPosition();
    }
}
