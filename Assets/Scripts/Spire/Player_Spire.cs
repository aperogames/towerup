using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TapticPlugin;
using UnityEngine.SceneManagement;

public class Player_Spire : MonoBehaviour {
	private SpireGenerator m_spire;
	private GameProperty_Spire m_gameProp;
	private Main m_main;
	private PlayerBoostVisuals m_boostVisuals;
	public bool m_realDistance = false;
	public float m_speed = 20.0f; // speed of the player on spline
	public float m_boostSpeed = 100.0f;
	public float m_initialAcceleration = 15.0f;
	public float m_jumpImpulse = 20.0f;
	public int m_jumpAnticipationFrameCount = 4;
	public bool m_allowBounce = true;
	public bool m_allowSwipe = true;
	public float m_swipeThreshold = 10.0f;
	public float m_bounceImpulse = 10.0f;
	public float m_bounceAllowedAngle = 45.0f;
	public float m_jumpButtonPressedGravityMultiplier = 1.5f;
	public float m_bounceGravityMultiplier = 1.0f;
	public float m_fallGravityMultiplier = 2.5f;
	public float m_lowJumpMultiplier = 2.0f;
	public float m_forceFallMultiplier = 10.0f;
	public float m_stopMoveAfterForceFallTimer = 0.2f;
	public float m_stopMoveAfterObstacleDestruction = 0.2f;
	public GameObject m_deathFx = null;
	public CameraShakeEffect m_deathShake;
	public CameraShakeEffect m_forceFallShake;
	public CameraShakeEffect m_obstacleDestructionShake;
	private float m_currentSpeed = 0.0f;
	private float m_deadTimer = 0.0f;
	private float m_stopTimer = 0.0f;
	private float m_boostDistanceLeft = 0.0f;
	private int m_lastJustPressedFrame = -9999;

	private Vector2 m_swipeBegin;

	public float JumpVelocity { get; private set; } = 0.0f;

	public float CurrentHeight { get; private set; } = 0.0f;

	public float PathPosition { get; private set; } = 0.0f;
	public float PathRatio { get; private set; } = 0.0f;

	public void SetStopTimer( float _time ) { m_stopTimer = _time; }

	public enum State {
		NORMAL,
		JUMP,
		BOUNCE,
		FALL,
		CRASH,
		BOOST,
		DASH,
		STOP_TIME,
		DEAD,
	};

	enum ButtonState {
		JUST_RELEASED,
		RELEASED,
		JUST_PRESSED,
		PRESSED,
	}

	public State CurrentState { get; private set; } = State.NORMAL;
	ButtonState m_currentButtonState = ButtonState.RELEASED;
	ButtonState m_currentFireState = ButtonState.RELEASED;

	float UpdateMove() {
		//m_currentSpeed = Mathf.Min( m_currentSpeed + m_initialAcceleration * Time.deltaTime, CurrentState != State.BOOST ? m_speed : m_boostSpeed );
		m_currentSpeed = CurrentState != State.BOOST ? m_speed : m_boostSpeed;
		m_currentSpeed = m_currentSpeed * ( CurrentState == State.DASH ? 3.0f : 1.0f );
		float move = m_currentSpeed * Time.deltaTime;
		PathPosition += move;

		float pathPct = PathRatio;
		Vector3 nextPos = m_spire.Spline.GetAtDistanceFrom( ref pathPct, m_currentSpeed * Time.deltaTime );
		PathRatio = pathPct;

		Vector3 tangent = m_spire.Spline.GetTangentAtPercentage( PathRatio, 0.001f );
		Vector3 lateralAxis = Vector3.Cross( tangent.normalized, Vector3.up );
		Vector3 realUp = Vector3.Cross( lateralAxis.normalized, tangent );

		nextPos += Vector3.up * ( m_gameProp.m_sphereRadius + CurrentHeight );
		transform.position = nextPos;
		transform.rotation = Quaternion.LookRotation( tangent, Vector3.up );

		if ( PathRatio >= 1.0f - float.Epsilon ) {
			m_main.NewGame();
		}

		return move;
	}

	#region STATES
	void Update_Normal() {
		// Pressed, need jump
		bool needJump = false;
		if ( m_allowSwipe ) {
			needJump = m_currentButtonState == ButtonState.JUST_RELEASED;
		} else {
			needJump = ( Time.frameCount - m_lastJustPressedFrame ) < m_jumpAnticipationFrameCount;
		}
		if ( needJump ) {
			JumpVelocity = m_jumpImpulse;
			CurrentState = State.JUMP;
		} else if ( m_currentFireState == ButtonState.JUST_PRESSED ) {
			Dash( 5.0f );
		}
		UpdateMove();
	}

