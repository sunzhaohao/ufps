#pragma warning disable 0618 // SphereCap obsolete - use SphereHandleCap

using UnityEngine;
using UnityEditor;

using System;
using System.Collections; 
using System.Collections.Generic;

using AXClipperLib;

using Path 		= System.Collections.Generic.List<AXClipperLib.IntPoint>;
using Paths 	= System.Collections.Generic.List<System.Collections.Generic.List<AXClipperLib.IntPoint>>;


using AXGeometryTools;

using AX;
using AX.Generators;
using AXEditor;

using Parameters 				= System.Collections.Generic.List<AX.AXParameter>;



namespace AX.GeneratorHandlers
{

	
	public class FreeCurveHandler : GeneratorHandler2D 
	{
		
		[System.NonSerialized]
		public int movingMidhandle = 0;
		

		public List<Vector2> handlePoints;


	



		public override void drawControlHandles(ref List<string> visited, Matrix4x4 consumerM, bool beingDrawnFromConsumer)
		{



			
			Matrix4x4 prevHandlesMatrix = Handles.matrix;

			FreeCurve gener = (FreeCurve) parametricObject.generator;




			base.drawControlHandles(ref visited, consumerM,  true);

			if (alreadyVisited(ref visited, "FreeCurveHandler"))
				return;


		
			AXParameter p = parametricObject.getParameter("Output Shape");
			
			if (p == null || p.getPaths() == null)
				return;
			

			parametricObject.model.addActiveFreeCurve(parametricObject);

			Event e = Event.current;

			if (ArchimatixEngine.sceneViewState == ArchimatixEngine.SceneViewState.AddPoint && e.type == EventType.keyDown && (e.keyCode == KeyCode.Escape || e.keyCode == KeyCode.Return))
			{
				

				ArchimatixEngine.setSceneViewState(ArchimatixEngine.SceneViewState.Default);

			
				e.Use();

			}


			handlePoints = new List<Vector2>();


			bool handleHasChanged = false;
			
			/*
			Matrix4x4 context 		= parametricObject.model.transform.localToWorldMatrix * generator.parametricObject.worldDisplayMatrix;
			if (generator.hasOutputsConnected() || parametricObject.is2D())
				context *= generator.localMatrix.inverse;
			else
				context *= parametricObject.getAxisRotationMatrix().inverse  * generator.localMatrix.inverse * parametricObject.getAxisRotationMatrix();

			Handles.matrix = context;
			*/
			Handles.matrix = parametricObject.model.transform.localToWorldMatrix * generator.parametricObject.worldDisplayMatrix;// * generator.localMatrix.inverse;


			float gridDim = parametricObject.model.snapSizeGrid * 100;

			// axis
			Handles.color = Color.red;
			Handles.DrawLine(new Vector3(-gridDim/2, 0, 0), new Vector3(gridDim/2, 0, 0));
			Handles.color = Color.green;
			Handles.DrawLine(new Vector3(0, -gridDim/2, 0), new Vector3(0, gridDim/2, 0));
			
			// grid

			if (ArchimatixEngine.snappingOn())
				Handles.color = new Color(1, .5f, .65f, .15f);
			else
				Handles.color = new Color(1, .5f, .65f, .05f);


			AXEditorUtilities.DrawGrid3D(gridDim, parametricObject.model.snapSizeGrid);
			
			

			
			//Handles.matrix = Matrix4x4.identity;

			CurvePoint newCurvePoint = null;; 
			int newCurvePointIndex = -1;

			if (parametricObject.curve != null)
			{
				//if (Event.current.type == EventType.mouseDown)
				//	selectedIndex = -1;
				
				//Vector3 pos;


				
				for (int i=0; i<parametricObject.curve.Count; i++)
				{
					//Debug.Log (i + ": "+ parametricObject.curve[i].position);
					
					// Control points in Curve

					bool pointIsSelected = (generator.selectedIndices != null && generator.selectedIndices.Contains(i));



					Vector3 pos = new Vector3(parametricObject.curve[i].position.x, parametricObject.curve[i].position.y, 0);




					Handles.color = (pointIsSelected) ? Color.white :  Color.magenta;

					float capSize = .13f*HandleUtility.GetHandleSize(pos);

					if (pointIsSelected)
					{
						capSize = .17f*HandleUtility.GetHandleSize(pos);
					}	



					 


					// POSITION
					//pos = new Vector3(parametricObject.curve[i].position.x, parametricObject.curve[i].position.y, 0);

//					pos = Handles.FreeMoveHandle(
//						pos, 
//						Quaternion.identity,
//						capSize,
//						Vector3.zero, 
//						(controlID, positione, rotation, size) =>
//					{
//						if (GUIUtility.hotControl > 0 && controlID == GUIUtility.hotControl)
//						Debug.Log("YOP");
//						Handles.SphereCap(controlID, positione, rotation, size);
//					});

					
					pos = Handles.FreeMoveHandle(
						pos, 
						Quaternion.identity,
						capSize,
						Vector3.zero, 
						(controlID, position, rotation, size) =>
						{ 
							
							if (GUIUtility.hotControl > 0 && controlID == GUIUtility.hotControl)
							{

								//Debug.Log("*** " + e.type + " -" + e.keyCode + "-");

								// MOUSE DOWN ON HANDLE!

								Undo.RegisterCompleteObjectUndo (parametricObject.model, "FreeCurve");
								//Debug.Log(controlID + ": " + e.type);

								ArchimatixEngine.selectedFreeCurve = gener;

								//Debug.Log("SELECT NODE " +i + " ci="+controlID);

								if (i == 0 && ArchimatixEngine.sceneViewState == ArchimatixEngine.SceneViewState.AddPoint)
								{
									

									generator.P_Output.shapeState = ShapeState.Closed;
									ArchimatixEngine.setSceneViewState(ArchimatixEngine.SceneViewState.Default);

								}
								else if (e.shift && ! ArchimatixEngine.mouseIsDownOnHandle)
									generator.toggleItem(i);
								
								else if (gener.selectedIndices == null || gener.selectedIndices.Count < 2)
								{
									if (! generator.isSelected(i))
										generator.selectOnlyItem(i);
									
								}
								ArchimatixEngine.isPseudoDraggingSelectedPoint = i;

																// CONVERT TO BEZIER
								if (e.alt)
									gener.convertToBezier(i);
								


								for (int j = 0; j < generator.P_Output.Dependents.Count; j++) 
									generator.P_Output.Dependents [j].parametricObject.generator.adjustWorldMatrices ();


								ArchimatixEngine.mouseDownOnSceneViewHandle();


							}


							Handles.SphereCap(controlID, position, rotation, size);
						});










					
					// MID_SEGEMNET HANDLE

					if (i < parametricObject.curve.Count)
					{
						//Handles.matrix = parametricObject.model.transform.localToWorldMatrix * generator.parametricObject.worldDisplayMatrix * generator.localMatrix.inverse;

						Handles.color = Color.cyan;

						//Debug.Log("mid handle "+i);
						 
						CurvePoint a = parametricObject.curve[i];

						int next_i = (i==parametricObject.curve.Count-1) ? 0 : i+1;

						CurvePoint b = parametricObject.curve[next_i];

						if (a.isPoint() && b.isPoint())
						{
							pos = Vector2.Lerp(a.position, b.position, .5f);
						}
						else 
						{
							Vector2 pt =  FreeCurve.bezierValue(a, b, .5f);
							pos = (Vector3) pt;
						}

						EditorGUI.BeginChangeCheck();
						 
						#if UNITY_5_6_OR_NEWER
						pos = Handles.FreeMoveHandle(
							pos, 
							Quaternion.identity, 
							.06f*HandleUtility.GetHandleSize(pos),
							Vector3.zero,
							(controlID, positione, rotation, size, eventType) =>
							{ 
								if (GUIUtility.hotControl > 0 && controlID == GUIUtility.hotControl)
									ArchimatixEngine.selectedFreeCurve = gener;
								Handles.CubeHandleCap(controlID, positione, rotation, size, eventType);
							});
						#else
						pos = Handles.FreeMoveHandle(
							pos, 
							Quaternion.identity, 
							.06f*HandleUtility.GetHandleSize(pos),
							Vector3.zero,
							(controlID, positione, rotation, size) =>
							{ 
								if (GUIUtility.hotControl > 0 && controlID == GUIUtility.hotControl)
									ArchimatixEngine.selectedFreeCurve = gener;
								Handles.CubeCap(controlID, positione, rotation, size);
							});

						#endif 
						
						if(EditorGUI.EndChangeCheck())
						{
							 
							// add point to spline at i using pos.x, pos.y
							Undo.RegisterCompleteObjectUndo (parametricObject.model, "New Midpoint");

							//Debug.Log(pos);
							//Debug.Log(ArchimatixEngine.isPseudoDraggingSelectedPoint + " ::: " + (i));

							//if (ArchimatixEngine.isPseudoDraggingSelectedPoint != (i+1))
							if (ArchimatixEngine.isPseudoDraggingSelectedPoint == -1)
							{
								//Debug.Log("CREATE!!!!");
								newCurvePoint = new CurvePoint(pos.x, pos.y);

								newCurvePointIndex= i+1;

								parametricObject.curve.Insert(newCurvePointIndex, newCurvePoint);
								ArchimatixEngine.isPseudoDraggingSelectedPoint = newCurvePointIndex;
								generator.selectedIndex = newCurvePointIndex;

								generator.selectOnlyItem(newCurvePointIndex);
							}

							parametricObject.model.isAltered();
						}


					}
					  









					
				} // \loop	
				

				// BEZIER HANDLES LOOP
				for (int i=0; i<parametricObject.curve.Count; i++)
				{
					//Debug.Log (i + ": "+ parametricObject.curve[i].position);

					// Control points in Curve

					bool pointIsSelected = (generator.selectedIndices != null && generator.selectedIndices.Contains(i));



					Vector3 pos = new Vector3(parametricObject.curve[i].position.x, parametricObject.curve[i].position.y, 0);
					Vector3 posA = new Vector3(parametricObject.curve[i].position.x+parametricObject.curve[i].localHandleA.x, parametricObject.curve[i].position.y+parametricObject.curve[i].localHandleA.y, 0);
					Vector3 posB = new Vector3(parametricObject.curve[i].position.x+parametricObject.curve[i].localHandleB.x, parametricObject.curve[i].position.y+parametricObject.curve[i].localHandleB.y, 0);


					Handles.color = (pointIsSelected) ? Color.white :  Color.magenta;



					if (pointIsSelected)
					{



						Handles.color = Color.magenta;

						if ( parametricObject.curve[i].isBezierPoint())
						{
							Handles.color = Color.white;
							Handles.DrawLine(pos, posA);
							Handles.DrawLine(pos, posB);




							EditorGUI.BeginChangeCheck();
							posA = Handles.FreeMoveHandle(
								posA, 
								Quaternion.identity,
								.1f*HandleUtility.GetHandleSize(pos),
								Vector3.zero, 
								Handles.SphereCap
							);

							if(EditorGUI.EndChangeCheck())
							{
								Undo.RegisterCompleteObjectUndo (parametricObject.model, "FreeformShapee");
								handleHasChanged = true;

								parametricObject.curve[i].setHandleA(new Vector2(posA.x, posA.y));




								//parametricObject.curve[i].localHandleA = new Vector2(pos.x, pos.y) - parametricObject.curve[i].position;
								//parametricObject.model.generate("Move FreeForm Shape Handle");
								parametricObject.model.isAltered();

							}





							// HANDLE_B


							EditorGUI.BeginChangeCheck();
							posB = Handles.FreeMoveHandle(
								posB, 
								Quaternion.identity,
								.1f*HandleUtility.GetHandleSize(pos),
								Vector3.zero, 
								Handles.SphereCap
							);

							if(EditorGUI.EndChangeCheck())
							{
								Undo.RegisterCompleteObjectUndo (parametricObject.model, "FreeformShapee");
								handleHasChanged = true;
								//parametricObject.curve[i].localHandleB = new Vector2(pos.x, pos.y) - parametricObject.curve[i].position;
								parametricObject.curve[i].setHandleB(new Vector2(posB.x, posB.y));



								//parametricObject.model.generate("Move FreeForm Shape Handle");
								parametricObject.model.isAltered();

							}


						}
					} // selected



				} // \bezier handles loop




				if (handleHasChanged)
				{
					
				}
			}
			
			Handles.matrix = prevHandlesMatrix;
			
		}



