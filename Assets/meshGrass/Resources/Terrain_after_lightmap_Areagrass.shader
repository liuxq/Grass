Shader "Terrain/After LightMap/Areagrass"
{
	Properties
	{
		_MainTex ("草纹理", 2D) = "white" {}
		_NoiseTex ("明暗噪音", 2D) = "black" {}
		_NoiseScale ("明暗噪音粒度", Range(0, 0.1)) = 0.8
		_NoiseTexHash ("Noise for slices hash", 2D) = "black" {}

		_StepOffset ("草偏移", Range(0, 1)) = 0.15

		_pushPos("位置", Vector) = (0,0,0,0)

		_HeightOffset ("高度", Range(0, 1)) = 0

	}

	SubShader
	{
		Tags { "Queue"="Opaque" }
		//LOD 100

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
			fixed _NoiseScale;
			fixed3 _pushPos;
			fixed _HeightOffset;
			
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
				else
				{
					wpos.y -= _HeightOffset;
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
				fixed2 htmp=tex2D(_NoiseTexHash, fixed2(float2(((i.wpos_dir.w > 0) ? i.wpos_dir.x : i.wpos_dir.z) * 0.03,0))).rg;

				fixed4 col;
				float2 realUv;

				realUv = fixed2(i.uv.x + htmp.y, i.uv.y * .25 + floor(htmp.x * 3.999) * .25);
				//realUv = fixed2(i.uv.x, i.uv.y * .25 );
				col = tex2D(_MainTex, realUv);

				if(col.a < 0.5)
				{
					discard;
				}

				fixed bottomScale = tex2D(_NoiseTex, i.wpos_dir.xz * _NoiseScale).b * 0.3 + 0.7;

				UNITY_APPLY_FOG(i.fogCoord, col);

				#ifndef LIGHTMAP_OFF
				//fixed3 lm = DecodeLightmap(UNITY_SAMPLE_TEX2D(unity_Lightmap, i.lmap));
				//col.rgb *= lm;
				#endif

				return col * bottomScale;
			}
			ENDCG
		}
	}
}
