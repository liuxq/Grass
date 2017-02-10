//
// Unity implementation of volume ray-traced algorithm
// (inspired by idea found in the paper "Instant Animated Grass" by Ralf Habel et al, published in WSCG Journal 2007)
//
// (C) Tomasz Stobierski 2014
//
Shader "Grass Surface Shader (no zwrite)" {
	Properties {
		//
		// settings
		//
		[HideInInspector] BlockStart ("Settings", Float) = 1
			MAX_RAYDEPTH ("Max raytrace steps", Float) = 10
			_MIP_BIAS ("Filter balance (tex MIPs)", Range(0.2, 0.8)) = 0.4 // 0.5 - center
			_view_angle_damper ("Bending for sharp view angles", Range(0, 0.95)) = 0.8
			_far_distance ("Far distance", Float) = 10
			_far_distance_transition ("Far distance transition length", Float) = 4
			_NoiseTexHash ("Noise for slices hash", 2D) = "black" {}
			HEIGHT ("   [Grass height]", Float) = 0.25
			PLANE_NUM ("   [Slice planes per world unit]", Float) = 8
			GRASS_SLICE_NUM ("   [Slices on blades texture]", Float) = 8
		[HideInInspector] BlockEnd ("", Float) = 0
		
		//
		// grass billboards
		//
		[HideInInspector] BlockStart ("Grass billboards", Float) = 1
			_BladesColor ("Blades Color (RGB)", Color) = (0.5,0.5,0.5,1)
			_BladesSaturation ("Blades saturation", Range (0,2)) = 1
			_DiffFresnel ("Diffuse scattering", Range(0,2)) = 0.6
			_BladesTex ("Grass blades (RGBA)", 2D) = "black" {}
			_BladesBackTex ("Grass blades back texture (RGB)", 2D) = "black" {}
		[HideInInspector] BlockEnd ("", Float) = 0
		
		//
		// grass floor
		//
		[HideInInspector] BlockStart ("Grass floor", Float) = 1
			_FloorColor ("Floor color (RGB)", Color) = (0.5,0.5,0.5,1)
			_FloorSaturation ("Floor saturation", Range (0,2)) = 1
			_MainTex ("Floor texture (RGB)", 2D) = "black" {}
			_floortiling ("Floor tiling", Float) = 3
			_Cutoff ("Floor cut (by AO noise)", Range(0,1)) = 0.5
		[HideInInspector] BlockEnd ("", Float) = 0
		
		//
		// fake AO
		//
		[HideInInspector] BlockStart ("Bottom coloring (fake AO)", Float) = 1
		_AO_value ("Fake AO strength", Range(0,0.4)) = 0.15
		_AO_strength_steep ("strength for steep angle", Range(0,1)) = 0.5
		_AO_far ("strength for far distance", Range(0,1)) = 0.1
		_AO_border_damp ("AO reduction on borders", Range(0,1)) = 0.5
		_NoiseTex ("Noise tex", 2D) = "black" {}
		_AO_noise_tiling ("noise tiling", Float) = 0.05
		[HideInInspector] BlockEnd ("", Float) = 0
		
		//
		// WIND
		//
		[HideInInspector] BlockStart ("Wind >>WIND", Float) = 1
			_wind_dir ("Constant wind bend (xy to world xz)", Vector) =(0,0,0,0)
			_wind_amp ("   amplitude", Float) = 0.02
			_wind_freq ("   noise frequency", Float) = 1
			_wind_speed ("   noise offset anim speed", Float) = 0.1
			// < means separator
			_wind_affector("<Spherical wind zone position (xyz,radius)", Vector) = (0,0,0,0)
			_wind_const_bend_affector ("   directional bend", Float) = 0
			_wind_amp_affector ("   amplitude", Float) = 1
			_wind_freq_affector ("   noise frequency", Float) = 1
			_wind_speed_affector ("   noise offset anim speed", Float) = 0.5
			
			_wind_to_normal ("<Normals modification by wind", Range(0, 1)) = 0.2
		[HideInInspector] BlockEnd ("", Float) = 0
		
		//
		// global colormap
		//
		[HideInInspector] BlockStart ("Global colormap >>GLOBAL_COLORING", Float) = 1
			_GlobalColorTex ("Global color texture", 2D) = "black" {}
			_GlobalColoringMult ("multiply value", Range(0,1)) = 0
			_GlobalColoringAdd ("additive value", Range(0,1)) = 1
			UVbounds ("UV bounds (xmin, xmax, zmin, zmax)", Vector) = (0,1,0,1)
		[HideInInspector] BlockEnd ("", Float) = 0

		//
		// IBL diffuse
		//
		[HideInInspector] BlockStart ("IBL Diffuse >>IBL_DIFFUSE", Float) = 1
			IBL_DiffStrength ("IBL Diffuse strength", Range(0,1)) = 0.2
			IBL_DiffDirectStrength ("Direct lighting strength", Range(0,1)) = 1
			_CubemapDiff ("Custom IBL Diffuse cubemap", CUBE) = "black" {}
		[HideInInspector] BlockEnd ("", Float) = 0
		
		//
		// IBL specular
		//
		[HideInInspector] BlockStart ("IBL Specular >>IBL_SPEC", Float) = 1
			_SpecColor ("IBL Specular Color", Color) = (0.5, 0.5, 0.5, 1)
			IBL_Gloss ("IBL Gloss", Range (0, 1)) = 0.5
			IBL_SpecStrength ("IBL Spec strength", Range(0,2)) = 0.2
			IBL_SpecFresnel ("IBL Spec fresnel", Range(0,1)) = 0.2
			_CubemapSpec ("Custom IBL Spec cubemap", CUBE) = "black" {}
		[HideInInspector] BlockEnd ("", Float) = 0
		
		//_mod ("mod (internal use)", Float) = 0
	}

	SubShader {
		Tags { "Queue"="Geometry+250" "IgnoreProjector"="False" "RenderType"="VolumeGrass"} // custom render type specified if you need to write special shader replacement functionality
		LOD 700
		
		CGPROGRAM
		#pragma surface surf Lambert alphatest:_Cutoff vertex:vert fullforwardshadows
		
		#pragma target 3.0
		#pragma glsl
		
		#pragma shader_feature GLOBAL_COLORING GLOBAL_COLORING_OFF
		#pragma shader_feature WIND WIND_OFF
		#pragma shader_feature IBL_DIFFUSE IBL_DIFFUSE_OFF
		#pragma shader_feature IBL_SPEC IBL_SPEC_OFF
		//#pragma shader_feature WRITE_INTO_DEPTH WRITE_INTO_DEPTH_OFF
		//#define GLOBAL_COLORING
		//#define WIND
		//#define IBL_DIFFUSE
		//#define IBL_SPEC
			
		#include "VG_Base.cginc"
		 
		ENDCG
		
	}
	
	Fallback "Diffuse"
	CustomEditor "VolumeGrassMaterialInspector"	
}
