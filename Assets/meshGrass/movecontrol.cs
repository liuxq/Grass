using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class movecontrol : MonoBehaviour {

    public Transform cameraTr = null;
    public MeshRenderer smr = null;

    void Start()
    {
        EasyJoystick.On_JoystickMove += move;
        //Application.targetFrameRate = 60;
    }
    
    void move(MovingJoystick mjs)
    {
        if(mjs.joystickName == "MOVEleft")
            cameraTr.position += cameraTr.forward * mjs.joystickAxis.y + cameraTr.right * mjs.joystickAxis.x;
        else if(mjs.joystickName == "MOVEright")
        {
            cameraTr.Rotate(Vector3.up, mjs.joystickAxis.x);
            cameraTr.Rotate(Vector3.left, mjs.joystickAxis.y);
        }

        smr.material.SetVector("_pushPos", new Vector4(cameraTr.position.x, cameraTr.position.y, cameraTr.position.z, 0));
    }
}
