using UnityEngine;
 
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;

using System.IO;

using AX.SimpleJSON;




using AXGeometryTools;

using AX.Generators;

using AXClipperLib;
using Path 		= System.Collections.Generic.List<AXClipperLib.IntPoint>;
using Paths 	= System.Collections.Generic.List<System.Collections.Generic.List<AXClipperLib.IntPoint>>;
 

namespace AX {

	[System.Serializable]
	public class Pather  
	{

		public Path path;
		public int[] segment_lengths;




		public static void printPath(Path path)
		{
			foreach(IntPoint ip in path)
				Debug.Log (ip.X+", "+ip.Y);
		}
		public static string pathToString(Path path) 
		{
			string ret = "";
			for (int i = 0; i < path.Count; i++) {
				IntPoint ip = path [i];
				ret += " ["+i+"]\t(" + ip.X + ", " + ip.Y + ")\r";
			}
			return ret;
		}
		public static void printPaths(Paths paths)
		{
			if (paths == null) {
				Debug.Log("print paths: EMPTY");
				return;
			}
			Debug.Log (paths.Count + " paths ------- ");
			int c = 0;
			foreach(Path p in paths)
			{
				Debug.Log ("["+(c++)+"] " + pathToString(p));

			}
			Debug.Log ("end paths ------- ");

		}





		/***********************************
		 From a given vertex, get its previous vertex point.
		 If the given point is the first one, 
		 it will return  the last vertex;
		 ***********************************/
		public IntPoint previousPoint(int index) {		

			if (path.Count > 0)
				return path[( ((index-1)<0) ? (path.Count-1) : index-1)];;
			return new IntPoint();
		}
		public IntPoint nextPoint(int index) {		
			if (path.Count > 0)
				return path[ (index+1) % path.Count ];
			return new IntPoint();
		}

		public static IntRect getBounds (Path path)
		{
			Paths paths = new Paths();
			paths.Add(path);
			return AXClipperLib.Clipper.GetBounds(paths);

		}
		public int[] getSegmentLengths()
		{
			segment_lengths = new int[path.Count];

			int segment_length = 0;

			for (int i=0; i<path.Count; i++) {
					segment_length 		= (int) Distance(previousPoint(i), path[i]);
					segment_lengths[i] 	= segment_length;

					/*
					if (i > 0) 
						curve_distance 	   += segment_length;

					curve_distances[i] 	= curve_distance;
					*/
			}
			return segment_lengths;


		}



		public static int getSegLenBasedOnSubdivision(Path path, int subdivision)
		{	
			if (subdivision <= 0)
				return 10000;
		
			Paths paths = new Paths();
			paths.Add(path);
			return getSegLenBasedOnSubdivision(paths, subdivision);

		}

		public static int getSegLenBasedOnSubdivision(Paths paths, int subdivision)
		{
			if (subdivision <= 0)
				return 10000;

			IntRect bounds = AXClipperLib.Clipper.GetBounds(paths);



			long bot 	= bounds.bottom;
			long top 	= bounds.top;
			long left 	= bounds.left;
			long right 	= bounds.right;



			if (left  > right)
			{
				left = bounds.right;
				right =  bounds.left;
			}
			if (bot > top)
			{
				bot = bounds.top;
				top = bounds.bottom;
			}
			int width 	= (int) Math.Abs(right-left);
			int height 	= (int) Math.Abs(top-bot);

			int max = width > height ? width : height;

			return max / subdivision;



		}


		public static Paths cleanPaths(Paths paths, int t = 10)
		{
			Paths retps = new Paths();

			for(int i=0; i<paths.Count; i++)
			{
				retps.Add(cleanPath(paths[i], t));
			}

			return retps;

		}
		public static Path cleanPath(Path path, int t = 10)
		{
			
			Path retp = new Path();

			int t2 = t*t;

			for(int i=0; i<path.Count-1; i++)
			{
				long d2 = DistanceSquared(path[i], path[i+1]);


				if (d2 < t2)
				{
					
					retp.Add(Lerp(path[i], path[i+1], .5f));
					i++;
				}
				else 
				{
					retp.Add(path[i]);

					if (i == path.Count-2)
						retp.Add(path[i+1]);
				}
			}

			return retp;

		}


		public static Paths segmentPaths(Paths paths, long bay=5000, bool isClosed = true)
		{
			Paths retps = new Paths();

			for (int i=0; i<paths.Count; i++)
			{
				retps.Add(segmentPath(paths[i], bay, isClosed));
				//AXGeometryTools.Utilities.printPath(segmentPath(paths[i], bay, isClosed));
			}
			//AXGeometryTools.Utilities.printPaths(retps);
			return retps;

		}

		public static Path segmentPath(Path path, long bay = 5000, bool isClosed = true)
		{
			Path retp = new Path();

			int endIndex = path.Count-1;

			for (int i=0; i<=endIndex; i++)
			{
				retp.Add(path[i]);

				int next = (i==path.Count-1) ? 0 : i+1;

				if (next == 0 && ! isClosed)
				{
					break;
				}
				//Debug.Log("i="+i+", next="+next);
				long d = Distance(path[i], path[next]);

					if (d > bay)
					{
						//a dd interstitial points
						int steps = (int) (d/bay);



						for (int j=1; j<steps; j++)
						{
							//Debug.Log("- " + j);

							float t = ((float)j)/((float)steps);
							retp.Add(Lerp(path[i], path[next], t));
						}
					}
//				


			}

			//retp.Add(path[path.Count-1]);

			return retp;


		}


		public static IntPoint Lerp(IntPoint a, IntPoint b, float p)
		{
			return new IntPoint( ((b.X-a.X)*p + a.X), ((b.Y-a.Y)*p +a.Y));
		}

