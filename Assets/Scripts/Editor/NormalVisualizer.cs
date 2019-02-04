using UnityEditor;
using UnityEngine;
/*
[CustomEditor( typeof( MeshFilter ) )]
public class NormalsVisualizer : Editor {

	private Mesh mesh;

	void OnEnable() {
		MeshFilter mf = target as MeshFilter;
		if ( mf != null ) {
			mesh = mf.sharedMesh;
		}
	}

	void OnSceneGUI() {
		if ( mesh == null ) {
			return;
		}

		MeshFilter filter = target as MeshFilter;
		Handles.matrix = filter.transform.localToWorldMatrix;

		for ( int i = 0; i < mesh.vertexCount; i++ ) {
			Handles.zTest = UnityEngine.Rendering.CompareFunction.Less;
			if ( i % 16 == 14 || i % 16 == 8 ) {
				Handles.color = Color.red;
				DebugExtension.DebugPoint( mesh.vertices[i], 0.1f, 0.0f, true );
			} else {
				Handles.color = new Color( ( float )i / mesh.vertexCount, 1.0f - i % 16 / 16.0f, i % 16 / 16.0f );
			}
			Handles.DrawLine(
				mesh.vertices[i],
				mesh.vertices[i] + mesh.normals[i] * 0.5f );
		}
	}
}

	*/