		public void drawGUIControls()
		{
			//FreeCurve gener = (FreeCurve) generator;

			//AXModel model = ArchimatixEngine.currentModel;


			// 2D GUI
			Handles.BeginGUI();

			GUIStyle buttonStyle = GUI.skin.GetStyle ("Button");
			buttonStyle.alignment = TextAnchor.MiddleCenter;

			float prevFixedWidth = buttonStyle.fixedWidth;
			float prevFixedHeight = buttonStyle.fixedHeight;

			buttonStyle.fixedWidth = 100;
			buttonStyle.fixedHeight = 30;

			//GUIStyle areaStyle = new GUIStyle();
			//areaStyle. = MakeTex(600, 1, new Color(1.0f, 1.0f, 1.0f, 0.1f));


			EditorGUIUtility.labelWidth = 40;



			Handles.EndGUI();




			buttonStyle.fixedWidth = prevFixedWidth;
			buttonStyle.fixedHeight = prevFixedHeight;


		}





		public override void OnSceneGUI()
		{
			

			AXModel model = ArchimatixEngine.currentModel;


			FreeCurve gener = (FreeCurve) generator;

			Event e = Event.current;




			if (model != null)
			{
				Matrix4x4 context = parametricObject.model.transform.localToWorldMatrix * generator.parametricObject.worldDisplayMatrix;// * generator.localMatrix;



				drawGUIControls();



				if((ArchimatixEngine.sceneViewState == ArchimatixEngine.SceneViewState.AddPoint || (e.type == EventType.mouseDrag && ArchimatixEngine.isPseudoDraggingSelectedPoint >= 0))   && ! e.alt) 
				{



					Vector3 v1 = context.MultiplyPoint3x4(Vector3.zero);
					Vector3 v2 = context.MultiplyPoint3x4(new Vector3( 100, 0, 0));
					Vector3 v3 = context.MultiplyPoint3x4(new Vector3(0, 100, 0));
					
					//Debug.Log ("-"+ArchimatixEngine.sceneViewState + "- plane points: " + v1 + ", " + v2+ ", " + v3 );


					Plane drawingSurface = new Plane( v1, v2, v3);


					//Matrix4x4 = parametricObject.model.transform.localToWorldMatrix * generator.parametricObject.worldDisplayMatrix;// * generator.localMatrix.inverse;



					Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);		

					float rayDistance = 0;
					Vector3 hitPoint;




					// Point on Plane
					if (drawingSurface.Raycast(ray, out rayDistance)) 
					{
						hitPoint = ray.GetPoint(rayDistance);


						Vector3 hit_position3D = new Vector3(hitPoint.x, hitPoint.y, hitPoint.z);

 

					

						if ( ArchimatixEngine.snappingOn() )
							hit_position3D = AXGeometryTools.Utilities.SnapToGrid(hit_position3D, parametricObject.model.snapSizeGrid);




						int nearId = HandleUtility.nearestControl;

						if (nearId == 0)
						{


							Color cyan = Color.cyan;
							cyan.a = .1f;

							float hScale = .09f*HandleUtility.GetHandleSize(hit_position3D);
							float lScale = 5*hScale;

							Handles.color = cyan;
							Handles.DrawSolidDisc(hit_position3D, 
							                      Vector3.up, 						                  
													hScale);

							cyan.a = .8f;
							Handles.color = cyan;
							Handles.DrawWireDisc(hit_position3D, 
							                      Vector3.up, 						                  
													2*hScale);

							
							Handles.DrawLine(hit_position3D+lScale*Vector3.forward, hit_position3D-lScale*Vector3.forward);

							//Handles.color = Color.white;
							Handles.DrawLine(hit_position3D+lScale*Vector3.right, hit_position3D-lScale*Vector3.right);

																		
							// put visual cue under mouse....
							//Debug.Log (hitPoint);

						}



						hitPoint = context.inverse.MultiplyPoint3x4(hitPoint);

						Vector2 hit_position2D = new Vector2(hitPoint.x, hitPoint.y);

						if (ArchimatixEngine.snappingOn())
							hit_position2D = AXGeometryTools.Utilities.SnapToGrid(hit_position2D, parametricObject.model.snapSizeGrid);


						// EVENTS 

						if (e.type == EventType.MouseDown && ! e.alt && nearId == 0 && e.button == 0)
						{

							ArchimatixEngine.isPseudoDraggingSelectedPoint = -1;

							if (gener != null)
							{ 
								if (! e.control) // add to end of line
								{
									// ADD POINT AT END

									gener.parametricObject.curve.Add(new CurvePoint(hit_position2D.x, hit_position2D.y));
									gener.selectedIndex = gener.parametricObject.curve.Count-1;
								}
								else // ADD POINT TO BEGINNING
								{
									gener.parametricObject.curve.Insert(0, new CurvePoint(hit_position2D.x, hit_position2D.y));
									gener.selectedIndex = 0;
								}
								
								
								ArchimatixEngine.isPseudoDraggingSelectedPoint = gener.selectedIndex;
								model.autobuild();
								
							}
						}
						else if (e.type == EventType.mouseDrag)
						{
							//Debug.Log("Dragging "+ ArchimatixEngine.isPseudoDraggingSelectedPoint + " :: " +  generator.selectedIndices);

							if( gener == ArchimatixEngine.selectedFreeCurve &&  ArchimatixEngine.isPseudoDraggingSelectedPoint >= 0)
							{
								if (gener.parametricObject.curve.Count > ArchimatixEngine.isPseudoDraggingSelectedPoint && generator.selectedIndices != null)
								{

									// The actual point being dragged:
									Vector2 displ = hit_position2D - gener.parametricObject.curve[ArchimatixEngine.isPseudoDraggingSelectedPoint].position;
									gener.parametricObject.curve[ArchimatixEngine.isPseudoDraggingSelectedPoint].position = hit_position2D;


									for (int i=0; i<model.activeFreeCurves.Count; i++)
									{ 
										FreeCurve fc = (FreeCurve) model.activeFreeCurves[i].generator;

										if (fc != null && fc.selectedIndices != null)
										{
											for (int j=0; j<fc.selectedIndices.Count; j++)
											{
												if (! (fc == gener  &&  fc.selectedIndices[j] == ArchimatixEngine.isPseudoDraggingSelectedPoint) )
												{
													fc.parametricObject.curve[fc.selectedIndices[j]].position += displ;
												}
											}
										}
									} 

									//Debug.Log ("DRAGGING");
									parametricObject.setAltered();
									model.isAltered();
									generator.adjustWorldMatrices();
								}
							}	
								
						 
							
							
						} 
						if (e.type == EventType.mouseUp)
						{
							//if (ArchimatixEngine.isPseudoDraggingSelectedPoint > -1)
								

							ArchimatixEngine.isPseudoDraggingSelectedPoint = -1;
							ArchimatixEngine.draggingNewPointAt = -1;
							//model.autobuild();

							//Debug.Log("mouse up");
						}


						if ((e.type == EventType.mouseDown) || (e.type == EventType.mouseDrag && ArchimatixEngine.isPseudoDraggingSelectedPoint >= 0)  || (e.type == EventType.mouseUp))
						{

							
							//e.Use();
						}

					

						SceneView sv = SceneView.lastActiveSceneView;
						if (sv != null)
							sv.Repaint();

					}

				}

			
			}
		} // \OnScenView








		public bool isNearHandle(Vector3 pt, float nearDist = .01f)
		{
			

			if (parametricObject.curve != null)
			{
				//if (Event.current.type == EventType.mouseDown)
				//	selectedIndex = -1;
				
				//Vector3 pos;
				
				for (int i=0; i<parametricObject.curve.Count; i++)
				{
					//Debug.Log (i + ": "+ parametricObject.spline.verts[i]);
					
					// Control points in Curve


					Vector3 pos  = new Vector3(parametricObject.curve[i].position.x, parametricObject.curve[i].position.y, 0);
					if (Vector3.Distance(pt, pos) < nearDist)
						return true;

					if (Vector3.Distance(pt, pos) < nearDist)
						return true;

					if (Vector3.Distance(pt, pos) < nearDist)
						return true;



				}
			}
			return false;

		}



	}






}
	
