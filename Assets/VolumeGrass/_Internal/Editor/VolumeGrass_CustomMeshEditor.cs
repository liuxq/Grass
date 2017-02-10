using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor (typeof(VolumeGrass_CustomMesh))]
public class VolumeGrass_CustomMeshEditor : Editor {
#if UNITY_EDITOR	
	public override void OnInspectorGUI () {
		VolumeGrass_CustomMesh _target=(VolumeGrass_CustomMesh)target;
		
		GUILayout.BeginHorizontal();
			GUILayout.Label ("Slices on blades texture", EditorStyles.label );
			float prev_slices_num=_target.slices_num;
			_target.slices_num=EditorGUILayout.IntSlider(_target.slices_num, 1, 16);
			if (_target.slices_num!=prev_slices_num) {
				setupShader();
				Undo.RecordObject(_target, "grass edit");
				EditorUtility.SetDirty(_target);
			}
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
			GUILayout.Label ("Slice planes per world unit", EditorStyles.label );
			float UV_ratio=1.0f/(_target.extrudeHeight*_target.slices_num);
			float prev_plane_num=_target.plane_num;
			_target.plane_num=EditorGUILayout.Slider( _target.plane_num*UV_ratio, 0.3f, 80f)/UV_ratio;
			if (_target.plane_num!=prev_plane_num) {
				setupShader();
				Undo.RecordObject(_target, "grass edit");
				EditorUtility.SetDirty(_target);
			}
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
			GUILayout.Label ("Grass height in world units", EditorStyles.label );
			float nh=EditorGUILayout.Slider(_target.extrudeHeight, 0.02f, 3f);
			if (nh!=_target.extrudeHeight) {
				_target.extrudeHeight=nh;
				Undo.RecordObject(_target, "grass edit");
				EditorUtility.SetDirty(_target);
			}		
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
			GUILayout.Label ("Init vertices colors (for coverage and sidewalls init)", EditorStyles.label);
			bool ninit_color = EditorGUILayout.Toggle ( _target.init_colors);
			if (ninit_color!=_target.init_colors) {
				Undo.RecordObject(_target, "grass edit");
				EditorUtility.SetDirty(_target);			
				_target.init_colors=ninit_color;
			}
		GUILayout.EndHorizontal();
			
		GUILayout.Space(10);
		Color c=GUI.color;
		GUI.color=new Color(0.9f,1, 0.9f);
		if (GUILayout.Button("Setup Mesh & Material params")) {
			RebuildMesh();
			setupShader();
		}
		GUI.color=c;		
		GUILayout.Space(4);
		if (_target.GetComponent<Renderer>()) {
			EditorGUI.BeginDisabledGroup( _target.GetComponent<Renderer>().sharedMaterial==null || AssetDatabase.GetAssetPath(_target.GetComponent<Renderer>().sharedMaterial)!="");
			if (GUILayout.Button("Save material to file", GUILayout.Height (25))) {
				SaveMaterial(_target.GetComponent<Renderer>().sharedMaterial, ref _target.save_path_material, "Grass material.mat");
			}
			EditorGUI.EndDisabledGroup();
		}		
		if (_target.originalMesh) {
			//_target.originalMesh=(Mesh)EditorGUILayout.ObjectField("Original Mesh", _target.originalMesh, typeof(Mesh), false);
			EditorGUI.BeginDisabledGroup(true);
			EditorGUILayout.ObjectField("Original Mesh", _target.originalMesh, typeof(Mesh), false);
			EditorGUI.EndDisabledGroup();
		}
		GUILayout.Space(6);
	}
	
	public void setupShader() {
		VolumeGrass_CustomMesh _target=(VolumeGrass_CustomMesh)target;
		if ((_target.GetComponent<Renderer>()!=null) && (_target.GetComponent<Renderer>().sharedMaterial!=null)) {
			_target.GetComponent<Renderer>().sharedMaterial.SetFloat("PLANE_NUM", _target.plane_num);
			_target.GetComponent<Renderer>().sharedMaterial.SetFloat("GRASS_SLICE_NUM", 1.0f*_target.slices_num);
			_target.GetComponent<Renderer>().sharedMaterial.SetFloat("HEIGHT", _target.extrudeHeight);
		}
		
	}
	
