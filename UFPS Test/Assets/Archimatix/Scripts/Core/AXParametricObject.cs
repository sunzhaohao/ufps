using UnityEngine;

#if UNITY_EDITOR  
using UnityEditor;

#endif

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

using AX.SoftCircuits;

using System.Linq;

using AX.SimpleJSON; 




using AXGeometryTools;


using AX.Generators;


using AXClipperLib;

using Path 		= System.Collections.Generic.List<AXClipperLib.IntPoint>;
using Paths 	= System.Collections.Generic.List<System.Collections.Generic.List<AXClipperLib.IntPoint>>;

using Curve		= System.Collections.Generic.List<AXGeometryTools.CurvePoint>;
using Curve3D	= System.Collections.Generic.List<AXGeometryTools.CurvePoint3D>;

namespace AX
{
	[Flags]
	public enum AXStaticEditorFlags
	{
		LightmapStatic = 1,
		OccluderStatic = 2,
		OccludeeStatic = 16,
		BatchingStatic = 4,
		NavigationStatic = 8,
		OffMeshLinkGeneration = 32,
		ReflectionProbeStatic = 64
	}


	// AX_PARAMETRIC_OBJECT B

	[System.Serializable]
	public class AXParametricObject : AXNode
	{
		// GENERATOR
		// extends the functionality of a ParamertricObject
		// by allowing any default Parameters to be set or generate logic
		// within a subclass. 

		[System.NonSerialized]
		private AX.Generators.Generator m_generator;
		public  AX.Generators.Generator generator
		{
			get { return m_generator; } 
			set { m_generator = value; }
		}  



		public bool isActive = true;

		public bool noMeshRenderer = false;


		[System.NonSerialized]
		public bool isAltered;

		[SerializeField]
		public AXSpline spline = new AXSpline();

		[SerializeField]
		public Curve curve = new Curve();

		[SerializeField]
		public Curve3D curve3D = new Curve3D();



		[SerializeField]
		private string m_guid	= System.Guid.NewGuid().ToString();
		public  string	Guid   
		{
			get  { return m_guid;  }
			set  { m_guid = value; }
		}

		[SerializeField]
		private string m_type;
		public  string 	Type
		{
			get { return m_type; }
			set { m_type = value; }
		}

		public Matrix4x4 worldDisplayMatrix;

		 
		 
		/// <summary>
		///  PROTOTYPE GAMEOBJECT
		///  AX stores component additions to a node in a GameObject prototype, which is stored in the AXModel hierarchy under prototypes.
		///  Not all nodes need a prototype, just those that need to attach Components as they generate their GameObjects.
		/// </summary>
		public GameObject prototypeGameObject;


		public void createGameObjectPrototype()
		{
			// if null, create one
			if (prototypeGameObject == null)
			{
				prototypeGameObject = model.createNewPrototypeGameObject();
			}
		}
		public bool displayPrototypes;

		public void copyComponentsFromPrototypeToGameObject(GameObject go)
		{
			if (prototypeGameObject != null)
			{
				Component[] components = prototypeGameObject.GetComponents<Component>();
        		for (int i = 0; i < components.Length; i++)
        		{
           			Component c = components[i];
					//Type collectionType = c.GetType();

					 
					//if ((collectionType is Transform))
					//	continue;

					if ((c is AXPrototype))
						continue;

					
					else 
					{
						//Component nc = go.AddComponent(collectionType);
						//EditorUtility.CopySerialized(c, nc);
					}

 				}


			}
		}





		#region ~ GROUPER SUPPORT #

		/// <summary>
		/// The grouper key.
		/// Each node can belong to one and only one grouper.
		/// If outside access is needed to this node, then 
		/// a proxy should be set up in the grouper to allow for input.
		/// </summary>
		public string 					grouperKey 	= null;

		// These are live values only - for convenience
		// * don't serialize these, it will only make copies on deserialize
		// The parent is set when the PO that owns this Parameter (in its serialized List)
		// instantiates it or is creating graph links
		[System.NonSerialized]
		private AXParametricObject		_grouper				= null;
		public 	AXParametricObject		grouper   
		{
			get  { return _grouper; }
			set  { _grouper = value; }
		}





		// GROUPER

		public void attachToGrouperPO(AXParametricObject grouper_po)
		{
			grouper_po.addGroupee(this);



		}





		// GROUPEES

		[System.NonSerialized]
		private List<AXParametricObject> 		groupees 			= new List<AXParametricObject>();
		public 	List<AXParametricObject>		Groupees  
		{
			get  { 	if (groupees == null)
				groupees = new List<AXParametricObject>();
				return groupees;  
			}
			set  { groupees = value; }
		}


		public Rect getBoundsRect()
		{

			float grouperBG_Margin = 200;

			Rect grouperCanvasRect = new Rect(rect.x + rect.width/2, rect.y-grouperBG_Margin, 2*(rect.width + grouperBG_Margin), rect.height+2*grouperBG_Margin) ;


			if ( Groupees.Count > 0)
			{

				grouperCanvasRect = AXUtilities.getBoundaryRectFromPOs(Groupees, grouperBG_Margin);

				if (float.IsNaN(rect.x))
					rect.x = 0;

				if (float.IsNaN(rect.y))
					rect.y = 0;


				// add distance from rect.x to grouper
				float diffx = grouperCanvasRect.x - rect.x - rect.width/2;

				grouperCanvasRect.x -= diffx;
				grouperCanvasRect.width += diffx; 


				if (grouperCanvasRect.y > rect.y-grouperBG_Margin)
				{
					float diffy = grouperCanvasRect.y - rect.y +grouperBG_Margin;

					grouperCanvasRect.y -= diffy;
					grouperCanvasRect.height += diffy; 

				}



				if (grouperCanvasRect.y+grouperCanvasRect.height < (rect.y + rect.height + grouperBG_Margin) )
				{
					// add height

					float diffh = (rect.y + rect.height) - (grouperCanvasRect.y+ grouperCanvasRect.height);
					grouperCanvasRect.height += diffh + grouperBG_Margin;
				}

			}

			return grouperCanvasRect;


		}







		// POP_PO's_FROM_GROUP //

		public void popPOsFromGroup(List<AXParametricObject> poList)
		{
			if (poList == null || poList.Count == 0)
				return;


			
			List<AXParametricObject> all_nodes_to_pop = new List<AXParametricObject>();

			for (int i=0; i< poList.Count; i++)
			{
				all_nodes_to_pop =  all_nodes_to_pop.Union(  poList[i].gatherSubnodes_SelectedOrHidden() ).ToList();
			}
			all_nodes_to_pop = all_nodes_to_pop.Union(poList).ToList();



			// ASSUME THAT ALL THE SELECTED PO's ARE IN THE SAME GROUP
			AXParametricObject fromGroup = poList[0].grouper;

			if (fromGroup == null)
				return;



			// RELATIONS PROXIES

			//Debug.Log("RELATIONS PROXIES::::::::::::::::::::::::::::::::::::::::::::::::::::::");
			List<AXParameter> controlParamsToRewire = new List<AXParameter>();
			List<AXRelation> relationsToDelete = new List<AXRelation>();

			for (int i=0; i<all_nodes_to_pop.Count; i++)
			{
				//Debug.Log(" +++ " +all_new_nodes[i].Name);

				if (all_nodes_to_pop[i].geometryControls == null)
					continue;

				List<AXNode> controlParams = all_nodes_to_pop[i].geometryControls.children;
				if (all_nodes_to_pop[i].positionControls != null)
					controlParams.AddRange(all_nodes_to_pop[i].positionControls.children);

				
				for(int j=0; j<controlParams.Count; j++)
				{
					AXParameter controlParam = (AXParameter) controlParams[j];

					//Debug.Log("---"+controlParam.parametricObject+"."+controlParam.Name);
					foreach(AXRelation relation in controlParam.relations)
					{
						AXParameter rel_p = ( relation.pA == controlParam) ? relation.pB : relation.pA;

						//Debug.Log("related to " + rel_p.parametricObject.Name + "." + rel_p.Name);

						if (rel_p != null && ! all_nodes_to_pop.Contains(rel_p.parametricObject) && ! controlParamsToRewire.Contains(rel_p))
						{

							// Make sure this is not a relation going to its own Groupee
							if ( rel_p.parametricObject != fromGroup  &&  (rel_p.parametricObject.grouper == null || rel_p.parametricObject.grouper != controlParam.parametricObject))
								controlParamsToRewire.Add(rel_p);
						}
					}
				} 
			}



			//Debug.Log("controlParamsToRewire.Count="+controlParamsToRewire.Count);
			foreach (AXParameter rel_p in controlParamsToRewire)
			{ 
				// is the name of rel_p one of the reserved names SizeX, SizeY or SizeZ?
				// If so, then just link to it.
				AXParameter proxy_p = null;

				//if (rel_p.Name == "SizeX" || rel_p.Name == "SizeX" || rel_p.Name == "SizeX")
				//	proxy_p = fromGroup.getParameter(rel_p.Name);

				//if (proxy_p == null) // Add a control parameter to this PO
				//	proxy_p = fromGroup.addParameter(new AXParameter(rel_p.Type, rel_p.PType, rel_p.Name));




				//proxy_p.FloatVal 	= rel_p.FloatVal;
				//proxy_p.IntVal		= rel_p.IntVal;
				//proxy_p.boolval		= rel_p.boolval;


				List<AXParameter> parametersToLinkToProxyP =  new List<AXParameter>();

				foreach(AXRelation relation in rel_p.relations) 
				{
					//AXParameter poppnP = (relation.pA == rel_p) ? relation.pA : relation.pB;
					AXParameter otherP = (relation.pA == rel_p) ? relation.pB : relation.pA;


					if (otherP != null)
					{
						if (otherP.parametricObject == fromGroup)
							proxy_p = otherP;
						else
						{
							parametersToLinkToProxyP.Add(otherP);
							relationsToDelete.Add(relation);

						}
					}
				}

				if (proxy_p == null)
				{
					proxy_p = fromGroup.addParameter(new AXParameter(rel_p.Type, AXParameter.ParameterType.GeometryControl, rel_p.parametricObject.Name+"_"+rel_p.Name));
					proxy_p.FloatVal 	= rel_p.FloatVal;
					proxy_p.IntVal		= rel_p.IntVal;
					proxy_p.boolval		= rel_p.boolval;
				}

				for (int i = 0; i < parametersToLinkToProxyP.Count; i++) {

					AXParameter del_p = parametersToLinkToProxyP [i];

					//Debug.Log (" +++ +++ TRY Remap ["+i+"] ->" + del_p.parametricObject.Name + " : " +  del_p.Name);
					if (all_nodes_to_pop.Contains (del_p.parametricObject)) {
						//Debug.Log (" +++ +++ Remap->" + del_p.parametricObject.Name + " : " +  del_p.Name);


						// Replace relationsToDelete[i] with this new relation
						// remap the guids, replacing the [partner of del_p] guid with the proxy_p guid
						AXRelation 	relToReplace = relationsToDelete[i];

						AXRelation newrel = model.relate(del_p, proxy_p);

						newrel.expression_AB = relToReplace.expression_AB.Replace(ArchimatixUtils.guidToKey(del_p.Guid), ArchimatixUtils.guidToKey(proxy_p.Guid));
						newrel.expression_BA = relToReplace.expression_BA.Replace(ArchimatixUtils.guidToKey(del_p.Guid), ArchimatixUtils.guidToKey(proxy_p.Guid));



					}
				}    

				model.relate(proxy_p, rel_p);

			}
			//Debug.Log(" releationsToDelete="+relationsToDelete.Count);
			foreach(AXRelation relation in relationsToDelete)
				model.unrelate(relation);








			foreach(AXParametricObject po in all_nodes_to_pop)
			{
				if (po.grouper == model.currentWorkingGroupPO)
					po.popFromCurrentGroup();
			}


			model.cleanGraph();



		}

		public void popFromCurrentGroup()
		{

			//Debug.Log("pop from group " + Name);

			if (grouper == null)
				return;




			AXParametricObject newGrouper = grouper.grouper;


			// modify connections

			// Go through each Input and see if it is connected to a parameter of the currentGrouper,
			// If so, then break the connection and connect directly to the upstream source.

			foreach (AXParameter input in getAllInputParameters())
			{
				AXParameter sourceP = input.DependsOn;

				if (sourceP != null)
				{
					//Debug.Log(input.Name);


					if (sourceP.parametricObject == grouper)
					{
						AXParameter upstreamSourceP = sourceP.DependsOn;

						// Go upstream
						if (upstreamSourceP != null)
						{
							if (sourceP.Dependents.Count == 1)
								sourceP.parametricObject.removeParameter(sourceP);

							// connect directly
							input.makeDependentOn(upstreamSourceP);
						}
					}
				}
			}



			// OUTUT DEPENDENTS - PASS THROUGH PROXY?

			List<AXParameter> paramsOut = getAllOuputs();

			for(int j=0; j<paramsOut.Count; j++)
			{
				//Debug.Log(" --- " + paramers[j].Name + " " + (( paramers[j].DependsOn != null) ? paramers[j].DependsOn.Name : "NULL"));

				AXParameter groupeeToPop_output_p =  paramsOut[j];

				List<AXParameter> dependentsToMakeDependentOnNewProxy = new List<AXParameter>();

				if (groupeeToPop_output_p.Dependents != null && groupeeToPop_output_p.Dependents.Count>0)
				{
					AXParameter newProxyParameter = null;

					for (int k = 0; k < groupeeToPop_output_p.Dependents.Count; k++) {
						AXParameter dependent = groupeeToPop_output_p.Dependents [k];


						// IS THIS DEPENDENT's PO STAYING IN GROUP? 
						// If it is NOT selected, then we can asume it isstaying behind.
						if (! model.isSelected(dependent.parametricObject))
						{
							// Not selected, so YES, STAYING BEHIND - CONNECT TO THIS DEPENDENT THROUGH PROXY

							// Create Proxy Parameter
							// On the first one. For the other dependents, use the same proxy
							if (newProxyParameter == null)
								newProxyParameter = grouper.addParameter(new AXParameter(groupeeToPop_output_p.Type, AXParameter.ParameterType.Input, groupeeToPop_output_p.Dependents[0].Name));

							dependentsToMakeDependentOnNewProxy.Add(groupeeToPop_output_p.Dependents[k]);
							//groupeeToPop_output_p.Dependents[k].makeDependentOn(newProxyParameter);
						}

					}

					foreach(AXParameter d in dependentsToMakeDependentOnNewProxy)
						d.makeDependentOn(newProxyParameter);


					if (newProxyParameter != null)
						newProxyParameter.makeDependentOn(groupeeToPop_output_p);


				}




			}






	



			// detatch
			grouper.Groupees.Remove(this);

			if (newGrouper != null)
			{
				newGrouper.addGroupee(this);
			} 
			else
			{
				grouper = null;
				grouperKey = null;
			}


			model.autobuild();
		}


		public void addGroupee(AXParametricObject groupee_po)
		{
			//Debug.Log(" adding " + groupee_po.Name);

			// Persistent Connection
			groupee_po.grouperKey = Guid;

			// Live Connections
			groupee_po.grouper = this;
			if (! Groupees.Contains(this))
				Groupees.Add(groupee_po);

			//groupee_po.attachToGrouperPO(this);

			//addGrouperUpstream(groupee_po, this);

			// go through groupee's up stream hiddens and 
			// add any found to group
			// if they do not have a grouper and they only have on dependent
			// if they do have more than one dependent, then add a grouper parameter and feed them through that.

			/*
			List<AXParametricObject> all_new_nodes = groupee_po.gatherSubnodes_SelectedOrHidden();
			for (int i=0; i<all_new_nodes.Count; i++)
			{				
				//Debug.Log("A D D ::: " + all_new_nodes[i].Name + ". " +all_new_nodes[i].grouperKey + " ---- " + Guid);
				if (String.IsNullOrEmpty(all_new_nodes[i].grouperKey) || (grouper != null && all_new_nodes[i].grouperKey == grouper.Guid) )
				{

					if (model.currentWorkingGroupPO == null || model.currentWorkingGroupPO != all_new_nodes[i])
					{
						all_new_nodes[i].grouperKey = Guid;
						all_new_nodes[i].grouper = this;
						Groupees.Add(all_new_nodes[i]);
					}
				}
			}
			*/

		} 


