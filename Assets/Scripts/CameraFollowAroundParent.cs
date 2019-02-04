using UnityEngine;

public class CameraFollowAroundParent : MonoBehaviour {
	public Vector3 m_cameraOffset = new Vector3();
	public Vector3 m_targetOffset = new Vector3();
	public Vector3 m_boostCameraOffset = new Vector3();
	public Vector3 m_boostTargetOffset = new Vector3();
	[Range( 0.0f, 1.0f )]
	public float m_transitionFactor = 0.2f;
	public float m_boostTransitionTime = 0.2f;

	private Transform m_playerTransform;
	private SpireGenerator m_spire;
	private Player_Spire m_playerCtrl;
	private float m_boostTransitionFactor = 0.0f;
	private float m_boostTransitionTimer = 0.0f;

	// Use this for initialization
	void Start() {
		GameObject player = GameObject.FindGameObjectWithTag( "Player" );
		m_playerTransform = player.transform;
		m_playerCtrl = player.GetComponent<Player_Spire>();
		m_spire = GameObject.FindGameObjectWithTag( "Spire" ).GetComponent<SpireGenerator>();

		Camera camera = GetComponent<Camera>();
		float frustumHeightAtUnit = 2.0f * Mathf.Tan( camera.fieldOfView * 0.5f * Mathf.Deg2Rad );
		float idealFrustumWidthAtUnit = frustumHeightAtUnit * 9.0f / 16.0f;
		float idealFrustumHeightAtUnit = idealFrustumWidthAtUnit / camera.aspect;

		camera.fieldOfView = 2.0f * Mathf.Atan( idealFrustumHeightAtUnit * 0.5f ) * Mathf.Rad2Deg;
	}

	float GetRatioToIdealPos() {
		float ratio = 1.0f;
		if ( m_playerCtrl.PathRatio < m_spire.StartFlatRatio ) {
			ratio = 0.0f;
			if ( ( m_spire.StartFlatRatio - m_playerCtrl.PathRatio ) < ( m_transitionFactor * m_spire.StartFlatRatio ) ) {
				ratio = 1.0f - ( m_spire.StartFlatRatio - m_playerCtrl.PathRatio ) / ( m_transitionFactor * m_spire.StartFlatRatio );
			}
		} else if ( m_playerCtrl.PathRatio > m_spire.EndFlatRatio ) {
			ratio = 0.0f;
			if ( ( m_playerCtrl.PathRatio - m_spire.EndFlatRatio ) < ( m_transitionFactor * ( 1.0f - m_spire.EndFlatRatio ) ) ) {
				ratio = 1.0f - ( m_playerCtrl.PathRatio - m_spire.EndFlatRatio ) / ( m_transitionFactor * ( 1.0f - m_spire.EndFlatRatio ) );
			}
		} else {
			// Boost
			if ( m_playerCtrl.CurrentState == Player_Spire.State.BOOST ) {
				if ( m_boostTransitionFactor >= 1.0f - Mathf.Epsilon ) {
					m_boostTransitionTimer = 0.0f;
					m_boostTransitionFactor = 1.0f;
				} else {
					m_boostTransitionTimer += Time.deltaTime;
					m_boostTransitionFactor = Mathf.Min( m_boostTransitionTimer / m_boostTransitionTime, 1.0f );
				}
			} else {
				if ( m_boostTransitionFactor <= 0.0f + Mathf.Epsilon ) {
					m_boostTransitionTimer = 0.0f;
					m_boostTransitionFactor = 0.0f;
				} else {
					m_boostTransitionTimer += Time.deltaTime;
					m_boostTransitionFactor = Mathf.Max( 1.0f - m_boostTransitionTimer / m_boostTransitionTime, 0.0f );
				}
			}
			ratio = 1.0f - m_boostTransitionFactor;
		}
		return ratio;
	}

	void GetTargetPositions( out Vector3 _camPos, out Vector3 _targetPos ) {
		Vector3 tangent = m_spire.Spline.GetTangentAtPercentage( m_playerCtrl.PathRatio, 0.001f );
		if ( m_playerCtrl.PathRatio < m_spire.StartFlatRatio || m_playerCtrl.PathRatio > m_spire.EndFlatRatio ) {
			Vector3 lateralAxis = Vector3.Cross( tangent, Vector3.up );
			Quaternion rotation = Quaternion.LookRotation( lateralAxis );
			Vector3 worldTargetOffset = rotation * m_targetOffset;
			Vector3 worldCamOffset = rotation * m_cameraOffset;
			Vector3 sideTargetPos = m_playerTransform.position + worldTargetOffset;

			_targetPos = m_playerTransform.position + worldTargetOffset;
			_camPos = sideTargetPos + worldCamOffset;
		} else {
			// BOOST Pos
			Vector3 towerCenter = Vector3.zero;
			towerCenter.y = m_playerTransform.position.y;
			Vector3 lateralAxis = towerCenter - m_playerTransform.position;
			Quaternion rotation = Quaternion.LookRotation( lateralAxis );
			Vector3 worldTargetOffset = rotation * m_boostTargetOffset;
			Vector3 worldCamOffset = rotation * m_boostCameraOffset;
			Vector3 towerTargetPos = m_playerTransform.position + worldTargetOffset;

			_targetPos = m_playerTransform.position + worldTargetOffset;
			_camPos = towerTargetPos + worldCamOffset;
		}
	}

	// Update is called once per frame
	void Update() {
		// Same for tower target
		Vector3 towerCenter = Vector3.zero;
		towerCenter.y = m_playerTransform.position.y;

		Vector3 lateralAxis = towerCenter - m_playerTransform.position;

		Quaternion rotation = Quaternion.LookRotation( lateralAxis );
		Vector3 worldTargetOffset = rotation * m_targetOffset;
		Vector3 worldCamOffset = rotation * m_cameraOffset;

		Vector3 towerTargetPos = m_playerTransform.position + worldTargetOffset;
		Vector3 towerCameraPos = towerTargetPos + worldCamOffset;

		float ratio = GetRatioToIdealPos();

		Vector3 finalCameraPos = towerCameraPos;
		Vector3 finalTargetPos = towerTargetPos;

		if ( ratio < 1.0f ) {
			Vector3 targetCamPos;
			Vector3 targetTargetPos;
			GetTargetPositions( out targetCamPos, out targetTargetPos );
			finalCameraPos = Vector3.Lerp( targetCamPos, towerCameraPos, ratio );
			finalTargetPos = Vector3.Lerp( targetTargetPos, towerTargetPos, ratio );
		}

		transform.SetPositionAndRotation( finalCameraPos, Quaternion.LookRotation( ( finalTargetPos - finalCameraPos ).normalized ) );
	}
}
