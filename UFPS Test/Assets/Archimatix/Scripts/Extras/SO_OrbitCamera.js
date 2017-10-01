#pragma strict


/*
This camera smoothes out rotation around the y-axis and height.
Horizontal Distance to the target is always fixed.

There are many different ways to smooth the rotation but doing it this way gives you a lot of control over how the camera behaves.

For every of those smoothed values we calculate the wanted value and the current value.
Then we smooth it using the Lerp function.
Then we apply the smoothed values to the transform's position.
*/

// The target we are following
var targetRig : Transform;
var target : Transform;

// The distance in the x-z plane to the target
var distance: float = 10.0;
// the height we want the camera to be above the target
var height: float = 5.0;
// How much we 
var heightDamping: float = 1000;
var rotationDamping: float = 3.0;


var beta		: float;
var betaTarget	: float;

var alpha		: float;
var alphaTarget : float;

private var isDraggingView: boolean = false;
private var prevTouchPinchDistance: float;



private var movingTarget_FromPoint: 	Vector3;
private var movingTarget_FromDistance: 	float;

private var movingTarget_ToPoint: 		Vector3;
private var movingTarget_ToDistance: 	float;
private var movingTarget_Duration: 		float; 

private var movingTarget_StartTime: 	float; 
private var movingTarget_IsMoving: 		boolean = false;


// Place the script in the Camera-Control group in the component menu
@script AddComponentMenu("Camera-Control/Smooth Follow")



function moveTargetTo(toPoint: Vector3) {
	moveTargetTo(toPoint, 150, 3);
}
function moveTargetTo(toPoint: Vector3, dist: float, duration: float) {
	
	movingTarget_ToPoint		= toPoint;
	movingTarget_ToDistance		= dist;
	movingTarget_Duration 		= duration;
	
	movingTarget_FromPoint 		= target.transform.position;
	movingTarget_FromDistance 	= distance;
   	
   	movingTarget_StartTime 		= Time.time;

	movingTarget_IsMoving 		= true;
	
}



