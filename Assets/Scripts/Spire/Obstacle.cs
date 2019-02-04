using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Obstacle : MonoBehaviour {
	private Player_Spire m_player;

	public GameObject m_destructionFx = null;
	public bool m_canBeJumpedOn = true;

	void Awake() {
		m_player = GameObject.FindGameObjectWithTag( "Player" ).GetComponent<Player_Spire>();
	}

	private void Start() {
		enabled = false;
	}

	public void SetupPosition( float _pathPosition ) {
		float pct = 0.0f;
		SpireGenerator spire = GameObject.FindGameObjectWithTag( "Spire" ).GetComponent<SpireGenerator>();
		transform.position = spire.Spline.GetAtDistanceFrom( ref pct, _pathPosition );

		Vector3 tangent = spire.Spline.GetTangentAtPercentage( pct );
		float angle = Mathf.Acos( Vector3.Dot( tangent, new Vector3( 0.0f, 0.0f, 1.0f ) ) ) * Mathf.Rad2Deg;
		Vector3 cross = Vector3.Cross( tangent, new Vector3( 0.0f, 0.0f, 1.0f ) );
		transform.rotation = Quaternion.Euler( 0.0f, angle * ( cross.y < 0.0f ? 1.0f : -1.0f ), 0.0f );
	}

	private void OnTriggerEnter( Collider _other ) {
		if ( _other.gameObject == m_player.gameObject ) {
			bool canBeDestroyed = false;
			if ( m_canBeJumpedOn ) {
				canBeDestroyed = m_player.CurrentState == Player_Spire.State.CRASH || m_player.CurrentState == Player_Spire.State.BOOST;
			} else {
				canBeDestroyed = m_player.CurrentState == Player_Spire.State.DASH || m_player.CurrentState == Player_Spire.State.BOOST;
			}
			if ( canBeDestroyed ) {
				if ( m_destructionFx != null ) {
					Instantiate( m_destructionFx, transform.position, transform.rotation );
				}
				Destroy( gameObject );
				m_player.OnObjectDestruction();
			} else {
				if ( m_player.m_allowBounce ) {
					if ( m_player.CurrentState == Player_Spire.State.FALL && m_canBeJumpedOn ) {
						m_player.Bounce();
						enabled = false;
						return;
					}
				}
				m_player.Die();
			}
		}
	}

	private void OnTriggerExit( Collider _other ) {
		if ( _other != m_player.gameObject ) {
			m_player.PassedObstacle();
		}
	}
}
