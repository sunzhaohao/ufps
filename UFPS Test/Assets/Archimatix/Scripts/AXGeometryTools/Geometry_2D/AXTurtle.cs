using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using AXClipperLib;
using Path 		= System.Collections.Generic.List<AXClipperLib.IntPoint>;
using Paths 	= System.Collections.Generic.List<System.Collections.Generic.List<AXClipperLib.IntPoint>>;



namespace AXGeometryTools
{
	[System.Serializable]
	public class AXTurtle {

				
		float rotation = 90.0f;
			
		float cos;
		float sin;
			
		float ratio = Mathf.Deg2Rad;// (2*Mathf.PI) / 360;
			


		public AXSpline s;

		public Path  path;

		public Paths paths;
		public Paths clips;

		public PolyTree polytree;

		public Vector2 lastMoveTo;

		public const float minRadius = .0001f;


		//bool drawingHole = false;

		public AXTurtle() {

			updateTrigs();
			dir(0);

			s = new AXSpline();

			paths = new Paths();
			clips = new Paths();

			polytree = new PolyTree();



		}	


		public void startNewPath(bool isSubj) {
			path = new Path();

			if (isSubj)
				paths.Add(path);
			else 
				clips.Add(path);


		}



		public void createBasePolyTreeFromDescription()
		{

			// combine all paths
			polytree = new PolyTree();
			Clipper c = new Clipper(Clipper.ioPreserveCollinear);

			// sort paths for subj and holes


			c.AddPaths(paths, PolyType.ptSubject, true);
			if (clips.Count > 0)
			{
				//Debug.Log ("Have clip");
				c.AddPaths(paths, PolyType.ptClip, true);
			}

			c.Execute(ClipType.ctDifference, polytree, PolyFillType.pftNonZero, PolyFillType.pftNonZero);


		}





		public Path getPath() {
			return path;
		}
		public int sup(float num)
		{
			return (int) (num * AXGeometryTools.Utilities.IntPointPrecision);
		}

		public void updateTrigs() {
			cos = Mathf.Cos(ratio * rotation);
			sin = Mathf.Sin(ratio * rotation);		
		}
		
		
		public void dir(float degs) {
			rotation = degs;
			updateTrigs();
		}
		public void adir(float dx , float dy) {
			dir(Mathf.Atan2(dy, dx)/ratio);
		}
		
		// TURN_L
		public void turnl(float degs) {
			rotation += degs;
			updateTrigs();
		}
		
		// TURN_R
		public void turnr(float degs) {
			rotation -= degs;
			updateTrigs();
		}
		
		
		public void mov(float xx , float yy) {
			
			Vector2 newMov = new Vector2(lastMoveTo.x+xx, lastMoveTo.y+yy);

			// - clipper
			if (path == null || path.Count > 1)
				startNewPath(true);
			else
				path.Clear();

			path.Add( new IntPoint( sup(xx), sup(yy) ) );
			
			lastMoveTo = newMov;
			
		}
		



		/*
		public void movfwd(float l) {


			IntPoint curPt = new IntPoint(); 

			// - clipper
			if (path == null || path.Count > 1)
			{
				curPt = path[path.Count-1];
				startNewPath(true);
			}
				
			path.Add( new IntPoint( curPt.X + sup(l*cos), curPt.Y + sup(l*sin) ) );
		}
		*/

		public void assertPath()
		{
			if (path == null)
			{
				mov(0, 0);
				dir (90);
			}
		}

		public void assertPath(Vector2 a)
		{
			if (path == null)
			{
				mov(a.x, a.y);
				dir (90);
			}
		}
	
		public void fwd(float l , float d=0) {
			if (l==0 && d == 0)
				return;

			assertPath();

			// - clipper 
			if (d == 0)
			{
				IntPoint ip = new IntPoint( path[path.Count-1].X + sup(l*cos), path[path.Count-1].Y + sup(l*sin) );

				if (path.Count > 0 && ip !=  path[path.Count-1])
					path.Add (ip);
			}
			else
			{
				float ang = -Mathf.Atan2(d, l) * Mathf.Rad2Deg;

				turnr(ang);

				fwd (Mathf.Sqrt( l*l + d*d ));

				turnl(ang);

			}

		}
		
