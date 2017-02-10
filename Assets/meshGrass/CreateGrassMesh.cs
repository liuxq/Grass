using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

public class CreateGrassMesh : EditorWindow {

    [MenuItem("Window/createWindow", false, 18)]
	public static void Init()
    {
        EditorWindow.GetWindow<CreateGrassMesh>(false, "createMesh");
    }

    void OnGUI()
    {
        if (GUILayout.Button("create Mesh"))
        {
            CreateMesh(-10,10,-10,10, 0.2f);
        }
    }

    void CreateMesh(float left, float right, float bottom, float top, float height)
    {
        Mesh grassMesh = new Mesh();
        List<Vector3> vertexs = new List<Vector3>();
        List<int> triangles = new List<int>();

        int xlen = 0;
        int ylen = 0;
        for(float i = left; i < right; i++)
        {
            xlen++;
            for (float j = bottom; j < top; j++)
            {
                ylen++;
                vertexs.Add(new Vector3(i, j, 0));
                vertexs.Add(new Vector3(i, j, height));
            }


        }


    }
}
