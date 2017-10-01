﻿using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;


using AXClipperLib;
using Path = System.Collections.Generic.List<AXClipperLib.IntPoint>;
using Paths = System.Collections.Generic.List<System.Collections.Generic.List<AXClipperLib.IntPoint>>;

using AXPoly2Tri;
using PolygonPoints = System.Collections.Generic.List<AXPoly2Tri.PolygonPoint>;

using AXGeometryTools;

using AX;


/* Generators
 * 
 * While the ParametricObject is a generic object, serving primarily
 * as a generative node in a graph data structure, the Generator provides
 * the meat on the bone. It defines the nature of the ParamtericObject.
 * 
 * The Generator provides the list of parameters and handels for new object instantiation
 * as welle as generate output (splines and meshes) and interactive handles in the SceneView.
 * 
 * For the most part, handles and shape descriptions are authored in the 
 * EditorWindow, but lower-level natures are defined in the subclasses of Generator,
 * namely Organizers, Meshers (generative mesh primatives), Repeaters, and Shapes.
 * 
 * 
 */

namespace AX.Generators
{
	public enum ValidityType {Translation, Rotation, Scaling};


	public interface ITerrainer
	{
		

	}
	public interface ICustomNode
	{
		

	}
	public interface ILogic
	{
		

	}
	public interface IGenerator
	{
		// A generator has a reference to a ParametricObject and can use its parameters
		// it returns a List of AXMeshes (and later a Boundary)
		void init_parametricObject();
		GameObject generate(bool makeGameObjects, AXParametricObject initiator_po, bool isReplica);
	}
	
	
	// GENERATOR
	public class Generator : IGenerator
	{


		public AXParametricObject parametricObject;						// the ParametricObject that uses this Generator


		public Type generatorHandlerType;


		public virtual string GeneratorHandlerTypeName { get { return "GeneratorHandler"; } }
	
		public enum ParameterType 	{None, Input, TextureControl, BoundsControl, GeometryControl, Output, PositionControl, DerivedValue, Subparameter};
		public enum ConsumerType 	{None, has2DConsumer, has3DConsumer};
		public 		ConsumerType 	consumerType = ConsumerType.None;

		public Matrix4x4 		localTranslationMatrix;
		public Matrix4x4 		localRotationMatrix;
		public Matrix4x4 		localScaleMatrix;
		public Matrix4x4 		localUnscaledMatrix;


		private Matrix4x4 		_localMatrix;
		public Matrix4x4 		localMatrix
		{
			get {
				return _localMatrix;
			}
			set {
				//Debug.Log(parametricObject.Name + ": SET LOCAL_MATRIX " + value);
				_localMatrix = value;
			}
		}
		public Matrix4x4 localMatrixWithAxisRotationAndAlignment;

		//public Matrix4x4 worldDisplayMatrix;


		// INPUTS

		public AXParameter 		P_IsActive;
		public AXParameter 		P_Input;
		public AXParameter 		P_Output;

		public AXParameter 		P_BoundsX;
		public AXParameter 		P_BoundsY;
		public AXParameter 		P_BoundsZ;


		public List<AXParameter> AllInput_Ps;
		public List<AXParameter> AllOutput_Ps;

		public AXParameter 			inputSrc_p; 
		public AXParametricObject 	inputSrc_po;

		public AXParameter P_Axis;

		public AXParameter	P_JitterTranslation;
		public AXParameter	P_JitterRotation;
		public AXParameter	P_JitterScale;







		// JITTER (Generators are never serialized)
		public JitterTool 			jitterTranslationTool;
		public JitterTool 			jitterRotationTool;
		public JitterTool 			jitterScaleTool;

		public Vector3				jitterScale		= Vector3.zero;





		// CONTROL
		public Axis axis;



		public float area;
		public float volume;

		public int selectedIndex = -1;

		public List<int> selectedIndices;



		public bool has3DConsumerToGoToFirst2DConsumer = false;
		
		public List<Vector3> debugVectors;
		
		public Color GUIColor;
		public Color GUIColorPro;
		public Color ThumbnailColor;


		public virtual float minNodePaletteWidth
		{
			get { return 175; }
		}
		//public float minNodePaletteWidth = 175;



		

		public Generator()
		{
			
		}

		public void setParametricObject(AXParametricObject po)
		{
			parametricObject = po;

			// now that po is set, we are ready to init.
			Init();

		}

		// Init is called after this generator has been instantiatied by 
		// its parametricObject, which is on Deserialization
		public virtual void Init()
     	{
			generatorHandlerType = ArchimatixUtils.AXGetType(GeneratorHandlerTypeName);

			initGUIColor();
			 
     	}	
		
		public virtual void initGUIColor()
		{
			GUIColor = Color.white;
			GUIColorPro = Color.white;
			ThumbnailColor  = new Color(.5f,.5f,.5f);
			
		}
		
		
		public virtual void init_parametricObject() 
		{ 
			// parameters
			//Debug.Log ("++++++++++++++++++++++++++++++++++++++++++++++ INIT Generator!");
			// handles

			parametricObject.createdate = DateTime.Now;

			parametricObject.assertBaseControls();

			if (parametricObject.getParameter("Enabled") == null)
				parametricObject.addParameter(AXParameter.DataType.Bool, AXParameter.ParameterType.Base, 	"Enabled", true);

		
		}

		public static bool assertFloatValidity(ValidityType vtype, AXParameter p )
		{
			bool isValid = true;


			if (p != null)
			{
				switch(vtype)
				{
				case ValidityType.Translation:
					if ((float.IsNaN (p.FloatVal) || float.IsInfinity (p.FloatVal)))
					{
						isValid = false;
						p.FloatVal = 0;
					}
						
					break;

				case ValidityType.Rotation:
					if ((float.IsNaN (p.FloatVal) || float.IsInfinity (p.FloatVal))) {
						isValid = false;
						p.FloatVal = 0;
					}	
					// now for max min
					p.FloatVal = p.FloatVal % 360;

					break;

				case ValidityType.Scaling:
					if ((float.IsNaN (p.FloatVal) || float.IsInfinity (p.FloatVal))) {
						isValid = false;
						p.FloatVal = 1;
					}	

					// now for max min
					p.FloatVal = Mathf.Clamp (p.FloatVal, .2f, 5);

					break;


				}
			}
			else
				isValid = false;

			return isValid;
				

		}


	
		// POLL INPUTS (only on graph change())
		public virtual void pollInputParmetersAndSetUpLocalReferences()
		{

			if (parametricObject.getParameter("Enabled") == null)
				parametricObject.addParameter(AXParameter.DataType.Bool, AXParameter.ParameterType.Base, 	"Enabled", true);

			P_IsActive = parametricObject.getParameter("Enabled");

			P_Input 	= getPreferredInputParameter();
			P_Output 	= getPreferredOutputParameter();

			AllInput_Ps		= new List<AXParameter>();
			AllOutput_Ps 	= new List<AXParameter>();

			for (int i=0; i<parametricObject.parameters.Count; i++)
			{
				if (parametricObject.parameters[i].PType == AXParameter.ParameterType.Input)
				{
					AllInput_Ps.Add(parametricObject.parameters[i]);
					continue;
				}
				else if (parametricObject.parameters[i].PType == AXParameter.ParameterType.Output)
				{
					AllOutput_Ps.Add(parametricObject.parameters[i]);
					continue;
				}
			} 

			P_BoundsX = parametricObject.getParameterByBinding(Axis.X);
			P_BoundsY = parametricObject.getParameterByBinding(Axis.Y);
			P_BoundsZ = parametricObject.getParameterByBinding(Axis.Z);



			AXShape shape = getInputShape ();

			if (shape != null) {

				// shape Inputs
				if (shape.inputs != null && shape.inputs.Count > 0)
				{
					for (int i=0; i<shape.inputs.Count; i++)
						AllInput_Ps.Add(shape.inputs[i]);
				}

				// shape outputs
				AllOutput_Ps.Add (shape.difference);
				AllOutput_Ps.Add (shape.differenceRail);
				AllOutput_Ps.Add (shape.intersection);
				AllOutput_Ps.Add (shape.intersectionRail);
				AllOutput_Ps.Add (shape.union);
				AllOutput_Ps.Add (shape.grouped);
			}


			P_Axis 	  	= parametricObject.getParameter("Axis");

			// JITTER 
			P_JitterTranslation 	= parametricObject.getParameter("Jitter Translation");
			P_JitterRotation 		= parametricObject.getParameter("Jitter Rotation");
			P_JitterScale 			= parametricObject.getParameter("Jitter Scale");





		}

		 

