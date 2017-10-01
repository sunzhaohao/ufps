using UnityEditor;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;

using System.Text;
using System.Text.RegularExpressions;


using AX.SimpleJSON;




using AXGeometryTools;

using AX;
using AX.Generators;
using AX.GeneratorHandlers;

using AXEditor;

using AXClipperLib;
using Path 	= System.Collections.Generic.List<AXClipperLib.IntPoint>;
using Paths 	= System.Collections.Generic.List<System.Collections.Generic.List<AXClipperLib.IntPoint>>;




[System.Serializable]
public class AXNodeGraphEditorWindow : EditorWindow {




	[MenuItem("Window/Archimatix/Graph Editor")]
	public static void Init() {
		AXNodeGraphEditorWindow edwin = (AXNodeGraphEditorWindow) EditorWindow.GetWindow(typeof(AXNodeGraphEditorWindow));
		edwin.titleContent = new GUIContent("Archimatix");
		edwin.autoRepaintOnSceneChange = true;
		edwin.setPositionIfNotDocked();
	}



	public static AXNodeGraphEditorWindow current { get; private set; }

	public AXModel model = null;

	// Support for dragging window space
	public Vector2 	editorWindowSpaceOffest;


	public float targetZoomScale;
	public bool  isZoomingScale;

	float headerHgt 		= 18;
	float footerHgt 		= 125;
	float rightSidebarWidth = 68;
	float leftSidebarWidth 	= 68;
	float leftSidebar2DHeightPercentage = .5f;
	Rect leftSidebarRect;
	Rect rightSidebarRect;


	Event relationEditorEvent;
	Rect relationEditorRect;


	public static string leftArrow  = "◀︎";
	public static string rightArrow = "▸";

	Vector2 origin;


	Rect 					graphRect;
	Vector2 				focusCenter;


	public static float 	maxZoom = 3.0f;

	public float 			zoomScale = 1f;
	public float 			zoomScalePrev = 1f;
	public static Vector2  	zoomPosAdjust;

	public Vector2 			panTo;
	public float 			panToSpeed = 1.5f;


	[System.NonSerialized]
	List<RelationBezierCurve> 	relationCurves;


	[System.NonSerialized]
	LibraryMenu libraryMenu2D;
	LibraryMenu libraryMenu3D;

	[System.NonSerialized]
	public static string 		lastFocusedGUIName;


	public SerializedObject 	serializedObject;
	public SerializedProperty 	parametricObjects_Property;

	public int 					isDraggingSelectedPoint 	= -1;

	[System.NonSerialized]
	public AXParameter 			OutputParameterBeingDragged = null;

	[System.NonSerialized]
	public AXParameter 			InputParameterBeingDragged = null;

	[System.NonSerialized]
	public AXParametricObject 	DraggingParameticObject = null;


	[System.NonSerialized]
	public AXParametricObject 	OverDropZoneOfPO = null;


	// RUBBERBAND 
	[System.NonSerialized]
	public List<AXParametricObject> tmpRubberbandSelectedPOs = null;



	public float testval = 0;

	private static bool _doAutobuild;

	public bool 		useInputRelations = false;

	private AXModel 	modelToSelect = null;

	public Rect 		last;

	public Rect 		relationEditWindowRect;


	public bool codeChanged = false;

	public delegate void MenuAction(string a);

	public string 		showMenu = "";
	public int 			menuWidth = 250;
	public Rect 		menuRect = new Rect(0, 0, 250, 300);


	public enum EditorState {Default, DraggingWindowSpace, DragResizingLogicWindow, DragResizingNodePalleteWindow, DraggingInputParameter, DraggingOutputParameter, AddPoint, DraggingRubbeband, DraggingNodePalette, ZoomingWindowSpace};

	public EditorState editorState;

	public Vector2 		mouseDownPoint;
	public Vector2 		dragStart;

	public bool 		mouseJustDown = false;
	public bool 		mouseHasBeenDragged = false;
	public long 		mouseDownTime;


	[System.NonSerialized]
	AXParametricObject 	mouseIsDownOnPO = null;

	[System.NonSerialized]
	AXParametricObject 	mouseLastDownOnPO = null;

	[System.NonSerialized]
	AXParametricObject 	draggingThumbnailOfPO = null;




	bool mouseIsDown = false;

	public Rect 		mostRecentLogicRect;
	public Rect 		mostRecentLogicRectLocal;

	public Rect 		mostRecentThumbnailRect;


	public Texture2D 	orbitIconTex; 
	public Texture2D 	dollyIconTex; 

	public Texture2D 	dropZoneTex;
	public Texture2D 	dropZoneOverTex;

	//Vector2 mouseDownRectPosition;





	private bool 		doRepaint = false;

	int relationsDrawnAtLevel;

	Texture2D menubarTexture;
	Texture2D menuIconTexture;
	Texture2D infoIconTexture;
	Texture2D resizeCornerTexture;
	public Texture2D closeIconTexture;

	public Texture2D solidIconTexture;
	public Texture2D voidIconTexture;
	Texture2D solidVoidIconTexture;

	public Texture2D shapeOpenIconTexture;
	public Texture2D shapeClosedIconTexture;



	Texture2D verticalShadowLeftTexture;
	Texture2D verticalShadowRightTexture;

	Texture2D verticalShadowLeftTexturePro;
	Texture2D verticalShadowRightTexturePro;

	// Bottom bar MenuIcons
	Texture2D CloseAllControlsIcon;
	Texture2D CloseAllToolsIcon;
	Texture2D ShowAllNodesIcon;


	public Texture2D prefabWindowBackground;

	public Texture[] prefabWindowFrames;


	public Dictionary<string, Texture2D> nodeIconTextures;




	Color gridColor;
	Color axisColor;
	Color curveShadowColor;
	public Color splineColor;




	int currentWindowOffset = 0;


	public string organizersMenuString = "";
	public string     shapesMenuString = "";
	public string  	 meshMenuString = "";
	public string  repeatersMenuString = "";




	[System.NonSerialized]
	public StopWatch commandStopWatch;


	// LIBRARY
	[System.NonSerialized]
	public AX.Library library;

	private Vector2 libraryScrollPosition;
	public bool editingLibrary = false;



	public bool test;

	public static bool IsOpen {
		get { return current != null; }
	}




	public static void repaintIfOpen()
	{

		if (AXNodeGraphEditorWindow.current != null)
			AXNodeGraphEditorWindow.current.Repaint();
	}

	public static void zoomToRectIfOpen(Rect rect)
	{

		if (AXNodeGraphEditorWindow.current != null)
			AXNodeGraphEditorWindow.current.zoomToRect(rect);
	}

	public static void displayGroupIfOpen(AXParametricObject po)
	{


		if (AXNodeGraphEditorWindow.current != null)
		{
			AXNodeGraphEditorWindow.current.openAsCurrentWorkingGroup(po);

		}

	}





	public void OnInspectorUpdate() {

		//if(Event.current.type == EventType.Repaint)
		Repaint();
	}



	// GUI COLORS BASED ON DATATYPE
	public Color getDataColor(AXParameter.DataType t) {

		Color value;
		return ArchimatixEngine.AXGUIColors.TryGetValue(t.ToString(), out value) ? value : ArchimatixEngine.defaultDataColor;
	}








	public void OnEnable()
	{
	 

		current = this;

		ArchimatixEngine.establishPaths();

//		if (ArchimatixEngine.library == null)
//			ArchimatixEngine.createLibrary();

		if (ArchimatixEngine.nodeIcons == null)
			AXEditorUtilities.loadNodeIcons();

		// catch scene events. See: http://answers.unity3d.com/questions/489738/get-mouse-events-with-editorwindow.html

		//SceneView.onSceneGUIDelegate += SceneGUI;

		useInputRelations = false;

		 

		libraryMenu2D = new LibraryMenu();

		libraryMenu3D = new LibraryMenu();
		 

		if (model == null)
		{
			// see if there is one in the scene
			// first try current selection


			if (model == null)
			{
				displayAnyAXModelInScene();
			}

		} 



		if (model != null)
		{
			serializedObject = new SerializedObject(model);

		}
		mouseIsDownOnPO = null;
		draggingThumbnailOfPO = null;

		InputParameterBeingDragged = null;
		OutputParameterBeingDragged = null;
		 


		//  UI IMAGES 

		closeIconTexture 			= (Texture2D) AssetDatabase.LoadAssetAtPath(ArchimatixEngine.ArchimatixAssetPath+"/ui/GeneralIcons/zz-AXIcons-Close.png", typeof(Texture2D));		


		solidIconTexture 			= (Texture2D) AssetDatabase.LoadAssetAtPath(ArchimatixEngine.ArchimatixAssetPath+"/ui/GeneralIcons/zz-AXIcons-SolidIcon.png", typeof(Texture2D));		
		voidIconTexture 			= (Texture2D) AssetDatabase.LoadAssetAtPath(ArchimatixEngine.ArchimatixAssetPath+"/ui/GeneralIcons/zz-AXIcons-VoidIcon.png", typeof(Texture2D));		


		shapeClosedIconTexture 		= (Texture2D) AssetDatabase.LoadAssetAtPath(ArchimatixEngine.ArchimatixAssetPath+"/ui/GeneralIcons/zz-AXIcons-ShapeClosed.png", typeof(Texture2D));		
		shapeOpenIconTexture 		= (Texture2D) AssetDatabase.LoadAssetAtPath(ArchimatixEngine.ArchimatixAssetPath+"/ui/GeneralIcons/zz-AXIcons-ShapeOpen.png", typeof(Texture2D));		



		if (EditorGUIUtility.isProSkin)
		{
			menubarTexture 			= (Texture2D) AssetDatabase.LoadAssetAtPath(ArchimatixEngine.ArchimatixAssetPath+"/ui/GeneralIcons/zz-AXIcons-MenubarDark.png", typeof(Texture2D));		
			resizeCornerTexture 	= (Texture2D) AssetDatabase.LoadAssetAtPath(ArchimatixEngine.ArchimatixAssetPath+"/ui/GeneralIcons/zz-AXIcons-ResizeCornerDark.png", typeof(Texture2D));		
			menuIconTexture 		= (Texture2D) AssetDatabase.LoadAssetAtPath(ArchimatixEngine.ArchimatixAssetPath+"/ui/GeneralIcons/zz-AXIcons-MenuIconDark.png", typeof(Texture2D));	

		}
		else
		{
			menubarTexture 			= (Texture2D) AssetDatabase.LoadAssetAtPath(ArchimatixEngine.ArchimatixAssetPath+"/ui/GeneralIcons/zz-AXIcons-MenubarLight.png", typeof(Texture2D));		
			resizeCornerTexture 	= (Texture2D) AssetDatabase.LoadAssetAtPath(ArchimatixEngine.ArchimatixAssetPath+"/ui/GeneralIcons/zz-AXIcons-ResizeCornerLight.png", typeof(Texture2D));		
			menuIconTexture 		= (Texture2D) AssetDatabase.LoadAssetAtPath(ArchimatixEngine.ArchimatixAssetPath+"/ui/GeneralIcons/zz-AXIcons-MenuIconLight.png", typeof(Texture2D));	

		}

		infoIconTexture				= (Texture2D) AssetDatabase.LoadAssetAtPath(ArchimatixEngine.ArchimatixAssetPath+"/ui/GeneralIcons/zz-AXMenuIcons-InfoIcon.png", typeof(Texture2D));

		orbitIconTex 				= (Texture2D) AssetDatabase.LoadAssetAtPath(ArchimatixEngine.ArchimatixAssetPath+"/ui/GeneralIcons/zz-AXIcons-OrbitIcon.png", typeof(Texture2D));
		dollyIconTex 				= (Texture2D) AssetDatabase.LoadAssetAtPath(ArchimatixEngine.ArchimatixAssetPath+"/ui/GeneralIcons/zz-AXIcons-DollyIcon.png", typeof(Texture2D));

		dropZoneTex 				= (Texture2D) AssetDatabase.LoadAssetAtPath(ArchimatixEngine.ArchimatixAssetPath+"/ui/GeneralIcons/zz-AXIcons-DropZone.png", typeof(Texture2D));
		dropZoneOverTex 				= (Texture2D) AssetDatabase.LoadAssetAtPath(ArchimatixEngine.ArchimatixAssetPath+"/ui/GeneralIcons/zz-AXIcons-DropZoneOver.png", typeof(Texture2D));


		verticalShadowLeftTexture 	= (Texture2D) AssetDatabase.LoadAssetAtPath(ArchimatixEngine.ArchimatixAssetPath+"/ui/GeneralIcons/zz-AXIcons-DropShadowVerticalLeft.png", typeof(Texture2D));
		verticalShadowRightTexture 	= (Texture2D) AssetDatabase.LoadAssetAtPath(ArchimatixEngine.ArchimatixAssetPath+"/ui/GeneralIcons/zz-AXIcons-DropShadowVerticalRight.png", typeof(Texture2D));

		CloseAllControlsIcon 			= (Texture2D) AssetDatabase.LoadAssetAtPath(ArchimatixEngine.ArchimatixAssetPath+"/ui/MenuIcons/zz-AXMenuIcons-CloseAllControls.png", typeof(Texture2D));
		CloseAllToolsIcon 				= (Texture2D) AssetDatabase.LoadAssetAtPath(ArchimatixEngine.ArchimatixAssetPath+"/ui/MenuIcons/zz-AXMenuIcons-CloseAllTools.png", typeof(Texture2D));
		ShowAllNodesIcon 				= (Texture2D) AssetDatabase.LoadAssetAtPath(ArchimatixEngine.ArchimatixAssetPath+"/ui/MenuIcons/zz-AXMenuIcons-ShowAllNodes.png", typeof(Texture2D));

		Thumbnail.defaultCubeMap = (Cubemap) AssetDatabase.LoadAssetAtPath(ArchimatixEngine.ArchimatixAssetPath+"/Materials/DefaultCubeMap.cubemap", typeof(Cubemap));





		/*
		prefabWindowBackground = (Texture2D) AssetDatabase.LoadAssetAtPath(ArchimatixEngine.ArchimatixAssetPath+"/ui/PrefabIcon/zz-AXPrefabIcon_DarkBackground.jpg", typeof(Texture2D));

		prefabWindowFrames = new Texture[9];
		for (int i=0;i<9; i++)
			prefabWindowFrames[i] = (Texture2D) AssetDatabase.LoadAssetAtPath(ArchimatixEngine.ArchimatixAssetPath+"/ui/PrefabIcon/zz-AXPrefabIcon_"+i+".png", typeof(Texture2D));

			*/










		Repaint ();  
	}

	public void refreshAllGraphs() 
	{ 
		AXModel[] mos =  FindObjectsOfType(typeof(AXModel)) as AXModel[];

		for (int i = 0; i < mos.Length; i++) {
			AXModel mo = mos [i];
			mo.cleanGraph ();
		}

	} 

	void OnSelectionChange() 
	{
		//Debug.Log("SELECTION CHANGED...");

		//setEditMode( EditorState.Default );

		//displayactiveSceneObject();

		//cacheThumbnailsDelayed();

	}


	void displayAXModel(AXModel mod) 
	{
		model = mod;

		if (model != null)
			serializedObject = new SerializedObject(model);

	}




	void displayAnyAXModelInScene() 
	{
		AXModel[] axModels =  FindObjectsOfType(typeof(AXModel)) as AXModel[];

		if (axModels != null && axModels.Length > 0)
			displayAXModel(axModels[0]);

	}



	public void closeAllControls()
	{

		//Debug.Log("closing all controls");
		foreach (AXParametricObject po in model.parametricObjects)
			po.closeAllParameterSets();
	}



	public void closeTools()
	{

		//Debug.Log("closing all tools");
		foreach (AXParametricObject po in model.parametricObjects)
			if (po.generator is AXTool ) po.isOpen = false;
	}



	public AXParametricObject poFactory_create(string po_type)
	{
		//closeAllControls();
		Debug.Log ("poFactory_create");
		AXParametricObject npo = model.addParametricObject(po_type);
		 
		model.autobuild();

		AXParametricObject mostRecentPO = model.mostRecentlySelectedPO;



		//int pos_x =  position.width/2 + Random.Range(-200, 200) -75;

		float pos_x = position.width/2-50;
		float pos_y = position.height/2-100;

		if(mostRecentPO != null)
		{
			//Debug.Log (mostRecentPO.Name + ": " +mostRecentPO.rect);

			pos_x =  mostRecentPO.rect.x + 200;
			pos_y =  mostRecentPO.rect.y;



		}
		npo.rect = new Rect(pos_x, pos_y, npo.generator.minNodePaletteWidth-2, 100);

		//Debug.Log (npo.rect);
		// if npo.rect is offscreen, slide to it
		// ...

		model.selectedPOs.Clear();
		selectPO(npo);



		npo.showHandles = false;
		npo.showControls = false;
		npo.showLogic = false;

		//Tools.current = Tool.None;

		//serializedObject.Update();


		EditorUtility.SetDirty(model.gameObject);

		return npo;
	}



	public void doAutobuild()
	{
		_doAutobuild = true;

	}

	void Update()
	{

		if (modelToSelect != null)
		{
			//Selection.activeGameObject = modelToSelect.gameObject;
			ArchimatixEngine.currentModel = modelToSelect; 

			modelToSelect = null;
		}

		if (ArchimatixEngine.currentModel != null)
			model = ArchimatixEngine.currentModel;



		if (model != null && (model.isPanningToPoint || isZoomingScale))
		{


			if (isZoomingScale)
			{

				zoomScale 		= model.zoomScale;

				zoomScale 		= Mathf.Lerp(zoomScale, targetZoomScale, panToSpeed * Time.deltaTime);

				if (Mathf.Abs(zoomScale-targetZoomScale) < .1f)
					isZoomingScale = false;
				model.zoomScale = zoomScale;
			}

			focusCenter = Vector2.Lerp(focusCenter, model.panToPoint, 1.8f*Time.deltaTime);

			if ( (focusCenter - model.panToPoint).magnitude < 150f)
				model.endPanningToPoint();

			model.focusPointInGraphEditor = focusCenter;

			zoomPosAdjust =  (graphRect.center-origin)/zoomScale - focusCenter;

			//	if (Event.current.type == EventType.Repaint)
			//	Repaint();


		}




	}

	public void zoomToRect(Rect rect)
	{


		float padding = 600; 

		targetZoomScale = (rect.width > rect.height) ? graphRect.width/(rect.width+padding) : graphRect.width/(rect.height+padding);
		isZoomingScale = true;



		if (model != null)
			model.beginPanningToPoint(rect.center);
	}









	void onMouseDown()
	{
		//Debug.Log("MOUSE DOWN");
		//clearFocus();
		mouseIsDown = true;


		// require that axmodel is selected in the scene view

		if (model != null)
		{

			Selection.activeGameObject = model.gameObject;
			ArchimatixEngine.currentModel = model; 


			//model.setRenderMode( AXModel.RenderMode.DrawMesh);


		}

	}

	public bool textAreaIsInFocus()
	{
		string focusedName = GUI.GetNameOfFocusedControl();

		return (focusedName != null && focusedName.Contains("logicTextArea"));

	}


	// Snapping
	public static float gridSize = 25;


	public static float snapValue(float dec) {

		float inc = gridSize/2;


		//Debug.Log(ArchimatixEngine.currentModel.zoomScale);
		if (ArchimatixEngine.currentModel != null && ArchimatixEngine.currentModel.zoomScale < .8f)

			inc = gridSize;
		
		float mod = (Mathf.Abs(dec) % inc);

		if (dec < 0)
		{
			return dec + mod - (Mathf.Abs(mod) > inc/2 ? inc : 0);
		}
		return dec - mod + (mod > inc/2 ? Math.Abs(inc) : 0);
	}

	public static void snapPosition(ref Rect rect)
	{

		rect.x = 1+snapValue(rect.x);
		rect.y = 1+snapValue(rect.y);
	}



