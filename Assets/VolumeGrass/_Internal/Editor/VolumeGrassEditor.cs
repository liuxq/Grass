using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor (typeof(VolumeGrass))]
public class VolumeGrassEditor : Editor {
#if UNITY_EDITOR	
	private Vector3[] cover_verts=new Vector3[500];
	//private Quaternion[] cover_norms=new Quaternion[500];
	//private Quaternion[] cover_norms_flip=new Quaternion[500];
	private	float[] cover_strength=new float[500];
	private	int[] cover_indices=new int[500];
	private	int cover_verts_num=0;
	private int cover_verts_num_start_drag=0;
	private float lCovTim=0;
	private bool control_down_flag=false;
	private Tool prev_tool;
	
	// icons
	private Texture paintButTexOn;
	private Texture paintButTexOff;
	
	void OnEnable() {
		if (paintButTexOn==null) paintButTexOn=AssetDatabase.LoadAssetAtPath("Assets/VolumeGrass/_Internal/icoPaintOn.png", typeof(Texture)) as Texture;
		if (paintButTexOff==null) paintButTexOff=AssetDatabase.LoadAssetAtPath("Assets/VolumeGrass/_Internal/icoPaintOff.png", typeof(Texture)) as Texture;
		
		prev_tool=Tools.current;
		
		VolumeGrass _target=(VolumeGrass)target;
		_target.disableSidewallsLightmapping();
		//VolumeGrass _target=(VolumeGrass)target;		
		
		EditorApplication.playmodeStateChanged += PlaymodeStateChange;
	}
	
	void PlaymodeStateChange(){
		if (EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPlaying) {
			VolumeGrass _target=(VolumeGrass)target;
			if (_target) {
				if (_target.paint_height) {
					_target.paint_height=false;
					SceneView.onSceneGUIDelegate -= VolumeGrass._SceneGUI;
				};
				EditorUtility.SetDirty(target);
				Tools.current=prev_tool;
			}
		}
	}
	
	void OnDisable() {
		VolumeGrass _target=(VolumeGrass)target;
		if (_target) {
			if (_target.paint_height) {
				_target.paint_height=false;
				SceneView.onSceneGUIDelegate -= VolumeGrass._SceneGUI;
			};
			EditorUtility.SetDirty(target);
		}
		Tools.current=prev_tool;
		if (!_target || !_target.transform) return;
		_target.disableSidewallsLightmapping();
		EditorApplication.playmodeStateChanged -= PlaymodeStateChange;
	}
	
