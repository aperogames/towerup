using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ObstacleDefinition {
	public GameObject m_prefab = null;
	[Range( 0.0f, 1.0f )]
	public float m_minDifficulty = 0.0f;
	[Range( 0.0f, 1.0f )]
	public float m_maxDifficulty = 1.0f;
	public float m_goldPositionOffset = 1.5f;

	public bool MatchDifficulty( float _difficulty ) {
		return _difficulty >= m_minDifficulty && _difficulty <= m_maxDifficulty;
	}
}

public class ObstacleGenerator : MonoBehaviour {
	[HideInInspector]
	public float m_minObstacleDistance = 5.0f;
	[HideInInspector]
	public float[] m_chanceForObstacleCount = { 5.0f, 10.0f, 10.0f, 5.0f, 10.0f, 3.0f, 2.0f, 1.0f, 1.0f };
	[HideInInspector]
	public int m_maxBoostCount = 1;
	[HideInInspector]
	public float m_boostChance = 0.5f;
	[HideInInspector]
	public float m_boostSafeDistance = 2.0f;
	[HideInInspector]
	public List<GameObject> m_obstaclePrefab = new List<GameObject>();
	[HideInInspector]
	public GameObject m_boostPrefab = null;


	public List<ObstacleDefinition> m_obstacleDefinitions = new List<ObstacleDefinition>();
	public GameObject m_goldPrefab = null;

	public float m_startFreeZoneSize = 5.0f;
	public float m_stepDistance = 2.0f;
	public int m_stepsBetweenObstacleForEasy = 5;
	public int m_stepsBetweenObstacleForHard = 2;
	public int m_obstacleStreakSize = 6;
	[Range( 0.0f, 1.0f )]
	public float m_globalDifficultyMin = 0.1f;
	[Range( 0.0f, 1.0f )]
	public float m_globalDifficultyMax = 0.5f;
	[HideInInspector]
	public int m_goldStreakCount = 5;
	[HideInInspector]
	public int m_stepsBetweenGoldStreak = 20;
	[Range( 0.0f, 1.0f )]
	public float m_chanceForSoloGold = 0.5f;
	[HideInInspector]
	public float m_maxHeightForGold = 3.0f;
	public int m_stepsForGoldHeightReset = 3;
	public int m_stepsForGoldHeightRise = 5;

	private SpireGenerator m_spire;

	void GenerateObstacleStreak( ref float _currentDistance, int _steps, float _difficulty ) {
		int prefabCount = m_obstacleDefinitions.Count;
		float distance = _currentDistance;
		List<float> heightOffsetPerObstacle = new List<float>();
		for ( int i = 0; i < m_obstacleStreakSize; i++ ) {
			int prefabIdx = Random.Range( 0, prefabCount );
			ObstacleDefinition prefabChoosen = null;
			// Find one with the good difficulty
			for ( int j = 0; j < prefabCount; j++ ) {
				int realIndex = ( j + prefabIdx ) % prefabCount;
				if ( m_obstacleDefinitions[ realIndex ].MatchDifficulty( _difficulty ) ) {
					prefabChoosen = m_obstacleDefinitions[realIndex];
					break;
				}
			}

			if ( prefabChoosen != null ) {
				GameObject instance = Instantiate( prefabChoosen.m_prefab );
				Obstacle obstacle = instance.GetComponent<Obstacle>();
				obstacle.SetupPosition( _currentDistance );
				heightOffsetPerObstacle.Add( prefabChoosen.m_goldPositionOffset );
			} else {
				heightOffsetPerObstacle.Add( 0.0f );
			}
			_currentDistance += m_stepDistance * _steps;
		}

		// SubSteps for coins
		float lastHeight = 0.0f;
		for ( int i = 0; i < m_obstacleStreakSize - 1; i++ ) {
			for ( int subStep = 0; subStep < _steps; ++subStep ) {
				int stepsToGo = _steps - subStep;

				float fraction = Mathf.Min( 1.0f, Mathf.Sin( subStep / (float)m_stepsForGoldHeightReset * Mathf.PI / 2.0f ) );

				float targetHeight = 0.0f;
				if ( stepsToGo <= m_stepsForGoldHeightRise ) {
					targetHeight = heightOffsetPerObstacle[i + 1];
					fraction = Mathf.Sin( ( 1.0f - ( stepsToGo / (float)m_stepsForGoldHeightRise ) ) * Mathf.PI / 2.0f );
				}

				float diff = targetHeight - lastHeight;
				float interpolatedHeight = lastHeight + diff * fraction;
				bool needCoin = Random.Range( 0.0f, 1.0f ) <= m_chanceForSoloGold;
				if ( needCoin ) {
					float pct = 0.0f;
					Vector3 position = m_spire.Spline.GetAtDistanceFrom( ref pct, distance );
					position.y += interpolatedHeight;

					Instantiate( m_goldPrefab, position, Quaternion.identity );
				}
				lastHeight = interpolatedHeight;

				distance += m_stepDistance;
			}
		}
	}