	void onMouseUp()
	{
		// assuming some editing task just completed, 
		// what needs to be done?


		if (editorState == EditorState.DraggingNodePalette && OverDropZoneOfPO != null)
		{
			//Debug.Log("DROP " + model.name);
			Undo.RegisterCompleteObjectUndo (model, "Drop on Grouper");

			OverDropZoneOfPO.addGroupees(model.selectedPOs);
		}

		if (mouseIsDownOnPO != null)
		{
			mouseIsDownOnPO.rect.width = -2 + snapValue(mouseIsDownOnPO.rect.width);

		}

		mouseIsDown = false;



		/*
		Debug.Log("UP "+mouseIsDownOnPO + " :: " + editorState + " :: " + (mouseDownPoint) + " ::: " + (Event.current.mousePosition+zoomPosAdjust));
		if (mouseIsDownOnPO == null && editorState == EditorState.DraggingRubbeband && ! mouseHasBeenDragged)
		{
			model.deselectAll();
		}
		Debug.Log("yup");
		*/

		editorState = EditorState.Default;


		if (mouseIsDownOnPO != null)
		{
			snapPosition(ref mouseIsDownOnPO.rect);
			//foreach(AXParametricObject pom in model.selectedPOs)
				//snapPosition(ref pom.rect);



			if (mouseIsDownOnPO == model.currentWorkingGroupPO && mouseIsDownOnPO.Groupees != null)
				for (int g=0; g<mouseIsDownOnPO.Groupees.Count; g++)
					snapPosition(ref mouseIsDownOnPO.Groupees[g].rect);

			else
				foreach(AXParametricObject pom in model.selectedPOs)
					if (pom != mouseIsDownOnPO)
						snapPosition(ref pom.rect);
					

			

			// IS THIS PO OVER A GROUPER DROP ZONE?
			if (mouseIsDownOnPO.grouper != null && model.currentWorkingGroupPO != null &&  (mouseIsDownOnPO.rect.x < model.currentWorkingGroupPO.rect.x))
			{
				// POP SELECTED NODES FROM GROUP

				// Use selected nodes and any of their hidden subnodes.
				Undo.RegisterCompleteObjectUndo (model, "Pop Groupees");
				mouseIsDownOnPO.popPOsFromGroup(model.selectedPOs);

			}
		}




		mouseHasBeenDragged = false;
		mouseIsDownOnPO = null;
		draggingThumbnailOfPO = null;




		// if pro version
		if (model != null)
		{
			model.isGenerating = false;


			model.renderMode = AXModel.RenderMode.GameObjects;

			if (model.buildStatus == AXModel.BuildStatus.Generated)
				model.build();


			if (model.renderMode == AXModel.RenderMode.DrawMesh)
			{
				//Debug.Log ("mouseup: GENERATE MODELS");


				if (model.buildStatus == AXModel.BuildStatus.Generated)
					model.isAltered(33);


				//Debug.Log ("cacheThumbnails F");
				model.cacheThumbnails("AXModelEditowWindow::onMouseUp()");


				//model.setRenderMode( AXModel.RenderMode.GameObjects );
				//model.setRenderMode( AXModel.RenderMode.DrawMesh );
			}




		}


	}


	public void setPositionIfNotDocked()
	{
		// Based on solution here: http://answers.unity3d.com/questions/62594/is-there-an-editorwindow-is-docked-value.html
		BindingFlags fullBinding = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

		MethodInfo isDockedMethod = typeof( EditorWindow ).GetProperty( "docked", fullBinding ).GetGetMethod( true );

		if ( ( bool ) isDockedMethod.Invoke(this, null) == false ) // if not docked
		{
			float windowWidth 	= 800;
			float windowHeight 	= 600;

			position = new Rect( 300, 300, windowWidth, windowHeight );
		}

	}



	public void setEditMode(EditorState eState)
	{
		//Debug.Log ("Setting edit mode to " + eState);
		editorState = eState;
	}







	// DEPRICATED
	void SceneGUI(SceneView sceneView)
	{
		
		Event e = Event.current;

		if (e.type != EventType.layout && e.type != EventType.repaint && e.type != EventType.mouseMove)
			Debug.Log("sceneGUI 2: " + e.type);

		// This is all in Archimatix Engine now....

	}



	public bool anyCornerIsInside(Rect a, Rect b)
	{
		float minX = (b.width < 0) ? (b.x+b.width) : b.x;
		float maxX = (b.width > 0) ? (b.x+b.width) : b.x;

		float minY = (b.height < 0) ? (b.y+b.height) : b.y;
		float maxY = (b.height > 0) ? (b.y+b.height) : b.y;





		// point 1
		if ( (a.x > minX && a.x < maxX) && (a.y > minY && a.y < maxY) )
			return true;

		if ( (a.x > minX && a.x < maxX) && ((a.y+a.height) > minY && (a.y+a.height)< maxY) )
			return true;

		if ( ((a.x+a.width) > minX && (a.x+a.width) < maxX) && ((a.y+a.height) > minY && (a.y+a.height)< maxY) )
			return true;

		if ( ((a.x+a.width) > minX && (a.x+a.width) < maxX) && (a.y > minY && a.y< maxY) )
			return true;



		return false;
	}





	public  void closeCurrentWorkingGroup()
	{
		if (model == null || model.currentWorkingGroupPO == null)
			return;

		// -- GO BACK UP ONE LEVEL OR TO MODEL
		if (model.currentWorkingGroupPO.grouper != null)
		{	
			// View Parent Group
			model.currentWorkingGroupPO = model.currentWorkingGroupPO.grouper;

			Rect r = AXUtilities.getBoundaryRectFromPOs(model.currentWorkingGroupPO.Groupees);
			r.x -= model.currentWorkingGroupPO.rect.width/2;
			zoomToRect(r);

		}
		// -- GO TO MODEL
		else
		{	
			// Veiw Mode, focus on the grouper just closed

			//model.beginPanningToPoint(model.currentWorkingGroupPO.rect.center);

			zoomToRect(model.currentWorkingGroupPO.rect);

			model.currentWorkingGroupPO = null;
		}
	}


	// OPEN GROUP
	public  void openAsCurrentWorkingGroup(AXParametricObject po)
	{
		//if (po == null)
		//	po = mouseLastDownOnPO;
		if (model != null)
		{
			model.currentWorkingGroupPO = po;



			if (model.currentWorkingGroupPO != null)
			{
				if (model.currentWorkingGroupPO.Groupees != null)
					AXUtilities.movePOsToRightOfPO(model.currentWorkingGroupPO, model.currentWorkingGroupPO.Groupees);

					//Rect r = AXUtilities.getBoundaryRectFromPOs(model.currentWorkingGroupPO.Groupees);
					//r.x -= model.currentWorkingGroupPO.rect.width/2;

				Rect grouperCanvasRect = model.currentWorkingGroupPO.getBoundsRect();
				zoomToRect(grouperCanvasRect);

			}
		}

	}






	#region Draw NODE GRAPH 
	public void drawNodeGraphWindow(int id)
	{


		// PALETTE WINDOWS

		Event e = Event.current;


		if (model != null && float.IsNaN(model.focusPointInGraphEditor.x))
			model.focusPointInGraphEditor = Vector2.zero;


		//Scale my gui matrix
		if (model != null)
		{
			focusCenter = model.focusPointInGraphEditor;
			zoomScale = model.zoomScale;
		}

		//Rect screenViewRect = ; //new Rect(0, 18, graphRect.width, graphRect.height);


		// BEGIN SCALED SPACE **
		EditorZoomArea.Begin(zoomScale, graphRect, graphRect.center);



		// Draw grid each frame
		gridColor.a = zoomScale/2.5f;
		axisColor.a = zoomScale/1.5f;




		GUIDrawing.DrawGrid(zoomPosAdjust, gridColor, axisColor);


		// CACHE RELATION CURVES TO CHECK DISTANCE ON CLICK
		//relationCurves = new List<RelationBezierCurve>();






		Rect rubberRect = new Rect(mouseDownPoint.x, mouseDownPoint.y, e.mousePosition.x-mouseDownPoint.x, e.mousePosition.y-mouseDownPoint.y);



		if(model != null) 
		{


			// RUBBER BAND SELECTION
			if (editorState == EditorState.DraggingRubbeband)
			{
				//model.deselectAll();


				for (int i=0; i<model.parametricObjects.Count; i++)
				{
					

					AXParametricObject po = model.parametricObjects[i];

					//Debug.Log(po.Name + " : " + po.isOpen);
					if (! po.isOpen || po.grouper != model.currentWorkingGroupPO )
						continue;
					
					Rect scaledRect = new Rect((zoomPosAdjust.x+po.rect.x), (zoomPosAdjust.y+po.rect.y), po.rect.width, po.rect.height);

					if (rubberRect.width < 0)
					{
						rubberRect.width = Mathf.Abs(rubberRect.width);
						rubberRect.x -= rubberRect.width;
					}
					if (rubberRect.height < 0)
					{
						rubberRect.height = Mathf.Abs(rubberRect.height);
						rubberRect.y -= rubberRect.height;
					}

					//if ( anyCornerIsInside(scaledRect, rubberRect) || anyCornerIsInside(rubberRect, scaledRect))
					if ( rubberRect.Overlaps(scaledRect))
					{
						model.selectPO(po);
						tmpRubberbandSelectedPOs.Add(po);

						EditorUtility.SetDirty(model.gameObject);
					} 
					else 
					{
						if (tmpRubberbandSelectedPOs.Contains(po))
						{
							model.deselectPO(po);
							tmpRubberbandSelectedPOs.Remove(po);
						}
					}
				}


			}




			// GROUP BACKGROUND

			// If WorkingGrouper, 
			// draw backround field
			float grouperBG_Margin = 200;
			if (model.currentWorkingGroupPO != null)
			{
				AXParametricObject groupPO = model.currentWorkingGroupPO;

				Rect grouperCanvasRect = groupPO.getBoundsRect();

				grouperCanvasRect.x += zoomPosAdjust.x;
				grouperCanvasRect.y += zoomPosAdjust.y;


				Handles.color = Color.white;
				Handles.DrawSolidRectangleWithOutline(grouperCanvasRect, new Color(1, 1, 1, .08f), Color.cyan);



				// GROUP CLOSE BUTTON
				if(GUI.Button ( new Rect( (grouperCanvasRect.x+grouperCanvasRect.width-44), (grouperCanvasRect.y+12), 32, 32), new GUIContent(closeIconTexture, "Close Group"), GUIStyle.none))		
				{
					closeCurrentWorkingGroup();
				}



				if (groupPO.Groupees == null || groupPO.Groupees.Count == 0)
				{
					float labeWidth = 400;
					GUI.Label(new Rect(grouperCanvasRect.x + grouperCanvasRect.width/2-labeWidth/2, grouperCanvasRect.y + grouperCanvasRect.height/2-labeWidth/2, labeWidth, labeWidth), "Empty Group");

				}

			}






			if (e.type == EventType.Repaint)
			{
				//Debug.Log("NEW RELATIONS");
				relationCurves = new List<RelationBezierCurve>();
			}



			// Draw curves

			if (e.type == EventType.Repaint || e.type == EventType.mouseDown || e.type == EventType.mouseUp)
			{
				drawRelations(0);
				drawNodeRelations(0);
			}
			currentWindowOffset = 0;


			//Debug.Log ("model.parametricObjects.Count="+model.parametricObjects.Count);
			BeginWindows();
			int index = 0;


			// DROP ZONE
			//if (OverDropZoneOfPO == po)
			//{
			//	Debug.Log("NULLER");
			if (e.type == EventType.Repaint)
				OverDropZoneOfPO = null;
			//}


			// EACH PARAMETRIC_OBJECT
			// Look at all the PO's in this model, and see which are to be displayed given the currentWorkingGrouper
			for(index = 0; index<model.parametricObjects.Count; index++) 
			{
				//if (gov++ > 125)
				//	break; 


				AXParametricObject po = model.parametricObjects[index];

	
				// Only display nodes that are in the current working grouper. 
				if (po.grouper != model.currentWorkingGroupPO && po != model.currentWorkingGroupPO)
					continue;

				// ... and it must be open
				if(! po.isOpen)
					continue;

				// add selected drag diff

				//if (colors != null && colors.ContainsKey(po.type)) {
				//	GUI.color = colors[po.type];
				//}


				// 1. DRAW WINDOW


				if (po.rect.height == 0) 
					po.rect.height = 200;

				Color cc = GUI.color;

				if (po.generator != null)
				{
					if (EditorGUIUtility.isProSkin)
						GUI.color = po.generator.GUIColorPro;
					else
						GUI.color = po.generator.GUIColor;

					if (isSelected(po)) 
						GUI.color = Color.Lerp (GUI.color, Color.white, .7f);//backplateColorSelected;




					po.guiWindowId = index+currentWindowOffset;



					Rect po_rectPrev = po.rect;
					Rect tmpR = new Rect(zoomPosAdjust.x+po.rect.x, zoomPosAdjust.y+po.rect.y, po.rect.width, po.rect.height);



					Color transWhite = Color.white;

					// NODE PALETTE BACKGROUND COLOR

					if (model.isSelected(po))
						transWhite = ArchimatixEngine.AXGUIColors["NodePaletteHighlight"];
					else
						transWhite = ArchimatixEngine.AXGUIColors["NodePaletteBG"];

					transWhite.a = .9f;

					// Set the color of the window background
					GUI.color = transWhite;


					// ** DRAW THE WINDOW ** HOOOG
					if ( (model != null && model.selectedRelationInGraph == null) || (e.type == EventType.repaint) )
						tmpR = GUI.Window(po.guiWindowId, tmpR, DoNodePallette, po.Type);

					//tmpR.x = 10*Mathf.Round(tmpR.x/10);
					//tmpR.y = 10*Mathf.Round(tmpR.y/10);


					po.rect = new Rect(tmpR.x-zoomPosAdjust.x, tmpR.y-zoomPosAdjust.y, tmpR.width, tmpR.height);




					// MOVING MULTIPLE WINDOWS
					// if this window moved, move any other selected windows or grouped the same amount..

					if (po.rect.position != po_rectPrev.position)
					{



						if (po.grouper != null && po.grouper == model.currentWorkingGroupPO)
						{
							// test if trying to go left of grouper
							float groupees_minx = AXUtilities.getMinXFromPOs(model.selectedPOs);

							float minx = model.currentWorkingGroupPO.rect.x +  model.currentWorkingGroupPO.rect.width/2  + grouperBG_Margin;

							//Debug.Log("groupees_minx="+groupees_minx+", minx="+minx);
							if (groupees_minx < minx)
							{
								//Debug.Log("boarder crossed "+(po.rect.x-groupees_minx));
								//po.rect.x = minx + (Mathf.Abs(po.rect.x)-Mathf.Abs(groupees_minx));
								//po.rect.x = minx + (Mathf.Abs(po.rect.x)-Mathf.Abs(groupees_minx));
							}
						} 

						Vector2 displ = po.rect.position-po_rectPrev.position;

						//Debug.Log(model.currentWorkingGroupPO);

						// MOVE GROUPEES WITH GROUPER
						if (po == model.currentWorkingGroupPO && po.Groupees != null)
						{ 
							//Debug.Log(model.currentWorkingGroupPO.Name + " ......................... " + model.currentWorkingGroupPO.Groupees.Count);

							for (int g=0; g<po.Groupees.Count; g++)
							{

								//if (! model.selectedPOs.Contains(po.Groupees[g]))
								po.Groupees[g].rect.position += displ;

								//snapPosition(ref po.Groupees[g].rect);

							}

						}
						else
						{
							foreach(AXParametricObject pom in model.selectedPOs)
								if (pom != po)
								{
									pom.rect.position += displ;

									//snapPosition(ref pom.rect);
								}

						}

					}


					if (po.focusMe)
					{
						po.focusMe = false;

						GUI.FocusWindow(po.guiWindowId);
					}
				}
				GUI.color = cc;
			}



			// RELATION EDITOR AS GUI.Window 
			Matrix4x4 pmatrix = GUI.matrix;
			GUI.matrix = Matrix4x4.identity;

			//Vector2 globalPoint = (localPoint-model.focusPointInGraphEditor) * model.zoomScale + graphRect.center;

			 

			// THIS IS A BACKGROUND GUI WINDOW TO BLOCK MOUSECLICKS FROM GOING LOWER HOOOG
			// Here, the rectangle is adjusted to global coordinates of the node graph window
//			Rect winRect = new Rect(((graphRect.center-origin)/model.zoomScale).x-graphRect.width/2, ((graphRect.center-origin)/model.zoomScale).y+graphRect.height/2-120, graphRect.width, 100);

//			if (model != null && model.selectedRelationInGraph != null)
//			{
//				GUI.Window(3333, winRect, doRelationGUI, "Edit Relation");
//			}
			GUI.matrix = pmatrix;



			// Draw curves

			if (e.type == EventType.Repaint)
			{
				drawRelations(1);
				drawNodeRelations(1);
			}



			//StopWatch swatch = new StopWatch();
			EndWindows();
			//Debug.Log ("EditorWindow::EndWindows ["+model.parametricObjects.Count+"]: " + swatch.time().ToString());


			// IF A MOUSEDOWN EVENT HAS NOT BEEN USED YET< THEN THERE WAS NOT A CLICK ON A NODE
			if(e.type == EventType.MouseDown)
			{ 
				
				// Click event off PO
				// If after all the node windows were drawn and there is 
				// still an unuse mouseDown, then it must be off a PO.

				//Debug.Log ("CLICK OFF PO");



				if (model.selectedRelationInGraph != null && relationEditorRect.Contains(getUnzoomedPoint(e.mousePosition)))
				{
					// do nothing the relation editing panel window was hit
				}
				else
				 	ClickOffPOs();
			}

			else if (e.type == EventType.ContextClick)
			{
				//Debug.Log ("CONTEXT OFF PO");

				/*EditorUtility.DisplayPopupMenu (new Rect (e.mousePosition.x,e.mousePosition.y,0,0), "GameObject/3D Object/Archimatix Nodes", null);
				e.Use();
				*/
			}




		}

		//GUI.EndScrollView();
		//GUILayout.EndArea();






		// INERACTIVE CURVES 
		if (model != null)
		{
			// STATS
			Color shadowColor = Color.black;

			if (InputParameterBeingDragged != null)
			{
				GUIDrawing.DrawCurves(Event.current.mousePosition, InputParameterBeingDragged.inputPoint+zoomPosAdjust, getDataColor(InputParameterBeingDragged.Type), shadowColor);
				doRepaint = true;
			}
			else if (OutputParameterBeingDragged != null)
			{
				GUIDrawing.DrawCurves( OutputParameterBeingDragged.outputPoint+zoomPosAdjust, Event.current.mousePosition, getDataColor(OutputParameterBeingDragged.Type), shadowColor);
				doRepaint = true;
			}
		}




		// RUBBER BAND
		if (editorState == EditorState.DraggingRubbeband)
		{

			//Debug.Log(mouseDownPoint);
			Handles.color = Color.white;
			Handles.DrawSolidRectangleWithOutline(rubberRect, new Color(1, 1, 1, .08f), Color.cyan);

		}






		EditorZoomArea.End();







		if(GUI.changed)
		{
			//Debug.Log ("EDITOR_WINDOW - MAIN GUI CHANGED");

			// THIS CHANGE SEEMS TO BE DETECTED MODE INTERMITTENTLY THAN A 
			// CHANGE CHECK IN THE PROPRTY DRAWERS BELOW. OR AT LEAST AFTER THE PROPERTY DRAWERS HAV
			// UPDATED THEIR VALUES

			// HOWEVER< THE PROPERTYDRAWERS UPDATE THEIR VARIABLES MORE SLOWLY
			// SO IF YOU CALL THE GENERATE ON CHANGE CHECK IN THE PROPERTY DRAWER, 
			// YOU GENERATE WITH AN OLD VALUE...

			//.... so the staccato editing here may be a function of the proprty drawer speed.

		}





	}



	public Vector2 getUnzoomedPoint(Vector2 pt)
	{
		
		Vector2 unzoomedPt =  pt * model.zoomScale + origin;
		return  unzoomedPt;
	}

	#endregion








