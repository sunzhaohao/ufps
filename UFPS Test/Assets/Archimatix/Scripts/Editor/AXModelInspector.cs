using UnityEditor;
using UnityEngine;

using System;
using System.Collections;
using System.Collections.Generic;

using AX.SimpleJSON;




using AXEditor;

using AX.Generators;
using AXGeometryTools;

using AX.GeneratorHandlers;
using AX;

 
[CustomEditor(typeof(AXModel))] 
public class AXModelInspector : Editor 
{

	public bool mouseJustDown = false;


	Tool LastTool; 

	private static GUIStyle richLabelStyle;

	Texture2D infoIconTexture;

	 
	[System.NonSerialized]
	public StopWatch commandStopWatch;


	public static bool doAutobuild;

	public void OnEnable()
	{
		ArchimatixEngine.establishPaths(); 

		AXEditorUtilities.loadNodeIcons();

		infoIconTexture				= (Texture2D) AssetDatabase.LoadAssetAtPath(ArchimatixEngine.ArchimatixAssetPath+"/ui/GeneralIcons/zz-AXMenuIcons-InfoIcon.png", typeof(Texture2D));

	}

	public override void OnInspectorGUI() {

		//if (Event.current.type != EventType.Layout)
		//	return;
		AXModel model = (AXModel) target;

		Event e = Event.current;


		//Debug.Log(evt.type);
		switch (e.type) {
			case EventType.Layout:
				if (doAutobuild)
				{
					doAutobuild = false;
					model.autobuild();
				}
				break;

		
			case EventType.MouseDown:
				//Debug.Log("Down");
				
				break;

		case EventType.MouseUp:
			
			doAutobuild = true;
			break;

		case EventType.KeyUp:
			Undo.RegisterCompleteObjectUndo (model, "Key Up");
			//Debug.Log("KeyUp");
			doAutobuild = true;
			break;

			case EventType.DragUpdated:
				//Debug.Log("Dragging");
				break;

   			case EventType.DragPerform:
				//DragAndDrop.AcceptDrag();
   			 	//Debug.Log("Drag and Drop not supported... yet");
				Undo.RegisterCompleteObjectUndo (model, "Default material scale");

			doAutobuild = true;
   			 	break;
   		}

		/*
		switch (e.type)
		{
		case EventType.mouseUp: 

			model.autobuild();
			//e.Use ();
			break;

		case EventType.mouseDown: 

			model.autobuild();
			//e.Use ();
			break;

		}
		*/



		//myTarget.MyValue = EditorGUILayout.IntSlider("Val-you", myTarget.MyValue, 1, 10);



		if(richLabelStyle == null)
		{
			richLabelStyle 							= new GUIStyle(GUI.skin.label);
			richLabelStyle.richText 				= true;
			richLabelStyle.wordWrap 				= true;
		}
		//if (infoIconTexture = null)


		String rubricColor = (EditorGUIUtility.isProSkin) ? "#bbbbff" : "#660022";










		EditorGUIUtility.labelWidth = 150;








		model.precisionLevel = (PrecisionLevel) EditorGUILayout.EnumPopup("Precision Level", model.precisionLevel);


		if (! AXNodeGraphEditorWindow.IsOpen)
		{
			GUILayout.Space(10);

			if (GUILayout.Button("Open in Node Graph"))
			{
				AXNodeGraphEditorWindow.Init();
			}
		}


		GUILayout.Space(20);




		// -- RUBRIC - MATERIAL --

		EditorGUILayout.BeginHorizontal();

		GUILayout.Label("<color="+rubricColor+"> <size=13>Material (Default)</size></color>", richLabelStyle);
		GUILayout.FlexibleSpace();

		//if (GUILayout.Button ( infoIconTexture, GUIStyle.none))
		if (GUILayout.Button ( infoIconTexture,  GUIStyle.none, new GUILayoutOption[] {GUILayout.Width(16), GUILayout.Height(16)}))
		{
			
			Application.OpenURL("http://www.archimatix.com/manual/materials"); 
		}

		EditorGUILayout.EndHorizontal();

		// -------- 


		// Material
		EditorGUI.BeginChangeCheck ();

		model.axMat.mat = (Material) EditorGUILayout.ObjectField( model.axMat.mat, typeof(Material), true);
		if (EditorGUI.EndChangeCheck ()) 
		{
			Undo.RegisterCompleteObjectUndo (model, "Default material for " +model.name);
			model.remapMaterialTools();
			model.autobuild();

		}



		// Texture //
		model.showDefaultMaterial = EditorGUILayout.Foldout(model.showDefaultMaterial, "Texture Scaling");
		if (model.showDefaultMaterial)
		{

			EditorGUI.BeginChangeCheck ();
			model.axTex.scaleIsUnified = EditorGUILayout.Toggle("Unified Scaling", model.axTex.scaleIsUnified);
			if (EditorGUI.EndChangeCheck ()) 
			{
				Undo.RegisterCompleteObjectUndo (model, "Default material scale");
				model.axTex.scale.y = model.axTex.scale.x;
				model.isAltered();
			}
			 
			if (model.axTex.scaleIsUnified)
			{
				EditorGUI.BeginChangeCheck ();
				model.axTex.scale.x = EditorGUILayout.FloatField("Scale", model.axTex.scale.x);
				if (EditorGUI.EndChangeCheck ()) 
				{
					Undo.RegisterCompleteObjectUndo (model, "Default material for " +model.name);

					model.axTex.scale.y = model.axTex.scale.x;
					model.isAltered();

				}
			}
			else
			{
				// Scale X
				EditorGUI.BeginChangeCheck ();
				model.axTex.scale.x = EditorGUILayout.FloatField("Scale X", model.axTex.scale.x);
				if (EditorGUI.EndChangeCheck ()) 
				{
					
					Undo.RegisterCompleteObjectUndo (model, "Default material for " +model.name);
					model.isAltered();

				}

				// Scale Y
				EditorGUI.BeginChangeCheck ();
				model.axTex.scale.y = EditorGUILayout.FloatField("Scale Y", model.axTex.scale.y);
				if (EditorGUI.EndChangeCheck ()) 
				{
					
					Undo.RegisterCompleteObjectUndo (model, "Default material for " +model.name);
					model.isAltered();


				}

			}

			EditorGUI.BeginChangeCheck ();
			model.axTex.runningU = EditorGUILayout.Toggle("Running U", model.axTex.runningU);
			if (EditorGUI.EndChangeCheck ()) 
			{
				Undo.RegisterCompleteObjectUndo (model, "Running U");
				model.isAltered();

			}



		}

		// PhysicMaterial //
		model.axMat.showPhysicMaterial = EditorGUILayout.Foldout(model.axMat.showPhysicMaterial, "Physics Material");
		if (model.axMat.showPhysicMaterial)
		{
		// PHYSIC MATERIAL
			EditorGUI.BeginChangeCheck ();
			model.axMat.physMat = (PhysicMaterial) EditorGUILayout.ObjectField( model.axMat.physMat, typeof(PhysicMaterial), true);
			if (EditorGUI.EndChangeCheck ()) 
			{
				Undo.RegisterCompleteObjectUndo (model, "Default PhysicMaterial for " +model.name);
				model.remapMaterialTools();
				model.autobuild();
			}

			// DENSITY
			EditorGUI.BeginChangeCheck ();
			model.axMat.density = EditorGUILayout.FloatField("Density", model.axMat.density);
			if (EditorGUI.EndChangeCheck ()) 
			{
				Undo.RegisterCompleteObjectUndo (model, "Material Density for " +model.name);
				model.isAltered();
			}
		}


		GUILayout.Space(20);





		// -- RUBRIC - LIGHTING --

		EditorGUILayout.BeginHorizontal();

		GUILayout.Label("<color="+rubricColor+"> <size=13>Lighting</size></color>", richLabelStyle);
		GUILayout.FlexibleSpace();

		//if (GUILayout.Button ( infoIconTexture, GUIStyle.none))
		if (GUILayout.Button ( infoIconTexture,  GUIStyle.none, new GUILayoutOption[] {GUILayout.Width(16), GUILayout.Height(16)}))
		{
			
			Application.OpenURL("http://www.archimatix.com/manual/lightmapping-with-archimatix"); 
		}

		EditorGUILayout.EndHorizontal();

		// -------- 


		// LIGHTMAP FLAGS ENABLED
		EditorGUI.BeginChangeCheck ();
		model.staticFlagsEnabled = EditorGUILayout.ToggleLeft ("Lightmap Flags Enabled",  model.staticFlagsEnabled);
		if (EditorGUI.EndChangeCheck ()) 
		{
			Undo.RegisterCompleteObjectUndo (model, "Static Masks Enabled change for " +model.name);

			model.staticFlagsJustEnabled = true;

			model.autobuild();
		}

		// SECONDARY UVs
		if (model.staticFlagsEnabled)
		{
			//if (model.buildStatus == AXModel.BuildStatus.Generated)
			EditorGUI.BeginChangeCheck ();
			model.createSecondaryUVs = EditorGUILayout.ToggleLeft ("Create Secondary UVs (for Baked GI)",  model.createSecondaryUVs);
			if (EditorGUI.EndChangeCheck ()) 
			{
				//if (model.createSecondaryUVs)
				//	AXEditorUtilities.makeLightMapUVs (model);
				model.createSecondaryUVsJustEnabled = true;


			}
		}

		 

		GUILayout.Space(20);



		/*
		if (GUILayout.Button("Set All Objects as Lightmap Static"))
		{
			Debug.Log("Set all");
			model.setLightmapStaticForAllPOs();
		}
		*/





		if (ArchimatixEngine.plevel == 3)
		{

			// RUNTIME //

			GUILayout.Label("<color="+rubricColor+"> <size=13>Pro Runtime Features</size></color>", richLabelStyle);


			// EXPOSED PARAMETERS 
			//if (model.cycleSelectedAXGO != null)
			//	GUILayout.Label("Consumer Address: "+model.cycleSelectedAXGO.consumerAddress);

			GUILayout.Label("Runtime Parameters");

			EditorGUI.BeginChangeCheck();
			foreach(AXParameterAlias pa in model.exposedParameterAliases)
				ParameterAliasGUILayout.OnGUI(pa);
			if (EditorGUI.EndChangeCheck())
			{
				model.isAltered();
			}

			GUILayout.Space(30);

			if (model.exposedParameterAliases != null && model.exposedParameterAliases.Count > 0)
			{
				if (GUILayout.Button( "Create Runtime Controller",  GUILayout.Width(200)))
				{
						ArchimatixEngine.createControllerForModel = model;
				}
			}




			// RUNTIME HANDLES 
			//if (model.cycleSelectedAXGO != null)
			//	GUILayout.Label("Consumer Address: "+model.cycleSelectedAXGO.consumerAddress);
			if (model.runtimeHandleAliases != null && model.runtimeHandleAliases.Count > 0)
			{
				GUILayout.Label("Runtime Handles");


				foreach(AXHandleRuntimeAlias rth in model.runtimeHandleAliases)
				{			
					AXRuntimeHandlesGUI.OnGUI(rth);
				}
			}
		

			GUILayout.Space(20);

		

		} // RUNTIME









		// RELATIONS

		if (model.selectedRelationInGraph != null)
		{
			GUILayout.Space(20);

			GUILayout.Label("<color="+rubricColor+"> <size=13>Selected Relation</size></color>", richLabelStyle);
		

			AXRelation r = model.selectedRelationInGraph;
			RelationEditorGUI.OnGUI(r);




		}


		GUILayout.Space(20);



		// -- RUBRIC - SELECTED NODES --

		EditorGUILayout.BeginHorizontal();

		GUILayout.Label("<color="+rubricColor+"> <size=13>Selected Nodes</size></color>", richLabelStyle);
		GUILayout.FlexibleSpace();

		//if (GUILayout.Button ( infoIconTexture, GUIStyle.none))
		if (GUILayout.Button ( infoIconTexture,  GUIStyle.none, new GUILayoutOption[] {GUILayout.Width(16), GUILayout.Height(16)}))
		{
			
			Application.OpenURL("http://www.archimatix.com/manual/node-selection"); 
		}

		EditorGUILayout.EndHorizontal();

		// -------- 



		GUILayout.Space(10);
		
		if (model.selectedPOs != null && model.selectedPOs.Count > 0)
		{
			for(int i=0; i<model.selectedPOs.Count; i++)
			{
				//Debug.Log(i);
				AXParametricObject po = model.selectedPOs[i];

				//Debug.Log(i+" ------------------------ po.Name="+po.Name+ " -- " + po.generator.AllInput_Ps.Count);


				doPO(po);

				// for subnodes...

				if  (po.generator.AllInput_Ps != null)
				{
					for (int j=po.generator.AllInput_Ps.Count-1; j>=0; j--)
					{

						AXParameter p = po.generator.AllInput_Ps[j];
						
						if (p.DependsOn != null)
						{
							AXParametricObject spo = p.DependsOn.parametricObject;
							doPO(spo);

							// sub-sub nodes...
							for (int k=spo.generator.AllInput_Ps.Count-1; k>=0; k--)
							{

								if (spo.generator.AllInput_Ps[k].DependsOn != null)
									doPO(spo.generator.AllInput_Ps[k].DependsOn.parametricObject);
							}
						}


					}
				}


			}
		}
		else
			GUILayout.Label( "...no nodes selected");

		GUILayout.Space(50);



		//model.controls[0].val = EditorGUILayout.Slider(model.controls[0].val, 0, 100);

		 
		/*
		switch (e.type)
		{
		case EventType.KeyUp:
		case EventType.mouseUp: 

			model.autobuild();
			//e.Use ();

			//return;
			break;

		case EventType.mouseDown: 

			//model.autobuild();
			//e.Use ();
			break;

		}
		*/

	
											//DrawDefaultInspector ();
	}




















