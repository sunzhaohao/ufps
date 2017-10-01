#pragma warning disable 0618 // SphereCap obsolete - use SphereHandleCap

using UnityEngine;
using UnityEditor;

using System;
using System.Collections;
using System.Collections.Generic;

using AX;
using AX.Generators;

namespace AX.GeneratorHandlers
{


	public class RepeaterHandler: GeneratorHandler3D 
	{
		/*
		[MenuItem("GameObject/3D Object/Archimatix Nodes/Repeaters/Step Repeater")]
		static void InitStep() {
			AXEditorUtilities.addNodeToCurrentModel("StepRepeater");
		}	
	*/

		/*
		[MenuItem("GameObject/3D Object/Archimatix Nodes/Repeaters/Instance")]
		static void InitInstance() {
			AXEditorUtilities.addNodeToCurrentModel("Instance");
		}
		[MenuItem("GameObject/3D Object/Archimatix Nodes/Repeaters/Replicant")]
		static void InitReplicant() {
			AXEditorUtilities.addNodeToCurrentModel("Replicant");
		}
		[MenuItem("GameObject/3D Object/Archimatix Nodes/Repeaters/Pair Repeater")]
		static void InitPair() {
			AXEditorUtilities.addNodeToCurrentModel("PairRepeater");
		}
		[MenuItem("GameObject/3D Object/Archimatix Nodes/Repeaters/Linear Repeater")]
		static void InitLinear() {
			AXEditorUtilities.addNodeToCurrentModel("LinearRepeater");
		}
		[MenuItem("GameObject/3D Object/Archimatix Nodes/Repeaters/Floor Repeater")]
		static void InitFloor() {
			AXEditorUtilities.addNodeToCurrentModel("FloorRepeater");
		}
		[MenuItem("GameObject/3D Object/Archimatix Nodes/Repeaters/Frame Repeater")]
		static void InitFrame() {
			AXEditorUtilities.addNodeToCurrentModel("FrameRepeater");
		}
		[MenuItem("GameObject/3D Object/Archimatix Nodes/Repeaters/Radial Repeater")]
		static void InitRadial() {
			AXEditorUtilities.addNodeToCurrentModel("RadialRepeater");
		}
		[MenuItem("GameObject/3D Object/Archimatix Nodes/Repeaters/ShapeRepeater")]
		static void InitShapeRepeater() {
			AXEditorUtilities.addNodeToCurrentModel("ShapeRepeater");
		}
		*/

	}


	public class InstanceHandler: GeneratorHandler3D 
	{


		public override void drawControlHandlesofInputParametricObjects(ref List<string> visited, Matrix4x4 consumerM)
		{
			/*
			List<AXParameter> inputMeshes = parametricObject.getAllInputMeshParameters();
			
			foreach(AXParameter input_p in inputMeshes)
			{
				
				
				if (input_p != null  &&  input_p.DependsOn != null  &&  input_p.DependsOn.meshes != null  &&  input_p.DependsOn.meshes.Count > 0)
				{
					AXParametricObject src_po = input_p.DependsOn.Parent;
					
					GeneratorHandler gh = getGeneratorHandler(src_po);
					
					// special case for Instance:
					consumerM *= src_po.getLocalMatrix().inverse;

					if (gh != null)
					{
						gh.drawControlHandles(ref visited, consumerM, true);
						//gh.drawTransformHandles(visited, consumerM);
					}
				}
			}
			*/
		}




	}












