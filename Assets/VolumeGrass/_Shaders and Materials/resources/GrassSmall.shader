//
// Unity implementation of volume ray-traced algorithm
// (inspired by idea found in the paper "Instant Animated Grass" by Ralf Habel et al, published in WSCG Journal 2007)
//
// (C) Tomasz Stobierski 2014
//
Shader "Grass Shader afterlightmap Small (no zwrite)" {
	Properties {
		//
		// settings
		//
		[HideInInspector] BlockStart ("Settings", Float) = 1
			//_MIP_BIAS ("Filter balance (tex MIPs)", Range(0.2, 0.8)) = 0.4 // 0.5 - center
			_view_angle_damper ("Bending for sharp view angles", Range(0, 0.95)) = 0.8
			_far_distance ("Far distance", Float) = 10
			_far_distance_transition ("Far distance transition length", Float) = 4
			_NoiseTexHash ("Noise for slices hash", 2D) = "black" {}
			HEIGHT ("   [Grass height]", Float) = 0.25
			//PLANE_NUM ("   [Slice planes per world unit]", Float) = 8
			//GRASS_SLICE_NUM ("   [Slices on blades texture]", Float) = 8
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
			//_wind_affector("<Spherical wind zone position (xyz,radius)", Vector) = (0,0,0,0)
			//_wind_const_bend_affector ("   directional bend", Float) = 0
			//_wind_amp_affector ("   amplitude", Float) = 1
			//_wind_freq_affector ("   noise frequency", Float) = 1
			//_wind_speed_affector ("   noise offset anim speed", Float) = 0.5
			
			//_wind_to_normal ("<Normals modification by wind", Range(0, 1)) = 0.2
		[HideInInspector] BlockEnd ("", Float) = 0
	}

	SubShader {
		Tags { "Queue"="Geometry+250" "IgnoreProjector"="False" "RenderType"="VolumeGrass"} // custom render type specified if you need to write special shader replacement functionality
		LOD 700

		pass{
		CGPROGRAM
		//#pragma surface surf Lambert alphatest:_Cutoff vertex:vert fullforwardshadows
		
		#pragma target 2.0
		#pragma glsl

		#pragma vertex vert
        #pragma fragment frag
        #include "UnityCG.cginc"
		 
		//#pragma shader_feature WIND WIND_OFF
		//#define WIND

		// when not defined (commented out) we're in Gamma color space
		//#define COLORSPACE_LINEAR
		//
		//////////////////////////////////////////////////////////////////////////// 

		#define TRANSPARENCY_ZTEST_VALUE 0.6
		#define TRANSPARENCY_BREAK_VALUE 0.95

		

		#define BOTTOM_COLORING
		#define BOTTOM_COLORING_NOISE_DRIVEN

		#define NOISE_CHANNEL_WINDX r
		#define NOISE_CHANNEL_WINDY g
		#define NOISE_CHANNEL_BCOLORING b	

		//////////////////////////////////////////////////////////////////

		//float PLANE_NUM;
		//float GRASS_SLICE_NUM;
		float HEIGHT;


		half4 _FloorColor;
		half4 _BladesColor;
		half _FloorSaturation;
		half _BladesSaturation;

		sampler2D _MainTex; // floor tex
		sampler2D _BladesTex;
		float4 _BladesTex_TexelSize;
		sampler2D _BladesBackTex;
		float4 _BladesBackTex_TexelSize;
		sampler2D _NoiseTex;
		sampler2D _NoiseTexHash;

		sampler2D _GrassDepthTex;
		float _VGZBufferParamA;
		float _VGZBufferParamB;
		float _VGZBufferFarClip;

		float _floortiling;
		float _view_angle_damper;

		float _far_distance;
		float _far_distance_transition;

		struct Input {
			float4 pos : SV_POSITION;
			float4 worldPosDepth: TEXCOORD0; // xyz - worldPos, z - eye depth
			float4 screenPos: TEXCOORD1;
			float3 EyeDirTan: TEXCOORD2;

			fixed4 color:COLOR0;
		};

		
		Input vert (appdata_full v) {
			Input o;
			UNITY_INITIALIZE_OUTPUT(Input,o);
			o.pos = UnityObjectToClipPos(v.vertex);

			o.worldPosDepth.xyz=mul(unity_ObjectToWorld, v.vertex).xyz;
			v.tangent.xyz = cross(v.normal, float3(0,0,1));
			v.tangent.w = -1;

			v.vertex.xyz -= v.normal * HEIGHT * v.color.g*1.04;
			COMPUTE_EYEDEPTH(o.worldPosDepth.w);

			o.screenPos.x = 1.0;

			fixed3 worldNormal = UnityObjectToWorldNormal(v.normal);
			fixed3 worldTangent = UnityObjectToWorldDir(v.tangent.xyz);
			fixed tangentSign = v.tangent.w * unity_WorldTransformParams.w;
			fixed3 worldBinormal = cross(worldNormal, worldTangent) * tangentSign;

			float3 t0 = float3(worldTangent.x, worldBinormal.x, worldNormal.x);
			float3 t1 = float3(worldTangent.y, worldBinormal.y, worldNormal.y);
			float3 t2 = float3(worldTangent.z, worldBinormal.z, worldNormal.z);

			fixed3 worldViewDir = UnityWorldSpaceViewDir(o.worldPosDepth.xyz);
			o.EyeDirTan = -(t0.xyz * worldViewDir.x + t1.xyz * worldViewDir.y  + t2.xyz * worldViewDir.z);

			o.screenPos = ComputeScreenPos (o.pos);
			o.color = v.color;

			return o;
		}

		fixed4 frag (Input IN) : color 
		{
			float3 EyeDirTan = IN.EyeDirTan;
			fixed4 o = fixed4(0,0,0,1);

			float GRASS_SLICE_NUM = 4;

			float PLANE_NUM_INV=0.125;
			float GRASS_SLICE_NUM_INV=0.25;

 			float zoffset = IN.color.r+IN.color.g;
 			zoffset = (zoffset>1) ? 1 : zoffset;

			//float zoffset = 0;

			// bend slices
			/*float angle_fade=EyeDirTan.z;
			angle_fade*=angle_fade;
			angle_fade=1-angle_fade;
			EyeDirTan.z*=lerp(1, angle_fade, _view_angle_damper);
			EyeDirTan = normalize(EyeDirTan);	*/		

			float3 rayPos = float3(IN.worldPosDepth.xz, -zoffset*GRASS_SLICE_NUM_INV);
			//float3 rayPos = float3(IN.worldPosDepth.xz, 0);
	
			float rayLength=0;
			float3 delta_next=float3(PLANE_NUM_INV, PLANE_NUM_INV, GRASS_SLICE_NUM_INV);
	
			// evaluated pixel color
			half4 c = half4(0.0,0.0,0.0,0.0);

 			float3 rayPosN=float3(rayPos.xy*8, rayPos.z*GRASS_SLICE_NUM);
			float3 delta=-frac(rayPosN);
			delta=(EyeDirTan>0) ? frac(-rayPosN) : delta;
			delta*=delta_next;
			delta_next/=abs(EyeDirTan);
			delta/=EyeDirTan;
			delta.z=(rayPos.z<0)?delta.z:delta_next.z;

			//fixed3 base_col = fixed3(0,0,0);
	
			float2 _uv;
			half4 _col;
			int hitcount;
	
			bool zhit=false;

			bool xy_flag;
			float delta_tmp; 
			float bladesTex_xw = _BladesTex_TexelSize.x*_BladesTex_TexelSize.w;

 			for(hitcount=0; hitcount < 3; hitcount++) {

				xy_flag= delta.x<delta.y;
				delta_tmp=xy_flag ? delta.x : delta.y;
				zhit=(delta.z<delta_tmp);
				delta_tmp=zhit ? delta.z : delta_tmp;

				//rayLength=(c.w>TRANSPARENCY_ZTEST_VALUE) ? rayLength : (rayLength+delta_tmp);
				rayPos+=delta_tmp*EyeDirTan;

 				if (!zhit) {
 					float3 rayPos_tmp = xy_flag ? rayPos.xyz : rayPos.yxz;

					//float2 htmp=tex2D(_NoiseTexHash, float2(rayPos_tmp.x*0.03+0.001,0)).rg;
					//float HASH_OFFSET=(xy_flag ? htmp.x : htmp.y);

					//_uv=rayPos_tmp.yz;//+float2(HASH_OFFSET,rayPos_tmp.x*PREMULT);
					//_uv = rayPos.yz;
	 				//_col=tex2Dlod(_BladesTex, float4(_uv.x*_BladesTex_TexelSize.x*_BladesTex_TexelSize.w, _uv.y, mip_selector));
					_col=tex2D(_BladesTex, float2(rayPos_tmp.y*bladesTex_xw, rayPos_tmp.z));
	 				//_col.a*=saturate( (rayPos_tmp.z*GRASS_SLICE_NUM+hgt)*removeVerticalWrap );

					_col.rgb*=_col.a;

 					c+=(1-c.w)*_col;
 			
 					delta.xyz=xy_flag ? float3(delta_next.x, delta.yz-delta.x) : float3(delta.x-delta.y, delta_next.y, delta.z-delta.y);
 				}
 				//if (zhit || c.w>=TRANSPARENCY_BREAK_VALUE) break;
			}

			return c;	 
			/*
			_uv.y*=GRASS_SLICE_NUM;

			_col=tex2D(_BladesBackTex, _uv);

			float floor_cw=zhit ? c.w : 1;
			c=(c+(1-c.w)*_col); 			

			half3 blades_desat=dot(half3(0.33,0.33,0.33), c.rgb).xxx;
			c.rgb=lerp(blades_desat, c.rgb, _BladesSaturation);
			c.rgb*=_BladesColor.rgb*2;
	
			c.rgb=lerp(base_col, c.rgb, floor_cw); 
 			o.rgb=lerp(c.rgb, base_col, distanceT);
 	
			o.a=floor_cw;
			o.a=(rayPos.z>-0.001) ? floor_cw : o.a;

			float sceneDepth;

			// custom LinearEyeDepth() parametrization
			sceneDepth = 1.0 / ( (_VGZBufferParamA * tex2Dproj(_GrassDepthTex,UNITY_PROJ_COORD(IN.screenPos)).r) + _VGZBufferParamB );
			sceneDepth=(sceneDepth>_VGZBufferFarClip)?10000:sceneDepth; // don't clip beyond depth buffer distance
			float grassDepth = IN.worldPosDepth.w + rayLength * TILING_FACTOR;
			grassDepth = IN.worldPosDepth.w > sceneDepth+0.05 ? 0 : grassDepth;
			clip(sceneDepth-grassDepth);

			return o;*/
	
		}

		 
		ENDCG
		}
	}
	
	Fallback "Diffuse"
	CustomEditor "VolumeGrassMaterialInspector"	
}
