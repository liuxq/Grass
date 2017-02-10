using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.IO;
using System.Text;
 
[System.Serializable]
public class GrassTextureData : ScriptableObject {
	[SerializeField] public static int numSlices=2; // 2 ^ numSlices
	[SerializeField] public static int numSourceBladesPerSlice=400;
	[SerializeField] public static int numSourceBladesOnBackground=1000;
	[SerializeField] public static int sliceHeight=7; // 2 ^ sliceHeight
	[SerializeField] public static int textureWidth=9; // 2 ^ textureWidth
	[SerializeField] public static float backAODarkening=0.4f;
	[SerializeField] public static Texture2D targetTexture; 
	[SerializeField] public static Texture2D targetBackTexture;
	[SerializeField] public static Color BackColor=new Color(95.0f/255, 115.0f/255, 72.0f/255, 1);
	[SerializeField] public static int randomSeed=1234;
	[SerializeField] public static float fakeSelfShadowStrength = 0.2f;
	[SerializeField] public static float randomYOffset = 0.2f;
	
	[SerializeField] public static string target_filename="";
	[SerializeField] public static string target_back_filename="";
	
	// source textures and their properties
	[SerializeField] public static Texture2D[] grassBlades;
	[SerializeField] public static Texture2D[] grassBladesMod;
	[SerializeField] public static Color[] grassBladesTints;
	[SerializeField] public static float[] grassBladesSaturations;
	[SerializeField] public static int[] grassBladesWeights;
}

class VolumeGrass_PrepareGrassTexture : EditorWindow {
	int progress_count_max;
	int progress_count_current;
	const int progress_granulation=10;
	string progress_description="";
	
	bool acceptDrag=false;
	Vector2 scrollPosition;
	float screenBottom=0;
	
	string restored_preset_filename="";
	
	bool dirtyFlag;
	double changedTime=0;
	
	[MenuItem("Window/VolumeGrass Tools/Prepare grass texture")]
	static void Init() {
		VolumeGrass_PrepareGrassTexture wind=EditorWindow.GetWindow<VolumeGrass_PrepareGrassTexture>();
		wind.Show();
		wind.title="Prepare Grass Texture";
		//wind.maxSize=new Vector2(400;
	}
 
	void OnGUI() {
		if (GrassTextureData.grassBlades==null) {
			NewSourceTextures();
		}
		GrassTextureDataPreset presetToRestore=null;
		Rect rect=this.position;
		scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, false,  GUILayout.Width(rect.width),  GUILayout.Height(rect.height));
		
		Color skin_color=GUI.color;
		if (acceptDrag) {
			GUI.color=new Color(0.8f, 1f, 0.8f, 1);
		}
		EditorGUILayout.HelpBox("\n   Drag & drop textures here to add source textures.\n   Drag & drop saved preset to load it.\n", MessageType.None);
		GUI.color=skin_color;
		
		dirtyFlag=false;
		
		DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
		if ( Event.current.type == EventType.DragUpdated ) {
			if ( GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition) || Event.current.mousePosition.y>screenBottom || GrassTextureData.grassBlades.Length==0) {
				bool ok=false;
				for (int i = 0; i < DragAndDrop.objectReferences.Length; i++) {
					if (DragAndDrop.objectReferences[i] is Texture2D) {
						ok=true;
						break;
					}
				}
				if (acceptDrag!=ok) {
					acceptDrag=ok;
					Repaint();
				}
			} else {
				if (acceptDrag!=false) {
					acceptDrag=false;
					Repaint();
				}
			}
		}
		
		if (!acceptDrag && Event.current.type == EventType.DragPerform) {
			if (DragAndDrop.objectReferences.Length==1 && DragAndDrop.objectReferences[0] is GrassTextureDataPreset) {
				presetToRestore=DragAndDrop.objectReferences[0] as GrassTextureDataPreset;
			}
		}
		
		if (Event.current.type == EventType.DragPerform && acceptDrag) {
			//Debug.Log (" "+Event.current.mousePosition);
			acceptDrag=false;
			int num=0;
			for (int i = 0; i < DragAndDrop.objectReferences.Length; i++) {
				if (DragAndDrop.objectReferences[i] is Texture2D) num++;
			}
			Texture2D[] newTextures = new Texture2D[num];
			num=0;
			for (int i = 0; i < DragAndDrop.objectReferences.Length; i++) {
				if (DragAndDrop.objectReferences[i] is Texture2D) {
					newTextures[num++] = DragAndDrop.objectReferences[i] as Texture2D;
				}
			}
			AddSourceTextures(newTextures);
			Repaint();
			GUILayout.EndScrollView();
			return;
		}
		
		if (GrassTextureData.grassBlades.Length>0) {
			if (GUILayout.Button("Save grass preset")) {
				Debug.Log (restored_preset_filename);
				string path = "";
				if (restored_preset_filename!="") {
					path = EditorUtility.SaveFilePanel("Save preset", Path.GetDirectoryName(restored_preset_filename), Path.GetFileNameWithoutExtension(restored_preset_filename), "asset");
				} else {
					path = EditorUtility.SaveFilePanel("Save preset", "Assets", "GrassTexturePreset", "asset");
				}
				if (path!="") {	
					int idx=path.IndexOf("/Assets/")+1;
					if (idx>0) {
						restored_preset_filename=path.Substring(path.IndexOf("Assets/"));
						path=path.Substring(idx);
						GrassTextureDataPreset savedPreset=GetPreset();
						Debug.Log("Grass texture preset saved at "+path);
						if (AssetDatabase.LoadAssetAtPath(path, typeof(GrassTextureDataPreset))!=null) AssetDatabase.DeleteAsset(path);
						AssetDatabase.CreateAsset(savedPreset, path);
					} else {
						Debug.Log ("Nothing saved...");
					}
				}
			}
			GUILayout.Space (20);
					
			int thumbSize=76;
			EditorGUILayout.LabelField("Target texture properties", EditorStyles.boldLabel);
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Slices count    = "+(1<<GrassTextureData.numSlices), GUILayout.MinWidth (140), GUILayout.MaxWidth (140));
			if ( checkChange(ref GrassTextureData.numSlices, Mathf.RoundToInt( GUILayout.HorizontalSlider(GrassTextureData.numSlices ,0 ,3) ) )) {
			}
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Slice height     = "+(1<<GrassTextureData.sliceHeight), GUILayout.MinWidth (140), GUILayout.MaxWidth (140));
			if ( checkChange(ref GrassTextureData.sliceHeight, Mathf.RoundToInt( GUILayout.HorizontalSlider(GrassTextureData.sliceHeight ,5 ,9) ) )) {
			}
			if (GrassTextureData.numSlices==3 && GrassTextureData.sliceHeight>8) GrassTextureData.sliceHeight=8;
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Texture width  = "+(1<<GrassTextureData.textureWidth), GUILayout.MinWidth (140), GUILayout.MaxWidth (140));
			if ( checkChange(ref GrassTextureData.textureWidth, Mathf.RoundToInt( GUILayout.HorizontalSlider(GrassTextureData.textureWidth ,7 ,11) ) )) {
			}
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Blades count per slice", GUILayout.MinWidth (140), GUILayout.MaxWidth (140));
			if ( checkChange(ref GrassTextureData.numSourceBladesPerSlice, EditorGUILayout.IntField(GrassTextureData.numSourceBladesPerSlice) ) ) {
			}
			EditorGUILayout.EndHorizontal();	
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Blades on background", GUILayout.MinWidth (140), GUILayout.MaxWidth (140));
			if ( checkChange(ref GrassTextureData.numSourceBladesOnBackground, EditorGUILayout.IntField(GrassTextureData.numSourceBladesOnBackground) ) ) {
			}
			EditorGUILayout.EndHorizontal();	
			
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Random seed", GUILayout.MinWidth (140), GUILayout.MaxWidth (140));
			if ( checkChange(ref GrassTextureData.randomSeed, EditorGUILayout.IntField(GrassTextureData.randomSeed) ) ) {
			}
			EditorGUILayout.EndHorizontal();	
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Average grass color", GUILayout.MinWidth (140), GUILayout.MaxWidth (140));
			if ( checkChange(ref GrassTextureData.BackColor, EditorGUILayout.ColorField(GrassTextureData.BackColor) ) ) {
			}
			EditorGUILayout.EndHorizontal();	
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Fake self-shadow", GUILayout.MinWidth (140), GUILayout.MaxWidth (140));
			if ( checkChange(ref GrassTextureData.fakeSelfShadowStrength, EditorGUILayout.Slider(GrassTextureData.fakeSelfShadowStrength, 0, 0.9f) ) ) {
			}
			EditorGUILayout.EndHorizontal();	
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Fake AO", GUILayout.MinWidth (140), GUILayout.MaxWidth (140));
			if ( checkChange(ref GrassTextureData.backAODarkening, EditorGUILayout.Slider(GrassTextureData.backAODarkening, 0, 0.9f) ) ) {
			}
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Random y offset", GUILayout.MinWidth (140), GUILayout.MaxWidth (140));
			if ( checkChange(ref GrassTextureData.randomYOffset, EditorGUILayout.Slider(GrassTextureData.randomYOffset, 0, 0.5f) ) ) {
			}
			EditorGUILayout.EndHorizontal();	
			
					
			GUILayout.Space (15);
			EditorGUILayout.LabelField("Source grass textures", EditorStyles.boldLabel);
			int n;
			for(n=0; n<GrassTextureData.grassBlades.Length; n++) {
			    EditorGUILayout.BeginHorizontal();
					GUI.SetNextControlName("tex"+n);
					Texture2D ntex=(Texture2D)EditorGUILayout.ObjectField (GrassTextureData.grassBladesMod[n], typeof(Texture2D), false, GUILayout.MinHeight(thumbSize), GUILayout.MinWidth(thumbSize), GUILayout.MaxWidth(thumbSize));
					if (ntex!=GrassTextureData.grassBladesMod[n]) {
						if (ntex==null) {
							GrassTextureData.grassBlades[n]=GrassTextureData.grassBladesMod[n]=null;
						} else {
							GrassTextureData.grassBladesSaturations[n]=1;
							GrassTextureData.grassBladesTints[n]=new Color(0.5f, 0.5f, 0.5f, 1);
							GrassTextureData.grassBlades[n]=GrassTextureData.grassBladesMod[n]=ntex;
						}
					}
			    	EditorGUILayout.BeginVertical();
						//GUI.color = new Color(1, 0.5f, 0.5f, 1);
						if (GUILayout.Button("Remove")) {
							GUI.FocusControl("");
							RemoveSourceTextureAt(n);
							if (n>=GrassTextureData.grassBlades.Length) {
								EditorGUILayout.EndVertical();
								EditorGUILayout.EndHorizontal();
								if (GrassTextureData.grassBlades.Length==0) NewSourceTextures();
								Repaint();
								break;
							}
							Repaint();
						}
						//GUI.color = skin_color;				
						if ( checkChange(ref GrassTextureData.grassBladesTints[n], EditorGUILayout.ColorField(GrassTextureData.grassBladesTints[n])) ) {
							//if ((EditorApplication.timeSinceStartup-changedTime)>0.25) {
								MakeTextureMod(n);
							//}
						}
						EditorGUILayout.BeginHorizontal();
						EditorGUILayout.LabelField("Saturation", GUILayout.MinWidth (70), GUILayout.MaxWidth (70));
						if ( checkChange(ref GrassTextureData.grassBladesSaturations[n], EditorGUILayout.Slider(GrassTextureData.grassBladesSaturations[n], 0f ,2f)) ) {
							//if ((EditorApplication.timeSinceStartup-changedTime)>0.25) {
								MakeTextureMod(n);
							//}						
						}
						EditorGUILayout.EndHorizontal();
						EditorGUILayout.BeginHorizontal();
						EditorGUILayout.LabelField("Occurence", GUILayout.MinWidth (70), GUILayout.MaxWidth (70));
						if ( checkChange(ref GrassTextureData.grassBladesWeights[n], EditorGUILayout.IntSlider(GrassTextureData.grassBladesWeights[n], 0 , 200)) ) {
						}
						EditorGUILayout.EndHorizontal();
					EditorGUILayout.EndVertical();
				EditorGUILayout.EndHorizontal();
			}
			
			GUILayout.Space(10);
			GUI.color=new Color(1, 0.5f, 0.5f, 1);
			if (GUILayout.Button("Clear All Source Textures")) {
				GUI.FocusControl("");
				if (EditorUtility.DisplayDialog("Warning", "Are you sure ?", "Yes", "Cancel")) {
					NewSourceTextures();
				}
			}
			GUI.color=skin_color;
			GUI.color=new Color(0.5f,1,0.5f,1);
			if (GUILayout.Button("Prepare")) {
				GUI.FocusControl("");
				Prepare(false); // blades
				Prepare(true); // blades back texture
			}
			GUI.color=skin_color;
			if (GrassTextureData.targetTexture!=null) {
				int preview_width=Screen.width-25;
				if (preview_width>GrassTextureData.targetTexture.width) preview_width=GrassTextureData.targetTexture.width;
				int preview_height=preview_width*GrassTextureData.targetTexture.height/GrassTextureData.targetTexture.width;
				EditorGUILayout.BeginHorizontal();
				int left_pad=((Screen.width-25)-preview_width)/2;
				if (left_pad>0) EditorGUILayout.LabelField("", GUILayout.Width(left_pad));
				EditorGUILayout.ObjectField( GrassTextureData.targetTexture, typeof(Texture2D), false, GUILayout.Height(preview_height), GUILayout.Width(preview_width) );
				EditorGUILayout.EndHorizontal();
				//GUILayout.Label("", GUILayout.Width(256), GUILayout.Height(256));
				//EditorGUI.DrawPreviewTexture(GUILayoutUtility.GetLastRect(),GrassTextureData.targetTexture);
				if (GUILayout.Button("Save grass texture")) {
					GUI.FocusControl("");
					SaveTexture(ref GrassTextureData.target_filename, "GrassBlades", ref GrassTextureData.targetTexture); // blades
				}
			}
			if (GrassTextureData.targetBackTexture!=null) {
				int preview_width=Screen.width-25;
				if (preview_width>GrassTextureData.targetBackTexture.width) preview_width=GrassTextureData.targetBackTexture.width;
				int preview_height=preview_width*GrassTextureData.targetBackTexture.height/GrassTextureData.targetBackTexture.width;
				EditorGUILayout.BeginHorizontal();
				int left_pad=((Screen.width-25)-preview_width)/2; 
				if (left_pad>0) EditorGUILayout.LabelField("", GUILayout.Width(left_pad));
				EditorGUILayout.ObjectField( GrassTextureData.targetBackTexture, typeof(Texture2D), false, GUILayout.Height(preview_height), GUILayout.Width(preview_width) );
				EditorGUILayout.EndHorizontal();
				//GUILayout.Label("", GUILayout.Width(256), GUILayout.Height(256));
				//EditorGUI.DrawPreviewTexture(GUILayoutUtility.GetLastRect(),GrassTextureData.targetBackTexture);
				if (GUILayout.Button("Save grass back texture")) {
					GUI.FocusControl("");
					SaveTexture(ref GrassTextureData.target_back_filename, "GrassBladesBack", ref GrassTextureData.targetBackTexture); // back
				}
			}
			
			if (Event.current.type == EventType.Repaint) screenBottom=GUILayoutUtility.GetLastRect().yMax;
		}
		GUILayout.EndScrollView();
		
		if (	presetToRestore) {
			restored_preset_filename=AssetDatabase.GetAssetPath(presetToRestore);
			RestorePreset(presetToRestore);
		}		
	}
 
	void Prepare(bool back) {
		int numSlices=back ? 1 : (1<<GrassTextureData.numSlices);
		int sliceHeight=1<<GrassTextureData.sliceHeight;
		int textureWidth=1<<GrassTextureData.textureWidth;
		int textureHeight=sliceHeight*numSlices;
		Texture2D targetTexture;
		if (back) {
			targetTexture=new Texture2D(textureWidth, textureHeight, TextureFormat.RGB24, true);
		} else {
			targetTexture=new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, true);
		}
		int numSourceBlades=back ? GrassTextureData.numSourceBladesOnBackground : GrassTextureData.numSourceBladesPerSlice;
		int sumWeights=0;
		int[] grassBladesWeights=new int[GrassTextureData.grassBladesWeights.Length];
		for(int i=0; i<GrassTextureData.grassBladesWeights.Length; i++) {
			sumWeights+=GrassTextureData.grassBladesWeights[i];
		}
		for(int i=0; i<GrassTextureData.grassBladesWeights.Length; i++) {
			grassBladesWeights[i]=Mathf.RoundToInt(1.0f*GrassTextureData.grassBladesWeights[i]*numSourceBlades/sumWeights);
		}
		sumWeights=0;
		for(int i=0; i<grassBladesWeights.Length; i++) {
			sumWeights+=grassBladesWeights[i];
		}
		int[] idxArray=new int[sumWeights];
		int n=0;
		for(int i=0; i<GrassTextureData.grassBladesWeights.Length; i++) {
			for(int j=0; j<grassBladesWeights[i]; j++) {
				idxArray[n]=i;
				n++;
			}
		}
		for(int i=0; i<20; i++) {
			for(int j=0; j<idxArray.Length; j++) {
				int idxA=Mathf.RoundToInt(UnityEngine.Random.value*(idxArray.Length-1));
				int idxB=Mathf.RoundToInt(UnityEngine.Random.value*(idxArray.Length-1));
				int tmpIdx=idxArray[idxA];
				idxArray[idxA]=idxArray[idxB];
				idxArray[idxB]=tmpIdx;
			}
		}
		for(int i=0; i<GrassTextureData.grassBladesMod.Length; i++) {
			if (GrassTextureData.grassBladesMod[i]==GrassTextureData.grassBlades[i]) {
				MakeReadable(GrassTextureData.grassBladesMod[i]);
			}
		}
		UnityEngine.Random.seed=GrassTextureData.randomSeed;
		Color[] cols=new Color[textureWidth*textureHeight];
		if (back) {
			for(int j=0; j<textureHeight; j++) {
				float hgt_val=1.0f*j/textureHeight;
				float _AO=Mathf.Lerp(1-GrassTextureData.backAODarkening, 1, hgt_val); 
				Color backColor=GrassTextureData.BackColor*_AO;
				for(int i=0; i<textureWidth; i++) {
					cols[j*textureWidth+i]=backColor;
				}
			}
		}
		
		System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");		
		ResetProgress(numSlices*idxArray.Length,"Preparing texture");		
		
