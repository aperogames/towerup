//#define DEBUG_SPLINE

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[ExecuteInEditMode]
public class SpireGenerator : MonoBehaviour {
	public float m_totalHeight = 50.0f;
	[Range( 2.0f, 20.0f )]
	public float m_minHeightPerTurn = 2.0f;
	[Range( 2.0f, 20.0f )]
	public float m_maxHeightPerTurn = 10.0f;
	[Range( 0.0f, 20.0f )]
	public float m_heightPerTurnMaxVariation = 4.0f;
	[Range( 1.0f, 20.0f )]
	public float m_minRadius = 2.0f;
	[Range( 1.0f, 20.0f )]
	public float m_maxRadius = 20.0f;
	[Range( 1, 20 ), Tooltip( "In path width step" )]
	public int m_maxRadiusVariation = 1;
	public float m_radiusVariationMultiplier = 1.0f;
	[Range( 0.1f, 10.0f )]
	public float m_meshGenStep = 1.0f;
	[Range( 0.0f, 10.0f )]
	public float m_pathWidth = 1.0f;
	[Range( 0.0f, 10.0f )]
	public float m_pathThick = 0.1f;
	[Range( 0.0f, 1.0f )]
	public float m_coefficient = 1.0f;
	public float m_curveStart = 0.0f;
	public float m_curveEnd = 0.0f;
	public float m_flatLengthStart = 0.0f;
	public float m_flatLengthEnd = 0.0f;
	public float m_flatStartHeightOffset = 1.0f;
	public Vector2 m_gutterOffset = new Vector2( 0.2f, 0.2f );

	public float m_loopingRadius = 0.0f;
	public float m_loopingSpread = 0.0f;

	private int m_controlPointsPerTurn = 8;
	private GameProperty_Spire m_gameProp;

	public float StartFlatRatio { get; private set; } = 0.0f;
	public float EndFlatRatio { get; private set; } = 0.0f;

	public Spline Spline { get; } = new Spline();

	public float PathLength { get; private set; } = 1.0f;

#if UNITY_EDITOR
	public void OnValidate() {
		if ( Application.isEditor && !EditorApplication.isPlaying ) {
			m_flatLengthStart = Mathf.Max( 0.0f, m_flatLengthStart );
			m_flatLengthEnd = Mathf.Max( 0.0f, m_flatLengthEnd );
			m_totalHeight = Mathf.Max( 0.0f, m_totalHeight );
			Generate();
		}
	}
#endif

	public void ComputePathLength() {
		// Compute path length
		PathLength = 0.0f;
		Vector3 previousPoint = Spline.GetPointAtPct( 0.0f );
		for ( float pct = 0.0001f; pct <= 1.0f; pct += 0.0001f ) {
			Vector3 point = Spline.GetPointAtPct( pct );

			float distance = ( point - previousPoint ).magnitude;
			PathLength += distance;
			previousPoint = point;
		}
	}

	// Use this for initialization
	void Start() {
		Generate();
	}

	void Stop() {
		//Destroy( GetComponent<MeshFilter>().mesh );
		System.GC.Collect();
	}

	public void Update() {
	}

#if DEBUG_SPLINE
	private void OnDrawGizmos() {
		for ( float pct = 0.0f; pct <= 1.0f; pct += 0.001f ) {
			Vector3 point = Spline.GetPointAtPct( pct );

			DebugExtension.DebugPoint( point );
		}

	}
#endif

	public void Generate() {
		m_gameProp = GameObject.FindGameObjectWithTag( "GameProp" ).GetComponent<GameProperty_Spire>();
#if UNITY_EDITOR
		if ( Application.isEditor || EditorApplication.isPlaying ) {
			Random.InitState( m_gameProp.m_seed );
		}
#else
		Random.InitState( m_gameProp.m_seed );
#endif
		GenerateSpline();
		GenerateMeshes();
	}

