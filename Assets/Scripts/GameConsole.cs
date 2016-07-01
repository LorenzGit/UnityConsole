using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

public class GameConsole : MonoBehaviour {
    private static GameConsole _instance;

    private const int MaxNumberOfLines = 14; //Number of visible lines on the screen
    private const float CharacterWidth = 40f; //Amount of characters per line
    private const float LineHieght = 40f; //Line height

    private Color[] _colors = new Color[2]{Color.white,Color.cyan}; //Color swapping for easier line reading
    private int _currentColor = 0; //Current line color

    //Struct to hold info about each line.
    private struct ConsoleString {
        public string text;
        public Color color;
        // Constructor:
        public ConsoleString( string text, Color color ) {
            this.text = text;
            this.color = color;
        }
    }

    private readonly List<Text> _textGameObjects = new List<Text>(); //Hold MaxNumberOfLines in Text
    private List<ConsoleString> _consoleStrings = new List<ConsoleString>(); //Holds all the strings ever logged

    private int _currentLine = 0; //Current line at the top
    private RectTransform _contentRectTranform; //Content holder
    private ScrollRect _scrollRect; //Scroller
    private InputField _inputTextField; //Input
    private Dictionary<string, Delegate> _callbacks = new Dictionary<string, Delegate>(); //All callback actions
    private Dictionary<string, string> _callbackDescriptions = new Dictionary<string, string>(); //Callbacks descriptions
    private bool _isInitialized;
    private RectTransform _rectTransform;
    private int _maxCharsPerLine;

    public void Initialize () {
        if (_isInitialized == false) {
            _isInitialized = true;
            _instance = this;

            //Initialize all the Text lines too pool
            GameObject textObject = transform.Find("ScrollView/Viewport/Content/Text").gameObject;
            _textGameObjects.Add(textObject.GetComponent<Text>());
            for (int i = 1; i < MaxNumberOfLines; i++) {
                GameObject textObjectClone = Instantiate(textObject);
                textObjectClone.transform.SetParent(textObject.transform.parent, false);
                textObjectClone.transform.localPosition = new Vector3(5f, -LineHieght*i, 0f);
                _textGameObjects.Add(textObjectClone.GetComponent<Text>());
            }

            _rectTransform = this.gameObject.GetComponent<RectTransform>();
            CalculateMaxCharsPerLine();
            _contentRectTranform = this.transform.Find("ScrollView/Viewport/Content").GetComponent<RectTransform>();
            _scrollRect = this.transform.Find("ScrollView").GetComponent<ScrollRect>();
            _scrollRect.onValueChanged.AddListener(delegate { UpdateScroller(); });
            _inputTextField = this.gameObject.transform.Find("InputField").GetComponent<InputField>();
            //_inputTextField.onEndEdit.AddListener(delegate { OnSendButtonClicked(); });

            this.gameObject.transform.Find("SendButton").GetComponent<Button>().onClick.AddListener( delegate { OnSendButtonClicked(); } );
            this.gameObject.transform.Find("CloseButton").GetComponent<Button>().onClick.AddListener( delegate { CloseConsole(); } );

            Application.logMessageReceived += HandleLog; //Everytime you write Debug.Log somewhere this handles it.

            AddCallback("help", (Action) ShowAllCallbacks, "Show all recorded commands"); //if you write help in the input it shows all the registered callbacks
            AddCallback("clear", (Action) ClearConsole, "Clears the console");
            AddCallback("multiply", (Action<int, int>) Multiply2Numbers, "Multiply 2 numbers, usage: multiply 2 4");

            Debug.Log("Write 'help' for a list of commands...");

            UpdateScroller();
        }
    }

    private void CalculateMaxCharsPerLine() {
        _instance._maxCharsPerLine = ( int )( CharacterWidth * ( _instance._rectTransform.sizeDelta.x / _instance._rectTransform.sizeDelta.y ) );
    }