	// ON_GUI 
	void OnGUI() {

//		if (Event.current.type == EventType.KeyDown)
//			ArchimatixEngine.keyIsDown = true;

//		if (Event.current.type == EventType.KeyUp)
//			ArchimatixEngine.keyIsDown = false;

		


		headerHgt = 24;
		footerHgt = 32;


		float sidebarHeight = position.height-headerHgt-footerHgt-2;

		leftSidebarWidth 	= 68;
		rightSidebarWidth 	= 68;

		leftSidebarRect  =  new Rect(								 0, headerHgt, leftSidebarWidth, sidebarHeight);
		rightSidebarRect =  new Rect(position.width-rightSidebarWidth, headerHgt, rightSidebarWidth, sidebarHeight);


		graphRect = new Rect(leftSidebarWidth, headerHgt, position.width-rightSidebarWidth-leftSidebarWidth, position.height-headerHgt-footerHgt);


		relationEditorRect = new Rect(leftSidebarWidth, position.height+footerHgt/2-170, graphRect.width-1, 170);


		origin = new Vector2(leftSidebarWidth, headerHgt);


		Event e = Event.current;

		Color oldBackgroundColor = GUI.backgroundColor;
		Color defaultColor = GUI.color;



		//Debug.Log("Focusing " + GUI.GetNameOfFocusedControl());

		if (Event.current.type == EventType.Repaint && model != null && model.canRegenerate && ! Application.isPlaying )
		{
			model.generateIfDirty();
		}


		//Handles.DrawLine(graphRect.center-new Vector2(-30, 0), graphRect.center-new Vector2(30, 0));
		//Handles.DrawLine(graphRect.center-new Vector2(0, -30), graphRect.center-new Vector2(0, 30));


		if (model == null)
		{




			// IS THERE ONLY ONE MODEL IN THE SCENE? IF SO DISPLAY IT
			AXModel[] models =  FindObjectsOfType(typeof(AXModel)) as AXModel[];

			if (models != null)
			{
				if (models != null && models.Length == 1)
					model = models[0];
			}

			if (e.type == EventType.ContextClick)
			{
				// Debug.Log ("NO MODEL SELECTED - CONTEXT CLICK ");

				EditorUtility.DisplayPopupMenu (new Rect (e.mousePosition.x,e.mousePosition.y,0,0), "GameObject/3D Object/Archimatix Nodes", null);
				e.Use();
			}

		}

		else
		{

			zoomScale 		= model.zoomScale;
			focusCenter 	= model.focusPointInGraphEditor;

			for (int i=0; i<model.parametricObjects.Count; i++)
			{
				AXParametricObject po = model.parametricObjects[i];

				if (po.rect.Contains(e.mousePosition))
					po.revealControls = false;
				else
					po.revealControls = false;
			}



		}






		zoomPosAdjust =  ( (graphRect.center) - origin )/zoomScale - focusCenter;



		if (e.type == EventType.ValidateCommand)
			e.Use ();


		

		switch (e.type)
		{


		case EventType.Layout:
			if (_doAutobuild)
			{
				_doAutobuild = false;
				model.autobuild();
			}

			break;

		case EventType.Repaint:

			if (lastFocusedGUIName != GUI.GetNameOfFocusedControl())
				lastFocusedGUIName = GUI.GetNameOfFocusedControl();
			break;

			//case EventType.


		case EventType.MouseDown:
			{
				bool doubleClick = false;

				if ( (StopWatch.now()-mouseDownTime) < 400)
					doubleClick = true;

				mouseJustDown 	= true;
				mouseIsDown 	= true;
				mouseDownTime	= StopWatch.now();

				mouseDownPoint = e.mousePosition;

				if (doubleClick) // DOUBLE_CLICK
				{
					if (model != null && ! lastFocusedGUIName.Contains("_Text_"))
					{

						if (model.selectedPOs != null && mouseLastDownOnPO != null && model.selectedPOs.Contains(mouseLastDownOnPO))
						{

							if (mouseLastDownOnPO.generator is Grouper)
							{
								//mouseLastDownOnPO.toggleGroupeesDisplay();
								Undo.RegisterCompleteObjectUndo (model, "Open Grouper");

								// //> CLOSE GROUP
								if (model.currentWorkingGroupPO == mouseLastDownOnPO)
									closeCurrentWorkingGroup();

								else //> OPEN GROUP
									openAsCurrentWorkingGroup(mouseLastDownOnPO);
							}
						}
					}
				}

				else // SINGLE CLICK
				{

					if (model != null)
						model.endPanningToPoint();

					if (e.button == 1 || e.button == 2 || e.alt || e.control)
					{
						if (model != null && editorState != EditorState.DraggingWindowSpace) 
							Undo.RegisterCompleteObjectUndo (model, "Window Drag");
						else 
							Undo.RegisterCompleteObjectUndo (this, "Window Drag");


						if (draggingThumbnailOfPO == null)
						{	
							editorState = EditorState.DraggingWindowSpace;
							e.Use();
						}


					}
					else
					{
						if (model != null)
							model.readyToRegisterUndo = true;


						else
						{ 
							if (draggingThumbnailOfPO == null)
							{
								//editorState = EditorState.DraggingWindowSpace;

								// Start rubberband
							}
						}
						onMouseDown();
					}

				}



				break;
			} 


		case EventType.mouseUp:
			{

				mouseJustDown = false;
				if (model != null)
					model.readyToRegisterUndo = false;

				onMouseUp();
				break;
			} 

		case EventType.MouseDrag:

			mouseHasBeenDragged = true;

			switch(editorState)
			{
			case EditorState.DragResizingLogicWindow:

				if (DraggingParameticObject != null)
				{
					DraggingParameticObject.codeWindowHeight += (int) (e.delta.y/model.zoomScale);

					DraggingParameticObject.rect.width 	 += (int) (e.delta.x/model.zoomScale);;
					//DraggingParameter.rect.height 	 += (int) (e.delta.y/model.zoomScale);;

					if (DraggingParameticObject.rect.width < DraggingParameticObject.generator.minNodePaletteWidth)
						DraggingParameticObject.rect.width = DraggingParameticObject.generator.minNodePaletteWidth;

					//DraggingParameticObject.rect.width = -2 + snapValue(DraggingParameticObject.rect.width);
				}
				break;

			case EditorState.DragResizingNodePalleteWindow:

				if (DraggingParameticObject != null)
				{
					//DraggingParameter.codeWindowHeight += (int) (e.delta.y/model.zoomScale);

					DraggingParameticObject.rect.width 	 += (int) (e.delta.x/model.zoomScale);;
					DraggingParameticObject.rect.height 	 += (int) (e.delta.y/model.zoomScale);;

					if (DraggingParameticObject.rect.width < DraggingParameticObject.generator.minNodePaletteWidth)
						DraggingParameticObject.rect.width = DraggingParameticObject.generator.minNodePaletteWidth;

					//DraggingParameticObject.rect.width = -2 + snapValue(DraggingParameticObject.rect.width);
				}
				break;

			}

			if(editorState == EditorState.DraggingWindowSpace)
			{
				// drag entire view
				if (model != null)
				{
					zoomScale 		= model.zoomScale;
					focusCenter 	= model.focusPointInGraphEditor;
				}
				if (zoomScale == 0) zoomScale = 1;



				//
				if (e.alt && (e.button == 1 || e.control) )
				{
					// ZOOM - Windows: alt-RMB, OS X control-alt

					Vector2 mouse_displ_px = mouseDownPoint - graphRect.center;
					focusCenter += mouse_displ_px/zoomScale;
					float deltaZoom = -e.delta.y/100;			
					zoomScale -= deltaZoom;
					zoomScale = Mathf.Clamp(zoomScale,.1f, maxZoom );
					focusCenter -= mouse_displ_px/zoomScale;

				}
				else
				{
					// PAN
					focusCenter -= e.delta/zoomScale;
				}

				if (model != null)
				{
					model.zoomScale = zoomScale;
					model.focusPointInGraphEditor = focusCenter;
				}	

				zoomPosAdjust =  (graphRect.center-origin)/zoomScale - focusCenter;

				doRepaint = true;
			}
			else if (mouseIsDownOnPO != null && draggingThumbnailOfPO != null)
			{

				if (e.command || e.control)
				{
					draggingThumbnailOfPO.cameraSettings.radiusAdjuster -= e.delta.y/100;

				}
				else
				{
					draggingThumbnailOfPO.cameraSettings.alpha += e.delta.x;
					draggingThumbnailOfPO.cameraSettings.beta += e.delta.y;
				}


				draggingThumbnailOfPO.cameraSettings.setPosition();
				//model.cacheThumbnails();

				Thumbnail.BeginRender();
				Thumbnail.render(draggingThumbnailOfPO);
				Thumbnail.EndRender();
			}
			break;


		case EventType.ScrollWheel:
			{

				// ZOOM - logicwheel Zooming

				if (leftSidebarRect.Contains( Event.current.mousePosition ) || rightSidebarRect.Contains( Event.current.mousePosition ) ||  GUI.GetNameOfFocusedControl().Contains("logicTextArea_"))
					break;

				if (model != null)
				{
					zoomScale 		= model.zoomScale;
					focusCenter 	= model.focusPointInGraphEditor;
				}
				Vector2 mouse_displ_px = Event.current.mousePosition - graphRect.center;

				// shift focus temporarily based on mouse point
				focusCenter += mouse_displ_px/zoomScale;

				// alter zoom based on scoll delta
				float deltaZoom = e.delta.y/100;			
				zoomScale -= deltaZoom;

				zoomScale = Mathf.Clamp(zoomScale,.1f, maxZoom );

				// resift now that zoom is done by subtracting out the mouse distance in the new zoom
				focusCenter -= mouse_displ_px/zoomScale;


				if (model != null)
				{
					model.zoomScale = zoomScale;
					model.focusPointInGraphEditor = focusCenter;
					model.endPanningToPoint();;
				}

				zoomPosAdjust =  (graphRect.center-origin)/zoomScale - focusCenter;

				doRepaint = true;

				break;
			}

		case EventType.ExecuteCommand:
			{
				//Debug.Log ("AXModelEditorWindow: EventType.ExecuteCommand =======================> > > > > > > > > > > > > > > > > > > > [1]");


				//if (commandStopWatch == null || commandStopWatch.stop() > 100)
				//{
				if (model != null)
				{
					//		commandStopWatch = new StopWatch();

					if (! textAreaIsInFocus())
						AXEditorUtilities.processEventCommand(e, model);
				}
				//}

				doRepaint = true;
				break;
			}


		case EventType.KeyDown:
		{

				if (! GUI.GetNameOfFocusedControl().Contains("logicTextArea_")  &&  (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter))
				{
					
					if (! String.IsNullOrEmpty(lastFocusedGUIName))
					{
						clearFocus();
						model.autobuild();
					}
				} 

				else if (e.keyCode == KeyCode.Space)
				{
					//Debug.Log("Space"); 




					//This was causing a bug where all the nodes in the grouper were being hidden
//					else if (model != null && model.selectedPOs != null && model.selectedPOs.Count > 0 && model.selectedPOs[0] != null && model.selectedPOs[0].generator is Grouper)
//					{
//						model.selectedPOs[0].toggleGroupeesDisplay();
//						e.Use();
//					}





				}


				else 
				{
					
					if (model != null && model.selectedPOs != null && model.selectedPOs.Count > 0)
					{
						if (e.command || e.alt || e.control)
							AXEditorUtilities.processEventCommandKeyDown(e, model);
					}
				}



				doRepaint = true;
				break;

			}




		}


	 if (e.rawType == EventType.mouseUp) 
		{

			if (mouseIsDownOnPO != null)
			{

				//snapPosition(ref mouseIsDownOnPO.rect);

				foreach(AXParametricObject pom in model.selectedPOs)
					snapPosition(ref pom.rect);

				
			}
			editorState = EditorState.Default;

			mouseIsDown = false;
			mouseIsDownOnPO = null;
			draggingThumbnailOfPO = null;

			doRepaint = true;

		}






		GUI.SetNextControlName("dummy_label");
		GUI.Label( new Rect(-10000, -10000, 1, 1), "focus_dummy");



		if (showMenu != ""  && ! menuRect.Contains(e.mousePosition))
		{
			//Debug.Log ("showMenu OFF");
			showMenu = "";
			doRepaint = true;
		}

		gridColor 			= ArchimatixEngine.AXGUIColors["GridColor"];
		axisColor 			= ArchimatixEngine.AXGUIColors["AxisColor"];
		curveShadowColor 	= ArchimatixEngine.AXGUIColors["ConnectorShadowColor"];
		splineColor 		= ArchimatixEngine.AXGUIColors["ConnectorColor"];


		 

		//Debug.Log ("FOCUS CONTROL: " + GUI.GetNameOfFocusedControl());




		// YOO
		drawNodeGraphWindow(1);


		// HEADER

		Rect headerRect = new Rect (0, 2, position.width, headerHgt);
		//Rect headerMenuGraphicWithShadowRect = new Rect (0, 0, position.width, 29);
		Rect headerMenuGraphicWithShadowRect = new Rect (0, 0, position.width, 36);



		Texture2D oldBackgroundTexture = GUI.skin.box.normal.background;
		GUI.skin.box.normal.background = menubarTexture;

		GUI.Box (new Rect(0, 0, position.width, headerHgt), GUIContent.none);

		GUI.DrawTexture (headerMenuGraphicWithShadowRect, menubarTexture); 

		GUI.skin.box.normal.background = oldBackgroundTexture;


		GUI.backgroundColor = oldBackgroundColor;
		GUI.backgroundColor = Color.white;
		GUI.color = defaultColor;


		//EditorGUILayout.BeginHorizontal();
		GUIStyle labelstyle = GUI.skin.GetStyle ("Label");




		// DO HEADER MENU BAR -- MODEL MENU

	
	 
	  
		GUILayout.BeginArea(headerRect);
		GUILayout.BeginHorizontal();

		labelstyle.alignment = TextAnchor.LowerLeft;

		labelstyle.fixedWidth = 150;


		GUILayout.Space(5);


		if(GUILayout.Button ("2D"))
		{
			ArchimatixEngine.openLibrary2D();
		}
		if(GUILayout.Button ("3D"))
		{
			ArchimatixEngine.openLibrary3D();
		}

		 

		// NEW MODEL
		GUILayout.Space(10);



		if(GUILayout.Button ("▼"))
		{

			AXModel[] allModels = ArchimatixUtils.getAllModels();

				
			GenericMenu menu = new GenericMenu ();

			//menu.AddSeparator("Library ...");


			if (allModels != null && allModels.Length > 0)
			{
				for (int i = 0; i < allModels.Length; i++) {
					AXModel m = allModels [i];
					menu.AddItem (new GUIContent (m.gameObject.name), false, () =>  {
						
						Selection.activeGameObject = m.gameObject;
						ArchimatixEngine.currentModel = m;


					});
				}
			}


			menu.AddSeparator("");

			menu.AddItem(new GUIContent("New Model"), false, () => {
				AXEditorUtilities.createNewModel();
			});

			menu.ShowAsContext ();
			

		}




		Color bgc = GUI.backgroundColor;
		GUI.backgroundColor = Color.clear;







		if (model != null)			
		{
			// SELECTED MODEL
			if(GUILayout.Button (model.name))
			{
				model.currentWorkingGroupPO = null;

				model.selectAllVisibleInGroup(null);

				float framePadding = 50;

				Rect allRect = AXUtilities.getBoundaryRectFromPOs(model.selectedPOs);
				allRect.x 		-= framePadding;
				allRect.y 		-= framePadding;
				allRect.width 	+= framePadding*2;
				allRect.height 	+= framePadding*2;



				AXNodeGraphEditorWindow.zoomToRectIfOpen(allRect);
			}



			if (model.currentWorkingGroupPO != null)
			{
				if (model.currentWorkingGroupPO.grouper != null)
				{
					// Make breadcrumb trail
					List<AXParametricObject> crumbs = new List<AXParametricObject>();
					AXParametricObject cursor = model.currentWorkingGroupPO;


					while (cursor.grouper != null)
					{						
						crumbs.Add(cursor.grouper);
						cursor = cursor.grouper;
					}
					crumbs.Reverse();

					// model button frames 



					for(int i=0; i<crumbs.Count; i++)
					{
						if (GUILayout.Button("> " + crumbs[i].Name))
						{
							model.currentWorkingGroupPO = crumbs[i];
							Rect grouperCanvasRect = model.currentWorkingGroupPO.getBoundsRect();
							zoomToRect(grouperCanvasRect);

						}
					}	
				}
				GUILayout.Button("> "+model.currentWorkingGroupPO.Name);
			}
		}
		GUILayout.FlexibleSpace();




		if (model != null)
		{
			Color buildBG = Color.Lerp(Color.cyan, Color.white, .8f);
			string buildLabel = "Rebuild";



			if (model.buildStatus == AXModel.BuildStatus.Generated)
			{
				buildBG		= Color.red;
				buildLabel	= "Build";
			}


	
			GUI.backgroundColor = buildBG;
			if (GUILayout.Button( buildLabel))
				model.build ();

			GUI.backgroundColor = Color.cyan;


			if (model.generatedGameObjects != null && model.generatedGameObjects.transform.childCount == 0)
			{	
				GUI.enabled = false;
			}

			if (GUILayout.Button( "Stamp"))
			{
				model.stamp ();

			}

			if (GUILayout.Button("Prefab"))
			{
				string startDir = Application.dataPath;

				string path = EditorUtility.SaveFilePanel(
					"Save Prefab",
					startDir,
					(""+model.name),
					"prefab");

				if (! string.IsNullOrEmpty(path))
					AXPrefabWindow.makePrefab(model, path, this);
			}
			GUI.enabled = true;

			GUILayout.Space(4);

		}


		GUILayout.EndHorizontal();
		GUILayout.EndArea();


		/*

			versionRect.x += 10;

			labelstyle.alignment = TextAnchor.MiddleLeft;
			labelstyle.fixedWidth = 200;
			GUI.Label (versionRect, "Archimatix v" + ArchimatixEngine.version);

			if (model != null)
			{
			
				Color bgColor = GUI.backgroundColor;



				if (model.buildStatus == AXModel.BuildStatus.Generated)
				{
					GUI.backgroundColor = Color.red;
					if (GUI.Button(new Rect(position.width-290,0,100,18), "Build"))
						model.build ();
				} else {


					GUI.backgroundColor = Color.Lerp(Color.cyan, Color.white, .8f);
					if (GUI.Button(new Rect(position.width-290,0,100,18), "Rebuild"))
						model.build ();
					GUI.backgroundColor = Color.cyan;



					if (GUI.Button(new Rect(position.width-190,0,100,18), "Stamp"))
					{
						model.stamp ();

					}
					if (GUI.Button(new Rect(position.width-90,0,100,18), "Prefab"))
					{

						string startDir = Application.dataPath;

						string path = EditorUtility.SaveFilePanel(
							"Save Prefab",
							startDir,
							(""+model.name),
							"prefab");
						
						//Debug.Log (path);


						// popup window
						//AXPrefabWindow prefabWin = ScriptableObject.CreateInstance<AXPrefabWindow>();
						//prefabWin.Popup(model, path, this);
						//model.prefab (path);
						AXPrefabWindow.makePrefab(model, path, this);
					}


				}


				GUI.backgroundColor = bgColor;




				 




				
			}
			
		*/


		GUI.backgroundColor = bgc;



		//EditorGUILayout.EndHorizontal();




		// RELATION EDITOR "WINDOW"
		if (model != null && model.selectedRelationInGraph != null)
		{
			// yugga  relationEditorRect
				
			//Rect relationEditorRect = new Rect(leftSidebarWidth, graphRect.y+graphRect.height-50,  graphRect.width, 50);

			GUI.DrawTexture (relationEditorRect, menubarTexture); 

			float pad = 20;


			GUILayout.BeginArea(new Rect((relationEditorRect.x+pad), (relationEditorRect.y+pad/4), (relationEditorRect.width-2*pad), (relationEditorRect.height+pad) ));



			RelationEditorGUI.OnGUI(model.selectedRelationInGraph);

			GUILayout.EndArea();

			if (e.type != EventType.Layout && e.type != EventType.Repaint)
				e.Use();
		}




		// SIDEBARS


		// -- LEFT SIDEBAR: LIBRARY MENU

		GUI.Box (leftSidebarRect, GUIContent.none);

		// SHADOW
		GUI.DrawTexture(new Rect (leftSidebarWidth, headerHgt, 10, sidebarHeight), verticalShadowLeftTexture);

		float leftHeight2D = sidebarHeight * leftSidebar2DHeightPercentage;
		float leftSidebarSeperatorHeight = 16;
		float leftHeight3D = sidebarHeight * (1-leftSidebar2DHeightPercentage) - leftSidebarSeperatorHeight;

		Rect leftSidebarRect2D  		= new Rect(2, 										  headerHgt, leftSidebarWidth, 					leftHeight2D);
		Rect leftSidebarSeperator3D		= new Rect(2, 		 				   headerHgt+leftHeight2D+2, leftSidebarWidth,  leftSidebarSeperatorHeight-4);
		Rect leftSidebarRect3D  		= new Rect(2, headerHgt+leftHeight2D+leftSidebarSeperatorHeight, leftSidebarWidth, 					leftHeight3D);

		//Debug.Log ();

		if (libraryMenu2D != null)
		{
			GUILayout.BeginArea(leftSidebarRect2D);
			libraryMenu2D.display(leftSidebarWidth-4, this, "2D");
			GUILayout.EndArea();
		}

		GUI.Button(leftSidebarSeperator3D, "=");

		if (libraryMenu3D != null)
		{
			GUILayout.BeginArea(leftSidebarRect3D);
			libraryMenu3D.display(leftSidebarWidth-4, this, "3D");
			GUILayout.EndArea();
		}





		// -- RIGHT SIDEBAR: NODE MENU
		GUI.Box (rightSidebarRect, GUIContent.none);

		//verticalShadowTexture
		GUI.DrawTexture(new Rect (rightSidebarRect.x-10, headerHgt, 10, sidebarHeight), verticalShadowRightTexture);

		GUILayout.BeginArea(rightSidebarRect);
		NodeMenu.display(rightSidebarWidth-4, this);
		GUILayout.EndArea();











		if (model != null)
		{



			



			// DO FOOTER
			float statusBarY = position.height-footerHgt;

			GUI.Box (new Rect(0, statusBarY, position.width, footerHgt), GUIContent.none);

			float bSize = 32;

			Rect vButtonRect	= new Rect (4, statusBarY-7, 	bSize, bSize);

			Color vcolor = Color.white;
			vcolor.a = .5f;

			GUI.color = vcolor;
			GUI.Label (vButtonRect, "AX v" + ArchimatixEngine.version);
			GUI.color = Color.white;


			Rect mButtonRect	= new Rect (90, statusBarY, 	bSize, bSize);
			Rect tooltipRect 	= new Rect (mButtonRect.x-10, statusBarY-25, 	100, bSize);

			labelstyle.alignment = TextAnchor.MiddleLeft;

			Color prevGUIColor = GUI.color;

			Color gcol = GUI.color;
			GUI.backgroundColor = Color.gray;

			tooltipRect.x 	= mButtonRect.x-10;
			// BUTTON: Close All Controls
			if (mButtonRect.Contains(Event.current.mousePosition)) // TOOLTIP
			{
				gcol.a = .8f;
				GUI.Label (tooltipRect, "Close All Controls");
			}
			else
				gcol.a = .5f;
			GUI.color = gcol;
			if (GUI.Button ( mButtonRect, CloseAllControlsIcon))
			{ 
				closeAllControls ();
			}


			// BUTTON: Close All Tools
			mButtonRect.x 	+= bSize + 3;
			tooltipRect.x 	= mButtonRect.x-10;

			if (mButtonRect.Contains(Event.current.mousePosition))// TOOLTIP
			{
				gcol.a = .8f;
				GUI.color = gcol;
				GUI.Label (tooltipRect, "Close All Tools");
			}
			else
				gcol.a = .5f;
			GUI.color = gcol;
			if (GUI.Button (mButtonRect, CloseAllToolsIcon ))
			{ 
				closeTools ();
			}


			// BUTTON: Show All Nodes
			mButtonRect.x 	+= bSize + 3;
			tooltipRect.x 	= mButtonRect.x-10;

			if (mButtonRect.Contains(Event.current.mousePosition))// TOOLTIP
			{
				gcol.a = .8f;
				GUI.color = gcol;
				GUI.Label (tooltipRect, "Show All Nodes");
			}
			else
				gcol.a = .5f;
			GUI.color = gcol;
			if (GUI.Button (mButtonRect, ShowAllNodesIcon))
			{
				foreach(AXParametricObject po in model.parametricObjects)
					po.isOpen = true;

			}


			// zoomScale

			mButtonRect.x 	+= bSize + 3;
			tooltipRect.x 	= mButtonRect.x-10;
			mButtonRect.width = 45;
			if (mButtonRect.Contains(Event.current.mousePosition))// TOOLTIP
			{
				gcol.a = .8f;
				GUI.color = gcol;
				GUI.Label (tooltipRect, "Zoom Scale");
			}
			else
				gcol.a = .5f;
			GUI.color = gcol;
			if (GUI.Button (mButtonRect, (""+(model.zoomScale*100))+"%"))
			{

				zoomScale = 1;
				model.zoomScale = 1;
				Repaint();
			}





			GUI.color = prevGUIColor;


			//GUI.Label (new Rect (position.width / 2, statusBarY + 10, 100, 20), "Archimatix v " + ArchimatixEngine.version);



			if (model != null)
			{
				//Debug.Log("model.stats_TriangleCount="+model.stats_TriangleCount);
				EditorGUI.LabelField(new Rect(position.width-335, statusBarY+7, 100, 20), "Vertices: " + model.stats_VertCount);
				EditorGUI.LabelField(new Rect(position.width-230, statusBarY+7, 100, 20), "Triangles: " + model.stats_TriangleCount);

				EditorGUI.BeginChangeCheck();
				EditorGUIUtility.labelWidth = 70;
				model.segmentReductionFactor = EditorGUI.Slider( new Rect(position.width-120, statusBarY+7, 115, 20), "Detail Level", model.segmentReductionFactor, 0, 1);
				if (EditorGUI.EndChangeCheck())
				{
					Undo.RegisterCompleteObjectUndo (model, "Segment Reduction");
					model.isAltered();
				}
			}

			Handles.BeginGUI( );
			Handles.color = Color.gray;
			Handles.DrawLine( 
				new Vector3(0, statusBarY, 0),
				new Vector3(position.width, statusBarY, 0));
			Handles.EndGUI ();






		}











		if (editorState == EditorState.DraggingWindowSpace || doRepaint == true )
		{
			//Repaint(); 
			//doRepaint = false;
		}



		if  (  Event.current.type == EventType.ScrollWheel  ||  (InputParameterBeingDragged != null) || (OutputParameterBeingDragged != null) ||( (model != null && model.isPanningToPoint) ||   mouseIsDown)  )
			Repaint();



		if (editorState == EditorState.DraggingNodePalette)
		{


			if (mouseIsDownOnPO != null)
			{
				
				//snapPosition(ref mouseIsDownOnPO.rect);
			}
		}

		labelstyle.fixedWidth = 0;

	} // OnGUI


