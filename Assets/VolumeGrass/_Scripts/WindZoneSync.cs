//////////////////////////////////////////////////////////////////////////////////////////////////////
//
// attach script to a game object and specify if it's either directional or spherical wind affector
//  fill the list of VolumeGrass materials to be affected

//  directional wind zone affects main wind props in Volume Grass material
//  spherical wind zone affects spherical (FIXME - use transform scale X for radius !!!)
//
// (C) Tomasz Stobierski 2014
//
//////////////////////////////////////////////////////////////////////////////////////////////////////
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
[AddComponentMenu("Volume Grass/Components for grass/Wind Zone Sync")]
public class WindZoneSync : MonoBehaviour {

	public float Amplitude=1f;
	public float ConstantBend=0.1f;
	public float NoiseFrequency=0.06f;
	public float NoiseOffsetAnimSpeed=0.5f;
	public bool WindZoneIsSpherical=false;
	public Material[] GrassMaterials;
	
	/////////////////////////////////////////////////////////////////////////////////////////////////
	private int AffectorPosPropID=-1;
	private int AffectorBendPropID=-1;
	private int DirPropID=-1;
	private int AmpPropID=-1;
	private int AffectorAmpPropID=-1;
	private int NoiseFrequencyPropID=-1;
	private int NoiseOffsetAnimSpeedPropID=-1;
	private int NoiseFrequencyAffectorPropID=-1;
	private int NoiseOffsetAnimAffectorSpeedPropID=-1;
	
	public void Awake() {
		TakeShaderPropIDs();
	}
	
	public void Update() {
		if (GrassMaterials==null) return;
		if (!Application.isPlaying) TakeShaderPropIDs();
		for(int i=0; i<GrassMaterials.Length; i++) {
			Material grassMat=GrassMaterials[i];
			if (grassMat) {
				Vector4 vec=Vector4.zero;
				if (WindZoneIsSpherical) {
					vec=new Vector4(transform.position.x, transform.position.y, transform.position.z, transform.localScale.x);
					if (grassMat.HasProperty(AffectorPosPropID)) grassMat.SetVector (AffectorPosPropID, vec);
					if (grassMat.HasProperty(AffectorBendPropID)) grassMat.SetFloat(AffectorBendPropID, ConstantBend);
					if (grassMat.HasProperty(AffectorAmpPropID)) grassMat.SetFloat(AffectorAmpPropID, Amplitude);
					if (grassMat.HasProperty(NoiseFrequencyAffectorPropID)) grassMat.SetFloat(NoiseFrequencyAffectorPropID, NoiseFrequency);
					if (grassMat.HasProperty(NoiseOffsetAnimAffectorSpeedPropID)) grassMat.SetFloat(NoiseOffsetAnimAffectorSpeedPropID, NoiseOffsetAnimSpeed);
				} else { 
					vec.x=transform.forward.x*ConstantBend;
					vec.y=transform.forward.z*ConstantBend;
					if (grassMat.HasProperty(DirPropID)) grassMat.SetVector (DirPropID, vec);
					if (grassMat.HasProperty(AmpPropID)) grassMat.SetFloat(AmpPropID, Amplitude);
					if (grassMat.HasProperty(NoiseFrequencyPropID)) grassMat.SetFloat(NoiseFrequencyPropID, NoiseFrequency);
					if (grassMat.HasProperty(NoiseOffsetAnimSpeedPropID)) grassMat.SetFloat(NoiseOffsetAnimSpeedPropID, NoiseOffsetAnimSpeed);
				}
			}
		}
	}
	
	private void TakeShaderPropIDs() {
		if (AffectorPosPropID==-1) 	AffectorPosPropID=Shader.PropertyToID("_wind_affector");
		if (AffectorBendPropID==-1) AffectorBendPropID=Shader.PropertyToID("_wind_const_bend_affector");
		if (DirPropID==-1) DirPropID=Shader.PropertyToID("_wind_dir");
		if (AmpPropID==-1) AmpPropID=Shader.PropertyToID("_wind_amp");
		if (AffectorAmpPropID==-1) AffectorAmpPropID=Shader.PropertyToID("_wind_amp_affector");
		if (NoiseFrequencyPropID==-1) NoiseFrequencyPropID=Shader.PropertyToID("_wind_freq");
		if (NoiseOffsetAnimSpeedPropID==-1) NoiseOffsetAnimSpeedPropID=Shader.PropertyToID("_wind_speed");
		if (NoiseFrequencyAffectorPropID==-1) NoiseFrequencyAffectorPropID=Shader.PropertyToID("_wind_freq_affector");
		if (NoiseOffsetAnimAffectorSpeedPropID==-1) NoiseOffsetAnimAffectorSpeedPropID=Shader.PropertyToID("_wind_speed_affector");
	}

}
