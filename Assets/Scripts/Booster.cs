using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Booster : MonoBehaviour {
	public float PathPosition { set; get; } = 0.0f;
	private float m_pathPct = 0.0f;
	public float m_boostDistance = 15.0f;
	private SpireGenerator m_spire = null;
	private Player_Spire m_player = null;

	private void Awake() {
		m_spire = GameObject.FindGameObjectWithTag( "Spire" ).GetComponent<SpireGenerator>();
		m_player = GameObject.FindGameObjectWithTag( "Player" ).GetComponent<Player_Spire>();
	}

	public void SetupPosition() {
		m_pathPct = 0.0f;
		Vector3 position = m_spire.Spline.GetAtDistanceFrom( ref m_pathPct, PathPosition );
		transform.position = position;
		transform.position += Vector3.up * 2.0f;

		Vector3 tangent = m_spire.Spline.GetTangentAtPercentage( m_pathPct );
		float angle = Mathf.Acos( Vector3.Dot( tangent, new Vector3( 0.0f, 0.0f, 1.0f ) ) ) * Mathf.Rad2Deg;
		Vector3 cross = Vector3.Cross( tangent, new Vector3( 0.0f, 0.0f, 1.0f ) );
		transform.rotation = Quaternion.Euler( 90.0f, angle * ( cross.y < 0.0f ? 1.0f : -1.0f ), 0.0f );
	}

	// Start is called before the first frame update
	void Start() {
		SetupPosition();
	}

    // Update is called once per frame
    void Update() {
        if ( m_player.PathRatio > m_pathPct ) {
        	m_player.Boost( m_boostDistance );
			Destroy( this );
		}
	}
}