	void doRelationGUI(int id)
	{
		GUI.Label(new Rect(20,20,200,30), "Hallo");
	}


	public bool parameterIsVisibleInPalette(AXParameter p)
	{
		//Debug.Log ("is vis? " + p.Parent.Name + "."+p.Name + " :: " + p.Parent.showControls + p.PType);

		if (p.Type == AXParameter.DataType.Spline || p.Type == AXParameter.DataType.Mesh)
			return true;

		return p.isVisible();
	}

	public bool nodeIsVisibleInPalette(AXNode p)
	{
		//Debug.Log ("is vis? " + p.Parent.Name + "."+p.Name + " :: " + p.Parent.showControls + p.PType);


		return p.isVisible();
	}

	public void drawRelations(int level, bool cacheCurve=false) 
	{
		

		if (model != null)
		{




			//Debug.Log ("RELATIONS ROLE CALL");
			for (int i = 0; i<model.relations.Count; i++) 
			{
				AXRelation r = model.relations[i];

				AXNode pA = r.pA;
				AXNode pB = r.pB;

				Event e = Event.current;

				r.isVisibleInGraphEditor = false;

				if (pA == null || pB == null)
					continue;


				if (model.currentWorkingGroupPO == null && (pA.parametricObject.grouper != null || pB.parametricObject.grouper != null))
					continue;


				if (pA.parametricObject.grouper != null && model.currentWorkingGroupPO != null && pA.parametricObject.grouper != model.currentWorkingGroupPO && pA.parametricObject != model.currentWorkingGroupPO)
					continue;


				if (pB.parametricObject.grouper != null && model.currentWorkingGroupPO != null && pB.parametricObject.grouper != model.currentWorkingGroupPO && pB.parametricObject != model.currentWorkingGroupPO)
					continue;

				if ( pA == null || pB == null ||  (! pA.parametricObject.isOpen && ! pB.parametricObject.isOpen))
					continue;




				bool pA_visible 		= (pA.parametricObject.isOpen && nodeIsVisibleInPalette(pA));
				bool pA_onlyRubric_visible 	= (pA.parametricObject.isOpen && ! pA_visible);

				bool pB_visible = (pB.parametricObject.isOpen && nodeIsVisibleInPalette(pB));
				bool pB_onlyRubric_visible 	= (pB.parametricObject.isOpen && ! pB_visible);

				//Debug.Log ("VISIBILITY: pA_visible="+pA_visible+", pB_visible="+pB_visible);


				if (pA_onlyRubric_visible)
					pA = pA.ParentNode;

				if (pB_onlyRubric_visible)
					pB = pB.ParentNode;

				bool doDraw = false;

				if (level==0)
				{
					if(editorState == EditorState.DraggingWindowSpace || ( ! model.isSelected(pA.parametricObject) && ! model.isSelected(pB.parametricObject)  )   )
						doDraw = true;
				}
				else // level = 1
				{
					if (  model.isSelected(pA.parametricObject) || model.isSelected(pB.parametricObject)  )
						doDraw = true;
				}

				Color col = getDataColor(r.pA.Type);
				if (doDraw && pA != null && pB != null && pA.inputPoint != Vector2.zero && pB.inputPoint != Vector2.zero && pA.parametricObject != null && pB.parametricObject != null )
				{

					// order in & out pts so in is always to right of out

					Vector2 leftpt, 		rightpt;
					Vector2 pA_buttonPos, 	pB_buttonPos;
					//float 	pA_expr_x, 		pB_expr_x; 
					//float 	pA_expr_y, 		pB_expr_y; 
					string  pA_arrow, 		pB_arrow;

//					float center_x;
//					float lower_y, upper_y, center_y;




					if ( (pA.inputPoint.x + pA.parametricObject.rect.width/2) < (pB.inputPoint.x + pB.parametricObject.rect.width/2))
					{
						// pA is to the LEFT of pB


						leftpt  = pA.outputPoint + zoomPosAdjust;
						rightpt = pB.inputPoint  + zoomPosAdjust;

						// center x
						//center_x = leftpt.x+(rightpt.x-leftpt.x)/2;

						pA_buttonPos = new Vector2( pA.outputPoint.x +  2, pA.inputPoint.y-8);
						pB_buttonPos = new Vector2( pB.inputPoint.x  - 20, pB.inputPoint.y-8);
						pA_buttonPos += zoomPosAdjust;
						pB_buttonPos += zoomPosAdjust;

						pA_arrow = rightArrow;
						pB_arrow = leftArrow;

						// expression x
						//pA_expr_x = pA.outputPoint.x  + 20;
						//pB_expr_x = pB.inputPoint.x   - 170;



					} 
					else 
					{
						// pB is to the RIGHT of pA

						leftpt  = pB.outputPoint + zoomPosAdjust;
						rightpt = pA.inputPoint  + zoomPosAdjust;

						//center_x = leftpt.x+(rightpt.x-leftpt.x)/2;

						// center x
						pA_buttonPos = new Vector2( pA.inputPoint.x  - 20, pA.inputPoint.y-8);
						pB_buttonPos = new Vector2( pB.outputPoint.x +  2, pB.inputPoint.y-8);
						pA_buttonPos += zoomPosAdjust;
						pB_buttonPos += zoomPosAdjust;

						pA_arrow = leftArrow;
						pB_arrow = rightArrow;

						// expression x
						//pA_expr_x = pA.inputPoint.x  -170;
						//pB_expr_x = pB.outputPoint.x +20;

					}
//					lower_y = (leftpt.y < rightpt.y) ? leftpt.y : rightpt.y;
//					upper_y = (leftpt.y > rightpt.y) ? leftpt.y : rightpt.y;
//					center_y = lower_y+(upper_y-lower_y)/2 -6;

					//pA_expr_y = pA.inputPoint.y-8;
					//pB_expr_y = pB.inputPoint.y-8;

					//Rect pA_expr_rect = new Rect(pA_expr_x, pA_expr_y, 150, 16);
					//Rect pB_expr_rect = new Rect(pB_expr_x, pB_expr_y, 150, 16);


					//Debug.Log ("ACTUALLY DRAW CURVES: pA_visible="+pA_visible+", pB_visible="+pB_visible);

					// ACTUALLY DRAW CURVES
					if ( (pA_visible || pA_onlyRubric_visible) && (pB_visible || pB_onlyRubric_visible) )
					{

						r.isVisibleInGraphEditor = true;

						//Debug.Log(new Vector2(leftP.parametricObject.rect.x, leftP.parametricObject.rect.y) + " :: " + (leftpt-zoomPosAdjust) + " :: " + zoomPosAdjust);
						//Vector2 leftPosition =  new Vector2(leftP.parametricObject.rect.x, leftP.parametricObject.rect.y) + leftpt - zoomPosAdjust;
						//Vector2 rightPosition =  new Vector2(rightP.parametricObject.rect.x, rightP.parametricObject.rect.y) + rightpt - zoomPosAdjust;
						Vector2 leftPosition =  leftpt - zoomPosAdjust;
						Vector2 rightPosition = rightpt - zoomPosAdjust;

						//Debug.Log ("Adding curve r["+i+"]="+r);
						if (level == 0)
							relationCurves.Add(new RelationBezierCurve(r, leftPosition, rightPosition));



						if (doDraw)
							{
							Color c = col;
							float curveThickness = 5;
							if (model.selectedRelationInGraph == r)
							{
								c = Color.white;
								curveThickness = 10;
							}


							GUIDrawing.DrawCurves( leftpt, rightpt, c, curveShadowColor, curveThickness);

						}
					}


					/*
					else if (pA_visible)
						GUIDrawing.DrawCurves( leftpt,   leftpt + new Vector2(20, 0), col);
					else if (pB_visible)
						GUIDrawing.DrawCurves( rightpt, rightpt + new Vector2(-20, 0), col);
					*/



					// ARROW BUTTONS

					GUIStyle buttonStyle = GUI.skin.GetStyle ("Button");


					buttonStyle.alignment = TextAnchor.MiddleCenter;
					buttonStyle.fontSize  = 10;
					//buttonStyle.fixedWidth = 0;
					//buttonStyle.fixedHeight = 0;


					if (true )//|| pA.Type == AXParameter.DataType.Float) 
					{
						// add relation buttons.
						Color oldColor = GUI.color;
						GUI.color = getDataColor(r.pA.Type);

						//Debug.Log ("DRAW rel: " + pA.Parent.Name +"."+ pA.Name + " and " +pB.Parent.Name +"."+ pB.Name);

						// pA BUTTON
						if (pA_visible || pA_onlyRubric_visible)
						{

							GUI.color = (model.selectedParameter == pA) ? Color.green : getDataColor(r.pA.Type);




							if (GUI.Button(new Rect(pA_buttonPos.x, pA_buttonPos.y, 18, 16), pA_arrow))
							{
								//Debug.Log("SHOW STUFF pA");
//
//								if (model.selectedParameter == pA)
//								{
//									model.selectedParameter = null;
//									r.pA.showAllRelated(false);
//								}
//								else {

								model.selectedParameter = r.pA;

								if (pA_visible)
								{
									if (! pB_visible)
									{
										r.pA.showAllRelated(true);
									}
									else
									{

										model.selectedRelation = r.pA.relations[0];
										//showRelationWindow();
										pA.ParentNode.isOpen = false;
										//											pB.ParentNode.isOpen = false;
										if (pA.ParentNode.children != null)
											foreach(AXNode c in pA.ParentNode.children)
												(c as AXParameter).foldAllRelated();

									}


								}
								if (pA_onlyRubric_visible)
								{
									pA.isOpen = true;
									if (pA.children != null)
										foreach(AXNode c in pA.children)
											(c as AXParameter).showAllRelated(true);
									r.pA.showAllRelated(true);
								}

								model.selectOnlyPO (pA.parametricObject);
								EditorUtility.SetDirty(model.gameObject);
								//}
								e.Use();
							}
						}
						else if (pA_onlyRubric_visible)
						{
							// Is its parent node visible (ie, the Rubric "Controls?"
							//Debug.Log(pA.ParentNode.Name);
						}


						// pB BUTTON
						if (pB_visible || pB_onlyRubric_visible)
						{

							GUI.color = (model.selectedParameter == r.pB) ? Color.green : getDataColor(r.pB.Type);
							if (GUI.Button(new Rect(pB_buttonPos.x, pB_buttonPos.y, 18, 16), pB_arrow))
							{
								//Debug.Log("SHOW STUFF pB");
								//Debug.Log ("edit expressionfrom " + pA.Name + " into " + pB.Name ); 
								/*if (model.selectedParameter == pB)
								{
									Debug.Log("yup 1");
									model.selectedParameter = null;
									r.pB.showAllRelated(false);
								}
								else
								{
								*/
								model.selectedParameter = r.pB;

								if (pB_visible)
								{
									if (! pA_visible)
									{
										r.pB.showAllRelated(true);
									}
									else
									{

										//Debug.Log("HERE B "+pA.parametricObject.Name +"."+pA.ParentNode.Name+ " ;;; "+pB.parametricObject.Name +"."+pB.ParentNode.Name);
										model.selectedRelation = r.pB.relations[0];
										//showRelationWindow();
										r.pA.ParentNode.isOpen = false;

										if (r.pB != null && r.pB.ParentNode != null)
											r.pB.ParentNode.isOpen = false;
									}
									/*
									if (pA.ParentNode.children != null)
										foreach(AXNode c in pA.ParentNode.children)
												(c as AXParameter).foldAllRelated();
									if (pB.ParentNode.children != null)
										foreach(AXNode c in pB.ParentNode.children)
												(c as AXParameter).foldAllRelated();
												*/
								}
								if (pB_onlyRubric_visible)
								{
									//Debug.Log("yup 222");
									pB.isOpen = true;
									if (pB.children != null)
										foreach(AXNode c in pB.children)
											(c as AXParameter).showAllRelated(true);
									r.pB.showAllRelated(true);
								}



								model.selectOnlyPO (pB.parametricObject);
								EditorUtility.SetDirty(model.gameObject);
								//}
								e.Use();
							}
						}
						else if (pB_onlyRubric_visible)
						{
							// Is its parent node visible (ie, the Rubric "Controls?"
							//Debug.Log(pB.ParentNode.Name);
						}



						//AXParameter dragP = InputParameterBeingDragged;
						//if (InputParameterBeingDragged == null)
						//	dragP = OutputParameterBeingDragged;

//						if ( pA_visible && pB_visible ) 
//						{
//							// Offer delete button if editing one or the other parameter
//
//							GUI.color = Color.green;
//							if (GUI.Button(new Rect(center_x-9, center_y, 22, 12), " "))
//							{
//								model.selectedRelation = r;
//								showRelationWindow();
//							} 
//						}


						GUI.color = oldColor;

					}

				}

			}
		}
	}