		public void addGroupees(List<AXParametricObject> new_groupees)
		{


			// 1. Make a list of all pos and sub pos of these new groupees

			// 2. Use this list to find all connections leaving this group set 		

			// 3. Make and rig up proxy parameters, consolodating where possible.

			//Debug.Log("Add GROUPEES " + new_groupees.Count);




			List<AXParametricObject> all_new_nodes = new List<AXParametricObject>();


			for (int i=0; i<new_groupees.Count; i++)
			{
				all_new_nodes =  all_new_nodes.Union( new_groupees[i].gatherSubnodes_SelectedOrHidden() ).ToList();
			}
			all_new_nodes = all_new_nodes.Union(new_groupees).ToList();


			//Debug.Log("new_groupees.Count = " + new_groupees.Count);
			//Debug.Log("all_new_nodes.Count = " + all_new_nodes.Count + " :: " + all_new_nodes[0].Name);

			// allnodes is now a distinct list of all the nodes upstream of, and including, grouppes

			// Add this group to any nodes that aren't currently in a group


			for (int i=0; i<all_new_nodes.Count; i++)
			{				
				//Debug.Log("A D D ::: " + all_new_nodes[i].Name + ". " +all_new_nodes[i].grouperKey + " ---- " + Guid);
				if (String.IsNullOrEmpty(all_new_nodes[i].grouperKey) || (grouper != null && all_new_nodes[i].grouperKey == grouper.Guid) )
				{

					if (model.currentWorkingGroupPO == null || model.currentWorkingGroupPO != all_new_nodes[i])
					{
						all_new_nodes[i].grouperKey = Guid;
						all_new_nodes[i].grouper = this;
						Groupees.Add(all_new_nodes[i]);
					}
				}
			}





			// DEPENDS_ON PROXIES: CREATE PROXIES TO DEPENDS_ON OUTSIDE THIS GROUP

			// Now check each DependsOn to see if it is out of the group's set.
			// That includes thes groupees and any existing nodes that may be in the group before this ad.
			// make a list of all inputs and Dictionary map their DependsOn's. 


			List<AXParameter> dependsToRewire = new List<AXParameter>();
			//List<AXParameter> newGroupees  = new List<AXParameter>();


			for (int i=0; i<all_new_nodes.Count; i++)
			{
				//Debug.Log("* " + all_new_nodes[i].Name);


				// NEW_GROUPEE OUTPUTS
				List<AXParameter> paramsOut = all_new_nodes[i].getAllOuputs();




				for(int j=0; j<paramsOut.Count; j++)
				{
					//Debug.Log(" --- " + paramsOut[j].Name + " " + (( paramsOut[j].DependsOn != null) ? paramsOut[j].DependsOn.Name : "NULL"));

					AXParameter groupee_output_p =  paramsOut[j];

					List<AXParameter> existingGroupeesToRewire = new List<AXParameter>();
					List<AXParameter> newGroupeeDependentsToMakeIndependent = new List<AXParameter>();

					if (groupee_output_p.Dependents != null && groupee_output_p.Dependents.Count>0)
					{
						for (int k = 0; k < groupee_output_p.Dependents.Count; k++) {

							AXParameter dependent = groupee_output_p.Dependents [k];

							// IS THIS DEPENDENT A PROXY?
							if (dependent.parametricObject == this) 
							{
								newGroupeeDependentsToMakeIndependent.Add(dependent);

								// connect any of the dependents dependents directly to this groupee output
								if (dependent.Dependents != null)
								{
									for(int m=0; m<dependent.Dependents.Count; m++)
										existingGroupeesToRewire.Add(dependent.Dependents[m]);
								}
							}

						}
					}

				
					foreach(AXParameter d in existingGroupeesToRewire)
						d.makeDependentOn(groupee_output_p);

					foreach(AXParameter d in newGroupeeDependentsToMakeIndependent)
					{
						d.makeIndependent();
						removeParameter(d);
					}



					

				}




				// NEW_GROUPEE INPUTS
				List<AXParameter> paramers = all_new_nodes[i].getAllInputs();

				for(int j=0; j<paramers.Count; j++)
				{
					//Debug.Log(" --- " + paramers[j].Name + " " + (( paramers[j].DependsOn != null) ? paramers[j].DependsOn.Name : "NULL"));

					if (paramers[j].DependsOn != null && ! all_new_nodes.Contains(paramers[j].DependsOn.parametricObject) && ! dependsToRewire.Contains(paramers[j].DependsOn))
						dependsToRewire.Add(paramers[j].DependsOn);
				}




			}

			//Debug.Log("dependsToRewire.Count="+dependsToRewire.Count);

			foreach (AXParameter p in dependsToRewire)
			{
				//Debug.Log(" +++ " + p.parametricObject.Name+" . " +p.Name);

				AXParameter newP = addParameter(new AXParameter(p.Type, AXParameter.ParameterType.Input, p.Dependents[0].Name));

				List<AXParameter> toProcess =  new List<AXParameter>(p.Dependents);

				//Debug.Log("Depens = " + toProcess.Count);
				for (int i = 0; i < toProcess.Count; i++) {
					AXParameter d = toProcess [i];
					//Debug.Log (" +++ +++ TRY Remap ["+i+"] ->" + d.parametricObject.Name + " : " +  d.Name);
					if (new_groupees.Contains (d.parametricObject)) {
						//Debug.Log (" +++ +++ Remap->" + d.parametricObject.Name + " : " +  d.Name);
						d.makeDependentOn (newP);
					}
				}   

				newP.makeDependentOn(p);

			}	
			/*
				if (poo.is3D())
				{
					AXParameter out_p = poo.generator.P_Output;// getPreferredOutputMeshParameter();
					if (out_p != null)
						po.addInputMesh().makeDependentOn(out_p);
				}
				*/



			
			// DEPENDENTS OF NEW GROUPEE TO REWIRE 



			// RELATIONS PROXIES

			//Debug.Log("RELATIONS PROXIES::::::::::::::::::::::::::::::::::::::::::::::::::::::");
			List<AXParameter> controlParamsToRewire = new List<AXParameter>();
			List<AXRelation> relationsToReplace = new List<AXRelation>();

			for (int i=0; i<all_new_nodes.Count; i++)
			{
				//Debug.Log(" +++ " +all_new_nodes[i].Name);

				if (all_new_nodes[i].geometryControls == null)
					continue;

				List<AXNode> controlParams = all_new_nodes[i].geometryControls.children;

				if (all_new_nodes[i].positionControls != null)
					controlParams.AddRange(all_new_nodes[i].positionControls.children);


				for(int j=0; j<controlParams.Count; j++)
				{
					AXParameter controlParam = (AXParameter) controlParams[j];

					foreach(AXRelation relation in controlParam.relations)
					{
						AXParameter other_p = ( relation.pA == controlParam) ? relation.pB : relation.pA;

 
						if (other_p != null && ! all_new_nodes.Contains(other_p.parametricObject) && ! controlParamsToRewire.Contains(other_p))
						{

							// Make sure this is not a relation going to its own Groupee
							if ( other_p.parametricObject != this  &&  (other_p.parametricObject.grouper == null || other_p.parametricObject.grouper != controlParam.parametricObject))
							{
								controlParamsToRewire.Add(other_p);
								relationsToReplace.Add(relation);
							}
						}
					}
				}
			}

			//Debug.Log("controlParamsToRewire.Count="+controlParamsToRewire.Count);


			List<AXRelation> subRelationsToReplace = new List<AXRelation>();

			for (int ii=0; ii<controlParamsToRewire.Count; ii++)
			{ 
				AXParameter other_p = controlParamsToRewire[ii];

				Debug.Log(other_p.Name);

				// is the name of rel_p one of the reserved names SizeX, SizeY or SizeZ?
				// If so, then just link to it.
				AXParameter proxy_p = null;

				//Debug.Log("rel_p.Name="+rel_p.Name);
				if (other_p.Name == "SizeX" || other_p.Name == "SizeY" || other_p.Name == "SizeZ")
					proxy_p = getParameter(other_p.Name);

				
				if (proxy_p == null) // Add a control parameter to this PO
				 	proxy_p = addParameter(new AXParameter(other_p.Type, other_p.PType, other_p.Name));

				proxy_p.FloatVal 	= other_p.FloatVal;
				proxy_p.IntVal		= other_p.IntVal;
				proxy_p.boolval		= other_p.boolval;



				List<AXParameter> toProcess =  new List<AXParameter>();
				foreach(AXRelation relation in other_p.relations) 
				{

					AXParameter partner_p = (relation.pA == other_p) ? relation.pB : relation.pA;

					if (partner_p == null)
						continue;

					// This "other_P" may be connected to nodes outside the selected new groupees
					if (all_new_nodes.Contains(partner_p.parametricObject))
					{
						toProcess.Add(partner_p);
						subRelationsToReplace.Add(relation);
					}




					//Debug.Log("to del: " + relation.toString() );
				}


				//Debug.Log("Depens = " + toProcess.Count);
				for (int i = 0; i < toProcess.Count; i++) {

					AXParameter del_p = toProcess [i];

					//Debug.Log (" +++ +++ TRY Remap ["+i+"] ->" + d.parametricObject.Name + " : " +  d.Name);
					if (new_groupees.Contains (del_p.parametricObject)) {
						//Debug.Log (" +++ +++ Remap->" + del_p.parametricObject.Name + " : " +  del_p.Name);

						AXRelation 	relToReplace = subRelationsToReplace[i];

						AXParameter partner_p = (relToReplace.pA == del_p) ? relToReplace.pB : relToReplace.pA;

						AXRelation newrel = model.relate(proxy_p, del_p);
						newrel.expression_AB = relToReplace.expression_AB.Replace(ArchimatixUtils.guidToKey(partner_p.Guid), ArchimatixUtils.guidToKey(proxy_p.Guid));
						newrel.expression_BA = relToReplace.expression_BA.Replace(ArchimatixUtils.guidToKey(partner_p.Guid), ArchimatixUtils.guidToKey(proxy_p.Guid));
					}
				}   


				model.relate(proxy_p, other_p);


			}

			//Debug.Log(" releationsToDelete="+relationsToDelete.Count);
			foreach(AXRelation relation in subRelationsToReplace)
				model.unrelate(relation);


			model.cleanGraph();




			//Debug.Log(Name + " DONE: " + Groupees.Count);






			model.autobuild();
		}




		// Recursive
		public void addGrouperUpstream(AXParametricObject po, AXParametricObject gpr)
		{

			if (po.grouperKey != gpr.Guid)
			{
				po.grouperKey = gpr.Guid;
				po.grouper = gpr;
				groupees.Add(po);
			}
			if (po.generator != null && po.generator.AllInput_Ps != null)
			{
				foreach(AXParameter input in  po.generator.AllInput_Ps)
				{
					if (input != null && input.DependsOn != null && String.IsNullOrEmpty(input.DependsOn.parametricObject.grouperKey))
					{
						if (input.DependsOn.Dependents.Count == 1 )
						{
							addGrouperUpstream(input.DependsOn.parametricObject, gpr);
						}
						else
						{
							//Make thu-parameter here
							AXParameter d = input.DependsOn;
							AXParameter newP = gpr.addParameter(new AXParameter(AXParameter.DataType.Spline, AXParameter.ParameterType.Input, input.Name));

							input.makeIndependent();
							input.makeDependentOn(newP);
							newP.makeDependentOn(d);



						}

					}

				}
			}

		}



		[System.NonSerialized]
		bool groupeesVisible;

		public void toggleGroupeesDisplay()
		{
			groupeesVisible = ! groupeesVisible;

			foreach(AXParametricObject po in model.parametricObjects)
			{
				if (po.grouper != null)
					po.isOpen = false;
				else {
					po.isOpen = ! groupeesVisible;
				}
			}

			if (groupees != null)
				foreach(AXParametricObject groupee in Groupees)
					groupee.isOpen = groupeesVisible;
			isOpen = true;
		}


		public bool isCurrentGrouper()
		{

			if (generator is Grouper && this == model.currentWorkingGroupPO)
				return true;

			return false;

		}
		#endregion









		[SerializeField]
		private Bounds m_bounds; // can be the bounding box of a series of meshes
		public  Bounds 	bounds
		{
			get { return m_bounds; }
			set { m_bounds = value; }
		}


		public Mesh boundsMesh;


		[SerializeField]
		private Vector3 m_margin; // can be the bounding box of a series of meshes
		public  Vector3   margin
		{
			get { return m_margin; }
			set { m_margin = value; }
		}


		public Matrix4x4 localMatrix;
		public Matrix4x4 worldMatrix;


		// SUB PO's like input splines groups
		public List<AXShape> shapes;




		public AXStaticEditorFlags axStaticEditorFlags;

		// TAG
		public string tag = "Untagged";

		// LAYER
		public int layer = 0;









		public Terrain terrain;


		public float[,] heightsOrig;



		#region Output Members	
		// DO NOT SERIALIZE THIS!
		[System.NonSerialized]
		private AXParameter m_output;
		public  AXParameter Output
		{
			get { return m_output; }
			set { m_output = value; }
		}

		public bool hasInputsConnected = false;

		public bool combineMeshes;
		public ColliderType colliderType = ColliderType.Mesh;


		// RIGIDBODY
		// The mass of the rigidody is controlled by the density of the material for this PO.

		public bool isRigidbody;
		public bool isKinematic;

		public float drag;
		public float angularDrag;




		// COLLIDER
		//public int Collider

		[System.NonSerialized]
		private int m_stats_VertCount = 0;
		public  int stats_VertCount 
		{
			get { return m_stats_VertCount; }
			set { m_stats_VertCount = value; } 
		}

		[System.NonSerialized]
		private int m_stats_TriangleCount = 0;
		public  int stats_TriangleCount 
		{
			get { return m_stats_TriangleCount; }
			set { m_stats_TriangleCount = value; }
		}

		#endregion

		// use this for a number of things like initializing Repeater Bays on connection with mesh
		public bool isInitialized;

		public Vector2 codeScrollPosition = Vector2.zero;



		// MATERIAL AND TEXTURE
		// When this ParametricObject node generates meshes,
		// it attaches a material, physicmaterial to its game objects based on the axMat object 
		// and it scales and shifts for UV's during generation based on the axTex object.

		// The material and texture members need not be
		// serialized since they are only live references.
		// The originals are saved in MaterialTool nodes or, 
		// for the defaults, in the model. For that reason, they are all serialized
		// so that the MAterialTool nodes have them. In other pos,
		// these are overwritten with live references in the model.cleanGraph()

		//A


		public AXMaterial 	axMat;
		public AXTexCoords 	axTex;
	
		

	  	// support for legacy material
		[SerializeField]
		private Material _mat = null;
		public  Material  Mat
		{ 
			get  { return _mat; }
			set { _mat = value; }
		}







		[SerializeField]
		public GameObject prefab;


		[SerializeField]
		public string basedOnAssetWithGUID;

		[NonSerialized]
		public string readIntoLibraryFromRelativeAXOBJPath;


		[SerializeField]
		public string author;


		[SerializeField]
		public string tags;


		[SerializeField]
		public Matrix4x4 transMatrix = Matrix4x4.identity;


		// DISPLAY MATRICES DEPENDING ON GRAPH STATE AND SELECTION

		[SerializeField]
		public Matrix4x4 			consumerMatrix = Matrix4x4.identity;

		[NonSerialized]
		private AXParametricObject 	_selectedConsumer;
		public AXParametricObject 	selectedConsumer
		{
			get { 
				return _selectedConsumer; 
			}
			set { 
				//Debug.Log(" * * * * * * * * * "+Name + " ... set selectedConsumer="+value);

				_selectedConsumer = value; 
			}

		}

		// this is nice, if you have it, but not essential for locating the world matrix of this PO's handles
		[NonSerialized]
		public string  				selectedConsumerAddress;

		[NonSerialized]
		public AXGameObject 		selectedAXGO;



		[NonSerialized]
		public MaterialTool materialTool;





		[SerializeField]
		public string code;

		[System.NonSerialized]
		public  string codeWarning = ""; 


		[System.NonSerialized]
		public List<AXMesh> temporaryOutputMeshes;

		#region UI Layout

		//public Rect rect = new Rect(100,100,100,100);
		public Rect startRect = new Rect(100,100,100,100);

		[System.NonSerialized]
		public bool revealControls = false;

		public int codeWindowHeight = 300;
		public Rect codeWindowRectLocal;


		public float minHeight = 0;
		public float outputEndHeight = 0;

		[System.NonSerialized]
		public int guiWindowId;

		// Use this to create a po and whenver its palette is first rendered, focus that GUI.Window
		[System.NonSerialized]
		public bool focusMe = false; 


		public bool inputHasBeenAttached = false;


		public float sortval = 10000;

		public DateTime createdate;
		public DateTime modifydate;

		#endregion


		// META DATA FOR SAVING AND SEACHING IN LIBRARY OF PARAMETRIC OBJECTS
		[SerializeField]
		public List<string> categories;

		[SerializeField]
		public List<string> genres;

		public bool useMeshInputs = false;
		public List<AXParameter> meshInputs;





		// BASE CONTROLS
		[System.NonSerialized]
		public AXNode baseControls;
		public void resetBaseControls()
		{
			baseControls = null;
			assertBaseControls();
		}

		public void assertBaseControls()
		{
			if (baseControls == null) 
			{
				baseControls = new AXNode ("baseControls");
				baseControls.isOpen = true;
				baseControls.ParentNode = this;
				baseControls.parametricObject = this;
			}
		}




		// INPUT CONTROLS
		[System.NonSerialized]
		public AXNode inputControls;
		public void resetInputControls()
		{
			inputControls = null;
			assertInputControls();
		}

		public void assertInputControls()
		{
			if (inputControls == null) 
			{
				inputControls = new AXNode ("inputControls");
				inputControls.ParentNode = this;
				inputControls.parametricObject = this;
				inputControls.isOpen = true;
			}
		}


		// POSITION CONTROLS
		[System.NonSerialized]
		public AXNode positionControls;
		public void resetPositionControls()
		{
			positionControls = null;
			assertPositionControls();
		}

		public void assertPositionControls()
		{
			if (positionControls == null) 
			{
				positionControls = new AXNode ("positionControls");
				positionControls.ParentNode = this;
				positionControls.parametricObject = this;
			}
		}



		[System.NonSerialized]
		private AXNode _textureControls;

		public AXNode textureControls 
		{
			get { return _textureControls; }
			set  {  _textureControls = value; } 
		}


		// GEOMETRY CONTROLS
		[System.NonSerialized]
		public AXNode geometryControls;

		public void resetGeometryControls()
		{
			geometryControls = null;
			assertGeometryControls();
		}
		public void assertGeometryControls()
		{
			if (geometryControls == null) 
			{
				geometryControls = new AXNode ("positionControls");
				geometryControls.ParentNode = this;
				geometryControls.parametricObject = this;
			}
		}



		// OUTPUTS
		[System.NonSerialized]
		public AXNode outputsNode;

		public void resetOutputsNode()
		{
			outputsNode = null;
			assertOutputsNode();
		}
		public void assertOutputsNode()
		{
			if (outputsNode == null) 
			{
				outputsNode = new AXNode ("outputsNode");
				outputsNode.isOpen = true;
				outputsNode.ParentNode = this;
				outputsNode.parametricObject = this;
			}
		}






		[System.NonSerialized]
		public Dictionary<string, float> vars = new Dictionary<string, float>();


		// TO CAPTURE SYMBOLS IN A MATHPARSE
		[System.NonSerialized]
		public Dictionary<AX.SoftCircuits.Eval, List<string>> symbolsUsed = new Dictionary<AX.SoftCircuits.Eval, List<string>>();



		public bool isClosed = true;



		// ----------------- PARAMETERS --------------------
		// One of the main responsibilities of the pParametricObject is to manage a set of parameters
		// This list is serialized. 
		public List<AXParameter> 	parameters 			= new List<AXParameter>();


		// ----------------- HANDLES --------------------
		[SerializeField]
		public  List<AXHandle> m_handles; 
		public  List<AXHandle>   handles 
		{
			get { 
				if (m_handles == null)
					m_handles = new List<AXHandle>();
				return m_handles; 
			}
			set {  m_handles = value; }
		} 


		public bool includeInSidebarMenu = true;

		[SerializeField]
		public bool showSubpartHandles = true;

		// Convenience list for paramter display order


		// VARIABLE

		public bool m_showControls;
		public  bool 	showControls
		{
			get { return m_showControls; }
			set {  m_showControls = value; }
		}



		public bool m_showHandles;
		public  bool 	showHandles
		{
			get { return m_showHandles; }
			set {  m_showHandles = value; }
		}


		public bool m_showLogic;
		public  bool 	showLogic
		{
			get { return m_showLogic; }
			set {  m_showLogic = value; }
		}

		public bool m_showPhysics;
		public  bool 	showPhysics
		{
			get { return m_showPhysics; }
			set {  m_showPhysics = value; }
		}


		public  bool 	showShapes;


		public bool hasCustomLogic = false;
		public bool hasDerivedValues = false;


		[System.NonSerialized]
		private bool m_isEditing;
		public  bool 	isEditing
		{
			get { return m_isEditing; }
			set {  m_isEditing = value; }
		}


		[System.NonSerialized]
		private bool m_codeIsDirty;
		public  bool 	codeIsDirty
		{
			get { return m_codeIsDirty; }
			set { m_codeIsDirty = value; }
		}



		// These are live values only - for convenience
		// * don't serialize these, it will only make copies on deserialize

		/*
		[System.NonSerialized]
		private AXModel		_model			= null;
		public 	AXModel		model   // the Name property
		{
			get { return _model; }
			set { _model = value; }
		}
		*/


		[System.NonSerialized]
		private string		genID			= null;
		public 	string		GenID   // the Name property
		{
			get { return genID; }
			set { genID = value; }
		}
		public int iterationsForGenID = 0;

		public string lastGeneratedGenId = "";



		// CACHE OF PARAMETER VALUES
		[System.NonSerialized]
		private List<AXParameter> parameterCache;

		// CACHE OF Bounds 
		[System.NonSerialized]
		private Bounds boundsCache;



		// STOWING SYSTEM

		[System.NonSerialized]
		public List<AXParametricObject> subnodes;


		public string stowTraversal_GUID;
		public string hereditaryTransformGUID;
		public string consumersTransformGUID;
		// Are the inputs of this PO stowed?
		public bool inputsStowed;



		// SELECTED ITM IDENTIFIER
		// This would be an instance created by this PO, say a Repeater. 
		//It lets the PO know which of the algorithmically defined slots is of current interst.
		// For eaxmple, selecting a column in a grid iterator which is at i,j,k.
		[System.NonSerialized]
		public string selectedSubItemAddress;


		// THUMBNAIL SUPPORT

		public ThumbnailState thumbnailState;

		// This PO can maintain a Camera GameObject in the scene under the Parent AXModel.gameObject.
		public AXCamera cameraSettings;


		public RenderTexture renTex;

		public Color thumbnailLineColor;

		// DO I NEED TO SAVE A TEXTURE2D?
		[SerializeField]
		public Texture2D thumbnail;






		// Documentation
		public string documentationURL = "";






