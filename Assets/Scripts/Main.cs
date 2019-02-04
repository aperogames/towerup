using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Main : MonoBehaviour {
	public GameObject m_scoreParticle_1 = null;
	public GameObject m_scoreParticle_5 = null;
	public GameObject m_scoreParticle_10 = null;
	public GameObject m_scoreParticle_15 = null;
	public GameObject m_scoreParticle_25 = null;
	public bool m_restartGameAfterDeath = true;
	public float m_eventTextTimeOnScreen = 0.25f;
	//	UnityEngine.UI.Text m_scoreText = null;
	UnityEngine.UI.Text m_currentScoreText = null;
	UnityEngine.UI.Text m_currentLevelText = null;
	UnityEngine.UI.Text m_eventText = null;
	bool m_uiLoaded = false;
//	bool m_scoreLoaded = false;
	bool m_gameLoaded = false;

	int m_currentLevel = 1;
	int m_currentScore = 0;

	int m_lastSeed = -1;
	float m_eventTextRemainingTime = 0.0f;

	// Use this for initialization
	void Awake () {
		Application.targetFrameRate = 60;
		SceneManager.LoadScene( "UI", LoadSceneMode.Additive );
//		SceneManager.LoadScene( "Score", LoadSceneMode.Additive );

		SceneManager.sceneLoaded += OnSceneLoaded;
		SceneManager.sceneUnloaded += OnSceneUnloaded;
	}

	void OnSceneUnloaded( Scene _current ) {
		if ( _current.name == "SpireUp" ) {
			m_gameLoaded = false;
			SceneManager.SetActiveScene( SceneManager.GetSceneAt( 0 ) );
			SceneManager.LoadSceneAsync( "SpireUp", LoadSceneMode.Additive );
		}
	}

	void OnSceneLoaded( Scene _current, LoadSceneMode _mode ) {
		if ( _current.name == "UI" ) {
			GameObject scoreText = GameObject.Find( "CurrentScore" );
			m_currentScoreText = scoreText.GetComponent<UnityEngine.UI.Text>();
			m_currentScoreText.text = "0";
			GameObject levelText = GameObject.Find( "CurrentLevel" );
			m_currentLevelText = levelText.GetComponent<UnityEngine.UI.Text>();
			m_currentLevelText.text = "LEVEL 1";
			GameObject eventText = GameObject.Find( "Event_Text" );
			m_eventText = eventText.GetComponent<UnityEngine.UI.Text>();
			m_eventText.text = "";

			m_uiLoaded = true;
		/*} else if ( _current.name == "Score" ) {
			GameObject scoreText = GameObject.Find( "ScoreText" );
			m_scoreText = scoreText.GetComponent<UnityEngine.UI.Text>();
			m_scoreLoaded = true;*/
		} else if ( _current.name == "SpireUp" ) {
			m_gameLoaded = true;
			SceneManager.SetActiveScene( _current );
			if ( m_lastSeed != -1 ) {
				GameObject.Find( "TheGame" ).GetComponent<GameProperty_Spire>().m_seed = m_lastSeed;
			} else {
				GameObject.Find( "TheGame" ).GetComponent<GameProperty_Spire>().m_seed = ( int )System.DateTime.Now.Ticks;
			}
		}

		if ( m_uiLoaded && /*m_scoreLoaded &&*/ !m_gameLoaded ) {
			NewGame();
		}
	}

	// Update is called once per frame
	void Update () {
		if ( m_eventTextRemainingTime > 0.0f ) {
			m_eventTextRemainingTime -= Time.deltaTime;
		} else {
			m_eventText.text = "";
		}
	}

	public void AddScore( Transform _parent, int _score ) {
		/*if ( m_scoreText != null ) {
			m_scoreText.text = "+" + _score + " !!";
			if ( m_scoreParticles != null ) {
				GameObject.Instantiate( m_scoreParticles, _parent );
			}*/
		if ( _score == 1 && m_scoreParticle_1 != null ) {
			GameObject.Instantiate( m_scoreParticle_1, _parent );
		} else if ( _score == 5 && m_scoreParticle_5 != null ) {
			GameObject.Instantiate( m_scoreParticle_5, _parent );
		} else if ( _score == 10 && m_scoreParticle_10 != null ) {
			GameObject.Instantiate( m_scoreParticle_10, _parent );
		} else if ( _score == 15 && m_scoreParticle_15 != null ) {
			GameObject.Instantiate( m_scoreParticle_15, _parent );
		} else if ( _score == 25 && m_scoreParticle_25 != null ) {
			GameObject.Instantiate( m_scoreParticle_25, _parent );
		}

		m_currentScore += _score;
		if ( m_currentScoreText != null ) {
			m_currentScoreText.text = m_currentScore.ToString();
		}
	}

	public void ShowEventText( string _text ) {
		if ( m_eventTextRemainingTime <= 0.0f + Mathf.Epsilon ) {
			m_eventText.text = _text;
			m_eventTextRemainingTime = m_eventTextTimeOnScreen;
		}
	}

	public void NewGame( bool _fromDeath = false ) {
		if ( m_gameLoaded ) {
			if ( !_fromDeath ) {
				m_currentLevel++;
				if ( m_currentLevelText != null ) {
					m_currentLevelText.text = "LEVEL " + m_currentLevel;
				}
				m_lastSeed = -1;
			} else {
				if ( m_restartGameAfterDeath ) {
					m_lastSeed = -1;
					m_currentScore = 0;
					if ( m_currentScoreText != null ) {
						m_currentScoreText.text = m_currentScore.ToString();
					}
					m_currentLevel = 1;
					if ( m_currentLevelText != null ) {
						m_currentLevelText.text = "LEVEL " + m_currentLevel;
					}
				} else {
					m_lastSeed = GameObject.Find( "TheGame" ).GetComponent<GameProperty_Spire>().m_seed;
				}
			}
			SceneManager.UnloadSceneAsync( "SpireUp" );
			Resources.UnloadUnusedAssets();
		} else {
			SceneManager.LoadSceneAsync( "SpireUp", LoadSceneMode.Additive );
		}
	}
}
