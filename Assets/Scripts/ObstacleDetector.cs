using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObstacleDetector : MonoBehaviour {
	private Player_Spire m_player;
	private SpireGenerator m_spire;

    // Start is called before the first frame update
    void Start() {
        m_player = GameObject.FindGameObjectWithTag( "Player" ).GetComponent<Player_Spire>();
		m_spire = GameObject.FindGameObjectWithTag( "Spire" ).GetComponent<SpireGenerator>();
	}

	// Update is called once per frame
	void Update() {
		Vector3 playerPos = m_player.gameObject.transform.position;

		Vector3 splinePos = m_spire.Spline.GetPointAtPct( m_player.PathRatio );
		splinePos.y += 1.0f;

		transform.position = splinePos;
    }
}
