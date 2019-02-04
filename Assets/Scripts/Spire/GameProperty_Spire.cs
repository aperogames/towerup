using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class GameProperty_Spire : MonoBehaviour {
	public float m_sphereRadius = 0.5f;
	public int m_seed = 0;

#if UNITY_EDITOR
	public void OnValidate() {
		if ( Application.isEditor && !EditorApplication.isPlaying ) {
			GameObject.Find( "Spire" ).GetComponent<SpireGenerator>().Generate();
		}
	}
#endif
}