	public static bool showTitle(AXParametricObject po)
	{
		if (po.generator is MaterialTool)
			return false;

		return true;

	}


	public void doPO(AXParametricObject po)
	{

		




		GUIStyle labelstyle = GUI.skin.GetStyle ("Label");
		//int fontSize = labelstyle.fontSize;



		labelstyle.fontSize = 20;


		GUILayout.BeginHorizontal();
			GUILayout.Space(40);
			Rect rect = GUILayoutUtility.GetLastRect();

			// NAME
			GUILayout.Label(po.Name);


			labelstyle.fontSize = 12;
		GUILayout.EndHorizontal();

		if  ( po.is2D() && po.generator.hasOutputsReady() )
		{
			AXParameter output_p = po.generator.getPreferredOutputParameter(); 
			GUIDrawing.DrawPathsFit(output_p, new Vector2(32, rect.y+10 ), 32);
		}
		else if ( ArchimatixEngine.nodeIcons != null && ArchimatixEngine.nodeIcons.ContainsKey(po.Type))
		{
			GUI.DrawTexture(new Rect(16, rect.y-5, 32, 32), ArchimatixEngine.nodeIcons[po.Type], ScaleMode.ScaleToFit, true, 1.0F);
		}

		if (showTitle(po))
		{
			GUILayout.BeginHorizontal();

				EditorGUI.BeginChangeCheck ();
				EditorGUIUtility.labelWidth = 0;
				po.isActive = EditorGUILayout.Toggle(po.isActive, GUILayout.MaxWidth(20));
				if (EditorGUI.EndChangeCheck ()) {
					Undo.RegisterCompleteObjectUndo (po.model, "isActive value change for " + po.Name);
					po.model.autobuild();
					po.generator.adjustWorldMatrices();
				}

				EditorGUIUtility.labelWidth = 0;
				po.Name = GUILayout.TextField(po.Name);
			
			 

			if (po.is3D())
			{

				

				EditorGUIUtility.labelWidth = 35;
				EditorGUI.BeginChangeCheck ();
				po.axStaticEditorFlags = (AXStaticEditorFlags) EditorGUILayout.EnumMaskField( "Static", po.axStaticEditorFlags );
				if (EditorGUI.EndChangeCheck ()) {
					Undo.RegisterCompleteObjectUndo (po.model, "Static value change for " + po.Name);

					po.setUpstreamersToYourFlags();
					// post dialog to change all children...
					po.model.autobuild();
					/*
					int option = EditorUtility.DisplayDialogComplex("Change Static Flags?",
						"Do you want to disable the Lightmap Static flag for all the child objects as well? ", 

						"No, only this ParametricObject",  
						"Yes, change children",
						"Cancel");
						 
					 
					switch( option )
					{
						// Save Scene
						case 0:
							Debug.Log("ONLY");
							break;

						// Save and Quit.

						// SAVE THIS MASK FOR CHILDREN AS WELL.
						case 1:
							Debug.Log("CHILDREN");
							break;

						case 2:
							Debug.Log("CANCEL");
							break;


						default:
							Debug.LogError( "Unrecognized option." );
							break;
					}
					*/
				
											
				}
			}

			GUILayout.FlexibleSpace();

			if (GUILayout.Button("Select"))
			{
				po.model.selectAndPanToPO(po);
			}


			GUILayout.EndHorizontal();

		}


		if (  po.is3D())
		{
			EditorGUIUtility.labelWidth = 35;

			GUILayout.BeginHorizontal();


				// TAGS
				EditorGUI.BeginChangeCheck ();
				po.tag = EditorGUILayout.TagField("Tag:", po.tag);

				if (EditorGUI.EndChangeCheck ()) {
					Undo.RegisterCompleteObjectUndo (po.model, "Tag value change for " + po.Name);

											
				}



				// LAYERS
				EditorGUI.BeginChangeCheck ();
				int intval = EditorGUILayout.LayerField("Layer:", po.layer);

				if (EditorGUI.EndChangeCheck ()) {
					Undo.RegisterCompleteObjectUndo (po.model, "Layer value change for " + po.Name);
					po.layer = intval;
											
				}


			GUILayout.EndHorizontal();

			bool hasMeshRenderer = ! po.noMeshRenderer;
			EditorGUI.BeginChangeCheck ();
			hasMeshRenderer = EditorGUILayout.ToggleLeft("Mesh Renderer", hasMeshRenderer);
			if (EditorGUI.EndChangeCheck ()) {
				Undo.RegisterCompleteObjectUndo (po.model, "hasMeshRenderer");
				po.noMeshRenderer = ! hasMeshRenderer;
			}
		} 
		else if (po.generator is MaterialTool)
		{
			EditorGUI.BeginChangeCheck ();

			//float thumbSize = 16;

			po.axMat.mat = (Material) EditorGUILayout.ObjectField(po.axMat.mat, typeof(Material), true);
			if (EditorGUI.EndChangeCheck ()) 
			{
				
				Undo.RegisterCompleteObjectUndo (po.model, "Material");

				po.model.remapMaterialTools();
				po.model.autobuild();
				 
			}

		}





			 


		AXParameter p;




		//EditorGUIUtility.labelWidth = 200;//EditorGUIUtility.currentViewWidth-16;

		if (po.geometryControls != null && po.geometryControls.children !=null)
		{

			for (int i=0; i<po.geometryControls.children.Count; i++) {

				p =  po.geometryControls.children[i] as AXParameter;

				EditorGUIUtility.labelWidth = 150;





				// PARAMETERS
				switch(p.Type)
				{



				// ANIMATION_CURVE

				case AXParameter.DataType.AnimationCurve:

					EditorGUI.BeginChangeCheck ();
					EditorGUILayout.CurveField(p.animationCurve);
					if (EditorGUI.EndChangeCheck ()) {
						Undo.RegisterCompleteObjectUndo (p.parametricObject.model, "ModifierCurve");
						p.parametricObject.model.isAltered(28);
					}

					break;

				// FLOAT
				case AXParameter.DataType.Float:
					AXEditorUtilities.assertFloatFieldKeyCodeValidity("FloatField_" + p.Name);

					GUILayout.BeginHorizontal();




					EditorGUI.BeginChangeCheck ();
					GUI.SetNextControlName("FloatField_" + p.Name);
					p.FloatVal = EditorGUILayout.FloatField(p.Name,  p.FloatVal);
					if (EditorGUI.EndChangeCheck ()) {
						//Debug.Log(p.FloatVal);
						Undo.RegisterCompleteObjectUndo (po.model, "value change for " + p.Name);
						p.parametricObject.initiateRipple_setFloatValueFromGUIChange(p.Name, p.FloatVal);
						p.parametricObject.model.isAltered(27);
						p.parametricObject.generator.adjustWorldMatrices ();
					}

					// Expose
					EditorGUI.BeginChangeCheck ();
					p.exposeAsInterface = EditorGUILayout.Toggle (p.exposeAsInterface, GUILayout.MaxWidth(20));
					if (EditorGUI.EndChangeCheck ()) {
						Undo.RegisterCompleteObjectUndo (p.parametricObject.model, "Expose Parameter");

						if (p.exposeAsInterface)
							p.parametricObject.model.addExposedParameter(p);
						else
							p.parametricObject.model.removeExposedParameter(p); 
					}


					GUILayout.EndHorizontal();
					break;

				// INT
				case AXParameter.DataType.Int:

					GUILayout.BeginHorizontal();



					EditorGUI.BeginChangeCheck ();
					GUI.SetNextControlName("IntField_" + p.Name);
					p.intval = EditorGUILayout.IntField(p.Name,  p.intval);
					if (EditorGUI.EndChangeCheck ()) {
						Undo.RegisterCompleteObjectUndo (po.model, "value change for " + p.Name);
						p.parametricObject.initiateRipple_setFloatValueFromGUIChange(p.Name, p.intval);
						p.parametricObject.model.isAltered(28);
						p.parametricObject.generator.adjustWorldMatrices ();
					}

					// Expose
					EditorGUI.BeginChangeCheck ();
					p.exposeAsInterface = EditorGUILayout.Toggle (p.exposeAsInterface, GUILayout.MaxWidth(20));
					if (EditorGUI.EndChangeCheck ()) {
						Undo.RegisterCompleteObjectUndo (p.parametricObject.model, "Expose Parameter");

						if (p.exposeAsInterface)
							p.parametricObject.model.addExposedParameter(p);
						else
							p.parametricObject.model.removeExposedParameter(p); 
					}


					GUILayout.EndHorizontal();
					break;

				// BOOL
				case AXParameter.DataType.Bool:
					//EditorGUIUtility.currentViewWidth-16;

					GUILayout.BeginHorizontal();






					//EditorGUIUtility.labelWidth = 150;
					EditorGUI.BeginChangeCheck ();
					p.boolval = EditorGUILayout.Toggle (p.Name,  p.boolval);
					if (EditorGUI.EndChangeCheck ()) {
						Undo.RegisterCompleteObjectUndo (po.model, " value change for " + p.Name);
						p.parametricObject.initiateRipple_setBoolParameterValueByName(p.Name, p.boolval);
						//p.parametricObject.model.autobuild();
						p.parametricObject.model.isAltered(27);
						//p.parametricObject.generator.adjustWorldMatrices();
					}

					GUILayout.FlexibleSpace();

					// Expose
					EditorGUI.BeginChangeCheck ();
					p.exposeAsInterface = EditorGUILayout.Toggle (p.exposeAsInterface, GUILayout.MaxWidth(20));
					if (EditorGUI.EndChangeCheck ()) {
						Undo.RegisterCompleteObjectUndo (p.parametricObject.model, "Expose Parameter");

						if (p.exposeAsInterface)
							p.parametricObject.model.addExposedParameter(p);
						else
							p.parametricObject.model.removeExposedParameter(p); 
					}


					GUILayout.EndHorizontal();
					break; 

				
				case AXParameter.DataType.CustomOption:
				{
					// OPTION POPUP
					
					string[] options = p.optionLabels.ToArray();

					EditorGUI.BeginChangeCheck ();
					GUI.SetNextControlName("CustomOptionPopup_" + p.Guid + "_"  + p.Name);	
					p.intval = EditorGUILayout.Popup(
						p.Name,
						p.intval, 
						options);
					if (EditorGUI.EndChangeCheck ()) {
						Undo.RegisterCompleteObjectUndo (p.Parent.model, "value change for " + p.Name);
						p.parametricObject.model.autobuild();

						if (p.PType == AXParameter.ParameterType.PositionControl)
							p.parametricObject.generator.adjustWorldMatrices();

					}
					
					break;
				}
				}

				//if (p.PType != AXParameter.ParameterType.None && p.PType != AXParameter.ParameterType.GeometryControl)
				//	continue;
							

			
				
			}
		}


		// PROTOTYPE
		if (po.is3D())
		{
			po.displayPrototypes = EditorGUILayout.Foldout (po.displayPrototypes, "Prototypes");

			if (po.displayPrototypes)
			{
				EditorGUI.BeginChangeCheck ();
				po.prototypeGameObject = (GameObject) EditorGUILayout.ObjectField( po.prototypeGameObject, typeof(GameObject), true);
				if (EditorGUI.EndChangeCheck ()) 
				{
					Undo.RegisterCompleteObjectUndo (po.model, "Prototype GameObject set for " + po.model.name);
					if (po.prototypeGameObject != null)
					{
						AXPrototype proto = (AXPrototype) po.prototypeGameObject.GetComponent("AXPrototype");
						if (proto == null)
						{
							proto = po.prototypeGameObject.AddComponent<AXPrototype>();
						}
						if (! proto.parametricObjects.Contains(po))
							proto.parametricObjects.Add(po);
					}
					po.model.autobuild();


				}
			}

		}


		if (po.selectedAXGO != null)
		{

			GUILayout.Label(po.selectedAXGO.consumerAddress);
		}
		GUILayout.Space(30);

	}

	
	

}
