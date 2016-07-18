using System;
using UnityEngine;

public class Test : MonoBehaviour
{
    public GameConsole gameConsole;

    // Use this for initialization
    void Start()
    {
        gameConsole.Initialize();

        for (var i = 0; i < 20; i++)
        {
            Debug.Log(i);
        }
        Debug.LogWarning("Warning");
        Debug.LogError("Error");

        GameConsole.AddCallback("divide", (Action<float, float>)Divide2Numbers, "Divide 2 floats, usage: divide 2 4");
        GameConsole.AddCallback("multiply", (Action<float, float>)Multiply2Numbers, "Multiply 2 floats, usage: divide 2 4");
    }

    private void Divide2Numbers(float x, float y)
    {
        Debug.Log("Division result: " + (x / y));
    }

    private void Multiply2Numbers(float x, float y)
    {
        Debug.Log("Multiply result: " + (x * y));
    }

    // Update is called once per frame
    void Update()
    {
        //Open when press "~"
        if (Input.GetKeyUp(KeyCode.BackQuote))
        {
            GameConsole.OpenConsole();
        }
    }
}