		public void movfwd(float l) {
			
			// - clipper 
			IntPoint prev = path[path.Count-1];
			startNewPath(true);
			path.Add (new IntPoint( prev.X + sup(l*cos), prev.Y + sup(l*sin) ));
		}
		public void movfwd(float l , float d) {
			
			// - clipper 
			IntPoint prev = path[path.Count-1];
			startNewPath(true);
			path.Add (new IntPoint( prev.X + sup(l*cos) - sup(d*sin), prev.Y + sup(l*sin)  - sup(d*cos) ));
		}


		public void back(float l, float d=0) {
			if (l==0)
				return;
			
			turnl(180f);
			fwd(l, -d);
			turnr(180f);

		}
		

		public void drw(float xx , float yy) {
			// - axspline


			assertPath();


			IntPoint ip = new IntPoint( sup(xx), sup(yy) );
			if (path.Count > 0 && path[path.Count-1] == ip)
				return;

			path.Add (ip);	
		} 
		
		public void rdrw(float xx , float yy) {
			// - clipper 
			if (xx == 0 && yy == 0)
				return;
 
			path.Add (new IntPoint( path[path.Count-1].X + sup(xx), path[path.Count-1].Y + sup(yy) ));

			dir(getDir(path[path.Count-1], path[path.Count-2]));
		}

		public void rmov(float xx , float yy) {
			// - clipper 
			IntPoint prev = new IntPoint();
			if (path == null)
				mov(0, 0);

			if (path.Count > 0)
				prev = path[path.Count-1];

			startNewPath(true);

			IntPoint newPt = new IntPoint( prev.X + sup(xx), prev.Y + sup(yy) );

			path.Add (newPt);
			//Debug.Log(newPt);
			dir(getDir(path[path.Count-1], prev));
		}






	

		public float getDir(IntPoint fromPt, IntPoint toPt)
		{
			//Debug.Log("dy="+(toPt.Y-fromPt.Y));

			float ang = Mathf.Rad2Deg*(Mathf.Atan2( (fromPt.Y-toPt.Y), (fromPt.X-toPt.X)) );
			//Debug.Log(ang);
			return ang;
		}
		
		public void left(float l) {
			if (l==0)
				return;

			turnl(90f);
			fwd(l);
			turnr(90f);
		}
		public void right(float l) {
			if (l==0)
				return;

			turnr(90f);
			fwd(l);
			turnl(90f);
		} 

		public void arcr(float degs , float radius , int segs) {
			
			arcr(degs, radius, segs, degs);
		}
		public void arcr(float degs , float radius , int segs, float perAngle, int min=1) {

			assertPath();

			// VALIDATION: Assert a min radius due to the inprecision of IntPoints where verts would get lost.
			radius = Mathf.Max(minRadius, radius);


			float sug_dtheta  = perAngle/(float)(segs);
			float seg_actual = Mathf.Floor(degs/sug_dtheta);


			seg_actual = (degs == 360f) ? seg_actual-1 : seg_actual;

			// VALIDATION: Assert that actual_segs can never be less than 1.
			//raise min? Always have at least one seg per 120 degs.
			if (degs > 120 && min<2) min=2;
			if (degs > 240 && min<3) min=3;

			seg_actual = Mathf.Max(seg_actual, min);

			float dtheta  = degs/seg_actual;

			
			//trace("dtheta=" + dtheta);
			float opp = radius * Mathf.Sin(ratio * dtheta);
			float adj = radius - (radius * Mathf.Cos(ratio * dtheta));
			
			float span = Mathf.Sqrt(opp*opp + adj*adj);
			
			turnr(dtheta/2f);
			for (int n=0; n<seg_actual; n++) {
				fwd(span);
				if (n != (seg_actual-1f)) turnr(dtheta);
				
			}
			turnr(dtheta/2f);


			
		}

		public void arcl(float degs , float radius , int segs) {
			arcl(degs, radius, segs, degs);
		}

		public void arcl(float degs , float radius , int segs, float perAngle, int min=1) 
		{
			assertPath();

			// VALIDATION: Assert a min radius due to the inprecision of IntPoints where verts would get lost.
			radius = Mathf.Max(minRadius, radius);

			float sug_dtheta  = perAngle/(float)(segs);
			float seg_actual = Mathf.Floor(degs/sug_dtheta);

				seg_actual = (degs == 360f) ? seg_actual-1 : seg_actual;

			// VALIDATION: Assert that actual_segs can never be less than 1.
			if (degs > 120 && min<2) min=2;
			if (degs > 240 && min<3) min=3;

			seg_actual = Mathf.Max(seg_actual, min);

			float dtheta  = degs/seg_actual;


			//Debug.Log("min="+min+", seg_actual="+seg_actual);
			float opp = radius * Mathf.Sin(ratio * dtheta);
			float adj = radius - (radius * Mathf.Cos(ratio * dtheta));
			
			float span = Mathf.Sqrt(opp*opp + adj*adj);
			
			turnl(dtheta/2f);
			for (int n=0; n<seg_actual; n++) {
				
				fwd(span);
				if (n != (seg_actual-1)) turnl(dtheta);
				
			}
			turnl(dtheta/2f);


		}

