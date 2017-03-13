using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

[CustomEditor(typeof(GrassScript))]
public class AreaGrassEditor : Editor {

    private Vector3 GetWorldPointFromMouse()
    {
        float planeLevel = 0;
        var groundPlane = new Plane(Vector3.up, new Vector3(0, planeLevel, 0));

        var ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        RaycastHit rayHit;
        Vector3 hit = new Vector3(0, 0, 0);
        float dist;

        if (Physics.Raycast(ray, out rayHit, Mathf.Infinity))
            hit = rayHit.point;
        else if (groundPlane.Raycast(ray, out dist))
            hit = ray.origin + ray.direction.normalized * dist;

        return hit;
    }

    private void getPointsAB(List<Vector3> points, out Vector2 xMinMax, out Vector2 zMinMax)
    {
        xMinMax.x = float.MaxValue;
        xMinMax.y = float.MinValue;
        zMinMax.x = float.MaxValue;
        zMinMax.y = float.MinValue;
        for(int i = 0; i < points.Count; i++)
        {
            if (points[i].x < xMinMax.x) xMinMax.x = points[i].x;
            if (points[i].x > xMinMax.y) xMinMax.y = points[i].x;

            if (points[i].z < zMinMax.x) zMinMax.x = points[i].z;
            if (points[i].z > zMinMax.y) zMinMax.y = points[i].z;
        } 
    }

    private List<Vector3> getIntersectPoints(bool isX, float value, List<Vector3> points)
    {
        List<Vector3> result = new List<Vector3>();
        if(isX)
        {
            for(int i = 0; i < points.Count; i++)
            {
                Vector3 p1 = points[i];
                Vector3 p2 = points[(i+1)%points.Count];

                if (p1.x < value && p2.x < value || p1.x > value && p2.x > value)
                    continue;

                if (p1.x == p2.x)
                {
                    result.Add(p1);
                    result.Add(p2);
                }
                else if (p1.x < p2.x)
                {
                    if (p1.x == value)
                        result.Add(p1);
                    else if (p2.x != value)
                        result.Add(p1 + (p2 - p1) * (value - p1.x) / (p2.x - p1.x));
                }
                else
                {
                    if (p2.x == value)
                        result.Add(p2);
                    else if (p1.x != value)
                        result.Add(p2 + (p1 - p2) * (value - p2.x) / (p1.x - p2.x));
                } 
            }
            result.Sort((a, b) =>
            {
                if (a.z < b.z)
                    return -1;
                return 1;
            });
        }
        else
        {
            for(int i = 0; i < points.Count; i++)
            {
                Vector3 p1 = points[i];
                Vector3 p2 = points[(i + 1) % points.Count];

                if (p1.z < value && p2.z < value || p1.z > value && p2.z > value)
                    continue;

                if (p1.z == p2.z)
                {
                    result.Add(p1);
                    result.Add(p2);
                }
                else if (p1.z < p2.z)
                {
                    if (p1.z == value)
                        result.Add(p1);
                    else if (p2.z != value)
                        result.Add(p1 + (p2 - p1) * (value - p1.z) / (p2.z - p1.z));
                }
                else
                {
                    if (p2.z == value)
                        result.Add(p2);
                    else if (p1.z != value)
                        result.Add(p2 + (p1 - p2) * (value - p2.z) / (p1.z - p2.z));
                }
            }
            result.Sort((a, b) =>
            {
                if (a.x < b.x)
                    return -1;
                return 1;
            });
        }

        return result;
    }