	// Draw the uni-directional relationship connections
	// Foreach ParametricObject and then each Mesh or Spline output Parameter
	public void drawNodeRelations(int level, bool cacheCurve = false) 
	{
		Event e = Event.current;

		if (model != null)
		{

			Color oldColor = GUI.color;


			for (int i=0; i<model.parametricObjects.Count; i++) 
			{
				AXParametricObject po = model.parametricObjects[i];

				if (po.grouper != model.currentWorkingGroupPO && po != model.currentWorkingGroupPO)
					continue;

				if (!po.isOpen) continue;

				List<AXParameter> inputParameters= po.getAllInputParameters ();

				//Debug.Log("Relations for: "+po.Name+" " + parameters.Count);


				bool hasDependsOns 			= false;
				bool hasClosedDependsOns 	= false;
				AXParameter tmp;
				for (int k = 0; k < inputParameters.Count; k++) {
					tmp = inputParameters[k];
					if (tmp.DependsOn != null)
					{
						hasDependsOns = true;
						if (! tmp.DependsOn.parametricObject.isOpen && ! (tmp.DependsOn.parametricObject.generator is AXTool))
						{
							hasClosedDependsOns = true;
							break;
						}
					}
				}



				//Debug.Log("******************************");
				//bool madeSingletonButton = false;



				bool inputFoldoutIsClosed = false;

				Vector2 pA_buttonPos = Vector2.zero;
				Vector2 pB_buttonPos = Vector2.zero;
				string  pA_arrow = leftArrow;
				//string  pB_arrow = rightArrow;


				for (int j = 0; j < inputParameters.Count; j++) {

					AXParameter pA_p = inputParameters[j];

					//Debug.Log ("draw depends " + pA_p.parametricObject.Name +"."+pA_p.Name + " ... " + pA_p.DependsOn);

					if (pA_p.Type != AXParameter.DataType.Mesh && pA_p.Type != AXParameter.DataType.Spline && pA_p.Type != AXParameter.DataType.MaterialTool && pA_p.Type != AXParameter.DataType.JitterTool && pA_p.Type != AXParameter.DataType.RepeaterTool && pA_p.Type != AXParameter.DataType.Plane && pA_p.Type != AXParameter.DataType.Float && !po.showControls)
						continue;

					// if dependsOn and p are visible, connect them


					if (pA_p.DependsOn == null)
						continue;

					//Debug.Log(pA_p.Name + " parentNode: " + pA_p.ParentNode.Name);


					AXParameter pB_p = pA_p.DependsOn;

					AXNode pA = pA_p;
					AXNode pB = pB_p;


					bool pA_visible = true;
					bool pB_visible = false;

					if (pB_p != null && pB_p.parametricObject.isOpen) {
						//Debug.Log (dp.Name);
						pB_visible = true;

					}

					Color col = getDataColor (pA_p.Type);



					//if (! pB.ParentNode.isOpen)
					//Debug.Log("pA="+pA.Name+" pA.ParentNode="+ pA.ParentNode.Name);
					AXNode parentNode = pA.ParentNode;
					if (po.generator is ShapeMerger)
						parentNode = parentNode.ParentNode;

					if (! parentNode.isOpen)
					{
						pA = parentNode;
						inputFoldoutIsClosed = true;
					}

					//if (! pB.ParentNode.isOpen)
					//	pB = pB.ParentNode;


					pA_p.dependsOnCurveIsVisible = false;


					if ( pA_visible  && pB_visible) 
					{
							pA_p.dependsOnCurveIsVisible = true;
					}
					if ((editorState == EditorState.DraggingWindowSpace && level == 0) || (editorState != EditorState.DraggingWindowSpace && (level == 0 || (pA_p != null && model.isSelected (pA_p.parametricObject)) || (pB_p != null && model.isSelected (pB_p.parametricObject))))) {
						if ( pA_visible  && pB_visible) 
						{
							//pA_p.dependsOnCurveIsVisible = true;

							//Debug.Log("model.selectedParameterInputRelation="+model.selectedParameterInputRelation);
							Color c = col;
							float curveThickness =  8;
							if (pA_p == model.selectedParameterInputRelation)
							{
								c = Color.white;
								curveThickness = 11;
							}

							GUIDrawing.DrawCurves (pB.outputPoint+zoomPosAdjust, pA.inputPoint+zoomPosAdjust, c, curveShadowColor, curveThickness);

							//if (cacheCurve)
							//	relationCurves.Add(new RelationBezierCurve(, pB.outputPoint+zoomPosAdjust, pA.inputPoint+zoomPosAdjust));

						}

					}




					//Vector2 leftpt, 		rightpt;
					//float 	pA_expr_x, 		pB_expr_x; 
					//float 	pA_expr_y, 		pB_expr_y; 







					if (pA != null && pB != null)
					{


						//if ( (pA.inputPoint.x + pA.Parent.rect.width/2) < (pB.inputPoint.x + pB.Parent.rect.width/2))
						//{
						// pA is to the left of pB
						//leftpt  = pB.outputPoint+zoomPosAdjust;
						//rightpt = pA.inputPoint+zoomPosAdjust;


						// center x
						pA_buttonPos = new Vector2( pA.inputPoint.x  - 20, pA.inputPoint.y-8);
						pB_buttonPos = new Vector2( pB.outputPoint.x +  2, pB.inputPoint.y-8);
						pA_buttonPos += zoomPosAdjust;
						pB_buttonPos += zoomPosAdjust;

						pA_arrow = (pB.parametricObject.isOpen && ! hasClosedDependsOns) ? rightArrow :  leftArrow;



						//pB_arrow = ">";


						//pA_expr_x = pA.inputPoint.x  -170;
						//pB_expr_x = pB.outputPoint.x +20;



					} 



					if (! inputFoldoutIsClosed)
					{
						
						GUI.color = (model.selectedParameter == pA) ? Color.green : getDataColor(pA_p.Type);
						if (GUI.Button(new Rect(pA_buttonPos.x, pA_buttonPos.y, 18, 16), pA_arrow))
						{
							if (pB.parametricObject.isOpen)
							{
								if (Event.current.shift && Event.current.command)
									pA.parametricObject.startStowInputs();
								else
									pB.parametricObject.startStowInputs();
								pB.parametricObject.isOpen = false;
							}
							else
							{
								//Debug.Log("here");
								if (Event.current.shift)
								{
									if (Event.current.command)
										pA.parametricObject.startShowInputs();
									else
										pB.parametricObject.startShowInputs();
								}
								pB.parametricObject.isOpen = true;
								if (pB.parametricObject.generator is AXTool)
									pB.parametricObject.geometryControls.isOpen = true;
								po.shiftNodeToLeftOfMe(pB.parametricObject);
							}

							e.Use();
						}
					}




				}


				if (po.inputControls != null && !(po.generator is ShapeDistributor))
				{
					pA_buttonPos = new Vector2( po.inputControls.inputPoint.x  - 20, po.inputControls.inputPoint.y-8);

					pA_buttonPos += zoomPosAdjust;


					pA_arrow = (! hasClosedDependsOns) ? rightArrow :  leftArrow;


					if (inputParameters != null && inputParameters.Count > 0 && hasDependsOns)
					{
						//madeSingletonButton = true;

						// Only need one of these buttons...
						// Close or Open all items (s if shift click when foldout opn
						GUI.color = Color.green;


						if (GUI.Button(new Rect(pA_buttonPos.x, pA_buttonPos.y, 18, 16), pA_arrow))
						{
							//Debug.Log("hasClosedDependsOns="+hasClosedDependsOns);
							if (hasClosedDependsOns)
							{

								if (Event.current.shift)
								{
									po.startShowInputs(); 
								} 
								else
								{
									for (int k = 0; k < inputParameters.Count; k++) 
										if (inputParameters[k].DependsOn != null && ! (inputParameters[k].DependsOn.parametricObject.generator is AXTool)) 
											inputParameters[k].DependsOn.parametricObject.isOpen = true;
								}

								//pB.parametricObject.isOpen = false;
							}
							else
							{

								po.startStowInputs();

								po.inputControls.isOpen = false;
							} 

							e.Use();
						}
					}
				}



				// \ PO.INPUT_CONTROLS

			}

			GUI.color = oldColor;
		}
	}





	//function selectPO (AXParametricObject)

















	void Callback () {
		Debug.Log ("Selected: ");
	}





	#region CLICKERS
	public void ClickOnPO(AXParametricObject po)
	{

//		Debug.Log("CLICK_ON PO: mostRecentLogicRect.Contains(Event.current.mousePosition)="+mostRecentLogicRect.Contains(Event.current.mousePosition));
//		Debug.Log(Event.current.mousePosition);
//		Debug.Log(GUI.GetNameOfFocusedControl());



		model.selectedParameterInputRelation = null;
		model.selectedRelationInGraph = null;

		mouseIsDownOnPO 	= po;
		mouseLastDownOnPO 	= po;


		// Event.current.mousePosition is relative to this GUIWindow at this point 
		mouseDownPoint = Event.current.mousePosition;
		dragStart = new Vector2(po.rect.x, po.rect.y)  + mouseDownPoint;// + zoomPosAdjust;

		//Debug.Log(  Event.current.mousePosition + " >>>...... " +  mouseDownPoint);

		// later select and object in the heirarchy a la the selectedConsumerPO
		Selection.activeGameObject = model.gameObject;

		Event e = Event.current;

		// MULTI SELECT SUPPORT
		if (! e.command && ! e.control && ! e.shift && ! model.selectedPOs.Contains(po))
			model.deselectAll();

		if (e.shift)
		{
			//model.selectedPOs.Clear();
			model.selectConnected(po);
			EditorUtility.SetDirty(model.gameObject);
		}
		else if (editorState != EditorState.DraggingRubbeband)
		if (! isSelected(po))
			selectPO(po);

		editorState = EditorState.DraggingNodePalette;
		EditorUtility.SetDirty(model.gameObject);

		//GUI.FocusControl ("dummy");  
	}







	public void clearFocus()
	{
		lastFocusedGUIName = "";
		GUI.FocusControl("dummy_label");
	}




	public void ClickOffPOs()
	{


		ArchimatixEngine.setSceneViewState(ArchimatixEngine.SceneViewState.Default);
			
		if (model != null)
		{

			Undo.RegisterCompleteObjectUndo (model, "Selection Changed");


			if (! string.IsNullOrEmpty(lastFocusedGUIName))
			{
				lastFocusedGUIName = "";
				GUI.FocusControl("dummy_label");
				model.autobuild();

			}



			Selection.activeGameObject = model.gameObject;		


			// CHECK IF CLICK ON CONNECTOR CABLES

			//Debug.Log(Event.current.mousePosition-zoomPosAdjust);

			bool clickedOnRelation = false;


			// CHECK NODE CONNECTORS CLICKED?
			for (int i = 0; i < model.parametricObjects.Count; i++) 
			{
				
				AXParametricObject po = model.parametricObjects[i];
				//List AXParameter
				if (po.generator.AllInput_Ps != null)
				{
					
					for (int j=0; j<po.generator.AllInput_Ps.Count; j++)
					{
						
						AXParameter p = po.generator.AllInput_Ps[j];

						if (p.DependsOn != null && p.dependsOnCurveIsVisible)
						{
							//Debug.Log("p.inputPoint="+p.inputPoint);
							Vector2 startPos = p.inputPoint;//new Vector2(po.rect.x, po.rect.y)+p.inputPoint;
							Vector2 endPos =   p.DependsOn.outputPoint;

							//Debug.Log("startPoint = "+p.inputPoint+", endPoint=" + p.DependsOn.outputPoint);
							float 	pdist 			= Vector3.Distance(startPos, endPos);
							Vector3 startTangent 	= startPos + Vector2.left  * (pdist / 3f) ;
							Vector3 endTangent 		= endPos   + Vector2.right * (pdist / 3f);


							float dist = HandleUtility.DistancePointBezier(Event.current.mousePosition-zoomPosAdjust,  startPos, endPos,  startTangent, endTangent);


							if (dist < 8/zoomScale)
							{
								if (model.selectedParameterInputRelation != p)
								{
									model.selectedParameterInputRelation = p;
									clickedOnRelation = true;
									model.deselectAll();
									model.selectedRelationInGraph 			= null;
								}
								else // TOGGLE OFF
									model.selectedParameterInputRelation = null;
							}
						}
					}
				}
			}



			if (! clickedOnRelation)
				model.selectedParameterInputRelation 	= null;




			// CHECK RELATIONS selectedRelation = r;
			//Debug.Log(relationCurves.Count);

			if (! clickedOnRelation)
			{
				for (int i=0; i<relationCurves.Count; i++)
				{
					RelationBezierCurve rbc = relationCurves[i];


					float dist = HandleUtility.DistancePointBezier(Event.current.mousePosition-zoomPosAdjust,  rbc.startPosition, rbc.endPosition,  rbc.startTangent, rbc.endTangent);

					if (dist < 8/zoomScale)
					{
						if (model.selectedRelationInGraph == null || model.selectedRelationInGraph != rbc.relation)
						{
							model.selectedRelationInGraph 	= rbc.relation;
							model.selectedRelation 			= rbc.relation;

							model.selectedRelationInGraph.memorize();

							clickedOnRelation = true;
							model.deselectAll();
						}
						break;
					} 

				}

				if (clickedOnRelation == false)
				{ // TOGGLE OFF
					model.selectedRelation 			= null;
					model.selectedRelationInGraph 	= null;
				}
			}









			if (! clickedOnRelation)
			{
				model.selectedParameterInputRelation 	= null;
				model.selectedRelationInGraph 			= null;

				// SET RUBBERBAND MODE INSTEAD
				//setEditMode( EditorState.DraggingWindowSpace );

				if (! Event.current.shift && model.selectedPOs.Count > 0)
					model.deselectAll();

				setEditMode( EditorState.DraggingRubbeband );
				tmpRubberbandSelectedPOs = new List<AXParametricObject>();

				mouseDownPoint = Event.current.mousePosition;

			}
			InputParameterBeingDragged = null;
			OutputParameterBeingDragged = null;




			model.selectedParameter = null;
			model.selectedRelation = null;

			//if (model != null)
			//	model.deselectAll();

			if (SceneView.lastActiveSceneView != null)
				SceneView.lastActiveSceneView.Repaint();

		}
		//if (draggingThumbnailOfPO == null)
		//editorState = ;
		EditorUtility.SetDirty(model.gameObject);
		Event.current.Use ();
	}





	public void selectPO(AXParametricObject po)
	{
		// This is a selectPO called from the GraphEditor

		//Debug.Log ("NodeGraphEditor::selectPO " + po.Name);
		// Evoking this from the NodeGraphEditor should cause a select in the 
		// select of a GameObject in the SceneView. Or in the minimum, a new consumerMatrix 
		// for the PO based on the last selection.

		// If this is a new node, then the consumerMatrix is identity
		//Debug.Log("selectPO");

		if (po != null && po.model != null && ! po.model.selectedPOs.Contains(po) )
		{
			//Debug.Log("here");
			ArchimatixEngine.setSceneViewState(ArchimatixEngine.SceneViewState.Default);


			po.model.selectPO(po);
			EditorUtility.SetDirty(model.gameObject);
			po.generator.adjustWorldMatrices();
		}

		if (SceneView.lastActiveSceneView != null)
			SceneView.lastActiveSceneView.Repaint();


	}




	public bool isSelected(AXParametricObject po) 
	{
		if (model != null)
			return model.isSelected(po);

		return false;
	}



	#endregion


	#region NODE MENU GUI.WINDOW

	void nodeMenu_window(int win_id)
	{

		GUI.Button(new Rect( 10, 10, 40, 20), "yubba");

	}

	#endregion






	// DO WINDOW
	#region NODE PALETTE GUI.WINDOW