		public void bezier(Vector2 a, Vector2 b, Vector2 c, Vector2 d, int segs)
		{
			assertPath(a);

			float dt = 1f/(float)segs;

			for (float i=1; i<=segs; i++)
			{
				Vector2 pt = AXTurtle.bezierValue(a, b, c, d,  i*dt);
				path.Add (new IntPoint(sup(pt.x), sup(pt.y)));
			}


			if (c.x == d.x && c.y != d.y)
			{
				if (c.y > d.y)
					dir(270);
				else
					dir(90);
			}
			else  
				dir( Mathf.Atan2(d.y-c.y, d.x-c.x) * Mathf.Rad2Deg );


		}

		public void molding(string type, Vector2 a, Vector2 b, float segs = 3, float tension = .3f)
		{
			assertPath(a);

			switch(type)
			{
			case "cove":
				cove(a, b, segs, tension);
				break;

			case "ovolo":
				ovolo(a, b, segs, tension);
				break;

			case "cymarecta":
				cymarecta(a, b, segs);
				break;

			case "cymareversa":
				cymareversa(a, b, segs);
				break;

			case "onion":
				onion(a, b, segs, tension);
				break;

			case "dome":
				dome(a, b, segs, tension);
				break;


			}

		}

		public void cove(Vector2 a, Vector2 b, float segs = 3, float tension = .3f)
		{
			float dt = 1f/(float)segs;

			Vector2 d = b-a;


			float hanx = tension*d.x;
			float hany = tension*d.y;

			// bezier 1

			Vector2 ha = new Vector2(a.x,  a.y+hany);
			Vector2 hb = new Vector2(b.x-hanx, 	b.y);



			Vector2 pt;
			for (float i=1; i<=segs; i++)
			{
				pt = AXTurtle.bezierValue(a, ha, hb, b,  i*dt);
				path.Add (new IntPoint(sup(pt.x), sup(pt.y)));
			}

		}
		public void ovolo(Vector2 a, Vector2 b, float segs = 3, float tension = .3f)
		{
			float dt = 1f/(float)segs;

			Vector2 d = b-a;


			float hanx = tension*d.x;
			float hany = tension*d.y;

			// bezier 1

			Vector2 ha = new Vector2(a.x+hanx,  a.y);
			Vector2 hb = new Vector2(b.x, 	b.y-hany);



			Vector2 pt;
			for (float i=1; i<=segs; i++)
			{
				pt = AXTurtle.bezierValue(a, ha, hb, b,  i*dt);
				path.Add (new IntPoint(sup(pt.x), sup(pt.y)));
			}

		}


		public void cymarecta(Vector2 a, Vector2 b, float segs = 3, float tension = .3f)
		{
			float dt = 1f/(float)segs;

			Vector2 d = b-a; 

			Vector2 midpt = a + d/2;

			float hanx = tension*d.x;
			float hany = tension*d.y;

			// bezier 1

			Vector2 ha1 = new Vector2(a.x+hanx,  a.y);
			Vector2 ha2 = new Vector2(midpt.x-hanx/2, 	midpt.y-hany);

			Vector2 pt;
			for (float i=1; i<=segs; i++)
			{
				pt = AXTurtle.bezierValue(a, ha1, ha2, midpt,  i*dt);
				path.Add (new IntPoint(sup(pt.x), sup(pt.y)));
			}

			// bezier 2

			Vector2 hb1 = new Vector2(midpt.x+hanx/2, 		midpt.y+hany);
			Vector2 hb2 = new Vector2(b.x-hanx, 	b.y);

			for (float i=1; i<=segs; i++)
			{
				pt = AXTurtle.bezierValue(midpt, hb1, hb2, b,  i*dt);
				path.Add (new IntPoint(sup(pt.x), sup(pt.y)));
			}

		}