		// POLL CONTROLS (every model.generate())
		public virtual void pollControlValuesFromParmeters()
		{
			//Debug.Log(parametricObject.Name +" pollControlValuesFromParmeters - parametersHaveBeenPolled="+parametersHaveBeenPolled);
			if (parametersHaveBeenPolled)
				return;
			parametersHaveBeenPolled = true;


			parametricObject.isActive =  (P_IsActive  != null) ? P_IsActive.boolval : true;
			 
			inputSrc_p 		= (P_Input  != null	&& P_Input.DependsOn  != null) 	? P_Input.DependsOn 						: null;
			inputSrc_po 	= (P_Input  != null	&& P_Input.DependsOn  != null) 	? P_Input.DependsOn.parametricObject 	: null;

			if (P_Axis != null) axis = (Axis)P_Axis.IntVal;

			jitterTranslationTool 	= ( P_JitterTranslation != null		&& P_JitterTranslation.DependsOn != null) 	? 	P_JitterTranslation.DependsOn.parametricObject.generator as JitterTool	: null;
			jitterRotationTool 		= ( P_JitterRotation 	!= null 	&& P_JitterRotation.DependsOn 	 != null) 	? 	P_JitterRotation.DependsOn.parametricObject.generator 	 as JitterTool	: null;
			jitterScaleTool 		= ( P_JitterScale 		!= null 	&& P_JitterScale.DependsOn 		 != null) 	?	P_JitterScale.DependsOn.parametricObject.generator 	 	 as JitterTool	: null;



		}

		public virtual void connectionMadeWith(AXParameter to_p, AXParameter from_p)
		{
			pollInputParmetersAndSetUpLocalReferences();

		}

		public virtual void connectionBrokenWith(AXParameter p)
		{

		}

		public virtual void deleteRequested()
		{

		}

		public bool hasMoreThanOneDependent()
		{
			AXParameter output = getPreferredOutputParameter();
			return (output != null && output.Dependents != null && output.Dependents.Count > 1);

		}

		[System.NonSerialized]
		public bool parametersHaveBeenPolled = false;


		public virtual float validateFloatValue(AXParameter p, float v)
		{

			return v;
		}
		public virtual float validateFloatValueWithMin(AXParameter P, float min, float def=1)
		{
			if (P != null) {
				if (P.FloatVal < min)
				{
					P.FloatVal = Math.Max (P.FloatVal, min);
					//P.intiateRipple_setFloatValueFromGUIChange(min);
				}
				//P.FloatVal = Math.Max (P.FloatVal, min);
				return P.FloatVal;
			}
			return def;

		}




		public virtual void parameterWasModified(AXParameter p)
		{

		}


		public virtual void preGenerate()
		{

			// Ths was an enormous optimization to not calculate the 
			// local matrix each time.
			// The real savings was probably not so much the reduction
			// of matrix operations, so much as 
			// the reduction of calls to geting parameter values based on name strings.
			parametricObject.setLocalMatrix();

			parametricObject.Output = getPreferredOutputParameter();

			// RESULTING MESHES


		}



		
		public virtual GameObject generate(bool makeGameObjects, AXParametricObject initiator_po, bool isReplica)
		{


			//if (parametricObject.grouper != null)
			//	Debug.Log ("Generator::generate ** "+parametricObject.grouper.Name +":"+ parametricObject.Name);



			return null;
		}
		
		


		public AXShape getInputShape()
		{
			AXShape inputShape = parametricObject.getShape("Input Shape");
			if (inputShape == null)
				inputShape = parametricObject.getShape("Input Shapes");
			
			
			if (inputShape == null)
				return null;
			
			return inputShape;
		}
		

		public virtual bool hasOutputsConnected()
		{
			//if (P_Output != null && P_Output.Dependents != null && P_Output.Dependents.Count > 0)
			//	return true;


			if (AllOutput_Ps != null && AllOutput_Ps.Count > 0)
			{
				for (int i=0; i<AllOutput_Ps.Count; i++)
				{
					if (AllOutput_Ps [i].Dependents != null && AllOutput_Ps [i].Dependents.Count > 0)
						return true;
				}
			}

			return false;
		}

		public virtual void clearOutputs()
		{
			if (AllOutput_Ps != null && AllOutput_Ps.Count > 0)
			{
				for (int i=0; i<AllOutput_Ps.Count; i++)
				{
					AllOutput_Ps[i].paths = null;
					AllOutput_Ps[i].mesh = null;
					AllOutput_Ps[i].meshes = null;
				}
			}
		}

		public virtual bool hasOutputsReady()
		{
			

				return false;
		}


		// RECURSIVE
		public void setHeadsAltered()
		{
			//Debug.Log(parametricObject.Name + "-----> " + AllOutput_Ps.Count);
			
			//
			bool isHead = true;

		
			// DEPENDENTS DOWNSTREAM CHAIN
			foreach(AXParameter output in AllOutput_Ps)
			{
				
				if (output.Dependents != null && output.Dependents.Count > 0) {
					foreach (AXParameter d in output.Dependents) {
						if (d != null) 
						{
							if (! (d.parametricObject.generator is IReplica))
								isHead = false;
							d.parametricObject.isAltered = true;
							d.parametricObject.generator.setHeadsAltered ();
						}
					}
				}

			}

			// GROUPER UPSTREAM
			if (parametricObject.grouper != null && ! parametricObject.grouper.isAltered)
			{
				parametricObject.grouper.isAltered  = true;
				parametricObject.grouper.generator.setHeadsAltered ();
			}

			// GROUPERS DOWNSTREAM
			if (parametricObject.Groupees != null && parametricObject.Groupees.Count > 0)
			{
				for (int i=0; i<parametricObject.Groupees.Count; i++)
				{
					if (! parametricObject.Groupees[i].isAltered)
						parametricObject.Groupees[i].generator.setHeadsAltered ();
				}
			}
	
			if (isHead)
				parametricObject.model.setAlteredHead(parametricObject);

		}

