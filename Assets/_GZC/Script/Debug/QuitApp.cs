using UnityEngine;
using System.Collections;

public class QuitApp : MonoBehaviour {
	
	void Update () {
        if (Input.GetKeyDown(KeyCode.Home) || Input.GetKeyDown(KeyCode.Escape)) {
            Application.Quit( );
        }
	}

}
