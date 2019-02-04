using UnityEngine;
using System.Collections;

[System.Serializable]
public class CameraShakeEffect {
	public float m_shakeDuration = 0.25f;
	public float m_shakeAmount = 0.05f;
	public float m_decreaseFactor = 1.0f;
	public bool m_sphereShake = false;

	public CameraShakeEffect( CameraShakeEffect _other ) {
		m_shakeDuration = _other.m_shakeDuration;
		m_shakeAmount = _other.m_shakeAmount;
		m_decreaseFactor = _other.m_decreaseFactor;
		m_sphereShake = _other.m_sphereShake;
	}
}

public class CameraShake : MonoBehaviour {
	// Transform of the camera to shake. Grabs the gameObject's transform
	// if null.
	public Transform camTransform;

	private CameraShakeEffect m_currentShake = null;

	void Awake() {
		if ( camTransform == null ) {
			camTransform = GetComponent( typeof( Transform ) ) as Transform;
		}
	}

	public void StartShake( CameraShakeEffect _effect ) {
		m_currentShake = new CameraShakeEffect( _effect );
	}

	void Update() {
		if ( m_currentShake != null && m_currentShake.m_shakeDuration > 0.0f ) {
			if ( m_currentShake.m_sphereShake ) {
				camTransform.position += Random.insideUnitSphere * m_currentShake.m_shakeAmount;
			} else {
				camTransform.position += Vector3.up * Random.Range( -1.0f, 1.0f ) * m_currentShake.m_shakeAmount;
			}

			m_currentShake.m_shakeDuration -= Time.deltaTime * m_currentShake.m_decreaseFactor;
		} else {
			m_currentShake = null;
		}
	}
}
