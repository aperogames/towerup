using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GradientColorChange {
	public bool m_allowChange = false;
	public Color m_targetColor = Color.white;

	public GradientColorChange( GradientColorChange _other ) {
		m_allowChange = _other.m_allowChange;
		m_targetColor = _other.m_targetColor;
	}
}

[System.Serializable]
public class GradientChangeRequest {
	public GradientColorChange m_startColor;
	public GradientColorChange m_middleColor;
	public GradientColorChange m_endColor;
	public float m_blendTime = 0.2f;

	public GradientChangeRequest( GradientChangeRequest _other ) {
		m_startColor = new GradientColorChange( _other.m_startColor );
		m_middleColor = new GradientColorChange( _other.m_middleColor );
		m_endColor = new GradientColorChange( _other.m_endColor );
		m_blendTime = _other.m_blendTime;
	}
}

public class GradientModifier : MonoBehaviour {
	private GradientChangeRequest m_currentRequest = null;
	private GradientChangeRequest m_stopRequest = null;

	private Material m_material;
	private int m_colorStartId;
	private int m_colorMiddleId;
	private int m_colorEndId;

	private float m_blendTimeLeft = 0.0f;

	public void SetRequest( GradientChangeRequest _request ) {
		if ( _request == null ) {
			return;
		}
		m_currentRequest = new GradientChangeRequest( _request );
		m_blendTimeLeft = m_currentRequest.m_blendTime;

		m_stopRequest = new GradientChangeRequest( m_currentRequest );
		m_stopRequest.m_startColor.m_targetColor = m_material.GetColor( m_colorStartId );
		m_stopRequest.m_middleColor.m_targetColor = m_material.GetColor( m_colorMiddleId );
		m_stopRequest.m_endColor.m_targetColor = m_material.GetColor( m_colorEndId );
	}

	public void StopRequest() {
		m_currentRequest = null;
		m_blendTimeLeft = m_stopRequest.m_blendTime;
	}

    // Start is called before the first frame update
    void Start() {
		m_material = GetComponent<MeshRenderer>().material;
		m_colorStartId = Shader.PropertyToID( "_ColorStart" );
		m_colorMiddleId = Shader.PropertyToID( "_ColorMiddle" );
		m_colorEndId = Shader.PropertyToID( "_ColorEnd" );
	}

	// Update is called once per frame
	void Update() {
		if ( m_blendTimeLeft <= 0.0f ) {
			if ( m_currentRequest == null ) {
				m_stopRequest = null;
			}
			return;
		}

		GradientChangeRequest currentRequest = null;
		if ( m_currentRequest != null ) {
			currentRequest = m_currentRequest;
		} else {
			currentRequest = m_stopRequest;
		}

		if ( currentRequest != null ) {
			float t = Mathf.Min( Time.deltaTime / m_blendTimeLeft, 1.0f );
			m_blendTimeLeft -= Time.deltaTime;

			if ( currentRequest.m_startColor.m_allowChange ) {
				Color color = Color.Lerp( m_material.GetColor( m_colorStartId ), currentRequest.m_startColor.m_targetColor, t );
				m_material.SetColor( m_colorStartId, color );
			}

			if ( currentRequest.m_middleColor.m_allowChange ) {
				Color color = Color.Lerp( m_material.GetColor( m_colorMiddleId ), currentRequest.m_middleColor.m_targetColor, t );
				m_material.SetColor( m_colorMiddleId, color );
			}

			if ( currentRequest.m_endColor.m_allowChange ) {
				Color color = Color.Lerp( m_material.GetColor( m_colorEndId ), currentRequest.m_endColor.m_targetColor, t );
				m_material.SetColor( m_colorEndId, color );
			}
		}
    }
}