		// CONSTRUCTORS //
		public AXParametricObject(string t, string name) :base(name)
		{
			m_type	= t;

			if (parameters.Count == 0)
				init ();

			localMatrix = Matrix4x4.identity;

			axStaticEditorFlags = axStaticEditorFlags | (AXStaticEditorFlags.LightmapStatic);
		}


		public void init()
		{

			cameraSettings = new AXCamera();
			cameraSettings.setPosition();

			instantiateGenerator();


			isOpen = true;

			thumbnailLineColor = Color.magenta;


		}

		public bool allInputsAreStowed()
		{

			bool ret = true;

			if (generator.AllInput_Ps != null)
			{
				for (int i=0; i<generator.AllInput_Ps.Count; i++)
				{
					if (generator.AllInput_Ps[i].DependsOn != null && generator.AllInput_Ps[i].DependsOn.parametricObject.isOpen)
					{
						ret = false;
						break;
					}

				}
			}
			return ret;

		}

		// RECURSIVE
		// This is similar to a sort of "SetDirty" but for generation purposes.
		// 
		public void setAltered()
		{
			//Debug.Log (Name + " setAltered  setAltered generator.AllOutput_Ps="+generator.AllOutput_Ps.Count);


			if (model != null) {
				if (!isAltered)
				{
					model.setAltered (this);
					isAltered = true;
				}


				// Output-Dependents

				for (int i=0; i<generator.AllOutput_Ps.Count; i++)
				{
					//Debug.Log("--- " + generator.AllOutput_Ps[i].parametricObject.Name + " : " + generator.AllOutput_Ps[i].parametricObject.isAltered);

					// each dependent
					AXParameter out_P = generator.AllOutput_Ps[i];

					if (out_P.Dependents != null && out_P.Dependents.Count > 0)
					{
						for(int j=0; j<out_P.Dependents.Count; j++)
						{
							AXParameter d = out_P.Dependents[j];
							//Debug.Log(" ------> " + d.parametricObject.Name +  d.parametricObject.isAltered);
							if (! d.parametricObject.isAltered)
							{
								d.parametricObject.setAltered();

								if (d.Dependents != null && d.Dependents.Count > 0)
								{
									for(int k=0; k<d.Dependents.Count; k++)
									{
										AXParameter dd=d.Dependents[k];
										if (! dd.parametricObject.isAltered)
											dd.parametricObject.setAltered();
									}
								}
							}
						}
					}
				}




			}

		}








		public void remapGuids(ref Dictionary<string, string> guidMap)
		{
			foreach (AXParameter p in parameters)
			{
				//Debug.Log ("here remapGuids");
				if (! String.IsNullOrEmpty(p.dependsOnKey))
					p.dependsOnKey = AXUtilities.swapOutGuids(p.dependsOnKey, ref guidMap);
			}

			if (shapes != null)
			{
				foreach(AXShape shape in shapes)
				{
					foreach(AXParameter p in shape.inputs)
					{
						if (! String.IsNullOrEmpty(p.dependsOnKey))
							p.dependsOnKey = AXUtilities.swapOutGuids(p.dependsOnKey, ref guidMap);
					}

				}
			}
		}


		public void onDeserialize()
		{
			//Debug.Log ("PO:"+Name+" :: onDeserialize" );
			// create a generator for this PO
			instantiateGenerator();

			// Link handels and this PO
			foreach(AXHandle han in handles)
				han.parametricObject = this;

			gatherSubnodes();
		}






	// RECRUSIVE SET PO FLAGS
	public  void setUpstreamersToYourFlags()
	{
		//po.axStaticEditorFlags = po.axStaticEditorFlags | flag;


			foreach(AXParameter input in getAllInputMeshParameters())
		{
			if (input.DependsOn != null && input.DependsOn.parametricObject.is3D())
			{
				input.DependsOn.parametricObject.axStaticEditorFlags = axStaticEditorFlags;
				input.DependsOn.parametricObject.setUpstreamersToYourFlags();

			}

		}
		if (generator is Grouper && groupees != null && groupees.Count > 0)
		{
			foreach(AXParametricObject groupee in groupees)
			{
					if (groupee != null && groupee.is3D())
				{
						groupee.axStaticEditorFlags = axStaticEditorFlags;
						groupee.setUpstreamersToYourFlags();

				}

			}
		}

	}


		#region THUMBNAIL CAMERA




		public void initCamera()
		{

			Debug.Log ("initing camera >>>>>>>>>>>>>>>>>>>>>>>>>> 1"); 

			renTex = new RenderTexture(256, 256, 24);
			renTex.Create();
			renTex.antiAliasing = 8;




		}
		public void resetRenTex()
		{
			Debug.Log ("po.resetRenTex()");
			renTex = new RenderTexture(256, 256, 24);
			renTex.Create();
			renTex.antiAliasing = 8;
		}


		public Vector3 getThumbnailCameraPosition()
		{


			if (cameraSettings == null)
				cameraSettings = new AXCamera();


			if (generator is MaterialTool)
				cameraSettings.radius = 7;
			else  
			{
				Bounds b = getBoundsAdjustedForAxis();
				cameraSettings.radius = b.size.magnitude;
			}
			//Debug.Log();
			cameraSettings.setPosition();

			return cameraSettings.localPosition + getThumbnailCameraTarget();

		}

		public Vector3 getThumbnailPosition()
		{
			return new Vector3(10000, 10000, 10000) + getLocalCenter();
		}



		public Vector3 getThumbnailCameraTarget()
		{
			Bounds b = bounds;
			return new Vector3(10000, 10000, 10000) + getLocalMatrix().MultiplyPoint(b.center);
		}


		#endregion



		public void copyTransformTo(AXParametricObject po)
		{
			po.intValue("Align_X", intValue("Align_X"));
			po.intValue("Align_Y", intValue("Align_Y"));
			po.intValue("Align_Z", intValue("Align_Z"));

			po.setParameterValueByName("Trans_X", floatValue("Trans_X"));
			po.setParameterValueByName("Trans_Y", floatValue("Trans_Y"));
			po.setParameterValueByName("Trans_Z", floatValue("Trans_Z"));

			po.setParameterValueByName("Rot_X", floatValue("Rot_X"));
			po.setParameterValueByName("Rot_Y", floatValue("Rot_Y"));
			po.setParameterValueByName("Rot_Z", floatValue("Rot_Z"));

			po.intValue("Axis", intValue("Axis")); 

		}



		// SHAPES
		public void addShape(string n)
		{
			if (shapes == null)
				shapes = new List<AXShape>();

			AXShape shape = new AXShape(this, n);

			shapes.Add(shape);
		}




		#region Parameters

		// ADDING PARAMETERS //

		public AXParameter addParameter() {
			AXParameter p = new AXParameter("param", 10.0f, 1f, 100.0f);
			//p.FloatVal = 1;
			addParameter(p);
			return p;
		}
		public AXParameter addParameter(string name) {
			AXParameter p = new AXParameter(name);
			addParameter(p);
			return p;
		}
		public AXParameter addParameter(AXParameter.DataType type, string name, float defval) {
			AXParameter p = new AXParameter(type, name);
			p.val = defval;
			if (p.min > p.val) p.min = p.val;
			addParameter(p);

			return p;
		}
		public AXParameter addParameter(AXParameter.DataType type, string name) {
			AXParameter p = new AXParameter(type, name);
			addParameter(p);
			return p;
		}
		public AXParameter addParameter(AXParameter.DataType type, string name, float defval, float mn, float mx) {
			AXParameter p = new AXParameter(type, name);
			//Debug.Log(name + ": " + mn);

			p.val = defval;
			p.min = mn;
			p.max = mx;
			addParameter(p);
			return p;
		}

		public AXParameter addParameter(AXParameter.DataType type, AXParameter.ParameterType p_type, string name, float defval, float mn, float mx) {
			AXParameter p = new AXParameter(type, p_type, name);

			p.val = defval;
			p.min = mn;
			p.max = mx;
			addParameter(p);
			return p;
		}

		// INT
		public AXParameter addParameter(AXParameter.DataType type, string name, int defval) {
			AXParameter p = new AXParameter(type, name);
			p.intval = defval;
			addParameter(p);
			return p;
		}

		public AXParameter addParameter(AXParameter.DataType type, AXParameter.ParameterType p_type, string name, int defval) {
			AXParameter p = new AXParameter(type, p_type, name);
			p.intval = defval;
			addParameter(p);
			return p;
		}

		public AXParameter addParameter(AXParameter.DataType type, string name, int defval, int mn, int mx) {
			AXParameter p = new AXParameter(type, name);
			p.intval = defval;
			p.intmin = mn;
			p.intmax = mx;
			addParameter(p);
			return p;
		}
		public AXParameter addParameter(AXParameter.DataType type, string name, bool b) {
			AXParameter p = new AXParameter(type, name);
			p.boolval = b;
			addParameter(p);
			return p;
		}
		public AXParameter addParameter(AXParameter.DataType type, AXParameter.ParameterType p_type, string name, bool b) {
			AXParameter p = new AXParameter(type, p_type, name);
			p.boolval = b;
			addParameter(p);
			return p;
		}
		public AXParameter addParameter(AXParameter p) {
			p.Parent 			= this;
			p.parametricObject 	= this;
			p.ParentNode		= this;
			p.isOpen			= false;
			p.isEditing 		= false;
			switch(p.PType)
			{
			case   AXParameter.ParameterType.Base:
				p.hasOutputSocket = true;
				assertBaseControls();
				baseControls.addChild(p);
				break;

			case   AXParameter.ParameterType.Input:
				p.hasOutputSocket = true;
				assertInputControls();
				inputControls.addChild(p);
				break;

			case   AXParameter.ParameterType.PositionControl:
				assertPositionControls();
				positionControls.addChild(p);
				break; 

			case   AXParameter.ParameterType.GeometryControl:
				assertGeometryControls();
				geometryControls.addChild(p);
				break;

			case   AXParameter.ParameterType.Output:
				p.hasInputSocket = false;
				break;
			default:

				break;
			}


			parameters.Add( p );


			//Parent.indexedParameters.Add(p.Guid, p);

			return p;
		}


		public AXParameter addSplineParameter(string name)
		{
			AXParameter p = addParameter(new AXParameter(AXParameter.DataType.Spline, AXParameter.ParameterType.Input, name));


			p.hasOutputSocket = true;

			return p;
		}


		public void removeParameterWithName(string _name)
		{
			removeParameter(getParameter (_name));
		}
		public void removeParameter(AXParameter p)
		{
			if (p != null)
			{
				for(int i=0; i<p.relations.Count; i++)
					p.Parent.model.unrelate(p.relations[i]);

				parameters.Remove(p);
				model.cleanGraph();
			}
		}


		public void removeParameterFromReplicantsControls(string _name)
		{
			AXParameter output_p = generator.P_Output;
			if (output_p == null)
				return;
			foreach (AXParameter consumer_p in output_p.Dependents)
			{
				if (consumer_p.Parent.generator is Replicant)
					consumer_p.Parent.removeParameterWithName(_name);
			}

		}
		public void removeHandleFromReplicantsControlsWithName(string _name)
		{
			AXParameter output_p = generator.P_Output;
			if (output_p == null)
				return;

			// immediate consumers that are replicants can be synced with this PO
			foreach (AXParameter consumer_p in output_p.Dependents)
			{
				if (consumer_p.Parent.generator is Replicant)
					consumer_p.Parent.removeHandleWithName(_name);
			}

		}

		public void removeInputDependencies()
		{
			//Debug.Log("*****  removeDependencies "+ Name);
			List<AXParameter> pList = getAllInputParameters();

			if (pList != null)
			{
				foreach(AXParameter input in pList)
				{
					AXParametricObject dependsOn_src = null;

					if (input.DependsOn != null)
					{
						dependsOn_src = input.DependsOn.parametricObject;

						input.DependsOn.removeDependent(input);

						if (dependsOn_src.selectedConsumer == input.parametricObject)
							dependsOn_src.selectedConsumer = null;

					}
					input.freeDependents();

					if (dependsOn_src != null)
						dependsOn_src.generator.adjustWorldMatrices();

				}
			}


		}
		public void removeOutputDependencies()
		{
			//Debug.Log("*****  removeOutputDependencies "+ Name);
			// FREE OUTPUTS
			List<AXParameter> outputs = getAllOuputs();

			foreach(AXParameter out_p in outputs)
				out_p.freeDependents();

			selectedConsumer = null;
			//generator.adjustWorldMatrices();

		} 
		public void deleteRelations() 
		{
			for (int i=0; i<parameters.Count; i++)
				for (int j=0; j<parameters[i].relations.Count; j++)
					model.unrelate(parameters[i].relations[j]);
		}



		public AXParameter getParameterByBinding(Axis axis) 
		{
			AXParameter p = parameters.Find(x => x.sizeBindingAxis.Equals(axis));

			return p;
		}
		public void setParameterByBinding(int axis, float val) 
		{
			AXParameter p = parameters.Find(x => x.sizeBindingAxis.Equals(axis));
			if (p != null)
				p.FloatVal = val;
		}
		public void propagateParameterByBinding(int axis, float val) 
		{
			AXParameter p = parameters.Find(x => x.sizeBindingAxis.Equals(axis));
			if (p != null)
			{
				p.initiateRipple_setFloatValueFromGUIChange(val);

			}
		}



		// GET PARAMETER
		public AXParameter getParameter(params string[] names) 
		{
			// used when multiple two keys may be used. Preference given to the first.
			// This is primarily for when a key name has been changed. For example, in the Extrude PO, height was changed to depth.
			AXParameter p = null;
			for (int i = 0; i < names.Length; i++)
			{
				p = parameters.Find(x => x.Name.Equals(names[i]));
				if (p != null)
					return p;
			}
			return null;
		}


		public AXParameter getControlParameter(string name)
		{
			if (inputControls != null && inputControls.children != null && inputControls.children.Count > 0)
			{
				for (int i=0; i<inputControls.children.Count; i++)
				{
					if (inputControls.children[i] != null && inputControls.children[i].Name != null && inputControls.children[i].Name.Equals(name))
						return (AXParameter) inputControls.children[i]; 
				}

			}

			return getParameter(name);

		}



		// GET FLOAT
		public float floatValue(params string[] names) 
		{
			// Preference given to the first. This is primarily for when a key name has been changed.
			AXParameter p = null;
			for (int i = 0; i < names.Length; i++)
			{
				p = parameters.Find(x => x.Name.Equals(names[i]));
				if (p != null)
					return p.FloatVal;
			}
			return 0;
		}

		// SET FLOAT
		public void floatValue(string n, float v) 
		{
			AXParameter p = parameters.Find(x => x.Name.Equals(n));
			if (p != null)
				p.FloatVal = v;
		}



		public void setParameterValueByName(string n, float v) 
		{
			AXParameter p = parameters.Find(x => x.Name.Equals(n));
			if (p == null)
				return;

			// v. 1.0.6
			//p.FloatVal = v;
			p.initiateRipple_setFloatValueFromGUIChange(v);
		}

		// RECURSION
		public void initiateRipple_setFloatValueFromGUIChange(string n, float v) 
		{
			AXParameter p = parameters.Find(x => x.Name.Equals(n));

			if (p == null)
				return;

			model.latestEditedParameter = p;

			p.initiateRipple_setFloatValueFromGUIChange(v);
			generator.parameterWasModified(p);
		}
		public void initiateRipple_setFloatValueFromGUIChange(string n1, string n2, float v) 
		{
			AXParameter p = parameters.Find(x => x.Name.Equals(n1));


			if (p == null)
				p = parameters.Find(x => x.Name.Equals(n2));

			if (p == null)
				return;

			model.latestEditedParameter = p;

			p.initiateRipple_setFloatValueFromGUIChange(v);
			generator.parameterWasModified(p);
		}

		/*
		public void setFloatValueUpwardsByName(string n, string guid, float v, bool upstreamOnly = false) 
		{
			AXParameter p = parameters.Find(x => x.Name.Equals(n));
			if (p == null)
				return;
			p.setFloatValueUpwards(guid, v, upstreamOnly);
		}
		*/






		// INT

		// GET INT
		public int intValue(params string[] names) 
		{
			// Preference given to the first. This is primarily for when a key name has been changed.
			AXParameter p = null;
			for (int i = 0; i < names.Length; i++)
			{
				p = parameters.Find(x => x.Name.Equals(names[i]));
				if (p != null)
					return p.IntVal;
			}
			return 0;
		}

		// SET INT
		public void intValue(string n, int v) 
		{
			AXParameter p = parameters.Find(x => x.Name.Equals(n));
			if (p != null)
				p.IntVal = v;
		}



		// RECURSION
		public void initiateRipple_setIntValueFromGUIChange(string n, int v) 
		{
			AXParameter p = parameters.Find(x => x.Name.Equals(n));


			if (p == null)
				return;

			model.latestEditedParameter = p;

			p.initiateRipple_setIntValueFromGUIChange(v);
		}


		/*
		 public void setIntValueUpwardsByName(string n, string guid, int v, bool upstreamOnly = false) 
		{
			AXParameter p = parameters.Find(x => x.Name.Equals(n));
			if (p == null)
				return;
			p.setIntValueUpwards(guid, v, upstreamOnly);
		}
		*/



		//BOOL


		// GET BOOL
		public bool boolValue(params string[] names) 
		{
			// Preference given to the first. This is primarily for when a key name has been changed.
			AXParameter p = null;
			for (int i = 0; i < names.Length; i++)
			{
				p = parameters.Find(x => x.Name.Equals(names[i]));
				if (p != null)
					return p.boolval;
			}
			return false;
		}

		public void boolValue(string name, bool b) 
		{
			// Preference given to the first. This is primarily for when a key name has been changed.
			AXParameter p = parameters.Find(x => x.Name.Equals(name));

			//Debug.Log("********* >>>>>>>>>>>>> " + p);


			if (p != null)
				p.boolval = b;
		}

		 


		public void initiateRipple_setBoolParameterValueByName(string n, bool v, bool upstreamOnly = false) 
		{

			AXParameter p = parameters.Find(x => x.Name.Equals(n));



			if (p == null)
				return;

			model.latestEditedParameter = p;

			p.initiateRipple_setBoolValueFromGUIChange(v);
			generator.parameterWasModified(p);
		}

		public void setBoolValueUpwardsByName(string n, string guid, bool v, bool upstreamOnly = false) 
		{
			AXParameter p = parameters.Find(x => x.Name.Equals(n));
			if (p == null)
				return;
			p.setBoolValueUpwards(guid, v);
		}



		public AXParameter getParameterForGUID(string g) 
		{
			return parameters.Find(x => x.Guid.Equals(g));
		}



		// ALL INPUT SHAPES
		public List<AXParameter> getAllInputShapes()
		{
			List<AXParameter> retList = new List<AXParameter>();

			//Debug.Log ("GET ALL INPUT SHAPES FOR "+Name);

			foreach(AXParameter p in parameters)
			{
				if (p.Type == AXParameter.DataType.Spline && p.PType == AXParameter.ParameterType.Input)
					retList.Add (p);
			}

			AXShape inputShape = getShape("Input Shape");
			if (inputShape == null)
				inputShape = getShape("Input Shapes");

			if (inputShape!= null && inputShape.inputs != null && inputShape.inputs.Count > 0)
			{	
				//Debug.Log (Name+" has Shape Inputs! " +inputShape.inputs);
				foreach(AXParameter shpP in inputShape.inputs)
					retList.Add (shpP);

			}

			return retList;
		}

