using UnityEngine;
using System.Collections;

public class Test : MonoBehaviour {

	// Use this for initialization
	void Start () {
        for( var i = 0; i < 50; i++ ) {
            Debug.Log( i );
        }
        Debug.LogWarning( "Warning" );
        Debug.LogError( "Error" );
    }
	
	// Update is called once per frame
	void Update () {
	    /*if (Time.time < 5) {
	        Debug.Log(Time.time);
	    }
	    else {
	        GameConsole.instance.OpenConsole();
	    }*/
	}
}