	void DoNodePallette(int win_id)
	{ 
		Color origBG = GUI.backgroundColor;
		
		int po_id = win_id-currentWindowOffset;

		Event e = Event.current;

		//bool mouseIsUpInWindow = false;
		Color currColor;

		if (po_id >= model.parametricObjects.Count)
			return;



		AXParametricObject po = model.parametricObjects[po_id];
		AXParameter p;



		GeneratorHandler generatorHandler = GeneratorHandler.getGeneratorHandler(po);


		//if (po.Name == "plan1") Debug.Log(po.rect + " - " +  mousePosition + " -- " + po.rect.Contains (mousePosition));

		Rect localRectForPO = new Rect(0, 0, po.rect.width, po.rect.height);


		ArchimatixUtils.paletteRect = po.rect;

		// DRAW HIGHLIGHT AROUND SELECTED NODE PALETTE
		if (model.isSelected(po))
		{
			float pad = (EditorGUIUtility.isProSkin) ? 1 : 1;
			Rect outline = new Rect(0, 0, po.rect.width-pad, po.rect.height-pad);
			Handles.color = Color.white;

			Handles.DrawSolidRectangleWithOutline(outline, new Color(1, 1, 1, 0f), ArchimatixEngine.AXGUIColors["NodePaletteHighlightRect"]);
			//Handles.DrawSolidRectangleWithOutline(outline, new Color(.1f, .1f, .3f, .05f), new Color(.1f, .1f, .8f, 1f));
		}




		// EVENT PROCESSING 

		if (e.type == EventType.ContextClick)
		{

			// No need to check mouse in rectangle! 
			// the GUI.Window function call must single out the event for this window.
			//  
		}
		else if ( (e.rawType == EventType.mouseDown ||  e.type == EventType.mouseDown) && localRectForPO.Contains (e.mousePosition))
		{
			//Debug.Log ("do_window: mouseDown: palette rect for "+po.Name+"  contains mouse ");
			//goog
			Vector2 mouseClickOnPOGlobal = (po.rect.position-model.focusPointInGraphEditor + e.mousePosition) * model.zoomScale + graphRect.center;

			//Debug.Log("tmp--- " + tmp + " --- " + (tmp+graphRect.center));

			// RELATION EDITOR IS VISIBLE
			if  ((e.rawType == EventType.mouseDown || e.type == EventType.mouseDown) && model.selectedRelationInGraph != null && relationEditorRect.Contains(mouseClickOnPOGlobal) )
			{
				e.Use();
			}
			else
				ClickOnPO(po);

		} 
		else if (e.rawType == EventType.mouseDown && (
			leftSidebarRect.Contains(e.mousePosition) 		|| 
			rightSidebarRect.Contains(e.mousePosition)	))
		{
			//Debug.Log ("do_window: mouseDown: palette rect for "+po.Name+"  contains mouse ");

			clearFocus();

		} 


		else if (e.rawType == EventType.mouseUp)
		{
			mouseIsDownOnPO 		= null;
			draggingThumbnailOfPO 	= null;
			mouseIsDown 			= false;


			// This gets rid of "select all text" when you click on the textarea. Not sure why all text was being selected.
			if (textAreaIsInFocus() && mouseLastDownOnPO != null && po.codeWindowRectLocal.Contains(Event.current.mousePosition))
			{
				e.Use();
			}
		

		}
		else if (e.rawType == EventType.mouseDrag)
		{
			if (editorState == EditorState.DraggingNodePalette)
			{


				if (mouseIsDownOnPO != null)
				{
					
					//mouseIsDownOnPO.rect.x = snapValue(mouseIsDownOnPO.rect.x, 250f);
					//Debug.Log(mouseIsDownOnPO.rect);

					// re-color background

					//if (mouseIsDownOnPO.grouper != null && model.currentWorkingGroupPO != null)
					//	Debug.Log(mouseIsDownOnPO.Name + " :: "+ (mouseIsDownOnPO.rect.x < model.currentWorkingGroupPO.rect.x));
				}
			}
		}






		// get serialized property for this po
		// then drill down to parameters for this property



		Rect headerRect = new Rect(0, 0, po.rect.width, po.rect.height);

		if ((e.button == 0) && (e.type == EventType.MouseDown) && headerRect.Contains(e.mousePosition)) {

			if ( po.startRect != po.rect)
				Undo.RegisterCompleteObjectUndo (model, "ParametricObject Drag");

			po.startRect = po.rect;
		}

		Color defaultColor = GUI.color;









		// START LAYOUT OF INNER PALETTE


		// Horizontal layput
		float winMargin = ArchimatixUtils.indent;
		float innerWidth = po.rect.width - 2*winMargin;



		int x1 = 10;
		int x2 = 20;

		// vertical layut
		int cur_y 	= 25;
		int gap 	= ArchimatixUtils.gap;
		int lineHgt = ArchimatixUtils.lineHgt;

		if (EditorGUIUtility.isProSkin)
		{
			GUI.color = po.generator.GUIColorPro;
			GUI.backgroundColor = Color.Lerp(po.generator.GUIColorPro, Color.white, .5f) ;
		}
		else
		{
			GUI.color = po.generator.GUIColor;
			GUI.backgroundColor = Color.Lerp(po.generator.GUIColor, Color.white, .5f) ;

		}


		//GUI.backgroundColor = Color.white;

		GUI.Box(new Rect(winMargin, lineHgt, innerWidth, po.outputEndHeight), "");
		GUI.Box(new Rect(winMargin, lineHgt, innerWidth, po.outputEndHeight), "");

		if (isSelected(po))
		{
			GUI.Box(new Rect(winMargin, lineHgt, innerWidth, po.outputEndHeight), "");
			GUI.Box(new Rect(winMargin, lineHgt, innerWidth, po.outputEndHeight), "");
		}
		GUI.color = defaultColor;





		GUIStyle buttonStyle = GUI.skin.GetStyle ("Button");


		buttonStyle.alignment = TextAnchor.MiddleLeft;
		buttonStyle.fontSize  = 12;
		buttonStyle.fixedWidth = 0;
		buttonStyle.fixedHeight = 0;

		//style.onHover.textColor = Color.cyan;
		GUIStyle styleTextField = GUI.skin.GetStyle ("TextField");


		int editButtonWid = (int) (po.rect.width - 2*x1)/2 -6;

		 
		// TITLE
		//Debug.Log ("Adding title " + po.rect);
		//if (poProperty != null) 
		if (true) 
		{
			if (po.isEditing)
			{
				string tname = po.Name.Trim();
				//tname = "bear";
				GUI.SetNextControlName("title_Text_"+po.Guid); 
				tname = EditorGUI.TextField(new Rect(winMargin, cur_y, innerWidth, lineHgt), tname);				

				po.Name = tname.Trim();

				cur_y += lineHgt + gap;



			} 
			else 
			{
				
				if (GUI.Button(new Rect(x1, cur_y, innerWidth-lineHgt*2-6, lineHgt*2), po.Name))
				{				
					po.isEditing = true;
					for (int i=0; i<po.parameters.Count; i++) {
						p =  po.parameters[i];
						p.isEditing = false;
					}
				}


				if ( ArchimatixEngine.nodeIcons.ContainsKey(po.Type))
					GUI.DrawTexture(new Rect(x1+innerWidth-lineHgt*2-4, cur_y, lineHgt*2, lineHgt*2), ArchimatixEngine.nodeIcons[po.Type], ScaleMode.ScaleToFit, true, 1.0F);


				cur_y += lineHgt + 2*gap;



			}





			if (po.isEditing)
			{
				buttonStyle.fontSize  = 12;


				cur_y += lineHgt + gap + 5;

				// DOCUMENTATION_URL (w/o domain)
				GUI.Label(new Rect(winMargin, cur_y, innerWidth-40, lineHgt), "Documentation URL");
				if (GUI.Button(new Rect(innerWidth-20, cur_y, 40, lineHgt), "Def"))
				{
					po.documentationURL = (po.is2D() ? "2d" : "3d")+"library/"+po.Name.ToLower();
				}
				cur_y += lineHgt + gap + 5; 

				GUI.SetNextControlName("documentationURL_Text_"+po.Guid);
				po.documentationURL = EditorGUI.TextField(new Rect(winMargin, cur_y, innerWidth, lineHgt), po.documentationURL);				


				cur_y += lineHgt + gap + 5; 


				AXEditorUtilities.assertFloatFieldKeyCodeValidity("sortval_" + po.Name);

				EditorGUI.BeginChangeCheck ();
				GUI.SetNextControlName("sortval_Text_" + po.Name);
				po.sortval = EditorGUI.FloatField(new Rect(x1+10, cur_y, 100,lineHgt), po.sortval);
				if(EditorGUI.EndChangeCheck())
				{
					//Debug.Log("changed");
					ArchimatixEngine.library.sortLibraryItems();
				}
				cur_y += lineHgt;


				// DONE
				if (GUI.Button (new Rect(x1, cur_y, innerWidth,lineHgt), "Done" ))
					po.doneEditing();

				cur_y += lineHgt + gap + 5;

			}

		}

		buttonStyle.fontSize  = 12;
		styleTextField.fontSize = 12;





		cur_y += 2*gap;







		//GUI.DrawTexture(new Rect(x1, cur_y, width, width), model.rt, ScaleMode.ScaleToFit, true, 1.0F);


		Color prevGUIColor = GUI.color;
		Color gcol = GUI.color;


		GUIStyle labelstyle	 	= GUI.skin.GetStyle("Label");
		labelstyle.fixedWidth = po.rect.width-x1-22;
		Rect tooltipRect = new Rect(x1+10,  cur_y+16, po.rect.width-x1-10, 16);





		// DO INFO BUTTON --
		Rect infoRect = new Rect(po.rect.width-x1-38, cur_y, 16, 16);


		// TOOLTIP
		if (infoRect.Contains(Event.current.mousePosition)) 
		{
			gcol.a = 1f;
			GUI.color = gcol;
			labelstyle.alignment = TextAnchor.MiddleRight;
			GUI.Label (tooltipRect, ("About " + System.IO.Path.GetExtension(po.generator.GetType ().ToString ()).Substring(1) ));
		}
		else
			gcol.a = .6f;
		GUI.color = gcol;



		if (GUI.Button ( infoRect, infoIconTexture, GUIStyle.none))
		{
			string typename = System.IO.Path.GetExtension(po.generator.GetType ().ToString ().TrimStart('.'));

			if (! string.IsNullOrEmpty(po.documentationURL))
				Application.OpenURL("http://"+ArchimatixEngine.doucumentationDomain+"/"+po.documentationURL); 
			else
				Application.OpenURL("http://www.archimatix.com/nodes/"+typename.ToLower()); 
		}


		// DO NODE MENU
		Rect poMenuRect = new Rect(po.rect.width-x1-20, cur_y, 16, 16);

		// TOOLTIP
		if (poMenuRect.Contains(Event.current.mousePosition)) // TOOLTIP
		{
			gcol.a = 1f;
			GUI.color = gcol;
			labelstyle.alignment = TextAnchor.MiddleRight;
			GUI.Label (tooltipRect, ("Node Menu"));
		}
		else
			gcol.a = .8f;
		GUI.color = gcol;

		//GUI.Label(poMenuRect, menuIconTexture);
		if (GUI.Button ( poMenuRect, menuIconTexture, GUIStyle.none))
		{

			bool isMac = SystemInfo.operatingSystem.Contains("Mac");

			GenericMenu menu = new GenericMenu ();

			//menu.AddSeparator("Library ...");


			menu.AddItem(new GUIContent("Save to Library             " + ((isMac) ? "⌘L" :  "Ctrl+L") ), false, LibraryEditor.doSave_MenuItem, po);
			menu.AddItem(new GUIContent("Save to Library Folder        "), false, LibraryEditor.doSave_MenuItem_NewFolder, po);

			menu.AddSeparator("");
			//menu.AddItem(new GUIContent("New ShapeDistributor"), false, splineOutputMenuItem, "ShapeDistributor");


			string editLabel = ((po.isEditing) ? "Done Editing                     ":"Edit                               ") + ((isMac) ? "⌘E" : "Ctrl+E");
			menu.AddItem(new GUIContent(editLabel), false,  () => {

				po.isEditing = !po.isEditing; 
			});



			menu.AddItem(new GUIContent("Copy                             " + ((isMac) ? "⌘C" : "Ctrl+C") ), false, () => {
				EditorGUIUtility.systemCopyBuffer = LibraryEditor.poWithSubNodes_2_JSON(po, true); 
				model.autobuild();
			});

			menu.AddItem(new GUIContent("Instance                       " + ((isMac) ? "⌘D" : "Ctrl+D") ), false,  () => {
				AXEditorUtilities.instancePO(po); 
				model.autobuild();
			});

			/*
			menu.AddItem(new GUIContent("Replicate          ⇧⌘D"), false,  () => {
				AXEditorUtilities.replicatePO(po); 
				model.autobuild();
			});
			*/

			//menu.AddItem(new GUIContent("Replicate"), false, () => {
			//	model.autoBuild();
			//});

			menu.AddItem(new GUIContent("Duplicate              " + ((isMac) ? "⌘C-⌘V" : "Ctrl+C-Ctrl+V") ), false, () => {
				AXEditorUtilities.duplicatePO(po);
				model.autobuild();
			});

			menu.AddItem(new GUIContent("Close Controls"), false,  () => {
				po.closeParameterSets() ;
			});
			//menu.AddSeparator(" ...");
			menu.AddSeparator("");
			//menu.AddItem(new GUIContent("Lock               ⌘L"), false, () => {

			//});

			//menu.AddItem(new GUIContent("Make Inactive"), false, () => {

			//});

			menu.AddItem(new GUIContent("Delete                      " + ((isMac) ? " ⌘⌫" : "Del") ), false,  () => {
				model.deletePO(po); 
				model.remapMaterialTools();

				model.autobuild();
			});
			//e.mousePosition *= model.zoomScale;

			//Vector2 pos = GUIUtility.GUIToScreenPoint(e.mousePosition);
			//menu.DropDown(new Rect(e.mousePosition.x*model.zoomScale, e.mousePosition.y*model.zoomScale, 200, 500));




			//menu.AddSeparator("Transforms...");
			menu.AddSeparator("");
			menu.AddItem(new GUIContent("Reset Transform           "), false,  () => {
				Undo.RegisterCompleteObjectUndo (model, "Rest Transform");
				po.initiateRipple_setFloatValueFromGUIChange("Trans_X", 0);
				po.initiateRipple_setFloatValueFromGUIChange("Trans_Y", 0);
				po.initiateRipple_setFloatValueFromGUIChange("Trans_Z", 0);
				po.initiateRipple_setFloatValueFromGUIChange("Rot_X", 0);
				po.initiateRipple_setFloatValueFromGUIChange("Rot_Y", 0);
				po.initiateRipple_setFloatValueFromGUIChange("Rot_Z", 0);
				po.initiateRipple_setFloatValueFromGUIChange("Scale_X", 1);
				po.initiateRipple_setFloatValueFromGUIChange("Scale_Y", 1);
				po.initiateRipple_setFloatValueFromGUIChange("Scale_Z", 1);
				model.autobuild();
				po.generator.adjustWorldMatrices();

			});
			menu.AddItem(new GUIContent("Reset Position           "), false,  () => {
				Undo.RegisterCompleteObjectUndo (model, "Rest Position");
				po.initiateRipple_setFloatValueFromGUIChange("Trans_X", 0);
				po.initiateRipple_setFloatValueFromGUIChange("Trans_Y", 0);
				po.initiateRipple_setFloatValueFromGUIChange("Trans_Z", 0);
				model.autobuild();
				po.generator.adjustWorldMatrices();
			});
			menu.AddItem(new GUIContent("Reset Rotation           "), false,  () => {
				Undo.RegisterCompleteObjectUndo (model, "Rest Rotation");
				po.initiateRipple_setFloatValueFromGUIChange("Rot_X", 0);
				po.initiateRipple_setFloatValueFromGUIChange("Rot_Y", 0);
				po.initiateRipple_setFloatValueFromGUIChange("Rot_Z", 0);
				model.autobuild();
				po.generator.adjustWorldMatrices();
			});
			menu.AddItem(new GUIContent("Reset Scale           "), false,  () => {
				Undo.RegisterCompleteObjectUndo (model, "Rest Scale");
				po.initiateRipple_setFloatValueFromGUIChange("Scale_X", 1);
				po.initiateRipple_setFloatValueFromGUIChange("Scale_Y", 1);
				po.initiateRipple_setFloatValueFromGUIChange("Scale_Z", 1);
				model.autobuild();
				po.generator.adjustWorldMatrices();
			});


			menu.ShowAsContext ();

			if(e.type != EventType.Repaint && e.type != EventType.Layout) 
				e.Use();


		}

		GUI.color = prevGUIColor;
		labelstyle.alignment = TextAnchor.MiddleLeft;

		cur_y += lineHgt + 2*gap;








		// BASE PARAMETERS
		AXParameter b = null;


		EditorGUI.BeginChangeCheck();
		po.baseControls.isOpen = EditorGUI.Foldout(new Rect(x1, cur_y-20, 30,lineHgt), po.baseControls.isOpen, " ");
		if(EditorGUI.EndChangeCheck())
		{
			doRepaint = true;  
		}

		if (po.baseControls.isOpen)
		{
		// Base (and other) controllers
		if (po.baseControls != null && po.baseControls.children !=null)
		{
			for (int i=0; i<po.baseControls.children.Count; i++) {
				
				b =  po.baseControls.children[i] as AXParameter;

				if (b.PType != AXParameter.ParameterType.None && b.PType != AXParameter.ParameterType.Base)
					continue;
							
				// these points are world, not relative to the this GUIWindow
				b.inputPoint 	= new Vector2( po.rect.x, 					po.rect.y+cur_y+lineHgt/2);
				b.outputPoint 	= new Vector2( po.rect.x+po.rect.width, 	po.rect.y+cur_y+lineHgt/2);
				
				Rect pRect = new Rect(x1, cur_y, innerWidth, lineHgt);

				try {
					int hgt = ParameterGUI.OnGUI(pRect, this, b);
					cur_y += hgt + gap;
				} catch {
					
				} 
			}
		}

		cur_y += lineHgt + 2*gap;
		}







		// CUSTOM NODE GUI ZONE_1
		if (generatorHandler != null)
			cur_y = generatorHandler.customNodeGUIZone_1(cur_y, this, po);







		// DO INPUTS
		ArchimatixUtils.cur_x = ArchimatixUtils.indent;


		if (po.inputControls != null)
		{



			// INPUT_CONTROL CONNECT BUTTON 
			if (! (po.generator is ShapeDistributor))
			{

				if ( ! po.inputControls.isOpen )
				{
					if (GUI.Button (new Rect (-3, cur_y, lineHgt, lineHgt), "")) {
						if (OutputParameterBeingDragged != null)
						{
							if (OutputParameterBeingDragged.parametricObject.is2D())
							{
								if (po.shapes != null && po.shapes.Count > 0)
								{
									// for now, just add to first shape
									AXParameter newp = po.shapes[0].addInput();
									newp.inputPoint = new Vector2( po.rect.x, po.rect.y+cur_y+lineHgt/2);
									newp.makeDependentOn(OutputParameterBeingDragged);
									OutputParameterBeingDragged = null;
									model.autobuild();
								}
								else 
								{
									List<AXParameter> inputs = po.getAllInputShapes();
									if (inputs.Count == 1)
									{
										inputs[0].makeDependentOn(OutputParameterBeingDragged);
										inputs[0].inputPoint = new Vector2( po.rect.x, po.rect.y+cur_y+lineHgt/2);
										OutputParameterBeingDragged = null;
										model.autobuild();
										if (po.geometryControls != null)
											po.geometryControls.isOpen = true;
									}
									else
									{
										inputsInputSocketClicked (po);
									}
								}

							}
							else if (OutputParameterBeingDragged.parametricObject.is3D())
							{
								if (po.generator is Grouper)
								{
									AXParameter new_p = po.addInputMesh();
									new_p.makeDependentOn(OutputParameterBeingDragged);
									OutputParameterBeingDragged = null;
									model.autobuild();
								}
								else 
								{
									inputsInputSocketClicked (po);
								}

							}

						}
						else
							inputsInputSocketClicked (po);

					}
				}
			}



			// INPUT_CONTROLS FOLDOUT TRINGLE


			if (po.generator is ShapeDistributor)
				po.inputControls.isOpen = true;


			if (! (po.generator is ShapeDistributor))
			{
				EditorGUI.BeginChangeCheck();
				po.inputControls.isOpen = EditorGUI.Foldout(new Rect(x1, cur_y, 30,lineHgt), po.inputControls.isOpen, " ");
				if(EditorGUI.EndChangeCheck())
				{
					doRepaint = true;  
				}
			}
			//GUI.color = Color.white;


			// INPUT_CONTROLS FOLDOUT BUTTON
			if (! (po.generator is ShapeDistributor))
			{

				if (GUI.Button(new Rect(x1+10, cur_y, 100,lineHgt), "Inputs"))
				{
					if (! po.inputControls.isOpen)
					{
						//po.closeParameterSets();
						po.inputControls.isOpen = true;
					}
					else
						po.inputControls.isOpen = false;
				}
				po.inputControls.inputPoint 		= new Vector2( po.rect.x, 					po.rect.y+cur_y+lineHgt/2);
				po.inputControls.outputPoint 		= new Vector2( po.rect.x+po.rect.width, 	po.rect.y+cur_y+lineHgt/2);



				if (! po.inputControls.isOpen)
				{
					if (po.shapes != null)
					{

						foreach(AXShape shp in po.shapes)
						{
							foreach(AXParameter sp in  shp.inputs)
								sp.inputPoint 	=  new Vector2( po.rect.x, 					po.rect.y+cur_y+lineHgt/2);
						}
					}


				}

				cur_y += lineHgt + 2*gap;
			}





















			if (po.inputControls.isOpen)
			{


				// INPUT SHAPES
				Color backgroundColor = GUI.backgroundColor;
				//Archimatix.indentLevel++;
				if (po.shapes != null)
				{
					// A SHAPE -- move this into AXShape.OnGUI()
					foreach(AXShape shp in po.shapes)
					{
						Rect pRect = new Rect(winMargin+ArchimatixUtils.indent, cur_y, innerWidth, lineHgt);
						cur_y = ShapeGUI.OnGUI(pRect, this, shp);
					}
				}
				//Archimatix.indentLevel--;
				GUI.backgroundColor = backgroundColor;





				// INPUT ITEMS
				// Parameter Lines

				//for (int i=0; i<po.parameters.Count; i++) {
				if (po.inputControls != null && po.inputControls.children != null)
				{
					for (int i=0; i<po.inputControls.children.Count; i++) {
						p =  (AXParameter) po.inputControls.children[i];


						if (p.PType != AXParameter.ParameterType.Input)
							continue;


						//if ( p.DependsOn != null && !p.DependsOn.Parent.isOpen && ! p.Name.Contains ("External"))
						//	continue;


						//if (parametricObjects_Property != null) 
						if (model.parametricObjects != null) 
						{
							// these points are world, not rlative to the this GUIWindow
							p.inputPoint 	= new Vector2( po.rect.x, 					po.rect.y+cur_y+lineHgt/2);
							p.outputPoint 	= new Vector2( po.rect.x+po.rect.width, 	po.rect.y+cur_y+lineHgt/2);


							Rect pRect = new Rect(x1, cur_y, innerWidth, 2*lineHgt);

							try {
								//if (parameters_Property.arraySize > i)
								//{


								if (p.Type == AXParameter.DataType.Spline)
									cur_y = ParameterSplineGUI.OnGUI_Spline(pRect, this, p);

								else if	(p.Type == AXParameter.DataType.Generic)
								{						
									int hgt = ParameterGUI.OnGUI( pRect, this, p);
									cur_y += hgt + gap;

								}
								else if (p.Type == AXParameter.DataType.Mesh)
									cur_y = ParameterMeshGUI.OnGUI_Mesh(pRect, this, p);

								else if (p.Name.Contains("Material") || (p.DependsOn != null && p.DependsOn.parametricObject.generator is MaterialTool))
									cur_y = ParameterToolGUI.display(pRect, this, p);
								else if (p.Name.Contains("Repeater"))
									cur_y = ParameterToolGUI.display(pRect, this, p);
								else if (p.Name.Contains("Jitter"))
									cur_y = ParameterToolGUI.display(pRect, this, p);
								else
									cur_y += ParameterGUI.OnGUI(pRect, this, p) + gap;
								//}



							} 
							catch
							{

								//Debug.Log ("INPUT : " + p.Name + " FAILED");
							}

							if (po.generator is RepeaterBase)
							{

							}


						}
					}
				}











				// MESH INPUT LIST

				if ( po.useMeshInputs && po.meshInputs != null && ! po.isCurrentGrouper())
				{			

					// Empty / New SHAPE PARAMETER

					GUI.backgroundColor = getDataColor(AXParameter.DataType.Mesh);

					if (OutputParameterBeingDragged != null && OutputParameterBeingDragged.Type != AXParameter.DataType.Mesh)
					{
						GUI.enabled = false;
					}
					if (GUI.Button (new Rect (-3, cur_y, lineHgt, lineHgt), "")) {

						AXParameter new_p = po.addInputMesh();
						inputSocketClicked (new_p);
						OutputParameterBeingDragged = null;

					}
					GUI.enabled = true;

					Rect boxRect = new Rect(x1+10, cur_y, innerWidth-20,lineHgt);//new Rect (x1 +11, cur_y, width - 38, lineHgt);
					GUI.Box (boxRect, " ");
					GUI.Box (boxRect, " ");
					//boxRect.x += 10;
					GUI.Label (boxRect, "Empty Input");


					cur_y +=  lineHgt + gap;

				}

				cur_y +=  lineHgt + gap;
			}

		}





		//GUI.backgroundColor = Color.magenta; 







		// CUSTOM NODE GUI ZONE_2
		if (generatorHandler != null)
			cur_y = generatorHandler.customNodeGUIZone_2(cur_y, this, po);







		// DO TRANSFORMATIONS
		if (! (po.generator is ShapeDistributor))
		{

			GUI.color = Color.white;
			GUI.backgroundColor = Color.white;

			if (po.positionControls != null)
			{
				EditorGUI.BeginChangeCheck();


				po.positionControls.isOpen = EditorGUI.Foldout(new Rect(x1, cur_y, 30,lineHgt), po.positionControls.isOpen, " ");
				if(EditorGUI.EndChangeCheck())
				{
					doRepaint = true;
				}
				//GUI.color = Color.white;





				// TRANSFORMATIONS BUTTON

				GUI.backgroundColor = Color.clear;
				buttonStyle.alignment = TextAnchor.LowerLeft;
				if (GUI.Button(new Rect(x1+10, cur_y, 80,lineHgt+1), "Transform"))
				{
					if (! po.positionControls.isOpen)
					{
						if (po.geometryControls != null)
							po.geometryControls.isOpen = false;

						po.positionControls.isOpen = true;
					}
					else
						po.positionControls.isOpen = false;
				}

				GUI.backgroundColor = Color.white;
				buttonStyle.alignment = TextAnchor.MiddleLeft;



				// SWITCH AXIS BUTTON
				int axisInt =  (int) po.generator.axis;
				//Debug.Log(po.Name + ": " + axisInt + " -- " + po.generator.axis);



				buttonStyle.alignment = TextAnchor.MiddleCenter;
				if (GUI.Button(new Rect(x1+90, cur_y, 50,lineHgt), "" + po.generator.axis))
				{
					int next = (axisInt == 6) ? 0 : axisInt+1;
					po.intValue("Axis", next);
					model.autobuild();
					po.generator.adjustWorldMatrices();
						
				}
				buttonStyle.alignment = TextAnchor.MiddleLeft;

				po.positionControls.inputPoint 		= new Vector2( po.rect.x, 					po.rect.y+cur_y+lineHgt/2);
				po.positionControls.outputPoint 	= new Vector2( po.rect.x+po.rect.width, 	po.rect.y+cur_y+lineHgt/2);




				cur_y +=  lineHgt + gap;

				EditorGUI.BeginChangeCheck();
				if (po.positionControls.isOpen)
				{
					cur_y +=  gap;

					for (int i=0; i<po.positionControls.children.Count; i++) {

						p =  (AXParameter) po.positionControls.children[i];

						//if (po.parameters[i].PType != AXParameter.ParameterType.PositionControl)
						//	continue;

						// these points are world, not rlative to the this GUIWindow
						p.inputPoint 	= new Vector2( po.rect.x, 					po.rect.y+cur_y+lineHgt/2);
						p.outputPoint 	= new Vector2( po.rect.x+po.rect.width, 	po.rect.y+cur_y+lineHgt/2);

						//if (parameters_Property != null && parameters_Property.arraySize > i)
						if (p != null )
						{
							int hgt = ParameterGUI.OnGUI(new Rect(x1, cur_y, innerWidth, lineHgt), this, p);
							cur_y += hgt + gap;
						}
					}
				}
				if(EditorGUI.EndChangeCheck())
				{

				}

			}

		}


	

		float thumbSize = 16;

		if (po.generator is PrefabInstancer)
		{
			EditorGUI.BeginChangeCheck ();

			po.prefab = (GameObject) EditorGUI.ObjectField(new Rect(x2, cur_y, innerWidth-2*thumbSize, lineHgt), po.prefab, typeof(GameObject), true);
			if (EditorGUI.EndChangeCheck ()) {
				Undo.RegisterCompleteObjectUndo (model, "Prefab");

				model.autobuild();

			}
			cur_y += lineHgt + gap;
		}









		// TEXTURES

		// MATERIAL


		//if ((po.is3D() && ! po.generator.isOfInterface("IReplica")) || po.generator is MaterialTool)
		// MATERIAL SELECTION
		if (po.generator is MaterialTool)
		{
			

			if (po.axMat == null) 
				po.axMat = new AXMaterial();

			if (po.axMat.mat == null && po.Mat != null)
				po.axMat.mat = po.Mat;

			// MATERIAL OBJECT FIELD
			EditorGUI.BeginChangeCheck ();
			po.axMat.mat = (Material) EditorGUI.ObjectField(new Rect(x2+5, cur_y, innerWidth-2*thumbSize, lineHgt), po.axMat.mat, typeof(Material), true);
			if (EditorGUI.EndChangeCheck ()) {
				Undo.RegisterCompleteObjectUndo (model, "Material");
				model.remapMaterialTools();
				model.autobuild();  
			}
			if(po.axMat.mat != null && po.axMat.mat.mainTexture != null)
				GUI.DrawTexture(new Rect(x2+(innerWidth-2*thumbSize), cur_y, thumbSize, thumbSize),   po.axMat.mat.mainTexture, ScaleMode.ScaleToFit, false, 1.0F);
			
			cur_y += lineHgt + gap;



			// PHYSICS MATERIAL
			po.axMat.showPhysicMaterial = EditorGUI.Foldout(new Rect(x1, cur_y, 30,lineHgt), po.axMat.showPhysicMaterial, "Physics");
			cur_y += lineHgt + gap;

			if (po.axMat.showPhysicMaterial)
			{

				po.axMat.physMat = (PhysicMaterial) EditorGUI.ObjectField(new Rect(x2+5, cur_y, innerWidth-2*thumbSize, lineHgt), po.axMat.physMat, typeof(PhysicMaterial), true);
				cur_y += lineHgt + gap;


				EditorGUIUtility.labelWidth = 50;
				//labelstyle.fixedWidth = 100;
				//labelstyle.alignment = 	TextAnchor.MiddleLeft;
										

					
				EditorGUI.BeginChangeCheck ();
				po.axMat.density = EditorGUI.FloatField(new Rect(x2+5, cur_y, 100,lineHgt), "Density",  po.axMat.density);

				if (EditorGUI.EndChangeCheck ()) {
				Undo.RegisterCompleteObjectUndo (model, "Material Density");
					model.autobuild();  
				}
				cur_y += lineHgt + gap;

			}
		}



		// TERRAIN INPUT
		if (po.generator is RepeaterBase || po.generator is PlanRepeater || po.generator is TerrainDeformer || po.generator is ITerrainer)
		{
			EditorGUI.BeginChangeCheck ();
		
			po.terrain = (Terrain) EditorGUI.ObjectField(new Rect(x2, cur_y, innerWidth-2*thumbSize, lineHgt), po.terrain, typeof(Terrain), true);
			if (EditorGUI.EndChangeCheck ()) {
				Undo.RegisterCompleteObjectUndo (model, "Terrain");

				model.autobuild();

			}
			cur_y += lineHgt + gap;

			if (po.generator is Lander)
			{

				if (GUI.Button(new Rect(x2, cur_y, innerWidth-2*thumbSize, lineHgt), "Memorize"))
				{
					Debug.Log("Memorize");

					((Lander) po.generator).memorizeTerrain();
				}
				cur_y +=  lineHgt + gap;
			}

		}

		cur_y +=  gap;






		// CUSTOM NODE GUI ZONE_3
		if (generatorHandler != null)
			cur_y = generatorHandler.customNodeGUIZone_3(cur_y, this, po);




		// DO GEOMETRY CONTROLS
		if ( ! (po.generator is Instance))
		{

			if (po.geometryControls != null)
			{
				// FOLDOUT 

				if (po.geometryControls.children.Count == 1)
				{
					po.geometryControls.isOpen = true;
				}
				else
				{
					EditorGUI.BeginChangeCheck();
					po.geometryControls.isOpen = EditorGUI.Foldout(new Rect(x1, cur_y, 30,lineHgt), po.geometryControls.isOpen, " ");
					if(EditorGUI.EndChangeCheck())
					{
						doRepaint = true;
					}
				//GUI.color = Color.white;



					// GEOMETRY BUTTON

					GUI.backgroundColor = Color.clear;
					buttonStyle.alignment = TextAnchor.LowerLeft;
					if (GUI.Button(new Rect(x1+10, cur_y, 100, lineHgt+1), ((po.is2D()) ? "Geometry":"Controls")))
					{
						if (! po.geometryControls.isOpen)
						{
							if (po.positionControls != null)
								po.positionControls.isOpen = false;
							po.geometryControls.isOpen = true;
						}
						else
							po.geometryControls.isOpen = false;
					}
					GUI.backgroundColor = Color.white;
					buttonStyle.alignment = TextAnchor.MiddleLeft;


					po.geometryControls.inputPoint 		= new Vector2( po.rect.x, 					po.rect.y+cur_y+lineHgt/2);
					po.geometryControls.outputPoint 	= new Vector2( po.rect.x+po.rect.width, 	po.rect.y+cur_y+lineHgt/2);

					cur_y +=  lineHgt+2*gap;
				}


				// CONTROL PARAMETERS
				if ( (po.geometryControls != null && po.geometryControls.isOpen)   || po.revealControls)
				{



					// GEOMETRY_CONTROLS_ON_GUI

					//cur_y = gh.GeometryControlsOnGUI(cur_y, this);
					cur_y = GeometryControlsGui.GeometryControlsOnGUI(cur_y, this, po);




					// DO HANDLES
					if (po.is2D() || po.generator is Grouper)
					{
						EditorGUI.BeginChangeCheck();
						currColor = GUI.color;
						GUI.color = new Color(1,1,1,.6f);
						po.showHandles = EditorGUI.Foldout(new Rect(x1, cur_y, 100,lineHgt), po.showHandles, "Handles");
						GUI.color = currColor;
						if(EditorGUI.EndChangeCheck()) 
							doRepaint = true;

						cur_y +=  lineHgt;
						AXHandle han;
						if (po.showHandles)
						{
							//po.showSubpartHandles = EditorGUI.Toggle(new Rect(x2, cur_y, po.rect.width*1.3f, lineHgt), new GUIContent("Show handles:", "Allow the direct editing of subparts."), po.showSubpartHandles);

							cur_y += gap*2;

							if (po.handles == null)
								po.handles = new List<AXHandle>();

							for (int i=0; i<po.handles.Count; i++) {
								han =  po.handles[i];
								if (han != null)
								{
									Rect pRect = new Rect(x2, cur_y, innerWidth,lineHgt);

									// HANDLE
									EditorGUI.BeginChangeCheck();
									int hgt = AXHandleGUI.OnGUI(han, pRect, this);
									if(EditorGUI.EndChangeCheck())
									{
										Undo.RegisterCompleteObjectUndo (model, "Handle Edit");

										Event.current.Use ();
										if (po.rect.width < 250)
											po.rect.width = 250;

									}
									cur_y += hgt + gap;
								}
							}

							if (GUI.Button(new Rect(x2, cur_y, lineHgt*1.25f, lineHgt), new GUIContent("+", "Create a new Handle")))
							{
								Undo.RegisterCompleteObjectUndo (model, "New AXHandle");
								//Debug.Log ("ADDING A HANDLE");
								AXHandle tmpH = po.addHandle ();

								tmpH.isEditing = true;
								po.model.cleanGraph();
								tmpH.Name = "";
							}
							if (po.isEditing)
							{
								if (GUI.Button (new Rect(x1+editButtonWid+6, cur_y, editButtonWid,lineHgt), "Done" ))
									po.doneEditing();



							}
							cur_y += lineHgt + gap + 5;

						}
					}

					/*
					if (EditorGUIUtility.isProSkin)
					{
						GUI.color = Color.Lerp(po.generator.GUIColorPro, Color.white, .7f);//po.generator.GUIColorPro;
						GUI.backgroundColor = Color.Lerp(po.generator.GUIColorPro, Color.white, .7f) ;
					}
					else
					{
						GUI.color = Color.Lerp(po.generator.GUIColor, Color.white, .8f);
						GUI.backgroundColor = Color.Lerp(po.generator.GUIColor, Color.white, .6f) ;

					}
					*/

				}

			}





			// LOGIC WINDOW
			GUI.backgroundColor = origBG;
			//GUI.skin.settings.selectionColor = new Color(.7f,.7f,.7f);
			if ( (po.geometryControls != null && po.geometryControls.isOpen )  || po.revealControls || (po.generator is ILogic))
			{
				

				// DO LOGIC
				if (po.is2D() || (po.generator is Channeler))
				{
					

					GUIStyle codestyle = EditorStyles.textField;
					codestyle.fontSize  = 12;
					int codeLineHgt = 14;

					if ((Event.current.type ==  EventType.KeyDown) && GUI.GetNameOfFocusedControl().Contains("logicTextArea_") && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter))
					{
						po.codeScrollPosition.y += codeLineHgt;

					}





					EditorGUI.BeginChangeCheck();
					po.showLogic = EditorGUI.Foldout(new Rect(x1, cur_y, 100,lineHgt), po.showLogic, "Logic");
					if(EditorGUI.EndChangeCheck())
					{
						doRepaint = true;
						if (po.showLogic && po.rect.width < 250)
							po.rect.width = 250;

					}

					if(po.showLogic)
					{


						// REFRESH BUTTON
	//					if (po.showLogic && po.codeIsDirty == true) 
	//					{
							GUI.SetNextControlName("RefreshButton_" + po.Guid);
							if (GUI.Button(new Rect(innerWidth-100, cur_y, 100, lineHgt), "Refresh")  )  
							{	

								po.codeIsDirty = false;
								model.autobuild();
							}

						//}


						 
						cur_y += lineHgt + gap;
						//Rect codeRect = GUILayoutUtility.GetLastRect();


						if (! String.IsNullOrEmpty(po.codeWarning))
						{
							//Debug.Log ("CODE WARNING: "+po.Name + " ... " + po.codeWarning);
							GUI.Label(new Rect(x1, cur_y, po.rect.width,lineHgt), po.codeWarning);
							cur_y += lineHgt + gap;
						}

						//Rect logicRect;

						//TEXTAREA

						if (po.codeWindowHeight < 100 && po.showLogic) 
							po.codeWindowHeight = 100;

						int codeLineCount = po.codeLineCount();
						int codeHgt = (codeLineCount > 6) ? codeLineCount*codeLineHgt : 6*codeLineHgt;

						//if (codeHgt < po.codeWindowHeight) 
						//	codeHgt = po.codeWindowHeight;

						//if (po.codeWindowHeight  < codeHgt) 
						//	po.codeWindowHeight = codeHgt;

						//Debug.Log("po.codeWindowHeight="+po.codeWindowHeight);
						Rect codeScrollWindowRect = new Rect(po.rect.x+x1, po.rect.y+cur_y, innerWidth, po.codeWindowHeight);

						// SCROLLVIEW
						//GUI.BeginGroup(new Rect(x1, cur_y, innerWidth, po.codeWindowHeight));

						mostRecentLogicRectLocal = new Rect(x1, cur_y, innerWidth, po.codeWindowHeight);
						po.codeWindowRectLocal = new Rect(x1, cur_y, innerWidth, po.codeWindowHeight);

						GUI.SetNextControlName("logicTextArea_Text_" + po.Guid);
						po.codeScrollPosition = GUI.BeginScrollView(new Rect(x1, cur_y, innerWidth, po.codeWindowHeight), po.codeScrollPosition, new Rect(0, 0, innerWidth-20, codeHgt));
						//po.codeScrollPosition = EditorGUILayout.BeginScrollView(po.codeScrollPosition);

						EditorGUI.BeginChangeCheck();
						GUI.SetNextControlName("logicTextArea_Text_Scrollview_" + po.Guid);

						// TEXTAREA
						po.code = EditorGUI.TextArea(new Rect(0, 5, innerWidth, Mathf.Max(po.codeWindowHeight-3, codeHgt)), po.code );

						//po.code = EditorGUILayout.TextArea(po.code,  GUILayout.Width(innerWidth) );
						if(EditorGUI.EndChangeCheck())  
						{
							Undo.RegisterCompleteObjectUndo (model, "Logic Text");

							// ** [complimets of S.Darkwell to fix copy and paste issue 2016.06.01]
							po.code = Regex.Replace(po.code, "[\r]","");
							// ** **
//
//
//							//Debug.Log("TEXT CHANGE");
//							// set this text area as hot rect not to generate onmouseup
//
//
							mostRecentLogicRect = codeScrollWindowRect;
//							//Debug.Log (mostRecentLogicRect);
//							codeChanged = true;
							po.codeIsDirty = true;
						} 
						else
							codeChanged = false;

						GUI.EndScrollView(); 
						//GUI.EndGroup();

						//logicRect = new Rect(x1, cur_y, innerWidth, lineHgt*20);


//						TextEditor te = GUIUtility.GetStateObject((TextEditor), GUIUtility.keyboardControl);
//						te.SelectCurrentParagraph();

						cur_y += po.codeWindowHeight + gap;


						//cur_y += lineHgt + gap;
					}
					// LOGIC


				}
			}
			// LOGIC AREA: WINDOW RESIZE BUTTON
			Rect codeResizeRect = new Rect(po.rect.width-x1-11, cur_y, x1, x1);
			if (e.type == EventType.mouseDown && codeResizeRect.Contains(e.mousePosition))
			{
				Undo.RegisterCompleteObjectUndo (model, "GUI LOGI Window Resize");
				editorState = EditorState.DragResizingLogicWindow;	
				DraggingParameticObject = po;	

			}
			GUI.Button ( codeResizeRect, resizeCornerTexture, GUIStyle.none);

			cur_y += lineHgt + gap;

		}