	void GenerateMeshes() {
		// 1. Generate mesh vertices
		List<Vector3> vertices = new List<Vector3>();
		List<Vector3> normals = new List<Vector3>();
		List<Vector3> uvs = new List<Vector3>();
		float lastPct = 0.0f;
		Vector3 lastPoint = Spline.GetPointAtPct(lastPct);

		while ( lastPct < 1.0f) {
			Vector3 tangent = Spline.GetTangentAtPercentage( lastPct, 0.001f );
			Vector3 lateralAxis = Vector3.Cross( Vector3.up, tangent );

			// Top
			vertices.Add( lastPoint - lateralAxis * ( m_pathWidth * 0.5f ) ); // TopIn
			vertices.Add( lastPoint - lateralAxis * ( m_pathWidth * 0.5f + m_gutterOffset.x ) + Vector3.up * m_gutterOffset.y ); // TopInSide
			vertices.Add( vertices[vertices.Count - 1] ); // TopInCap
			vertices.Add( lastPoint - lateralAxis * ( m_pathWidth * 0.5f + m_gutterOffset.x ) + Vector3.up * m_gutterOffset.y ); // TopInGoutiere
			vertices.Add( lastPoint - lateralAxis * ( m_pathWidth * 0.5f ) ); // TopInGoutiere

			vertices.Add( lastPoint + lateralAxis * ( m_pathWidth * 0.5f ) ); // TopOut
			vertices.Add( lastPoint + lateralAxis * ( m_pathWidth * 0.5f + m_gutterOffset.x ) + Vector3.up * m_gutterOffset.y ); // TopOutSide
			vertices.Add( vertices[vertices.Count - 1] ); // TopOutSideCap
			vertices.Add( lastPoint + lateralAxis * ( m_pathWidth * 0.5f + m_gutterOffset.x ) + Vector3.up * m_gutterOffset.y ); // TopOutGoutiere
			vertices.Add( lastPoint + lateralAxis * ( m_pathWidth * 0.5f ) ); // TopOutGoutiere

			// Bottom
			vertices.Add( lastPoint - lateralAxis * ( m_pathWidth * 0.5f ) - Vector3.up * m_pathThick ); // BottomIn
			vertices.Add( vertices[vertices.Count - 1] ); // BottomInSide
			vertices.Add( vertices[vertices.Count - 1] ); // BottomInSideCap

			vertices.Add( lastPoint + lateralAxis * ( m_pathWidth * 0.5f ) - Vector3.up * m_pathThick ); // BottomOut
			vertices.Add( vertices[vertices.Count - 1] ); // BottomOutSide
			vertices.Add( vertices[vertices.Count - 1] ); // BottomOutSideCap

			Vector3 normalLeftSide;
			Vector3 normalRigthSide;
			if ( vertices.Count >= 32 ) {
				normalLeftSide = Vector3.Cross(
					( vertices[vertices.Count - 32 + 11] - vertices[vertices.Count - 32 + 1] ).normalized,
					( vertices[vertices.Count - 16 + 1] - vertices[vertices.Count - 32 + 1] ).normalized );

				normalRigthSide = Vector3.Cross(
					( vertices[vertices.Count - 16 + 8] - vertices[vertices.Count - 32 + 8] ).normalized,
					( vertices[vertices.Count - 32 + 14] - vertices[vertices.Count - 32 + 8] ).normalized );
			} else {
				normalLeftSide = Vector3.Cross( ( vertices[11] - vertices[1] ).normalized, tangent );
				normalRigthSide = Vector3.Cross( tangent, ( vertices[14] - vertices[8] ).normalized );
			}

			Vector3 normal = Vector3.up;
			if ( normals.Count > 0) {
				Vector3 previousToCurrent = vertices[ vertices.Count - 16 ] - vertices[ vertices.Count - 32 ];
				previousToCurrent.Normalize();
				normal = Vector3.Cross( previousToCurrent, lateralAxis );
			}
			bool flipCapNormal = lastPct >= 1.0f;
			Vector3 capNormal = Vector3.Cross( lateralAxis, Vector3.up ) * ( flipCapNormal ? 1.0f : -1.0f );
			normals.Add( normal );// 0
			normals.Add( normalLeftSide );// 1
			normals.Add( capNormal ); // 2
			normals.Add( -normalLeftSide );
			normals.Add( -normalLeftSide );

			normals.Add( normal );// 5
			normals.Add( normalRigthSide );// 6
			normals.Add( capNormal ); // 7
			normals.Add( -normalRigthSide );
			normals.Add( -normalRigthSide );

			normals.Add( -normal );// 10
			normals.Add( normalLeftSide );// 11
			normals.Add( capNormal ); // 12

			normals.Add( -normal );// 13
			normals.Add( normalRigthSide );// 14
			normals.Add( capNormal ); // 15

			float distanceIn = 0.0f;
			float distanceOut = 0.0f;
			if ( uvs.Count > 0) {
				distanceIn = uvs[ uvs.Count - 16 ].x + 1.0f;
				distanceOut = uvs[ uvs.Count - 11 ].x + 1.0f;
			}

			uvs.Add( new Vector2( distanceIn, 0.0f ) ); // TopIn
			uvs.Add( new Vector2( distanceIn, -m_gutterOffset.x ) ); // TopInSide
			uvs.Add( new Vector2( 0.0f, 0.0f ) );		// TopInSideCap
			uvs.Add( new Vector2( distanceIn, -m_gutterOffset.x ) );		// TopInGoutiere
			uvs.Add( new Vector2( distanceIn, -m_gutterOffset.x ) );		// TopInGoutiere

			uvs.Add( new Vector2( distanceOut, 1.0f ) );    // TopOut
			uvs.Add( new Vector2( distanceOut, 1.0f+m_gutterOffset.x ) );    // TopOutSide
			uvs.Add( new Vector2( 1.0f, 0.0f ) );			// TopOutSideCap
			uvs.Add( new Vector2( distanceOut, 1.0f + m_gutterOffset.x ) );           // TopOutGoutiere
			uvs.Add( new Vector2( distanceOut, 1.0f + m_gutterOffset.x ) );           // TopOutGoutiere

			uvs.Add( new Vector2( distanceIn, 0.0f ) );							// BottomIn
			uvs.Add( new Vector2( distanceIn, m_pathThick / m_pathWidth ) );    // BottomInSide
			uvs.Add( new Vector2( 0.0f, m_pathThick / m_pathWidth ) );			// BottomInSideCap

			uvs.Add( new Vector2( distanceOut, 1.0f ) );						// BottomOut
			uvs.Add( new Vector2( distanceOut, m_pathThick / m_pathWidth ) );   // BottomOutSide
			uvs.Add( new Vector2( 1.0f, m_pathThick / m_pathWidth ) );			// BottomOutSideCap

			lastPoint = Spline.GetAtDistanceFrom( ref lastPct, m_meshGenStep );
		}
		Mesh newMesh = new Mesh {
			hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor,
			name = "spire"
		};
		newMesh.SetVertices( vertices );
		newMesh.SetNormals(normals);
		newMesh.SetUVs( 0, uvs );

		// 2. Triangles
		newMesh.subMeshCount = 1;// vertices.Count / 8 - 1;
		List<int> triangles = new List<int>();
		for ( int meshIdx = 0; meshIdx < ( vertices.Count / 16 - 1 ); ++meshIdx) {
			int startIdx = meshIdx * 16;
			int nextstartIdx = meshIdx * 16 + 16;

			int inUp = nextstartIdx;
			int inUpPrevious = startIdx;
			int sideInUp = nextstartIdx + 1;
			int sideInUpPrevious = startIdx + 1;
			int capInUp = nextstartIdx + 2;
			int capInUpPrevious = startIdx + 2;
			int gutterLeft = nextstartIdx + 3;
			int gutterLeftPrevious = startIdx + 3;
			int gutterLeftLow = nextstartIdx + 4;
			int gutterLeftLowPrevious = startIdx + 4;

			int outUp = nextstartIdx + 5;
			int outUpPrevious = startIdx + 5;
			int sideOutUp = nextstartIdx + 6;
			int sideOutUpPrevious = startIdx + 6;
			int capOutUp = nextstartIdx + 7;
			int capOutUpPrevious = startIdx + 7;
			int gutterRight = nextstartIdx + 8;
			int gutterRightPrevious = startIdx + 8;
			int gutterRightLow = nextstartIdx + 9;
			int gutterRightLowPrevious = startIdx + 9;

			int inDown = nextstartIdx + 10;
			int inDownPrevious = startIdx + 10;
			int sideInDown = nextstartIdx + 11;
			int sideInDownPrevious = startIdx + 11;
			int capInDown = nextstartIdx + 12;
			int capInDownPrevious = startIdx + 12;
			int outDown = nextstartIdx + 13;
			int outDownPrevious = startIdx + 13;
			int sideOutDown = nextstartIdx + 14;
			int sideOutDownPrevious = startIdx + 14;
			int capOutDown = nextstartIdx + 15;
			int capOutDownPrevious = startIdx + 15;

			// Cap
			if ( ( nextstartIdx + 16 ) >= vertices.Count ) {
				triangles.Add( capInUp );
				triangles.Add( capInDown );
				triangles.Add( capOutUp );

				triangles.Add( capInDown );
				triangles.Add( capOutDown );
				triangles.Add( capOutUp );
			} else if ( startIdx == 0 ) {
				triangles.Add( capInUpPrevious );
				triangles.Add( capOutUpPrevious );
				triangles.Add( capInDownPrevious );

				triangles.Add( capInDownPrevious );
				triangles.Add( capOutUpPrevious );
				triangles.Add( capOutDownPrevious );
			}

			// top
			triangles.Add(inUpPrevious);
			triangles.Add(inUp);
			triangles.Add(outUpPrevious);

			triangles.Add(outUpPrevious);
			triangles.Add(inUp);
			triangles.Add(outUp);

			// gutter right
			triangles.Add( gutterRightLowPrevious );
			triangles.Add(gutterRightLow);
			triangles.Add( gutterRightPrevious );

			triangles.Add( gutterRightLow );
			triangles.Add( gutterRight );
			triangles.Add( gutterRightPrevious );

			// gutter left
			triangles.Add( gutterLeftLowPrevious );
			triangles.Add( gutterLeftPrevious );
			triangles.Add( gutterLeft );

			triangles.Add( gutterLeftLowPrevious );
			triangles.Add( gutterLeft );
			triangles.Add( gutterLeftLow );

			// bottom
			triangles.Add(inDownPrevious);
			triangles.Add(outDownPrevious);
			triangles.Add(inDown);

			triangles.Add(inDown);
			triangles.Add(outDownPrevious);
			triangles.Add(outDown);

			// out side 1
			triangles.Add(sideOutUpPrevious);
			triangles.Add(sideOutUp);
			triangles.Add(sideOutDownPrevious);

			// out side2
			triangles.Add(sideOutUp);
			triangles.Add(sideOutDown);
			triangles.Add(sideOutDownPrevious);

			// in side 1
			triangles.Add(sideInUpPrevious);
			triangles.Add(sideInDownPrevious);
			triangles.Add(sideInUp);

			// in side2
			triangles.Add(sideInUp);
			triangles.Add(sideInDownPrevious);
			triangles.Add(sideInDown);
		}
		newMesh.SetTriangles( triangles, 0 );
		newMesh.RecalculateBounds();
		MeshFilter pathMesh = GetComponent<MeshFilter>();
		pathMesh.mesh = newMesh;
	}