		public static long DistanceSquared (IntPoint a, IntPoint b)
		{
			long diffx = Math.Abs(b.X-a.X);
			long diffy = Math.Abs(b.Y-a.Y);

			return  diffx*diffx + diffy*diffy;
		}
		public static long Distance(IntPoint a, IntPoint b)
		{
			return (long) Mathf.Sqrt( (float) Mathf.Pow((b.X-a.X), 2) + (float) Mathf.Pow((b.Y-a.Y), 2) );
		}


		// MANPAGE: http://www.archimatix.com/uncategorized/axspline-getinsetcornerspines
		public Paths getInsetCornerSplinesOLD(float inset)
		{
			//There can't be more subsplines then there are vertices...
			// Each of these subslines will have at least 3 points


			Paths returnPaths; 


			// PLANSWEEPS AT CORNERS
			// First, go around and group verts that are closer
			// to  each other than min_sengmentLength
			float min_sengmentLength = 2*inset;



			// FIND FIRST CURSOR VERTEX

			int cursor = 0; // essentially 0

			if (segment_lengths[cursor] < min_sengmentLength )
			{
				cursor = path.Count-1;

				// back up to first long segment you find
				while (segment_lengths[cursor] < min_sengmentLength )
				{
					if (cursor == 0)
					{
						// if we mad it all the way back to 0, then all the segments were too small.
						// just return this AXSpline
						returnPaths = new Paths();
						returnPaths.Add(path);
						return returnPaths;
					}
					cursor--;
				}

			}

			
			// OK: Now we have our starting point: cursor. 
			// Proceed forward from here with the grouping.

			// Use a single array of ints with -88888 as seperators and -99999 as terminator.
			// Cant have more the 2*verCount entries (a seperator between each vert)

			int[] groupedIndicies = new int[2*path.Count*100];

			int index = 0;
			groupedIndicies[index++] = cursor++;


			int countOfSplines = 1;

			// GROUP VERTS THAT DEFINE THE SUBSPLINES
			while ( (cursor % path.Count) != groupedIndicies[0])
			{
				if (segment_lengths[cursor % path.Count] > min_sengmentLength)
				{
					countOfSplines++;
					groupedIndicies[index++] = -88888; // add break code
				}
				
				groupedIndicies[index++] = cursor % path.Count;

				// starting from cursor, add vertices to subspline

				cursor++;
			}

			// done... add terminator
			groupedIndicies[index++] = -99999;

			   

			// Take each group and add a beginning and ending vertex inset by margin.
			returnPaths = new Paths();


			Path subpath = null;

			for(int j=0; j<groupedIndicies.Length; j++)
			{
				if (j==0 || groupedIndicies[j] == -88888 || groupedIndicies[j] == -99999)
				{
					// End a spline
					if (groupedIndicies[j] == -88888 || groupedIndicies[j] == -99999)
					{ 
						// Add end vert
						int nexti = (groupedIndicies[j-1]+1) % path.Count;
						float percentInset = inset/segment_lengths[nexti];
						IntPoint endVert = Lerp ( path[groupedIndicies[j-1]], nextPoint(groupedIndicies[j-1]), percentInset);
			
						subpath.Add(endVert);
						returnPaths.Add(subpath);

						if (groupedIndicies[j] == -99999)
							break;
					}

					// Begin a spline
					if 	(j==0 || groupedIndicies[j] == -88888)
					{
						// skip over -88888
						if 	(groupedIndicies[j] == -88888) 
							j++;
						// start new AXSpline...
						subpath = new Path();

						//int nexti = (groupedIndicies[j-1]+1) % path.Count;
						float percentInset = inset/segment_lengths[groupedIndicies[j]];

						IntPoint begVert = Lerp (previousPoint(groupedIndicies[j]), path[groupedIndicies[j]], 1-percentInset);
						subpath.Add(begVert);
						subpath.Add(path[groupedIndicies[j]]);
					}
				}
				else
				{
					subpath.Add(path[groupedIndicies[j]]);
				}
			}

			/*
			Debug.Log("===========================");
			for(int j=0; j<groupedIndicies.Length; j++)
			{
				Debug.Log(groupedIndicies[j]);
				if (groupedIndicies[j] == -99999)
					break;
			}

			foreach(Path s in returnPaths)
			{
				Debug.Log("----");
				AXGeometryTools.Utilities.printPath(s);
			}
			*/


			return returnPaths;

		}






		public static bool isConvex(Path path)
		{

			
			for (int i=0; i<path.Count; i++)
			{
				int prev_i = (i==0) 			? path.Count-1 : i-1;
				int next_i = (i==path.Count-1) 	? 			 0 : i+1;

				Vector2 pp = new Vector2(path[prev_i].X, path[prev_i].Y);
				Vector2  p = new Vector2(path[i].X, path[i].Y);
				Vector2 np = new Vector2(path[next_i].X, path[next_i].Y);

				Vector2 v1 	=  p - pp;
				Vector2 v2 	= np - p;


				// NODE ROTATION & TRANSFORM
				Vector2 v1PN = (new Vector2(v1.y, -v1.x)).normalized;
				Vector2 v2PN = (new Vector2(v2.y, -v2.x)).normalized;
				
				// -- BISECTOR: the addition of the normalized perpendicular vectors leads to a bisector
				Vector2 bisector = v1PN + v2PN ;

				float tmp_ang = -Mathf.Atan2(bisector.y, bisector.x)*Mathf.Rad2Deg;
				if (tmp_ang < 0)
					tmp_ang += 360;

				
				// BEVEL ANGLE
				float bevelAng = Vector2.Angle(bisector, v2) - 90;

				if (bevelAng < 0)
					return false;
				

			}

			return true;


		}





	}

} //\AX
