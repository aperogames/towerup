using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JumpScale : MonoBehaviour {
	public float m_maxScaleYAddition = 1.0f;
	public float m_widthScaleRatio = 0.5f;
	public float m_velForMaxScale = 10.0f;
	public float m_k = 1.0f;
	public float m_b = 1.0f;

	private float m_currentYScale = 1.0f;
	private float m_velocity = 0.0f;
	private Player_Spire m_playerCtrl;

	// Use this for initialization
	void Start() {
		m_playerCtrl = GetComponent<Player_Spire>();
	}

	// Update is called once per frame
	void Update() {
		// Spring
		float deltaScale = ( m_playerCtrl.JumpVelocity > 0.0f ? ( 1.0f + ( m_maxScaleYAddition * m_playerCtrl.JumpVelocity / m_velForMaxScale ) ) : 1.0f ) - m_currentYScale;
		float force = m_k * deltaScale - m_b * m_velocity;
		m_velocity += force * Time.deltaTime;
		m_currentYScale += m_velocity * Time.deltaTime;

		float widthScale = 1.0f + ( 1.0f - m_currentYScale ) * m_widthScaleRatio;
		transform.localScale = new Vector3( widthScale, m_currentYScale, widthScale );
	}
}