		cur_y += gap;





		// CUSTOM NODE GUI ZONE_4
		if (generatorHandler != null)
			cur_y = generatorHandler.customNodeGUIZone_4(cur_y, this, po);









		// DO OUPUT GENERATED

		// Parameter Line



		// SHAPE MERGER SHAPES

		//Archimatix.indentLevel++;
		if (po.shapes != null)
		{
			//Rect lRect = new Rect(x1+11, cur_y, 50 , lineHgt);

			// A SHAPE -- move this into AXShape.OnGUI()
			foreach(AXShape shp in po.shapes)
			{
				Rect pRect = new Rect(winMargin+ArchimatixUtils.indent, cur_y, innerWidth, lineHgt);
				cur_y = ShapeGUI.displayOutput(pRect, this, shp);
			}
		}
		//Archimatix.indentLevel--;
		//GUI.backgroundColor = backgroundColor;


		//for (int i=0; i<po.parameters.Count; i++) {



		if (po.outputsNode != null)
		{
			for (int i=0; i<po.outputsNode.children.Count; i++) {

				p =  (AXParameter) po.outputsNode.children[i];

				if (p == null)
					continue;

				//if (p.hasInputSocket || ! p.hasOutputSocket)
				if (p.PType != AXParameter.ParameterType.Output)
					continue;


				//if (parametricObjects_Property != null)
				if (model.parametricObjects != null)
				{
					// these points are world, not relative to the this GUIWindow
					p.inputPoint 	= new Vector2( po.rect.x, 					po.rect.y+cur_y+lineHgt/2);
					p.outputPoint 	= new Vector2( po.rect.x+po.rect.width, 	po.rect.y+cur_y+lineHgt/2);

					Rect pRect = new Rect(x1, cur_y, innerWidth, lineHgt);

					//if (parameters_Property.arraySize > i)
					if (po.parameters != null && po.parameters.Count > i)
					{
						int hgt = 0;

						if (po.is2D())
						{

							hgt = ParameterSplineGUI.OnGUI_Spline(pRect, this, p);
							cur_y = hgt + gap;
						}
						else
						{
							hgt = ParameterGUI.OnGUI( pRect, this, p);
							cur_y += hgt + gap;




							// PHYSICS

							if (p.Name == "Output Mesh" && ! (po.generator is IReplica))
							{
								float labelWidth = EditorGUIUtility.labelWidth;

								EditorGUIUtility.labelWidth = innerWidth-40;


								GUIStyle labelStyle = GUI.skin.GetStyle ("Label");
								TextAnchor prevTextAlignment = labelStyle.alignment;
								labelStyle.alignment = TextAnchor.MiddleLeft;

								Rect cntlRect = new Rect(x1+16, cur_y, innerWidth, lineHgt);
								EditorGUI.BeginChangeCheck ();
								po.combineMeshes = EditorGUI.Toggle (cntlRect, "Combine Meshes",  po.combineMeshes);
								if (EditorGUI.EndChangeCheck ()) {
									Undo.RegisterCompleteObjectUndo (model, "value change for " + po.Name);
									model.autobuild();
								} 
								labelStyle.alignment = prevTextAlignment;
								EditorGUIUtility.labelWidth = labelWidth;


								cur_y += hgt + gap;
								po.showPhysics = EditorGUI.Foldout(new Rect(x1, cur_y, 30,lineHgt), po.showPhysics, " ");

								if (GUI.Button(new Rect(x1+10, cur_y, 100,lineHgt), "Physics"))
								{
									po.showPhysics = ! po.showPhysics;
								}


								if ( po.showPhysics)
								{

									cur_y += hgt + gap;


									cntlRect = new Rect(x1+16, cur_y, innerWidth-24, lineHgt);

									EditorGUIUtility.labelWidth = 50;
									EditorGUI.BeginChangeCheck ();
									po.colliderType = (ColliderType) EditorGUI.EnumPopup(cntlRect, "Collider ", po.colliderType);
									if (EditorGUI.EndChangeCheck ()) 
									{
										Undo.RegisterCompleteObjectUndo (model, "value change for " + po.Name + ":collidertype");
										model.autobuild();
									}

									cur_y += hgt + gap;





									// isRigidbody

									EditorGUIUtility.labelWidth = 70;

									cntlRect = new Rect(x1+16, cur_y, innerWidth-24, lineHgt);

									EditorGUI.BeginChangeCheck ();
									cntlRect = new Rect(x1+16, cur_y, innerWidth, lineHgt);
									po.isRigidbody = EditorGUI.Toggle (cntlRect, "Rigidbody",  po.isRigidbody);
									if (EditorGUI.EndChangeCheck ()) {
										Undo.RegisterCompleteObjectUndo (model, "value change for " + po.Name);
										if (po.isRigidbody && po.colliderType == ColliderType.Mesh)
											po.colliderType = ColliderType.ConvexMesh;
										model.autobuild();
									} 

									cur_y += hgt + gap;

									if (po.isRigidbody)
									{


										/*
										cntlRect = new Rect(x1+16, cur_y, innerWidth-24, lineHgt);

										EditorGUI.BeginChangeCheck ();
										po.axMat.physMat = (PhysicMaterial) EditorGUI.ObjectField(cntlRect, po.axMat.physMat, typeof(PhysicMaterial), true);
										if (EditorGUI.EndChangeCheck ()) {
											Undo.RegisterCompleteObjectUndo (model, "PhysicMaterial");
											model.autobuild();
										}

										cur_y += hgt + gap;
										*/

										cntlRect = new Rect(x1+16, cur_y, innerWidth, lineHgt);

										EditorGUI.BeginChangeCheck ();
										po.isKinematic = EditorGUI.Toggle (cntlRect, "isKinematic",  po.isKinematic);

										if (EditorGUI.EndChangeCheck ()) {
											Undo.RegisterCompleteObjectUndo (model, "isKinematic");

											model.autobuild();
										}
									}

								}
							}
							cur_y += hgt + gap;



						}

					}
				}

			}
		}