		public virtual bool isHead()
		{
			
			
			if (hasOutputsConnected() && parametricObject.grouper == null)
				return false;

			return true;
		}

		public bool quaternionIsValid(Quaternion q)
		{
			bool isNAN = float.IsNaN(q.x + q.y + q.z + q.w);

			bool isZero = (q.x == 0 && q.y == 0 && q.z == 0 && q.w == 0);

			return !(isNAN || isZero);
		}


		/// <summary>
		/// Gets the upstream source parameter.
		/// 
		/// Considers that the immediate source may be a pass-thru parameter. 
		/// Thus, search upstream until you find a source ancestor parameter that has PType of Output
		/// </summary>
		/// <returns>The upstream source parameter.</returns>
		/// <param name="input_p">Input p.</param>
		public static AXParameter getUpstreamSourceParameter(AXParameter input_p)
		{
			AXParameter upstream_p = null;

			if (input_p != null)
			{
				upstream_p = input_p;

				while (upstream_p.DependsOn != null && upstream_p.DependsOn.PType != AXParameter.ParameterType.Output)
					upstream_p = upstream_p.DependsOn;

				upstream_p = upstream_p.DependsOn;
			} 

			return upstream_p;

		}






		public virtual void onInputMeshAttachedWithBounds(AXParametricObject po){
			if (parametricObject.inputHasBeenAttached)
				return;
			
			parametricObject.inputHasBeenAttached = true;
			
			// alter the defaults to something relative to the size of the input mesh.
			

		}



	
	#region worldMatrices


		// This can be called either when a po is selected in the NodeGraphEditor
		// or when a po's transform has been edited
		// still need to think about what happens when a relation for say height alters anothe po's Trans_Y.
		public void adjustWorldMatrices()
		{
			//Debug.Log("****** !!!!! =============== adjustWorldMatrices: " + parametricObject.Name);
			// DOWN_STREAM
			getConsumerMatrix();

			// UP_STREAM
			setSelectedConsumerAndWorldMatrixOfAllInputs();
		}


		/*
		public virtual AXParametricObject getConsumer()
		{


		}
		*/












		/// <summary>
		/// Gets the DOWNSTREAM consumer matrix. (RECURSIVE)
		/// 
		/// 
		/// Grouper Special Case: the consumer of a node in a Grouper (its grouper is non-null) that does not have an Output.Dependent, uses the Grouper as its consumer
		/// 
		/// </summary>
		/// <returns>The consumer matrix.</returns>
		/// <param name="upStreamCaller">Up stream caller.</param>

		public Matrix4x4 getConsumerMatrix(int governor=0, AXParametricObject upStreamCaller = null)
		{
			//Debug.Log("[ ] --- [ ] >> "+parametricObject.Name+"::getConsumerMatrix upStreamCaller=" + ((upStreamCaller != null) ? upStreamCaller.Name : "NULL"));

			// Go recusively DOWN the consumer chain of downstream consumers
			// by selecting their cached selectedConsumer if there is one.
			// If no selected consumer, then select the first consumer in the output.dependents
			Matrix4x4 retM = Matrix4x4.identity;


			if (governor++ > 40)
			{
				Debug.Log("getConsumerMatrix governor hit)");
				return retM;
			}	

			// DOWNSTREAM


			// FIRST, GET DOWNSTREAM CONSUMER
			// if there is a selected consumer... should add that, if no consumer, try to get first one...

			AXParametricObject selectedConsumer = null;

			bool goUpstream = true;


			//Debug.Log("getConsumerMatrix for "+ parametricObject.Name);

			if (parametricObject.grouper != null && ! parametricObject.grouper.hasDependents() && ! parametricObject.hasDependents())
			{
				// Grouper Special Case: use the Grouper as the consumer...
				//Debug.Log("here A YES GROUPER IS CONSUMER");
				selectedConsumer = parametricObject.grouper;
				goUpstream = true;
			}

			else
			{
				selectedConsumer = parametricObject.getSelectedConsumer();
			} 




			//Debug.Log(parametricObject.Name + " " + parametricObject.generator.GetType() + " goUpstream = " + goUpstream);
			if ( selectedConsumer == null )
			{
				selectedConsumer = parametricObject.getConsumer(upStreamCaller);

				//Debug.Log("C");
			 }
			  


			if (parametricObject.generator is AX.Generators.Instance2D) 
			{
				goUpstream = false;
			}




			//Debug.Log(parametricObject.Name + " " + parametricObject.generator.GetType() + " " + goUpstream);

			// ADD DOWNSTREAM MATRIX FROM SELECTED CONSUMER'S (RECURSION)

			//if (selectedConsumer != null && selectedConsumer != this.parametricObject &&  parametricObject.grouper != null && selectedConsumer != parametricObject.grouper)
						//if (selectedConsumer != null && selectedConsumer != this.parametricObject &&  parametricObject.grouper != null) -- not sure why the grouper condition was in here...
			if (selectedConsumer != null && selectedConsumer != this.parametricObject )
			{
				//Debug.Log("selectedConsumer="+selectedConsumer.Name);

				retM = selectedConsumer.generator.getConsumerMatrix(governor, this.parametricObject);
			
			}
			
			// ADD THIS NODE'S TRANSFORM
			retM *= localMatrix;


			// TAKE THIS OPPORTUNITY TO SET THIS NODE"S WORLD_MATRIX_DISPLAY					
			//Debug.Log("here a "+parametricObject.Name);	


			if (parametricObject.is3D() || selectedConsumer == null)
			{
				//Debug.Log("HERE "+ parametricObject.getAxisRotationMatrix());
				if (parametricObject.is2D())
				{
					//Debug.Log("here b");	
					if (parametricObject.grouper != null && parametricObject.grouper.parametricObject != null)
					{
						//Debug.Log("here");
						retM = parametricObject.grouper.parametricObject.getAxisRotationMatrix() * retM;

					}
					else
						retM = parametricObject.getAxisRotationMatrix() * retM;
				}
				if (parametricObject.is3D())
					retM *= parametricObject.getAxisRotationMatrix() * parametricObject.getLocalAlignMatrix();
			}



			parametricObject.worldDisplayMatrix = retM;










			// UPSTREAM....


			//Debug.Log(",. ,. ,. ,. ,. >>> head upstream ?" + ((upStreamCaller != null) ? upStreamCaller.Name : " no caller"));

			// BEFORE FINISHING, 
			// ADD THIS NODE'S LOCAL SUB_LOCATION TRANSFORM 
			// BASED ON THE UPSTREAM_CALLER'S NAME OR ADDRESS


			if (upStreamCaller != null )
				retM *= getLocalConsumerMatrixPerInputSocket(upStreamCaller); 

			if (goUpstream)
			{
				/*
				if (this is Grouper && upStreamCaller != null) // assume upstream caller is a groupee
				{
					Debug.Log("NO... ### groupee "+ upStreamCaller.Name);
					//upStreamCaller.generator.setSelectedConsumerAndWorldMatrixOfAllInputs (0);
					return retM;

				} 
				*/


				if (selectedConsumer != null && selectedConsumer.generator != null)
					selectedConsumer.generator.setSelectedConsumerAndWorldMatrixOfAllInputs (0);
			}
			// validate
			 
			
			// RETURN UPSTREAM
			return retM;

		}


		 


