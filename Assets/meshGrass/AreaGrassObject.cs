using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AreaGrassObject : ScriptableObject {
    public int active_idx = -1;
    public List<Vector3> control_points = new List<Vector3>();
    public int state = 0;
    public float height = 0.2f;
    public float step = 0.12f;
    public string[] stateStrings = new string[] { "Edit", "Build" };

    static private AreaGrassObject instance = null;

    static public AreaGrassObject Instance{
        get
        {
            if (instance == null)
                instance = ScriptableObject.CreateInstance<AreaGrassObject>();
            return instance;
        }
    }

    
}