	public override void OnInspectorGUI () {
		VolumeGrass _target=(VolumeGrass)target;
		
		GUILayout.Space(10);
		EditorGUILayout.BeginVertical("box");
		Color c=GUI.color;
		GUI.color=new Color(1,1, 0.5f);
		GUILayout.Space (-1);
		GUILayout.Label ("Working mode", EditorStyles.boldLabel);
		GUI.color=c;		
		int _state = GUILayout.Toolbar(_target.state, _target.stateStrings, GUILayout.Height (30));
		MeshFilter filter = _target.gameObject.GetComponent(typeof(MeshFilter)) as MeshFilter;
		if ((_target.state==0) && (_state==1)) {
			// build mesh
			if (!_target.BuildMesh()) {
				EditorUtility.DisplayDialog("Error...", "Can't build mesh   ", "Proceed", "");
				return; // nie można zbudowac mesha (np. za mało wierzchołków)
			}
		} else if ((_target.state==1) && (_state==0)) {
			if (filter && filter.sharedMesh) {
				filter.sharedMesh=null;
				MeshFilter filter_sidewalls = null;
				Transform tr=_target.transform.Find("sidewalls");
				if (tr!=null) filter_sidewalls=tr.gameObject.GetComponent(typeof(MeshFilter)) as MeshFilter;
				if (filter_sidewalls && filter_sidewalls.sharedMesh) filter_sidewalls.sharedMesh=null;
			}
			_target.state=_state;
			EditorUtility.SetDirty(_target);
			return;
		}
		_target.state=_state;
		EditorGUILayout.EndVertical();
		
		GUILayout.Space(10);
		
		GUIStyle boldFoldoutStyle = new GUIStyle(EditorStyles.foldout);
		boldFoldoutStyle.fontStyle = FontStyle.Bold;
		
		EditorGUILayout.BeginVertical("box");
		c=GUI.color;
		GUI.color=new Color(1,1, 0.5f);
		GUILayout.BeginHorizontal();
		GUILayout.Space(12);
		_target.show_build_settings=EditorGUILayout.Foldout (_target.show_build_settings, "Build settings", boldFoldoutStyle);
		GUILayout.EndHorizontal();
		GUI.color=c;
		GUILayout.BeginHorizontal();
		GUILayout.Space(10);
		GUILayout.BeginVertical();
		if (_target.show_build_settings) {
			GUILayout.BeginHorizontal();
				GUILayout.Label ("Slices on blades texture", EditorStyles.label, GUILayout.Width (180)  );
				float prev_slices_num=_target.slices_num;
				_target.slices_num=EditorGUILayout.IntSlider(_target.slices_num, 1, 64);
				if (_target.slices_num!=prev_slices_num) {
					_target.setupLODAndShader(_target.act_lod, true);
				}
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();
				GUILayout.Label ("Slice planes per world unit", EditorStyles.label, GUILayout.Width (180)  );
				float UV_ratio=1.0f/(_target.mesh_height*_target.slices_num);
				float prev_plane_num=_target.plane_num;
				_target.plane_num=EditorGUILayout.Slider( _target.plane_num*UV_ratio, 0.3f, 80f)/UV_ratio;
				if (_target.plane_num!=prev_plane_num) {
					_target.setupLODAndShader(_target.act_lod, true);
				}
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();
				GUILayout.Label ("Grass height in world units", EditorStyles.label, GUILayout.Width (180)  );
				_target.mesh_height=EditorGUILayout.Slider(_target.mesh_height, 0.02f, 3f);
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();
				GUILayout.Label ("Additional mesh height offset", EditorStyles.label, GUILayout.Width (180)  );
				_target.add_height_offset=EditorGUILayout.Slider (_target.add_height_offset, -2f, 2f);
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();
				GUILayout.Label("Subnode optimization threshold angle", EditorStyles.label);
				_target.colinear_treshold = EditorGUILayout.Slider (_target.colinear_treshold, 91f, 180f);
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();
				GUILayout.Label ("Follow ground shape", EditorStyles.label, GUILayout.Width (125)  );
				_target.snap_on_build = EditorGUILayout.Toggle (_target.snap_on_build);
			GUILayout.EndHorizontal();
			if (_target.snap_on_build) {
				GUILayout.BeginHorizontal();
					GUILayout.Label ("     Max height error", EditorStyles.label);
					_target.max_y_error[_target.act_lod] = EditorGUILayout.Slider ( _target.max_y_error[_target.act_lod], 0.01f, 1);
				GUILayout.EndHorizontal();
				GUILayout.BeginHorizontal();
					GUILayout.Label ("     Min endge length", EditorStyles.label);
					_target.min_edge_length[_target.act_lod] = EditorGUILayout.Slider (_target.min_edge_length[_target.act_lod], 0.01f, 5);
				GUILayout.EndHorizontal();
			}
			
//			GUILayout.BeginHorizontal();
//				GUILayout.Label ("UV2 range (lightmap 0..u, 0..v)", EditorStyles.label);
//				_target.UV2range.x = EditorGUILayout.Slider (_target.UV2range.x, 0.1f, 1f);
//				_target.UV2range.y = EditorGUILayout.Slider (_target.UV2range.y, 0.1f, 1f);
//			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();
				GUILayout.Label ("Fadeout borders", EditorStyles.label, GUILayout.Width (125)  );
				_target.auto_border_transitions = EditorGUILayout.Toggle ( _target.auto_border_transitions);
			GUILayout.EndHorizontal();
		}
		
		GUILayout.Space(8);
		GUILayout.BeginVertical("box");
		if ((filter==null) || (filter.sharedMesh==null)) {
			GUILayout.Label ("Current LOD mesh ", EditorStyles.miniLabel);
		} else {
			GUILayout.Label ("Current LOD mesh ("+filter.sharedMesh.vertices.Length+" verts, "+(filter.sharedMesh.triangles.Length/3)+" tris)", EditorStyles.miniLabel);
		}
		if (_target.state==1) {
			GUILayout.BeginHorizontal();
			GUILayout.Label("Used when distance less than ", EditorStyles.miniLabel);
			_target.LOD_distances[_target.act_lod]=EditorGUILayout.IntField(_target.LOD_distances[_target.act_lod]);
			GUILayout.EndHorizontal();
		}
		for(int i=0; i<4; i++) {
			if (_target.LODs[i]==null) {
				_target.lodStrings[i]=_target.lodStrings_empty[i];
			} else {
				_target.lodStrings[i]=_target.lodStrings_occupied[i];
			}
		}
		int act_lod = GUILayout.Toolbar(_target.act_lod, _target.lodStrings);
		if (_target.act_lod!=act_lod) {
			if (_target.state==1) {
				_target.setupLODAndShader(act_lod, true);
			}
			_target.act_lod=act_lod;
		}
		if (_target.state==1) {
			GUILayout.BeginHorizontal();
			string lb;
			if (_target.LODs[_target.act_lod]!=null) {
				lb="Rebuild LOD";
			} else {
				lb="Build LOD";
			}
			
			c=GUI.color;
			GUI.color=new Color(0.9f,1, 0.9f);
			if (GUILayout.Button(lb, GUILayout.Height(20))) {
				if (!_target.BuildMesh()) {
					EditorUtility.DisplayDialog("Error...", "Can't build mesh   ", "Proceed", "");
				}
			}
			GUI.color=c;
			
			if (_target.LODs[_target.act_lod]!=null) {
				c=GUI.color;
				GUI.color=new Color(1, 0.9f, 0.9f);
				if (GUILayout.Button("Delete LOD", GUILayout.Height(20))) {
					if ((filter!=null) && (filter.sharedMesh!=null)) {
						filter.sharedMesh=null;
						DestroyImmediate(_target.LODs[_target.act_lod]);
						_target.LODs[_target.act_lod]=null;
						if (_target.LODs_sidewalls[_target.act_lod]!=null) {
							DestroyImmediate(_target.LODs_sidewalls[_target.act_lod]);
							_target.LODs_sidewalls[_target.act_lod]=null;
						}
					}
				}
				GUI.color=c;
			}
			GUILayout.EndHorizontal();
		}	
		GUILayout.EndVertical();		
		
		if (_target.active_idx>=0) {
			GUILayout.Space(8);
			GUILayout.BeginVertical("box");
			GUILayout.Space(1);
			if (_target.which_active==3) {
				// tesselation points
				c=GUI.color;
				GUI.color=new Color(1,1, 0.5f);
				GUILayout.Label ("Selected tesselation point properties", EditorStyles.boldLabel);
				GUI.color=c;			
				Vector3 vec;
				if (_target.localGlobalState==0) {
					// local
					vec = EditorGUILayout.Vector3Field("Position", _target.tesselation_points[_target.active_idx]);
				} else {
					// global
					vec = _target.T_wl(EditorGUILayout.Vector3Field("Position", _target.T_lw(_target.tesselation_points[_target.active_idx])));
				}
				if (_target.ConstrainPoints(vec)) EditorUtility.SetDirty(_target);
				GUILayout.BeginHorizontal();
				_target.localGlobalState = GUILayout.Toolbar(_target.localGlobalState, _target.localGlobalStrings);
				if (GUILayout.Button("Delete Point", GUILayout.Width(100), GUILayout.Height(20))) {
					Undo.RecordObjects(new Object[2]{_target, _target.transform}, "grass edit");
					_target.DeleteActiveControlPoint();
					EditorUtility.SetDirty(_target);
				}
				GUILayout.EndHorizontal();
			} else {
				// nodes
				c=GUI.color;
				GUI.color=new Color(1,1, 0.5f);
				GUILayout.Space (-1);
				GUILayout.Label ("Selected node properties", EditorStyles.boldLabel);
				GUI.color=c;			
				if (_target.state==0) {
					Vector3 vec;
					if (_target.localGlobalState==0) {
						// local
						if (_target.which_active==0) {
							vec = EditorGUILayout.Vector3Field("Position", _target.control_points[_target.active_idx]);
						} else if (_target.which_active==1) {
							vec = EditorGUILayout.Vector3Field("Position", _target.bezier_pointsA[_target.active_idx]);
						} else {
							vec = EditorGUILayout.Vector3Field("Position", _target.bezier_pointsB[_target.active_idx]);
						}
					} else {
						// global
						if (_target.which_active==0) {
							vec = _target.T_wl(EditorGUILayout.Vector3Field("Position", _target.T_lw(_target.control_points[_target.active_idx])));
						} else if (_target.which_active==1) {
							vec = _target.T_wl(EditorGUILayout.Vector3Field("Position", _target.T_lw(_target.bezier_pointsA[_target.active_idx])));
						} else {
							vec = _target.T_wl(EditorGUILayout.Vector3Field("Position", _target.T_lw(_target.bezier_pointsB[_target.active_idx])));
						}
					}
					if (_target.ConstrainPoints(vec)) EditorUtility.SetDirty(_target);
					GUILayout.BeginHorizontal();
					_target.localGlobalState = GUILayout.Toolbar(_target.localGlobalState, _target.localGlobalStrings);
					if (GUILayout.Button("Delete Node", GUILayout.Width(100), GUILayout.Height(20))) {
						Undo.RecordObjects(new Object[2]{_target, _target.transform}, "grass edit");
						_target.DeleteActiveControlPoint();
						EditorUtility.SetDirty(_target);
					}
					GUILayout.EndHorizontal();
				}
				GUILayout.BeginHorizontal();
				GUILayout.Label ("Subnodes count", EditorStyles.label, GUILayout.Width (165) );
				int subs = EditorGUILayout.IntSlider ((int)_target.subdivisions[_target.active_idx], 1, 50);
				GUILayout.EndHorizontal();		
				if (subs!=_target.subdivisions[_target.active_idx]) {
					_target.subdivisions[_target.active_idx]=subs;
					EditorUtility.SetDirty(_target);
				}
				GUILayout.BeginHorizontal();
				GUILayout.Label ("Optimize collinear subnodes", EditorStyles.label, GUILayout.Width (165) );
				_target.optimize_subnodes[_target.active_idx] = EditorGUILayout.Toggle(_target.optimize_subnodes[_target.active_idx]);
				GUILayout.EndHorizontal();		
				EditorGUI.BeginDisabledGroup(!_target.optimize_subnodes[_target.active_idx]);	
				GUILayout.BeginHorizontal();
				GUILayout.Label("                  threshold angle", EditorStyles.label);
				_target.colinear_treshold = EditorGUILayout.Slider (_target.colinear_treshold, 91f, 180f);
				GUILayout.EndHorizontal();				
				EditorGUI.EndDisabledGroup();
				GUILayout.BeginHorizontal();
				GUILayout.Label ("Build sidewall", EditorStyles.label, GUILayout.Width (90) );
				_target.side_walls[_target.active_idx] = EditorGUILayout.Toggle(_target.side_walls[_target.active_idx]);
				GUILayout.EndHorizontal();					
			}
			GUILayout.EndVertical();
		}	
		
		GUILayout.EndVertical();
		GUILayout.EndHorizontal();
		EditorGUILayout.EndVertical();
		
		// uv1
		GUILayout.Space(6);
		EditorGUILayout.BeginVertical("box");
		c=GUI.color;
		GUI.color=new Color(1,1, 0.5f);
		GUILayout.BeginHorizontal();
		GUILayout.Space(12);
		_target.show_uv_mapping=EditorGUILayout.Foldout (_target.show_uv_mapping, "UV for global maps", boldFoldoutStyle);
		GUILayout.EndHorizontal();
		GUI.color=c;
		GUILayout.BeginHorizontal();
		GUILayout.Space(10);
		GUILayout.BeginVertical();
		Vector4 UV1bounds=Vector4.zero;
		if (_target.show_uv_mapping) {
			if (_target.GetComponent<Renderer>() && _target.GetComponent<Renderer>().sharedMaterial && _target.GetComponent<Renderer>().sharedMaterial.HasProperty("UVbounds")) {
				Vector4 old_bounds=_target.GetComponent<Renderer>().sharedMaterial.GetVector("UVbounds");
				UV1bounds=old_bounds;
				Vector4 new_bounds = EditorGUILayout.Vector4Field("Bounds rect (X=xMin, Y=xMax, Z=zMin, W=zMax)", old_bounds);
				if (Vector4.Distance(old_bounds, new_bounds)>0) {
					UV1bounds=new_bounds;
					_target.GetComponent<Renderer>().sharedMaterial.SetVector("UVbounds", new_bounds);
					EditorUtility.SetDirty(_target);
				}
				EditorGUILayout.BeginHorizontal();
					if (GUILayout.Button("Align to bounds")) {
						Vector4 uvBounds_tmp=_target.UV2bounds;
						_target.getUV2Bounds();
						UV1bounds=_target.UV2bounds;
						_target.GetComponent<Renderer>().sharedMaterial.SetVector("UVbounds", _target.UV2bounds);
						_target.UV2bounds=uvBounds_tmp;
					}
					if (GUILayout.Button("Get from underlying terrain")) {
						Ray ray = new Ray(_target.transform.position+Vector3.up*10000,-Vector3.up);
						RaycastHit[] hits=Physics.RaycastAll(ray);
						bool found=false;
						for(int i=0; i<hits.Length; i++) {
							Terrain ter=hits[i].transform.gameObject.GetComponent(typeof(Terrain)) as Terrain;
							if (ter && ter.terrainData) {
								_target.GetComponent<Renderer>().sharedMaterial.SetVector("UVbounds", new Vector4(ter.GetPosition().x, ter.GetPosition().x+ter.terrainData.size.x, ter.GetPosition().z, ter.GetPosition().z+ter.terrainData.size.z));
								found=true;
								break;
							}
						}
						if (!found) EditorUtility.DisplayDialog ("Notification", "No terrain found under volume grass transform...","OK");
					}
				EditorGUILayout.EndHorizontal();
			}
		}
		GUILayout.EndVertical();
		GUILayout.EndHorizontal();
		EditorGUILayout.EndVertical();		
		
		// uv2
		GUILayout.Space(6);
		EditorGUILayout.BeginVertical("box");
		c=GUI.color;
		GUI.color=new Color(1,1, 0.5f);
		GUILayout.BeginHorizontal();
		GUILayout.Space(12);
		_target.show_uv2_mapping=EditorGUILayout.Foldout (_target.show_uv2_mapping, "UV2 for lightmapping", boldFoldoutStyle);
		GUILayout.EndHorizontal();
		GUI.color=c;
		GUILayout.BeginHorizontal();
		GUILayout.Space(10);
		GUILayout.BeginVertical();
		if (_target.show_uv2_mapping) {		
			EditorGUILayout.BeginHorizontal();
			_target.lightmapped=EditorGUILayout.Toggle("Use lightmapping", _target.lightmapped);		
			EditorGUILayout.EndHorizontal();
			{
				if (_target.lightmapped) {
					Vector4 new_bounds = EditorGUILayout.Vector4Field("UV2 bounds rect (X=xMin, Y=xMax, Z=zMin, W=zMax)", _target.UV2bounds);
					if (Vector4.Distance(_target.UV2bounds, new_bounds)>0) {
						_target.UV2bounds=new_bounds;
						_target.UV2dirty=true;
					}
					EditorGUILayout.BeginHorizontal();
						if (GUILayout.Button("Default")) {
							_target.getUV2Bounds();
							_target.SetupUV2( _target.LODs[0] ); _target.SetupUV2( _target.LODs[1] ); _target.SetupUV2( _target.LODs[2] ); _target.SetupUV2( _target.LODs[3] );
						}
						if (GUILayout.Button("Copy from main UV")) {
							_target.UV2bounds=UV1bounds;
							_target.SetupUV2( _target.LODs[0] ); _target.SetupUV2( _target.LODs[1] ); _target.SetupUV2( _target.LODs[2] ); _target.SetupUV2( _target.LODs[3] );
						}
						EditorGUI.BeginDisabledGroup(!_target.UV2dirty);
							c=GUI.color;
							GUI.color=new Color(0.9f,1, 0.9f);
							if (GUILayout.Button("Apply")) {
								_target.SetupUV2( _target.LODs[0] ); _target.SetupUV2( _target.LODs[1] ); _target.SetupUV2( _target.LODs[2] ); _target.SetupUV2( _target.LODs[3] );
							}
							GUI.color=c;
						EditorGUI.EndDisabledGroup();
					EditorGUILayout.EndHorizontal();
					if (GUILayout.Button("Adjust to underlying terrain")) {
						Ray ray = new Ray(_target.transform.position+Vector3.up*10000,-Vector3.up);
						RaycastHit[] hits=Physics.RaycastAll(ray);
						bool found=false;
						for(int i=0; i<hits.Length; i++) {
							Terrain ter=hits[i].transform.gameObject.GetComponent(typeof(Terrain)) as Terrain;
							if (ter && ter.terrainData) {
								_target.UV2bounds=new Vector4(ter.GetPosition().x, ter.GetPosition().x+ter.terrainData.size.x, ter.GetPosition().z, ter.GetPosition().z+ter.terrainData.size.z);
								_target.SetupUV2( _target.LODs[0] ); _target.SetupUV2( _target.LODs[1] ); _target.SetupUV2( _target.LODs[2] ); _target.SetupUV2( _target.LODs[3] );
								#if UNITY_EDITOR
								StaticEditorFlags flags=GameObjectUtility.GetStaticEditorFlags(_target.gameObject);
								if ((flags & StaticEditorFlags.LightmapStatic)==0) {
									if (_target.GetComponent<Renderer>()) {
										_target.GetComponent<Renderer>().lightmapIndex=ter.lightmapIndex;
									}
								}			
								#endif
								
								found=true;
								break;
							}
						}
						if (!found) EditorUtility.DisplayDialog("Notification", "No terrain found under volume grass transform...","OK");
					}				
				}
			}
		}
		GUILayout.EndVertical();
		GUILayout.EndHorizontal();		
		EditorGUILayout.EndVertical();
		
		GUILayout.Space(6);
		
		EditorGUILayout.BeginVertical("box");
		c=GUI.color;
		GUI.color=new Color(1,1, 0.5f);
		GUILayout.BeginHorizontal();
		GUILayout.Space(12);
		_target.show_general_settings=EditorGUILayout.Foldout (_target.show_general_settings, "Settings", boldFoldoutStyle);
		GUILayout.EndHorizontal ();
		GUI.color=c;
		GUILayout.BeginHorizontal();
		GUILayout.Space(10);
		GUILayout.BeginVertical();
		if (_target.show_general_settings) {
			GUILayout.BeginHorizontal();
			GUILayout.Label ("Show node numbers", EditorStyles.label, GUILayout.Width (220) );
			bool tmp_showNodeNumbers = EditorGUILayout.Toggle (_target.showNodeNumbers);
			if (_target.showNodeNumbers!=tmp_showNodeNumbers) {
				_target.showNodeNumbers=tmp_showNodeNumbers;
				EditorUtility.SetDirty(_target);
			}
			GUILayout.EndHorizontal();
			EditorGUI.indentLevel=0;
			GUILayout.BeginHorizontal();
			GUILayout.Label("Show tesselation points in build mode", EditorStyles.label, GUILayout.Width (220) );
			bool new_flag=EditorGUILayout.Toggle(_target.show_tesselation_points);
			if (_target.show_tesselation_points!=new_flag) {
				_target.show_tesselation_points=new_flag;
				if ((!new_flag) && (_target.which_active==3)) {
					_target.active_idx=-1;
				}
				EditorUtility.SetDirty(_target);
			}
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();
			GUILayout.Label ("Default subnodes", EditorStyles.label);
			_target.bezier_subdivisions = EditorGUILayout.IntSlider (_target.bezier_subdivisions, 1, 50);
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();
			GUILayout.Label ("Snap to ground when dragging node", EditorStyles.label, GUILayout.Width (220)  );
			_target.snap_on_move = EditorGUILayout.Toggle (_target.snap_on_move);
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();
			GUILayout.Label ("Snap to ground every node at once", EditorStyles.label, GUILayout.Width (220)  );
			_target.snap_always = EditorGUILayout.Toggle (_target.snap_always);
			GUILayout.EndHorizontal();
		}
		GUILayout.Space(2);
		GUILayout.BeginHorizontal();
		GUILayout.Label ("Ground collision layer mask", EditorStyles.label );
		_target.ground_layerMask.value=EditorGUILayout.LayerField(_target.ground_layerMask.value);
		GUILayout.EndHorizontal();
		GUILayout.Space(4);
		if (_target.GetComponent<Renderer>()) {
			EditorGUI.BeginDisabledGroup( _target.GetComponent<Renderer>().sharedMaterial==null || AssetDatabase.GetAssetPath(_target.GetComponent<Renderer>().sharedMaterial)!="");
			if (GUILayout.Button("Save material to file", GUILayout.Height (25))) {
				SaveMaterial(_target.GetComponent<Renderer>().sharedMaterial, ref _target.save_path_material, "Grass material.mat");
			}
			EditorGUI.EndDisabledGroup();
		}

		GUILayout.EndVertical();
		GUILayout.EndHorizontal();
		EditorGUILayout.EndVertical();	
			
		if (_target.state==1 && _target.LODs[_target.act_lod]) {
			GUILayout.Space(6);
			EditorGUILayout.BeginVertical("box");			
			c=GUI.color;
			GUI.color=new Color(1,1, 0.5f);
			GUILayout.Space (-1);
			GUILayout.Label ("Coverage painting per vertex", EditorStyles.boldLabel);
			GUI.color=c;	
			
			bool prev_paint_flag=_target.paint_height;
			
			if (!_target.paint_height) {
				c=GUI.color;
				GUI.color=new Color(0.9f,1, 0.9f);
				if (GUILayout.Button(new GUIContent("Paint grass coverage per vertex (M)",paintButTexOn, "Click to turn on coverge painting"), GUILayout.Height (30))) {
					_target.paint_height=true;
				}
				GUI.color=c;
			} else if (_target.paint_height) {
				c=GUI.color;
				GUI.color=new Color(1,0.9f,0.9f);
				if (GUILayout.Button(new GUIContent("End coverage painting (M)",paintButTexOff, "Click to turn off painting"))) {
					_target.paint_height=false;
				}
				GUI.color=c;
			}
			if (!prev_paint_flag && _target.paint_height) {
				prev_tool=Tools.current;
				Tools.current=Tool.View;
				VolumeGrass._SceneGUI = new SceneView.OnSceneFunc(CustomOnSceneGUI);
				SceneView.onSceneGUIDelegate += VolumeGrass._SceneGUI;
			} else if (prev_paint_flag && !_target.paint_height) {
				Tools.current=prev_tool;
				SceneView.onSceneGUIDelegate -= VolumeGrass._SceneGUI;
			}
			if (prev_paint_flag!=_target.paint_height) EditorUtility.SetDirty(target);
			if (_target.paint_height) {
				EditorGUILayout.HelpBox("Hold SHIFT while painting to restore coverage.",MessageType.Info, true);
				
				GUILayout.BeginHorizontal();
				GUILayout.Label ("Area size", EditorStyles.label, GUILayout.Width(100) );
				_target.paint_size = EditorGUILayout.Slider(_target.paint_size, 0.5f, 50);
				GUILayout.EndHorizontal();				
				GUILayout.BeginHorizontal();
				GUILayout.Label ("Area smoothness", EditorStyles.label, GUILayout.Width(100) );
				_target.paint_smoothness = EditorGUILayout.Slider (_target.paint_smoothness, 0, 1);
				GUILayout.EndHorizontal();
				GUILayout.BeginHorizontal();
				GUILayout.Label ("Opacity", EditorStyles.label, GUILayout.Width(100) );
				_target.paint_opacity = EditorGUILayout.Slider (_target.paint_opacity, 0, 1);
				GUILayout.EndHorizontal();				
			}
			EditorGUILayout.EndVertical();
		} else {
			if (_target.paint_height) {
				_target.paint_height=false;
				Tools.current=prev_tool;
				SceneView.onSceneGUIDelegate -= VolumeGrass._SceneGUI;
				EditorUtility.SetDirty(target);
			}
		}		

		GUILayout.Space(3);
		
		Event current = Event.current;
		switch(current.type) {
		case EventType.keyDown:
			if (current.keyCode==KeyCode.M) {
				_target.paint_height=!_target.paint_height;
				if (_target.paint_height) {
					prev_tool=Tools.current;
					Tools.current=Tool.View;
					VolumeGrass._SceneGUI = new SceneView.OnSceneFunc(CustomOnSceneGUI);
					SceneView.onSceneGUIDelegate += VolumeGrass._SceneGUI;
				} else {
					Tools.current=prev_tool;
					SceneView.onSceneGUIDelegate -= VolumeGrass._SceneGUI;
				}
				EditorUtility.SetDirty(target);
			}
			break;
		}		
	}
	
