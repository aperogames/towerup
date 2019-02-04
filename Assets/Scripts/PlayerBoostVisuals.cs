using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBoostVisuals : MonoBehaviour {
	public Gradient m_trailGradient;
	public GradientChangeRequest m_backGroundGradientChange;
	public Color m_ballColor;
	public float m_ballFadeTime = 0.2f;

	private TrailRenderer m_trail;
	private Gradient m_oldGradient;
	private GradientModifier m_gradient;

	private int m_ballColorId;
	private Color m_oldBallColor;
	private Material m_ballMaterial;

	private float m_fadeTimer = 0.0f;

	private bool m_shouldStop = true;

	// Start is called before the first frame update
	void Start() {
		m_trail = GetComponent<TrailRenderer>();
		GameObject gradientObj = GameObject.FindGameObjectWithTag( "Gradient" );
		if ( gradientObj != null ) {
			m_gradient = gradientObj.GetComponent<GradientModifier>();
		}

		m_oldGradient = m_trail.colorGradient;

		m_ballColorId = Shader.PropertyToID( "_Color" );
		m_ballMaterial = GetComponent<MeshRenderer>().material;
		m_oldBallColor = m_ballMaterial.GetColor( m_ballColorId );

		enabled = false;
	}

	public void StartEffect() {
		m_gradient.SetRequest( m_backGroundGradientChange );
		m_fadeTimer = 0.0f;
		m_shouldStop = false;

		Gradient newGradient = new Gradient();

		List<GradientAlphaKey> newAlphaKeyList = new List<GradientAlphaKey>();
		List<GradientColorKey> newColorKeyList = new List<GradientColorKey>();
		foreach ( GradientAlphaKey alphaKey in m_oldGradient.alphaKeys ) {
			newAlphaKeyList.Add( new GradientAlphaKey( alphaKey.alpha, alphaKey.time ) );
		}
		foreach ( GradientColorKey colorKey in m_oldGradient.colorKeys ) {
			newColorKeyList.Add( new GradientColorKey( colorKey.color, colorKey.time ) );
		}

		foreach ( GradientAlphaKey alphaKey in m_trailGradient.alphaKeys ) {
			bool add = true;
			foreach ( GradientAlphaKey existingKey in newAlphaKeyList ) {
				if ( alphaKey.time == existingKey.time ) {
					add = false;
					break;
				}
			}

			if ( add ) {
				Color colorAtTime = m_oldGradient.Evaluate( alphaKey.time );
				newAlphaKeyList.Add( new GradientAlphaKey( colorAtTime.a, alphaKey.time ) );
			}
		}

		foreach ( GradientColorKey colorKey in m_trailGradient.colorKeys ) {
			bool add = true;
			foreach ( GradientColorKey existingKey in newColorKeyList ) {
				if ( colorKey.time == existingKey.time ) {
					add = false;
					break;
				}
			}

			if ( add ) {
				Color colorAtTime = m_oldGradient.Evaluate( colorKey.time );
				colorAtTime.a = 1.0f;
				newColorKeyList.Add( new GradientColorKey( colorAtTime, colorKey.time ) );
			}
		}

		newGradient.SetKeys( newColorKeyList.ToArray(), newAlphaKeyList.ToArray() );

		m_trail.colorGradient = newGradient;

		enabled = true;
	}

	public void StopEffect() {
		m_gradient.StopRequest();
		m_fadeTimer = 0.0f;
		m_shouldStop = true;
	}

	private void OnEnable() {

	}

	private void OnDisable() {

	}

	// Update is called once per frame
	void Update() {
		float ratio = Mathf.Clamp( m_fadeTimer / m_ballFadeTime, 0.0f, 1.0f );
		m_fadeTimer += Time.deltaTime;

		Color ballColor = m_ballMaterial.GetColor( m_ballColorId );
		Color newColor;
		if ( m_shouldStop ) {
			newColor = Color.Lerp( ballColor, m_oldBallColor, ratio );

			if ( ratio >= 1.0f - Mathf.Epsilon ) {
				enabled = false;
			}
		} else {
			newColor = Color.Lerp( ballColor, m_ballColor, ratio );
		}
		m_ballMaterial.SetColor( m_ballColorId, newColor );

		// Handle trail
		List<GradientAlphaKey> newAlphaKeys = new List<GradientAlphaKey>();
		List<GradientColorKey> newColorKeys = new List<GradientColorKey>();
		for ( int i = 0; i < m_trail.colorGradient.alphaKeys.Length; ++i ) {
			float time = m_trail.colorGradient.alphaKeys[i].time;
			Color original = m_oldGradient.Evaluate( time );
			Color destination = m_trailGradient.Evaluate( time );

			float alpha;
			if ( m_shouldStop ) {
				alpha = Mathf.Lerp( destination.a, original.a, ratio );
			} else {
				alpha = Mathf.Lerp( original.a, destination.a, ratio );
			}
			newAlphaKeys.Add( new GradientAlphaKey( alpha, time ) );
		}

		for ( int i = 0; i < m_trail.colorGradient.colorKeys.Length; ++i ) {
			float time = m_trail.colorGradient.colorKeys[i].time;
			Color original = m_oldGradient.Evaluate( time );
			Color destination = m_trailGradient.Evaluate( time );

			Color color;
			if ( m_shouldStop ) {
				color = Color.Lerp( destination, original, ratio );
			} else {
				color = Color.Lerp( original, destination, ratio );
			}
			newColorKeys.Add( new GradientColorKey( new Color( color.r, color.g, color.b ), time ) );
		}

		Gradient newGradient = new Gradient();
		newGradient.SetKeys( newColorKeys.ToArray(), newAlphaKeys.ToArray() );
		m_trail.colorGradient = newGradient;
	}
}
