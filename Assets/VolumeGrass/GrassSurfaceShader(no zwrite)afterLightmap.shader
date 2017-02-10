//
// Unity implementation of volume ray-traced algorithm
// (inspired by idea found in the paper "Instant Animated Grass" by Ralf Habel et al, published in WSCG Journal 2007)
//
// (C) Tomasz Stobierski 2014
//
Shader "Grass Shader afterlightmap (no zwrite)" {
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

		#pragma vertex vert
        #pragma fragment frag
        #include "UnityCG.cginc"
		
		#pragma shader_feature WIND WIND_OFF
		//#define WIND
			
		// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

		////////////////////////////////////////////////////////////////////////////
		//
		// IBL configuration section
		// use to sync with skyshop sky rotation
		//
		//#define SKYSHOP_SKY_ROTATION
		// if not defined we will decode LDR cubemaps (RGB only)
		#define IBL_HDR_RGBM

		// when not defined (commented out) we're in Gamma color space
		//#define COLORSPACE_LINEAR
		//
		//////////////////////////////////////////////////////////////////////////// 

		#define TRANSPARENCY_ZTEST_VALUE 0.6
		#define TRANSPARENCY_BREAK_VALUE 0.95

		//#define CUSTOM_HASH_FUNCTION (frac(fmod(rayPos_tmp.x,39))*fmod(rayPos_tmp.x,39)*9.15)
		#define FADE_PARALLELS

		#define BOTTOM_COLORING
		#define BOTTOM_COLORING_NOISE_DRIVEN

		#define NOISE_CHANNEL_WINDX r
		#define NOISE_CHANNEL_WINDY g
		#define NOISE_CHANNEL_BCOLORING b	

		//#define DONT_USE_SPHERICAL_WIND
		//#define DONT_USE_MAIN_WIND

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
		half _AO_value;
		half _AO_strength_steep;
		float _AO_noise_tiling;
		float _AO_far;
		float _AO_border_damp;

		float _wind_amp;
		float2 _wind_dir;
		float _wind_speed;
		float _wind_freq;

		//float4 _wind_affector;		
		//float _wind_const_bend_affector;
		//float _wind_amp_affector;
		//float _wind_speed_affector;
		//float _wind_freq_affector;
		//float _wind_to_normal;
		float affector_blend_darkening; // accessed dynamically from soccerball script

		//float _MIP_BIAS;

		float _DiffFresnel;

		struct Input {
			float4 pos : SV_POSITION;
			float4 worldPosDepth: TEXCOORD0; // xyz - worldPos, z - eye depth
			float3 t0: TEXCOORD1;
			float3 t1: TEXCOORD2;
			float3 t2: TEXCOORD3;
			float4 screenPos: TEXCOORD4;
			fixed4 color:COLOR0;
		};

		// quick gamma to linear approx of pow(n,2.2) function
		inline float FastToLinear(float t) {
				t *= t * (t * 0.305306011 + 0.682171111) + 0.012522878;
				return t;
		}

		half3 DecodeRGBM(float4 rgbm)
		{
			#ifdef IBL_HDR_RGBM
				// gamma/linear RGBM decoding
				#if defined(COLORSPACE_LINEAR)
	    			return rgbm.rgb * FastToLinear(rgbm.a) * 8;
				#else
	    			return rgbm.rgb * rgbm.a * 8;
				#endif
			#else
    			return rgbm.rgb;
			#endif
		}

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

			o.t0 = float3(worldTangent.x, worldBinormal.x, worldNormal.x);
			o.t1 = float3(worldTangent.y, worldBinormal.y, worldNormal.y);
			o.t2 = float3(worldTangent.z, worldBinormal.z, worldNormal.z);

			o.screenPos = ComputeScreenPos (o.pos);
			o.color = v.color;

			return o;
		}

		fixed4 frag (Input IN) : color 
		{
			fixed3 worldViewDir = normalize(UnityWorldSpaceViewDir(IN.worldPosDepth.xyz));
			fixed3 viewDir = IN.t0.xyz * worldViewDir.x + IN.t1.xyz * worldViewDir.y  + IN.t2.xyz * worldViewDir.z;

			fixed4 o = fixed4(0,0,0,0);
			o.a=1;

			float GRASS_SLICE_NUM = 4;
			float PLANE_NUM = 7;

			float PLANE_NUM_INV=1.0/PLANE_NUM;
			float GRASS_SLICE_NUM_INV=1.0/GRASS_SLICE_NUM;
			float TILING_FACTOR=HEIGHT*GRASS_SLICE_NUM;
			float PREMULT=PLANE_NUM*GRASS_SLICE_NUM_INV;

			float dist=distance(IN.worldPosDepth.xyz, _WorldSpaceCameraPos);// IN.worldPosDepth.w;//
			float distanceT=saturate((dist-_far_distance)/_far_distance_transition);

			float2 coordsDetail=IN.worldPosDepth.xz/TILING_FACTOR;

 			float zoffset = IN.color.r+IN.color.g;
 			zoffset = (zoffset>1) ? 1 : zoffset;

			float3 EyeDirTan = -normalize(viewDir); // eye vector in tangent space

			// bend slices
			float angle_fade=EyeDirTan.z;
			angle_fade*=angle_fade;
			angle_fade=1-angle_fade;
			EyeDirTan.z*=lerp(1, angle_fade, _view_angle_damper);
			EyeDirTan = normalize(EyeDirTan);		

			//float3 mipcoords=(IN.worldPosDepth.xzy*float3(1,1,GRASS_SLICE_NUM)/TILING_FACTOR)*_BladesTex_TexelSize.w*_MIP_BIAS; // 3d filtering
			/*float3 mipcoords=(float3(1,1,4)); // 3d filtering
			float3 dx = ddx( mipcoords );
			float3 dy = ddy( mipcoords );
			float d = min( dot( dx, dx ), dot( dy, dy ));
			float2 mip_selector=min(log2(d), float2(4,4)).xx-(1.5*angle_fade-1.5);*/

			float2 mip_selector = float2(0,0);

			
			#if defined(WIND)
				float4 wtmp;
				float2 wind_coords;
				float2 wind_offset_main=0;
		
				#ifndef DONT_USE_MAIN_WIND
					float _wind_dir_magnitude=length(_wind_dir);
					float2 wind_dir=(_wind_dir_magnitude==0) ? float2(1,1) : -_wind_dir/_wind_dir_magnitude;
					wind_coords=coordsDetail*_wind_freq+_Time.xx*_wind_speed*wind_dir;
					wtmp=tex2D(_NoiseTex, wind_coords.xy);
					wind_offset_main=float2(wtmp.NOISE_CHANNEL_WINDX, wtmp.NOISE_CHANNEL_WINDY)-0.5;
					wind_offset_main-=_wind_dir;
					wind_offset_main*=_wind_amp;
				#endif
		

				//float wind_to_normal=_wind_to_normal;
				
				float2 wind_offset=saturate(1-IN.color.r*2);
				

				wind_offset*=wind_offset_main;

				EyeDirTan.xy += wind_offset*EyeDirTan.z/(1-zoffset);
	 			float3 rayPos = float3(coordsDetail+wind_offset*GRASS_SLICE_NUM_INV, -zoffset*GRASS_SLICE_NUM_INV);
				EyeDirTan=normalize(EyeDirTan);	 			
			#else
	 			float3 rayPos = float3(coordsDetail, -zoffset*GRASS_SLICE_NUM_INV);
			#endif	

 			float hgt=GRASS_SLICE_NUM_INV*saturate(IN.color.g-IN.color.r);

			float3 EyeDirTanAbs=abs(EyeDirTan);
			#ifdef FADE_PARALLELS
				float2 fade_parallels=angle_fade*EyeDirTanAbs.xy-angle_fade+1;
				fade_parallels+=IN.color.z*(1-fade_parallels);
			#endif
	
			float rayLength=0;
			float3 delta_next=float3(PLANE_NUM_INV,PLANE_NUM_INV,GRASS_SLICE_NUM_INV);
	
			// evaluated pixel color
			half4 c = half4(0.0,0.0,0.0,0.0);

 			float3 rayPosN=float3(rayPos.xy*PLANE_NUM, rayPos.z*GRASS_SLICE_NUM);
			float3 delta=-frac(rayPosN);
			delta=(EyeDirTan>0) ? frac(-rayPosN) : delta;
			delta*=delta_next;
			delta_next/=EyeDirTanAbs;
			delta/=EyeDirTan;
			delta.z=(rayPos.z<0)?delta.z:delta_next.z;
	
			float2 uv_bottom=rayPos.xy+delta.z*EyeDirTan.xy;
			#ifdef BOTTOM_COLORING
				float bottom_cut=_AO_value*(1-zoffset*_AO_border_damp);
				#ifdef BOTTOM_COLORING_NOISE_DRIVEN
					float bval=tex2D(_NoiseTex, uv_bottom*_AO_noise_tiling).NOISE_CHANNEL_BCOLORING;
					bval=0.3*bval+0.7*tex2D(_NoiseTex, uv_bottom*_AO_noise_tiling*0.2).NOISE_CHANNEL_BCOLORING;
					bottom_cut*=bval*0.8+0.2;
				#endif
				float bcoloring_fct=(1-angle_fade);
				bcoloring_fct*=bcoloring_fct;
				bcoloring_fct*=bcoloring_fct;
				bottom_cut*=lerp(1, _AO_strength_steep, bcoloring_fct);
				bottom_cut*=GRASS_SLICE_NUM;
				bottom_cut*=(1-IN.color.g*0.7);
				float bottom_ao_damp=1;
			#endif		
			
			

			// resolve far distance color

			fixed3 base_col=tex2D(_MainTex, uv_bottom/_floortiling).rgb;
			#ifdef BOTTOM_COLORING
			float AO_bottom=saturate(1-bottom_cut*lerp(1, _AO_far, distanceT) );
			base_col*=AO_bottom;
			#endif
			base_col=lerp(dot(half3(0.33,0.33,0.33), base_col).xxx, base_col, _FloorSaturation);
			base_col*=_FloorColor.rgb*4;

			float cval=1;
			float fresnelFct=(1+EyeDirTan.z);
			fresnelFct*=fresnelFct;
			fresnelFct*=fresnelFct;
			fresnelFct*=fresnelFct;
			float diffuseScatteringFactor=1.0 + fresnelFct*_DiffFresnel*(distanceT*0.7*(1-zoffset)+0.3);

			// early exit at far distance
 			if (distanceT>0.999) { 
				o.rgb=base_col;
				o.rgb *= diffuseScatteringFactor;
		
				#ifdef BOTTOM_COLORING_NOISE_DRIVEN
					o.a=(1-IN.color.g)*(2.5+bval);
				#endif

				return o;
			}
				
			float removeVerticalWrap=-1.0f/_BladesTex_TexelSize.y*GRASS_SLICE_NUM_INV / exp2(mip_selector.x);
			float2 _uv;
			half4 _col;
			int hitcount;
	
			bool zhit=false;

			return fixed4(1,1,1,1);	

 			for(hitcount=0; hitcount < 5; hitcount++) {

				bool xy_flag=delta.x<delta.y;
				float delta_tmp=xy_flag ? delta.x : delta.y;
				zhit=(delta.z<delta_tmp);
				delta_tmp=zhit ? delta.z : delta_tmp;

				rayLength=(c.w>TRANSPARENCY_ZTEST_VALUE) ? rayLength : (rayLength+delta_tmp);
				float3 _delta=delta_tmp*EyeDirTan;
				rayPos+=_delta;
				//global_uv+=_delta.xy*global_uv_mult;
	
 				if (!zhit) {
 					float3 rayPos_tmp = xy_flag ? rayPos.xyz : rayPos.yxz;
 			
					#ifdef CUSTOM_HASH_FUNCTION
						#define HASH_OFFSET CUSTOM_HASH_FUNCTION
					#else
					float2 htmp=tex2D(_NoiseTexHash, float2(rayPos_tmp.x*0.03+0.001,0)).rg;
						float HASH_OFFSET=(xy_flag ? htmp.x : htmp.y);
					#endif
			
					_uv=rayPos_tmp.yz+float2(HASH_OFFSET,rayPos_tmp.x*PREMULT+hgt);
	 				_col=tex2Dlod(_BladesTex, float4(_uv.x*_BladesTex_TexelSize.x*_BladesTex_TexelSize.w, _uv.y, mip_selector));
					//_col=tex2D(_BladesTex, float4(_uv.x*_BladesTex_TexelSize.x*_BladesTex_TexelSize.w, _uv.y, mip_selector));
	 				_col.a*=saturate( (rayPos_tmp.z*GRASS_SLICE_NUM+hgt)*removeVerticalWrap );

					_col.rgb*=_col.a;
 					#ifdef FADE_PARALLELS
					float fade_parallelsXY=xy_flag ? fade_parallels.x : fade_parallels.y;
 						_col*=fade_parallelsXY;
 					#endif
 					 			
 					#ifdef BOTTOM_COLORING
 						cval=1+(rayPos.z*GRASS_SLICE_NUM)*bottom_cut;
 						cval=saturate(cval);
 						bottom_ao_damp*=lerp(cval, 1, c.w);
 						_col.rgb*=cval;
 					#endif

 					c+=(1-c.w)*_col;
 			
 					delta.xyz=xy_flag ? float3(delta_next.x, delta.yz-delta.x) : float3(delta.x-delta.y, delta_next.y, delta.z-delta.y);
 				}
 				if (zhit || c.w>=TRANSPARENCY_BREAK_VALUE) break;
			}
	
			_uv.y*=GRASS_SLICE_NUM;

			_col=tex2D(_BladesBackTex, _uv);
			#ifdef BOTTOM_COLORING
				bottom_ao_damp*=lerp(cval, 1, saturate(c.w));
				_col.rgb*=cval;
			#endif
			float floor_cw=zhit ? c.w : 1;
			c=(c+(1-c.w)*_col); 			

			half3 blades_desat=dot(half3(0.33,0.33,0.33), c.rgb).xxx;
			c.rgb=lerp(blades_desat, c.rgb, _BladesSaturation);
			c.rgb*=_BladesColor.rgb*2;
	
			c.rgb=lerp(base_col, c.rgb, floor_cw); 
 			o.rgb=lerp(c.rgb, base_col, distanceT);
			o.rgb *= diffuseScatteringFactor;
 	
			o.a=floor_cw;
			#ifdef BOTTOM_COLORING_NOISE_DRIVEN
			o.a+=(0.5+bval)*(1-IN.color.g);
			#endif
			o.a=(rayPos.z>-0.001) ? floor_cw : o.a;

			float sceneDepth;

			// custom LinearEyeDepth() parametrization
			sceneDepth = 1.0 / ( (_VGZBufferParamA * tex2Dproj(_GrassDepthTex,UNITY_PROJ_COORD(IN.screenPos)).r) + _VGZBufferParamB );
			sceneDepth=(sceneDepth>_VGZBufferFarClip)?10000:sceneDepth; // don't clip beyond depth buffer distance
			float grassDepth = IN.worldPosDepth.w + rayLength * TILING_FACTOR;
			grassDepth = IN.worldPosDepth.w > sceneDepth+0.05 ? 0 : grassDepth;
			clip(sceneDepth-grassDepth);

			return o;
	
		}

		 
		ENDCG
		}
	}
	
	Fallback "Diffuse"
	CustomEditor "VolumeGrassMaterialInspector"	
}
