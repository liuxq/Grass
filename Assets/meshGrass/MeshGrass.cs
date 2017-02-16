using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MeshGrass : MonoBehaviour {
    public int active_idx = -1;
    public List<Vector3> control_points = new List<Vector3>();
    public int state = 0;
    public float height = 0.2f;
    public string[] stateStrings = new string[] { "Edit", "Build" };
}