		// ALL OUTPUT SHAPES
		public List<AXParameter> getAllOuputs()
		{
			List<AXParameter> retList = new List<AXParameter>();
			foreach(AXParameter p in parameters)
			{
				if (p.PType == AXParameter.ParameterType.Output)
					retList.Add (p);
			}

			if (generator is ShapeMerger)
			{
				ShapeMerger gener = (ShapeMerger) generator;

				if (gener.S_InputShape != null)
				{
					retList.Add (gener.S_InputShape.difference);
					retList.Add (gener.S_InputShape.differenceRail);
					retList.Add (gener.S_InputShape.intersection);
					retList.Add (gener.S_InputShape.intersectionRail);
					retList.Add (gener.S_InputShape.union);
					retList.Add (gener.S_InputShape.grouped);
				}
			}

			return retList;
		}





		// GET_PREFERRED_INPUT_PARAMETER
		public AXParameter getPreferredInputParameter()
		{
			if (generator == null)
				return null;

			return generator.getPreferredInputParameter();

		}

		public AXParameter getPreferredOutputSplineParameter()
		{
			if (generator == null)
				return null;

			return generator.getPreferredOutputParameter();

		}

		// GET_ALL_INPUT_PARAMETERS
		public List<AXParameter> getAllInputParameters()
		{
			List<AXParameter> inputs = new List<AXParameter>();
			for (int i = 0; i < parameters.Count; i++) 
			{
				if (parameters [i].PType == AXParameter.ParameterType.Input) 
					inputs.Add (parameters [i]);
			}
			if (shapes != null)
			{
				for (int i = 0; i < shapes.Count; i++) {
					for (int j = 0; j < shapes [i].inputs.Count; j++) {
						if (shapes [i].inputs [j].PType == AXParameter.ParameterType.Input)
							inputs.Add (shapes [i].inputs [j]);
					}
				}
			}
			return inputs;
		}



		public void setInputsStatus()
		{

			List<AXParameter> inputs = getAllInputParameters();

			hasInputsConnected = false;

			foreach (AXParameter input in inputs)
			{
				if (input.DependsOn != null)
				{
					hasInputsConnected = true;
					return;
				}


			}


		}


		// GET_ALL_OUTPUT_PARAMETERS
		public List<AXParameter> getAllSplineAndMeshParameters()
		{
			List<AXParameter> outputs = new List<AXParameter>();
			foreach(AXParameter p in parameters)
			{
				if (p.Type == AXParameter.DataType.Spline || p.Type == AXParameter.DataType.Mesh)
					outputs.Add (p);
			}
			if (shapes != null)
			{
				foreach(AXShape shp in shapes)
				{
					foreach(AXParameter p in shp.inputs)
						if (p.Type == AXParameter.DataType.Spline || p.Type == AXParameter.DataType.Mesh)
							outputs.Add (p);

					outputs.Add(shp.difference);
					outputs.Add(shp.differenceRail);
					outputs.Add(shp.intersection);
					outputs.Add(shp.intersectionRail);
					outputs.Add(shp.union);
					outputs.Add(shp.grouped);
				}
			}
			return outputs;
		}


		#endregion


		public void clearParameterSets()
		{
			if (baseControls != null && baseControls.children != null)
				baseControls.children.Clear();

			if (inputControls != null && inputControls.children != null)
				inputControls.children.Clear();

			if (outputsNode != null && outputsNode.children != null)
				outputsNode.children.Clear();

			if (geometryControls != null && geometryControls.children != null)
				geometryControls.children.Clear();

			if (positionControls != null && positionControls.children != null)
				positionControls.children.Clear();

		}

		public void closeParameterSets()
		{
			showHandles 	= false;
			showLogic 	= false;
			showControls = false;

			//if (inputControls != null)
			//inputControls.isOpen = false;

			if (geometryControls != null)
				geometryControls.isOpen = false;

			if (positionControls != null)
				positionControls.isOpen = false;

			if (textureControls != null)
				textureControls.isOpen = false;

			showPhysics = false;

			foreach (AXParameter p in parameters)
				p.isOpen = false;


		}

		public void closeAllParameterSets()
		{
			showHandles 	= false;
			showLogic 	= false;
			showControls = false;

			if (inputControls != null)
				inputControls.isOpen = false;

			if (geometryControls != null)
				geometryControls.isOpen = false;

			if (positionControls != null)
				positionControls.isOpen = false;

			if (textureControls != null)
				textureControls.isOpen = false;

			showPhysics = false;

			foreach (AXParameter p in parameters)
				p.isOpen = false;


		}


		public void syncAllReplicantsControls()
		{
			AXParameter output_p = generator.P_Output;
			if (output_p == null)
				return;

			// immediate consumers that are replicants can be synced with this PO
			foreach (AXParameter consumer_p in output_p.Dependents)
			{
				if (consumer_p.Parent.generator is Replicant)
					consumer_p.Parent.syncControlsOfPTypeWith(AXParameter.ParameterType.GeometryControl, this);
			}
		}
		public void syncAllReplicantsHandles()
		{
			AXParameter output_p = generator.P_Output;
			if (output_p == null)
				return;

			// immediate consumers that are replicants can be synced with this PO
			foreach (AXParameter consumer_p in output_p.Dependents)
				if (consumer_p.Parent.generator is Replicant)
					consumer_p.Parent.syncHandlesWith(this);
		}

		public void syncControlsOfPTypeWith(AXParameter.ParameterType pt, AXParametricObject source_po)
		{
			List<AXParameter> source_parameters = source_po.getAllParametersOfPType(pt);

			AXParameter np = null;
			foreach (AXParameter p in source_parameters)
			{
				np = getParameter(p.Name);
				if (np == null)
					np = addParameter(p.clone());
				else 
					np.copyValues(p);
			}
		}

		public void syncHandlesWith(AXParametricObject source_po)
		{
			resetHandles();
			foreach(AXHandle h in source_po.handles)
			{
				addHandle(h.clone());
			}
		}

		public void removeHandleWithName(string _name)
		{
			if (handles == null)
				return;

			foreach (AXHandle h in handles)
			{
				if (h.Name == _name)
				{
					handles.Remove(h);
					break;
				}
			}

		}


		public void copyTextureControlsFromPO(AXParametricObject po)
		{
			floatValue("uScale", 		po.floatValue("uScale"));
			floatValue("vScale", 		po.floatValue("vScale"));
			floatValue("uShift", 		po.floatValue("uShift"));
			floatValue("vShift", 		po.floatValue("vShift"));
			floatValue("Rot Sides Tex", po.floatValue("Rot Sides Tex"));
		}














		#region Relative Graph Navigation
		public void subItemSelect(string subItemAddress)
		{
			selectedSubItemAddress = subItemAddress; // for example, in grid, "i:j:k"
		}




		// GET_CONSUMER
		// later, move this into generator, so individual ones can best termine what there "nearestRelative"
		public AXParametricObject getConsumer(AXParametricObject upStreamCaller = null)
		{
			// if a Grouper, find the input that has this Upstream Caller, then go downstream based on that input Parameter's Debpendents 
			// rather than the standard Output

			/*
			if (generator is Grouper && upStreamCaller != null)
			{
				// MULTIPLE OUTPUTS... WHICH ONE?
				List<AXParameter> inputPs = upStreamCaller.generator.AllInput_Ps;
				AXParameter input = null;
				for (int i=0; i<inputPs.Count; i++)
				{
					if (inputPs[i].DependsOn != null && inputPs[i].DependsOn.parametricObject == upStreamCaller)
					{
						
					}
				}


			}
			else 
			{
			*/
			// USE STANDARD OUTPUT
			AXParameter output_p = generator.P_Output;// getParameter("Output Mesh");


			if(output_p != null && output_p.Dependents != null && output_p.Dependents.Count > 0)
			{
				for (int i=0; i<output_p.Dependents.Count; i++)
					if (  (! (output_p.Dependents[i].parametricObject.generator is Instance)) && (! (output_p.Dependents[i].parametricObject.generator is Replicant)))
						return output_p.Dependents[i].parametricObject;
			}
			//}
			return null;
		}



		public AXParametricObject getSourcePObyParameterName(string _name)
		{
			AXParameter p = getParameter(_name);
			if (p != null && p.DependsOn != null)
				return p.DependsOn.Parent;

			return null;
		}

		public AXParametricObject getSourcePO()
		{
			/* return input mesh, if there is one 
			 * If not, try to return a spline.
			 */

			AXParameter input_p = getParameter("Input Mesh");

			if (input_p != null && input_p.DependsOn != null)
				return input_p.DependsOn.Parent;

			input_p = getParameter("Input Shape");
			if (input_p != null && input_p.DependsOn != null)
				return input_p.DependsOn.Parent;

			input_p = getParameter("Node Shape");
			if (input_p != null && input_p.DependsOn != null)
				return input_p.DependsOn.Parent;

			input_p = getParameter("Plan Spline");
			if (input_p != null && input_p.DependsOn != null)
				return input_p.DependsOn.Parent;


			return null;
		}





		public bool hasDependents()
		{
			if (generator.AllOutput_Ps != null && generator.AllOutput_Ps.Count > 0)
			{
				for (int i=0; i<generator.AllOutput_Ps.Count; i++)
				{
					if (generator.AllOutput_Ps [i].Dependents != null && generator.AllOutput_Ps [i].Dependents.Count > 0)
						return true;
				}
			}

			/*
			AXParameter output = generator.P_Output;// getParameter("Output Mesh");
				
			if (output != null && output.Dependents != null && output.Dependents.Count > 0)
				return true;
			*/
			return false;
		}


		public List<AXParameter> getAllInputs()
		{
			List<AXParameter> inputs = new List<AXParameter>();


			if (shapes != null)
			{
				foreach (AXShape shp in shapes)
				{
					foreach (AXParameter input in shp.inputs)
					{
						inputs.Add(input);
					}
				}
			}

			foreach (AXParameter p in parameters)
			{
				if (p.PType == AXParameter.ParameterType.Input)
					inputs.Add (p);
			}

			return inputs;
		}
		#endregion







		#region SUBNODES

		public List<AXParametricObject> gatherSubnodes()
		{

			subnodes = new List<AXParametricObject>();
			//Debug.Log (Name+": make parts "+subparts);
			stowTraversal_GUID = System.Guid.NewGuid().ToString();

			foreach (AXParameter p in getAllInputs())
				if (p.DependsOn != null  && ! p.Name.Contains("External"))
					p.DependsOn.Parent.continueToGatherPartsFromInputs(stowTraversal_GUID, this);

			if (generator is Grouper && Groupees != null)
				for (int i=0; i<Groupees.Count; i++)
					Groupees[i].continueToGatherPartsFromInputs(stowTraversal_GUID, this);

			return subnodes;
		}


		public void continueToGatherPartsFromInputs(string _guid, AXParametricObject head)
		{
			/*
			 * Traverse the graph based on all inputs (input parameters as well as controls)
			 * - Stow each
			 * - may encounter problems with po's that have multiple dependents...
			 * 
			 */

			// traversal governor
			if (stowTraversal_GUID == _guid)
				return;
			stowTraversal_GUID = _guid;


			head.subnodes.Add (this);

			foreach (AXParameter p in getAllInputs())
				if (p.DependsOn != null )
					p.DependsOn.parametricObject.continueToGatherPartsFromInputs(_guid, head);

			if (generator is Grouper && Groupees != null)
				for (int i=0; i<Groupees.Count; i++)
					Groupees[i].continueToGatherPartsFromInputs(_guid, head);


		}




		public List<AXParametricObject> gatherSubnodes_SelectedOrHidden()
		{

			subnodes = new List<AXParametricObject>();

			//Debug.Log (Name+": make parts "+subparts);
			stowTraversal_GUID = System.Guid.NewGuid().ToString();

			foreach (AXParameter p in getAllInputs())
			{


				if (p.DependsOn != null)
				{	
					AXParametricObject subnode = p.DependsOn.parametricObject;
					if (model.selectedPOs.Contains(subnode) || (! subnode.isOpen && p.DependsOn.Dependents.Count == 1))
						p.DependsOn.parametricObject.continueToGatherSubnodes_SelectedOrHidden(stowTraversal_GUID, this);
				}

			}

			return subnodes;
		}

		public void continueToGatherSubnodes_SelectedOrHidden(string _guid, AXParametricObject head)
		{
			/*
			 * Traverse the graph based on all inputs (input parameters as well as controls)
			 * - Stow each
			 * - may encounter problems with po's that have multiple dependents...
			 * 
			 */

			// traversal governor
			if (stowTraversal_GUID == _guid)
				return;
			stowTraversal_GUID = _guid;

			head.subnodes.Add (this);

			foreach (AXParameter p in getAllInputs())
			{
				if (p.DependsOn != null)
				{
					AXParametricObject subnode = p.DependsOn.parametricObject;
					if (model.selectedPOs.Contains(subnode) || (! subnode.isOpen && p.DependsOn.Dependents.Count == 1))
					{
						p.DependsOn.parametricObject.continueToGatherPartsFromInputs(_guid, head);
					}
				}
			}
		}





		/*
		public Rect getSubnodesRect(List<AXParametricObject> subnodes)
		{
			
			Rect R = new Rect();
			

			for (int i = 0; i < subnodes.Count; i++) {
				
				Rect r = subnodes[i].rect;
				
				if (i==0)
				{
					R = r;
					continue;
				}
				
				// Keep expanding R to includ this r.
				if (r.xMin<R.xMin) R.xMin=r.xMin;
				if (r.xMax>R.xMax) R.xMax=r.xMax;
				
				if (r.yMin<R.yMin) R.yMin=r.yMin;
				if (r.yMax>R.yMax) R.yMax=r.yMax;
				
			}
			
			return R;
		}
		*/

		public void shiftSubnodesToLeftOfMe(List<AXParametricObject> subnodes)
		{	
			Rect R =  AXUtilities.getBoundaryRectFromPOs(subnodes);

			float xdiff = R.xMax-rect.xMax;
			float ydiff = R.yMax-rect.yMax;

			float safedist = 250;

			float shiftx = 0;
			float shifty = 0;

			if (xdiff > -safedist)
				shiftx = xdiff+safedist;
			else if (xdiff < -safedist)
				shiftx = xdiff+safedist;


			if (ydiff > -safedist)
				shifty = ydiff+safedist;
			else if (ydiff < -safedist)
				shifty = ydiff+safedist;





			if (shiftx != 0)
				for (int i = 0; i < subnodes.Count; i++) 
				{
					subnodes[i].rect.x -= shiftx;
					subnodes[i].rect.y -= shifty;
				}


		}
		public void shiftNodeToLeftOfMe(AXParametricObject po)
		{
			//Debug.Log(Mathf.Abs(po.rect.x-rect.x));
			if (Mathf.Abs(po.rect.x-rect.x) > 250)
			{
				po.rect.x = rect.x-250;
				//po.rect.y = rect.y+150;
			}

		}





		#endregion


		#region CONSUMERS

		public AXParametricObject getSelectedConsumer()
		{
			//Debug.Log("----------------------> " + Name + ".getSelectedConsumer() "+ selectedConsumer);

			if (selectedConsumer != null)
			{
				return selectedConsumer;
			}

			// Proceed if a selected consumer is not defined. 	
			AXParameter p = generator.getPreferredOutputParameter();

			if (p == null || p.Type == AXParameter.DataType.MaterialTool)
				return null;

			//Debug.Log("***** " + p.Name + " **** ouput type **** " + p.Type);
			// perhaps later do a seach through the downstream tree of something that had been selected
			// For now, just take the first one that is not an Instance.
			if (p.Dependents != null && p.Dependents.Count > 0)
			{
				for (int i=0; i<p.Dependents.Count; i++)
				{
					AXParameter d = p.Dependents[i];

					if (d.Type == AXParameter.DataType.MaterialTool || d.Type == AXParameter.DataType.JitterTool || d.parametricObject.generator is Instance2D || d.parametricObject.generator is Instance || d.parametricObject.generator is Replicant || d.parametricObject.generator is AXTool)
						continue;

						//Debug.Log("RETURNING " + d.parametricObject.Name+"."+d.Name);

					return p.Dependents[i].parametricObject;
				}
			}
			return null;
		}
		  







		// RECURSIVE Create a selection chain all the way up the tributaries
		// Each po gets the one above as its "selectedConsumer"
		// Each po also sets its worldDisplayMatrix based on this po' worldMatrix
		public void setSelectedConsumerOfAllInputs(int governor=0)
		{
			// Do not set Selected Consumer of items under an IReplica
			if (generator is IReplica || generator is Instance2D)
				return;

			if (governor++ > 300)
			{
				Debug.Log("governor hit)");
				return;
			}	


			List<AXParameter> inputs = getAllInputParameters();

			foreach(AXParameter input in inputs)
			{
				if (input.Type == AXParameter.DataType.MaterialTool || input.Type == AXParameter.DataType.JitterTool)
					continue;

				if (input.DependsOn == null)
					continue;

				if (input.DependsOn.parametricObject.is3D() && input.DependsOn.Type == AXParameter.DataType.Spline) // for example, the StairProfile output
					continue;

				//Debug.Log (" ++++++++++++++++++++ setting consumer of "+ input.DependsOn.parametricObject.Name + " to " + this.Name);

				//if (input.DependsOn.parametricObject.generator is Grouper)
					

				input.DependsOn.parametricObject.selectedConsumer = this;


				input.DependsOn.parametricObject.setSelectedConsumerOfAllInputs(governor);
			}
		}
		#endregion









		#region STOW_SHOW Subnodes
		// GUI
		public void stowInputsButton(Rect rect)
		{
			/* Toggle to stow and show inputs to this po.
			 *
			 * When stowed, the PO is more self-contained and encapsulates what has gone into it.
			 */
			if (inputsStowed)
			{
				if (GUI.Button(rect, "Show Subnodes"))
				{
					inputsStowed = false;
					startShowInputs();
				}
			}
			else
			{
				if (GUI.Button(rect, "Stow Inputs"))
				{
					inputsStowed = true;

					// traverse graph and stow all inputs (without changing their preference to stow there inputs)
					startStowInputs();
				}
			}
		}



		public void startStowInputs()
		{
			//Debug.Log ("startStowInputs");
			inputsStowed = true;

			stowTraversal_GUID = System.Guid.NewGuid().ToString();

			subnodes = new List<AXParametricObject>();

			foreach (AXParameter p in getAllInputs())
			{
				if (! p.Name.Contains("External") && p.DependsOn != null)
					p.DependsOn.Parent.stowInputs(stowTraversal_GUID, this);
			}
		}
		public void stowInputs(string _guid, AXParametricObject head)
		{
			/*
			 * Traverse the graph based on all inputs (input parameters as well as controls)
			 * - Stow each
			 * - may encounter problems with po's that have multiple dependents...
			 * 
			 */
			// traversal governor
			if (stowTraversal_GUID == _guid)
				return;

			stowTraversal_GUID = _guid;

			isOpen = false;

			head.subnodes.Add (this);

			foreach (AXParameter p in getAllInputs())
			{ 
				if (! p.Name.Contains("External") && p.DependsOn != null)
					p.DependsOn.Parent.stowInputs(_guid, head);
			}
		}





		public void startShowInputs()
		{
			inputsStowed = false;




			
			//stowTraversal_GUID = System.Guid.NewGuid().ToString();
			List<AXParameter> inputs =  getAllInputs();
			float startx = rect.x - inputs.Count*225;
			float starty = rect.y + inputs.Count*50/2;

			for (int i = 0; i < inputs.Count; i++) {
				AXParameter p = inputs [i];

				if (p.DependsOn != null) {
					AXParametricObject po = p.DependsOn.parametricObject;

					po.rect.x = startx + i*225;
					po.rect.y = starty + i*50;

					po.isOpen = true;
					po.startShowInputs ();
				}
			}
			
		}