	public class LinearRepeaterHandler: GeneratorHandler3D 
	{
		/*
		[MenuItem("GameObject/3D Object/Archimatix Nodes/Repeaters/Linear Repeater")]
		static void Init() {
			AXEditorUtilities.addNodeToCurrentModel("LinearRepeater");
		}
	*/
		public override void drawBoundsHandles(Matrix4x4 consumerM, bool forceDraw=false)
		{
			base.drawBoundsHandles(consumerM, forceDraw);

			LinearRepeater repeater = (parametricObject.generator as LinearRepeater);

			RepeaterTool repeaterTool = repeater.repeaterToolU;

			if (repeater.zAxis)
				repeaterTool = repeater.repeaterToolV;



			Matrix4x4 prevHandleMatrix = Handles.matrix;

			Matrix4x4 context = parametricObject.model.transform.localToWorldMatrix * generator.parametricObject.worldDisplayMatrix;

			Handles.matrix = context;

			if (repeaterTool != null)
			{
				GeneratorHandler gh =  getGeneratorHandler(repeaterTool.parametricObject);

				if (gh != null)
				{
					if (repeater.zAxis)
						Handles.matrix *= Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0, 90, 0), Vector3.one);

					gh.drawBoundsHandles(context, true);

					List<string> visited = new List<string>();
					gh.drawControlHandles(ref visited, context, true);
				}
			}