		/// <summary>
		/// UPSTREAM (RECURSIVE)
		/// Sets the selected consumer and world matrix of all inputs.
		/// 
		/// Grouper Special Case: If the DependsOn.parametricObject is a Grouper, continue on and by-pass the Grouper
		/// </summary>
		/// <param name="governor">Governor.</param>
		public void setSelectedConsumerAndWorldMatrixOfAllInputs(int governor=0, AXParameter incomingToParameter = null)
		{
			
			// Do not set Selected Consumer of items under an IReplica
			if (this is IReplica)
				return;
			
			if (governor++ > 30)
			{
				Debug.Log("governor hit)");
				return;
			}	
			 
			//Debug.Log("-----------------------> " + this.parametricObject.Name + " ---> go... "+ ((incomingToParameter != null) ? incomingToParameter.Name:"none"));

			if(incomingToParameter != null)

			if (incomingToParameter != null && incomingToParameter.PType != AXParameter.ParameterType.Output)
			{
				// PASS THRU!
				if (incomingToParameter.DependsOn != null)
				{
					incomingToParameter.DependsOn.parametricObject.generator.setSelectedConsumerAndWorldMatrixOfAllInputs(governor, incomingToParameter.DependsOn);
				}
				return;

			} 


			//Debug.Log("<< setSelectedConsumerAndWorldMatrixOfAllInputs: " + parametricObject.Name);

			List<AXParameter> inputs = parametricObject.getAllInputParameters();

			//if (this is Grouper && parametricObject.Groupees != null)
			//	upstreamPOs.AddRange(parametricObject.Groupees);






			for (int i = 0; i < inputs.Count; i++)
			{

				AXParameter input = inputs [i];
				if (input.DependsOn == null) 
					continue;


				if ((this is Grouper) && input.Dependents != null && input.Dependents.Count > 0)
					continue; 
			
				if ((! (input.DependsOn.parametricObject.generator is Grouper)) && input.DependsOn.parametricObject.is3D () && input.DependsOn.Type == AXParameter.DataType.Spline)
					// for example, the StairProfile output
					continue;

				if (input.Type == AXParameter.DataType.MaterialTool)
					continue;
				
				AXParametricObject upStream_src_po = input.DependsOn.parametricObject;

				//Debug.Log(input.Name + " -----------> upStream_src_po.Name="+upStream_src_po.Name + "." + input.DependsOn.Name);

				
				if (input.DependsOn.PType == AXParameter.ParameterType.Output)
				{
					// SET THE SELECTED_CONSUME FOR THIS CHILD


					//Debug.Log ("&*&*&**&*&*&*&*__ setting consumer of "+ upStream_src_po.Name + " to " + this.parametricObject.Name);

					upStream_src_po.selectedConsumer = this.parametricObject;
					  
					  
					   
					// SET THE WORLD_DISPLAY_MATRIX FOR THIS CHILD
					Matrix4x4 adjuster = Matrix4x4.identity;
					//if (parametricObject.is3D() && upStream_src_po.is2D())
						//adjuster *= parametricObject.getAxisRotationMatrix2D(Axis.Y);

					adjuster *= getLocalConsumerMatrixPerInputSocket(upStream_src_po); //

					upStream_src_po.worldDisplayMatrix = parametricObject.worldDisplayMatrix * adjuster * upStream_src_po.localMatrix;
		 
				}
				else 
				{
					// pass thru! Move on upstream from input.DependsOn					 

					AXParameter upstream_p = getUpstreamSourceParameter(input);
					if (upstream_p != null)
						upStream_src_po = upstream_p.parametricObject;

					Matrix4x4 adjuster = Matrix4x4.identity;
	
					if (upStream_src_po != null)
					{
						//Debug.Log("ADJUSTER: "+this.parametricObject.Name+".getLocalConsumerMatrixPerInputSocket("+upStream_src_po.Name+")");
						adjuster *= getLocalConsumerMatrixPerInputSocket(upStream_src_po, input); //

						upStream_src_po.worldDisplayMatrix = parametricObject.worldDisplayMatrix * adjuster * upStream_src_po.localMatrix;
					}
				}

				// NOW, HEAD RECURSIVELY FURTHER UP_STREAM
				//Debug.Log("now call for " + input.DependsOn.parametricObject.Name);
				input.DependsOn.parametricObject.generator.setSelectedConsumerAndWorldMatrixOfAllInputs (governor, input.DependsOn);

				  
			}


			// GROUPEES ARE UPSTREAM by definition
			if (this is Grouper)
			{

				for (int i=0; i<parametricObject.Groupees.Count; i++)
				{
					AXParametricObject groupee = parametricObject.Groupees[i];



					if (! groupee.hasDependents())
					{
						groupee.selectedConsumer = this.parametricObject;

						// SET THE WORLD_DISPLAY_MATRIX FOR THIS CHILD
						groupee.worldDisplayMatrix = parametricObject.worldDisplayMatrix  * groupee.localMatrix;
						groupee.generator.setSelectedConsumerAndWorldMatrixOfAllInputs (governor);

					}
				}
				 
			}

		}









		public virtual Matrix4x4 getLocalConsumerMatrixPerInputSocket(AXParameter input_p)
		{

			if(input_p.DependsOn == null)
				return Matrix4x4.identity;

			return getLocalConsumerMatrixPerInputSocket(input_p.DependsOn.parametricObject, input_p);
		}

		public virtual Matrix4x4 getLocalConsumerMatrixPerInputSocket(AXParametricObject input_po)
		{
			return getLocalConsumerMatrixPerInputSocket(input_po, null);
		}


		public virtual Matrix4x4 getLocalConsumerMatrixPerInputSocket(AXParametricObject input_po, AXParameter input_p)
		{
			return Matrix4x4.identity;
		}


		#endregion










	



		public virtual AXParameter getPreferredOutputParameter()
		{
			// might normally return a parameter named output.
			// in the case of ShapeMerger, the shape's current merge pref is returned.
			if (parametricObject.is2D())
				return  parametricObject.getParameter("Output", "Output Shape", "Output Spline"); 

			return parametricObject.getParameter("Output Mesh", "Output");
			
		}

		
		
		public virtual AXParameter getPreferredInputParameter()
		{
			// might normally return a parameter named output.
			// in the case of ShapeMerger, the shape's current merge pref is returned.
			
			AXParameter input = parametricObject.getParameter("Input");
			
			if (input == null)
				input = parametricObject.getParameter("Input Spline");
			
			if (input == null)
				input = parametricObject.getParameter("Input Shape");

			if (input == null)
				input = parametricObject.getParameter("Node Shape");

			if (input == null)
				input = parametricObject.getParameter("Input Mesh");
			
			if (input == null)
				input = parametricObject.getParameter("Node Mesh");
			
			return input;
		}
		