		public void startShowInputsOLD()
		{
			inputsStowed = false;

			List<AXParametricObject> subnodes = gatherSubnodes();
			//Debug.Log("subnodes.Count="+subnodes.Count);
			shiftSubnodesToLeftOfMe(subnodes);


			//if (! inputsStowed )
			//{
			foreach(AXParametricObject subnode in subnodes)
			{
				subnode.isOpen = true;

			}





			/*
			stowTraversal_GUID = System.Guid.NewGuid().ToString();
			
			foreach (AXParameter p in getAllInputs())
			{
				if (p.DependsOn != null)
					p.DependsOn.Parent.showInputs(stowTraversal_GUID);
			}
			*/
		}

				public void showInputs(string _guid)
		{
			/*
			 * Traverse the graph based on all inputs (input parameters as well as controls)
			 * - Stow each
			 * - may encounter problems with po's that have multiple dependents...
			 * 
			 */

			// traversal governor
			if (stowTraversal_GUID == _guid)
				return;
			stowTraversal_GUID = _guid;

			isOpen = true;


			List<AXParametricObject> subnodes = gatherSubnodes();

			shiftSubnodesToLeftOfMe(subnodes);


			//if (! inputsStowed )
			//{
			foreach(AXParametricObject subnode in subnodes)
				subnode.isOpen = true;

			/*foreach (AXParameter p in getAllInputs())
					if (p.DependsOn != null)
						p.DependsOn.Parent.showInputs(_guid);
						*/
			//}

		}
		#endregion




		// GET_ALL_PARAMETERS
		public List<AXParameter> getAllParameters()
		{
			List<AXParameter> retList = new List<AXParameter>();
			foreach(AXParameter p in parameters)
				retList.Add (p);

			if (shapes != null)
				foreach(AXShape shp in shapes)
					if (shp.inputs != null)
						foreach(AXParameter p in shp.inputs)
							retList.Add (p);
			return retList;
		}


		// GET_ALL_PARAMETERS_OF_ParameterType
		public List<AXParameter> getAllParametersOfPType(AXParameter.ParameterType pt)
		{
			List<AXParameter> retList = new List<AXParameter>();
			foreach(AXParameter p in parameters)
			{
				if(p.PType == pt || (pt == AXParameter.ParameterType.GeometryControl && p.PType == AXParameter.ParameterType.None))
					retList.Add (p);
			}
			return retList;
		}


		public void moveParameterUp(AXParameter p)
		{
			int ind = parameters.IndexOf(p);

			if (ind > 1 && parameters[ind-1].PType ==  AXParameter.ParameterType.GeometryControl)
			{
				parameters.RemoveAt(ind);
				parameters.Insert(ind-1, p);
				regroupParametersByPType();
			}
		}
		public void moveParameterDown(AXParameter p)
		{
			int ind = parameters.IndexOf(p);
			if (ind < parameters.Count+1 && parameters[ind+1].PType ==  AXParameter.ParameterType.GeometryControl)
			{
				parameters.Insert(ind+2, p);
				parameters.RemoveAt(ind);
				regroupParametersByPType();
			}
		}
	

		public void regroupParametersByPType()
		{


				clearParameterSets();
	
		 

				// Top Level Parameters in PO
				for (int j = 0; j < parameters.Count; j++) {
					AXParameter p = parameters [j];
					p.parametricObject = this;
					p.Parent = this;


					//Debug.Log(Name+":"+p.Name + "  ......... " + p.Type + " - " + p.PType);

					switch (p.PType) {
					case AXParameter.ParameterType.Base:
						assertBaseControls();
						baseControls.addChild(p);
						break; 

					case AXParameter.ParameterType.Input:
						assertInputControls();
						inputControls.addChild(p);
						break; 

					case AXParameter.ParameterType.Output:
						assertOutputsNode();
						outputsNode.addChild(p);
						break; 

					case AXParameter.ParameterType.PositionControl:
						assertPositionControls();
						positionControls.addChild(p); ;
						break;
											 
					case AXParameter.ParameterType.GeometryControl:
						assertGeometryControls();
						geometryControls.addChild(p);  
						break; 

					case AXParameter.ParameterType.TextureControl:
						break;

					default:
						p.ParentNode = this;

						break;
					}
				}


		}




		/* ADD INPUT MESH
		 *
		 *
		 */

		// A shape can have any number of input shapes.
		// These shapes will be combined during the generation cycle
		// According to 
		public AXParameter addInputMesh()
		{
			if (meshInputs == null)
				meshInputs = new List<AXParameter>();

			// NEW Mesh PARAMETER 
			AXParameter meshInput = addParameter(AXParameter.DataType.Mesh, AXParameter.ParameterType.Input,  "Input Mesh", true);

			return meshInput;
		} 







		public List<AXParameter> getAllInputMeshParameters() 
		{
			List<AXParameter> meshInputs = new List<AXParameter>();

			foreach(AXParameter p in parameters) 
			{
				if (p.Type == AXParameter.DataType.Mesh && p.PType == AXParameter.ParameterType.Input)
					meshInputs.Add (p);
			}

			return meshInputs;

		}	



		/* SUPPORT CACHING AND RESETTING OF PARAMETERS
		 * This allows repreaters to use this ParametricObject as a template
		 * 
		 */
		public void cacheAndSetParameters(List<AXParameter> pList)
		{

			// 1. cache
			cacheParameterValues();

			//Debug.Log ("*********************************************** SET PARAMETERS *********");
			// 2. set values

			foreach(AXParameter p in pList)
			{
				//Debug.Log (Name + " setParameter " + p.Name);
				rippleValueFromParameter(p);
			}
		}
		public void cacheParameterValues()
		{
			parameterCache = new List<AXParameter>();

			foreach(AXParameter p in parameters)
				parameterCache.Add (p.clone());

			boundsCache = bounds;
		}

		public void revertParametersFromCache()
		{
			//Debug.Log ("*********************************************** REVERT PARAMETERS *********");
			if (parameterCache != null)
				foreach (AXParameter p in parameterCache)
					rippleValueFromParameter(p);

			bounds = boundsCache;
		}



		public void rippleValueFromParameter(AXParameter p)
		{
			//Debug.Log (Name+": setting "+p.Name+ " to " + p.FloatVal);
			switch(p.Type)
			{
			case AXParameter.DataType.Float:
				initiateRipple_setFloatValueFromGUIChange(p.Name, p.FloatVal);	
				break;
			case AXParameter.DataType.Int:
				Debug.Log ("Yo 2");
				initiateRipple_setIntValueFromGUIChange(p.Name, p.IntVal);	
				break;
			case AXParameter.DataType.Bool:
				initiateRipple_setBoolParameterValueByName(p.Name, p.boolval, true);	
				break;
			}
		}







		public bool hasInputSplineReady(string _name)
		{
			AXParameter input_p = getParameter(_name);

			if (input_p == null)
				return false;

			AXParameter source_p = input_p.DependsOn;

			if (source_p != null)
			{
				if(source_p != null && source_p.spline != null && source_p.spline.vertCount > 0) 
					return true;

				if(source_p.polyTree != null && source_p.polyTree.Childs != null && source_p.polyTree.Childs.Count > 0)
					return true;

				if (source_p.paths != null && source_p.paths.Count > 0)
					return true;			
			}

			return false;
		}

		public bool hasInputMeshReady(string _name)
		{
			AXParameter input_p = getParameter(_name);

			if (input_p != null && input_p.DependsOn != null ) 
				return true;

			return false;
		}


		public bool isHead()
		{
			if (generator != null)
				return generator.isHead();

			return false;
		}





		#region Handles
		public void resetHandles()
		{
			handles = new List<AXHandle>();
		}

		public AXHandle addHandle() {

			AXHandle h = new AXHandle(this);
			handles.Add(h);
			//Debug.Log ("HandlesCount = " + handles.Count);
			return h;
		}
		public void addHandle(AXHandle h)
		{
			h.parametricObject = this;
			handles.Add(h);
		}



		public AXHandle addHandle(string name, AXHandle.HandleType typ) 
		{

			AXHandle h = new AXHandle(this);
			h.Name = name;
			h.Type = typ;
			h.pos_x = "0";
			h.pos_y = "0";
			h.pos_z = "0";

			h.expressions = new List<string>();
			h.expressions.Add ("Trans_X=han_x+Trans_X");
			h.expressions.Add ("Trans_Y=han_y+Trans_Y");
			h.expressions.Add ("Trans_Z=han_z+Trans_Z");

			handles.Add(h);
			return h;
		}


		public AXHandle addCircleHandle(string name,   string px, string py, string pz, string pr, string pt, string rad_exp, string tan_exp)
		{
			AXHandle h = new AXHandle(this);
			h.Name = name;
			h.Type = AXHandle.HandleType.Circle;
			h.pos_x = px;
			h.pos_y = py;
			h.pos_z = pz;

			h.radius = pr;
			h.tangent = pt;

			h.expressions = new List<string>();
			h.expressions.Add (rad_exp);
			h.expressions.Add (tan_exp);


			handles.Add(h);
			return h;

		}

		public AXHandle addHandle(string name, AXHandle.HandleType typ, string px, string py, string pz, string exp) 
		{

			AXHandle h = new AXHandle(this);
			h.Name = name;
			h.Type = typ;
			h.pos_x = px;
			h.pos_y = py;
			h.pos_z = pz;
			h.expressions = new List<string>();

			h.expressions.Add (exp);

			handles.Add(h);
			return h;
		}

		public AXHandle addHandle(string name, AXHandle.HandleType typ, string px, string py, string pz, string radius, string exp) 
		{

			AXHandle h = new AXHandle(this);
			h.Name = name;
			h.Type = typ;
			h.pos_x = px;
			h.pos_y = py;
			h.pos_z = pz;
			h.radius = radius;
			h.expressions = new List<string>();

			h.expressions.Add (exp);

			handles.Add(h);
			return h;
		}  

		public AXHandle addHandle(string name, AXHandle.HandleType typ, string px, string py, string pz, params string[] exprs) 
		{

			AXHandle h = new AXHandle(this);
			h.Name = name;
			h.Type = typ;
			h.pos_x = px;
			h.pos_y = py;
			h.pos_z = pz;
			h.expressions = new List<string>();

			for (int i = 0; i < exprs.Length; i++)
				h.expressions.Add (exprs[i]);

			handles.Add(h);
			return h;
		}






		#endregion





		public AXShape getShape(string n)
		{
			if (shapes == null)
				return null;


			return shapes.Find(x => x.Name.Equals(n));
		}

		public List<AXParameter> getShapeInputs(string n)
		{
			if (shapes == null)
				return null;

			AXShape shp = shapes.Find(x => x.Name.Equals(n));

			return shp.inputs;
		}







		public Bounds getBoundsAdjustedForAxis()
		{
			Axis axis 		=  (Axis) intValue("Axis");

			// Y-Axis orientation
			Vector3 center 	= bounds.center;
			Vector3 extents = bounds.extents;
			Vector3 size 	= bounds.size;

			switch (axis)
			{
			case Axis.X:
				center 		= new Vector3(-bounds.center.y, 	bounds.center.z, 	bounds.center.x);
				extents 	= new Vector3(bounds.extents.y, bounds.extents.z, 	bounds.extents.x);
				size 		= new Vector3(bounds.size.y, 	bounds.size.z, 		bounds.size.x);

				break;

			case Axis.NX:
				center 		= new Vector3(bounds.center.y, bounds.center.z, 	bounds.center.x);
				extents 	= new Vector3(bounds.extents.y, bounds.extents.z, 	bounds.extents.x);
				size 		= new Vector3(bounds.size.y, 	bounds.size.z, 		bounds.size.x);

				break;

			case Axis.NY:
				center 		= new Vector3(bounds.center.x, -bounds.center.y, 	bounds.center.z);

				break;

			case Axis.Z:
				center 		= new Vector3(bounds.center.x, 	bounds.center.z, 	bounds.center.y);
				extents 	= new Vector3(bounds.extents.x, bounds.extents.z, 	bounds.extents.y);
				size 		= new Vector3(bounds.size.x, 	bounds.size.z, 		bounds.size.y);

				break;

			case Axis.NZ:
				center 		= new Vector3(bounds.center.x, 	bounds.center.z, 	-bounds.center.y);
				extents 	= new Vector3(bounds.extents.x, bounds.extents.z, 	bounds.extents.y);
				size 		= new Vector3(bounds.size.x, 	bounds.size.z, 		bounds.size.y);


				break;

			}

			Bounds b 		= new Bounds();
			b.center 		= center;
			b.extents 		= extents;
			b.size 			= size;

			return b;

		}




		public int codeLineCount()
		{
			if (code == null)
				return 0;

			List<string> lines = new List<string>( code.Split("\n"[0]) );
			return lines.Count;
		}



		public void parameterEditClicked(AXParameter p)
		{
			Debug.Log("Parameter edit clicked: " + p.Name);
			doneEditing();
			p.isEditing = true;
		}


		public bool is2D()
		{
			if (generator is Generator2D)
				return true;
			return false;
		}
		public bool is3D()
		{
			if (generator is AX.Generators.Generator3D)
				return true;
			return false;
		}





		#region Transform Matrices

		public Curve getTransformedCurve() {
			Curve c = new Curve();
			float tx = floatValue("Trans_X");
			float ty = floatValue("Trans_X");

			foreach (CurvePoint cp in curve)
				c.Add(new CurvePoint(cp.position.x+tx, cp.position.y+ty));

			return c;
		}





		public Matrix4x4 getLocalTransformMatrix()
		{

			if (is2D())
			{

				AX.Generators.Generator2D gener2D = (generator as AX.Generators.Generator2D);
				return Matrix4x4.TRS(new Vector3(gener2D.transX, gener2D.transY, 0), Quaternion.Euler(0, 0, gener2D.rotZ), Vector3.one);
			}
			else if (is3D())
			{

				AX.Generators.Generator3D gener3D = (generator as AX.Generators.Generator3D);
				//Vector3 scale = (getParameter("Scale_X")== null) ? Vector3.one : new Vector3(floatValue("Scale_X"), floatValue("Scale_Y"), floatValue("Scale_Z"));
				Vector3 scale = (gener3D.P_Scale_X == null) ? Vector3.one : getLocalScale();

				//scale = Vector3.one;
				return Matrix4x4.TRS(new Vector3(gener3D.transX, gener3D.transY, gener3D.transZ), Quaternion.Euler(gener3D.rotX, gener3D.rotY, gener3D.rotZ), scale);
			}
			return Matrix4x4.identity;
		}

		public Matrix4x4 getLocalTranslationMatrix()
		{
			if (is2D())
			{
				AX.Generators.Generator2D gener2D = (generator as AX.Generators.Generator2D);
				return Matrix4x4.TRS(new Vector3(gener2D.transX, gener2D.transY, 0), Quaternion.identity, Vector3.one);
			}
			else if (is3D())
			{
				AX.Generators.Generator3D gener3D = (generator as AX.Generators.Generator3D);
				return Matrix4x4.TRS(new Vector3(gener3D.transX, gener3D.transY, gener3D.transZ), Quaternion.identity, Vector3.one);
			}
			return Matrix4x4.identity;
		}

		public Matrix4x4 getLocalRotationMatrix()
		{	
			if (is2D())
			{
				AX.Generators.Generator2D gener2D = (generator as AX.Generators.Generator2D);

				return Matrix4x4.TRS(Vector2.zero, Quaternion.Euler(0, 0, gener2D.rotZ), Vector3.one) ;
			}
			else if (is3D())
			{
				AX.Generators.Generator3D gener3D = (generator as AX.Generators.Generator3D);
				return Matrix4x4.TRS(Vector2.zero, Quaternion.Euler(gener3D.rotX, gener3D.rotY, gener3D.rotZ), Vector3.one);
			}
			return Matrix4x4.identity;



		}

		public Vector3 getLocalScale()
		{
			AX.Generators.Generator3D gener3D = (generator as AX.Generators.Generator3D);
			if (gener3D != null)
				return  new Vector3(gener3D.scaleX, gener3D.scaleY, gener3D.scaleZ);

			return Vector3.one;

		}

		public Vector3 getLocalScaleAxisRotated()
		{

			AX.Generators.Generator3D gener3D = (generator as AX.Generators.Generator3D);

			//if (gener3D.P_Scale_X == null)
			//	return Vector3.one;


			Vector3 scale =  new Vector3(gener3D.scaleX, gener3D.scaleY, gener3D.scaleZ);



			switch ((Axis) gener3D.axis)
			{
			case Axis.X:
				scale = new Vector3(gener3D.scaleZ, gener3D.scaleX, gener3D.scaleY);
				break;
			case Axis.NX:
				scale = new Vector3(gener3D.scaleZ, gener3D.scaleX, gener3D.scaleY);
				break;
			case Axis.Z:
				scale = new Vector3(gener3D.scaleX, gener3D.scaleZ, gener3D.scaleY);
				break;
			case Axis.NZ:
				scale = new Vector3(gener3D.scaleX, gener3D.scaleZ, gener3D.scaleY);
				break;

			}

			return scale; 

		}




		public Matrix4x4 getLocalScaleMatrix()
		{
			if (getParameter("Scale_X")== null)
				return Matrix4x4.identity;

			return Matrix4x4.TRS(Vector3.zero, Quaternion.identity, getLocalScale());
		}


		public Matrix4x4 getAxisRotationMatrix()
		{
			Matrix4x4 returnM = Matrix4x4.identity;

			if (is2D())
			{

				//if (generator != null && generator.selectedConsumer != null)
				//	Debug.Log ("selectedConsumer: " + generator.selectedConsumer.Name + " :: " + !generator.selectedConsumer.is2D());
				if (generator != null)				
				{
					switch (generator.consumerType)
					{
					case AX.Generators.Generator.ConsumerType.None:
						// Only time you use this guys home rotation
						returnM = getAxisRotationMatrix2D();
						break;

					case AX.Generators.Generator.ConsumerType.has2DConsumer:
						// Here, there is no 3D directly generator above. 
						// Ignore this generators axis an use the top level 2D PO's axis
						returnM = getAxisRotationMatrix2D(Axis.Y);

						break;

					case AX.Generators.Generator.ConsumerType.has3DConsumer:
						// Because the consumer is 3D, which interprets axis rotation differently, 
						// rotate this just to Y, 
						// then rely on the stack of consumer rotations
						returnM = getAxisRotationMatrix2D(Axis.Y);
						break;


					} 


				}

				return returnM;
			}
			return getAxisRotationMatrix3D();
		}		
		public Matrix4x4 getAxisRotationMatrix2D(Axis axis = 0)
		{		
			//Debug.Log (Name + ": getAxisRotationMatrix2D("+axis+")");
			//return Matrix4x4.identity;
			if (axis == Axis.NONE && generator != null && generator.P_Axis != null)
				axis = (Axis) generator.P_Axis.IntVal;// intValue("Axis");
			float rot_x = 0;
			float rot_y = 0;
			float rot_z = 0;

			switch(axis)
			{
			case Axis.X:
				//rot_x = -90;
				rot_y = -90;

				break;

			case Axis.NX:
				//rot_x = -90;
				rot_y = 90;

				break;

			case Axis.Y:
				rot_x = 90;
				break;

			case Axis.NY:
				rot_x = -90;
				break;

			case Axis.Z:
				rot_y = 180;
				break;

			case Axis.NZ:

				break;			
			}


			return Matrix4x4.TRS(new Vector3(), Quaternion.Euler(rot_x, rot_y, rot_z), new Vector3(1, 1, 1));
		}


