using UnityEngine;
using System.Collections;

public class RoteObj : MonoBehaviour {

    public float m_speed = 5F;
	
	// Update is called once per frame
	void Update () {
        this.transform.Rotate(Vector3.up, m_speed * Time.deltaTime);
	}
}
