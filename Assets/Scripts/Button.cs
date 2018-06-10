using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Button : MonoBehaviour {

	void OnMouseUpAsButton()
    {
        switch (gameObject.name)
        {
            case "go":
                Application.LoadLevel("GameScene");
                Debug.Log("gamescene");
                break;

           	case "howto":
                Application.LoadLevel("HowTo");
                Debug.Log("how");
                break;

            case "back":
                Application.LoadLevel("MenuScene");
                Debug.Log("menu");
                break;
        }
    }
}