    private void UpdateScroller() {
        float percentage = 1 - _scrollRect.verticalScrollbar.value; //For some reason Unity decided that it made sense to invert the value parameters ._.
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
            else if (type == LogType.Assert) { color = Color.green; }

            string s = logString;
            string[] lines = s.Split(new string[] {"\r\n", "\n"}, StringSplitOptions.None);
            for (int i = 0; i < lines.Length; i++) {
                string currentLine = lines[i];
                while (currentLine.Length > 0) {
                    if (currentLine.Length > _maxCharsPerLine) {
                        _consoleStrings.Add(new ConsoleString(currentLine.Substring(0, _maxCharsPerLine ), color ));
                        currentLine = currentLine.Remove(0, _maxCharsPerLine );
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
        if (_scrollRect.verticalScrollbar.value < 0.01f || _scrollRect.verticalScrollbar.value > 0.99f ) {
            float position = Mathf.Max(0f, _contentRectTranform.sizeDelta.y - this.GetComponent<RectTransform>().sizeDelta.y + 55f);
            _contentRectTranform.localPosition = new Vector3(_contentRectTranform.localPosition.x, position, 0f);
            UpdateScroller();
        }
    }

    private void OnSendButtonClicked() {
        if (_isInitialized) {
            if (_inputTextField.text != "") {
                string s = _inputTextField.text;
                _inputTextField.text = "";
                Log(s);
            }
            _inputTextField.Select();
            _inputTextField.ActivateInputField();
        }
    }

    private void Log(string s) {
        bool isCommand = false;
        string[] args = s.Split(new string[] {" "}, StringSplitOptions.None);
        foreach (KeyValuePair<string, Delegate> keyValue in _callbacks) {
            if (keyValue.Key == args[0] ) {
                isCommand = true;
                Debug.LogAssertion("COMMAND: "+s);

                object[] arr = new object[args.Length-1];
                for (int i = 1; i < args.Length; i++) {
                    arr[i - 1] = args[i]; //TODO: this only accepts signatures with strings, please extend it.
                }


                try
                {

                    //get signiture of the delegate
                    var methodSig = keyValue.Value.Method.GetParameters();

                    //build a parameter list to pass to the delegate
                    var paraList = new object[methodSig.Length];

                    for (int i = 0; i < paraList.Length; i++)
                    {
                        //get type for the parameter and cast it from our string
                        var type = methodSig[i].ParameterType;
                        paraList[i] = Convert.ChangeType(args[i + 1], type);
                    }

                    keyValue.Value.DynamicInvoke(paraList);
                }
                catch {
                    Debug.LogError("Parameters do not match method signature, make sure to only use strings!");
                }
            }
        }
        if ( isCommand == false) {
            Debug.LogError( "UNKNOWN COMMAND: "+ s );
        }
    }

    void Update() {
        if (Input.GetKeyDown(KeyCode.Return)) {
            if (this.gameObject.activeSelf) {
                OnSendButtonClicked();
            }
        }
    }

    // Public methods

    //-------
    public static void CloseConsole() {
        if (_instance != null) {
            _instance.gameObject.SetActive(false);
        }
    }

    public static void OpenConsole() {
        if ( _instance != null ) {
            if (_instance.gameObject.activeSelf == false) {
                _instance.CalculateMaxCharsPerLine();
                _instance.gameObject.SetActive( true );
                _instance._scrollRect.verticalScrollbar.value = 0f;
                _instance.SetContentPosition();
            }
        }
    }

    //Adds a callback that can hold many parameters, the signature of the method has to be either empty or contain all strings
    public static void AddCallback( string s, Delegate callBack, string description = "" ) {
        if ( _instance != null ) {
            _instance._callbacks[s] = callBack;
            _instance._callbackDescriptions[s] = description;
        }
    }


    public static void RemoveCallback( string s) {
        if ( _instance != null ) {
            _instance._callbacks.Remove(s);
            _instance._callbackDescriptions.Remove(s);
        }
    }
    //------

    //callbacks
    private void ShowAllCallbacks(){
        Debug.Log("Writing all commands recorded:");
        foreach (KeyValuePair<string, string> keyValue in _callbackDescriptions) {
            Debug.Log("'"+keyValue.Key+"'" + " - "+ keyValue.Value);
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
    
    private void Multiply2Numbers(int x, int y) {
        Debug.Log("Multiplication result: "+ (x * y)) ;
    }
}