	void Update_Jump() {
		float gravity = Physics.gravity.y;
		if ( m_currentButtonState == ButtonState.PRESSED ) {
			gravity *= m_jumpButtonPressedGravityMultiplier;
		} else {
			gravity *= m_lowJumpMultiplier;
		}

		if ( m_currentButtonState == ButtonState.JUST_RELEASED ) {
			CurrentState = State.FALL;
		}

		JumpVelocity += gravity * Time.deltaTime;
		CurrentHeight += JumpVelocity * Time.deltaTime;
		if ( CurrentHeight <= 0.0f ) {
			CurrentHeight = 0.0f;
			CurrentState = State.NORMAL;
		}
		UpdateMove();
	}

	void Update_Bounce() {
		float gravity = Physics.gravity.y;
		gravity *= m_bounceGravityMultiplier;

		if ( m_currentButtonState == ButtonState.JUST_PRESSED && CurrentHeight > 2.0f * m_gameProp.m_sphereRadius ) {
			CurrentState = State.CRASH;
		}

		JumpVelocity += gravity * Time.deltaTime;
		CurrentHeight += JumpVelocity * Time.deltaTime;
		if ( CurrentHeight <= 0.0f ) {
			CurrentHeight = 0.0f;
			CurrentState = State.NORMAL;
		}
		UpdateMove();
	}

	void Update_Fall() {
		if ( m_currentButtonState == ButtonState.JUST_PRESSED && CurrentHeight > 2.0f * m_gameProp.m_sphereRadius ) {
			CurrentState = State.CRASH;
		}

		float gravity = Physics.gravity.y;
		if ( JumpVelocity < 0.0f ) {
			gravity *= m_fallGravityMultiplier;
		} else {
			gravity *= m_lowJumpMultiplier;
		}

		JumpVelocity += gravity * Time.deltaTime;
		CurrentHeight += JumpVelocity * Time.deltaTime;
		if ( CurrentHeight <= 0.0f ) {
			CurrentHeight = 0.0f;
			CurrentState = State.NORMAL;
		}
		UpdateMove();
	}

	void Update_Crash() {
		float gravity = Physics.gravity.y;
		gravity *= m_forceFallMultiplier;
		JumpVelocity += gravity * Time.deltaTime;
		CurrentHeight += JumpVelocity * Time.deltaTime;
		if ( CurrentHeight <= 0.0f ) {
			CurrentHeight = 0.0f;

			CameraShake shake = GameObject.FindGameObjectWithTag( "MainCamera" ).GetComponent<CameraShake>();
			shake.StartShake( m_forceFallShake );
			SetStopTimer( m_stopMoveAfterForceFallTimer );
			TapticManager.Impact( ImpactFeedback.Light );

			CurrentState = State.STOP_TIME;
		}
		UpdateMove();
	}

	void Update_Boost() {
		m_boostDistanceLeft -= UpdateMove();

		if( m_boostDistanceLeft <= 0.0f ) {
			m_boostVisuals.StopEffect();
			CurrentState = State.NORMAL;
		}
	}

	void Update_StopTime() {
		m_stopTimer -= Time.deltaTime;
		if ( m_stopTimer <= 0.0f ) {
			CurrentState = m_boostDistanceLeft > 0.0f ? State.BOOST : State.NORMAL;
		}
	}

	void Update_Dead() {
		m_deadTimer += Time.deltaTime;
		if ( m_deadTimer > 1.0f ) {
			m_main.NewGame( true );
		}
	}
	#endregion

	// Use this for initialization
	void Start() {
		m_gameProp = GameObject.FindGameObjectWithTag( "GameProp" ).GetComponent<GameProperty_Spire>();
		m_spire = GameObject.FindGameObjectWithTag( "Spire" ).GetComponent<SpireGenerator>();

		GameObject mainObj = GameObject.Find( "Main" );
		if ( mainObj != null ) {
			m_main = mainObj.GetComponent<Main>();
		} else {
			SceneManager.UnloadSceneAsync( "SpireUp" );
			SceneManager.LoadSceneAsync( "Main" );
		}

		m_boostVisuals = GetComponent<PlayerBoostVisuals>();

		// Adjust ball size
		transform.localScale = Vector3.one * m_gameProp.m_sphereRadius * 2.0f;
	}