		public void cymareversa(Vector2 a, Vector2 b, float segs = 3, float tension = .3f)
		{
			float dt = 1f/(float)segs;

			Vector2 d = b-a;

			Vector2 midpt = a + d/2;

			float hanx = tension*d.x;
			float hany = tension*d.y;

			Vector2 pt;

			// bezier 1

			Vector2 ha1 = new Vector2(a.x,  a.y+hanx);
			Vector2 ha2 = new Vector2(midpt.x-hanx, 	midpt.y-hany/2);
			for (float i=1; i<=segs; i++)
			{
				pt = AXTurtle.bezierValue(a, ha1, ha2, midpt,  i*dt);
				path.Add (new IntPoint(sup(pt.x), sup(pt.y)));
			}

			// bezier 2

			Vector2 hb1 = new Vector2(midpt.x+hanx, 		midpt.y+hany/2);
			Vector2 hb2 = new Vector2(b.x, 	b.y-hany);
			for (float i=1; i<=segs; i++)
			{
				pt = AXTurtle.bezierValue(midpt, hb1, hb2, b,  i*dt);
				path.Add (new IntPoint(sup(pt.x), sup(pt.y)));
			}

		}


		public void onion (Vector2 a, Vector2 b, float segs=3,  float tension = .9f)
		{


			// VALIDATION: Assert at least 3 sides
			segs = Mathf.Max(segs, 3);

			float dt = 1f/(float)segs;

			Vector2 d = b-a;

			Vector2 midpt = a + d/2;
			midpt.x = a.x - .2f*d.x;
			midpt.y = a.y + .5f*d.y;

			float hanx = 1.2f*Mathf.Abs(tension*d.x);
			float hany = .20f * Mathf.Abs(tension*d.y);



			Vector2 pt;


			// bezier 1

			Vector2 ha1 = new Vector2(a.x+hanx,  a.y);
			Vector2 ha2 = new Vector2(midpt.x+hanx, 	midpt.y-hany);
			for (float i=1; i<=segs; i++)
			{
				pt = AXTurtle.bezierValue(a, ha1, ha2, midpt,  i*dt);
				path.Add (new IntPoint(sup(pt.x), sup(pt.y)));
			}


			// bezier 2

			Vector2 hb1 = new Vector2(midpt.x-hanx, 		midpt.y+hany);
			Vector2 hb2 = new Vector2(b.x, 	b.y-.5f*hany);
			for (float i=1; i<=segs; i++)
			{
				pt = AXTurtle.bezierValue(midpt, hb1, hb2, b,  i*dt);
				path.Add (new IntPoint(sup(pt.x), sup(pt.y)));
			}




		}

		public void dome(Vector2 a, Vector2 b, float segs = 3, float tension = .3f)
		{
			float dt = 1f/(float)segs;

			Vector2 d = b-a;


			float hanx = tension*d.x;
			float hany = tension*d.y;

			// bezier 1

			//Vector2 ha = new Vector2(a.x+hanx,  a.y);
			//Vector2 hb = new Vector2(b.x, 	b.y-hany);

			Vector2 ha = new Vector2(a.x,  a.y+hany);
			Vector2 hb = new Vector2(b.x-hanx, 	b.y);



			Vector2 pt;
			for (float i=1; i<=segs; i++)
			{
				pt = AXTurtle.bezierValue(a, ha, hb, b,  i*dt);
				path.Add (new IntPoint(sup(pt.x), sup(pt.y)));
			}

		}



		public static Path Circle(float radius, int segs)
		{

			// VALIDATION: Assert at least 3 sides
			segs = Mathf.Max(segs, 3);
				
			Path circlePath = new Path();;

									
			// circle spline creation...

			for (int i = 0; i<segs; i++) {
				float rads = Mathf.Deg2Rad*(((float)(i*360))/((float)segs));

				//Debug.Log("rads = "+rads);

				IntPoint pt = new IntPoint((radius*Mathf.Cos(rads) * AXGeometryTools.Utilities.IntPointPrecision), (radius )*Mathf.Sin(rads)* AXGeometryTools.Utilities.IntPointPrecision);
				circlePath.Add (pt);
			}
			return circlePath;

		}

		public static Path Arc(float radius, float begAngle = 0, float endAngle = 270, int segs = 8)
		{
			Path arcPath = new Path();

			float arcAngle = endAngle-begAngle;

			float deltaAngle = arcAngle / (float) segs;

			// circle spline creation...
			for (int i = 0; i<=segs; i++) {
				float rads = Mathf.Deg2Rad*(begAngle + i*deltaAngle);
				IntPoint pt = new IntPoint((radius * AXGeometryTools.Utilities.IntPointPrecision)*Mathf.Cos(rads), (radius * AXGeometryTools.Utilities.IntPointPrecision)*Mathf.Sin(rads));
				arcPath.Add (pt);
			}


			return arcPath;
		}