function Update() {
	//Debug.Log("camera upadte!");


	if (Input.GetMouseButtonDown(0))
	{
            //Debug.Log("Pressed left click. " +  Input.mousePosition);
           if (Input.mousePosition.y > 182 && Input.mousePosition.y < Screen.height-65 ) 

           {
           		Debug.Log("start dragging");
           		isDraggingView = true;
           }

    } 
    else if (Input.GetMouseButtonUp(0))
    {
    	isDraggingView = false;
    }
        


	

	if (isDraggingView) {
		
		//Debug.Log("isDraggingView!");
	    if (Input.touchCount > 0) {
	    	if (Input.GetTouch(0).phase == TouchPhase.Moved) {
		       	var speed: float = .25;
		    	var touch1 : Touch = Input.GetTouch(0); 
		       	// iOS Input
		       	var touchDeltaPosition: Vector2 = touch1.deltaPosition;

		    	if (Input.touchCount == 1) {
		        	
		        	// Rotate camera
		        	target.transform.Rotate(0, touchDeltaPosition.x*speed, 0);
		       		height -= .5*speed*touchDeltaPosition.y;
		       		prevTouchPinchDistance = 0;
		    	
		    	
		    	
		    	
		    	
		    	
		    	} else if (Input.touchCount == 2) {
		        	
		        	// PAN
		        	// by sliding 2 fingers

					// if more than 45 degs, just slide target in x & z
					
					if (touchDeltaPosition.x != 0 || touchDeltaPosition.y != 0 ) {
						target.transform.Translate(-touchDeltaPosition.x * speed, 0, -touchDeltaPosition.y * speed);
					}
							
					//target.transform.Translate (-speed*touchDeltaPosition.y*this.transform.up.normalized);
					//target.transform.Translate (-touchDeltaPosition.x * speed, 0, 0);
		    	
		    	
		    		// ZOOM 
		    		// by pinching
	       			var touch2 : Touch = Input.GetTouch( 1 ); 
	
					var touchPinchDistance: float = Vector2.Distance(touch1.position, touch2.position);
					if (prevTouchPinchDistance > 0) {
						var pinch: float = touchPinchDistance-prevTouchPinchDistance;
							//print("pinch = " + pinch);
							if (Mathf.Abs(pinch) > 5) {
								
								distance -= pinch/2.0;
								if (distance < 5) distance = 5;
								height -= pinch/2.0;
							}
					}
					prevTouchPinchDistance = touchPinchDistance;
		    	}
	    	}
	    } else {
	    	// Mouse Input
	    
			var h: float =  5 * Input.GetAxis("Mouse X");
			var v: float = -10 * Input.GetAxis("Mouse Y");

			alphaTarget += h;
			betaTarget 	+= v;

			// Rotate the target and the camera will follow

			//target.transform.Rotate(h, 0, 0);
			//height -= .2f*v;
		

			//height -= 1*scroll;
	    }
//		if (height < -target.transform.position.y) {
//			height = -target.transform.position.y+1;
//		}
		
	}
	
	var scroll: float = Input.GetAxis("Mouse ScrollWheel");
	distance -= 5*scroll;

	
	
	
	
	speed = 60.0;
	var rotationSpeed : float = 150.0;
	
	var translationH : float = -Input.GetAxis ("Horizontal") * Time.deltaTime;
	var translationV : float = -Input.GetAxis ("Vertical")   * Time.deltaTime;
	
	
	
	if (Input.GetKey(KeyCode.LeftAlt)) {
		
		// dolly in
		var tmpspeed = speed;
		if (translationV != 0) {
			
			//if (distance < 10) {
				// slow down
					tmpspeed = distance*distance/20;
			//}
			if (tmpspeed > 100) tmpspeed = 200;
			
			var ratio: float = height/distance;
			
			distance += translationV*tmpspeed;
			
			if (distance <1) distance = 1;
			
			height = ratio * distance;
			
			
			
		}
		
		if (translationH != 0) {
			target.transform.Rotate (0, translationH * -rotationSpeed, 0);
		}
		
		if (distance < 1) {
			distance = 1;
			height = 1;
			
		} 		
		
	} else {
		// divide into components to translate the target up or along z
		if (translationH != 0 || translationV != 0 ) {
			target.transform.Translate(-speed*translationH, 0, -speed*translationV);
		}
	}

	
	



	// MOVING TARGET ANIMATION
	
	var duration = 1.5;
	if (movingTarget_IsMoving) {
		var gap: float = Time.time - movingTarget_StartTime;
		
		if (gap > duration) {
			movingTarget_IsMoving = false;
		} else {
  		    target.transform.position = Vector3.Lerp(movingTarget_FromPoint, movingTarget_ToPoint, gap/duration );
  		    distance = 3+movingTarget_FromDistance + (movingTarget_ToDistance-movingTarget_FromDistance)*(gap/duration);  		    
		}
	}

	
} 	


function LateUpdate () {
 	//Early out if we don't have a target
	if (!target)
		return;
	
	// Calculate the current rotation angles





	var wantedHeight: 	float = target.position.y + height;
		
	
	var currentHeight: float = transform.position.y;



	// Damp the rotation around the y-axis
	alpha 	= Mathf.LerpAngle (alpha, 	alphaTarget, 	rotationDamping * Time.deltaTime);
	beta 	= Mathf.LerpAngle (beta, 	betaTarget, 	rotationDamping * Time.deltaTime);

	// Damp the height
	currentHeight = Mathf.Lerp (currentHeight, wantedHeight, heightDamping * Time.deltaTime);

	var wantedTargetRigY = (beta > 180) ? (360-beta)/5: -beta/5;
	wantedTargetRigY /= 3;
	wantedTargetRigY -= 4;
	//Debug.Log(currentAlpha);
	targetRig.position.y = Mathf.Lerp (targetRig.position.y, wantedTargetRigY, heightDamping * Time.deltaTime);



	// Convert the angle into a rotation
	var currentRotation = Quaternion.Euler (beta, alpha, 0);
	
	// Set the position of the camera on the x-z plane to:
	// distance meters behind the target
	transform.position = target.position;
	transform.position -= currentRotation * Vector3.forward * distance;

	// Set the height of the camera
	//transform.position.y = currentHeight;
	
	// Always look at the target
	transform.LookAt (target);
	
	RenderSettings.fogDensity = .005 - (Mathf.Sqrt(distance)/8500.0);
	if (RenderSettings.fogDensity < .0007)  RenderSettings.fogDensity = .0007;
	//Debug.Log(RenderSettings.fogDensity);
}


function startDraggingView(): void {
	isDraggingView = true;
}
function stopDraggingView(): void {
	isDraggingView = false;
}









