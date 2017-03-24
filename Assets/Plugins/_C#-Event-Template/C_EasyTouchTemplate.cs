// EasyTouch v3.0 (September 2012)
// EasyTouch library is copyright (c) of Hedgehog Team
// Please send feedback or bug reports to the.hedgehog.team@gmail.com
using UnityEngine;
using System.Collections;

public class C_EasyTouchTemplate : MonoBehaviour {

	void OnEnable(){
	
		EasyTouch.On_Cancel += On_Cancel;
		EasyTouch.On_TouchStart += On_TouchStart;
		EasyTouch.On_TouchDown += On_TouchDown;
		EasyTouch.On_TouchUp += On_TouchUp;
		EasyTouch.On_SimpleTap += On_SimpleTap;
		EasyTouch.On_DoubleTap += On_DoubleTap;
		EasyTouch.On_LongTapStart +=On_LongTapStart;
		EasyTouch.On_LongTap += On_LongTap;
		EasyTouch.On_LongTapEnd += On_LongTapEnd;
		EasyTouch.On_DragStart += On_DragStart;
		EasyTouch.On_Drag += On_Drag;
		EasyTouch.On_DragEnd += On_DragEnd;
		EasyTouch.On_SwipeStart += On_SwipeStart;
		EasyTouch.On_Swipe += On_Swipe;
		EasyTouch.On_SwipeEnd += On_SwipeEnd;
		EasyTouch.On_TouchStart2Fingers += On_TouchStart2Fingers;
		EasyTouch.On_TouchDown2Fingers += On_TouchDown2Fingers;
		EasyTouch.On_TouchUp2Fingers += On_TouchUp2Fingers;
		EasyTouch.On_SimpleTap2Fingers += On_SimpleTap2Fingers;
		EasyTouch.On_DoubleTap2Fingers += On_DoubleTap2Fingers;
		EasyTouch.On_LongTapStart2Fingers += On_LongTapStart2Fingers;
		EasyTouch.On_LongTap2Fingers += On_LongTap2Fingers;
		EasyTouch.On_LongTapEnd2Fingers += On_LongTapEnd2Fingers;
		EasyTouch.On_Twist += On_Twist;
		EasyTouch.On_TwistEnd += On_TwistEnd;
		EasyTouch.On_PinchIn += On_PinchIn;
		EasyTouch.On_PinchOut += On_PinchOut;
		EasyTouch.On_PinchEnd += On_PinchEnd;
		EasyTouch.On_DragStart2Fingers += On_DragStart2Fingers;
		EasyTouch.On_Drag2Fingers += On_Drag2Fingers;
		EasyTouch.On_DragEnd2Fingers += On_DragEnd2Fingers;
		EasyTouch.On_SwipeStart2Fingers += On_SwipeStart2Fingers;
		EasyTouch.On_Swipe2Fingers += On_Swipe2Fingers;
		EasyTouch.On_SwipeEnd2Fingers += On_SwipeEnd2Fingers;
	}
	