		public Matrix4x4 getAxisRotationMatrix3D()
		{		
			//return Matrix4x4.identity;
			if (generator == null || (generator as AX.Generators.Generator3D) == null)
				return Matrix4x4.identity;

			Generator3D gener = (AX.Generators.Generator3D) generator;
			Axis	axis = (Axis)    ((gener != null && gener.P_Axis != null) ?   gener.P_Axis.IntVal : 0);// intValue("Axis");
			//int	axis =  intValue("Axis");
			float rot_x = 0;
			float rot_y = 0;
			float rot_z = 0;

			switch(axis)
			{
			case Axis.X:
				rot_x = -90;
				rot_y = -90;			
				break;

			case Axis.NX:
				rot_x = -90;
				rot_y = 90;			
				break;

			case Axis.Y:			
				break;

			case Axis.NY:
				rot_x = 180;
				break;

			case Axis.Z:
				rot_x = -90;
				rot_y = 180;
				break;

			case Axis.NZ:
				rot_x = -90;


				break;

			}


			return Matrix4x4.TRS(new Vector3(), Quaternion.Euler(rot_x, rot_y, rot_z), new Vector3(1, 1, 1));
		}







		public Matrix4x4 getLocalAlignMatrix()
		{
			return Matrix4x4.TRS(getLocalAlignShifter(), Quaternion.identity, new Vector3(1, 1, 1));
		}
		public Vector3 getLocalAlignShifter()
		{
			// based on the boundingEnvelop box, will generate a positioning matrix for this ParametricObject
			// This is analogous to layout positioning in the UI: TOP-CENTER, etc.

			Axis axis =  (Axis) intValue("Axis");


			//Debug.Log ("axis = " + axis + ", " + Axis.Z);
			Bounds b = bounds;


			float trans_x = 0f;
			float trans_y = 0f;
			float trans_z = 0f;

			float cen_x = b.center.x;
			float cen_y = b.center.y;
			float cen_z = b.center.z;

			float ext_x = b.extents.x;
			float ext_y = b.extents.y;
			float ext_z = b.extents.z;


			int align_x = intValue("Align_X");
			int align_y = intValue("Align_Y");
			int align_z = intValue("Align_Z");



			if (axis == Axis.X || axis == Axis.NX)
			{
				align_y = intValue("Align_X");
				align_x = intValue("Align_Z");
				align_z = intValue("Align_Y");


				if (axis == Axis.NX)
				{
					align_x = -(align_x-2) + 2;
					align_z = -(align_z-2) + 2;
				}

			}
			if (axis == Axis.Z || axis == Axis.NZ)
			{
				align_y = intValue("Align_Z");
				align_z = intValue("Align_Y");

				if (axis == Axis.Z)
				{
					align_y = -(align_y-2) + 2;
					align_z = -(align_z-2) + 2;
				}
			}


			switch (align_x)
			{
			case 1: trans_x = -cen_x + ext_x; 	break;
			case 2: trans_x = -cen_x; 							break;
			case 3: trans_x = -cen_x - ext_x; 	break;
			}

			switch (align_y)
			{
			case 1: trans_y = -cen_y - ext_y; 	break;
			case 2: trans_y = -cen_y; 							break;
			case 3: trans_y = -cen_y + ext_y; 	break;
			}

			switch (align_z)
			{
			case 1: trans_z = -cen_z - ext_z; 	break;
			case 2: trans_z = -cen_z; 							break;
			case 3: trans_z = -cen_z + ext_z; 	break;
			}

			Vector3 shifter = new Vector3(trans_x, trans_y, trans_z);

			// alignment
			return shifter;


		}

		public Vector3 getBoundsShiftAdjustedForAlignmentAndAxis()
		{
			Vector3 shifter = getLocalAlignShifter();


			Axis axis =  (Axis) intValue("Axis");

			if (axis == Axis.X) 						// X-Axis orientation
			{
				shifter 		= new Vector3(shifter.z, 	shifter.y, 	shifter.x);
			} 
			else if (axis == Axis.NZ)					// Z-Axis orientation
			{
				shifter 		= new Vector3(shifter.x,  	shifter.z, shifter.y);
			} 


			return shifter;
		}











		public Vector3 getLocalCenter()
		{	
			if (is2D())
			{	
				AX.Generators.Generator2D gener2D = generator as AX.Generators.Generator2D;
				return new Vector3(gener2D.transX, gener2D.transY, 0);
			}
			AX.Generators.Generator3D gener3D = generator as AX.Generators.Generator3D;
			return new Vector3(gener3D.transX, gener3D.transY, gener3D.transZ);

		}


		// LOCAL_MATRIX
		public void setLocalMatrix()
		{
			if (is2D () )
			{
				//Matrix4x4 m = getLocalTransformMatrix(); 

				Matrix4x4 m = generator.localMatrix; 

				// Validate

				if (float.IsNaN (m.m00)  ||  AXUtilities.GetScale(m) == Vector3.zero)
					m = Matrix4x4.identity;



				// should use axis rotation? Depends on consumer in the selected line....
				selectedConsumer = getSelectedConsumer();
				//Debug.Log(Name + " > selectedConsumer=" + selectedConsumer + " > " + generator.hasOutputsConnected());
				if (selectedConsumer == null &&  ! generator.hasOutputsConnected() )
				{
					m = getAxisRotationMatrix() * m;

				}
				localMatrix = m;

			}
			else
			{
				localMatrix = getLocalTransformMatrix() * getAxisRotationMatrix() * getLocalAlignMatrix() ;
			}

			//return m *  ;


			worldMatrix = model.gameObject.transform.localToWorldMatrix * localMatrix;
		}

		public Matrix4x4 getLocalMatrix()
		{
			return localMatrix;

		}

		public Matrix4x4 getWorldMatrix()
		{
			return worldMatrix;

		}


		public Vector3 getCenterInWorldCoordinates()
		{
			return getLocalMatrix().MultiplyPoint(bounds.center);
		}

		#endregion



		// GENERATOR INSTANTIATION

		public void instantiateGenerator()
		{
			/* The Generator for this is not serialized and so must be instantiated here
			 * 
			 * Generator instantiation also takes place when a new PO is created in the EditorWindow.
			 * 
			 */
			// CHOOSE A GENERATOR BASE ON TYPE...
			if (m_type == "ShapeRepeater2D")
				m_type = "GridRepeater2D";

			//Type theType = System.Type.GetType("AX.Generators."+m_type);
			Type theType = ArchimatixUtils.AXGetType(m_type);

			//Debug.Log ("Generating "+Name + " using type: "+m_type+" ======= generatorType = " + theType);

			if(theType != null)
			{
				generator = (AX.Generators.Generator)Activator.CreateInstance(theType);

				generator.setParametricObject(this);


			}
		}


		#region Generate Output: Meshes
		/* GENERATE_OUTPUT
		 * 
		 * This function is called by Model or by a ParametricObject
		 * when generating a temporary instance run of its input.
		 * 
		 * This represents a node in a graph traversal and so is a bit 
		 * complex to understand. 
		 * 
		 * A guid is used to ensure that a PO is not called twice, thereby avoiding a cycle.
		 * 
		 * However, since modules do connect in a loop, the loop can overlap its initiating object just once.
		 * 
		 */





		public GameObject generateOutputNow(bool makeGameObjects, AXParametricObject initiator_po) 
		{
			return generateOutputNow(makeGameObjects, initiator_po, true);
		}


		public GameObject generateOutputNow(bool makeGameObjects, AXParametricObject initiator_po, bool renderToOutputParameter) {




			if (! isActive)
			{
				generator.clearOutputs();
				return null;
			}

			if (model.visitedPOs.Contains (this) && ! renderToOutputParameter)
				return null;

			//Debug.Log (" -------- ** GENERATING PO NOW...: " + Name + " isAltered="+isAltered+" ******* First do dependsOn... " + model.visitedPOs.Count);
			//model.sw.milestone(" -> begin generate " + Name);

			model.visitedPOs.Add (this);


			// in the future, the selected PO's get called, they crawl downstream first, 
			// then start executing upstream, either as a replicant, or just letting them use their internal values.
			// In this way, you limit the numeber of executions and only those in the connected components (i.e., a 
			// totally separate assembly will not be re-executed.

			// In Dependents graph Shapes and Meshes are polled (as opposed to Relations, which are pushed)
			// Make sure po's that this depends are generated first

			//Debug.Log("Do dependents "+(makeGameObjects == false && isAltered));
			if (makeGameObjects == false && isAltered)
			{

				for (int i = 0; i < getAllInputParameters ().Count; i++) {
					AXParameter p = getAllInputParameters () [i];
					if (p.DependsOn != null && p.DependsOn.parametricObject != null) {
						//Debug.Log ("check " + p.Name + " <= " + p.DependsOn.Parent.Name);
						if (grouper == null || p.DependsOn.parametricObject != grouper)
						if (! model.visitedPOs.Contains (p.DependsOn.parametricObject))
						if (p.DependsOn.parametricObject.isAltered)
							p.DependsOn.parametricObject.generateOutputNow (makeGameObjects, initiator_po);
					}
				}



				// *** Perhaps move this to the actual gernate of the Groupers 
				// so that all the inputs (thicknesses, etc.) have been processed first.
				/*
				if (Groupees != null && Groupees.Count > 0)
				{
					for( int i=0; i<Groupees.Count; i++)
					{
						AXParametricObject groupee = Groupees [i];
						if (! visited_pos.Contains (groupee))

							//Debug.Log(groupee.Name + " isAltered="+groupee.isAltered);
						if (groupee.isAltered)
							groupee.generateOutputNow (ref visited_pos, makeGameObjects, initiator_po);
					}
					 
				}
				*/

			}




			if (code != null) 
				executeOutParameters(new List<string>( code.Split("\n"[0])));



			// NOW GENERATE!!!
			//Debug.Log(Name + " generate " + generator);


			isAltered = false;

			if (generator != null)
			{
				GameObject retGO =  generator.generate(makeGameObjects, initiator_po, renderToOutputParameter);
				//Debug.Log("WHOA: " + Name + " : " + generator.P_Output.meshes);

				//model.sw.milestone(" *** ***  end generate " + Name);
				return retGO;
			}	

			//model.sw.milestone("done: " + Name);


			return null;

		}







		// FINISH MESHES


		/* Mesh Generators will call the following finishing functions to take
		 * care of common bookkeeping.
		 * 
		 * Single mesh or single AXMesh finihing does not
		 * need to worry about inhereting transformations because
		 * these ParametricObjects do not have Input Meshes
		 * 
		 * 
		 */
		public void finishSingleMeshAndOutput(Mesh mesh, Matrix4x4 tm)
		{
			finishSingleMeshAndOutput(mesh, tm, null);
		}
		public void finishSingleMeshAndOutput(Mesh mesh, Matrix4x4 tm, Material mat)
		{
			finishSingleMeshAndOutput(mesh, tm, mat, false);
		}
		public void finishSingleMeshAndOutput(Mesh mesh, Matrix4x4 tm, Material mat, bool isReplica)
		{
			//Output = getParameter("Output Mesh");
			if (generator.P_Output == null)
				return;

			

			Material tmpMat = (mat == null) ? axMat.mat : mat;
			if (tmpMat == null)
				tmpMat = model.axMat.mat;
			//Debug.Log ("HAVE A MAT: " + tmpMat);


			AXMesh axmesh 				= new AXMesh( mesh, tm, tmpMat);
			bounds = axmesh.mesh.bounds;

			axmesh.rot_Y_rand =  boolValue("rot_Y_Rand");
			axmesh.name = Name;

			axmesh.transMatrix = getLocalMatrix();

			axmesh.makerPO = this;



			generator.P_Output.meshes.Clear ();
			generator.P_Output.meshes.Add (axmesh);


			stats_VertCount 			= mesh.vertices.Length;
			stats_TriangleCount 		= mesh.triangles.Length/3;

			if (shouldRenderSelf())
			{
				model.addAXMeshes(generator.P_Output.meshes);
			}



		}


		public void finishSingleAXMeshAndOutput(AXMesh axmesh)
		{
			finishSingleAXMeshAndOutput(axmesh, false);
		}
		public void finishSingleAXMeshAndOutput(AXMesh axmesh, bool isReplica)
		{
			//Output = getParameter("Output Mesh");
			if (generator.P_Output == null)
				return;

			axmesh.rot_Y_rand =  boolValue("rot_Y_Rand");

			bounds = axmesh.mesh.bounds;
			transMatrix = getLocalMatrix();
			axmesh.transMatrix = getLocalMatrix();
			axmesh.makerPO = this;



			// THUMBNAILS IN RENDERTEXTURE (LIVE, NOT CACHED TO Texture2D)

			//if (cam == null || renTex == null)
			//		initCamera();

			//Bounds b = getBoundsAdjustedForAxis();

			//cameraRig.transform.position = getThumbnailCameraPosition(b); 
			//Matrix4x4 remoteM =  Matrix4x4.TRS (getThumbnailCameraTarget(b), Quaternion.identity, new Vector3(1,1,1));

			//Graphics.DrawMesh(axmesh.mesh, remoteM*axmesh.transMatrix, axmesh.mat, 0, cam);



			generator.P_Output.meshes.Clear ();
			generator.P_Output.meshes.Add (axmesh);

			stats_VertCount 			= axmesh.mesh.vertices.Length;
			stats_TriangleCount 		= axmesh.mesh.triangles.Length/3;

			//if (! isReplica && (Output.Dependents == null || Output.Dependents.Count == 0 || Output.Dependents[0].Parent.Type=="Instance" || Output.Dependents[0].Parent.Type=="Replicant"))
			if (shouldRenderSelf())
			{   
				model.addAXMeshes(generator.P_Output.meshes);
			}




		}

		public bool shouldRenderSelf(bool calledFromGrouper = false)
		{
			if (is2D())
				return false;

			//AXParameter Output = getParameter("Output Mesh");
			if (! calledFromGrouper && grouper != null)
				return false;


			if (generator.P_Output != null && generator.P_Output.Dependents != null && generator.P_Output.Dependents.Count > 0)
			{
				foreach (AXParameter d in generator.P_Output.Dependents)
					if (d.parametricObject.generator is Grouper)
						return false;			

				foreach (AXParameter d in generator.P_Output.Dependents)
				{
					if (d.parametricObject.generator is IReplica)
						return true;
				}
				return false;	    	    	    
			}


			return true;

		}

		/*
		 * Assume each of the AXMeshes passed into this function (originated up the input chain)
		 * already have baked into their transforms a hereditary component and an organizational
		 * component (especially if this is a Repeater). All that is left is to multiply 
		 * each AXMesh by this object's local transformation matrix.
		 * 
		 * Each organization object decides what hereditary transform to include before this step.
		 * For example, an Instance or Replicant ignores 
		 * the most recent input parent and goes to the grand parent
		 */ 


		public void finishMultiAXMeshAndOutput(List<AXMesh> ax_meshes, bool renderToOutputParameter = true)
		{
			
			stats_VertCount 	= 0;
			stats_TriangleCount = 0;

			//Debug.Log("ax_meshes.Count="+ax_meshes.Count + " " + Name);

			if (! renderToOutputParameter)
			{	
				// assume this is a temporary render 
				// This is used when a PO makes temporary versions of its upstream items.
				// For example, when a grid Replicant or a Repeater makes a custom version
				// by caching, altering and generating a one-off item. 
				temporaryOutputMeshes = new List<AXMesh>();


				for (int i = 0; i < ax_meshes.Count; i++) {
					AXMesh amesh = ax_meshes [i];
					Material tmp_mat = (amesh.mat == null) ? axMat.mat : amesh.mat;
					if (tmp_mat == null)
					{
						if (grouper != null && grouper.axMat != null && grouper.axMat.mat != null)
							tmp_mat = grouper.axMat.mat;
					}

					if (tmp_mat == null)
						tmp_mat = model.axMat.mat;
					amesh.transMatrix = getLocalMatrix () * amesh.transMatrix;
					//Debug.Log("Mesh.verts="+amesh.mesh.vertices.Length);
					temporaryOutputMeshes.Add (amesh);
				}

			}
			else
			{


				//StopWatch sw = new StopWatch();
				//Debug.Log("START: " + Name +"---------------------"+generator.P_Output);
				//Output = getParameter("Output Mesh");
				if (generator.P_Output == null)
				{

					Debug.Log("Node has no Output Paramter ");
					return;
				}

				if (generator.P_Output.meshes == null)
					generator.P_Output.meshes = new List<AXMesh>();

				//Debug.Log(Name + " " + generator.P_Output.meshes);
				//foreach(AXMesh omesh in Output.meshes)
				//UnityEngine.Object.DestroyImmediate(omesh.mesh);

				generator.P_Output.meshes.Clear();


				Matrix4x4 localM = getLocalMatrix();

				//localM *= getLocalScaleMatrix();

				//Debug.Log("REP 1:"+ sw.duration());
				for (int i = 0; i < ax_meshes.Count; i++) {
					AXMesh amesh = ax_meshes [i];
					if (amesh.mesh == null)
						return;
					Material tmp_mat = (amesh.mat == null) ? axMat.mat : amesh.mat;
					if (tmp_mat == null)
					{
						if (grouper != null && grouper.axMat != null && grouper.axMat.mat != null)
							tmp_mat = grouper.axMat.mat;
					}

					if (tmp_mat == null)
						tmp_mat = model.axMat.mat;
					/* Inherit transforms
					 * adjust each mesh to positioning pref of this Parametric object
					 */amesh.transMatrix = localM * amesh.transMatrix;
					//if (true)
					//{
					/*
						for (int i=0; i<amesh.mesh.vertices.Length; i++)
						{
							if (false && amesh.mesh.normals != null && amesh.mesh.normals.Length >= (i-1))
						     	Debug.DrawRay(amesh.mesh.vertices[i], amesh.mesh.normals[i], Color.yellow, 125);
							
							if (amesh.mesh.tangents.Length > i)
							Debug.DrawRay(amesh.mesh.vertices[i], amesh.mesh.tangents[i], Color.red, 125);
						}
						*///}

					generator.P_Output.meshes.Add (amesh);
					if (amesh.mesh.vertices != null) {
						stats_VertCount += amesh.mesh.vertices.Length;
						stats_TriangleCount += amesh.mesh.triangles.Length / 3;
					}
				}
				//Debug.Log("REP 2:"+ sw.duration());
				if (shouldRenderSelf() && generator.P_Output != null)
				{
					model.addAXMeshes(generator.P_Output.meshes);
				}
				//Debug.Log("REP 3:"+ sw.duration());

			}

		}

		#endregion







		#region Generate Output: GameObjects



		// MAKE_GAMEOBJECTS_FROM_AXMESHES

		// finish meshes as GameObjects
		// Takes a list of AXMeshes and sorts them into sublists by Material
		// Then combines meshes for all items of like Material

