//
//
// (C) Tomasz Stobierski 2014 - attach this script to the camera, it's Unity Pro only (render texture used), but we don't need to use "Write to Z buffer"
//
//
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections;

[AddComponentMenu("Volume Grass/Components for camera/Setup for rendering")]
[RequireComponent (typeof (Camera))]
public class SetupForGrassRendering : MonoBehaviour {
	
	//
	// you may want to increase this value to render depth for objects placed farther
	// (or decrease it to clip more geometry closer to observer)
	//
	private float FarClip=40f;
	
	//-------------------------------------------------------------------------------
	
	private Camera myCam;
	private Shader shad;
	private RenderTexture myRenderTexture=null;
	public LayerMask cullingMask;
	//public bool useCustomDepthShader=false;
	private const bool useCustomDepthShader=true;
	public static float ZBufferParamA,ZBufferParamB,ZBufferFarClip;
	
	void Awake() {
		if (!enabled) return;
		
		if (GetComponent<Camera>()==null) {
			Debug.LogError("SetupForGrassRendering script (at "+gameObject.name+") - can't find camera component !");
			return;
		}
		
		if ((useCustomDepthShader) && (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.Depth))) {
			shad=Shader.Find("GrassRenderDepth"); // our simple depth rendering shader (rely on native z-depth buffer and don't render into color channels)
		} else {
			shad=Shader.Find("Hidden/Camera-DepthTexture"); // unity's render depth - probably slower as it renders everything into color buffer
		}
		if (!shad) {
			// we've got no shader ? Make simple one (handles native z-buffer for Opaque RenderType only)
			Material mat=new Material("Shader \"RenderDepth\" {SubShader { Tags { \"RenderType\"=\"Opaque\"} \n Pass { ColorMask 0 }}}");
			shad=mat.shader;
		}
		SetupTexture();
		GameObject go=new GameObject("GrassDepthCamera");
		go.AddComponent(typeof(Camera));
		go.transform.parent=transform;
		myCam=go.GetComponent<Camera>();
		SetupParams();
		Shader.SetGlobalFloat("_VGZBufferParamA", SetupForGrassRendering.ZBufferParamA);
		Shader.SetGlobalFloat("_VGZBufferParamB", SetupForGrassRendering.ZBufferParamB);
		Shader.SetGlobalFloat("_VGZBufferFarClip", SetupForGrassRendering.ZBufferFarClip-1);
		#if UNITY_EDITOR
			EditorApplication.playmodeStateChanged += PlaymodeStateChange;
		#endif
	}
	
	#if UNITY_EDITOR
		void PlaymodeStateChange(){
			if (!EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPlaying) {
				Shader.SetGlobalFloat("_VGZBufferParamA", 0);
				Shader.SetGlobalFloat("_VGZBufferParamB", 0);
				Shader.SetGlobalFloat("_VGZBufferFarClip", 0);
				EditorApplication.playmodeStateChanged -= PlaymodeStateChange;
				//EditorUtility.SetDirty(this);
			}
		}
	#endif

	private void SetupTexture() {
		if (myRenderTexture!=null) {
			myRenderTexture.Release();
		}
		myRenderTexture=new RenderTexture(Screen.width, Screen.height, 16);
		if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.Depth)) {
			myRenderTexture.format=RenderTextureFormat.Depth;
		}
		myRenderTexture.Create();
		myRenderTexture.filterMode=FilterMode.Point;
		RenderTexture.active=myRenderTexture;
		myRenderTexture.SetGlobalShaderProperty("_GrassDepthTex");
	}
	private void SetupParams() {
		GetComponent<Camera>().cullingMask=GetComponent<Camera>().cullingMask&(~(1<<GrassRenderingReservedLayer.layer_num));
		myCam.CopyFrom(GetComponent<Camera>());
		myCam.targetTexture=myRenderTexture;
		myCam.cullingMask=cullingMask|(1<<GrassRenderingReservedLayer.layer_num);
		myCam.depth=GetComponent<Camera>().depth-1;
		myCam.SetReplacementShader(shad,"RenderType");
		myCam.renderingPath = RenderingPath.Forward;
		myCam.clearFlags = CameraClearFlags.SolidColor;
		myCam.backgroundColor = Color.white;
		
		float zc0, zc1;
		//if (Application.platform==RuntimePlatform.WindowsEditor || Application.platform==RuntimePlatform.WindowsPlayer || Application.platform==RuntimePlatform.WindowsWebPlayer) {
			// D3D depth linearization factors
			zc0 = 1.0f - FarClip / GetComponent<Camera>().nearClipPlane;
			zc1 = FarClip / GetComponent<Camera>().nearClipPlane;
		//} else {
		//	zc0 = (1.0f - FarClip / camera.nearClipPlane) / 2.0f;
		//	zc1 = (1.0f + FarClip / camera.nearClipPlane) / 2.0f;
		//}
		ZBufferParamA=zc0/FarClip;
		ZBufferParamB=zc1/FarClip;
		ZBufferFarClip=FarClip;
		myCam.farClipPlane=FarClip;
	}
	
	void Update() {
		if ((Screen.width!=myRenderTexture.width) || (Screen.height!=myRenderTexture.height)) {
			SetupTexture();
			SetupParams();
		}
	}

}