		if (po.isEditing)
		{

			//GUILayout.FlexibleSpace();
			cur_y += lineHgt + gap;
			if (GUI.Button (new Rect(x1+3, cur_y, editButtonWid*2,lineHgt), "Delete Object"))
			{
				Undo.RegisterCompleteObjectUndo (model, "ParametricObject Delete");
				model.removeParametricObject(po);
			}
			cur_y += lineHgt + gap;

		}
		po.outputEndHeight = cur_y-gap;








		// DO THUMBNAIL / DROP_ZONE


	
		int bottomPadding = 55;
		int splineCanvasSize = (int)(po.rect.width-60);

		mostRecentThumbnailRect = new Rect(x1, cur_y+lineHgt, innerWidth, innerWidth);

		Rect lowerRect = new Rect(0, cur_y-50, po.rect.width, po.rect.width);


		if (po.thumbnailState == ThumbnailState.Open)
		{


			//if (po.Output != null && po.Output.Type == AXParameter.DataType.Spline) 
			if (po.is2D()) 
			{


				AXParameter output_p = po.generator.getPreferredOutputParameter(); 
				if  ( po.generator.hasOutputsReady() )
				{
					Color color = po.thumbnailLineColor;

					if (color.Equals(Color.clear))
						color = Color.magenta;

					if (po.generator is ShapeDistributor)
					{   

						color = (EditorGUIUtility.isProSkin) ? new Color(.7f, .6f, 1) : new Color(.1f, 0f, .6f);
					}
					GUIDrawing.DrawPathsFit(output_p, new Vector2(po.rect.width/2, cur_y+po.rect.width/2 ), po.rect.width-60, color);

				}
				else if (ArchimatixEngine.nodeIcons.ContainsKey(po.Type.ToString()))
					GUI.DrawTexture(mostRecentThumbnailRect, ArchimatixEngine.nodeIcons[po.Type], ScaleMode.ScaleToFit, true, 1.0F);


			} 

			else
			{
				//Debug.Log ("po.renTex.IsCreated()="+po.renTex.IsCreated());

				//Debug.Log (po.renTex + " :::::::::::::::::::::::--::




				if (po.generator is PrefabInstancer && po.prefab != null)
				{
					Texture2D thumber = AssetPreview.GetAssetPreview(po.prefab);
					if (e.type == EventType.Repaint)
					{
						if (thumber != null)
							GUI.DrawTexture(mostRecentThumbnailRect,    thumber, ScaleMode.ScaleToFit, true, 1.0F);
						else
							GUI.DrawTexture(mostRecentThumbnailRect, ArchimatixEngine.nodeIcons[po.Type], ScaleMode.ScaleToFit, true, 1.0F);

					}

				}

				else if ( ((po.Output != null && po.Output.meshes != null && po.Output.meshes.Count > 0) || po.generator is MaterialTool) && (po.renTex != null || po.thumbnail != null)   )
					//if ( po.thumbnail != null )   
				{
					//Debug.Log("thumb " + po.renTex);
					if (e.type == EventType.Repaint)
					{
						GUI.DrawTexture(mostRecentThumbnailRect,    po.renTex, ScaleMode.ScaleToFit, true, 1.0F);


						// DROP ZONE

						if (po.generator is Grouper && editorState == EditorState.DraggingNodePalette && mouseIsDownOnPO != po && po != model.currentWorkingGroupPO)
						{
							if (mostRecentThumbnailRect.Contains(e.mousePosition))
							{
								
								GUI.DrawTexture(mostRecentThumbnailRect,    dropZoneOverTex, ScaleMode.ScaleToFit, true, 1.0F);
								OverDropZoneOfPO = po;
							}
							else
								GUI.DrawTexture(mostRecentThumbnailRect,    dropZoneTex, ScaleMode.ScaleToFit, true, 1.0F);
						}


					}
					//else
					//	GUI.DrawTexture(mostRecentThumbnailRect, po.thumbnail, ScaleMode.ScaleToFit, false, 1.0F);



					if (mostRecentThumbnailRect.Contains(e.mousePosition) || draggingThumbnailOfPO == po)
					{
						Rect orbitButtonRect = new Rect(x1+innerWidth-16-3, cur_y+lineHgt+3, 16, 16);

						if (e.command || e.control)
							GUI.DrawTexture(orbitButtonRect, dollyIconTex);
						else
							GUI.DrawTexture(orbitButtonRect, orbitIconTex);


						if (e.type == EventType.MouseDown && orbitButtonRect.Contains(e.mousePosition))
						{
							model.selectOnlyPO(po);
							draggingThumbnailOfPO = po;
							e.Use();
						}

					}	


				}
				else if (ArchimatixEngine.nodeIcons.ContainsKey(po.Type.ToString()))
				{ 
					GUI.DrawTexture(mostRecentThumbnailRect, ArchimatixEngine.nodeIcons[po.Type], ScaleMode.ScaleToFit, true, 1.0F);

					// DROP ZONE

					if (po.generator is Grouper && editorState == EditorState.DraggingNodePalette && mouseIsDownOnPO != po && po != model.currentWorkingGroupPO)
					{
						if (mostRecentThumbnailRect.Contains(e.mousePosition))
						{
							GUI.DrawTexture(mostRecentThumbnailRect,    dropZoneOverTex, ScaleMode.ScaleToFit, true, 1.0F);
							OverDropZoneOfPO = po;
						}
						else
							GUI.DrawTexture(mostRecentThumbnailRect,    dropZoneTex, ScaleMode.ScaleToFit, true, 1.0F);
					}


				}

			}

			cur_y += lineHgt + bottomPadding + splineCanvasSize+  gap;

			po.rect.height = cur_y ;
			cur_y += 4*gap;
		}
		else
		{
			// no thumbnail
			cur_y += 2*lineHgt;
			po.rect.height = cur_y ;
		}










		// FOOTER //





		// STATS
		if (po.stats_VertCount > 0 || po.generator is MaterialTool)
		{

			string statsText;

			if (po.generator is MaterialTool)
			{
				statsText = (po.generator as MaterialTool).texelsPerUnit.ToString("F0") + " Texels/Unit";
			}
			else
			{
				statsText = po.stats_VertCount + " verts";

				if (po.stats_TriangleCount > 0)
					statsText += ", " + po.stats_TriangleCount + " tris";
			}

			GUIStyle statlabelStyle = GUI.skin.GetStyle ("Label");
			TextAnchor prevStatTextAlignment = statlabelStyle.alignment;
			statlabelStyle.alignment = TextAnchor.MiddleLeft;
			EditorGUIUtility.labelWidth = 500;
			GUI.Label(new Rect(10, po.rect.height-x2+2, 500, lineHgt), statsText);
			statlabelStyle.alignment = prevStatTextAlignment;
		}





		if (e.type == EventType.mouseDown &&  lowerRect.Contains(e.mousePosition) )  
		{
			clearFocus();
			GUI.FocusWindow(po.guiWindowId);
		}

		// WINDOW RESIZE
		Rect buttonRect = new Rect(po.rect.width-16, po.rect.height-17, 14, 14);
		if (e.type == EventType.mouseDown && buttonRect.Contains(e.mousePosition))
		{
			Undo.RegisterCompleteObjectUndo (model, "GUI Window Resize");

			editorState = EditorState.DragResizingNodePalleteWindow;	
			DraggingParameticObject = po;		

		}
		//GUI.Button ( buttonRect, "∆", GUIStyle.none);
		GUI.Button ( buttonRect, resizeCornerTexture, GUIStyle.none);


		if (e.type == EventType.mouseDown && buttonRect.Contains(e.mousePosition))
		{
		}

		//cur_y += lineHgt + gap;

		// Window title bar is the dragable area
		//GUI.DragWindow(headerRect);

		if (draggingThumbnailOfPO == null || draggingThumbnailOfPO != po)
		{
			

			GUI.DragWindow();

		}

		GUI.backgroundColor = origBG;


	}
	// \ DRAW NODE PALETTE ** ** * ** *** *

	#endregion


































	public void outputSocketClicked(AXParameter p )
	{
		
		if (OutputParameterBeingDragged != null) 
		{
			if (OutputParameterBeingDragged == p)
			{
				Undo.RegisterCompleteObjectUndo (model, "Clear Dependencies");
				OutputParameterBeingDragged.freeDependents();
				OutputParameterBeingDragged = null;

				//Debug.Log("*********************************** outputSocketClicked");
				model.remapMaterialTools();

				model.autobuild();

				p.parametricObject.generator.adjustWorldMatrices();

			}
			else 
				OutputParameterBeingDragged = p;
		}
		else if (InputParameterBeingDragged != null) 
		{ 
			

			// CYCLE? add new solo input or swap out the previous input
			if (p.Parent.Guid == InputParameterBeingDragged.Parent.Guid)
			{	
				if (EditorUtility.DisplayDialog("Illegal Relationship", "A parametric object cannot link to itself.", "Got it!")) {
					InputParameterBeingDragged= null;
					return;
				}
			} 
			else if (AXParameter.checkForCycle(p, InputParameterBeingDragged))
			{
				if (EditorUtility.DisplayDialog("Illegal Relationship", "A parametric object cannot \"downstream\" of itself.", "Got it!")) {
					InputParameterBeingDragged= null;
					return;
				}
			}

			// OK - NO CYCLE...

			if (InputParameterBeingDragged.Parent.Guid == p.Parent.Guid)
			{	
				if (EditorUtility.DisplayDialog("Illegal Relationship", "A parametric object cannot be linked to itself", "Got it!")) {
					InputParameterBeingDragged = null;
					return;
				}
			}

			Undo.RegisterCompleteObjectUndo (model, "Create Dependencies");


			if (useInputRelations || p.Type == AXParameter.DataType.Float || p.Type == AXParameter.DataType.Int || p.Type == AXParameter.DataType.Bool || p.Type == AXParameter.DataType.Generic)
				model.relate(InputParameterBeingDragged, p);
			else
				InputParameterBeingDragged.makeDependentOn(p);

			model.autobuild();
			p.parametricObject.generator.adjustWorldMatrices();


		}
		else 
			OutputParameterBeingDragged = p;

		InputParameterBeingDragged = null;

	}


	public void inputsInputSocketClicked(AXParametricObject po)
	{
		po.inputControls.isOpen = true;

	}	  
	public void inputSocketClicked(AXParameter p )
	{



		if (InputParameterBeingDragged != null) 
		{ 
			if (InputParameterBeingDragged == p)
			{
				Undo.RegisterCompleteObjectUndo (model, "Clear Dependencies");

				AXParametricObject dep_src = null;
				if (p.DependsOn != null)
					dep_src = p.DependsOn.parametricObject;

				InputParameterBeingDragged.makeIndependent();

				InputParameterBeingDragged = null;
				model.autobuild();

				if (dep_src != null && dep_src.selectedConsumer != null && dep_src.selectedConsumer==p.parametricObject)
					dep_src.selectedConsumer = null;
				dep_src.generator.adjustWorldMatrices();

			}	
			else
				InputParameterBeingDragged = p;
		}
		else if (OutputParameterBeingDragged != null) 
		{ 
			
			Undo.RegisterCompleteObjectUndo (model, "Create Dependencies");


			if (useInputRelations || p.Type == AXParameter.DataType.Float || p.Type == AXParameter.DataType.Int || p.Type == AXParameter.DataType.Bool || p.Type == AXParameter.DataType.Generic)
			{
				//Debug.Log ("relating " + OutputParameterBeingDragged.Guid + "to " + p.Guid);
				model.relate(OutputParameterBeingDragged, p);
			}
			else
			{

				// INPUT OR OUTPUT CONNECTION OF A SPLINE OR MESH


				// CYCLE? add new solo input or swap out the previous input
				if (p.Parent.Guid == OutputParameterBeingDragged.Parent.Guid)
				{	
					if (EditorUtility.DisplayDialog("Illegal Relationship", "A parametric object cannot link to itself.", "Got it!")) {
						OutputParameterBeingDragged= null;
						return;
					}
				} 
				else if (AXParameter.checkForCycle(OutputParameterBeingDragged, p))
				{
					if (EditorUtility.DisplayDialog("Illegal Relationship", "A parametric object cannot \"downstream\" of itself.", "Got it!")) {
						OutputParameterBeingDragged= null;
						return;
					}
				}

				// OK, NO CYCLE - GO AHEAD
				//Debug.Log (" * OK ********* NO CYCLE");



				/*
				AXParametricObject draggingPO = OutputParameterBeingDragged.parametricObject;

				if (draggingPO.is2D() && !(draggingPO.generator is ShapeDistributor) && !(p.parametricObject.generator is ShapeDistributor) && OutputParameterBeingDragged.Dependents != null && OutputParameterBeingDragged.Dependents.Count > 0)
				{
					// INSERT SHAPE_DISTRIBUTOR


					model.insertShapeDistributor(OutputParameterBeingDragged, p);

					AXNodeGraphEditorWindow.repaintIfOpen();

				}
				*/

				// REPLACE OR MERGE SHAPES?


				if ( p.DependsOn != null  &&  (Event.current.command || Event.current.button == 1) && p.DependsOn.Parent.is2D() )//|| p.DependsOn.parametricObject == p.parametricObject) // example of the last case is a Union from a merger is hooked in place of a Difference.
				{	
					// MERGE WITH EXISTING SHAPE INPUT
					// already an input... add a merger between?
					AXParametricObject mergerPO = AXEditorUtilities.addNodeToCurrentModel("ShapeMerger");

					AXShape shp = mergerPO.generator.getInputShape(); 

					// originally connected parameter
					AXParameter newInput =  shp.addInput();
					newInput.makeDependentOn(p.DependsOn);


					newInput =  shp.addInput();
					newInput.makeDependentOn(OutputParameterBeingDragged);

					AXParameter nop = mergerPO.getPreferredOutputSplineParameter();

					mergerPO.rect = OutputParameterBeingDragged.parametricObject.rect;
					mergerPO.rect.x += 250;

					p.parametricObject.rect.x += 250;

					p.makeDependentOn(nop); 
					OutputParameterBeingDragged = null;

					model.remapMaterialTools();
					model.setAltered(mergerPO);
					model.setAltered(p.parametricObject);

				}	
				else 
				{

					// REPLACE SHAPE INPUT
					p.makeDependentOn(OutputParameterBeingDragged);

					p.shapeState = OutputParameterBeingDragged.shapeState;

					OutputParameterBeingDragged = null;

					model.remapMaterialTools();
					model.setAltered(p.parametricObject);



				}


			}

			model.autobuild();
			p.parametricObject.generator.adjustWorldMatrices();
			//Debug.Log("draw");



		} 
		else 
			InputParameterBeingDragged = p;

		//Debug.Log ("Setting _draggingOutputParameter to null");
		OutputParameterBeingDragged = null;



	}














	void removeLibraryItem(string _name)
	{

	Debug.Log("read library");

		string filename = AX.Library.getRelativeLibraryPath() + "data/" + _name+".json";
		Debug.Log(" deleting: " + filename);


		// [S.Darkwell: Change from "? This is undoable!"]
		if (EditorUtility.DisplayDialog("Delete Library Object?",
			"Are you sure you want to delete " + _name
			+ "? This cannot be undone!", "Really Delete", "Cancel")) {

			if(AssetDatabase.DeleteAsset(filename))
				Debug.Log(filename + " deleted");

			// Refresh the AssetDatabase after all the changes
			AssetDatabase.Refresh();

			//Debug.Log("yo 6");
			//library.readLibraryFromFiles();
		}
	}












	/*
	[MenuItem("GameObject/3D Object/Archimatix Model")]
	static void newModel() {

		//LibraryEditorWindow libwinNewModel = (LibraryEditorWindow) EditorWindow.GetWindow(typeof(LibraryEditorWindow));
		//libwinNewModel.title = "AX Library";

		
		
		//AXModelEditorWindow edwin = (AXModelEditorWindow) EditorWindow.GetWindow(typeof(AXModelEditorWindow));
		
		AXEditorUtilities.createNewModel();
		
		//return model;
	}
	*/









}