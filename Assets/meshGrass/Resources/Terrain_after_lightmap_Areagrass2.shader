Shader "Terrain/After LightMap/Areagrass2"
{
	Properties
	{
		_MainTex ("草纹理", 2D) = "white" {}
		_NoiseTex ("明暗噪音", 2D) = "black" {}
		_NoiseScale ("明暗噪音粒度", Range(0, 0.1)) = 0.8
		_NoiseTexHash ("Noise for slices hash", 2D) = "black" {}

		_StepOffset ("草偏移", Range(0, 0.5)) = 0.15

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
			Cull Off
			
			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog
			
			#include "UnityCG.cginc"

			struct v2f
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
				UNITY_FOG_COORDS(1)
				float4 wpos_dir: TEXCOORD2;
				#ifndef LIGHTMAP_OFF
				float2 lmap : TEXCOORD3;
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

			fixed _NoiseScale;
			
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
						offset.x = _StepOffset * (_WorldSpaceCameraPos.x - wpos.x > 0 ? -1 : 1);
					}
					else//z方向
					{
						offset.y = _StepOffset * (_WorldSpaceCameraPos.z - wpos.z > 0 ? -1 : 1);
					}
					wpos.x -= offset.x;
					wpos.z -= offset.y;
				}
				
				o.pos = mul(UNITY_MATRIX_VP, wpos);
				o.uv = v.texcoord1;
				UNITY_TRANSFER_FOG(o,o.pos);

				#ifndef LIGHTMAP_OFF
				o.lmap = v.texcoord1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
				#endif

				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed diss = distance(i.wpos_dir.xyz, _pushPos.xyz);
				clip( 5 - diss);

				

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

				float bottomScale = tex2D(_NoiseTex, i.wpos_dir.xz * _NoiseScale).b * 0.3 + 0.7;

				UNITY_APPLY_FOG(i.fogCoord, col);

				#ifndef LIGHTMAP_OFF
				fixed3 lm = DecodeLightmap(UNITY_SAMPLE_TEX2D(unity_Lightmap, i.lmap));
				//col.rgb *= lm;
				#endif

				return col * bottomScale;
			}
			ENDCG
		}
	}
}