		// GET_ALL_INPUT_PARAMETERS
		public virtual List<AXParameter> getPreferedInputParameters()
		{
			List<AXParameter> inputs = new List<AXParameter>();
			foreach(AXParameter p in parametricObject.parameters)
			{
				if (p.PType == AXParameter.ParameterType.Input && (p.Type == AXParameter.DataType.Spline || p.Type == AXParameter.DataType.Mesh || p.Type == AXParameter.DataType.MaterialTool))
					inputs.Add (p);
			}
			if (parametricObject.shapes != null)
			{
				foreach(AXShape shp in parametricObject.shapes)
				{
					foreach(AXParameter p in shp.inputs)
						if (p.PType == AXParameter.ParameterType.Input && (p.Type == AXParameter.DataType.Spline || p.Type == AXParameter.DataType.Mesh))
							inputs.Add (p);
				}
			}
			return inputs;
		}
		



		
		

		public virtual void calculateBounds()
		{

		}



		public void selectItem(int i)
		{
			selectedIndex = i;

			if (selectedIndices == null)
				selectedIndices = new List<int>();

			if (! selectedIndices.Contains(i))
				selectedIndices.Add(i);
		}

		public void toggleItem(int i)
		{
			selectedIndex = i;

			if (selectedIndices == null)
				selectedIndices = new List<int>();

			if (selectedIndices.Contains(i))
				selectedIndices.Remove(i);
			else
				selectedIndices.Add(i);
		}

		public void selectOnlyItem(int i)
		{
			deslectAllItems();

			selectItem(i);
		}

		public bool isSelected(int i)
		{
			return (selectedIndices != null && selectedIndices.Contains(i));

		}

		public void deslectAllItems()
		{
			selectedIndex = -1;
			selectedIndices = null;
		}
		
		
		
		
		
		
		
	} 
	
	
	
	
	public class Generator2D : AX.Generators.Generator
	{
	
		public override string GeneratorHandlerTypeName { get { return "GeneratorHandler2D"; } }
		

		// INPUTS
		public AXParameter P_Align_X;
		public AXParameter P_Align_Y;

		public AXParameter P_Trans_X;
		public AXParameter P_Trans_Y;

		public AXParameter P_Rot_Z;

		public AXParameter P_Unified_Scaling;
		public AXParameter P_Scale_X;
		public AXParameter P_Scale_Y;

		public AXParameter P_Flip_X;
		public AXParameter P_Flip_Y;


		public AXParameter P_Area;

		public override void initGUIColor ()
		{
			
			GUIColor 		= new Color (.85f, .8f, .9f, .9f);
			GUIColorPro 	= new Color(1f,.85f,1f,.8f);
			ThumbnailColor  = new Color(.6f,.5f,.6f);
		}
		

		// CONTROL

		public int 		alignX;
		public int 		alignY;

		public float 	transX;
		public float 	transY;

		public float 	rotZ;

		public bool 	unifiedScaling;
		public float 	scaleX;
		public float 	scaleY;

		public bool 	flipX;
		public bool 	flipY;



	
		// INIT_PARAMETRIC_OBJECT
		public override void init_parametricObject() 
		{
			base.init_parametricObject();

			parametricObject.positionControls 	= new AXNode("positionControls");



			parametricObject.addParameter(AXParameter.DataType.Option, AXParameter.ParameterType.PositionControl, 	"Axis", (int) Axis.Y);
			parametricObject.addParameter(AXParameter.DataType.Option, AXParameter.ParameterType.PositionControl, 	"Align_X", 0);
			parametricObject.addParameter(AXParameter.DataType.Option, AXParameter.ParameterType.PositionControl, 	"Align_Y", 0);
		
			// Translation
			parametricObject.addParameter(AXParameter.DataType.Float,  AXParameter.ParameterType.PositionControl, 	"Trans_X", 0f, -5000f, 5000f);
			parametricObject.addParameter(AXParameter.DataType.Float,  AXParameter.ParameterType.PositionControl, 	"Trans_Y", 0f, -5000f, 5000f);
		
			// Rotation
			parametricObject.addParameter(AXParameter.DataType.Float,  AXParameter.ParameterType.PositionControl, 	"Rot_Z", 0f, -720f, 720f);

			// Scale
			parametricObject.addParameter(AXParameter.DataType.Bool,  AXParameter.ParameterType.PositionControl,	"Unified_Scaling",  true);
			parametricObject.addParameter(AXParameter.DataType.Float,  AXParameter.ParameterType.PositionControl, 	"Scale_X", 1f, -500f, 500f);
			parametricObject.addParameter(AXParameter.DataType.Float,  AXParameter.ParameterType.PositionControl, 	"Scale_Y", 1f, -500f, 500f);
		
			// Flip
			parametricObject.addParameter(AXParameter.DataType.Bool,  AXParameter.ParameterType.PositionControl, 	"Flip_X", false);
			parametricObject.addParameter(AXParameter.DataType.Bool,  AXParameter.ParameterType.PositionControl, 	"Flip_Y", false);
		

		}

		
		// POLL INPUTS (only on graph change())
		public override void pollInputParmetersAndSetUpLocalReferences()
		{
			base.pollInputParmetersAndSetUpLocalReferences();

			//Debug.Log("pollInputParmetersAndSetUpLocalReferences");



			P_Input 	= parametricObject.getParameter("Input Shape", "Node Shape");
			P_Output 	= parametricObject.getParameter("Output Shape", "Output Spline");

			P_Area 	= parametricObject.getParameter("Area");
//			if (P_Area == null)
//				P_Area = parametricObject.addParameter(new AXParameter(AXParameter.DataType.Float, AXParameter.ParameterType.Output, "Area"));
//				parametricObject.outputsNode.addChild(P_Area);
//			}	 

			if (P_Output != null && P_Output.Name == "Output Spline")
				P_Output.Name = "Output Shape";
			
			P_Align_X 	= parametricObject.getParameter("Align_X");
			P_Align_Y 	= parametricObject.getParameter("Align_Y");

			P_Trans_X 	= parametricObject.getParameter("Trans_X");
			P_Trans_Y 	= parametricObject.getParameter("Trans_Y");

			P_Rot_Z   	= parametricObject.getParameter("Rot_Z");


			P_Unified_Scaling 	= parametricObject.getParameter("Unified_Scaling");
			P_Scale_X 	= parametricObject.getParameter("Scale_X");
			P_Scale_Y 	= parametricObject.getParameter("Scale_Y");

			P_Flip_X   	= parametricObject.getParameter("Flip_X");
			P_Flip_Y   	= parametricObject.getParameter("Flip_Y");



		}