	void Generate() {
		m_spire = GameObject.FindGameObjectWithTag( "Spire" ).GetComponent<SpireGenerator>();
		float currentDistance = m_spire.m_flatLengthStart + m_startFreeZoneSize;

		do {
			float currentDifficulty = Random.Range( m_globalDifficultyMin, m_globalDifficultyMax );

			int steps = (int)( currentDifficulty * ( m_stepsBetweenObstacleForHard - m_stepsBetweenObstacleForEasy ) + m_stepsBetweenObstacleForEasy );
			GenerateObstacleStreak( ref currentDistance, steps, currentDifficulty );


		} while (currentDistance<(m_spire.PathLength - m_spire.m_flatLengthEnd - m_startFreeZoneSize ) );

	}

// Start is called before the first frame update
void Start() {
		Generate();
	}

	// Update is called once per frame
	void Update() {

    }

	private void GenerateObstacles() {
		if ( m_obstaclePrefab == null ) {
			return;
		}

		SpireGenerator m_spire = GameObject.FindGameObjectWithTag( "Spire" ).GetComponent<SpireGenerator>();

		float groupDistance = m_minObstacleDistance * ( m_chanceForObstacleCount.Length - 1 );
		float currentDistance = m_spire.m_flatLengthStart + groupDistance;

		float randomRange = 0.0f;
		foreach ( float value in m_chanceForObstacleCount ) {
			randomRange += value;
		}

		int boostCount = 0;
		float boostEnd = 0.0f;
		float boostSafeEnd = 0.0f;
		int lastObstacleCount = -1;

		int maxObstacleCount = m_chanceForObstacleCount.Length;

		do {
			int obstacleCount = 0;
			do {
				float obstacleChance = Random.Range( 0.0f, randomRange );
				for ( ; obstacleCount < maxObstacleCount; ++obstacleCount ) {
					obstacleChance -= m_chanceForObstacleCount[obstacleCount];
					if ( obstacleChance <= 0.0f ) {
						break;
					}
				}
			} while ( obstacleCount == lastObstacleCount );

			int obstacleDone = 0;
			for ( int i = 0; i < ( m_chanceForObstacleCount.Length - 1 ) && obstacleDone < obstacleCount; ++i ) {
				float obstaclePosition = currentDistance + i * m_minObstacleDistance;
				if ( obstaclePosition > boostEnd && obstaclePosition < boostSafeEnd ) {
					continue;
				}

				if ( obstaclePosition > boostSafeEnd ) {
					if ( ( m_chanceForObstacleCount.Length - 1 - i ) > ( obstacleCount - obstacleDone ) ) {
						if ( Random.Range( 0, 2 ) == 1 ) {
							// Chance to insert a boost here
							if ( boostCount < m_maxBoostCount ) {
								if ( ( Random.Range( 0.0f, 1.0f ) ) < m_boostChance ) {
									boostCount++;
									GameObject boostInstance = Instantiate( m_boostPrefab );
									Booster booster = boostInstance.GetComponent<Booster>();
									booster.PathPosition = obstaclePosition;
									booster.SetupPosition();

									boostEnd = obstaclePosition + booster.m_boostDistance;
									boostSafeEnd = obstaclePosition + booster.m_boostDistance + m_boostSafeDistance;
								}
							}
							continue;
						}
					}
				}

				obstacleDone++;

				int randomIdx = Random.Range( 0, m_obstaclePrefab.Count );
				GameObject instance = Instantiate( m_obstaclePrefab[randomIdx] );
				Obstacle obstacle = instance.GetComponent<Obstacle>();
				obstacle.SetupPosition( obstaclePosition );
			}
			currentDistance += groupDistance;
		} while ( currentDistance < ( m_spire.PathLength - m_spire.m_flatLengthEnd - groupDistance ) );
	}
}
