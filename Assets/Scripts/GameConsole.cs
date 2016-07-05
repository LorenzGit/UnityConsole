using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

public class GameConsole : MonoBehaviour
{
	#region Constants

	/// <summary>
	/// Amount of visible lines on the screen
	/// </summary>
	private const int MaxNumberOfLines = 14;

	/// <summary>
	/// Amount of characters per line
	/// </summary>
	private const float CharacterWidth = 40f;

	/// <summary>
	/// Height of each line
	/// </summary>
	private const float LineHeight = 40f;

	#endregion


	#region Singleton

	/// <summary>
	/// Singleton instance for the MonoBehavior
	/// </summary>
	private static GameConsole _instance;

	#endregion


	#region Structs

	/// <summary>
	/// Struct that holds the info of each line
	/// </summary>
	private struct ConsoleString
	{
		/// <summary>
		/// Text on the line
		/// </summary>
		public string text;

		/// <summary>
		/// Color of the line
		/// </summary>
		public Color color;

        
		/// <summary>
		/// Initializes a new instance of the <see cref="GameConsole+ConsoleString"/> struct.
		/// </summary>
		/// <param name="text">Text.</param>
		/// <param name="color">Color.</param>
		public ConsoleString (string text, Color color)
		{
			this.text = text;
			this.color = color;
		}
	}


	/// <summary>
	/// Struct that holds the callback and description for a console command
	/// </summary>
	private struct ConsoleCallback
	{
		/// <summary>
		/// Action to execute
		/// </summary>
		public Delegate callback;

		/// <summary>
		/// Description of the command
		/// </summary>
		public string description;
	}

	#endregion


	#region Fields

	/// <summary>
	/// Holds MaxNumberOfLines in Text.
	/// </summary>
	private readonly List<Text> _textGameObjects = new List<Text> ();

	/// <summary>
	/// List of colors to alternate through for easier line reading
	/// </summary>
	private Color[] _colors = new Color[2]{ Color.white, Color.cyan };

	/// <summary>
	/// Current index into the colors array
	/// </summary>
	private int _currentColor = 0;

	//Current line color
	/// <summary>
	/// Holds list of all strings ever logged
	/// </summary>
	private List<ConsoleString> _consoleStrings = new List<ConsoleString> ();

	/// <summary>
	/// Index of the current line at the top
	/// </summary>
	private int _currentLine = 0;

	/// <summary>
	/// Transform that holds the content
	/// </summary>
	private RectTransform _contentRectTranform;

	/// <summary>
	/// Scrolling instance
	/// </summary>
	private ScrollRect _scrollRect;

	/// <summary>
	/// Input text instance
	/// </summary>
	private InputField _inputTextField;

	/// <summary>
	/// Dictionary that holds all callbacks
	/// </summary>
	private Dictionary<string, ConsoleCallback> _callbacks = new Dictionary<string, ConsoleCallback> ();

	/// <summary>
	/// True if we have initialized the console
	/// </summary>
	private bool _isInitialized;

	/// <summary>
	/// RectTransform of the console
	/// </summary>
	private RectTransform _rectTransform;

	/// <summary>
	/// Calculated maximum number of characters per line
	/// </summary>
	private int _maxCharsPerLine;

	#endregion


	#region Public methods

	/// <summary>
	/// Opens the console
	/// </summary>
	public static void OpenConsole ()
	{
		if (_instance != null && _instance.gameObject.activeSelf == false) {
			_instance.CalculateMaxCharsPerLine ();
			_instance.gameObject.SetActive (true);
			_instance._scrollRect.verticalScrollbar.value = 0f;
			_instance.SetContentPosition ();
		}
	}


	/// <summary>
	/// Closes the console
	/// </summary>
	public static void CloseConsole ()
	{
		if (_instance != null) {
			_instance.gameObject.SetActive (false);
		}
	}


	/// <summary>
	/// Adds a callback for the console. Callback method signature can either be empty or contain all strings
	/// </summary>
	/// <param name="s">Name of the console command.</param>
	/// <param name="callBack">Callback.</param>
	/// <param name="description">Description.</param>
	public static void AddCallback (string s, Delegate callback, string description = "")
	{
		if (_instance != null) {
			_instance._callbacks [s] = new ConsoleCallback {
				callback = callback,
				description = description
			};
		}
	}

