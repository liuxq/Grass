//////////////////////////////////////////////////////////////////////////////////////////////////////
//
// VolumeGrass class. Add this script component to a game object with mesh you'd like to use as grass surface
//
// (C) Tomasz Stobierski 2014
//
//////////////////////////////////////////////////////////////////////////////////////////////////////
using UnityEngine;
using System;

[AddComponentMenu("Volume Grass/Components for grass/Volume Grass on custom mesh")]
[RequireComponent (typeof (MeshFilter))]
public class VolumeGrass_CustomMesh : MonoBehaviour {
	public int slices_num=4;
	public float plane_num=8;
	public float extrudeHeight=0.15f; 
	
	public bool init_colors=true;
	public Mesh originalMesh;
	public string save_path_material="";
	
//	void Start() {
//	}

}
