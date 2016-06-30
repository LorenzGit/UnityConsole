# UnityConsole
Nice efficient console for Unity.
Assumes that you have an orthographic camera with size at 300.

# Usage
```c#
public class Test : MonoBehaviour {
  public GameConsole gameConsole;

	// Use this for initialization
	void Start () {
        gameConsole.Initialize();

        for( var i = 0; i < 20; i++ ) {
            Debug.Log( i );
        }
        Debug.LogWarning( "Warning" );
        Debug.LogError( "Error" );

        GameConsole.AddCallback( "divide", ( Action<string, string> )Divide2Numbers, "Divide 2 numbers, usage: divide 2 4" );
    }

    private void Divide2Numbers( string x, string y ) {
        Debug.Log( "Division result: " + ( float.Parse( x ) / float.Parse( y ) ) );
    }

    // Update is called once per frame
    void Update () {
        //Open when press "~"
        if (Input.GetKeyUp(KeyCode.BackQuote)) {
            GameConsole.OpenConsole();
        }
	}
}
```