		// POLL CONTROLS (every model.generate())
		public override void pollControlValuesFromParmeters()
		{
			//Debug.Log("Generator2D::pollControlValuesFromParmeters " + parametricObject.Name + " parametersHaveBeenPolled="+parametersHaveBeenPolled);

			if (parametersHaveBeenPolled)
				return;

			base.pollControlValuesFromParmeters();




			// CALCULATE LOCAL_MATRIX (AND HELPER MATRICES)

			// asset new parameters
			if (P_Unified_Scaling == null)
				P_Unified_Scaling = parametricObject.addParameter(AXParameter.DataType.Bool,  AXParameter.ParameterType.PositionControl, 	"Unified_Scaling", true);
			if (P_Scale_X == null)
				P_Scale_X = parametricObject.addParameter(AXParameter.DataType.Float,  AXParameter.ParameterType.PositionControl, 	"Scale_X", 1f, -500f, 500f);
			if (P_Scale_Y == null)
				P_Scale_Y = parametricObject.addParameter(AXParameter.DataType.Float,  AXParameter.ParameterType.PositionControl, 	"Scale_Y", 1f, -500f, 500f);
			if (P_Flip_X == null)
				P_Flip_X = parametricObject.addParameter(AXParameter.DataType.Bool,  AXParameter.ParameterType.PositionControl, 	"Flip_X", false);
			if (P_Flip_Y == null)
				P_Flip_Y = parametricObject.addParameter(AXParameter.DataType.Bool,  AXParameter.ParameterType.PositionControl, 	"Flip_Y", false);



			// -- TRANSLATION

			if (P_Trans_X != null)
			{
				// Validate Trans
				assertFloatValidity (ValidityType.Translation, P_Trans_X);
				assertFloatValidity (ValidityType.Translation, P_Trans_Y);


				// set Trans
				transX 		= P_Trans_X.FloatVal;
				transY 		= P_Trans_Y.FloatVal;

			}

			// -- ROTATION 

			if (P_Rot_Z != null)
			{
				// Validate Rot
				assertFloatValidity (ValidityType.Rotation, P_Rot_Z);

				// set Rot
				rotZ 		= P_Rot_Z.FloatVal;

			}

			// -- SCALE

			// Validate Scale
			if (P_Scale_X != null)
			{
				assertFloatValidity (ValidityType.Scaling, P_Scale_X);
				assertFloatValidity (ValidityType.Scaling, P_Scale_Y);

				// Set Scale
				unifiedScaling = P_Unified_Scaling.boolval;
				scaleX 		= P_Scale_X.FloatVal;
				scaleY 		= P_Scale_Y.FloatVal;



				flipX 		= P_Flip_X.boolval;
				flipY 		= P_Flip_Y.boolval;


			}

			localTranslationMatrix  		= Matrix4x4.TRS(new Vector3(transX, transY, 0), 	Quaternion.identity,  							Vector3.one);
			localRotationMatrix 			= Matrix4x4.TRS(Vector3.zero, 						Quaternion.Euler(0, 0, (flipX ? -rotZ : rotZ)), Vector3.one);
			localScaleMatrix				= Matrix4x4.TRS(Vector3.zero, 						Quaternion.identity, 							new Vector3((flipX ? -scaleX : scaleX), (flipY ? -scaleY : scaleY), 1));



			// This is not needed
			//Matrix4x4 flipM = Matrix4x4.TRS(Vector3.zero, 	Quaternion.identity, 	new Vector3((flipX ? -1 : 1), (flipY ? -1 : 1), 1));


			localUnscaledMatrix = localTranslationMatrix * localRotationMatrix;

			localMatrix  =  localScaleMatrix * localUnscaledMatrix;

		
				

			if (float.IsNaN (localMatrix.m00)  ||  AXUtilities.GetScale(localMatrix) == Vector3.zero)
				localMatrix = Matrix4x4.identity;
			

			// temporary
			//worldDisplayMatrix = getConsumerMatrix();

		}




		public override bool hasOutputsReady()
		{
			
			if (P_Output != null && ( (P_Output.paths != null && P_Output.paths.Count > 0) || P_Output.polyTree != null))
				return true;

			return false;
		}



		public override void parameterWasModified(AXParameter p)
		{
			switch(p.Name)
			{
			case "Scale_X":
				if(P_Unified_Scaling.boolval)
					P_Scale_Y.initiateRipple_setFloatValueFromGUIChange(p.FloatVal);
				break;
			case "Scale_Y":
				if(P_Unified_Scaling.boolval)
					P_Scale_X.initiateRipple_setFloatValueFromGUIChange(p.FloatVal);
				break;
			}
		}








		// GENERATOR_2D :: GENERATE
		public override GameObject generate(bool makeGameObjects, AXParametricObject initiator_po, bool isReplica)
		{
			if (! parametricObject.isActive)
				return null;


			if (P_Output == null) // for example, a ShapeMerger does not have a generic output
				return null;

			preGenerate ();

			P_Output.polyTree = null;

			AXShape.thickenAndOffset(ref P_Output,  P_Output); 

			deriveStatsETC(P_Output);

			
			//Debug.Log(parametricObject.stats_VertCount);
			//adjustWorldMatrices();

			return null;
			
		}




		public static void deriveStatsETC(AXParameter p)
		{

			//AXShape.thickenAndOffset(ref P_Output,  P_Output);

			// Get VERT count
			p.parametricObject.stats_VertCount = 0;


			// DERIVE STATS: verts, area, etc.
			if (p != null && p.getPaths() != null)
			{
				p.area = 0;

				foreach (Path path in p.getPaths())
				{
					p.parametricObject.stats_VertCount += (path.Count-1);

					if (Clipper.Orientation(path))
						p.area += AXGeometryTools.Utilities.pathArea(path);
					else
						p.area -= AXGeometryTools.Utilities.pathArea(path);
				}

				p.sendWasAlteredEvent();
			}


		}


		public override void calculateBounds()
		{
			Paths paths = (P_Output != null) ? P_Output.getPaths() : null;

			if (paths == null)
				return;

			IntRect cb = Clipper.GetBounds(P_Output.getPaths());
			Vector3 size = new Vector3(cb.right-cb.left, cb.bottom-cb.top, 0);///Archimatix.IntPointPrecision;

			Vector3 center = new Vector3(cb.left+size.x/2, cb.top+size.y/2, 0);
			parametricObject.bounds = new Bounds( (center/AXGeometryTools.Utilities.IntPointPrecision-new Vector3(transX, transY, 0)), size/AXGeometryTools.Utilities.IntPointPrecision);

			//Debug.Log(parametricObject.Name + " parametricObject.bounds="+parametricObject.bounds);
		}










		// **** BEGIN DEPRICATED - MOVED TO AXGeometryTools

		// CLONE_PATH
		public static Path clonePath(Path path)
		{
			if (path == null)
				return null;

			Path clonePath = new Path();

			for (int j=0; j<path.Count; j++)
				clonePath.Add(new IntPoint(path[j].X, path[j].Y));

			return clonePath;
		}

		// CLONE_PATHS
		public static Paths clonePaths(Paths paths)
		{
			if (paths == null)
				return null;

			Paths clonePaths 	= new Paths();

			for(int i=0; i<paths.Count; i++)
				clonePaths.Add (clonePath(paths[i]));

			return clonePaths;
		}




		// TRANSFORM_PATH
		public static Path transformPath(Path path, Matrix4x4 m)
		{
			if (path == null)
				return null;

			Path clonePath = new Path();
			IntPoint tmp_ip;

			for (int j=0; j<path.Count; j++)
			{
				tmp_ip	= path[j];

				Vector3 pt = new Vector3( (float)tmp_ip.X/(float)AXGeometryTools.Utilities.IntPointPrecision, (float)tmp_ip.Y/(float)AXGeometryTools.Utilities.IntPointPrecision, 0);

				pt = m.MultiplyPoint3x4(pt);

				tmp_ip = new IntPoint((int)(pt.x*AXGeometryTools.Utilities.IntPointPrecision), (int)(pt.y*AXGeometryTools.Utilities.IntPointPrecision));

				clonePath.Add(tmp_ip);
			}

			return clonePath;
		}

