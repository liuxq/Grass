// EasyJoystick v2.0 (April 2013)
// EasyJoystick library is copyright (c) of Hedgehog Team
// Please send feedback or bug reports to the.hedgehog.team@gmail.com
using UnityEngine;
using System.Collections;

/// <summary>
/// C_ easy joystick template.
/// </summary>
public class C_EasyJoystickTemplate : MonoBehaviour {

	
	void OnEnable(){
		EasyJoystick.On_JoystickTouchStart += On_JoystickTouchStart;
		EasyJoystick.On_JoystickMoveStart += On_JoystickMoveStart;
		EasyJoystick.On_JoystickMove += On_JoystickMove;
		EasyJoystick.On_JoystickMoveEnd += On_JoystickMoveEnd;
		EasyJoystick.On_JoystickTouchUp += On_JoystickTouchUp;
		EasyJoystick.On_JoystickTap += On_JoystickTap;
		EasyJoystick.On_JoystickDoubleTap += On_JoystickDoubleTap;
	}
	
	void OnDisable(){
		EasyJoystick.On_JoystickTouchStart -= On_JoystickTouchStart;
		EasyJoystick.On_JoystickMoveStart -= On_JoystickMoveStart;
		EasyJoystick.On_JoystickMove -= On_JoystickMove;
		EasyJoystick.On_JoystickMoveEnd -= On_JoystickMoveEnd;
		EasyJoystick.On_JoystickTouchUp -= On_JoystickTouchUp;
		EasyJoystick.On_JoystickTap -= On_JoystickTap;
		EasyJoystick.On_JoystickDoubleTap -= On_JoystickDoubleTap;
	}

	void OnDestroy(){
		EasyJoystick.On_JoystickTouchStart -= On_JoystickTouchStart;
		EasyJoystick.On_JoystickMoveStart -= On_JoystickMoveStart;
		EasyJoystick.On_JoystickMove -= On_JoystickMove;
		EasyJoystick.On_JoystickMoveEnd -= On_JoystickMoveEnd;
		EasyJoystick.On_JoystickTouchUp -= On_JoystickTouchUp;
		EasyJoystick.On_JoystickTap -= On_JoystickTap;
		EasyJoystick.On_JoystickDoubleTap -= On_JoystickDoubleTap;
	}	
	
    void On_JoystickDoubleTap (MovingJoystick move){Debug.Log("double tap");}

    void On_JoystickTap (MovingJoystick move){Debug.Log("one tap");}

    void On_JoystickTouchUp (MovingJoystick move){Debug.Log("touch up");}
		
    void On_JoystickMoveEnd (MovingJoystick move){Debug.Log("move end");}

    void On_JoystickMove(MovingJoystick move){Debug.Log("move");}

    void On_JoystickMoveStart (MovingJoystick move){Debug.Log("move start");}

    void On_JoystickTouchStart (MovingJoystick move){Debug.Log("touch start");}

	
}
