using System;
using System.Collections.Generic;
using UnityEngine;

public class Spline {
	public float m_coefficient = 1.0f;
	private float m_curveDistance = 1.0f;

	public List<Vector3> ControlPoints { get; set; } = new List<Vector3>();

	public Vector3 GetTangentAtPercentage( float _percentage, float _gap = float.Epsilon ) {
		Vector3 before;
		Vector3 after;
		Vector3 result;

		if ( _percentage <= _gap ) {
			return ( ControlPoints[ 2 ] - ControlPoints[ 1 ] ).normalized;
		} else if ( _percentage > ( 1.0f - _gap ) ) {
			return ( ControlPoints[ ControlPoints.Count - 3 ] - ControlPoints[ ControlPoints.Count - 4 ] ).normalized;
		}

		before = GetPointAtPct( _percentage - _gap );
		after = GetPointAtPct( _percentage + _gap );

		result = ( after - before ).normalized;

		if ( result.magnitude < _gap ) {
			return GetTangentAtPercentage( _percentage, _gap * 2.0f );
		}

		return result.normalized;
	}

	public Vector3 GetAtDistanceFrom( ref float _percent, float _distance ) {
		Vector3 result = GetPointAtPct( _percent );
		Vector3 lastPoint = result;

		float distanceLeft = _distance;
		while ( distanceLeft > Mathf.Epsilon && _percent < ( 1.0f - Mathf.Epsilon ) ) {
			_percent += 0.0001f;
			_percent = Math.Min( _percent, 1.0f );
			result = GetPointAtPct( _percent );
			distanceLeft -= ( lastPoint - result ).magnitude;
			lastPoint = result;
		}

		return result;
	}

	public Vector3 GetPointAtPct( float _percent ) {
		Vector3 result = Vector3.zero;

		if ( ControlPoints.Count > 2 ) {
			if ( _percent <= float.Epsilon ) {
				result = ControlPoints[ 1 ];
			} else if ( _percent >= ( 1.0f - Mathf.Epsilon ) ) {
				result = ControlPoints[ ControlPoints.Count - 2 ];
			} else {
				int ctrPointCnt = ( ControlPoints.Count - 3 );
				float t = _percent * ctrPointCnt;

				float kf = ( float )Math.Floor( t ) - 1.0f;
				int k = ( int )kf;

				float ck1 = k == -1 ? -m_coefficient : m_coefficient;// m_controlPoints[k + 1].Coefficient;
				float ck2 = m_coefficient;// m_controlPoints[k + 2].Coefficient;

				float kf1 = kf + 1.0f;
				float kf2 = kf + 2.0f;
				float kf3 = kf + 3.0f;
				float curve_distance_factor = 2.0f / ( m_curveDistance * m_curveDistance );

				float a_sum = 0.0f;

				if ( k < ctrPointCnt && k >= 0 ) {
					float a0;
					float t0p = kf1 + ( ( ck1 > 0.0f ) ? ck1 : 0.0f ) * m_curveDistance;
					float qp0 = ( ck1 < 0.0f ) ? -ck1 / 2.0f : 0.0f;
					if ( t <= t0p ) {
						float pm1 = ( kf - t0p ) * ( kf - t0p ) * curve_distance_factor;
						a0 = GetGFunction( ( t - t0p ) / ( kf - t0p ), qp0, pm1 );
					} else {
						a0 = ( qp0 > 0.0f ) ? GetHFunction( ( t - t0p ) / ( kf - t0p ), qp0 ) : 0.0f;
					}

					Vector3 v = ControlPoints[ k + 1 ]; //.Value;
					v *= a0;
					result += v;
					a_sum += a0;
				}

				if ( ( ( k + 1 ) < ctrPointCnt ) && ( ( k + 1 ) >= 0 ) ) {
					float t1p = kf2 + ( ( ck2 > 0.0f ) ? ck2 : 0.0f ) * m_curveDistance;
					float pm0 = ( kf1 - t1p ) * ( kf1 - t1p ) * curve_distance_factor;
					float qp1 = ( ck2 < 0.0f ) ? -ck2 / 2.0f : 0.0f;
					float a1 = GetGFunction( ( t - t1p ) / ( kf1 - t1p ), qp1, pm0 );

					Vector3 v = ControlPoints[ k + 2 ]; //.Value;
					v *= a1;
					result += v;
					a_sum += a1;
				}

				if ( ( ( k + 2 ) < ctrPointCnt ) && ( ( k + 2 ) >= 0 ) ) {
					float t2m = kf1 - ( ( ck1 > 0.0f ) ? ck1 : 0.0f ) * m_curveDistance;
					float pp1 = ( kf2 - t2m ) * ( kf2 - t2m ) * curve_distance_factor;
					float qp2 = ( ck1 < 0.0f ) ? -ck1 / 2.0f : 0.0f;
					float a2 = GetGFunction( ( t - t2m ) / ( kf2 - t2m ), qp2, pp1 );

					Vector3 v = ControlPoints[ k + 3 ]; //.Value;
					v *= a2;
					result += v;
					a_sum += a2;
				}

				if ( ( ( k + 3 ) < ctrPointCnt ) && ( ( k + 3 ) >= 0 ) ) {
					float t3m = kf2 - ( ( ck2 > 0.0f ) ? ck2 : 0.0f ) * m_curveDistance;
					float qp3 = ( ck2 < 0.0f ) ? -ck2 / 2.0f : 0.0f;
					float a3;
					if ( t >= t3m ) {
						float pp2 = ( kf3 - t3m ) * ( kf3 - t3m ) * curve_distance_factor;
						a3 = GetGFunction( ( t - t3m ) / ( kf3 - t3m ), qp3, pp2 );
					} else {
						a3 = ( qp3 > 0.0f ) ? GetHFunction( ( t - t3m ) / ( kf3 - t3m ), qp3 ) : 0.0f;
					}

					Vector3 v = ControlPoints[ k + 4 ]; //.Value;
					v *= a3;
					result += v;
					a_sum += a3;
				}

				result /= a_sum;
			}
		}
		return result;
	}

	private float GetGFunction( float u, float q, float p ) {
		return
			q * u
			+ 2.0f * q * u * u
			+ ( 10.0f - 12.0f * q - p ) * u * u * u
			+ ( 2.0f * p + 14.0f * q - 15.0f ) * u * u * u * u
			+ ( 6.0f - 5.0f * q - p ) * u * u * u * u * u;
	}

	private float GetHFunction( float u, float q ) {
		return
			q * u
			+ 2 * q * u * u
			- 2 * q * u * u * u * u
			- q * u * u * u * u * u;
	}
}