		// TRANSFORM_PATHS
		public static Paths transformPaths(Paths paths, Matrix4x4 m)
		{
			//Debug.Log(m);
			if (paths == null)
				return null;

			Paths clonePaths = new Paths();
			Path clonePath = null;

			for(int i=0; i<paths.Count; i++)
			{
				clonePath = transformPath(paths[i], m);

				//if (Clipper.Orientation(clonePath) != Clipper.Orientation(paths[i]))
				//	clonePath.Reverse();

				clonePaths.Add (clonePath);
			}
			return clonePaths;
		}

		// TRANSFORM_POLY_TREE
		public static void transformPolyTree(AXClipperLib.PolyTree polyTree, Matrix4x4 m)
		{
			
		//Debug.Log(m);
			if (polyTree == null)
				return;

			if (polyTree.Childs != null && polyTree.Childs.Count > 0)
				transformPolyNode(polyTree.Childs, m);
		}

		// TRANSFORM_POLY_NODE
		public static void transformPolyNode(List<PolyNode> childs, Matrix4x4 m)
		{
			if (childs == null || childs.Count == 0)
				return;

			foreach(PolyNode child in childs)
			{
				Path tmpPath = transformPath(child.Contour, m);
				for (int i = 0; i < tmpPath.Count; i++) 
					child.Contour[i] = tmpPath [i];

				if (child.Childs != null)
					transformPolyNode(child.Childs, m);
			}
			
		}




		// **** END DEPRICATED - MOVED TO AXGeometryTools



	}






	public class Generator3D : AX.Generators.Generator
	{
		public override string GeneratorHandlerTypeName { get { return "GeneratorHandler3D"; } }


		// INPUTS
		public AXParameter P_MaterialTool;

		public AXParameter P_Align_X;
		public AXParameter P_Align_Y;
		public AXParameter P_Align_Z;

		public AXParameter P_Trans_X;
		public AXParameter P_Trans_Y;
		public AXParameter P_Trans_Z;

		public AXParameter P_Rot_X;
		public AXParameter P_Rot_Y;
		public AXParameter P_Rot_Z;

		public AXParameter P_Scale_X;
		public AXParameter P_Scale_Y;
		public AXParameter P_Scale_Z;

		public AXParameter P_Size_X;
		public AXParameter P_Size_Y;
		public AXParameter P_Size_Z;


		// CONTROL

		public int alignX;
		public int alignY;
		public int alignZ;

		public float transX;
		public float transY;
		public float transZ;

		public float rotX;
		public float rotY;
		public float rotZ;

		public float scaleX;
		public float scaleY;
		public float scaleZ;


		public float sizeX;
		public float sizeY;
		public float sizeZ;

		public Generator3D()
		{
			Init();
		}
		public override void Init()
     	{
     		base.Init();
     	}	



		// INIT_PARAMETRIC_OBJECT
		public override void init_parametricObject() 
		{
			base.init_parametricObject();

			parametricObject.positionControls 	= new AXNode("positionControls");


			parametricObject.addParameter(AXParameter.DataType.Option, AXParameter.ParameterType.PositionControl, 	"Axis", 			(int) Axis.Y);
			parametricObject.addParameter(AXParameter.DataType.Option, AXParameter.ParameterType.PositionControl, 	"Align_X", 			0);
			parametricObject.addParameter(AXParameter.DataType.Option, AXParameter.ParameterType.PositionControl, 	"Align_Y", 			0);
			parametricObject.addParameter(AXParameter.DataType.Option, AXParameter.ParameterType.PositionControl, 	"Align_Z", 			0);
		 
			// translation
			parametricObject.addParameter(AXParameter.DataType.Float,  AXParameter.ParameterType.PositionControl, 	"Trans_X", 			0f, -5000f, 5000f);
			parametricObject.addParameter(AXParameter.DataType.Float,  AXParameter.ParameterType.PositionControl, 	"Trans_Y", 			0f, -5000f, 5000f);
			parametricObject.addParameter(AXParameter.DataType.Float,  AXParameter.ParameterType.PositionControl, 	"Trans_Z", 			0f, -5000f, 5000f);
	
			// rotation
			parametricObject.addParameter(AXParameter.DataType.Float,  AXParameter.ParameterType.PositionControl, 	"Rot_X", 			0f, -360f, 360f);
			parametricObject.addParameter(AXParameter.DataType.Float,  AXParameter.ParameterType.PositionControl, 	"Rot_Y", 			0f, -360f, 360f);
			parametricObject.addParameter(AXParameter.DataType.Float,  AXParameter.ParameterType.PositionControl, 	"Rot_Z", 			0f, -720f, 720f);
		
			// scale
			parametricObject.addParameter(AXParameter.DataType.Float,  AXParameter.ParameterType.PositionControl, 	"Scale_X", 			1f, -100, 100);
			parametricObject.addParameter(AXParameter.DataType.Float,  AXParameter.ParameterType.PositionControl, 	"Scale_Y", 			1f, -100, 100);
			parametricObject.addParameter(AXParameter.DataType.Float,  AXParameter.ParameterType.PositionControl, 	"Scale_Z", 			1f, -100, 100);
		


		}

		// POLL INPUTS (only on graph change())
		public override void pollInputParmetersAndSetUpLocalReferences()
		{
			base.pollInputParmetersAndSetUpLocalReferences();

			P_MaterialTool = parametricObject.getParameter("Material");

			P_Align_X 	= parametricObject.getParameter("Align_X");
			P_Align_Y 	= parametricObject.getParameter("Align_Y");
			P_Align_Z 	= parametricObject.getParameter("Align_Z");

			P_Trans_X 	= parametricObject.getParameter("Trans_X");
			P_Trans_Y 	= parametricObject.getParameter("Trans_Y");
			P_Trans_Z 	= parametricObject.getParameter("Trans_Z");

			P_Rot_X   	= parametricObject.getParameter("Rot_X");
			P_Rot_Y   	= parametricObject.getParameter("Rot_Y");
			P_Rot_Z   	= parametricObject.getParameter("Rot_Z");

			P_Scale_X 	= parametricObject.getParameter("Scale_X");
			P_Scale_Y 	= parametricObject.getParameter("Scale_Y");
			P_Scale_Z 	= parametricObject.getParameter("Scale_Z");

			P_Size_X 	= parametricObject.getParameter("SizeX");
			P_Size_Y 	= parametricObject.getParameter("SizeY");
			P_Size_Z 	= parametricObject.getParameter("SizeZ");

		}