			Handles.matrix = prevHandleMatrix;
		}		
	}

	public class FloorRepeaterHandler: GeneratorHandler3D 
	{
		/*
		[MenuItem("GameObject/3D Object/Archimatix Nodes/Repeaters/Linear Repeater")]
		static void Init() {
			AXEditorUtilities.addNodeToCurrentModel("FloorRepeater");
		}
	*/
		public override void drawBoundsHandles(Matrix4x4 consumerM, bool forceDraw=false)
		{
			base.drawBoundsHandles(consumerM, forceDraw);

			RepeaterTool repeaterTool = (parametricObject.generator as FloorRepeater).repeaterTool;


			Matrix4x4 context = parametricObject.model.transform.localToWorldMatrix * generator.parametricObject.worldDisplayMatrix;

			Handles.matrix = context;

			if (repeaterTool != null)
			{
				GeneratorHandler gh = getGeneratorHandler(repeaterTool.parametricObject);

				if (gh != null)
				{
					Handles.matrix *= Matrix4x4.TRS(new Vector3(parametricObject.bounds.extents.x, repeaterTool.size/2, 0), Quaternion.Euler(0, 0, 90), Vector3.one);
					gh.drawBoundsHandles(consumerM *  parametricObject.getAxisRotationMatrix() * parametricObject.getLocalAlignMatrix(), true);

					List<string> visited = new List<string>();
					gh.drawControlHandles(ref visited, consumerM *  parametricObject.getAxisRotationMatrix() * parametricObject.getLocalAlignMatrix(), true);
				}
			}
		}		
	}




	public class PairRepeaterHandler: GeneratorHandler3D 
	{
		/*
		[MenuItem("GameObject/3D Object/Archimatix Nodes/Repeaters/Pair Repeater")]
		static void Init() {
			AXEditorUtilities.addNodeToCurrentModel("PairRepeater");
		}
	*/
		public override void drawBoundsHandles(Matrix4x4 consumerM, bool forceDraw=false)
		{
			base.drawBoundsHandles(consumerM, forceDraw);

			PairRepeater gener = (parametricObject.generator as PairRepeater);


			bool zAxis = parametricObject.boolValue("zAxis");
			float separation = gener.P_Seperation.FloatVal;


			// RADIUS HANDLE



			Matrix4x4 prevHandlesMatrix = Handles.matrix;

			Handles.matrix = parametricObject.model.transform.localToWorldMatrix * generator.parametricObject.worldDisplayMatrix;


			//Handles.matrix *= Matrix4x4.TRS(new Vector3(0, -parametricObject.getBoundsAdjustedForAxis().size.y/2, 0), Quaternion.identity, Vector3.one);
			//Handles.matrix *= Matrix4x4.TRS(new Vector3(0, -parametricObject.getBoundsAdjustedForAxis().size.y/2, 0), Quaternion.identity, Vector3.one);
			if (zAxis)
				Handles.matrix *= Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0,90,0), Vector3.one);



			float left 	= -separation/2;
			float right = separation/2;

			float hgt = Mathf.Clamp(.5f*separation, .25f, 100);
			float lhgt = .8f*hgt;



			float len;

			Handles.color = new Color(1, 1f, .5f, .5f);

			Handles.DrawLine(new Vector3(left, 0, 0), new Vector3(left, -hgt, 0));
			Handles.DrawLine(new Vector3(right, 0, 0), new Vector3(right, -hgt, 0));


			Handles.DrawLine(new Vector3(left, -lhgt, 0), new Vector3(right, -lhgt, 0));

			// hatches
			len = .9f * .15f*HandleUtility.GetHandleSize(new Vector3(right, -lhgt, 0));
			Handles.DrawLine(new Vector3(right+len, -lhgt-len, 0), new Vector3(right-len, -lhgt+len, 0));

			len = .9f * .15f*HandleUtility.GetHandleSize(new Vector3(left, -lhgt, 0));
			Handles.DrawLine(new Vector3(left+len, -lhgt-len, 0), new Vector3(left-len, -lhgt+len, 0));

			GUIStyle labelStyle = new GUIStyle();
			labelStyle.alignment = TextAnchor.MiddleCenter;
			labelStyle.normal.textColor = Color.white;

			Handles.Label(new Vector3(0, -lhgt*1.1f, 0), ""+System.Math.Round(separation, 2), labelStyle);


			Handles.color = new Color(1, .5f, .5f, .9f);

			Vector3 pos;

			//right
			pos  = new Vector3(separation/2, 0, 0);
			EditorGUI.BeginChangeCheck();
			pos = Handles.FreeMoveHandle(
				pos, 
				Quaternion.identity,
				.15f*HandleUtility.GetHandleSize(pos),
				Vector3.zero, 
				Handles.SphereCap
			);
			if(EditorGUI.EndChangeCheck())
			{
				Undo.RegisterCompleteObjectUndo (parametricObject.model, "Separation");
				gener.P_Seperation.initiateRipple_setFloatValueFromGUIChange( Mathf.Max(2*pos.x, .1f) );
				parametricObject.model.isAltered(13);
			}

			pos  = new Vector3(separation/2, -lhgt, 0);
			EditorGUI.BeginChangeCheck();
			pos = Handles.FreeMoveHandle(
				pos, 
				Quaternion.identity,
				.15f*HandleUtility.GetHandleSize(pos),
				Vector3.zero, 
				Handles.SphereCap
			);
			if(EditorGUI.EndChangeCheck())
			{
				Undo.RegisterCompleteObjectUndo (parametricObject.model, "Separation");
				gener.P_Seperation.initiateRipple_setFloatValueFromGUIChange( Mathf.Max(2*pos.x, .1f) );
				parametricObject.model.isAltered(14);
			}


			pos = new Vector3(-separation/2, 0, 0);
			EditorGUI.BeginChangeCheck();
			pos = Handles.FreeMoveHandle(
				pos, 
				Quaternion.identity,
				.15f*HandleUtility.GetHandleSize(pos),
				Vector3.zero, 
				Handles.SphereCap
			);
			if(EditorGUI.EndChangeCheck())
			{
				Undo.RegisterCompleteObjectUndo (parametricObject.model, "Separation");
				gener.P_Seperation.initiateRipple_setFloatValueFromGUIChange( Mathf.Max(-2*pos.x, -.1f) );
				parametricObject.model.isAltered(15);
			}

			pos = new Vector3(-separation/2, -lhgt, 0);
			EditorGUI.BeginChangeCheck();
			pos = Handles.FreeMoveHandle(
				pos, 
				Quaternion.identity,
				.15f*HandleUtility.GetHandleSize(pos),
				Vector3.zero, 
				Handles.SphereCap
			);
			if(EditorGUI.EndChangeCheck())
			{
				Undo.RegisterCompleteObjectUndo (parametricObject.model, "Separation");
				gener.P_Seperation.initiateRipple_setFloatValueFromGUIChange( Mathf.Max(-2*pos.x, -.1f) );
				parametricObject.model.isAltered(16);
			}







			Handles.matrix = prevHandlesMatrix;
		}		
	}











	public class GridRepeaterHandler: GeneratorHandler3D 
	{
		/*
		[MenuItem("GameObject/3D Object/Archimatix Nodes/Repeaters/Grid Repeater")]
		static void Init() {
			AXEditorUtilities.addNodeToCurrentModel("GridRepeater");
		}
		*/

		public override void drawBoundsHandles(Matrix4x4 consumerM, bool forceDraw=false)
		{
			base.drawBoundsHandles(generator.parametricObject.worldDisplayMatrix, forceDraw);

			RepeaterTool repeaterToolU = (parametricObject.generator as GridRepeater).repeaterToolU;
			RepeaterTool repeaterToolV = (parametricObject.generator as GridRepeater).repeaterToolV;




			if (repeaterToolU != null && repeaterToolV != null)
			{

				// DRAW GRID
				Handles.color = new Color(1, .5f, 0, .4f);

				Matrix4x4 prevHandleMatrix = Handles.matrix;

				Matrix4x4 context = parametricObject.model.transform.localToWorldMatrix * generator.parametricObject.worldDisplayMatrix;

				Handles.matrix = context;

				for (int i = 0; i<=repeaterToolU.cells; i++)
					Handles.DrawLine(new Vector3(-repeaterToolU.size/2+i*repeaterToolU.actualBay, 0, -repeaterToolV.size/2), new Vector3(-repeaterToolU.size/2+i*repeaterToolU.actualBay, 0, repeaterToolV.size/2));

				for (int k = 0; k<=repeaterToolV.cells; k++)
					Handles.DrawLine(new Vector3(-repeaterToolU.size/2, 0, -repeaterToolV.size/2+k*repeaterToolV.actualBay), new Vector3(repeaterToolU.size/2, 0, -repeaterToolV.size/2+k*repeaterToolV.actualBay));



				// ASK RepeaterTools to DRAW

				// - U TOOL
				GeneratorHandler gh = getGeneratorHandler(repeaterToolU.parametricObject);
				if (gh != null)
				{
					//Handles.matrix = context *   Matrix4x4.TRS(new Vector3(0, 0, 1f*repeaterToolV.bay+repeaterToolV.size/2), Quaternion.Euler(-90, 0, 0), Vector3.one);
					Handles.matrix = context *   Matrix4x4.TRS(new Vector3(0, 0, repeaterToolV.size/2), Quaternion.Euler(-90, 0, 0), Vector3.one);

					gh.drawBoundsHandles(context, true);
					List<string> visited = new List<string>();
					gh.drawControlHandles(ref visited, parametricObject.model.transform.localToWorldMatrix * generator.parametricObject.worldDisplayMatrix *  parametricObject.getAxisRotationMatrix() * parametricObject.getLocalAlignMatrix(), true);
				}

				// - V TOOL
				gh = getGeneratorHandler(repeaterToolV.parametricObject);
				if (gh != null)
				{
					Handles.matrix = context *    Matrix4x4.TRS(new Vector3(repeaterToolU.size/2, 0, 0), Quaternion.Euler(90, -90, 0), Vector3.one);
					gh.drawBoundsHandles(context, true);

					List<string> visited = new List<string>();
					gh.drawControlHandles(ref visited, parametricObject.model.transform.localToWorldMatrix * generator.parametricObject.worldDisplayMatrix *  parametricObject.getAxisRotationMatrix() * parametricObject.getLocalAlignMatrix(), true);
				}

				Handles.matrix = prevHandleMatrix;


			}


		}	




		public override void drawControlHandlesofInputParametricObjects(ref List<string> visited, Matrix4x4 consumerM)
		{

			GridRepeater gener = (generator as GridRepeater);
			// Draw the plan AND the section splines.



			// BOUNDING_SHAPE 
			if (  gener.boundingShapeSrc_po != null && gener.boundingShapeSrc_po.generator != null)
			{
				GeneratorHandler gh = getGeneratorHandler(gener.boundingShapeSrc_po);

				if (gh != null)
				{
					// don't pass consumerM anymore. 
					// Draw functions now use wordDisplayMatrix precacled with calls to Generator.getLocalConsumerMatrixPerInputSocket
					Matrix4x4 localPlanM = Matrix4x4.identity;

					//gener.worldDisplayMatrix = 	localPlanM;		
					gh.drawTransformHandles(visited, localPlanM, true);
					gh.drawControlHandles(ref visited, 	 localPlanM, true);
				}
			}


		}



	}



	// STEP_REPEATER HANDLER
	public class StepRepeaterHandler: GeneratorHandler3D 
	{
		/*
		[MenuItem("GameObject/3D Object/Archimatix Nodes/Repeaters/Step Repeater")]
		static void Init() {
			AXEditorUtilities.addNodeToCurrentModel("StepRepeater");
		}
		*/
		public override void drawControlHandles(ref List<string> visited, Matrix4x4 consumerM, bool beingDrawnFromConsumer)
		//public override void drawBoundsHandles(Matrix4x4 consumerM, bool forceDraw=false)
		{
			if (alreadyVisited(ref visited, "StepRepeaterHandler"))
				return;

			base.drawBoundsHandles(consumerM, beingDrawnFromConsumer);

			Matrix4x4 context = parametricObject.model.transform.localToWorldMatrix * generator.parametricObject.worldDisplayMatrix;
			if (parametricObject.is2D() && ! generator.hasOutputsConnected())
				context *=  parametricObject.getAxisRotationMatrix().inverse  * generator.localMatrix.inverse * parametricObject.getAxisRotationMatrix()  * generator.localMatrix;

			if (! (this.generator is Replicant) )
				drawControlHandlesofInputParametricObjects(ref visited, context * generator.getLocalConsumerMatrixPerInputSocket(parametricObject));

			StepRepeater gener = generator as StepRepeater;


			Matrix4x4 prev = Handles.matrix;

			Handles.matrix = parametricObject.model.transform.localToWorldMatrix * generator.parametricObject.worldDisplayMatrix * generator.localMatrix; //consumerM;

			if (gener.zAxis)
				Handles.matrix *= Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0, -90, 0), Vector3.one);


			Handles.color = new Color(1, .6f, .6f);
			drawPaths(gener.P_Stair_Profile);







			Vector3 pos;

			//right
			pos  = new Vector3(gener.end, gener.rise, 0);
			EditorGUI.BeginChangeCheck();
			pos = Handles.FreeMoveHandle(
				pos, 
				Quaternion.identity,
				.15f*HandleUtility.GetHandleSize(pos),
				Vector3.zero, 
				Handles.SphereCap
			);
			if(EditorGUI.EndChangeCheck())
			{
				Undo.RegisterCompleteObjectUndo (parametricObject.model, "StairRepeater");

				parametricObject.getParameter("End").initiateRipple_setFloatValueFromGUIChange( Mathf.Max(pos.x, .1f) );

				parametricObject.getParameter("Rise").initiateRipple_setFloatValueFromGUIChange( Mathf.Max(pos.y, .1f) );
				parametricObject.model.isAltered(17);
			}


			//left
			pos  = new Vector3(gener.start, gener.riser, 0);
			EditorGUI.BeginChangeCheck();
			pos = Handles.FreeMoveHandle(
				pos, 
				Quaternion.identity,
				.15f*HandleUtility.GetHandleSize(pos),
				Vector3.zero, 
				Handles.SphereCap
			);
			if(EditorGUI.EndChangeCheck())
			{
				Undo.RegisterCompleteObjectUndo (parametricObject.model, "StairRepeater");

				parametricObject.getParameter("Start").initiateRipple_setFloatValueFromGUIChange( pos.x );
				parametricObject.getParameter("Riser").initiateRipple_setFloatValueFromGUIChange( Mathf.Max(pos.y, .1f) );
				parametricObject.model.isAltered(18);
			}



			if (! gener.topStep)
			{
				Handles.DrawDottedLine(new Vector3(gener.run, gener.rise-gener.actual_riser, 0), new Vector3(gener.run, gener.rise, 0), 2);
			}



			// RADIUS LABEL
			GUIStyle labelStyle = new GUIStyle();
			labelStyle.alignment = TextAnchor.MiddleCenter;
			labelStyle.normal.textColor = Color.white; 

			Vector3 labelPosition = new Vector3(gener.run/2, -.4f*HandleUtility.GetHandleSize(Vector3.zero), 0);

			float len = .3f*HandleUtility.GetHandleSize(labelPosition);
			float size = .15f*HandleUtility.GetHandleSize(labelPosition);




			if(Handles.Button(labelPosition-new Vector3(0, len, 0),  Quaternion.Euler(90,0,0), size, size, Handles.ConeCap ))
			{
				Undo.RegisterCompleteObjectUndo (parametricObject.model, "Cell Count");

				gener.P_Steps.initiateRipple_setIntValueFromGUIChange(gener.steps-1);

				gener.parametricObject.model.autobuild();
				for (int i = 0; i < generator.P_Output.Dependents.Count; i++) 
					generator.P_Output.Dependents [i].parametricObject.generator.adjustWorldMatrices ();
			}
			if(Handles.Button(labelPosition+new Vector3(0, len*.5f, 0), Quaternion.Euler(-90,0,0), size, size, Handles.ConeCap ))
			{
				Undo.RegisterCompleteObjectUndo (parametricObject.model, "Cell Count");

				gener.P_Steps.initiateRipple_setIntValueFromGUIChange(gener.steps+1);

				gener.parametricObject.model.autobuild();
				for (int i = 0; i < generator.P_Output.Dependents.Count; i++) 
					generator.P_Output.Dependents [i].parametricObject.generator.adjustWorldMatrices ();
			}

			GUIStyle buttonStyle = GUI.skin.GetStyle("Button");
			buttonStyle.fixedWidth = 100;

			Handles.Label(labelPosition, "steps="+gener.steps+"   ", labelStyle);




			Handles.matrix = prev;



		}	

		void DrawFunc(int controlId, Vector3 position, Quaternion rotation, float size)
		//Draw the button
		{
			//You can draw other stuff than cube, but i havent found something better as a "Button" than cube
			//Handles.DrawCube(controlId, position, rotation, size);
			Vector3 targetPos = SceneView.lastActiveSceneView.camera.gameObject.transform.position; //sceneCameras[0].transform.position;
			//Handles.DrawSolidDisc(position, targetPos, size);
			Handles.DrawSolidDisc(position, targetPos, size);
		}

	}




	// RADIAL_REPEATER HANDLER

	public class RadialStepRepeaterHandler: GeneratorHandler3D 
	{
		/*
		[MenuItem("GameObject/3D Object/Archimatix Nodes/Repeaters/Radial Repeater")]
		static void Init() {
			AXEditorUtilities.addNodeToCurrentModel("RadialRepeater");
		}
		*/

		public override void drawBoundsHandles(Matrix4x4 consumerM, bool forceDraw=false)
		{
			base.drawBoundsHandles(consumerM, forceDraw);


			//StepRepeater gener = generator as StepRepeater;



			Matrix4x4 prevHandleMatrix = Handles.matrix;


			Handles.matrix = prevHandleMatrix;
		}		
	}





	// RADIAL_REPEATER HANDLER

	public class RadialRepeaterHandler: GeneratorHandler3D 
	{
		/*
		[MenuItem("GameObject/3D Object/Archimatix Nodes/Repeaters/Radial Repeater")]
		static void Init() {
			AXEditorUtilities.addNodeToCurrentModel("RadialRepeater");
		}
		*/

		public override void drawBoundsHandles(Matrix4x4 consumerM, bool forceDraw=false)
		{
			base.drawBoundsHandles(consumerM, forceDraw);

			RadialRepeaterTool radialRepeaterTool = (generator as RadialRepeater).repeaterToolU as RadialRepeaterTool;

			Matrix4x4 prevHandleMatrix = Handles.matrix;

			float depth = .55f* HandleUtility.GetHandleSize(Vector3.zero);

			Matrix4x4 context = Matrix4x4.TRS(new Vector3(0, -depth, 0), Quaternion.identity, Vector3.one) * parametricObject.model.transform.localToWorldMatrix * generator.parametricObject.worldDisplayMatrix ;

			Handles.matrix = context;

			if (radialRepeaterTool != null)
			{
				// REPEATER TOOL HANDLER
				GeneratorHandler gh = getGeneratorHandler(radialRepeaterTool.parametricObject);

				if (gh != null)
				{
					//Handles.matrix *= Matrix4x4.TRS(new Vector3(parametricObject.bounds.extents.x, radialRepeaterTool.size/2, 0), Quaternion.Euler(0, 0, 90), Vector3.one);
					gh.drawBoundsHandles(consumerM *  parametricObject.getAxisRotationMatrix() * parametricObject.getLocalAlignMatrix(), true);

					List<string> visited = new List<string>();
					gh.drawControlHandles(ref visited, consumerM *  parametricObject.getAxisRotationMatrix() * parametricObject.getLocalAlignMatrix(), true);
				}
			}

			Handles.matrix = prevHandleMatrix;
		}		
	}













	public class PlanRepeaterHandler: GeneratorHandler3D 
	{
		/*
		[MenuItem("GameObject/3D Object/Archimatix Nodes/Repeaters/Plan Repeater")]
		static void Init() {
			AXEditorUtilities.addNodeToCurrentModel("PlanRepeater");
		}
		*/

		public override void drawBoundsHandles(Matrix4x4 consumerM, bool forceDraw=false)
		{
			base.drawBoundsHandles(consumerM, forceDraw);

			RepeaterTool repeaterTool = (parametricObject.generator as PlanRepeater).repeaterTool;

			if (repeaterTool != null)
			{
				GeneratorHandler gh = getGeneratorHandler(repeaterTool.parametricObject);

				if (gh != null)
				{
					consumerM *= Matrix4x4.TRS(new Vector3(parametricObject.bounds.extents.x, repeaterTool.size/2, 0), Quaternion.Euler(0, 0, 90), Vector3.one);
					gh.drawBoundsHandles(consumerM *  parametricObject.getAxisRotationMatrix() * parametricObject.getLocalAlignMatrix(), true);
					List<string> visited = new List<string>();
					gh.drawControlHandles(ref visited, consumerM *  parametricObject.getAxisRotationMatrix() * parametricObject.getLocalAlignMatrix(), true);
				}
			}
		}


		public override void drawControlHandlesofInputParametricObjects(ref List<string> visited, Matrix4x4 consumerM)
		{

			PlanRepeater gener = (generator as PlanRepeater);
			// Draw the plan AND the section splines.

			// PLAN first, since the section needsplan information to position itself.
			if (  gener.planSrc_po != null && gener.planSrc_po.generator != null)
			{


				GeneratorHandler gh = getGeneratorHandler(gener.planSrc_po);

				if (gh != null)
				{
					//Matrix4x4 localPlanM = parametricObject.model.transform.localToWorldMatrix * generator.parametricObject.worldDisplayMatrix * generator.getLocalConsumerMatrixPerInputSocket(gener.planSrc_po);
					Matrix4x4 localPlanM = Matrix4x4.identity;
					if (gener.planSrc_po.is2D())				
						gh.drawTransformHandles(visited, localPlanM, true);
					gh.drawControlHandles(ref visited, 	 localPlanM, true);
				}
			}

			// SECTION
			if (  gener.sectionSrc_po != null && gener.sectionSrc_po.generator != null)
			{

				GeneratorHandler gh = getGeneratorHandler(gener.sectionSrc_po);

				if (gh != null)
				{
					//Matrix4x4 localSecM = parametricObject.model.transform.localToWorldMatrix.inverse * generator.parametricObject.worldDisplayMatrix * generator.getLocalConsumerMatrixPerInputSocket(gener.sectionSrc_po);
					Matrix4x4 localSecM = Matrix4x4.identity;


					//Debug.Log("localSecM");
					//Debug.Log(localSecM);

					gh.drawTransformHandles(visited, localSecM, true);
					gh.drawControlHandles(ref visited, localSecM, true);

				}
			}





		}








	}


}