		public GameObject makeGameObjectsFromAXMeshes(List<AXMesh> ax_meshes, bool doInvert = false, bool addColliders = true)
		{

			//Debug.Log("makeGameObjectsFromAXMeshes");

			// 0. create GameObject to hold the super mesh
			// 1. Create the super Mesh

			// 1. Get the count of vertices from the sum all the vertices in ax_meshes
			// 2. Combine all vertices, normals, tangents and uvs into one arrays for the super mesh
			// 3. sort the axmeshes by material
			// 4. create a mapping lookup of old verts to new verts indices.

			// Triangles and renderer sharedMaterials

			// 4. Sorting hat - sort meshes into lists by material
			// 5. Use dictionary to establish an order of materials. 
			// 6. take all the triangles 

			// VOLUME
			// Some of these ax_meshes will have volume set, others won't.
			// summ volumes for the volume of the go.


			Dictionary<Material, List<AXMesh>> axmeshesByMatDictionary = new Dictionary<Material, List<AXMesh>>();

			// Since go's are created per material, each material may have a collective volume (though often not, since tit is often caps with no added volume.
			Dictionary<Material, float> volumesByMatDictionary = new Dictionary<Material, float>();

			Matrix4x4 inverter = Matrix4x4.identity;

			if (doInvert)
				inverter = getLocalMatrix().inverse ;


			// SORT BY MATERIAL INTO LISTS
			for (int i = 0; i < ax_meshes.Count; i++) {
				AXMesh axmesh = ax_meshes [i];

				//Debug.Log("axmesh.mat ="+axmesh.mat + " axMat.mat=" + axMat.mat);
				Material tmp_mat = (axmesh.mat == null) ? axMat.mat : axmesh.mat;

				if (tmp_mat == null)
				{
					if (grouper != null && grouper.axMat != null && grouper.axMat.mat != null)
						tmp_mat = grouper.axMat.mat;
				}
				if (tmp_mat == null)
					tmp_mat = model.axMat.mat;
				List<AXMesh> tmpList = null;

				if (axmeshesByMatDictionary.ContainsKey (tmp_mat))
				{
					tmpList = axmeshesByMatDictionary [tmp_mat];
					volumesByMatDictionary[tmp_mat] += axmesh.volume;
				}
				else 
				{
					tmpList = new List<AXMesh> ();
					axmeshesByMatDictionary.Add (tmp_mat, tmpList);
					volumesByMatDictionary.Add(tmp_mat, axmesh.volume);
				}
				tmpList.Add (axmesh);
			}


			//GameObject go = new GameObject(Name);
			GameObject go = ArchimatixUtils.createAXGameObject(Name, this);
			int itemNo = 1;

			GameObject prevColliderObject = null;



			// order the dictionary to get an order of materials
			// add 


			/// ALTERANTIVE FOR MEGAFIERS - USE ONE MESH WITH SUBMESHES PER MATERIAL (though this hurts draw calls)
			/// /// /// Renderer rr = new Renderer();
			/// /// /// rr.sharedMaterials = new Material[axmeshesByMatDictionary.Count];

			int mat_i = 0;

			foreach(KeyValuePair<Material, List<AXMesh>> entry in axmeshesByMatDictionary)
			{

				// // // rr.sharedMaterials[mat_i] =  (Material) entry.Key;


				//Debug.Log ("******* mat list");

				// 1. add this material to the supermesh's rendereer as a new sharedMaterial
				// 2. go through each of these ax_meshes and 
				// 	a. add the verts to the super
				//	b. creat index map
				//  c. add triangles.

				// https://forum.unity3d.com/threads/meshcombineutility-cs-unityengine-mesh-settrianglestrip-int-int-is-obsolete.168766/

				AXParametricObject makerPO = null;


				List<AXMesh> axmeshesToCombineForThisMaterial = entry.Value;
				Material material = entry.Key;
				float vol = volumesByMatDictionary[material];

				CombineInstance[] combinator 			= new CombineInstance[axmeshesToCombineForThisMaterial.Count];


				bool getsCollider = false;



				int combineCt = 0;
				for (int i = 0; i < axmeshesToCombineForThisMaterial.Count; i++) {
					AXMesh axmesh = axmeshesToCombineForThisMaterial [i];
					getsCollider = axmesh.getsCollider;

					if (axmesh.makerPO != null)
						makerPO = axmesh.makerPO;

					combinator[combineCt].mesh 		= axmesh.mesh;
					combinator[combineCt].transform = inverter * axmesh.transMatrix;

					combineCt++;
				}
				//Debug.Log("combineCt="+combineCt);


				Mesh returnMesh = new Mesh();
				returnMesh.CombineMeshes(combinator, true, true);
				returnMesh.name = Name;


				// TANGENTS
				AXGeometryTools.Utilities.calculateMeshTangents(ref returnMesh);


				// SECONDARY UVs FOR BAKING
				// This was very useful: http://www.protoolsforunity3d.com/forum/index.php?/topic/1652-erroneous-lightmapping-behaviour/
				if (model.createSecondaryUVs)
				{
					#if UNITY_EDITOR  
					Unwrapping.GenerateSecondaryUVSet (returnMesh); 
					#endif
				}





				if (axmeshesByMatDictionary.Count == 1)
				{
					// There was only one material


					// just add the meshes to the gameObject
					MeshFilter mf = (MeshFilter) go.AddComponent(typeof(MeshFilter));
					mf.sharedMesh = returnMesh;

					MeshRenderer rend = (MeshRenderer) go.AddComponent(typeof(MeshRenderer));

					if (noMeshRenderer)
						rend.enabled = false;

					rend.material = entry.Key;




					if (addColliders)
						addCollider(go, vol);


				}
				else
				{
					// add children by material

					if (entry.Key == null)
						continue;

					if (makerPO == null)
						makerPO = this;

					GameObject obj = ArchimatixUtils.createAXGameObject("item_"+ (itemNo++) +"_"+entry.Key.name, makerPO);


					MeshFilter mf = (MeshFilter) obj.AddComponent(typeof(MeshFilter));
					mf.sharedMesh = returnMesh;

					MeshRenderer rend = (MeshRenderer) obj.AddComponent(typeof(MeshRenderer));
					rend.material = entry.Key;

					//if (! combineMeshes)
					if (getsCollider || prevColliderObject == null)
					{    
						if (addColliders)
							addCollider (obj, vol);
						prevColliderObject = obj;
						obj.transform.parent = go.transform;
					}
					else
						obj.transform.parent = prevColliderObject.transform;

				}

//				List<AXMesh> meshes = new List<AXMesh>();
//				meshes.Add(new AXMesh(returnMesh, Matrix4x4.identity));
//				generator.P_Output.meshes = meshes;


				mat_i++;

			}

			return go;

		}







		public  void addCollider(GameObject go, float volume = 0)
		{
			//Debug.Log (Name + ":"+go.name+" colliderType = " + colliderType);
			Mesh mesh = null;
			MeshFilter mf = go.GetComponent<MeshFilter>();
			if (mf != null)
				mesh = mf.sharedMesh;

			if (mesh == null)
				return;

			mesh.RecalculateBounds();

			//if (m != null)
			//	bounds = m.bounds;

			PhysicMaterial tmpPhysMat = axMat.physMat;

			if (tmpPhysMat == null)
				tmpPhysMat = model.axMat.physMat;


			switch(colliderType)
			{

			case ColliderType.Box:
				BoxCollider bc = (BoxCollider) go.AddComponent(typeof(BoxCollider));

				bc.center  = bounds.center;
				bc.size = bounds.size;

				if (axMat.physMat != null)
					bc.material = axMat.physMat;
				break;

			case ColliderType.Capsule:
				CapsuleCollider cc = (CapsuleCollider) go.AddComponent(typeof(CapsuleCollider));
				cc.center  = bounds.center;
				cc.radius = bounds.extents.x;
				if (axMat.physMat != null)
					cc.material = axMat.physMat;
				break;

			case ColliderType.Sphere:
				SphereCollider sc = (SphereCollider) go.AddComponent(typeof(SphereCollider));
				sc.center  = bounds.center;
				sc.radius = bounds.extents.x;
				if (axMat.physMat != null)
					sc.material = axMat.physMat;
				break;

			case ColliderType.Mesh:
				MeshCollider mc = (MeshCollider) go.AddComponent(typeof(MeshCollider));

				if (axMat.physMat != null)
					mc.material = axMat.physMat;
				break;

			case ColliderType.ConvexMesh:
				MeshCollider mvc = (MeshCollider) go.AddComponent(typeof(MeshCollider));
				mvc.convex = true;
				if (axMat.physMat != null)
					mvc.material = axMat.physMat;
				break;


			}



			if(isRigidbody)
			{
				Rigidbody rb =  (Rigidbody) go.GetComponent<Rigidbody>();
				if (rb == null) rb =  (Rigidbody) go.AddComponent(typeof(Rigidbody));
				rb.interpolation = RigidbodyInterpolation.Interpolate;

				rb.drag = .5f;
				rb.angularDrag = .5f;



				// Calculate mass
				//rb.mass = (volume > 0) ? volume * tex.density : 1;


				float density = axMat.density;

				if (density == 0 && grouper != null)
					density = grouper.axMat.density;

				if (density == 0)
					density = model.axMat.density;

				rb.SetDensity(density);
				rb.mass = rb.mass;


				// The mass has been calculated by Unity based on the Collider geometry.
				// Cache this mass and derived volume in the AXGO.
				AXGameObject axgo = go.GetComponent<AXGameObject>();
				if (axgo != null)
				{
					axgo.volume = rb.mass / axMat.density;
					axgo.mass   = rb.mass;
				}
				//rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;


				rb.isKinematic = isKinematic ;


				//go.isStatic = false;

				//StoneBlockBehavior bb = go.AddComponent<StoneBlockBehavior>();

				 
			}
			else 
			{
				//go.isStatic = true;
			}


			// RUNTIME NAVMESH
			//go.AddComponent(typeof(NavMeshSourceTag));

			//MeshCollider ccc = go.GetComponent<MeshCollider>();
			//Debug.Log("bounds ["+go.name+"]= " + ccc.bounds.center+" :: " + ccc.bounds.extents+" :: " + ccc.bounds.size);

		}





		#endregion






		public Vector3[] transformVertices(Vector3[] origVerts, Matrix4x4 m)
		{
			Vector3[] newVerts = new Vector3[origVerts.Length];

			for (int i=0; i < origVerts.Length; i++) 
				newVerts[i] = m.MultiplyPoint3x4(origVerts[i]);

			return newVerts;
		}













































		#region Editing Mode
		public void doneEditing()
		{
			isEditing = false;
			stopEditingAllParameters();
			stopEditingAllHandles();

		}
		public void stopEditingAllParameters()
		{
			for (int i=0; i<parameters.Count; i++) 
				parameters[i].isEditing = false;
			syncAllReplicantsControls();
		}

		public void stopEditingAllHandles()
		{
			for (int i=0; i<handles.Count; i++) 
				handles[i].isEditing = false;
			syncAllReplicantsHandles();
		}
		public void stopEditingAllHandlesExcept(AXHandle handle)
		{
			for (int i=0; i<handles.Count; i++) 
				if (handles[i] != handle)
					handles[i].isEditing = false;

			syncAllReplicantsHandles();
		}

		#endregion



		#region Mathematical Expression Parsing
		// EXECUTE CODE BLOCK
		public void executeOutParameters(List<string> lines) {
			// initial pass, calculating variable values


			foreach(string line in lines) {
				string trimmedLine = Regex.Replace(line, @"\s+", " ");
				trimmedLine = trimmedLine.Replace(", ",",");

				string[] chunks = trimmedLine.Split (" "[0]);

				AXParameter p;

				switch(chunks[0]) {
				// define a parameter parameter through script

				case "float":
					p = getParameter(chunks[1]);
					if (p == null)
					{
						float val = .5f;
						float min = 0;
						float max = 10;
						if (chunks.Length>2 && chunks[2] != null)
							val = float.Parse( chunks[2] );
						if (chunks.Length>3 && chunks[3] != null)
							min = float.Parse( chunks[3] );
						if (chunks.Length>4 && chunks[4] != null)
							max = float.Parse( chunks[4] );
						p = addParameter(AXParameter.DataType.Float, chunks[1], val, min, max);

					}

					break;

				case "out":
					p = getParameter(chunks[1]);
					if (p == null)
						p = addParameter(new AXParameter(AXParameter.DataType.Float, AXParameter.ParameterType.Output, chunks[1]));
					//p = addParameter(AXParameter.DataType.Float, AXParameter.ParameterType.Output, chunks[1]);

					p.FloatVal = (float) parseMath(chunks[2]);
					break;


				}



			}


		}


		public void setVar(string key, float val)
		{
			if (vars == null)
				vars = new Dictionary<string, float>();
			vars[key] = val;
		}


		public void executeCodeBloc(List<string> lines) 
		{
			executeCodeBloc(lines, null);
		}


		public void executeCodeBloc(List<string> lines, AXTurtle shape) {


			//Debug.Log("executeCodeBloc ======================");
			AXTurtle t = null;

			bool scanningLoop 	= false;
			bool scanningIf		= false;

			List<string> blockLines = new List<string>();
			//bool isParsingLoop = false;

			string codeAsLine = "";
			foreach (String line in lines)
			{
				string newline = "";
				if (codeAsLine != "")
					newline = "%";
				codeAsLine += newline+line;
			}
			//Debug.Log (codeAsLine);


			if (shape != null) {
				t = shape;
			}

			if (t == null)
				return;

			int 		loopct 		= 0;
			double 		repeats 	= 3;
			int 		step 		= 1;
			string 		loopCounterName = "count";

			int if_ct = 0;
			string condition = "";


//			for (int i = 0; i < lines.Count; i++) 
//			{
//				Debug.Log (" .... [" + i + "] : " + lines[i]);
//			}
			//Debug.Log("---------------------------");
			for (int i = 0; i < lines.Count; i++) 
			{
				string line = lines [i];
				string trimmedLine = Regex.Replace (line.Trim(), @"\s+", " ");

				//Debug.Log (i + " : " + line);

				trimmedLine = trimmedLine.Replace (", ", ",");

				string[] chunks = trimmedLine.Split (" " [0]);


				if (chunks [0] == "end") 
				{
					return;
				}

				// LOOP CONTROL
				if (chunks [0] == "loop") {

					if (loopct == 0 && ! scanningIf) {
						//Debug.Log("*** Start scanning loop");
						scanningLoop = true;

						try {
							//Debug.Log(chunks [1]);

							// REPEATS
							repeats = parseMath (chunks [1]) - 1;

							// COUNTER VARIABLE NAME
							if ((chunks.Length > 2) && (! string.IsNullOrEmpty(chunks[2])))
								loopCounterName = chunks[2];
							else 
								loopCounterName = "count";


							// STEP
							if ((chunks.Length > 3) && (! string.IsNullOrEmpty(chunks[3])))
								step = (int) parseMath (chunks [3]);

							if (step < 1)
								step = 1;
							//Debug.Log("**** use loopCounterName="+loopCounterName);
						}
						catch {
							Debug.Log ("parsing error");
						}
					}
					else
					{
						// keep inner loop in this block
						blockLines.Add (trimmedLine);
					}
					loopct++;
					continue;
				}
				else if (chunks [0] == "endloop")
				{
					
					loopct--;

					//Debug.Log(" [ loopct="+loopct+", scanningIf = " + scanningIf);
					if (loopct == 0  && ! scanningIf) {

						scanningLoop = false;
						//Debug.Log("*** *** Execute block " + repeats + " times. blockLines.Count = " + blockLines.Count);
						for (int rr = 0; rr <= repeats; rr+=step) {
							//Debug.Log ("EXECUTING BLOCK: "+blockLines[0]);
							//Debug.Log("set value "+loopCounterName + "="+ rr);
							setVar (loopCounterName, rr); 
							executeCodeBloc (blockLines, t);
							continue;
						}

						blockLines.Clear ();
					}
					else
					{
						// Add nested, inner endloop to this blok
						blockLines.Add (trimmedLine);
					}
					continue;
					
				}




				// CONDIITONAL CONTROL
				//Debug.Log(" do: " + chunks [0]);

				if (chunks [0] == "if") {
					if (if_ct == 0 && ! scanningLoop) {
						scanningIf = true;
						condition = trimmedLine;
						//Debug.Log ("--> " + condition);
					}
					else 
						blockLines.Add (trimmedLine);

					// keep adding ifs until endif count balances
					if_ct++;
					continue;
				}
				else if (chunks [0] == "endif")
				{

					if_ct--;

					//Debug.Log(" [ if_ct="+if_ct+", scanningLoop = " + scanningLoop);

					if (if_ct == 0  && ! scanningLoop) {
						// process conditional

						scanningIf = false;

						string[] cond_chunks = condition.Split (" " [0]);
						string cond1 = cond_chunks [1];
						bool not = (cond1.StartsWith ("!")) ? true : false;
						if (not)
							cond1 = cond1.TrimStart ('!');
						AXParameter cond1_P = getControlParameter (cond1);

						if (cond1_P != null && cond1_P.Type == AXParameter.DataType.Bool) {
							if ((cond1_P.boolval && !not) || (!cond1_P.boolval && not)) {
								executeCodeBloc (blockLines, t);
							}
							else
								blockLines.Clear ();
						}
						else {
							string cond = cond_chunks [2];
							float cval_1 = (float)parseMath (cond_chunks [1]);
							if (cond_chunks.Length < 4) {
								if (cval_1 == 1) {
									executeCodeBloc (blockLines, t);
								}
								continue;
							}
							float cval_2 = (float)parseMath (cond_chunks [3]);
							//Debug.Log("cond_chunks[1] = "+cond_chunks[1]+", cval_1 = " + cval_1+ ", cond_chunks[3] = "+cond_chunks[3]+", cval_2 = " + cval_2);
							switch (cond) {
							case "EQ":
								if (cval_1 == cval_2) {
									executeCodeBloc (blockLines, t);
								}
								else {
									blockLines.Clear ();
								}
								break;
							case "NE":
								if (cval_1 != cval_2) {
									executeCodeBloc (blockLines, t);
								}
								else {
									blockLines.Clear ();
								}
								break;
							case "LT":
								if (cval_1 < cval_2) {
									executeCodeBloc (blockLines, t);
								}
								else {
									blockLines.Clear ();
								}
								break;
							case "LE":
								if (cval_1 <= cval_2) {
									executeCodeBloc (blockLines, t);
								}
								else {
									blockLines.Clear ();
								}
								break;
							case "GE":
								if (cval_1 >= cval_2) {
									executeCodeBloc (blockLines, t);
								}
								else {
									blockLines.Clear ();
								}
								break;
							case "GT":
								if (cval_1 > cval_2) {
									executeCodeBloc (blockLines, t);
								}
								else {
									blockLines.Clear ();
								}
								break;
							}
							// Debug.Log ("EXECUTING BLOCK: "+blockLines[0]);
						}
						blockLines.Clear();

					}
					else
					{
						// keep the endif in this block
						blockLines.Add (trimmedLine);
					}
					continue;
				}



				if (scanningLoop || scanningIf)
				{
					
					blockLines.Add (trimmedLine);
					continue;

				}












				float val1 = 0.0f;
				float val2 = 0.0f;
				int val3 = 0;
				//string expr;
				switch (chunks [0]) {
				case "closed":
					if (chunks [1] == "false") {
						isClosed = false;
						if (generator.P_Output != null)
							generator.P_Output.shapeState = ShapeState.Open;
						//ShapeState
					}
					else {
						isClosed = true;
						if (generator.P_Output != null)
							generator.P_Output.shapeState = ShapeState.Closed;
					}
					break;
				case "set":
					if (chunks.Length == 3) {
						setVar (chunks [1], (float)parseMath (chunks [2]));
						AXParameter v = getParameter (chunks [1]);
						if (v != null) {
							if (v.Type == AXParameter.DataType.Int)
								intValue (chunks [1], (int)parseMath (chunks [2]));
							else
								if (v.Type == AXParameter.DataType.Float)
									setParameterValueByName (chunks [1], (float)parseMath (chunks [2]));
						}
					}
					break;
				case "let":
					if (chunks.Length == 3) {
						setVar (chunks [1], (float)parseMath (chunks [2]));
					}
					break;
				case "mov":
					if (chunks.Length < 3) {
						Debug.Log ("mov needs two arguments: line ignored");
						continue;
					}
					val1 = (float)parseMath (chunks [1]);
					val2 = (float)parseMath (chunks [2]);
					t.mov (val1, val2);
					if (chunks.Length == 4 && chunks [3] != null)
						t.dir ((float)parseMath (chunks [3]));
					break;
				case "dir":
					if (chunks.Length <= 1) {
						Debug.Log ("mov needs two arguments: line ignored");
						continue;
					}
					//if ( (val1= parseFloatToken(chunks[1]) ) == -999999)  continue;
					val1 = (float)parseMath (chunks [1]);
					t.dir (val1);
					break;
				case "fwd":
					if (chunks.Length <= 1) {
						Debug.Log ("mov needs two arguments: line ignored");
						continue;
					}
					//if ( (val1= parseFloatToken(chunks[1]) ) == -999999) continue;
					//Debug.Log ("chunks[1] " + chunks[1]);
					val1 = (float)parseMath (chunks [1]);
					if (chunks.Length > 2) {
						val2 = (float)parseMath (chunks [2]);
						t.fwd (val1, -val2);
					}
					else {
						t.fwd (val1);
					}
					break;
				case "movfwd":
					if (chunks.Length <= 1) {
						Debug.Log ("movfwd needs two arguments: line ignored");
						continue;
					}
					//if ( (val1= parseFloatToken(chunks[1]) ) == -999999) continue;
					//Debug.Log ("chunks[1] " + chunks[1]);
					val1 = (float)parseMath (chunks [1]);
					if (chunks.Length > 2) {
						val2 = (float)parseMath (chunks [2]);
						t.movfwd (val1, -val2);
					}
					else
						t.movfwd (val1);
					break;
				case "back":
					if (chunks.Length <= 1) {
						Debug.Log ("mov needs two arguments: line ignored");
						continue;
					}
					//if ( (val1= parseFloatToken(chunks[1]) ) == -999999) continue;
					val1 = (float)parseMath (chunks [1]);
					if (chunks.Length > 2) {
						val2 = (float)parseMath (chunks [2]);
						t.back (val1, -val2);
					}
					else
						t.back (val1);
					break;
				case "left":
					if (chunks.Length <= 1) {
						Debug.Log ("mov needs two arguments: line ignored");
						continue;
					}
					//if ( (val1= parseFloatToken(chunks[1]) ) == -999999) continue;
					val1 = (float)parseMath (chunks [1]);
					t.left (val1);
					break;
				case "right":
					if (chunks.Length <= 1) {
						Debug.Log ("mov needs two arguments: line ignored");
						continue;
					}
					//if ( (val1= parseFloatToken(chunks[1]) ) == -999999) continue;
					val1 = (float)parseMath (chunks [1]);
					t.right (val1);
					break;
				case "drw":
					if (chunks.Length <= 2) {
						Debug.Log ("mov needs two arguments: line ignored");
						continue;
					}
					//if ( (val1= parseFloatToken(chunks[1]) ) == -999999)  continue;
					//if ( (val2= parseFloatToken(chunks[2]) ) == -999999)  continue;
					val1 = (float)parseMath (chunks [1]);
					val2 = (float)parseMath (chunks [2]);
					t.drw (val1, val2);
					break;
				case "rdrw":
					if (chunks.Length <= 2) {
						Debug.Log ("rdrw needs two arguments: line ignored");
						continue;
					}
					val1 = (float)parseMath (chunks [1]);
					val2 = (float)parseMath (chunks [2]);
					t.rdrw (val1, val2);
					break;
				case "rmov":
					if (chunks.Length <= 2) {
						Debug.Log ("rmov needs two arguments: line ignored");
						continue;
					}
					val1 = (float)parseMath (chunks [1]);
					val2 = (float)parseMath (chunks [2]);
					t.rmov (val1, val2);
					break;
				case "arcl":
					if (chunks.Length <= 3) {
						Debug.Log ("arcl needs two arguments: line ignored");
						continue;
					}
					//if ( (val1= parseFloatToken(chunks[1]) ) == -999999)  continue;
					//if ( (val2= parseFloatToken(chunks[2]) ) == -999999)  continue;
					val1 = (float)parseMath (chunks [1]);
					val2 = (float)parseMath (chunks [2]);
					//if ( (val3= (int) parseFloatToken(chunks[3]) ) == -999999)    continue;
					//val3 = (int) parseMath(chunks[3]);
					val3 = Mathf.Max (1, Mathf.FloorToInt (((float)parseMath (chunks [3]) * model.segmentReductionFactor)));
					if (chunks.Length > 4) {
						float val4 = (float)parseMath (chunks [4]);
						t.arcl (val1, val2, val3, val4);
					}
					else {
						t.arcl (val1, val2, val3);
					}
					break;
				case "arcr":
					if (chunks.Length <= 3) {
						Debug.Log ("arcr needs two arguments: line ignored");
						continue;
					}
					val1 = (float)parseMath (chunks [1]);
					val2 = (float)parseMath (chunks [2]);
					val3 = Mathf.Max (1, Mathf.FloorToInt (((float)parseMath (chunks [3]) * model.segmentReductionFactor)));
					if (chunks.Length > 4) {
						float val4 = (float)parseMath (chunks [4]);
						t.arcr (val1, val2, val3, val4);
					}
					else {
						t.arcr (val1, val2, val3);
					}
					break;
				case "bezier":
					if (chunks.Length <= 9) {
						Debug.Log ("mov needs nine arguments: line ignored");
						continue;
					}
					try {
						Vector2 a = new Vector2 ((float)parseMath (chunks [1]), (float)parseMath (chunks [2]));
						Vector2 b = new Vector2 ((float)parseMath (chunks [3]), (float)parseMath (chunks [4]));
						Vector2 c = new Vector2 ((float)parseMath (chunks [5]), (float)parseMath (chunks [6]));
						Vector2 d = new Vector2 ((float)parseMath (chunks [7]), (float)parseMath (chunks [8]));
						int segs = Mathf.Max (1, Mathf.FloorToInt (((float)parseMath (chunks [9]) * model.segmentReductionFactor)));
						t.bezier (a, b, c, d, segs);
					}
					catch {
						Debug.Log ("bad bezier");
					}
					//int segs = (int) parseMath(chunks[9]);
					break;
				case "molding":
					//  type  a.x a.y b.x b.y segs tension
					if (chunks.Length <= 6) {
						Debug.Log ("mov needs six arguments: line ignored");
						continue;
					}
					string mtype = chunks [1];
					Vector2 pt1 = new Vector2 ((float)parseMath (chunks [2]), (float)parseMath (chunks [3]));
					Vector2 pt2 = new Vector2 ((float)parseMath (chunks [4]), (float)parseMath (chunks [5]));
					//int msegs = (int) parseMath(chunks[6]);
					int msegs = Mathf.Max (1, Mathf.FloorToInt (((float)parseMath (chunks [6]) * model.segmentReductionFactor)));
					float tension = (chunks.Length > 7) ? (float)parseMath (chunks [7]) : .3f;
					t.molding (mtype, pt1, pt2, msegs, tension);
					break;
				case "turnl":
					if (chunks.Length <= 1) {
						Debug.Log ("mov needs two arguments: line ignored");
						continue;
					}
					val1 = (float)parseMath (chunks [1]);
					t.turnl (val1);
					break;
				case "turnr":
					if (chunks.Length <= 1) {
						Debug.Log ("mov needs two arguments: line ignored");
						continue;
					}
					val1 = (float)parseMath (chunks [1]);
					t.turnr (val1);
					break;
				}
				// process each line
			}

		}





