	/// <summary>
	/// Removes a callback
	/// </summary>
	/// <param name="s">S.</param>
	public static void RemoveCallback (string s)
	{
		if (_instance != null) {
			_instance._callbacks.Remove (s);
		}
	}

	#endregion

	#region Initialization

	/// <summary>
	/// Initializes the GameObject
	/// </summary>
	public void Initialize ()
	{
		if (_isInitialized == false) {
			_isInitialized = true;
			_instance = this;

			InitializeTextObjects ();

			_rectTransform = this.gameObject.GetComponent<RectTransform> ();
			CalculateMaxCharsPerLine ();

			_contentRectTranform = this.transform.Find ("ScrollView/Viewport/Content").GetComponent<RectTransform> ();

			_scrollRect = this.transform.Find ("ScrollView").GetComponent<ScrollRect> ();
			_scrollRect.onValueChanged.AddListener (delegate {
				UpdateScroller ();
			});

			_inputTextField = this.gameObject.transform.Find ("InputField").GetComponent<InputField> ();

			InitButtons ();

			Application.logMessageReceived += HandleLog; //Everytime you write Debug.Log somewhere this handles it.
			AddCallbacks ();
			Debug.Log ("Write 'help' for a list of commands...");

			UpdateScroller ();
		}
	}


	/// <summary>
	/// Initialize all the Text GameObjects to pool
	/// </summary>
	private void InitializeTextObjects ()
	{
		// Get the text GameObject from the chain
		GameObject textObject = transform.Find ("ScrollView/Viewport/Content/Text").gameObject;

		// Add it to our list
		_textGameObjects.Add (textObject.GetComponent<Text> ());

		// Clone the prefab and add the remaining lines
		for (int i = 1; i < MaxNumberOfLines; i++) {
			GameObject textObjectClone = Instantiate (textObject);
			textObjectClone.transform.SetParent (textObject.transform.parent, false);
			textObjectClone.transform.localPosition = new Vector3 (5f, -LineHeight * i, 0f);

			_textGameObjects.Add (textObjectClone.GetComponent<Text> ());
		}
	}


	/// <summary>
	/// Add event handlers for the buttons
	/// </summary>
	private void InitButtons ()
	{
		this.gameObject.transform.Find ("SendButton").GetComponent<Button> ().onClick.AddListener (delegate {
			OnSendButtonClicked ();
		});

		this.gameObject.transform.Find ("CloseButton").GetComponent<Button> ().onClick.AddListener (delegate {
			CloseConsole ();
		});

	}

	/// <summary>
	/// Adds all callbacks for the console
	/// </summary>
	private void AddCallbacks ()
	{
		AddCallback ("help", (Action)ShowAllCallbacks, "Show all recorded commands"); //if you write help in the input it shows all the registered callbacks
		AddCallback ("clear", (Action)ClearConsole, "Clears the console");
	}

	#endregion


	#region Behavior

	/// <summary>
	/// Calculaetes maximum characters per line
	/// </summary>
	private void CalculateMaxCharsPerLine ()
	{
		_instance._maxCharsPerLine = (int)(CharacterWidth * (_instance._rectTransform.sizeDelta.x / _instance._rectTransform.sizeDelta.y));
	}


	/// <summary>
	/// Updates the text based on where the console is scrolled
	/// </summary>
	private void UpdateScroller ()
	{
		float percentage = 1 - _scrollRect.verticalScrollbar.value; //For some reason Unity decided that it made sense to invert the value parameters ._.

		_currentLine = (int)Math.Max (0, Mathf.Ceil (percentage * (float)(_consoleStrings.Count - MaxNumberOfLines)));

		for (int i = 0; i < MaxNumberOfLines; i++) {
			if (_consoleStrings.Count > _currentLine + i) {
				_textGameObjects [i].transform.localPosition = new Vector3 (5, -LineHeight * (_currentLine + i));
				_textGameObjects [i].text = _consoleStrings [_currentLine + i].text;
				_textGameObjects [i].color = _consoleStrings [_currentLine + i].color;
			}
		}
	}