	void UpdateButtonState() {
		bool buttonState = false;
		bool fireState = false;
		if ( Input.touchSupported ) {
			if ( Input.touchCount >= 1 ) {
				Touch touchEvent = Input.GetTouch( 0 );
				switch ( touchEvent.phase ) {
					case TouchPhase.Began: {
						m_swipeBegin = touchEvent.position;
						buttonState = true;
					}
					break;

					case TouchPhase.Canceled: {
					}
					break;

					case TouchPhase.Moved: {
						buttonState = true;
					}
					break;

					case TouchPhase.Stationary: {
						buttonState = true;
					}
					break;

					case TouchPhase.Ended: {
						if ( m_allowSwipe ) {
							Vector2 swipeEnd = touchEvent.position;
							if ( ( swipeEnd - m_swipeBegin ).magnitude > m_swipeThreshold ) {
								fireState = true;
							}
						}
					}
					break;
				}
			}
		} else {
			buttonState = Input.GetButton( "Jump" );
			fireState = Input.GetButton( "Fire1" );
		}

		if ( buttonState ) {
			if ( m_currentButtonState == ButtonState.RELEASED ) {
				m_currentButtonState = ButtonState.JUST_PRESSED;
				m_lastJustPressedFrame = Time.frameCount;
			} else {
				m_currentButtonState = ButtonState.PRESSED;
			}
		} else {
			if ( m_currentButtonState == ButtonState.PRESSED ) {
				m_currentButtonState = ButtonState.JUST_RELEASED;
			} else {
				m_currentButtonState = ButtonState.RELEASED;
			}
		}

		if ( fireState ) {
			if ( m_currentFireState == ButtonState.RELEASED ) {
				m_currentFireState = ButtonState.JUST_PRESSED;
			} else {
				m_currentFireState = ButtonState.PRESSED;
			}
		} else {
			if ( m_currentFireState == ButtonState.PRESSED ) {
				m_currentFireState = ButtonState.JUST_RELEASED;
			} else {
				m_currentFireState = ButtonState.RELEASED;
			}
		}
	}

	public void OnObjectDestruction() {
		CameraShake shake = GameObject.FindGameObjectWithTag( "MainCamera" ).GetComponent<CameraShake>();
		shake.StartShake( m_obstacleDestructionShake );

		m_main.AddScore( transform, 15 );
		m_main.ShowEventText( "BOOM!" );

		TapticManager.Impact( ImpactFeedback.Medium );
		CurrentHeight = 0.0f;

		if ( m_boostDistanceLeft <= 0.0f ) {
			SetStopTimer( m_stopMoveAfterObstacleDestruction );
			CurrentState = State.STOP_TIME;
		}
	}

	// Update is called once per frame
	void Update() {
		UpdateButtonState();

		switch (CurrentState) {
			case State.NORMAL: Update_Normal(); break;
			case State.JUMP: Update_Jump(); break;
			case State.BOUNCE: Update_Bounce(); break;
			case State.FALL: Update_Fall(); break;
			case State.CRASH: Update_Crash(); break;
			case State.BOOST: Update_Boost(); break;
			case State.DASH: Update_Boost(); break;
			case State.STOP_TIME: Update_StopTime(); break;
			case State.DEAD: Update_Dead(); break;
		}
	}

	public void Bounce() {
		JumpVelocity = m_bounceImpulse;
		m_main.AddScore( transform, 25 );
		m_main.ShowEventText( "BOUNCE!" );
		CurrentState = State.BOUNCE;
	}

	public void PassedObstacle() {
		m_main.AddScore( transform, 5 );
	}

	public void Die() {
		if ( CurrentState != State.DEAD ) {
			if ( m_deathFx != null ) {
				Instantiate( m_deathFx, transform.position, transform.rotation );
			}
			GetComponent<MeshRenderer>().enabled = false;
			CurrentState = State.DEAD;

			CameraShake shake = GameObject.FindGameObjectWithTag( "MainCamera" ).GetComponent<CameraShake>();
			shake.StartShake( m_deathShake );
			TapticManager.Impact( ImpactFeedback.Heavy );
			m_main.ShowEventText( "GAME OVER" );
		}
	}

	public void Boost( float _distance ) {
		if ( CurrentHeight <= 0.0f ) {
			m_boostDistanceLeft = _distance;
			CurrentState = State.BOOST;
			m_boostVisuals.StartEffect();
			m_main.ShowEventText( "BOOST" );
		}
	}

	public void Dash( float _distance ) {
		if ( CurrentHeight <= 0.0f ) {
			m_boostDistanceLeft = _distance;
			CurrentState = State.DASH;
			m_boostVisuals.StartEffect();
			m_main.ShowEventText( "DASH" );
		}
	}

	public void AddCoin() {
		m_main.AddScore( transform, 1 );
	}
}