		// BI_CHAMFER_SIDE

		public static Path BiChamferSide(	float 	H			= 1, 
											float 	R2			= 0, 	
											float 	R1			= 0, 
											int 	SegsPer90	= 3,
											bool 	BevelOut	= true, 
											float 	Taper		= 0, 
											float 	TopLip		= 0, 
											float 	BotLip		= 0,
											float 	LipEdge		= 0,
											float 	LipEdgeBot	= 0,
											int		Segs		= 1
											)
		{

			// DERIVED VARIABLES

			/*
			float o 		= H-(R1+R2);
			float a 		= Mathf.Atan2(o, Taper) * Mathf.Rad2Deg;

			float d 		= Mathf.Sqrt(Taper*Taper + o*o);
			float dr 		= Mathf.Abs(R1-R2);
			float b 		= Mathf.Asin(dr/d) * Mathf.Rad2Deg;

			// his is the slope of the line or straight edge of the extrude side.
			float dir 		= (R2 > R1) ? 180 - (a+b)  : 270 - (a + (90-b));

			float s 		= Mathf.Sqrt(d*d - dr*dr);
			*/



			float o 		= H-(R1+R2);
			float l 		= Taper+R2-R1;

			float a 		= Mathf.Atan2(o, l) * Mathf.Rad2Deg;

			float d 		= Mathf.Sqrt(l*l + o*o);
			float dr 		= Mathf.Abs(R1-R2);
			float s 		= Mathf.Sqrt(d*d - dr*dr);


			float b 		= Mathf.Asin(dr/d) * Mathf.Rad2Deg;

			// his is the slope of the line or straight edge of the extrude side.
			float dir 		= (R2 > R1) ? 180 - (a+b)  : 270 - (a + (90-b));





			// START_X
			//float startX = 0;
			float startX = (R2 > R1) ? R2-R1 : 0;

			startX -= BotLip;

			if (! BevelOut)
				startX -= R1;


			float startY = LipEdgeBot;


			// DRAW SHAPE

			AXTurtle t = new AXTurtle();

			t.mov(startX, startY);
			t.dir(90);

			if (LipEdgeBot != 0)
				t.back(LipEdgeBot);

			t.dir(0);

			if (BotLip > 0)
				t.fwd(BotLip);

			// Chamfer Bottom

			if (R1 > 0)
				t.arcl(dir, R1,  Mathf.FloorToInt((dir/90f)*SegsPer90));
			else 
				t.dir( dir );

			for (int i=0; i<Segs; i++)
				t.fwd(s/Segs);



			// Chamfer Top

			if (R2 > 0)
				t.arcl((180-dir), R2, Mathf.FloorToInt(((180-dir)/90f)*SegsPer90));
			else 
				t.dir(180);

			// TopLip
			if (TopLip > 0)
				t.fwd(TopLip);


			if (LipEdge != 0)
				t.left(LipEdge);


			return t.path;
		}


		public static Vector2  bezierValue(Vector2 pt0, Vector2 pt1, Vector2 pt2, Vector2 pt3, float t)
		{
			
			return 	  Mathf.Pow((1-t), 3)*pt0    +    3*Mathf.Pow((1-t), 2)*t*pt1    +    3*(1-t)*t*t*pt2   +   Mathf.Pow(t, 3)*pt3;

		}
		/*
		public static Vector2  bezierValue(Vector2 pt0, Vector2 pt1, Vector2 pt2, Vector2 pt3, float t)
		{
			return    Mathf.Pow((1-t), 3)*pt0    +    3*Mathf.Pow((1-t), 2)*t*pt1    +    3*(1-t)*t*t*pt2   +   Mathf.Pow(t, 3)*pt3;

		}
		public static Vector2  bezierValue(CurvePoint a, CurvePoint b, float t)
		{
			return    Mathf.Pow((1-t), 3)*a.position    +    3*Mathf.Pow((1-t), 2)*t*(a.position+a.localHandleB)    +    3*(1-t)*t*t*(b.position+b.localHandleA)   +   Mathf.Pow(t, 3)*b.position;

		}
		*/

		
	}

}