	void OnDisable(){
		EasyTouch.On_Cancel -= On_Cancel;
		EasyTouch.On_TouchStart -= On_TouchStart;
		EasyTouch.On_TouchDown -= On_TouchDown;
		EasyTouch.On_TouchUp -= On_TouchUp;
		EasyTouch.On_SimpleTap -= On_SimpleTap;
		EasyTouch.On_DoubleTap -= On_DoubleTap;
		EasyTouch.On_LongTapStart -=On_LongTapStart;
		EasyTouch.On_LongTap -= On_LongTap;
		EasyTouch.On_LongTapEnd -= On_LongTapEnd;
		EasyTouch.On_DragStart -= On_DragStart;
		EasyTouch.On_Drag -= On_Drag;
		EasyTouch.On_DragEnd -= On_DragEnd;
		EasyTouch.On_SwipeStart -= On_SwipeStart;
		EasyTouch.On_Swipe -= On_Swipe;
		EasyTouch.On_SwipeEnd -= On_SwipeEnd;
		EasyTouch.On_TouchStart2Fingers -= On_TouchStart2Fingers;
		EasyTouch.On_TouchDown2Fingers -= On_TouchDown2Fingers;
		EasyTouch.On_TouchUp2Fingers -= On_TouchUp2Fingers;
		EasyTouch.On_SimpleTap2Fingers -= On_SimpleTap2Fingers;
		EasyTouch.On_DoubleTap2Fingers -= On_DoubleTap2Fingers;
		EasyTouch.On_LongTapStart2Fingers -= On_LongTapStart2Fingers;
		EasyTouch.On_LongTap2Fingers -= On_LongTap2Fingers;
		EasyTouch.On_LongTapEnd2Fingers -= On_LongTapEnd2Fingers;
		EasyTouch.On_Twist -= On_Twist;
		EasyTouch.On_TwistEnd -= On_TwistEnd;
		EasyTouch.On_PinchIn -= On_PinchIn;
		EasyTouch.On_PinchOut -= On_PinchOut;
		EasyTouch.On_PinchEnd -= On_PinchEnd;
		EasyTouch.On_DragStart2Fingers -= On_DragStart2Fingers;
		EasyTouch.On_Drag2Fingers -= On_Drag2Fingers;
		EasyTouch.On_DragEnd2Fingers -= On_DragEnd2Fingers;
		EasyTouch.On_SwipeStart2Fingers -= On_SwipeStart2Fingers;
		EasyTouch.On_Swipe2Fingers -= On_Swipe2Fingers;
		EasyTouch.On_SwipeEnd2Fingers -= On_SwipeEnd2Fingers;		
	}
	
	void OnDestroy(){
		EasyTouch.On_Cancel -= On_Cancel;
		EasyTouch.On_TouchStart -= On_TouchStart;
		EasyTouch.On_TouchDown -= On_TouchDown;
		EasyTouch.On_TouchUp -= On_TouchUp;
		EasyTouch.On_SimpleTap -= On_SimpleTap;
		EasyTouch.On_DoubleTap -= On_DoubleTap;
		EasyTouch.On_LongTapStart -=On_LongTapStart;
		EasyTouch.On_LongTap -= On_LongTap;
		EasyTouch.On_LongTapEnd -= On_LongTapEnd;
		EasyTouch.On_DragStart -= On_DragStart;
		EasyTouch.On_Drag -= On_Drag;
		EasyTouch.On_DragEnd -= On_DragEnd;
		EasyTouch.On_SwipeStart -= On_SwipeStart;
		EasyTouch.On_Swipe -= On_Swipe;
		EasyTouch.On_SwipeEnd -= On_SwipeEnd;
		EasyTouch.On_TouchStart2Fingers -= On_TouchStart2Fingers;
		EasyTouch.On_TouchDown2Fingers -= On_TouchDown2Fingers;
		EasyTouch.On_TouchUp2Fingers -= On_TouchUp2Fingers;
		EasyTouch.On_SimpleTap2Fingers -= On_SimpleTap2Fingers;
		EasyTouch.On_DoubleTap2Fingers -= On_DoubleTap2Fingers;
		EasyTouch.On_LongTapStart2Fingers -= On_LongTapStart2Fingers;
		EasyTouch.On_LongTap2Fingers -= On_LongTap2Fingers;
		EasyTouch.On_LongTapEnd2Fingers -= On_LongTapEnd2Fingers;
		EasyTouch.On_Twist -= On_Twist;
		EasyTouch.On_TwistEnd -= On_TwistEnd;
		EasyTouch.On_PinchIn -= On_PinchIn;
		EasyTouch.On_PinchOut -= On_PinchOut;
		EasyTouch.On_PinchEnd -= On_PinchEnd;
		EasyTouch.On_DragStart2Fingers -= On_DragStart2Fingers;
		EasyTouch.On_Drag2Fingers -= On_Drag2Fingers;
		EasyTouch.On_DragEnd2Fingers -= On_DragEnd2Fingers;
		EasyTouch.On_SwipeStart2Fingers -= On_SwipeStart2Fingers;
		EasyTouch.On_Swipe2Fingers -= On_Swipe2Fingers;
		EasyTouch.On_SwipeEnd2Fingers -= On_SwipeEnd2Fingers;		
	}
		
