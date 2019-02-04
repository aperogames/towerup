using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gold : MonoBehaviour {
	public float m_rotationSpeed = 10.0f;

	// Start is called before the first frame update
	void Start() {

	}

	// Update is called once per frame
	void Update() {
		transform.Rotate( 0.0f, m_rotationSpeed * Time.deltaTime, 0.0f );
	}

	private void OnTriggerEnter( Collider _other ) {
		if ( _other.tag == "Player" ) {
			_other.GetComponent<Player_Spire>().AddCoin();
			Destroy( gameObject );
		}
	}
}
