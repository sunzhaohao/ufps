using UnityEngine;
using System.Collections;

/* CURVE_POINT
 *
 * For controlling a Bezier curve. Has Tangents.
 */
 
namespace AXGeometryTools
{


	/// <summary>
	/// Curve point.

	// A CurvePoint is a point that has extra data associated with it.
	/// The common use is for a point on a Bezier curcve with handles.
	/// </summary>
	[System.Serializable]
	public class CurvePoint  {


		public CurvePointType curvePointType;

		public Vector2 position;


		public Vector2 localHandleA;
		public Vector2 localHandleB;

		
		public CurvePoint (float _x, float _y)
		{
			position.x = _x;
			position.y = _y;

		}
		public CurvePoint (Vector2 p)
		{
			position = p;
			curvePointType = CurvePointType.Point;
			
		}
		public CurvePoint (Vector2 p, Vector2 a)
		{
			position = p;

			localHandleA = a - position;

			curvePointType = CurvePointType.BezierMirrored;
			localHandleB = -localHandleA;
		}
		public CurvePoint (Vector2 p, Vector2 a, Vector2 b)
		{
			position = p;

			localHandleA = a - position;
			localHandleB = b - position;

		}

		public bool isPoint()
		{
			if (curvePointType == CurvePointType.Point)
				return true;
			return false;
		}
		public bool isBezierPoint()
		{
			if (curvePointType == CurvePointType.BezierBroken || curvePointType == CurvePointType.BezierMirrored || curvePointType == CurvePointType.BezierUnified)
				return true;
			return false;
		}

		public void convertToBezier()
		{
			
			curvePointType = CurvePointType.BezierMirrored;

		}




		public void setHandleA(Vector2 gloabalA)
		{
			localHandleA =  gloabalA - position;

			if (curvePointType == CurvePointType.BezierMirrored)
				localHandleB = -localHandleA; 
				
		}
		public void setHandleB(Vector2 gloabalB)
		{
			localHandleB = gloabalB - position ;

			if (curvePointType == CurvePointType.BezierMirrored)
				localHandleA = -localHandleB; 
				
		}




		public Vector3 asVec3()
		{
			return new Vector3(position.x, 0, position.y);
		}



		
		
	}
}