    bool BuildMesh()
    {
        Mesh grassMesh = new Mesh();
        List<Vector3> vertexs = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<Vector2> uv2s = new List<Vector2>();
        List<Vector2> uv3s = new List<Vector2>();
        List<Vector3> normals = new List<Vector3>();
        List<int> triangles = new List<int>();

        int index = 0;

        AreaGrassObject _target = AreaGrassObject.Instance;
        Vector3 heightOffset = new Vector3(0, _target.height, 0);

        Vector2 xMinMax, zMinMax;
        getPointsAB(_target.control_points, out xMinMax, out zMinMax);

        List<Vector3> intersectPs = new List<Vector3>();

        //边界
        for (int i = 0; i < _target.control_points.Count; i++)
        {
            Vector3 p1 = _target.control_points[i];
            Vector3 p2 = _target.control_points[(i + 1) % _target.control_points.Count];

            float dis = Vector3.Distance(p1, p2);

            vertexs.Add(p1); uvs.Add(new Vector2(0, 0)); normals.Add(new Vector3(0, 1, 0)); uv2s.Add(new Vector2(0, 0 + index / 4)); uv3s.Add(new Vector2(1, 0));
            vertexs.Add(p1 + heightOffset); uvs.Add(new Vector2(0, 1)); normals.Add(new Vector3(0, 1, 0)); uv2s.Add(new Vector2(0, 1 + index / 4)); uv3s.Add(new Vector2(1, 1));
            vertexs.Add(p2); uvs.Add(new Vector2(dis / _target.height / 4, 0)); normals.Add(new Vector3(0, 1, 0)); uv2s.Add(new Vector2(dis / _target.height / 4, 0 + index / 4)); uv3s.Add(new Vector2(1, 0));
            vertexs.Add(p2 + heightOffset); uvs.Add(new Vector2(dis / _target.height / 4, 1)); normals.Add(new Vector3(0, 1, 0)); uv2s.Add(new Vector2(dis / _target.height / 4, 1 + index / 4)); uv3s.Add(new Vector2(1, 1));

            triangles.Add(index);
            triangles.Add(index + 1);
            triangles.Add(index + 2);

            triangles.Add(index + 2);
            triangles.Add(index + 1);
            triangles.Add(index + 3);

            index += 4;
        }

        //x 
        for(float i = xMinMax.x; i < xMinMax.y; i += _target.step)
        {
            intersectPs = getIntersectPoints(true, i, _target.control_points);
            if (intersectPs.Count <= 1)
                continue;
            for (int j = 0; j < intersectPs.Count-1; j+=2 )
            {
                float dis = Vector3.Distance(intersectPs[j], intersectPs[j + 1]);
                vertexs.Add(intersectPs[j]); uvs.Add(new Vector2(0, 0)); normals.Add(new Vector3(0, 1, 0)); uv2s.Add(new Vector2(0, 0 + index / 4)); uv3s.Add(new Vector2(1, 0));
                vertexs.Add(intersectPs[j] + heightOffset); uvs.Add(new Vector2(0, 1)); normals.Add(new Vector3(0, 1, 0)); uv2s.Add(new Vector2(0, 1 + index / 4)); uv3s.Add(new Vector2(1, 1));
                vertexs.Add(intersectPs[j + 1]); uvs.Add(new Vector2(dis / _target.height / 4, 0)); normals.Add(new Vector3(0, 1, 0)); uv2s.Add(new Vector2(dis / _target.height / 4, 0 + index / 4)); uv3s.Add(new Vector2(1, 0));
                vertexs.Add(intersectPs[j + 1] + heightOffset); uvs.Add(new Vector2(dis / _target.height / 4, 1)); normals.Add(new Vector3(0, 1, 0)); uv2s.Add(new Vector2(dis / _target.height / 4, 1 + index / 4)); uv3s.Add(new Vector2(1, 1));

                triangles.Add(index);
                triangles.Add(index + 1);
                triangles.Add(index + 2);

                triangles.Add(index + 2);
                triangles.Add(index + 1);
                triangles.Add(index + 3);

                index += 4;
            }
        }

        //z 
        for (float i = zMinMax.x; i < zMinMax.y; i += _target.step)
        {
            intersectPs = getIntersectPoints(false, i, _target.control_points);
            if (intersectPs.Count <= 1)
                continue;
            for (int j = 0; j < intersectPs.Count - 1; j += 2)
            {
                float dis = Vector3.Distance(intersectPs[j], intersectPs[j + 1]);
                vertexs.Add(intersectPs[j]); uvs.Add(new Vector2(0, 0)); normals.Add(new Vector3(0, 1, 0)); uv2s.Add(new Vector2(0, 0 + index / 4)); uv3s.Add(new Vector2(0, 0));
                vertexs.Add(intersectPs[j] + heightOffset); uvs.Add(new Vector2(0, 1)); normals.Add(new Vector3(0, 1, 0)); uv2s.Add(new Vector2(0, 1 + index / 4)); uv3s.Add(new Vector2(0, 1));
                vertexs.Add(intersectPs[j + 1]); uvs.Add(new Vector2(dis / _target.height / 4, 0)); normals.Add(new Vector3(0, 1, 0)); uv2s.Add(new Vector2(dis / _target.height / 4, 0 + index / 4)); uv3s.Add(new Vector2(0, 0));
                vertexs.Add(intersectPs[j + 1] + heightOffset); uvs.Add(new Vector2(dis / _target.height / 4, 1)); normals.Add(new Vector3(0, 1, 0)); uv2s.Add(new Vector2(dis / _target.height / 4, 1 + index / 4)); uv3s.Add(new Vector2(0, 1));

                triangles.Add(index);
                triangles.Add(index + 1);
                triangles.Add(index + 2);

                triangles.Add(index + 2);
                triangles.Add(index + 1);
                triangles.Add(index + 3);

                index += 4;
            }
        }

        grassMesh.SetVertices(vertexs);
        grassMesh.SetNormals(normals);
        //grassMesh.SetUVs(0, uvs);
        grassMesh.SetUVs(0, uv2s);
        grassMesh.SetUVs(2, uv3s);
        grassMesh.SetTriangles(triangles, 0);

        GameObject mobj = new GameObject("lxq");

        
        MeshRenderer mr = mobj.AddComponent<MeshRenderer>();
        mr.sharedMaterial = Resources.Load("meshgrass") as Material;
        mr.sharedMaterial.SetFloat("_StepOffset", _target.step + 0.03f);
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        MeshFilter mf = mobj.AddComponent<MeshFilter>();
        mf.sharedMesh = grassMesh;

        return true;
    }