	private void RebuildMesh() {
		VolumeGrass_CustomMesh _target=(VolumeGrass_CustomMesh)target;
		Mesh mesh;
		MeshFilter meshFilter=_target.transform.GetComponent(typeof(MeshFilter)) as MeshFilter;
		if (_target.originalMesh) {
			mesh=_target.originalMesh;
		} else {
			_target.originalMesh=meshFilter.sharedMesh;
			mesh=meshFilter.sharedMesh;
			EditorUtility.CopySerialized(meshFilter.sharedMesh, mesh);
		}
		
		if (_target.GetComponent<Renderer>().sharedMaterial!=null && _target.GetComponent<Renderer>().sharedMaterial.shader==Shader.Find("Grass Shader")) {
			setupShader();
		} else {
			Material mat=null;
			Material default_mat=(Material)(AssetDatabase.LoadAssetAtPath("Assets/VolumeGrass/_Shaders and Materials/Grass.mat", typeof(Material)));
			if (default_mat) {
				mat=Object.Instantiate(default_mat) as Material;
				mat.name=default_mat.name;
				_target.GetComponent<Renderer>().sharedMaterial=mat;
				//Debug.Log (mat);
			} else {
				EditorUtility.DisplayDialog("Warning...", "Can't find default material at:\nAssets/VolumeGrass/_Shaders and Materials/Grass.mat   \n\nYou'll have to assing material properties\n(textures) by hand.", "Proceed", "");
				mat=new Material(Shader.Find("Grass Surface Shader (no zwrite)"));
				if (mat) {
					mat.name="Grass";
					_target.GetComponent<Renderer>().sharedMaterial=mat;
				} else {
					EditorUtility.DisplayDialog("Warning...", "Can't find default material at:\nAssets/VolumeGrass/_Shaders and Materials/Grass.mat   \n\nYou'll have to assing material properties\n(textures) by hand.", "Proceed", "");
				}
			}
			_target.GetComponent<Renderer>().sharedMaterial.shader=Shader.Find("Grass Surface Shader (no zwrite)");
			if (_target.GetComponent<Renderer>().sharedMaterial) _target.GetComponent<Renderer>().sharedMaterial.SetFloat("_tiling_factor", (_target.extrudeHeight*_target.slices_num));
		}
		_target.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;

		Color32[] colors=mesh.colors32;
		if (colors==null || colors.Length==0 || _target.init_colors) {
			colors=new Color32[mesh.vertices.Length];
			for(int i=0; i<colors.Length; i++) {
				colors[i]=new Color32(0,0,0,0);
			}
		}
		
		Vector3[] vertices=mesh.vertices;
		int[] triangles=mesh.triangles;
		ArrayList outer_edges=new ArrayList();
		for(int i=0; i<triangles.Length; i+=3) {
			if (check_edge(triangles[i], triangles[i+1], i, triangles)) { outer_edges.Add(triangles[i]); outer_edges.Add(triangles[i+1]); }
			if (check_edge(triangles[i+1], triangles[i+2], i, triangles)) { outer_edges.Add(triangles[i+1]); outer_edges.Add(triangles[i+2]); }
			if (check_edge(triangles[i+2], triangles[i], i, triangles)) { outer_edges.Add(triangles[i+2]); outer_edges.Add(triangles[i]); }
		}
		if (outer_edges.Count==0) {
			EditorUtility.DisplayDialog("Error...", "Can't extrude mesh   ", "Proceed", "");
			return;
		}
		int[] _outer_edges=(int[])outer_edges.ToArray(typeof(int));
		ArrayList new_vertices=new ArrayList();
		for(int i=0; i<outer_edges.Count; i++) {
			if (new_vertices.IndexOf(outer_edges[i])<0) new_vertices.Add(outer_edges[i]);
		}
		int[] _new_vertices=(int[])new_vertices.ToArray(typeof(int));
				
		Vector3[] normals=mesh.normals;
		Vector2[] uv2=mesh.uv2;
		Vector3[] normals_extruded=new Vector3[normals.Length + new_vertices.Count];
		Vector3[] vertices_extruded=new Vector3[vertices.Length + new_vertices.Count];
		Vector2[] uv2_extruded=new Vector2[uv2.Length + new_vertices.Count];
		Color32[] colors_extruded=new Color32[vertices.Length + new_vertices.Count];
		
		for(int i=0; i<vertices.Length; i++) {
			vertices_extruded[i]=vertices[i];
			colors_extruded[i]=colors[i];
		}
		int index_offset=vertices.Length;
		for(int i=0; i<_new_vertices.Length; i++) {
			vertices_extruded[i+index_offset]=vertices[_new_vertices[i]]-_target.extrudeHeight*normals[_new_vertices[i]];
			Color32 col;
			if (_new_vertices[i]<colors.Length) col=colors[_new_vertices[i]]; else col=new Color32();
			colors_extruded[i+index_offset]=col;
			colors_extruded[i+index_offset].r=255;			
			colors_extruded[i+index_offset].g=255;	
		}
		
		for(int i=0; i<normals.Length; i++) {
			normals_extruded[i]=normals[i];
		}
		index_offset=normals.Length;
		for(int i=0; i<_new_vertices.Length; i++) {
			normals_extruded[i+index_offset]=normals[_new_vertices[i]];
		}
		
		if (uv2.Length>0) {
			for(int i=0; i<uv2.Length; i++) {
				uv2_extruded[i]=uv2[i];
			}
			index_offset=uv2.Length;
			for(int i=0; i<_new_vertices.Length; i++) {
				uv2_extruded[i+index_offset]=uv2[_new_vertices[i]];
			}
		}
		
		int[] triangles_extruded=new int[triangles.Length + _outer_edges.Length*3];
		for(int i=0; i<triangles.Length; i+=3) {
			triangles_extruded[i]=triangles[i];
			triangles_extruded[i+1]=triangles[i+1];
			triangles_extruded[i+2]=triangles[i+2];
		}
		index_offset=triangles.Length;
		int index_offset2=vertices.Length;
		for(int i=0; i<_outer_edges.Length; i+=2) {
			triangles_extruded[index_offset+2]=_outer_edges[i];
			triangles_extruded[index_offset+1]=_outer_edges[i+1];
			for(int j=0; j<_new_vertices.Length; j++) {
				if (_new_vertices[j]==_outer_edges[i]) {
					triangles_extruded[index_offset]=j+index_offset2;
					break;
				}
			}
			index_offset+=3;
			triangles_extruded[index_offset+2]=triangles_extruded[index_offset-3];
			triangles_extruded[index_offset+1]=_outer_edges[i+1];
			for(int j=0; j<_new_vertices.Length; j++) {
				if (_new_vertices[j]==_outer_edges[i+1]) {
					triangles_extruded[index_offset]=j+index_offset2;
					break;
				}
			}
			index_offset+=3;			
		}
		
		Mesh new_mesh=new Mesh();
		new_mesh.vertices=vertices_extruded;
		new_mesh.normals=normals_extruded;
		if (uv2.Length>0) new_mesh.uv2=uv2_extruded;
		new_mesh.colors32=colors_extruded;
		new_mesh.triangles=triangles_extruded;
		new_mesh.name=_target.originalMesh.name+" (extruded)";
			
		new_mesh.RecalculateBounds();
		new_mesh.Optimize();
		meshFilter.mesh=new_mesh;		
		
		// sidewall mesh
		vertices_extruded=new Vector3[new_vertices.Count*2];
		normals_extruded=new Vector3[new_vertices.Count*2];
		
		ArrayList vertices_idxmap=new ArrayList();
		for(int i=0; i<_new_vertices.Length; i++) {
			vertices_idxmap.Add(_new_vertices[i]);
			
			vertices_extruded[i]=vertices[_new_vertices[i]];
			vertices_extruded[i+_new_vertices.Length]=vertices[_new_vertices[i]]-normals[_new_vertices[i]]*_target.extrudeHeight;
			
			normals_extruded[i]=normals[_new_vertices[i]];
			normals_extruded[i+_new_vertices.Length]=normals[_new_vertices[i]];
		}
		
		Mesh sidewall_mesh=new Mesh();
		sidewall_mesh.vertices=vertices_extruded;
		sidewall_mesh.normals=normals_extruded;
		
		triangles_extruded=new int[_outer_edges.Length*3];
		index_offset=0;
		for(int i=0; i<_outer_edges.Length; i+=2) {
			triangles_extruded[index_offset] = vertices_idxmap.IndexOf( _outer_edges[i] );
			triangles_extruded[index_offset+1] = vertices_idxmap.IndexOf( _outer_edges[i+1] );
			triangles_extruded[index_offset+2] = triangles_extruded[index_offset] + new_vertices.Count;
			index_offset+=3;
			triangles_extruded[index_offset] = vertices_idxmap.IndexOf( _outer_edges[i+1] );
			triangles_extruded[index_offset+1] = triangles_extruded[index_offset] + new_vertices.Count;
			triangles_extruded[index_offset+2] = vertices_idxmap.IndexOf( _outer_edges[i] ) + new_vertices.Count;
			index_offset+=3;			
		}
		sidewall_mesh.triangles=triangles_extruded;
		
		sidewall_mesh.RecalculateBounds();
		sidewall_mesh.Optimize();
		sidewall_mesh.name=_target.originalMesh.name+" (sidewall extruded)";
		
		Transform sidewalls=_target.transform.Find("sidewalls");
		if (sidewalls==null) {
			GameObject go=new GameObject("sidewalls");
			MeshFilter mf;
			go.layer=GrassRenderingReservedLayer.layer_num;
			sidewalls=go.transform;
			sidewalls.position=_target.transform.position;
			sidewalls.rotation=_target.transform.rotation;
			sidewalls.localScale=_target.transform.localScale;
			sidewalls.parent=_target.transform;
	        go.AddComponent(typeof(MeshRenderer));
        	mf = go.AddComponent(typeof(MeshFilter)) as MeshFilter;
	        mf.sharedMesh = sidewall_mesh;
			go.GetComponent<Renderer>().sharedMaterial=new Material(Shader.Find("GrassRenderDepth"));
			go.GetComponent<Renderer>().receiveShadows=false;
			go.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
		} else {
			GameObject go=sidewalls.gameObject;
			MeshFilter mf;
			go.layer=GrassRenderingReservedLayer.layer_num;
			if (go.GetComponent(typeof(MeshRenderer))==null) {
		        go.AddComponent(typeof(MeshRenderer));
			}
			if (go.GetComponent(typeof(MeshFilter))==null) {
		        go.AddComponent(typeof(MeshFilter));
			}
			mf = go.GetComponent(typeof(MeshFilter)) as MeshFilter;
	        mf.sharedMesh = sidewall_mesh;
			if (go.GetComponent<Renderer>().sharedMaterial!=Shader.Find("GrassRenderDepth")) {
				go.GetComponent<Renderer>().sharedMaterial=new Material(Shader.Find("GrassRenderDepth"));
			}
			go.GetComponent<Renderer>().receiveShadows=false;
			go.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
		}
	}
			
