Shader "Terrain/Before LightMap/Areagrass"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_NoiseTex ("Noise tex", 2D) = "black" {}
		_NoiseTexHash ("Noise for slices hash", 2D) = "black" {}

		_StepOffset ("Step Offset", Range(0, 0.5)) = 0.15

		_wind_dir ("Constant wind bend (xy to world xz)", Vector) =(0,0,0,0)
		_wind_amp ("amplitude", Range(0, 0.2)) = 0.1
		_wind_freq ("noise frequency", Float) = 0
		_wind_speed ("noise offset anim speed", Float) = 0.1

		_pushPos("pushPos and width", Vector) = (0,0,0,0)
	}

	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			Tags { "LightMode" = "ForwardBase" }
			Cull Off
			
			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog
			#pragma multi_compile_fwdbase
			
			#include "HLSLSupport.cginc"
			#include "UnityShaderVariables.cginc"

			#define UNITY_PASS_FORWARDBASE
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "AutoLight.cginc"

			struct v2f
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
				SHADOW_COORDS(1)
				UNITY_FOG_COORDS(2)
				float4 wpos_dir: TEXCOORD3;
				half3 worldNormal : TEXCOORD4;
				float3 worldPos : TEXCOORD5;

				#if UNITY_SHOULD_SAMPLE_SH
				half3 sh : TEXCOORD6; // SH
				#endif
			};

			sampler2D _MainTex;
			sampler2D _NoiseTex;
			sampler2D _NoiseTexHash;

			fixed _StepOffset;

			float _wind_amp;
			float2 _wind_dir;
			float _wind_speed;
			float _wind_freq;

			float4 _pushPos;
			
			v2f vert (appdata_full v)
			{
				v2f o;
				float4 wpos = mul(UNITY_MATRIX_M, v.vertex);
				o.wpos_dir.xyz = wpos.xyz;
				o.wpos_dir.w = v.texcoord2.x;

				float2 offset = float2(0,0);
				
				if(v.texcoord2.y == 0)
				{
					if(v.texcoord2.x > 0)//x方向
					{
						float2 delta = _WorldSpaceCameraPos.xy - wpos.xy;
						float tan = abs(delta.y / delta.x);
						offset.x = _StepOffset;
						offset.x *= (delta.x > 0 ? -1 : 1);
					}
					else//z方向
					{
						float2 delta = _WorldSpaceCameraPos.yz - wpos.yz;
						float tan = abs(delta.x / delta.y);
						offset.y = _StepOffset;
						offset.y *= (delta.y > 0 ? -1 : 1);
					}
					wpos.x -= offset.x;
					wpos.z -= offset.y;
				}
				
				o.pos = mul(UNITY_MATRIX_VP, wpos);
				o.uv = v.texcoord;


				float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				fixed3 worldNormal = UnityObjectToWorldNormal(fixed3(0,1,0));
				o.worldPos = worldPos;
				o.worldNormal = worldNormal;

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

				TRANSFER_SHADOW(o);
				UNITY_TRANSFER_FOG(o,o.pos);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				float _wind_dir_magnitude=length(_wind_dir);
				float2 wind_dir=(_wind_dir_magnitude==0) ? float2(1,1) : -_wind_dir/_wind_dir_magnitude;
				float2 wind_coords=i.wpos_dir.xz*_wind_freq+_Time.xx*_wind_speed*wind_dir;
				float3 wtmp=tex2D(_NoiseTex, wind_coords.xy);
				float2 wind_offset_main=float2(wtmp.x, wtmp.y)-0.5;
				wind_offset_main-=_wind_dir;
				wind_offset_main*=_wind_amp;

				// 推动草
				float3 dis = i.wpos_dir.xyz - _pushPos.xyz;
				float3 absdis = abs(dis);
				float disScale = max(max(absdis.x,absdis.y),absdis.z);
				bool pushFlag = false;
				if(disScale < _pushPos.w)
				{
					dis = dis/(dis.x * dis.x + dis.z * dis.z);
					wind_offset_main -= dis.xz * 1;
					pushFlag = true;
				}

				wind_offset_main *= frac(i.uv.y);


				bool xflag = (i.wpos_dir.w > 0);
				
				float2 htmp=tex2D(_NoiseTexHash, float2((xflag ? i.wpos_dir.x : i.wpos_dir.z) * 0.03,0)).rg;

				float x = floor(htmp.x * 3.999);

				fixed4 col;
				float2 realUv;
				if(xflag)
				{
					realUv = float2(i.uv.x + htmp.y, i.uv.y * .25 + x/4);
					realUv.x += wind_offset_main.y;
					if(pushFlag)
					{
						realUv.y += abs(wind_offset_main.y);
						if(realUv.y > x/4 + 0.25 || realUv.y < x/4)
							discard;
					}
					
					col = tex2D(_MainTex, realUv);
				}
				else
				{
					realUv = float2(i.uv.x + htmp.y, i.uv.y * .25 + x/4);
					realUv.x += wind_offset_main.x;
					if(pushFlag)
					{
						realUv.y += abs(wind_offset_main.x);
						if(realUv.y > x/4 + 0.25 || realUv.y < x/4)
							discard;
					}

					col = tex2D(_MainTex, realUv);
				}

				clip(col.a - 0.5);

				float bottomScale = tex2D(_NoiseTex, i.wpos_dir.xz * 0.1).b * 0.3 + 0.7;

				//UNITY_APPLY_FOG(i.fogCoord, col);
				//return col * bottomScale;

				//------------------light
				float3 worldPos = i.worldPos;
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
				o.Albedo = col * bottomScale;
				o.Emission = 0.0;
				o.Specular = 0.0;
				o.Alpha = 0.0;
				o.Gloss = 0.0;

				fixed3 normalWorldVertex = fixed3(0,0,1);
				o.Normal = i.worldNormal;
				normalWorldVertex = i.worldNormal;
				// compute lighting & shadowing factor
				UNITY_LIGHT_ATTENUATION(atten, i, worldPos)

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
				//giInput.lightmapUV = i.lmap;
				#else
				giInput.lightmapUV = 0.0;
				#endif
				#if UNITY_SHOULD_SAMPLE_SH
				giInput.ambient = i.sh;
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

				UNITY_APPLY_FOG(i.fogCoord, c); // apply fog
				UNITY_OPAQUE_ALPHA(c.a);
				return c;
			}
			ENDCG
		}

		Pass
		{
			Tags { "LightMode" = "ForwardAdd" }
			ZWrite Off Blend One One
			
			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog
			#pragma multi_compile_fwdadd
			
			#include "HLSLSupport.cginc"
			#include "UnityShaderVariables.cginc"

			#define UNITY_PASS_FORWARDADD
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "AutoLight.cginc"

			struct v2f
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
				SHADOW_COORDS(1)
				UNITY_FOG_COORDS(2)
				float4 wpos_dir: TEXCOORD3;
				half3 worldNormal : TEXCOORD4;
				float3 worldPos : TEXCOORD5;
			};

			sampler2D _MainTex;
			sampler2D _NoiseTex;
			sampler2D _NoiseTexHash;

			fixed _StepOffset;

			float _wind_amp;
			float2 _wind_dir;
			float _wind_speed;
			float _wind_freq;

			float4 _pushPos;
			
			v2f vert (appdata_full v)
			{
				v2f o;
				float4 wpos = mul(UNITY_MATRIX_M, v.vertex);
				o.wpos_dir.xyz = wpos.xyz;
				o.wpos_dir.w = v.texcoord.x;

				float2 offset = float2(0,0);
				
				if(v.texcoord.y == 0)
				{
					if(o.wpos_dir.w > 0)//x方向
					{
						float2 delta = _WorldSpaceCameraPos.xy - wpos.xy;
						float tan = abs(delta.y / delta.x);
						offset.x = _StepOffset;
						offset.x *= (delta.x > 0 ? -1 : 1);
					}
					else//z方向
					{
						float2 delta = _WorldSpaceCameraPos.yz - wpos.yz;
						float tan = abs(delta.x / delta.y);
						offset.y = _StepOffset;
						offset.y *= (delta.y > 0 ? -1 : 1);
					}
					wpos.x -= offset.x;
					wpos.z -= offset.y;
				}
				
				o.pos = mul(UNITY_MATRIX_VP, wpos);
				o.uv = v.texcoord;

				float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				fixed3 worldNormal = UnityObjectToWorldNormal(fixed3(0,1,0));
				o.worldPos = worldPos;
				o.worldNormal = worldNormal;

				TRANSFER_SHADOW(o);
				UNITY_TRANSFER_FOG(o,o.pos);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				float _wind_dir_magnitude=length(_wind_dir);
				float2 wind_dir=(_wind_dir_magnitude==0) ? float2(1,1) : -_wind_dir/_wind_dir_magnitude;
				float2 wind_coords=i.wpos_dir.xz*_wind_freq+_Time.xx*_wind_speed*wind_dir;
				float3 wtmp=tex2D(_NoiseTex, wind_coords.xy);
				float2 wind_offset_main=float2(wtmp.x, wtmp.y)-0.5;
				wind_offset_main-=_wind_dir;
				wind_offset_main*=_wind_amp;

				// 推动草
				float3 dis = i.wpos_dir.xyz - _pushPos.xyz;
				float3 absdis = abs(dis);
				float disScale = max(max(absdis.x,absdis.y),absdis.z);
				bool pushFlag = false;
				if(disScale < _pushPos.w)
				{
					dis = dis/(dis.x * dis.x + dis.z * dis.z);
					wind_offset_main -= dis.xz * 1;
					pushFlag = true;
				}

				wind_offset_main *= frac(i.uv.y);


				bool xflag = (i.wpos_dir.w > 0);
				
				float2 htmp=tex2D(_NoiseTexHash, float2((xflag ? i.wpos_dir.x : i.wpos_dir.z) * 0.03,0)).rg;

				float x = floor(htmp.x * 3.999);

				fixed4 col;
				float2 realUv;
				if(xflag)
				{
					realUv = float2(i.uv.x + htmp.y, i.uv.y * .25 + x/4);
					realUv.x += wind_offset_main.y;
					if(pushFlag)
					{
						realUv.y += abs(wind_offset_main.y);
						if(realUv.y > x/4 + 0.25 || realUv.y < x/4)
							discard;
					}
					
					col = tex2D(_MainTex, realUv);
				}
				else
				{
					realUv = float2(i.uv.x + htmp.y, i.uv.y * .25 + x/4);
					realUv.x += wind_offset_main.x;
					if(pushFlag)
					{
						realUv.y += abs(wind_offset_main.x);
						if(realUv.y > x/4 + 0.25 || realUv.y < x/4)
							discard;
					}

					col = tex2D(_MainTex, realUv);
				}

				clip(col.a - 0.5);

				float bottomScale = tex2D(_NoiseTex, i.wpos_dir.xz * 0.1).b * 0.3 + 0.7;

				UNITY_APPLY_FOG(i.fogCoord, col);
				//return col * bottomScale;

				//------------------light
				float3 worldPos = i.worldPos;
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
				o.Albedo = col * bottomScale;
				o.Emission = 0.0;
				o.Specular = 0.0;
				o.Alpha = 0.0;
				o.Gloss = 0.0;

				o.Normal = i.worldNormal;
				// compute lighting & shadowing factor
				UNITY_LIGHT_ATTENUATION(atten, i, worldPos)

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
