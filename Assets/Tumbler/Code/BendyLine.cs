// TUMBLER
// Copyright (c) 2020 Ted Brown

using Jambox;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tumbler
{
	public class BendyLine : MonoBehaviour 
	{
		public class BezierLinePoint
		{
			public Vector3 ControlPoint1;
			public Vector3 ControlPoint2;
			public Vector3 LinePoint;
		}

		public static float CurveLerpSpeed = 0.4f;
		public static float CurveHeightScalar = 1;

		// mostly transparent blue to full transparent
		public Gradient _noTargetGradient = new Gradient() 
		{
			colorKeys = new GradientColorKey[2] {
				// Add your colour and specify the stop point
				new GradientColorKey(new Color(0, 0.8f, 1), 0),
				new GradientColorKey(new Color(1, 1, 1), 1)
			},
			alphaKeys = new GradientAlphaKey[2] {
				new GradientAlphaKey(0.5f, 0),
				new GradientAlphaKey(0, 1)
			}
		};

		// green to semi-transparent blue
		public Gradient _validTargetGradient = new Gradient() 
		{
			colorKeys = new GradientColorKey[2] {
				new GradientColorKey(new Color(0, 1, 0), 0),
				new GradientColorKey(new Color(0.25f, 0.9f, 1), 1)
			},
			alphaKeys = new GradientAlphaKey[2] {
				new GradientAlphaKey(1, 0),
				new GradientAlphaKey(0.75f, 1)
			}
		};

		private BezierLinePoint[] _points;
		private float _curveHeight;
		private LineRenderer _lineRenderer;
		private Vector3 _startPosition;
		private Vector3 _endPosition;

		/// <summary>
		/// returns a point along a cubic bezier spline's first derivative
		/// </summary>
		/// <param name="p0">begin point</param>
		/// <param name="p1">control point 1</param>
		/// <param name="p2">control point 2</param>
		/// <param name="p3">end point</param>
		/// <param name="t">offset along the line, 0-1</param>
		/// <returns></returns>
		public static Vector3 DeriveCubicBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
		{
			t = Mathf.Clamp01(t);
			float oneMinusT = 1 - t;
			float tSquare = t * t;
			float b0 = -3 * oneMinusT * oneMinusT;
			float b1 = (3 * oneMinusT * oneMinusT - 6 * t * oneMinusT);
			float b2 = (6 * t * oneMinusT - 3 * tSquare);
			float b3 = 3 * tSquare;
			return b0 * p0 + b1 * p1 + b2 * p2 + b3 * p3;
		}

		/// <summary>
		/// returns a point along a quadratic bezier spline
		/// </summary>
		/// <param name="p0">begin point</param>
		/// <param name="p1">control point</param>
		/// <param name="p3">end point</param>
		/// <param name="t">offset along the line, 0-1</param>
		/// <returns></returns>
		/// 
		public static Vector3 DeriveQuadraticBezier(Vector3 p0, Vector3 p1, Vector3 p2, float t)
		{
			float oneMinusT = 1f - t;
			return oneMinusT * oneMinusT * p0 + 2f * t * oneMinusT * p1 + t * t * p2;
		}

		public void HandleNoTarget ()
		{
			_lineRenderer.colorGradient = _noTargetGradient;
		}

		public void HandleValidTarget ()
		{
			_lineRenderer.colorGradient = _validTargetGradient;
		}

		public void SetExtents (Vector3 start, Vector3 end)
		{
			_startPosition = start;
			_endPosition = end;

			//_lineRenderer.SetPosition(0, start);
			//_lineRenderer.SetPosition(1, end);

			//DrawCurvedLine(start, end);
			DrawStraightLine(start, end);
		}

		public void DrawStraightLine (Vector3 start, Vector3 end)
		{
			_lineRenderer.positionCount = 2;
			_lineRenderer.SetPositions(new Vector3[] { start, end });
		}

		public void DrawCurvedLine (Vector3 start, Vector3 targetPosition, Vector3 end)
		{
			// Delta between target and end position
			float delta = Vector3.Distance(targetPosition, end);

			// Use the distance to set the curve height and imply potential energy
			float targetCurveHeight = delta * CurveHeightScalar;

			// If the distance to the target is shorter than the distance to the object,
			// then straighten it out. Pulling shouldn't have the same visual effect as pushing.
			if (Vector3.Distance(start, targetPosition) < Vector3.Distance(start, end))
			{
				targetCurveHeight = 0;
			}

			// Lerp so small adjustments don't cause the height to jump.
			_curveHeight = Mathf.Lerp(_curveHeight, targetCurveHeight, CurveLerpSpeed);

			// Direction between target and end
			Vector3 dir = (targetPosition - end).normalized;

			float halfDistanceToObject = Vector3.Distance(start, end) / 2;

			_points[0].LinePoint = start;
			_points[0].ControlPoint2 = start + transform.forward * halfDistanceToObject + dir * _curveHeight;
			_points[1].LinePoint = end;

			// determine the number of points that we need for the line renderer
			int rendererPoints = _points.Length;
			for (int i = 0; i < _points.Length - 1; i++)
			{
				rendererPoints += GetNumberOfPointsForSpan(_points[i].LinePoint, _points[i + 1].LinePoint);
			}

			// need to clamp to make sure this doesn't go below zero, was happening in some situations
			rendererPoints = Mathf.Max(0, rendererPoints);
			if (_lineRenderer.positionCount != rendererPoints)
			{
				_lineRenderer.positionCount = rendererPoints;
			}

			int lineRendererIndex = 0;
			
			for (int i = 0; i < _points.Length - 1; i++)
			{
				_lineRenderer.SetPosition(lineRendererIndex, _points[i].LinePoint);
				int nPointsForSpan = GetNumberOfPointsForSpan(_points[i].LinePoint, _points[i + 1].LinePoint);
				for (int j = 0; j < nPointsForSpan; j++)
				{
					lineRendererIndex++;
					_lineRenderer.SetPosition(lineRendererIndex,
						DeriveQuadraticBezier(_points[i].LinePoint, _points[i].ControlPoint2, _points[i + 1].LinePoint, (j + 1) / (float)(nPointsForSpan + 1)));
				}
				lineRendererIndex++;
			}

			_lineRenderer.SetPosition(lineRendererIndex, _points[_points.Length - 1].LinePoint);			
		}

		public void DrawCurvedLineV1 (Vector3 start, Vector3 targetPosition, Vector3 end)
		{
			// Draw a curved line, the total length of which should approximate the distance between the pointer and move target.
			float halfDistanceToTarget = Vector3.Distance(start, targetPosition) / 2;
			float halfDistanceToObject = Vector3.Distance(start, end) / 2;
			float targetCurveHeight = 0;

			float delta = halfDistanceToTarget - halfDistanceToObject;

			//if (halfDistanceToTarget > halfDistanceToObject)
			{
				// use pythagorean theorem to get height of bezier curve
				// (a) height
				// (b) distance to object / 2
				// (c) distance to target / 2
				// a = sqrt(c^2 - b^2)
				targetCurveHeight = Mathf.Sqrt((halfDistanceToTarget * halfDistanceToTarget) - (halfDistanceToObject * halfDistanceToObject));

				// shrink it down a bit to approximate the extra distance added by the arc
				//targetCurveHeight *= 0.8f;

				float d = Vector3.Distance(targetPosition, end);
				targetCurveHeight = Mathf.Clamp(d, 0, 2);
			}

			// Lerp so small adjustments don't cause the height to jump.
			_curveHeight = Mathf.Lerp(_curveHeight, targetCurveHeight, CurveLerpSpeed);
			//_curveHeight = 0;

			// Vector3 aimDirection = transform.rotation * _aimDirection;

			_points[0].LinePoint = start;
//			_points[0].ControlPoint2 = start + aimDirection * halfDistanceToObject + transform.up * _curveHeight;
			_points[0].ControlPoint2 = start + transform.forward * halfDistanceToObject + transform.up * _curveHeight;
			_points[1].LinePoint = end;


			// determine the number of points that we need for the line renderer
			int rendererPoints = _points.Length;
			for (int i = 0; i < _points.Length - 1; i++)
			{
				rendererPoints += GetNumberOfPointsForSpan(_points[i].LinePoint, _points[i + 1].LinePoint);
			}
			// need to clamp to make sure this doesn't go below zero, was happening in some situations
			rendererPoints = Mathf.Max(0, rendererPoints);
			if (_lineRenderer.positionCount != rendererPoints)
			{
				_lineRenderer.positionCount = rendererPoints;
			}

			int lineRendererIndex = 0;
//			_lineRenderer.positionCount = 8;
			
			for (int i = 0; i < _points.Length - 1; i++)
			{
				_lineRenderer.SetPosition(lineRendererIndex, _points[i].LinePoint);
				int nPointsForSpan = GetNumberOfPointsForSpan(_points[i].LinePoint, _points[i + 1].LinePoint);
				for (int j = 0; j < nPointsForSpan; j++)
				{
					lineRendererIndex++;
					_lineRenderer.SetPosition(lineRendererIndex,
						//DeriveCubicBezier(_points[i].LinePoint, _points[i].ControlPoint2, _points[i + 1].ControlPoint1, _points[i + 1].LinePoint, (j + 1) / (float)(nPointsForSpan + 1)));
						DeriveQuadraticBezier(_points[i].LinePoint, _points[i].ControlPoint2, _points[i + 1].LinePoint, (j + 1) / (float)(nPointsForSpan + 1)));
				}
				lineRendererIndex++;
			}
			_lineRenderer.SetPosition(lineRendererIndex, _points[_points.Length - 1].LinePoint);			
		}

		private int GetNumberOfPointsForSpan(Vector3 a, Vector3 b)
		{
			const int MAXIMUM_POINTS_PER_SPAN = 500;
			const int MAXIMUM_SPAN_MAGNITUDE = 100000;
			float _pointDensity = 6f; // 0.6f

			float magnitude = _pointDensity * (b - a).magnitude;
			// if we have an absurdly large magitude, then clamp it down to zero, something has gone wrong.
			if(magnitude > MAXIMUM_SPAN_MAGNITUDE)
			{
				return 0;
			}
			// requires a clamp to be above zero, due to degenerate cases where FloorToInt appears to produce -1
			// also clamp the maximum number of points per span so it doesn't get out of control for long line segments
			return Mathf.Clamp(Mathf.FloorToInt(magnitude),0,MAXIMUM_POINTS_PER_SPAN);
		}

		protected void Awake ()
		{
			_lineRenderer = GetComponent<LineRenderer>();
			_lineRenderer.positionCount = 8;

			_points = new BezierLinePoint[2] {
				new BezierLinePoint(),
				new BezierLinePoint()
			};
		}
	}
}