	private bool check_edge(int ev0, int ev1, int i, int[] triangles) {
		int eV0;
		int eV1;
		
		for(int j=0; j<triangles.Length; j+=3) {
			if (i!=j) {
				eV0=triangles[j]; eV1=triangles[j+1];
				if (((eV0==ev0) && (eV1==ev1)) || ((eV1==ev0) && (eV0==ev1))) return false;
				eV0=triangles[j+1]; eV1=triangles[j+2];
				if (((eV0==ev0) && (eV1==ev1)) || ((eV1==ev0) && (eV0==ev1))) return false;
				eV0=triangles[j+2]; eV1=triangles[j];
				if (((eV0==ev0) && (eV1==ev1)) || ((eV1==ev0) && (eV0==ev1))) return false;
			}
		}
		return true;		
	}	
	
	private Vector2 get_UV2ObjectScale(int i1, int i2, int i3, Vector2[] uvs, Vector3[] vertices) {
		float u1,u2,u3;
		float v1,v2,v3;
		u1=uvs[i1].x; u2=uvs[i2].x; u3=uvs[i3].x;
		v1=uvs[i1].y; v2=uvs[i2].y; v3=uvs[i3].y;
		Vector3 p1=vertices[i1];
		Vector3 p2=vertices[i2];
		Vector3 p3=vertices[i3];
		Vector2 scale=new Vector2();
		scale.x=Vector3.Magnitude(( (v3 - v1)*(p2 - p1) - (v2 - v1)*(p3 - p1) ) / ( (u2 - u1)*(v3 - v1) - (v2 - v1)*(u3 - u1) ));
		scale.y=Vector3.Magnitude(( (u3 - u1)*(p2 - p1) - (u2 - u1)*(p3 - p1) ) / ( (v2 - v1)*(u3 - u1) - (u2 - u1)*(v3 - v1) ));
		return scale;
	}	
	
	private Material SaveMaterial(Material mat, ref string save_path, string default_name) {
		Material saved_mat=null;
		
		string directory;
		string file;
		if (save_path=="") {
			directory=Application.dataPath;
			file=default_name;
		} else {
			directory=System.IO.Path.GetDirectoryName(save_path);
			file=System.IO.Path.GetFileNameWithoutExtension(save_path)+".mat";
		}
		string path = EditorUtility.SaveFilePanel("Save material", directory, file, "mat");
		if (path!="") {
			path=path.Substring(Application.dataPath.Length-6);
			save_path=path;
			AssetDatabase.CreateAsset(mat, path);
			AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
			saved_mat=AssetDatabase.LoadAssetAtPath(path, typeof(Material)) as Material;
		}		
		
		return saved_mat;
	}		
#endif
}