    private void  On_Cancel( Gesture gesture ){Debug.Log("cancel");}

    private void  On_TouchStart( Gesture gesture ){Debug.Log("touch start");}

    private void  On_TouchDown( Gesture gesture ){Debug.Log("touch down");}

    private void  On_TouchUp( Gesture gesture ){Debug.Log("touch up");}

    private void  On_SimpleTap( Gesture gesture ){Debug.Log("simple tap");}

    private void  On_DoubleTap( Gesture gesture ){Debug.Log("double tap");}

    private void  On_LongTapStart( Gesture gesture ){Debug.Log("long tap start");}

    private void  On_LongTap( Gesture gesture ){Debug.Log("long tap");}

    private void  On_LongTapEnd( Gesture gesture ){Debug.Log("long tap end");}

    private void  On_DragStart( Gesture gesture ){Debug.Log("drag start");}

    private void  On_Drag( Gesture gesture ){Debug.Log("drag");}

    private void  On_DragEnd( Gesture gesture ){Debug.Log("drag end");}

    private void  On_SwipeStart( Gesture gesture ){Debug.Log("swipe start"+"ishover "+gesture.isHoverReservedArea);}

    private void  On_Swipe( Gesture gesture ){Debug.Log("swipe"+gesture.isHoverReservedArea);}

    private void  On_SwipeEnd( Gesture gesture ){Debug.Log("swipe end"+gesture.isHoverReservedArea);}

    private void  On_TouchStart2Fingers( Gesture gesture ){Debug.Log("touch start 2 finger");}

    private void  On_TouchDown2Fingers( Gesture gesture ){Debug.Log("touch down 2 finger");}

    private void  On_TouchUp2Fingers( Gesture gesture ){Debug.Log("touch up 2 finger");}

    private void  On_SimpleTap2Fingers( Gesture gesture ){Debug.Log("simple tap 2 finger");}

    private void  On_DoubleTap2Fingers( Gesture gesture ){Debug.Log("double tap 2 finger");}

    private void  On_LongTapStart2Fingers( Gesture gesture ){Debug.Log("long tap start 2 finger");}

    private void  On_LongTap2Fingers( Gesture gesture ){Debug.Log("long tap 2 finge");}

    private void  On_LongTapEnd2Fingers( Gesture gesture ){Debug.Log("long tap end 2 finger");}

    private void  On_Twist( Gesture gesture ){Debug.Log("twist");}

    private void  On_TwistEnd( Gesture gesture ){Debug.Log("twist end");}

    private void  On_PinchIn( Gesture gesture ){Debug.Log("pinch in");}

    private void  On_PinchOut( Gesture gesture ){Debug.Log("pinch out");}

    private void  On_PinchEnd( Gesture gesture ){Debug.Log("pinch end");}

    private void  On_DragStart2Fingers( Gesture gesture ){Debug.Log("drag start 2 finger");}

    private void  On_Drag2Fingers( Gesture gesture ){Debug.Log("drag 2 finger");}

    private void  On_DragEnd2Fingers( Gesture gesture ){Debug.Log("drag end 2 finger");}

    private void  On_SwipeStart2Fingers( Gesture gesture ){Debug.Log("swipe start 2 finger");}

    private void  On_Swipe2Fingers( Gesture gesture ){Debug.Log("swipe 2 finger");}

    private void  On_SwipeEnd2Fingers( Gesture gesture ){Debug.Log("swipe end 2 finger");}
}