//		float[] randBottoms=new float[idxArray.Length];
//		for(int j=0; j<idxArray.Length; j++) {
//			randBottoms[j]=UnityEngine.Random.value*0.5f+0.3f;
//		}
		for(int i=0; i<numSlices; i++) {
			int sliceY=(i*sliceHeight);
			//float spacingX=1.0f*textureWidth/idxArray.Length;
			for(int j=0; j<idxArray.Length; j++) {
				// wrysuj source tex
				CheckProgress();

				int idx=idxArray[j];
				if (GrassTextureData.grassBladesMod[idx]==null) break;
				//int sliceX=Mathf.RoundToInt(j*spacingX+UnityEngine.Random.value*spacingX*0.25f);
				int sliceX=Mathf.RoundToInt(UnityEngine.Random.value*(textureWidth-1));
				int halfSize=(UnityEngine.Random.value<0.3f) ? 1:0;
				while ((GrassTextureData.grassBladesMod[idx].height>>halfSize) > sliceHeight) halfSize++;
				Color[] srcCols=GrassTextureData.grassBladesMod[idx].GetPixels(halfSize);
				int srcWidth=GrassTextureData.grassBladesMod[idx].width>>(halfSize);
				int srcHeight=GrassTextureData.grassBladesMod[idx].height>>(halfSize);
				int hgt=srcHeight<sliceHeight ? srcHeight : sliceHeight;
				int yoffset=0;
				if (back) {
					yoffset=Mathf.RoundToInt(UnityEngine.Random.value*hgt*0.5f);
					hgt-=yoffset;
				}
				bool flipX=(UnityEngine.Random.value<0.5f);
				int shadowXOffset = Mathf.RoundToInt( UnityEngine.Random.value*20 - 10 );
				int shadowYOffset = (Mathf.RoundToInt( UnityEngine.Random.value*30 ) + 20)*2*128/sliceHeight;
				float shadowVal = UnityEngine.Random.value*GrassTextureData.fakeSelfShadowStrength;
				int randomYOffset = Mathf.RoundToInt( UnityEngine.Random.value*hgt*GrassTextureData.randomYOffset );
				
				float _val = (j+1.0f)/idxArray.Length;
				_val *= _val;
				_val *= _val;
				_val *= _val;
				float backAODarkeningValue = Mathf.Lerp(1-GrassTextureData.backAODarkening, 1, _val);
				
				for(int iy=0; iy<hgt; iy++) {
//					float edgeFade=1;
//					if (!back) {
//						if (iy<32) {
//							edgeFade=iy/32.0f;
//						} else if (iy>hgt-32)  {
//							edgeFade=(hgt-iy)/32.0f;
//						}
//					}
					float hgt_val=1.0f*iy/hgt;
					//float _AO=Mathf.Clamp(1.0f*iy/hgt, randBottoms[j],1);
					float _AO=Mathf.Lerp(backAODarkeningValue, 1, hgt_val); //(1-hgt_val)*(1-hgt_val));
					for(int ix=0; ix<srcWidth; ix++) {
						Color source_col=srcCols[iy*srcWidth+ix];
						// cien
						int target_idx;
						if (iy>randomYOffset+shadowYOffset) {
							target_idx=(sliceY+yoffset-randomYOffset-shadowYOffset+iy)*textureWidth; 
							if (flipX) target_idx+=((sliceX+ix+shadowXOffset)%textureWidth); else target_idx+=((sliceX+srcWidth-1-ix+shadowXOffset)%textureWidth);
							cols[target_idx].r*=(1-source_col.a*shadowVal);
							cols[target_idx].g*=(1-source_col.a*shadowVal);
							cols[target_idx].b*=(1-source_col.a*shadowVal);
						}
						// kolor
						if (iy>randomYOffset) {
							target_idx=(sliceY+yoffset-randomYOffset+iy)*textureWidth; 
							if (flipX) target_idx+=((sliceX+ix)%textureWidth); else target_idx+=((sliceX+srcWidth-1-ix)%textureWidth);
							Color target_col=cols[target_idx];
							if (back) {
								target_col.r=source_col.r*source_col.a*_AO + target_col.r*(1-source_col.a);
								target_col.g=source_col.g*source_col.a*_AO + target_col.g*(1-source_col.a);
								target_col.b=source_col.b*source_col.a*_AO + target_col.b*(1-source_col.a);
							} else {
								float alpha=source_col.a;// *edgeFade;
								if (alpha>0f) {
									float blend=target_col.a*alpha + (1-target_col.a);
									target_col.r=source_col.r*blend*_AO + target_col.r*(1-blend);
									target_col.g=source_col.g*blend*_AO + target_col.g*(1-blend);
									target_col.b=source_col.b*blend*_AO + target_col.b*(1-blend);
								}
								target_col.a=target_col.a>alpha ? target_col.a : alpha;
							}
							cols[ target_idx ] = target_col;
						}
					}
				}
			}
		}
		targetTexture.SetPixels(cols);
		targetTexture.Apply(true,false);
		if (back) {
			GrassTextureData.targetBackTexture=targetTexture;
		} else {
			GrassTextureData.targetTexture=targetTexture;
		}
	    
		EditorUtility.ClearProgressBar();
		
	}
 
	
	void NewSourceTextures() {
		//Debug.Log ("NEW");
		GrassTextureData.grassBlades=new Texture2D[0];	
		GrassTextureData.grassBladesMod=new Texture2D[0];	
		GrassTextureData.grassBladesTints=new Color[0];
		GrassTextureData.grassBladesSaturations=new float[0];
		GrassTextureData.grassBladesWeights=new int[0];
		GrassTextureData.targetTexture=null;
		GrassTextureData.targetBackTexture=null;
	}
	
	void AddSourceTextures(Texture2D[] addTextures) {
		Texture2D[] newTextures=new Texture2D[GrassTextureData.grassBlades.Length + addTextures.Length];
		Texture2D[] newTexturesMod=new Texture2D[newTextures.Length];
		Color[] newTints=new Color[newTextures.Length];
		float[] newSaturations=new float[newTextures.Length];
		int[] newWeights=new int[newTextures.Length];
		
		int num;
		for(num=0; num<GrassTextureData.grassBlades.Length; num++) {
			newTextures[num] = GrassTextureData.grassBlades[num];
			newTexturesMod[num] = GrassTextureData.grassBladesMod[num];
			newTints[num] = GrassTextureData.grassBladesTints[num];
			newSaturations[num] = GrassTextureData.grassBladesSaturations[num];
			newWeights[num] = GrassTextureData.grassBladesWeights[num];
		}
		for (int i = 0; i < addTextures.Length; i++) {
			newTextures[num] = newTexturesMod[num] = addTextures[i];
			newTints[num] = new Color(0.5f,0.5f,0.5f,1);
			newSaturations[num] = 1.0f;
			newWeights[num] = 100;
			num++;
		}
		GrassTextureData.grassBlades=newTextures;
		GrassTextureData.grassBladesMod=newTexturesMod;
		GrassTextureData.grassBladesTints=newTints;
		GrassTextureData.grassBladesSaturations=newSaturations;
		GrassTextureData.grassBladesWeights=newWeights;
	}
	
	void RemoveSourceTextureAt(int idx) {
		Texture2D[] newTextures=new Texture2D[GrassTextureData.grassBlades.Length-1];
		Texture2D[] newTexturesMod=new Texture2D[newTextures.Length];
		Color[] newTints=new Color[newTextures.Length];
		float[] newSaturations=new float[newTextures.Length];
		int[] newWeights=new int[newTextures.Length];
		
		int num=0;
		for(int i=0; i<GrassTextureData.grassBlades.Length; i++) {
			if (i!=idx) {
				newTextures[num] = GrassTextureData.grassBlades[i];
				newTexturesMod[num] = GrassTextureData.grassBladesMod[i];
				newTints[num] = GrassTextureData.grassBladesTints[i];
				newSaturations[num] = GrassTextureData.grassBladesSaturations[i];
				newWeights[num] = GrassTextureData.grassBladesWeights[i];
				num++;
			}
		}
		GrassTextureData.grassBlades=newTextures;
		GrassTextureData.grassBladesMod=newTexturesMod;
		GrassTextureData.grassBladesTints=newTints;
		GrassTextureData.grassBladesSaturations=newSaturations;
		GrassTextureData.grassBladesWeights=newWeights;
	}
	
	void MakeTextureMod(int idx, bool changed_flag=false) {
		//Debug.Log("mod "+idx);
		if (GrassTextureData.grassBlades[idx]==null) return;
		MakeReadable(GrassTextureData.grassBlades[idx]);
		Color[] cols=GrassTextureData.grassBlades[idx].GetPixels();
		for(int i=0; i<cols.Length; i++) {
			float gscale=cols[i].grayscale;
			float alpha=cols[i].a;
			Color colDesat=new Color(gscale, gscale, gscale, 1);
			cols[i]=Color.Lerp(colDesat, cols[i], GrassTextureData.grassBladesSaturations[idx]);
			cols[i].r=cols[i].r*GrassTextureData.grassBladesTints[idx].r*2;
			cols[i].g=cols[i].g*GrassTextureData.grassBladesTints[idx].g*2;
			cols[i].b=cols[i].b*GrassTextureData.grassBladesTints[idx].b*2;
			cols[i].a=alpha;
		}
		if (changed_flag || GrassTextureData.grassBladesMod[idx]==GrassTextureData.grassBlades[idx] || GrassTextureData.grassBladesMod[idx]==null || GrassTextureData.grassBladesMod[idx].width!=GrassTextureData.grassBlades[idx].width || GrassTextureData.grassBladesMod[idx].height!=GrassTextureData.grassBlades[idx].height ) {
			GrassTextureData.grassBladesMod[idx]=new Texture2D(GrassTextureData.grassBlades[idx].width, GrassTextureData.grassBlades[idx].height, TextureFormat.ARGB32, true);
		}
		GrassTextureData.grassBladesMod[idx].SetPixels (cols);
		GrassTextureData.grassBladesMod[idx].Apply(true,false);
	}
	
	void MakeReadable(Texture2D ntex) {
		AssetImporter _importer=AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(ntex));
		if (_importer) {
			TextureImporter tex_importer=(TextureImporter)_importer;
			if (!tex_importer.isReadable) {
				Debug.LogWarning("Texture ("+ntex.name+") has been reimported as readable.");
				tex_importer.isReadable=true;
				AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(ntex),  ImportAssetOptions.ForceUpdate);
			}
		}
	}
		
	private bool checkChange(ref int val, int nval) {
		bool changed=(nval!=val);
		if (changed) {
			if ((EditorApplication.timeSinceStartup-changedTime)>0.5) {
				changedTime=EditorApplication.timeSinceStartup;
				//Undo.RecordObjects(GrassTextureData, "Undo Volume Grass edit");
			}
		}
		dirtyFlag=dirtyFlag || changed;
		val=nval;
		return changed;
	}	
	private bool checkChange(ref float val, float nval) {
		bool changed=(nval!=val);
		if (changed) {
			if ((EditorApplication.timeSinceStartup-changedTime)>0.5) {
				changedTime=EditorApplication.timeSinceStartup;
				//Undo.RecordObjects(GrassTextureData, "Undo Volume Grass edit");
			}
		}
		dirtyFlag=dirtyFlag || changed;
		val=nval;
		return changed;
	}	
	private bool checkChange(ref Color val, Color nval) {
		bool changed=(nval!=val);
		if (changed) {
			if ((EditorApplication.timeSinceStartup-changedTime)>0.5) {
				changedTime=EditorApplication.timeSinceStartup;
				//Undo.RecordObjects(GrassTextureData, "Undo Volume Grass edit");
			}
		}
		dirtyFlag=dirtyFlag || changed;
		val=nval;
		return changed;
	}	
	
	void SaveTexture(ref string target_filename, string default_filename, ref Texture2D tex) {
		string directory;
		string file;
		if (target_filename=="") {
			directory="Assets";
			file=default_filename;
		} else {
			directory=Path.GetDirectoryName(target_filename);
			file=Path.GetFileNameWithoutExtension(target_filename);
		}
		string path = EditorUtility.SaveFilePanel("Save texture", directory, file, "png");
		if (path != "") {
			target_filename = path = path.Substring(Application.dataPath.Length-6);
			byte[] bytes;
			bytes = tex.EncodeToPNG();
			System.IO.File.WriteAllBytes(path, bytes);
			AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
			AssetDatabase.ImportAsset(path,  ImportAssetOptions.ForceUpdate);
			tex = AssetDatabase.LoadAssetAtPath(path,  typeof(Texture2D)) as Texture2D;
		}
	}
	
	private void ResetProgress(int progress_count, string _progress_description="") {
		progress_count_max=progress_count;
		progress_count_current=0;
		progress_description=_progress_description;
	}
	
	private void CheckProgress() {
		if ( ((progress_count_current++) % progress_granulation) == (progress_granulation-1) )
		{
			EditorUtility.DisplayProgressBar( "Processing...", progress_description, 1.0f*progress_count_current/progress_count_max );
		}
		
	}
	
	public GrassTextureDataPreset GetPreset() {
		GrassTextureDataPreset preset = ScriptableObject.CreateInstance(typeof(GrassTextureDataPreset)) as GrassTextureDataPreset;
		preset.numSlices = GrassTextureData.numSlices;
		preset.numSourceBladesPerSlice = GrassTextureData.numSourceBladesPerSlice;
		preset.numSourceBladesOnBackground = GrassTextureData.numSourceBladesOnBackground;
		preset.sliceHeight = GrassTextureData.sliceHeight;
		preset.textureWidth = GrassTextureData.textureWidth;
		preset.BackColor = GrassTextureData.BackColor;
		preset.randomSeed = GrassTextureData.randomSeed;
		preset.fakeSelfShadowStrength = GrassTextureData.fakeSelfShadowStrength;
		preset.randomYOffset = GrassTextureData.randomYOffset;
		
		// source textures and their properties
		preset.grassBlades = GrassTextureData.grassBlades;
		preset.grassBladesTints = GrassTextureData.grassBladesTints;
		preset.grassBladesSaturations = GrassTextureData.grassBladesSaturations;
		preset.grassBladesWeights = GrassTextureData.grassBladesWeights;
		
		return preset;
	}
	
	public void RestorePreset(GrassTextureDataPreset _preset) {
		GrassTextureDataPreset preset = ScriptableObject.Instantiate(_preset) as GrassTextureDataPreset;
		NewSourceTextures();
		
		GrassTextureData.numSlices=preset.numSlices;
		GrassTextureData.numSourceBladesPerSlice = preset.numSourceBladesPerSlice;
		GrassTextureData.numSourceBladesOnBackground = preset.numSourceBladesOnBackground;
		GrassTextureData.sliceHeight = preset.sliceHeight;
		GrassTextureData.textureWidth = preset.textureWidth;
		GrassTextureData.BackColor = preset.BackColor;
		GrassTextureData.randomSeed = preset.randomSeed;
		GrassTextureData.fakeSelfShadowStrength = preset.fakeSelfShadowStrength;
		GrassTextureData.randomYOffset = preset.randomYOffset;
		
		// source textures and their properties
		GrassTextureData.grassBladesMod = GrassTextureData.grassBlades = preset.grassBlades;
		GrassTextureData.grassBladesTints = preset.grassBladesTints;
		GrassTextureData.grassBladesSaturations = preset.grassBladesSaturations;
		GrassTextureData.grassBladesWeights = preset.grassBladesWeights;
		Color grey = new Color(0.5f, 0.5f, 0.5f, 1); 
		for(int i=0; i<preset.grassBlades.Length; i++) {
			if (	GrassTextureData.grassBladesSaturations[i]!=1 || GrassTextureData.grassBladesTints[i]!=grey) {
				MakeTextureMod(i,true);
			}
		}		
		
	}
}