	/// <summary>
	/// Callback that handles logging from Debug.Log
	/// </summary>
	/// <param name="logString">Log string.</param>
	/// <param name="stackTrace">Stack trace.</param>
	/// <param name="type">Type.</param>
	private void HandleLog (string logString, string stackTrace, LogType type)
	{
		// Ignore local Debug.Logs
		if (logString.StartsWith ("GameConsoleDebug"))
			return;
		
		if (++_currentColor >= _colors.Length) {
			_currentColor = 0;
		}

		// Get color for current LogLevel
		Color color = _colors [_currentColor];
		if (type == LogType.Warning) {
			color = Color.yellow;
		} else if (type == LogType.Error) {
			color = Color.red;
		} else if (type == LogType.Assert) {
			color = Color.green;
		}

		string[] lines = logString.Split (new string[] { "\r\n", "\n" }, StringSplitOptions.None);

		// Add every line to the console
		for (int i = 0; i < lines.Length; ++i) {
			string currentLine = lines [i];

			// Split the line if it's too long
			while (currentLine.Length > 0) {
				if (currentLine.Length > _maxCharsPerLine) {
					_consoleStrings.Add (new ConsoleString (currentLine.Substring (0, _maxCharsPerLine), color));
					currentLine = currentLine.Remove (0, _maxCharsPerLine);
				} else {
					_consoleStrings.Add (new ConsoleString (currentLine, color));
					break;
				}
			}
		}
        
		SetContentPosition ();
	}

	/// <summary>
	/// Updates the position of the content after the console is updated
	/// </summary>
	private void SetContentPosition ()
	{
		// todo: this could use more explanation
		_contentRectTranform.sizeDelta = new Vector2 (_contentRectTranform.sizeDelta.x, LineHeight * _consoleStrings.Count);

		if (_scrollRect.verticalScrollbar.value < 0.01f || _scrollRect.verticalScrollbar.value > 0.99f) {
			float position = Mathf.Max (0f, _contentRectTranform.sizeDelta.y - this.GetComponent<RectTransform> ().sizeDelta.y + 55f);
			_contentRectTranform.localPosition = new Vector3 (_contentRectTranform.localPosition.x, position, 0f);
			UpdateScroller ();
		}
	}

	/// <summary>
	/// Parses the command in the console when the user clicks the send button
	/// </summary>
	private void OnSendButtonClicked ()
	{
		if (!_isInitialized)
			return;
		
		if (_inputTextField.text != "") {
			// Log the command
			string s = _inputTextField.text;
			_inputTextField.text = "";
			Parse (s);
		}

		_inputTextField.Select ();
		_inputTextField.ActivateInputField ();
	}


	/// <summary>
	/// Parses the console command from the user
	/// </summary>
	/// <param name="s">Text from the user.</param>
	private void Parse (string s)
	{
		bool isCommand = false;

		// split the args
		string[] args = Regex.Split (s, "\\s+");

		foreach (KeyValuePair<string, ConsoleCallback> keyValue in _callbacks) {
			if (keyValue.Key == args [0]) {
				isCommand = true;
				Debug.LogAssertion ("COMMAND: " + s);

				// remove the first element from the args
				string[] arr = new string[args.Length - 1];
				Array.Copy (args, 1, arr, 0, arr.Length);

				// try to call the callback
				try {
					keyValue.Value.callback.DynamicInvoke (arr);
				} catch {
					Debug.LogError ("Parameters do not match method signature, make sure to only use strings!");
				}

				break;
			}
		}

		if (isCommand == false) {
			Debug.LogError ("UNKNOWN COMMAND: " + s);
		}
	}


	/// <summary>
	/// Update method. Right now only detects "enter" key.
	/// </summary>
	private void Update ()
	{
		if (Input.GetKeyDown (KeyCode.Return) && this.gameObject.activeSelf) {
			OnSendButtonClicked ();
		}
	}

	#endregion

	#region Callbacks

	/// <summary>
	/// Shows all callbacks. Called from the "help" command.
	/// </summary>
	private void ShowAllCallbacks ()
	{
		Debug.Log ("Writing all commands recorded:");

		foreach (KeyValuePair<string, ConsoleCallback> keyValue in _callbacks) {
			Debug.Log ("'" + keyValue.Key + "'" + " - " + keyValue.Value.description);
		}
	}

	/// <summary>
	/// Clears the console.
	/// </summary>
	private void ClearConsole ()
	{
		_currentLine = 0;
		_consoleStrings = new List<ConsoleString> ();

		for (int i = 0; i < MaxNumberOfLines; i++) {
			_textGameObjects [i].transform.localPosition = new Vector3 (5, -LineHeight * (_currentLine + i));
			_textGameObjects [i].text = "";
		}

		SetContentPosition ();
	}

	#endregion
}