    public override void OnInspectorGUI()
    {
        AreaGrassObject _target = AreaGrassObject.Instance;

        GUILayout.Label("Working mode", EditorStyles.boldLabel);
        int _state = GUILayout.Toolbar(_target.state, _target.stateStrings, GUILayout.Height(30));

        if ((_target.state == 0) && (_state == 1))
        {
            // build mesh
            if (!BuildMesh())
            {
                EditorUtility.DisplayDialog("Error...", "Can't build mesh   ", "Proceed", "");
                return;
            }
        }
        else if ((_target.state == 1) && (_state == 0))
        {
            _target.state = _state;
            EditorUtility.SetDirty(_target);
            return;
        }
        _target.state = _state;

        if(_state == 0)
        {
            if(_target.active_idx >= 0)
            {
                _target.control_points[_target.active_idx] = EditorGUILayout.Vector3Field("Position", _target.control_points[_target.active_idx]);
                EditorUtility.SetDirty(_target);
            }
                
        }
    }

    public void OnSceneGUI()
    {
        AreaGrassObject _target = AreaGrassObject.Instance;

        Event current = Event.current;
        int i;

        switch (current.type)
        {
            case EventType.keyDown:
                if (current.keyCode == KeyCode.Delete)
                {
                    if(_target.active_idx >= 0)
                    {
                        Undo.RecordObject(_target, "delete pos");
                        _target.control_points.RemoveAt(_target.active_idx);
                        _target.active_idx = -1;
                        current.Use();
                    }
                }

                break;
            case EventType.keyUp:
                //					current.Use();
                break;
            case EventType.mouseDown:
                //Debug.Log(current +"     "+controlID);
                float dist;
                float min_dist;

                _target.active_idx = -1;
                // pressing control points
                min_dist = 12f;
                for (i = 0; i < _target.control_points.Count; i++)
                {
                    dist = Vector2.Distance(current.mousePosition, HandleUtility.WorldToGUIPoint(_target.control_points[i]));
                    if (dist < min_dist)
                    {
                        min_dist = dist;
                        _target.active_idx = i;
                    }
                }
                
                if (current.shift && _target.active_idx == -1)
                {
                    //Vector3 insert_pos = new Vector3(0, 0, 0);

                    Undo.RecordObject(_target, "add pos");
                    _target.control_points.Add(GetWorldPointFromMouse());
                    
                    current.Use();
                }
                //current.Use();
                break;
            //				case EventType.mouseMove:
            //				break;
            case EventType.mouseDrag:
                //current.Use();
                break;
            case EventType.mouseUp:
                //current.Use();
                break;
            case EventType.layout:
                // HandleUtility.AddDefaultControl(controlID);
                break;

        }

        // Node Numbers
        for (i = 0; i < _target.control_points.Count; i++)
        {
            Handles.Label(_target.control_points[i], "  " + i);
        }

        // control_points
        for (i = 0; i < _target.control_points.Count; i++)
        {
            Vector3 vec =
                Handles.FreeMoveHandle((_target.control_points[i]),
                                       Quaternion.identity, HandleUtility.GetHandleSize((_target.control_points[i])) * 0.08f, Vector3.one,
                                       Handles.RectangleCap);

            Undo.RecordObject(_target, "modify pos");
            //vec.y = 0;
            _target.control_points[i] = vec;


            Handles.DrawLine(_target.control_points[i], _target.control_points[(i + 1) % _target.control_points.Count]);
        }

        Handles.color = Color.gray;
    }
	
}