		// POLL CONTROLS (every model.generate())
		public override void pollControlValuesFromParmeters()
		{

			if (parametersHaveBeenPolled)
				return; 

			base.pollControlValuesFromParmeters();



			// CALCULATE LOCAL_MATRICES

			alignX 	= (P_Align_X != null) 	? P_Align_X.IntVal : 0;
			alignY 	= (P_Align_Y != null) 	? P_Align_Y.IntVal : 0;
			alignZ 	= (P_Align_Z != null) 	? P_Align_Z.IntVal : 0;

			transX 	= (P_Trans_X != null) 	? P_Trans_X.FloatVal : 0;
			transY 	= (P_Trans_Y != null) 	? P_Trans_Y.FloatVal : 0;
			transZ 	= (P_Trans_Z != null) 	? P_Trans_Z.FloatVal : 0;

			rotX 	= (P_Rot_X != null) 	? P_Rot_X.FloatVal : 0; 
			rotY 	= (P_Rot_Y != null) 	? P_Rot_Y.FloatVal : 0;
			rotZ 	= (P_Rot_Z != null) 	? P_Rot_Z.FloatVal : 0;

			scaleX 	= (P_Scale_X != null) 	? P_Scale_X.FloatVal : 1;
			scaleY 	= (P_Scale_Y != null) 	? P_Scale_Y.FloatVal : 1;
			scaleZ 	= (P_Scale_Z != null) 	? P_Scale_Z.FloatVal : 1;

			sizeX 	= (P_Size_X != null) 	? P_Size_X.FloatVal : 1;
			sizeY 	= (P_Size_Y != null) 	? P_Size_Y.FloatVal : 1;

			sizeY 	= validateFloatValueWithMin(P_Size_Y, .01f);

			sizeZ 	= (P_Size_Z != null) 	? P_Size_Z.FloatVal : 1;


			//Debug.Log(parametricObject.Name+" POLLING* * * * * * parametricObject.materialTool = "+parametricObject.materialTool + " === " + parametricObject.consumerMaterialTool );

				
			if (parametricObject.axMat == null)
			{
				if (parametricObject.grouper != null && parametricObject.grouper.axMat != null)
					parametricObject.axMat = parametricObject.grouper.axMat;
				else
					parametricObject.axMat = parametricObject.model.axMat;
			}



			if (parametricObject.axTex == null)
				parametricObject.axTex = parametricObject.model.axTex; 

			

			localMatrix = Matrix4x4.TRS(new Vector3(transX, transY, transZ), Quaternion.Euler(rotX, rotY, rotZ), new Vector3(scaleX, scaleY, scaleZ));

			localMatrixWithAxisRotationAndAlignment = localMatrix * parametricObject.getAxisRotationMatrix() * parametricObject.getLocalAlignMatrix();

			 

		}


		public void setCubeMeshFromBounds(Bounds b)
		{
			parametricObject.boundsMesh = new Mesh();

			float x1 = b.center.x - b.extents.x;
			float x2 = b.center.x + b.extents.x;

			float y1 = b.center.y - b.extents.y;
			float y2 = b.center.y + b.extents.y;

			float z1 = b.center.z - b.extents.z;
			float z2 = b.center.z + b.extents.z;


			Vector3[] vertices = new Vector3[8]; 

			vertices[0] = new Vector3(x1, y1, z1);
			vertices[1] = new Vector3(x2, y1, z1);
			vertices[2] = new Vector3(x2, y1, z2);
			vertices[3] = new Vector3(x1, y1, z2);

			vertices[4] = new Vector3(x1, y2, z1);
			vertices[5] = new Vector3(x2, y2, z1);
			vertices[6] = new Vector3(x2, y2, z2);
			vertices[7] = new Vector3(x1, y2, z2);

			parametricObject.boundsMesh.vertices = vertices;

			// we don't need uvs or triangles for the purposes of a bounds mesh.

		}




		public void setBoundsWithCombinator(CombineInstance[] boundsCombinator)
		{

			// FINISH BOUNDS
			Mesh tmpBoundsMesh = new Mesh();
			tmpBoundsMesh.CombineMeshes(boundsCombinator);
			tmpBoundsMesh.RecalculateBounds();

			parametricObject.bounds = tmpBoundsMesh.bounds;
			setCubeMeshFromBounds(tmpBoundsMesh.bounds);
		}


		public void setBoundsFromPOs(List<AXParametricObject> POs)
		{

			List<AXMesh> boundingMeshes = new List<AXMesh>();



			// Process


			for (int i = 0; i < POs.Count; i++) 
			{
				if (POs[i].is3D() && POs[i].boundsMesh != null)
					boundingMeshes.Add(new AXMesh(POs[i].boundsMesh, POs[i].generator.localMatrixWithAxisRotationAndAlignment));
			}

			CombineInstance[] boundsCombinator = new CombineInstance[boundingMeshes.Count];
			for(int bb=0; bb<boundsCombinator.Length; bb++)
			{
				boundsCombinator[bb].mesh 		= boundingMeshes[bb].mesh;
				boundsCombinator[bb].transform 	= boundingMeshes[bb].transMatrix;
			}
			setBoundsWithCombinator(boundsCombinator);

			P_Size_X.FloatVal = parametricObject.bounds.size.x;
			P_Size_Y.FloatVal = parametricObject.bounds.size.y;
			P_Size_Z.FloatVal = parametricObject.bounds.size.z;



			//Debug.Log("P_Size_X="+P_Size_X.FloatVal+", P_BoundsX="+P_BoundsX.FloatVal);

		}



		  
		public void setBoundaryFromAXMeshes(List<AXMesh> ax_meshes)
		{
			if (ax_meshes.Count == 0)
				return;

			//Debug.Log(" +++++++++++++ setBoundaryFromAXMeshes ++++++++++++++++  " + parametricObject.Name + " : " + ax_meshes.Count);


			
			CombineInstance[] combinatorAll = new CombineInstance[ax_meshes.Count];
			int combineAllCt = 0;
			for (int i = 0; i < ax_meshes.Count; i++) {
				AXMesh axmesh = ax_meshes [i];
				//Debug.Log(" using Actual mesh ... " + parametricObject.Name + " from ? transMatrix: -- :: " + axmesh.transMatrix );
				combinatorAll [combineAllCt].mesh 		= axmesh.mesh;				
				//combinatorAll [combineAllCt].transform 	= parametricObject.localMatrix.inverse * axmesh.transMatrix;
				combinatorAll [combineAllCt].transform 	= parametricObject.localMatrix.inverse * axmesh.transMatrix;
				combineAllCt++;
			}
			Mesh allMesh = new Mesh();
			allMesh.CombineMeshes(combinatorAll);

			allMesh.RecalculateBounds();
			parametricObject.bounds = allMesh.bounds;

			setCubeMeshFromBounds(allMesh.bounds);

	


		}









		public override GameObject generate(bool makeGameObjects, AXParametricObject initiator_po, bool isReplica)
		{
			Debug.Log ("Made a AX.Generators.Generator3D!");

			return null;
		}



		public override void connectionMadeWith(AXParameter to_p, AXParameter from_p)
		{
			//Debug.Log("connectionMadeWith........................"+from_p.parametricObject.Name+":"+from_p.Name);

			base.connectionMadeWith(to_p, from_p);

			 
			/*
			if (to_p.Name == "Material")
			{
				Debug.Log("Connecting texturetool");
				parametricObject.materialTool = from_p.parametricObject.generator as MaterialTool;
				AXParametricObject.setConsumerMaterialToolRecursive(this.parametricObject, (from_p.parametricObject.generator as MaterialTool));
			}
			*/

		}



		public override void connectionBrokenWith(AXParameter p)
		{


			//Debug.Log("connection broken with "+ p.Name);
			if (p.Name == "Material")
			{
				parametricObject.materialTool = null;
			}

		}
		 

		
	}	
	
	
	
	
	
	
	
	
	
	
	



}