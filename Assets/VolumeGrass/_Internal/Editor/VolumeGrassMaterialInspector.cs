using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

public class VolumeGrassMaterialInspector : MaterialEditor {
	private bool unfolded=true;
	private bool propBlockAvailable=true;
	public static bool[] blocks=new bool[1000];
	
	private void ShaderPropertyImpl(Shader shader, int propertyIndex)
	{
		Material myMat=target as Material;
		Material[] mats=new Material[1] {myMat};
		MaterialProperty aProp=MaterialEditor.GetMaterialProperty(mats, propertyIndex); 
		GUIStyle boldFoldoutStyle = new GUIStyle(EditorStyles.foldout);
		boldFoldoutStyle.fontStyle = FontStyle.Bold;
		Color c;
		
		int i = propertyIndex;
		bool separator_flag=false;
		string label = ShaderUtil.GetPropertyDescription(shader, i);
		if (label.IndexOf("<")>=0) {
			label=label.Substring(1);
			separator_flag=true;
		}
		string propertyName = ShaderUtil.GetPropertyName(shader, i);
		bool adjustementFlag=false;
		if (ShaderUtil.GetPropertyType(shader, i)==ShaderUtil.ShaderPropertyType.Float) {
			if (propertyName=="BlockStart") {
				adjustementFlag=true;
				
				EditorGUILayout.BeginVertical("box");
				c=GUI.color;
				GUI.color=new Color(0.8f,1, 0.8f);
				GUILayout.BeginHorizontal();
				GUILayout.Space(12);
				
				propBlockAvailable=IsAvailable(ref label);
				if (propBlockAvailable) {
					bool newVal=EditorGUILayout.Foldout(VolumeGrassMaterialInspector.blocks[i], label, boldFoldoutStyle);
					if (VolumeGrassMaterialInspector.blocks[i]!=newVal) {
						VolumeGrassMaterialInspector.blocks[i]=newVal;
						EditorUtility.SetDirty(target);
					}
				} else {
					EditorGUILayout.LabelField(label+" (switched off)");
				}
				unfolded=VolumeGrassMaterialInspector.blocks[i];
				GUILayout.EndHorizontal ();
				GUI.color=c;
				GUILayout.BeginHorizontal();
				GUILayout.Space(10);
				GUILayout.BeginVertical();
			} else if (propertyName=="BlockEnd") {
				adjustementFlag=true;
				unfolded=true;
				propBlockAvailable=true;
				
				GUILayout.EndVertical();
				GUILayout.EndHorizontal();
				EditorGUILayout.EndVertical();
				
				GUILayout.Space(3);
			}
		}
		if (!unfolded || !propBlockAvailable) return;
		
		if (	separator_flag) GUILayout.Space(8);
		
		switch (ShaderUtil.GetPropertyType(shader, i))
		{
		case ShaderUtil.ShaderPropertyType.Range: // float ranges
		{
			GUILayout.BeginHorizontal();
			RangeProperty(aProp, label);
			GUILayout.EndHorizontal();
			
			break;
		}
		case ShaderUtil.ShaderPropertyType.Float: // floats
		{
			if (!adjustementFlag) FloatProperty(aProp, label);
			break;
		}
		case ShaderUtil.ShaderPropertyType.Color: // colors
		{
			ColorProperty(aProp, label);
			break;
		}
		case ShaderUtil.ShaderPropertyType.TexEnv: // textures
		{
			TextureProperty(aProp, label, false);
			GUILayout.Space(6);
			break;
		}
		case ShaderUtil.ShaderPropertyType.Vector: // vectors
		{
			VectorProperty(aProp, label);
			break;
		}
		default:
		{
			GUILayout.Label("(unknown prop type for " + label + " ): " + ShaderUtil.GetPropertyType(shader, i));
			break;
		}
		}
	}
	
	private bool IsAvailable(ref string label) {
		if (label.IndexOf(">>")<0) return true;
		
		string keyword=label.Substring(label.IndexOf(">>")+2);
		label=label.Substring(0, label.IndexOf(">>"));
		Material targetMat = target as Material;
		string[] keyWords = targetMat.shaderKeywords;
		return keyWords.Contains(keyword);	
	}
	
	public override void OnInspectorGUI ()
	{
		serializedObject.Update ();
		var theShader = serializedObject.FindProperty ("m_Shader");	
		if (isVisible && !theShader.hasMultipleDifferentValues && theShader.objectReferenceValue != null)	{
			//
			// features (by shader keywords)
			//
			Color c=GUI.color;
			GUI.color=new Color(0.8f, 1, 0.8f);
			EditorGUILayout.BeginVertical("box");
			GUILayout.Space(2);
			GUI.color=new Color(1f, 1, 0.5f);
			EditorGUILayout.LabelField("Features used:", EditorStyles.boldLabel);
			GUI.color=new Color(1f, 1, 1f);
			Material targetMat = target as Material;
			string[] keyWords = targetMat.shaderKeywords;
			
			bool globalColoring_keyword = keyWords.Contains ("GLOBAL_COLORING");
			bool wind_keyword = keyWords.Contains ("WIND");
			bool IBL_diff_keyword = keyWords.Contains ("IBL_DIFFUSE");
			bool IBL_spec_keyword = keyWords.Contains ("IBL_SPEC");
			//bool zwrite_keyword = keyWords.Contains ("WRITE_INTO_DEPTH");
			
			EditorGUI.BeginChangeCheck();
			globalColoring_keyword = EditorGUILayout.Toggle ("Global color map", globalColoring_keyword);
			wind_keyword = EditorGUILayout.Toggle ("Wind", wind_keyword);
			IBL_diff_keyword = EditorGUILayout.Toggle ("IBL Diffuse", IBL_diff_keyword);
			IBL_spec_keyword = EditorGUILayout.Toggle ("IBL Specular", IBL_spec_keyword);
			//zwrite_keyword = EditorGUILayout.Toggle ("Write to Z buffer", zwrite_keyword);
			if (EditorGUI.EndChangeCheck())
			{
				//var keywords = new List<string> { globalColoring_keyword ? "GLOBAL_COLORING" : "GLOBAL_COLORING_OFF",  wind_keyword ? "WIND" : "WIND_OFF", IBL_diff_keyword ? "IBL_DIFFUSE":"IBL_DIFFUSE_OFF", IBL_spec_keyword ? "IBL_SPEC":"IBL_SPEC_OFF", zwrite_keyword ? "WRITE_INTO_DEPTH":"WRITE_INTO_DEPTH_OFF"};
				var keywords = new List<string> { globalColoring_keyword ? "GLOBAL_COLORING" : "GLOBAL_COLORING_OFF",  wind_keyword ? "WIND" : "WIND_OFF", IBL_diff_keyword ? "IBL_DIFFUSE":"IBL_DIFFUSE_OFF", IBL_spec_keyword ? "IBL_SPEC":"IBL_SPEC_OFF"};
				targetMat.shaderKeywords = keywords.ToArray ();
				EditorUtility.SetDirty(targetMat);
			}		
			EditorGUILayout.EndVertical();
			GUI.color=c;
			
			GUILayout.Space(8);				
			//
			// props
			//
			EditorGUIUtility.labelWidth=200;
			
			EditorGUI.BeginChangeCheck();
			Shader shader = theShader.objectReferenceValue as Shader;
			
			for (int i = 0; i < ShaderUtil.GetPropertyCount(shader); i++)
			{
				ShaderPropertyImpl(shader, i);
			}
			
			if (EditorGUI.EndChangeCheck()) {
				PropertiesChanged();
			}
		}
		
	}
	
}