	public void OnSceneGUI() {
		VolumeGrass _target=(VolumeGrass)target;
		Event current = Event.current;
		int i;
				
		if (Event.current.type==EventType.keyDown) {
			if (Event.current.keyCode==KeyCode.M) {
				_target.paint_height=!_target.paint_height;
				if (_target.paint_height) {
					prev_tool=Tools.current;
					Tools.current=Tool.View;
					VolumeGrass._SceneGUI = new SceneView.OnSceneFunc(CustomOnSceneGUI);
					SceneView.onSceneGUIDelegate += VolumeGrass._SceneGUI;
				} else {
					Tools.current=prev_tool;
					SceneView.onSceneGUIDelegate -= VolumeGrass._SceneGUI;					
				}
				EditorUtility.SetDirty(target);
				Event.current.Use();
			}
		}
		
		switch(current.type) {
		case EventType.keyDown:
			if (current.keyCode==KeyCode.Delete) {
				if ((_target.state==0) || (_target.which_active==3)) {
					if (_target.active_idx>=0) {
						Undo.RecordObjects(new Object[2]{_target, _target.transform}, "grass edit");
						_target.DeleteActiveControlPoint();
						current.Use();
					}
				} else {
					if (_target.active_idx>=0) {
						current.Use();
						if (_target.which_active==0) {
							EditorUtility.DisplayDialog("Info", "Deleting nodes in Edit mode only   ", "Proceed", "");
						}
					}
				}
			} else if (current.keyCode==KeyCode.Return) {
				if (_target.state==1) {
					// rebuild
					current.Use();
					if (!_target.BuildMesh()) {
						EditorUtility.DisplayDialog("Error...", "Can't build mesh   ", "Proceed", "");
					}
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
			
			_target.active_idx=-1;
			_target.which_active=0;
			// pressing control points
			min_dist=12f; // promień kliku w obrębie którego "łapiemy" pkt kontrolny
			for(i=0; i<_target.control_points.Count; i++) {
				dist=Vector2.Distance(current.mousePosition, HandleUtility.WorldToGUIPoint(T_lw(_target.control_points[i])));
				if (dist<min_dist) {
					min_dist=dist;
					_target.active_idx=i;
				}
			}
			if (_target.state==0) { // zaznaczanie pktów beziera tylko w trybie edycji (w rybie build i tak są niewidoczne)
				// pressing bezierA points
				min_dist=5f; // kółko bezier handla jest mniejsze niż control point
				for(i=0; i<_target.control_points.Count; i++) {
					// pkt beziera traktujemy jako "aktywny" jeśli jest choć trochę oddalony od control_pointa
					if (Vector2.Distance(HandleUtility.WorldToGUIPoint(T_lw(_target.control_points[i])), HandleUtility.WorldToGUIPoint(T_lw(_target.bezier_pointsA[i])))>0.01f) {
						dist=Vector2.Distance(current.mousePosition, HandleUtility.WorldToGUIPoint(T_lw(_target.bezier_pointsA[i])));
						if (dist<min_dist) {
							min_dist=dist;
							_target.which_active=1;
							_target.active_idx=i;
						}
					}
				}
				// pressing bezierB points
				min_dist=5f; // kółko bezier handla jest mniejsze niż control point
				for(i=0; i<_target.control_points.Count; i++) {
					// pkt beziera traktujemy jako "aktywny" jeśli jest choć trochę oddalony od control_pointa
					if (Vector2.Distance(HandleUtility.WorldToGUIPoint(T_lw(_target.control_points[i])), HandleUtility.WorldToGUIPoint(T_lw(_target.bezier_pointsB[i])))>0.01f) {
						dist=Vector2.Distance(current.mousePosition, HandleUtility.WorldToGUIPoint(T_lw(_target.bezier_pointsB[i])));
						if (dist<min_dist) {
							min_dist=dist;
							_target.which_active=2;
							_target.active_idx=i;
						}
					}
				}
			}
			// pressing tesselation points
			if ((_target.state==0) || (_target.show_tesselation_points)) {
				min_dist=8f; // kółko tesselation handla jest mniejsze niż control point
				for(i=0; i<_target.tesselation_points.Count; i++) {
					dist=Vector2.Distance(current.mousePosition, HandleUtility.WorldToGUIPoint(T_lw(_target.tesselation_points[i])));
					if (dist<min_dist) {
						min_dist=dist;
						_target.which_active=3;
						_target.active_idx=i;
					}
				}
			}
			if ((_target.state==0) && current.shift && (!current.alt) && (_target.active_idx==-1) && (_target.which_active==0)) {
				// dodawanie pktów kontrolnych
				Vector3 insert_pos=new Vector3(0,0,0);
				int insert_idx=-1;
				_target.GetInsertPos(current.mousePosition, ref insert_pos, ref insert_idx);
				if (insert_idx<0) {
					// add point
					Undo.RecordObjects(new Object[2]{_target, _target.transform}, "grass edit");
					_target.AddControlPoint(GetWorldPointFromMouse(_target.ground_layerMask), -1);
				} else {
					// insert point
					Undo.RecordObjects(new Object[2]{_target, _target.transform}, "grass edit");
					_target.AddControlPoint(insert_pos, insert_idx);
				}
				current.Use();
			} else if (current.shift && current.alt && (_target.active_idx==-1) && (_target.which_active==0) && ((_target.state==0) || (_target.show_tesselation_points))) {
				// dodawanie pktów tesselacji
				Undo.RecordObjects(new Object[2]{_target, _target.transform}, "grass edit");
				_target.AddTesselationPoint(GetWorldPointFromMouse(_target.ground_layerMask));
				current.Use();
			} else if ((_target.state==0) && (_target.active_idx>=0) && (_target.which_active==0) && (current.alt)) {
				// dodawanie pktów beziera
				if (Vector2.Distance(HandleUtility.WorldToGUIPoint(T_lw(_target.control_points[_target.active_idx])), HandleUtility.WorldToGUIPoint(T_lw(_target.bezier_pointsA[_target.active_idx])))<0.01f) {
					// wstawienie bezier_pointA
					//_target.which_active=1;
					Vector3 dir_vec=_target.control_points[(_target.active_idx+1)%_target.control_points.Count] - _target.control_points[_target.active_idx];
					dir_vec+=1.5f*(_target.control_points[(_target.active_idx+_target.control_points.Count-1)%_target.control_points.Count] - _target.control_points[_target.active_idx]);
					if (dir_vec.magnitude<0.01f) {
						dir_vec=Vector3.right;
					} else if (dir_vec.magnitude>5) {
						dir_vec.Normalize();
						dir_vec*=5;
					}
					_target.bezier_pointsA[_target.active_idx]-=dir_vec;
				} else if (Vector2.Distance(HandleUtility.WorldToGUIPoint(T_lw(_target.control_points[_target.active_idx])), HandleUtility.WorldToGUIPoint(T_lw(_target.bezier_pointsB[_target.active_idx])))<0.01f) {
					// wstawienie bezier_pointB
					//_target.which_active=2;
					Vector3 dir_vec=_target.control_points[(_target.active_idx+_target.control_points.Count-1)%_target.control_points.Count] - _target.control_points[_target.active_idx];
					dir_vec+=1.5f*(_target.control_points[(_target.active_idx+1)%_target.control_points.Count] - _target.control_points[_target.active_idx]);
					if (dir_vec.magnitude<0.01f) {
						dir_vec=Vector3.right;
					} else if (dir_vec.magnitude>5) {
						dir_vec.Normalize();
						dir_vec*=5;
					}
					_target.bezier_pointsB[_target.active_idx]-=dir_vec;
				}
				//current.Use();
			}
			//					if ((prev_target_active_idx!=_target.active_idx) || (prev_target_which_active!=_target.which_active)) {
			//						Undo.RecordObjects(new Object[2]{_target, _target.transform}, "grass edit");
			//					}
			_target.undo_flag=false;
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
		if (_target.showNodeNumbers) {
			for(i=0; i<_target.control_points.Count; i++) {
				Handles.Label(T_lw(_target.control_points[i]), "  "+i);
			}
		}
		
		// tesselation points labels
		if ((_target.state==0) || (_target.show_tesselation_points)) {
			for(i=0; i<_target.tesselation_points.Count; i++) {
				Handles.Label(T_lw(_target.tesselation_points[i]), " tp");
			}
		}
		
		// control_points
		for(i=0; i<_target.control_points.Count; i++) {
			Vector3 vec= 
				Handles.FreeMoveHandle(T_lw(_target.control_points[i]), 
				                       Quaternion.identity, HandleUtility.GetHandleSize(T_lw(_target.control_points[i]))*0.08f, Vector3.one, 
				                       Handles.RectangleCap);
			if ( (_target.state==0) && (Vector3.Distance(vec, T_lw(_target.control_points[i]))>0) ) {
				if (!_target.undo_flag) {
					Undo.RecordObjects(new Object[2]{_target, _target.transform}, "grass edit");
					_target.undo_flag=true;
				}
				Vector3 delta_bezierA=T_lw(_target.bezier_pointsA[i])-T_lw(_target.control_points[i]);
				Vector3 delta_bezierB=T_lw(_target.bezier_pointsB[i])-T_lw(_target.control_points[i]);
				if (_target.snap_on_move)
					_target.control_points[i]=T_wl(GetWorldPointFromMouse(_target.ground_layerMask));
				else
					_target.control_points[i]=T_wl(vec);
				_target.bezier_pointsA[i]=T_wl(T_lw(_target.control_points[i])+delta_bezierA);
				_target.bezier_pointsB[i]=T_wl(T_lw(_target.control_points[i])+delta_bezierB);
			}
		}
		Handles.color=Color.gray;
		// tesselation point handles
		if ((_target.state==0) || (_target.show_tesselation_points) && (!_target.paint_height)) {
			for(i=0; i<_target.tesselation_points.Count; i++) {
				Vector3 vec= 
					Handles.FreeMoveHandle(T_lw(_target.tesselation_points[i]), 
					                       Quaternion.identity, HandleUtility.GetHandleSize(T_lw(_target.tesselation_points[i]))*0.06f, Vector3.one, 
					                       Handles.CircleCap);
				if (Vector3.Distance(vec, T_lw(_target.tesselation_points[i]))>0) {
					if (!_target.undo_flag) {
						Undo.RecordObjects(new Object[2]{_target, _target.transform}, "grass edit");
						_target.undo_flag=true;
					}
					if (_target.snap_on_move)
						_target.tesselation_points[i]=T_wl(GetWorldPointFromMouse(_target.ground_layerMask));
					else
						_target.tesselation_points[i]=T_wl(vec);
				}
			}
		}
		// UV bounds handles
		if (_target.GetComponent<Renderer>() && _target.GetComponent<Renderer>().sharedMaterial && _target.GetComponent<Renderer>().sharedMaterial.HasProperty("UVbounds")) {
			Vector4 UVbounds=_target.GetComponent<Renderer>().sharedMaterial.GetVector("UVbounds");
			Vector3 handle_pos, vec;
			// minx
			handle_pos=new Vector3(UVbounds.x, _target.transform.position.y, 0.5f*(UVbounds.z+UVbounds.w));
			vec=Handles.FreeMoveHandle(handle_pos, Quaternion.identity, HandleUtility.GetHandleSize(handle_pos)*0.04f, Vector3.one, Handles.DotCap);
			if (Mathf.Abs(vec.x-handle_pos.x)>0) {
				if (!_target.undo_flag) {
					Undo.RecordObject(_target, "grass edit");
					_target.undo_flag=true;
				}
				//if (vec.x>_target.UV2bounds.x) vec.x=_target.UV2bounds.x;
				UVbounds.x=vec.x;
				_target.GetComponent<Renderer>().sharedMaterial.SetVector("UVbounds", UVbounds);
			}
			// maxx
			handle_pos=new Vector3(UVbounds.y, _target.transform.position.y, 0.5f*(UVbounds.z+UVbounds.w));
			vec=Handles.FreeMoveHandle(handle_pos, Quaternion.identity, HandleUtility.GetHandleSize(handle_pos)*0.04f, Vector3.one, Handles.DotCap);
			if (Mathf.Abs(vec.x-handle_pos.x)>0) {
				if (!_target.undo_flag) {
					Undo.RecordObject(_target, "grass edit");
					_target.undo_flag=true;
				}
				//if (vec.x<_target.UV2bounds.y+0.0f) vec.x=_target.UV2bounds.y+0.0f;
				UVbounds.y=vec.x;
				_target.GetComponent<Renderer>().sharedMaterial.SetVector("UVbounds", UVbounds);
			}
			// minz
			handle_pos=new Vector3(0.5f*(UVbounds.x+UVbounds.y), _target.transform.position.y, UVbounds.z);
			vec=Handles.FreeMoveHandle(handle_pos, Quaternion.identity, HandleUtility.GetHandleSize(handle_pos)*0.04f, Vector3.one, Handles.DotCap);
			if (Mathf.Abs(vec.z-handle_pos.z)>0) {
				if (!_target.undo_flag) {
					Undo.RecordObject(_target, "grass edit");
					_target.undo_flag=true;
				}
				//if (vec.z>_target.UV2bounds.z) vec.z=_target.UV2bounds.z;
				UVbounds.z=vec.z;
				_target.GetComponent<Renderer>().sharedMaterial.SetVector("UVbounds", UVbounds);
			}
			// maxz
			handle_pos=new Vector3(0.5f*(UVbounds.x+UVbounds.y), _target.transform.position.y, UVbounds.w);
			vec=Handles.FreeMoveHandle(handle_pos, Quaternion.identity, HandleUtility.GetHandleSize(handle_pos)*0.04f, Vector3.one, Handles.DotCap);
			if (Mathf.Abs(vec.z-handle_pos.z)>0) {
				if (!_target.undo_flag) {
					Undo.RecordObject(_target, "grass edit");
					_target.undo_flag=true;
				}
				//if (vec.z<_target.UV2bounds.w+0.0f) vec.z=_target.UV2bounds.w+0.0f;
				UVbounds.w=vec.z;
				_target.GetComponent<Renderer>().sharedMaterial.SetVector("UVbounds", UVbounds);
			}
		}
		
		if (_target.state==0) {
			if (_target.which_active!=3) {
				// bezier handle A
				for(i=0; i<_target.control_points.Count; i++) {
					if ( (_target.active_idx==i) && ((_target.which_active==1) || (Vector3.Distance(T_lw(_target.control_points[i]), T_lw(_target.bezier_pointsA[i]))>0.01f)) ) {
						Vector3 vec= 
							Handles.FreeMoveHandle(T_lw(_target.bezier_pointsA[i]), 
							                       Quaternion.identity, HandleUtility.GetHandleSize(T_lw(_target.bezier_pointsA[i]))*0.05f, Vector3.one, 
							                       Handles.CircleCap);
						if (Vector3.Distance(vec, T_lw(_target.bezier_pointsA[i]))>0) {
							if (!_target.undo_flag) {
								Undo.RecordObjects(new Object[2]{_target, _target.transform}, "grass edit");
								_target.undo_flag=true;
							}
							if (_target.snap_on_move)
								_target.bezier_pointsA[i]=T_wl(GetWorldPointFromMouse(_target.ground_layerMask));
							else
								_target.bezier_pointsA[i]=T_wl(vec);
						}
						Handles.DrawLine(T_lw(_target.control_points[i]), T_lw(_target.bezier_pointsA[i]));
					}
				}
				// bezier handle B
				for(i=0; i<_target.control_points.Count; i++) {
					if ( (_target.active_idx==i) && ((_target.which_active==2) || (Vector3.Distance(T_lw(_target.control_points[i]), T_lw(_target.bezier_pointsB[i]))>0.01f)) ) {
						Vector3 vec= 
							Handles.FreeMoveHandle(T_lw(_target.bezier_pointsB[i]), 
							                       Quaternion.identity, HandleUtility.GetHandleSize(T_lw(_target.bezier_pointsB[i]))*0.05f, Vector3.one, 
							                       Handles.CircleCap);
						if (Vector3.Distance(vec, T_lw(_target.bezier_pointsB[i]))>0) {
							if (!_target.undo_flag) {
								Undo.RecordObjects(new Object[2]{_target, _target.transform}, "grass edit");
								_target.undo_flag=true;
							}
							if (_target.snap_on_move)
								_target.bezier_pointsB[i]=T_wl(GetWorldPointFromMouse(_target.ground_layerMask));
							else
								_target.bezier_pointsB[i]=T_wl(vec);
						}
						Handles.DrawLine(T_lw(_target.control_points[i]), T_lw(_target.bezier_pointsB[i]));
					}
				}
			}
		}		
		
		
		
		
	}	
	
	public void CustomOnSceneGUI(SceneView sceneview) {
		VolumeGrass _target=(VolumeGrass)target;
		
		EditorWindow currentWindow = EditorWindow.mouseOverWindow;
		if(!currentWindow) return;
		
		//Rect winRect = currentWindow.position;
		Event current = Event.current;
		
		if (current.alt) {
			return;
		}		
		if (Event.current.button == 1) {
			HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
			return;
		}
		
		if (Tools.current!=Tool.View && _target.paint_height) {
			_target.paint_height=false;
			SceneView.onSceneGUIDelegate -= VolumeGrass._SceneGUI;
			Tools.current=prev_tool;
			EditorUtility.SetDirty(target);
			return;
		}		
		
		if (current.type==EventType.layout) {
			HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
			return;
		}
				
		int i;
		
		if (_target.state==1) {
		
			switch(current.type) {
			case EventType.keyDown:
				if (current.keyCode==KeyCode.M || ((current.keyCode==KeyCode.Escape) && _target.paint_height)) {
					SceneView.onSceneGUIDelegate -= VolumeGrass._SceneGUI;
					_target.paint_height=!_target.paint_height;
					if (_target.paint_height) {
						prev_tool=Tools.current;
						Tools.current=Tool.View;
						VolumeGrass._SceneGUI = new SceneView.OnSceneFunc(CustomOnSceneGUI);
						SceneView.onSceneGUIDelegate += VolumeGrass._SceneGUI;
					} else {
						Tools.current=prev_tool;
						SceneView.onSceneGUIDelegate -= VolumeGrass._SceneGUI;					
					}					
					EditorUtility.SetDirty(_target);
				}
				break;
			}		
			
			if (_target.paint_height) {
				
				if (current.control) {
						if (current.type==EventType.mouseMove) {
							if (control_down_flag) {
								control_down_flag=false;
								EditorUtility.SetDirty(_target);
							}
						}
						return;
				}
				control_down_flag=true;
				switch(current.type) {
					case EventType.mouseDown:
						get_paint_coverage();
						cover_verts_num_start_drag=cover_verts_num;
						if (cover_verts_num>0) {
							RegisterUndoForMeshes("grass edit (height)");
							_target.modify_height(cover_verts_num, cover_indices, cover_strength, current.shift);
							current.Use();
						} else {
							_target.undo_flag=true;
						}
					break;
					case EventType.mouseDrag:
						get_paint_coverage();
						if (cover_verts_num>0) {
							if (_target.undo_flag) {
								RegisterUndoForMeshes("grass edit (height)");
								_target.undo_flag=false;
							}
						}
						if (cover_verts_num_start_drag>0) {
							_target.modify_height(cover_verts_num, cover_indices, cover_strength, current.shift);
							current.Use();
						}
					break;
					case EventType.mouseMove:
						get_paint_coverage();
					break;
				}
		
				if (current.shift) {
					for(i=0; i<cover_verts_num; i++) {
						Handles.color=new Color(0,1,0,cover_strength[i]);
						//Handles.ArrowCap(0, cover_verts[i], cover_norms[i], 5*cover_strength[i]);
						Handles.DrawSolidDisc(cover_verts[i], Camera.current.transform.position-cover_verts[i], HandleUtility.GetHandleSize(cover_verts[i])*0.03f);
					}
				} else {
					Handles.color=Color.red;
					for(i=0; i<cover_verts_num; i++) {
						Handles.color=new Color(1,0,0,cover_strength[i]);
						//Handles.ArrowCap(0, cover_verts[i], cover_norms_flip[i], 5*cover_strength[i]);
						Handles.DrawSolidDisc(cover_verts[i], Camera.current.transform.position-cover_verts[i], HandleUtility.GetHandleSize(cover_verts[i])*0.03f);
					}
				}
					
				return;
			}
		}
		
		
		
	}
	
	private void get_paint_coverage() {
		if (Time.realtimeSinceStartup<lCovTim) return;
		lCovTim=Time.realtimeSinceStartup+0.04f;
		VolumeGrass _target=(VolumeGrass)target;		
		Vector3[] vertices=_target.get_volume_vertices();
		Vector3[] normals=_target.get_volume_normals();
		Color[] colors=_target.get_volume_colors();
		if ((vertices!=null) && (normals!=null)) {
			Vector3 pnt=T_wl(GetWorldPointFromMouse(_target.ground_layerMask));
			cover_verts_num=0;
			for(int i=0; i<vertices.Length; i++) {
				float dist=Vector3.Distance(pnt, vertices[i]);
				if ((cover_verts_num<cover_verts.Length) && (colors[i].r==0) && (dist<_target.paint_size)) {
					cover_verts[cover_verts_num]=T_lw(vertices[i]-normals[i]*colors[i].g*_target.mesh_height);
					//cover_norms[cover_verts_num]=Quaternion.LookRotation(normals[i]);
					//cover_norms_flip[cover_verts_num]=Quaternion.LookRotation(-normals[i]);
					cover_strength[cover_verts_num]=(_target.paint_size-dist*_target.paint_smoothness)/_target.paint_size;
					cover_indices[cover_verts_num]=i;
					cover_verts_num++;
				}
			}
		}
		EditorUtility.SetDirty(_target);
	}		
    private Vector3 GetWorldPointFromMouse(LayerMask layerMask)
    {
		float planeLevel = 0;
        var groundPlane = new Plane(Vector3.up, new Vector3(0, planeLevel, 0));

        var ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        RaycastHit rayHit;
        Vector3 hit = new Vector3(0,0,0);
        float dist;
		
        if (Physics.Raycast(ray, out rayHit, Mathf.Infinity, 1<<layerMask.value))
            hit = rayHit.point;
        else if (groundPlane.Raycast(ray, out dist))
            hit = ray.origin + ray.direction.normalized * dist;

        return hit;
    }
	
	Vector3 T_lw(Vector3 input) {
		VolumeGrass _target=(VolumeGrass)target;
		return _target.transform.TransformPoint(input);
	}
	Vector3 T_wl(Vector3 input) {
		VolumeGrass _target=(VolumeGrass)target;
		return _target.transform.InverseTransformPoint(input);
	}
	
	void RegisterUndoForMeshes(string undo_description) {
		VolumeGrass _target=(VolumeGrass)target; 
		UnityEngine.Object[] objs=new UnityEngine.Object[3];
		objs[0] = _target.gameObject.GetComponent (typeof(MeshFilter)) as MeshFilter;
		if (_target.transform.childCount>0 && _target.transform.GetChild(0)!=null) objs[1]=_target.transform.GetChild(0).gameObject.GetComponent (typeof(MeshFilter)) as MeshFilter;
		bool meshAvailable=false;
		bool meshChildAvailable=false;
		if (objs[0]==null) objs[0]=_target; else meshAvailable=true;
		if (objs[1]==null) objs[1]=_target; else meshChildAvailable=true;
		objs[2]=_target;
		Undo.RecordObjects( objs, undo_description);
		if (meshAvailable) {
			string nam=(objs[0] as MeshFilter).sharedMesh.name;
			(objs[0]as MeshFilter).sharedMesh=Instantiate((objs[0] as MeshFilter).sharedMesh) as Mesh;
			(objs[0]as MeshFilter).sharedMesh.name=nam;
		}
		if (meshChildAvailable) {
			string nam=(objs[1] as MeshFilter).sharedMesh.name;
			(objs[1]as MeshFilter).sharedMesh=Instantiate((objs[1] as MeshFilter).sharedMesh) as Mesh;
			(objs[1]as MeshFilter).sharedMesh.name=nam;
		}
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
