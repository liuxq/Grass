Shader "Terrain/Before LightMap/4Textures" {
	Properties {
		_Splat0 ("Layer 1", 2D) = "white" {}
		_Splat1 ("Layer 2", 2D) = "white" {}
		_Splat2 ("Layer 3", 2D) = "white" {}
		_Splat3 ("Layer 4", 2D) = "white" {}
		_Control ("Control (RGBA)", 2D) = "white" {}
	}

	SubShader {
		Tags { "RenderType"="Opaque" "SplatCount" = "4"}
		
	Pass {
		Tags { "LightMode" = "ForwardBase" }

		CGPROGRAM

		#pragma vertex vert_surf
		#pragma fragment frag_surf
		#pragma exclude_renderers xbox360 ps3
		#pragma multi_compile_fog
		#pragma multi_compile_fwdbase
		#include "HLSLSupport.cginc"
		#include "UnityShaderVariables.cginc"

		#define UNITY_PASS_FORWARDBASE
		#include "UnityCG.cginc"
		#include "Lighting.cginc"
		#include "AutoLight.cginc"

		sampler2D _Control;
		sampler2D _Splat0,_Splat1,_Splat2,_Splat3;
		float _globalDarkFactor;

		struct Input {
			float2 uv_Control : TEXCOORD0;
			float2 uv_Splat0 : TEXCOORD1;
			float2 uv_Splat1 : TEXCOORD2;
			float2 uv_Splat2 : TEXCOORD3;
			float2 uv_Splat3 : TEXCOORD4; 
		};

		void surf (Input IN, inout SurfaceOutput o) {
			fixed4 splat_control = tex2D (_Control, IN.uv_Control).rgba;
		
			fixed3 lay1 = tex2D(_Splat0, IN.uv_Splat0);
			fixed3 lay2 = tex2D(_Splat1, IN.uv_Splat1);
			fixed3 lay3 = tex2D(_Splat2, IN.uv_Splat2);
			fixed3 lay4 = tex2D(_Splat3, IN.uv_Splat3);

			o.Alpha = 0.0;
			o.Albedo.rgb = (lay1 * splat_control.r + lay2 * splat_control.g + lay3 * splat_control.b + lay4 * splat_control.a);
		}
		
		// vertex-to-fragment interpolation data
		// no lightmaps:
		#ifdef LIGHTMAP_OFF
		struct v2f_surf {
		  float4 pos : SV_POSITION;
		  float4 pack0 : TEXCOORD0; // _Control _Splat0
		  float4 pack1 : TEXCOORD1; // _Splat1 _Splat2
		  float2 pack2 : TEXCOORD2; // _Splat3
		  half3 worldNormal : TEXCOORD3;
		  float3 worldPos : TEXCOORD4;
		  #if UNITY_SHOULD_SAMPLE_SH
		  half3 sh : TEXCOORD5; // SH
		  #endif
		  SHADOW_COORDS(6)
		  UNITY_FOG_COORDS(7)
		  //#if SHADER_TARGET >= 30
		  //float4 lmap : TEXCOORD8;
		  //#endif
		};
		#endif
		// with lightmaps:
		#ifndef LIGHTMAP_OFF
		struct v2f_surf {
		  float4 pos : SV_POSITION;
		  float4 pack0 : TEXCOORD0; // _Control _Splat0
		  float4 pack1 : TEXCOORD1; // _Splat1 _Splat2
		  float2 pack2 : TEXCOORD2; // _Splat3
		  half3 worldNormal : TEXCOORD3;
		  float3 worldPos : TEXCOORD4;
		  float4 lmap : TEXCOORD5;
		  SHADOW_COORDS(6)
		  UNITY_FOG_COORDS(7)
		  #ifdef DIRLIGHTMAP_COMBINED
		  fixed3 tSpace0 : TEXCOORD8;
		  fixed3 tSpace1 : TEXCOORD9;
		  fixed3 tSpace2 : TEXCOORD10;
		  #endif
		};
		#endif

		float4 _Control_ST;
		float4 _Splat0_ST;
		float4 _Splat1_ST;
		float4 _Splat2_ST;
		float4 _Splat3_ST;

		// vertex shader
		v2f_surf vert_surf (appdata_full v) {
			v2f_surf o;
			UNITY_INITIALIZE_OUTPUT(v2f_surf,o);
			o.pos = UnityObjectToClipPos(v.vertex);
			o.pack0.xy = TRANSFORM_TEX(v.texcoord, _Control);
			o.pack0.zw = TRANSFORM_TEX(v.texcoord, _Splat0);
			o.pack1.xy = TRANSFORM_TEX(v.texcoord, _Splat1);
			o.pack1.zw = TRANSFORM_TEX(v.texcoord, _Splat2);
			o.pack2.xy = TRANSFORM_TEX(v.texcoord, _Splat3);
			float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
			fixed3 worldNormal = fixed3(0,1,0);//UnityObjectToWorldNormal(v.normal);
			#if !defined(LIGHTMAP_OFF) && defined(DIRLIGHTMAP_COMBINED)
			fixed3 worldTangent = UnityObjectToWorldDir(v.tangent.xyz);
			fixed tangentSign = v.tangent.w * unity_WorldTransformParams.w;
			fixed3 worldBinormal = cross(worldNormal, worldTangent) * tangentSign;
			#endif
			#if !defined(LIGHTMAP_OFF) && defined(DIRLIGHTMAP_COMBINED)
			o.tSpace0 = float4(worldTangent.x, worldBinormal.x, worldNormal.x, worldPos.x);
			o.tSpace1 = float4(worldTangent.y, worldBinormal.y, worldNormal.y, worldPos.y);
			o.tSpace2 = float4(worldTangent.z, worldBinormal.z, worldNormal.z, worldPos.z);
			#endif
			o.worldPos = worldPos;
			o.worldNormal = worldNormal;
			#ifndef DYNAMICLIGHTMAP_OFF
			//o.lmap.zw = v.texcoord2.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
			#endif
			#ifndef LIGHTMAP_OFF
			o.lmap.xy = v.texcoord1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
			#endif

			// SH/ambient and vertex lights
			#ifdef LIGHTMAP_OFF
			#if UNITY_SHOULD_SAMPLE_SH
				o.sh = 0;
				// Approximated illumination from non-important point lights
				#ifdef VERTEXLIGHT_ON
				o.sh += Shade4PointLights (
					unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
					unity_LightColor[0].rgb, unity_LightColor[1].rgb, unity_LightColor[2].rgb, unity_LightColor[3].rgb,
					unity_4LightAtten0, worldPos, worldNormal);
				#endif
				o.sh = ShadeSHPerVertex (worldNormal, o.sh);
			#endif
			#endif // LIGHTMAP_OFF

			TRANSFER_SHADOW(o); // pass shadow coordinates to pixel shader
			UNITY_TRANSFER_FOG(o,o.pos); // pass fog coordinates to pixel shader
			return o;
		}

		// fragment shader
		fixed4 frag_surf (v2f_surf IN) : SV_Target {
			// prepare and unpack data
			Input surfIN;
			UNITY_INITIALIZE_OUTPUT(Input,surfIN);
			surfIN.uv_Control = IN.pack0.xy;
			surfIN.uv_Splat0 = IN.pack0.zw;
			surfIN.uv_Splat1 = IN.pack1.xy;
			surfIN.uv_Splat2 = IN.pack1.zw;
			surfIN.uv_Splat3 = IN.pack2.xy;
			float3 worldPos = IN.worldPos;
			#ifndef USING_DIRECTIONAL_LIGHT
			fixed3 lightDir = normalize(UnityWorldSpaceLightDir(worldPos));
			#else
			fixed3 lightDir = _WorldSpaceLightPos0.xyz;
			#endif
			#ifdef UNITY_COMPILER_HLSL
			SurfaceOutput o = (SurfaceOutput)0;
			#else
			SurfaceOutput o;
			#endif
			o.Albedo = 0.0;
			o.Emission = 0.0;
			o.Specular = 0.0;
			o.Alpha = 0.0;
			o.Gloss = 0.0;
			fixed3 normalWorldVertex = fixed3(0,0,1);
			o.Normal = IN.worldNormal;
			normalWorldVertex = IN.worldNormal;

			// call surface function
			surf (surfIN, o);

			// compute lighting & shadowing factor
			UNITY_LIGHT_ATTENUATION(atten, IN, worldPos)
			fixed4 c = 0;

			// Setup lighting environment
			UnityGI gi;
			UNITY_INITIALIZE_OUTPUT(UnityGI, gi);
			gi.indirect.diffuse = 0;
			gi.indirect.specular = 0;
			#if !defined(LIGHTMAP_ON)
				gi.light.color = _LightColor0.rgb;
				gi.light.dir = lightDir;
				gi.light.ndotl = LambertTerm (o.Normal, gi.light.dir);
			#endif
			// Call GI (lightmaps/SH/reflections) lighting function
			UnityGIInput giInput;
			UNITY_INITIALIZE_OUTPUT(UnityGIInput, giInput);
			giInput.light = gi.light;
			giInput.worldPos = worldPos;
			giInput.atten = atten;
			#if defined(LIGHTMAP_ON) || defined(DYNAMICLIGHTMAP_ON)
			//giInput.lightmapUV = IN.lmap;
			#else
			giInput.lightmapUV = 0.0;
			#endif
			#if UNITY_SHOULD_SAMPLE_SH
			giInput.ambient = IN.sh;
			#else
			giInput.ambient.rgb = 0.0;
			#endif
			giInput.probeHDR[0] = unity_SpecCube0_HDR;
			giInput.probeHDR[1] = unity_SpecCube1_HDR;
			#if UNITY_SPECCUBE_BLENDING || UNITY_SPECCUBE_BOX_PROJECTION
			giInput.boxMin[0] = unity_SpecCube0_BoxMin; // .w holds lerp value for blending
			#endif
			#if UNITY_SPECCUBE_BOX_PROJECTION
			giInput.boxMax[0] = unity_SpecCube0_BoxMax;
			giInput.probePosition[0] = unity_SpecCube0_ProbePosition;
			giInput.boxMax[1] = unity_SpecCube1_BoxMax;
			giInput.boxMin[1] = unity_SpecCube1_BoxMin;
			giInput.probePosition[1] = unity_SpecCube1_ProbePosition;
			#endif
			LightingLambert_GI(o, giInput, gi);

			// realtime lighting: call lighting function
			c += LightingLambert (o, gi);
			UNITY_APPLY_FOG(IN.fogCoord, c); // apply fog
			UNITY_OPAQUE_ALPHA(c.a);
			return c;
		}

		ENDCG

	}

	// ---- forward rendering additive lights pass:
	Pass {
		Tags { "LightMode" = "ForwardAdd" }
		ZWrite Off Blend One One

		CGPROGRAM
		// compile directives
		#pragma vertex vert_surf
		#pragma fragment frag_surf
		#pragma exclude_renderers xbox360 ps3
		#pragma multi_compile_fog
		#pragma multi_compile_fwdadd
		#include "HLSLSupport.cginc"
		#include "UnityShaderVariables.cginc"

		#define UNITY_PASS_FORWARDADD
		#include "UnityCG.cginc"
		#include "Lighting.cginc"
		#include "AutoLight.cginc"

		//#pragma surface surf Lambert
		//#pragma exclude_renderers xbox360 ps3

		sampler2D _Control;
		sampler2D _Splat0,_Splat1,_Splat2,_Splat3;
		float _globalDarkFactor;

		struct Input {
			float2 uv_Control : TEXCOORD0;
			float2 uv_Splat0 : TEXCOORD1;
			float2 uv_Splat1 : TEXCOORD2;
			float2 uv_Splat2 : TEXCOORD3;
			float2 uv_Splat3 : TEXCOORD4;
		};

		void surf (Input IN, inout SurfaceOutput o) {
			fixed4 splat_control = tex2D (_Control, IN.uv_Control).rgba;
		
			fixed3 lay1 = tex2D(_Splat0, IN.uv_Splat0);
			fixed3 lay2 = tex2D(_Splat1, IN.uv_Splat1);
			fixed3 lay3 = tex2D(_Splat2, IN.uv_Splat2);
			fixed3 lay4 = tex2D(_Splat3, IN.uv_Splat3);

			o.Alpha = 0.0;
			o.Albedo.rgb = (lay1 * splat_control.r + lay2 * splat_control.g + lay3 * splat_control.b + lay4 * splat_control.a);
		}
		

		// vertex-to-fragment interpolation data
		struct v2f_surf {
		  float4 pos : SV_POSITION;
		  float4 pack0 : TEXCOORD0; // _Control _Splat0
		  float4 pack1 : TEXCOORD1; // _Splat1 _Splat2
		  float4 pack2 : TEXCOORD2; // _Splat1 _Splat2
		  half3 worldNormal : TEXCOORD3;
		  float3 worldPos : TEXCOORD4;
		  SHADOW_COORDS(5)
		  UNITY_FOG_COORDS(6)
		};
		float4 _Control_ST;
		float4 _Splat0_ST;
		float4 _Splat1_ST;
		float4 _Splat2_ST;
		float4 _Splat3_ST;

		// vertex shader
		v2f_surf vert_surf (appdata_full v) {
		  v2f_surf o;
		  UNITY_INITIALIZE_OUTPUT(v2f_surf,o);
		  o.pos = UnityObjectToClipPos(v.vertex);
		  o.pack0.xy = TRANSFORM_TEX(v.texcoord, _Control);
		  o.pack0.zw = TRANSFORM_TEX(v.texcoord, _Splat0);
		  o.pack1.xy = TRANSFORM_TEX(v.texcoord, _Splat1);
		  o.pack1.zw = TRANSFORM_TEX(v.texcoord, _Splat2);
		  o.pack2.xy = TRANSFORM_TEX(v.texcoord, _Splat3);
		  float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
		  fixed3 worldNormal = fixed3(0,1,0);//UnityObjectToWorldNormal(v.normal);
		  o.worldPos = worldPos;
		  o.worldNormal = worldNormal;

		  TRANSFER_SHADOW(o); // pass shadow coordinates to pixel shader
		  UNITY_TRANSFER_FOG(o,o.pos); // pass fog coordinates to pixel shader
		  return o;
		}

		// fragment shader
		fixed4 frag_surf (v2f_surf IN) : SV_Target {
		  // prepare and unpack data
		  Input surfIN;
		  UNITY_INITIALIZE_OUTPUT(Input,surfIN);
		  surfIN.uv_Control.x = 1.0;
		  surfIN.uv_Splat0.x = 1.0;
		  surfIN.uv_Splat1.x = 1.0;
		  surfIN.uv_Splat2.x = 1.0;
		  surfIN.uv_Splat3.x = 1.0;
		  surfIN.uv_Control = IN.pack0.xy;
		  surfIN.uv_Splat0 = IN.pack0.zw;
		  surfIN.uv_Splat1 = IN.pack1.xy;
		  surfIN.uv_Splat2 = IN.pack1.zw;
		  surfIN.uv_Splat3 = IN.pack2.xy;
		  float3 worldPos = IN.worldPos;
		  #ifndef USING_DIRECTIONAL_LIGHT
			fixed3 lightDir = normalize(UnityWorldSpaceLightDir(worldPos));
		  #else
			fixed3 lightDir = _WorldSpaceLightPos0.xyz;
		  #endif
		  #ifdef UNITY_COMPILER_HLSL
		  SurfaceOutput o = (SurfaceOutput)0;
		  #else
		  SurfaceOutput o;
		  #endif
		  o.Albedo = 0.0;
		  o.Emission = 0.0;
		  o.Specular = 0.0;
		  o.Alpha = 0.0;
		  o.Gloss = 0.0;
		  fixed3 normalWorldVertex = fixed3(0,0,1);
		  o.Normal = IN.worldNormal;
		  normalWorldVertex = IN.worldNormal;

		  // call surface function
		  surf (surfIN, o);
		  UNITY_LIGHT_ATTENUATION(atten, IN, worldPos)
		  fixed4 c = 0;

		  // Setup lighting environment
		  UnityGI gi;
		  UNITY_INITIALIZE_OUTPUT(UnityGI, gi);
		  gi.indirect.diffuse = 0;
		  gi.indirect.specular = 0;
		  #if !defined(LIGHTMAP_ON)
			  gi.light.color = _LightColor0.rgb;
			  gi.light.dir = lightDir;
			  gi.light.ndotl = LambertTerm (o.Normal, gi.light.dir);
		  #endif
		  gi.light.color *= atten;
		  c += LightingLambert (o, gi);
		  c.a = 0.0;
		  UNITY_APPLY_FOG(IN.fogCoord, c); // apply fog
		  UNITY_OPAQUE_ALPHA(c.a);
		  return c;
		}

		ENDCG

		}

	} 
	FallBack "Diffuse"
}