		// general purpose math parser that uses parameters managed by this object

		// PARSE_MATH
		public double parseMath(string expr) {

			AX.SoftCircuits.Eval eval = new AX.SoftCircuits.Eval();



			eval.ProcessSymbol 		+= new AX.SoftCircuits.Eval.ProcessSymbolHandler(processSymbol);
			eval.ProcessFunction 	+= new AX.SoftCircuits.Eval.ProcessFunctionHandler(processFunction);

			//Debug.Log ("EVAL " + expr);

			double retval = 0;

			try
			{
				retval = eval.Execute(expr);
			}
			catch (EvalException e) 
			{
				//Debug.Log(eval.ToString());
				Debug.Log (Name + " LOGIC CODE ERROR in [ " + expr + " ] => " + e.ToString());
			}
			return retval;

		}





		public MathParseResults parseMathWithResults(string expr) 
		{

			AX.SoftCircuits.Eval eval = new AX.SoftCircuits.Eval();

			//Debug.Log ("parseMath 1");

			eval.ProcessSymbol 		+= new AX.SoftCircuits.Eval.ProcessSymbolHandler(processSymbol);
			eval.ProcessFunction 	+= new AX.SoftCircuits.Eval.ProcessFunctionHandler(processFunction);

			//Debug.Log ("parseMath 2");
			//Debug.Log ("EVAL " + expr);


			MathParseResults mathParseResults = new MathParseResults();

			if (symbolsUsed == null)
				symbolsUsed = new Dictionary<Eval, List<string>>();



			symbolsUsed.Add(eval, new List<string>());

			try
			{
				mathParseResults.result = (float) eval.Execute(expr);
				mathParseResults.symbolsFound = symbolsUsed[eval];
				symbolsUsed.Remove(eval);

				return mathParseResults;
				//retval = eval.Execute(expr);
			}
			catch (InvalidCastException e) 
			{
				Debug.Log ("OUCH!!!! " + e.ToString());
			}
			//Debug.Log ("parseMath 3");
			return mathParseResults;

		}

		public void processFunction(object sender, AX.SoftCircuits.FunctionEventArgs e)
		{
			//Debug.Log ("processFunction:: "+e.Name + " " +e.Parameters[0]);// + " : " + e.Parameters[1]);

			// [Based on S.Darkwell's prompt, converted to switch statement and mapped more Mathf functions. 2016.06.01]
			switch(e.Name)
			{
			case "greater":

				if (e.Parameters.Count == 2)
					e.Result = e.Parameters[0] > e.Parameters[1] ?  e.Parameters[0] : e.Parameters[1];
				else
					e.Status = FunctionStatus.WrongParameterCount;

				break;
			case "lesser":

				if (e.Parameters.Count == 2)
					e.Result = e.Parameters[0] < e.Parameters[1] ?  e.Parameters[0] : e.Parameters[1];
				else
					e.Status = FunctionStatus.WrongParameterCount;
				break;

			case "range":

				if (e.Parameters.Count == 3)
					e.Result = Mathf.Clamp((float)e.Parameters[0], (float)e.Parameters[1], (float)e.Parameters[2]);
				else
					e.Status = FunctionStatus.WrongParameterCount;
				break;

			case "isEven":
			case "IsEven":
			case "iseven":
				if (e.Parameters.Count == 1)
				{
					e.Result =  ((int)e.Parameters[0] % 2 == 0) ? 1 : 0;
				}
				else
					e.Status = FunctionStatus.WrongParameterCount;

				
				break;


			case "modulo":

				//Debug.Log(" --> " + e.Parameters[0] + ", " +  e.Parameters[0]);
				if (e.Parameters.Count == 2)
					e.Result =  (int)e.Parameters[0] % (float) e.Parameters[1];
				else
					e.Status = FunctionStatus.WrongParameterCount;

				
				break;

			case "Epsilon":
			case "epsilon":

				e.Result = Mathf.Epsilon;

				break;

			case "PI":
			case "pi":

				e.Result = Mathf.PI;

				break;

			case "Abs":
			case "abs":

				if (e.Parameters.Count == 1)
					e.Result =  Mathf.Abs((float)e.Parameters[0]);
				else
					e.Status = FunctionStatus.WrongParameterCount;

				break;

			case "Acos":
			case "acos":

				if (e.Parameters.Count == 1)
				try{
					e.Result = Mathf.Acos( (float)e.Parameters[0]) * Mathf.Rad2Deg;
					} catch {
					Debug.Log("bad cos "+e.Parameters[0]);
					}
				else
					e.Status = FunctionStatus.WrongParameterCount;
				break;



			case "Asin":
			case "asin":

				if (e.Parameters.Count == 1)
					e.Result = Mathf.Asin( (float)e.Parameters[0]) * Mathf.Rad2Deg;
				else
					e.Status = FunctionStatus.WrongParameterCount;
				break;

			case "Atan":
			case "atan":

				//Debug.Log(" --> " + e.Parameters[0] + ", " +  e.Parameters[0]);
				if (e.Parameters.Count == 1)
					e.Result = Mathf.Rad2Deg * Mathf.Atan( (float)e.Parameters[0] );
				else
					e.Status = FunctionStatus.WrongParameterCount;
				break;

			case "Atan2":
			case "atan2":
			case "arctan":

				//Debug.Log(" --> " + e.Parameters[0] + ", " +  e.Parameters[0]);
				if (e.Parameters.Count == 2)
					e.Result = Mathf.Rad2Deg * Mathf.Atan2( (float)e.Parameters[0], (float) e.Parameters[1]);
				else
					e.Status = FunctionStatus.WrongParameterCount;
				break;


			case "Ceil":
			case "ceil":

				if (e.Parameters.Count == 1)
					e.Result = Mathf.Ceil( (float)e.Parameters[0] );
				else
					e.Status = FunctionStatus.WrongParameterCount;
				break;


			case "CeilToInt":
			case "ceilToInt":

				
				if (e.Parameters.Count == 1)
					e.Result = Mathf.CeilToInt( (float)e.Parameters[0] );
				else
					e.Status = FunctionStatus.WrongParameterCount;
				break;

			case "Clamp01":
			case "clamp01":

				if (e.Parameters.Count == 1)
					e.Result = Mathf.Clamp01( (float)e.Parameters[0] );
				else
					e.Status = FunctionStatus.WrongParameterCount;
				break;



			case "Cos":
			case "cos":


				if (e.Parameters.Count == 1)
				{
					
					if ( (Math.Abs((float)e.Parameters[0]-90) < .01f) ||  (Math.Abs((float)e.Parameters[0]-270) <.01f) )
						e.Result = 0;
					else 
						e.Result = Mathf.Cos( ((float)e.Parameters[0]) * Mathf.Deg2Rad);
				}
				else
				{
					e.Status = FunctionStatus.WrongParameterCount;
				}
				break;

			case "Exp":
			case "exp":

				if (e.Parameters.Count == 1)
					e.Result = Mathf.Exp( (float)e.Parameters[0] );
				else
					e.Status = FunctionStatus.WrongParameterCount;
				break;

			case "FloorToInt":
			case "floorToInt":

				if (e.Parameters.Count == 1)
					e.Result = Mathf.FloorToInt( (float)e.Parameters[0] );
				else
					e.Status = FunctionStatus.WrongParameterCount;
				break;

			case "Floor":
			case "floor":

				if (e.Parameters.Count == 1)
					e.Result = Mathf.Floor( (float)e.Parameters[0] );
				else
					e.Status = FunctionStatus.WrongParameterCount;
				break;

			case "Lerp":
			case "lerp":

				if (e.Parameters.Count == 3)
					e.Result = Mathf.Lerp( (float)e.Parameters[0], (float)e.Parameters[1], (float)e.Parameters[2] );
				else
					e.Status = FunctionStatus.WrongParameterCount;
				break;

			case "Max":
			case "max":

				if (e.Parameters.Count == 2)
					e.Result = Mathf.Max( (float)e.Parameters[0], (float) e.Parameters[1]);
				else
					e.Status = FunctionStatus.WrongParameterCount;
				break;

			case "Min":
			case "min":

				if (e.Parameters.Count == 2)
					e.Result = Mathf.Min( (float)e.Parameters[0], (float) e.Parameters[1]);
				else
					e.Status = FunctionStatus.WrongParameterCount;
				break;
			case "MoveTowards":
			case "moveTowards":

				if (e.Parameters.Count == 3)
					e.Result = Mathf.MoveTowards( (float)e.Parameters[0], (float)e.Parameters[1], (float)e.Parameters[2] );
				else
					e.Status = FunctionStatus.WrongParameterCount;
				break;

			case "MoveTowardsAngle":
			case "moveTowardsAngle":

				if (e.Parameters.Count == 3)
					e.Result = Mathf.MoveTowardsAngle( (float)e.Parameters[0], (float)e.Parameters[1], (float)e.Parameters[2] );
				else
					e.Status = FunctionStatus.WrongParameterCount;
				break;

			case "Pow":
			case "pow":

				if (e.Parameters.Count == 2)
					e.Result = Mathf.Pow( (float)e.Parameters[0], (float) e.Parameters[1]);
				else
					e.Status = FunctionStatus.WrongParameterCount;
				break;


			case "Repeat":
			case "repeat":

				if (e.Parameters.Count == 2)
					e.Result = Mathf.Repeat( (float)e.Parameters[0], (float) e.Parameters[1]);
				else
					e.Status = FunctionStatus.WrongParameterCount;
				break;

			case "Round":
			case "round":

				if (e.Parameters.Count == 1)
					e.Result = Mathf.Round( (float)e.Parameters[0]);
				else
					e.Status = FunctionStatus.WrongParameterCount;
				break;


			case "Sign":
			case "sign":

				if (e.Parameters.Count == 1)
					e.Result = Mathf.Sign( (float)e.Parameters[0]);
				else
					e.Status = FunctionStatus.WrongParameterCount;
				break;

			case "Sin":
			case "sin":

				if (e.Parameters.Count == 1)
				{
//					//Debug.Log( "Sin: " + (float)e.Parameters[0]);
					if ( Math.Abs((float)e.Parameters[0]-180) < .01f || Math.Abs((float)e.Parameters[0]-360) < .01)
						e.Result = 0;
					else 
						e.Result = Mathf.Sin( ((float)e.Parameters[0]) * Mathf.Deg2Rad);
				}
				else
					e.Status = FunctionStatus.WrongParameterCount;


				break;

			case "Sqrt":
			case "sqrt":

				if (e.Parameters.Count == 1)
					e.Result = Mathf.Sqrt( (float)e.Parameters[0] );
				else
					e.Status = FunctionStatus.WrongParameterCount;
				break;

			case "Tan":
			case "tan":

				if (e.Parameters.Count == 1)
					e.Result = Mathf.Tan( (float)e.Parameters[0] * Mathf.Deg2Rad );
				else
					e.Status = FunctionStatus.WrongParameterCount;
				break;


			case "int":

				if (e.Parameters.Count == 1)
					e.Result = Mathf.FloorToInt( (float)e.Parameters[0]);
				else
					e.Status = FunctionStatus.WrongParameterCount;
				break;

			} // \switch



		}



		private bool IsValidGUID(string GUIDCheck)
		{
			if (!string.IsNullOrEmpty(GUIDCheck))
			{
				return new Regex(@"^(\{{0,1}([0-9a-fA-F]){8}-([0-9a-fA-F]){4}-([0-9a-fA-F]){4}-([0-9a-fA-F]){4}-([0-9a-fA-F]){12}\}{0,1})$").IsMatch(GUIDCheck);
			}
			return false;
		}

		public void processSymbol(object sender, AX.SoftCircuits.SymbolEventArgs e)
		{

			AXParameter p = null;

			// see if the variable name is "_"+p.Guid+"_"



			string guidstr = ArchimatixUtils.keyToGuid(e.Name);




			if (e.Name.Length > 20 && IsValidGUID(guidstr))
			{
				// this symbol is a guid
				p = model.getParameterByGUID(guidstr);

			} 
			else 
			{
				// this symbol is a parameter.Name
				p = getParameter(e.Name);
			}



			bool variableDefined = false;

			if (p != null)
			{
				// use parameters validators here!

				variableDefined = true;

				// mark as used
				if (symbolsUsed != null && symbolsUsed.ContainsKey((AX.SoftCircuits.Eval) sender))
					symbolsUsed[(AX.SoftCircuits.Eval)sender].Add(e.Name);

				if (p.Type == AXParameter.DataType.Float)
					e.Result = p.FloatVal;
				else if (p.Type == AXParameter.DataType.Int)
					e.Result = p.IntVal;
			}
			else if (vars != null)
			{
				// vars - code defined parameters
				foreach(KeyValuePair<string, float> item in vars)
				{			
					if (item.Key == e.Name)
					{
						variableDefined = true;
						e.Result = item.Value;
					}
				}
			}

			if (! variableDefined)
			{
				//Debug.Log("Parameter \""+e.Name+"\" not defined. Created a new parameter for it.");
				e.Result = 0;

				//addParameter(AXParameter.DataType.Float, e.Name, .5f, 0, 10);
			}
			else 
			{
				//Debug.Log("sym result: " + e.Result);
			}

		}

		#endregion





	}
}
