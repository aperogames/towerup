using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DashButtonHandler : MonoBehaviour {
	private Player_Spire m_playerCtrl;

	private void Awake() {
		SceneManager.sceneLoaded += OnSceneLoaded;
	}

	void OnSceneLoaded( Scene _current, LoadSceneMode _mode ) {
		if ( _current.name == "SpireUp" ) {
			m_playerCtrl = GameObject.FindGameObjectWithTag( "Player" ).GetComponent<Player_Spire>();
		}
	}

	public void OnClick() {
		m_playerCtrl.Dash( 10.0f );
	}
}