	private void GenerateSpline() {
		Spline.ControlPoints.Clear();
		Spline.m_coefficient = m_coefficient;

		float currentRadius = Random.Range( m_minRadius, m_maxRadius );
		float currentHeightPerTurn = Random.Range( m_minHeightPerTurn, m_maxHeightPerTurn );
		float currentHeight = 0.0f;

#region FlatStart
		Spline.ControlPoints.Add( new Vector3( currentRadius, currentHeight - m_flatStartHeightOffset, -m_flatLengthStart - 1.0f ) );
		if ( m_flatLengthStart > 0.0f ) {
			for ( float point = m_flatLengthStart; point > 0.0f; point -= 1.0f ) {
				float curveSin = ( point / m_flatLengthStart ) * Mathf.PI;
				float heightOffset = m_flatStartHeightOffset * point / m_flatLengthStart;
				Spline.ControlPoints.Add( new Vector3( currentRadius + Mathf.Sin( curveSin ) * m_curveStart, currentHeight - heightOffset, -point ) );
			}
		}
		Spline.ControlPoints.Add( new Vector3( currentRadius, currentHeight, 0.0f ) );
		int flatStartIdx = Spline.ControlPoints.Count - 1;
#endregion

#region Spire
		float angleDelta = -( 0.0f ) * 360.0f;
		do {
			// Do one turn
			currentHeightPerTurn = Mathf.Clamp( currentHeightPerTurn + Random.Range( -m_heightPerTurnMaxVariation, m_heightPerTurnMaxVariation ), m_minHeightPerTurn, m_maxHeightPerTurn );
			int radiusVariation = Random.Range( -m_maxRadiusVariation, m_maxRadiusVariation + 1 );
			if ( radiusVariation == 0 ) {
				radiusVariation = Random.Range( 0, 2 ) == 1 ? 1 : -1;
			}
			float targetRadius = Mathf.Clamp( currentRadius + m_pathWidth * radiusVariation * m_radiusVariationMultiplier, m_minRadius, m_maxRadius );

			float targetHeight = currentHeight + currentHeightPerTurn;

			float deltaToGoal = m_totalHeight - targetHeight;
			if ( deltaToGoal < m_minHeightPerTurn ) {
				if ( ( deltaToGoal + currentHeightPerTurn ) <= m_maxHeightPerTurn ) {
					targetHeight = m_totalHeight;
				} else {
					targetHeight = currentHeight + m_minHeightPerTurn;
				}
			}

			float deltaRadius = targetRadius - currentRadius;
			float deltaHeight = targetHeight - currentHeight;

			float turnFraction = 1.0f / ( float )m_controlPointsPerTurn;
			float radiusFraction = deltaRadius / ( float )m_controlPointsPerTurn;
			float heightFraction = deltaHeight / ( float )m_controlPointsPerTurn;
			for ( int ptIdx = 1; ptIdx < ( m_controlPointsPerTurn + 1 ); ++ptIdx ) {
				float angle = -( turnFraction * ptIdx ) * 360.0f + angleDelta;
				Vector3 ctrlPoint = Quaternion.Euler( 0.0f, angle, 0.0f ) * Vector3.right;
				ctrlPoint *= currentRadius + radiusFraction * ptIdx;
				ctrlPoint.y = currentHeight + heightFraction * ptIdx;

				Spline.ControlPoints.Add( ctrlPoint );
			}
			currentHeight = targetHeight;
			currentRadius = targetRadius;
		} while ( currentHeight.CompareTo( m_totalHeight ) != 0 );
#endregion

#region Looping
		if ( m_loopingRadius > 0.0f ) {
			Vector3 direction = Vector3.down;
			Vector3 center = Spline.ControlPoints[ Spline.ControlPoints.Count - 1 ] + Vector3.up * m_loopingRadius;
			for ( float angle = 0.0f; angle <= 360.0f; angle += 20.0f ) {
				Quaternion rotation = Quaternion.AngleAxis( angle, Vector3.left );

				Vector3 newDir = rotation * Vector3.down;
				Vector3 newPos = newDir * m_loopingRadius + center;
				newPos.x += angle / 360.0f * m_loopingSpread;
				Spline.ControlPoints.Add( newPos );
			}
		}
#endregion

#region Flat_Ending
		int flatEndIdx = Spline.ControlPoints.Count;
		if ( m_flatLengthEnd > 0.0f ) {
			for ( float point = 0.0f; point < m_flatLengthEnd; point += 1.0f ) {
				float curveSin = ( point / m_flatLengthEnd ) * Mathf.PI;
				Spline.ControlPoints.Add( new Vector3( currentRadius + Mathf.Sin( curveSin ) * m_curveEnd + m_loopingSpread, currentHeight, point ) );
			}
		}
		Spline.ControlPoints.Add( new Vector3( currentRadius, currentHeight, m_flatLengthEnd + 1.0f ) );
#endregion

#region PathLengthCompute
		ComputePathLength();

		float pct = 0.0f;
		float previousDistance = float.MaxValue;
		for ( ; pct < 1.0f; pct += 0.0001f ) {
			float distance = ( Spline.GetPointAtPct( pct ) - Spline.ControlPoints[ flatStartIdx ] ).magnitude;
			if ( distance > previousDistance ) {
				StartFlatRatio = pct;
				break;
			}
			previousDistance = distance;
		}
		pct = 1.0f;
		previousDistance = float.MaxValue;
		for ( ; pct > 0.0f; pct -= 0.0001f ) {
			float distance = ( Spline.GetPointAtPct( pct ) - Spline.ControlPoints[ flatEndIdx ] ).magnitude;
			if ( distance > previousDistance ) {
				EndFlatRatio = pct;
				break;
			}
			previousDistance = distance;
		}
#endregion
	}
}
