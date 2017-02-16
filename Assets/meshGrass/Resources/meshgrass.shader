Shader "Unlit/meshgrass"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_NoiseTex ("Noise tex", 2D) = "black" {}
		_NoiseTexHash ("Noise for slices hash", 2D) = "black" {}
		_height("height", Float) = 0.2

		_vample("_vample", Float) = 0.25

		_wind_dir ("Constant wind bend (xy to world xz)", Vector) =(0,0,0,0)
		_wind_amp ("   amplitude", Float) = 0.22
		_wind_freq ("   noise frequency", Float) = 0
		_wind_speed ("   noise offset anim speed", Float) = 0.1

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

			struct appdata
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				UNITY_FOG_COORDS(1)
				float3 mpos: TEXCOORD2;
				float3 wpos: TEXCOORD3;
				float3 normal: TEXCOORD4;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			sampler2D _NoiseTex;
			sampler2D _NoiseTexHash;
			float4 _MainTex_ST;
			float _vample;
			float _height;

			float _wind_amp;
			float2 _wind_dir;
			float _wind_speed;
			float _wind_freq;

			float4 _pushPos;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.mpos = v.vertex;
				float4 wpos = mul(UNITY_MATRIX_M, v.vertex);
				o.wpos = wpos;
				o.normal = v.normal;

				float2 offset = float2(0,0);
				
				if(v.normal.y == 0)
				{
					if(v.normal.x > 0)//y方向
					{
						float2 delta = _WorldSpaceCameraPos.xy - wpos.xy;
						float tan = abs(delta.y / delta.x);
						offset.x = 0.15;
						offset.x *= (delta.x > 0 ? -1 : 1);
					}
					else//x方向
					{
						float2 delta = _WorldSpaceCameraPos.yz - wpos.yz;
						float tan = abs(delta.x / delta.y);
						offset.y = 0.15;
						offset.y *= (delta.y > 0 ? -1 : 1);
					}
					wpos.x -= offset.x;
					wpos.z -= offset.y;
				}
				
				float4 vpos = mul(UNITY_MATRIX_V, wpos);
				float4 ppos = mul(UNITY_MATRIX_P, vpos);
				o.vertex = ppos;
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				float _wind_dir_magnitude=length(_wind_dir);
				float2 wind_dir=(_wind_dir_magnitude==0) ? float2(1,1) : -_wind_dir/_wind_dir_magnitude;
				float2 wind_coords=i.wpos.xz*_wind_freq+_Time.xx*_wind_speed*wind_dir;
				float3 wtmp=tex2D(_NoiseTex, wind_coords.xy);
				float2 wind_offset_main=float2(wtmp.x, wtmp.y)-0.5;
				wind_offset_main-=_wind_dir;
				wind_offset_main*=_wind_amp;

				// 推动草
				float3 dis = i.wpos.xyz - _pushPos.xyz;
				float3 absdis = abs(dis);
				float disScale = max(max(absdis.x,absdis.y),absdis.z);
				bool pushFlag = false;
				if(disScale < _pushPos.w)
				{
					dis = dis/(dis.x * dis.x + dis.z * dis.z);
					wind_offset_main -= dis.xz * 1;
					pushFlag = true;
				}
				//

				wind_offset_main *= i.mpos.y;


				bool xflag = (i.normal.x > 0);
				
				float2 htmp=tex2D(_NoiseTexHash, float2((xflag ? i.wpos.x : i.wpos.z) * 0.03,0)).rg;

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

				float bottomScale = tex2D(_NoiseTex, i.wpos.xz * 0.1).b * 0.3 + 0.7;

				UNITY_APPLY_FOG(i.fogCoord, col);
				return col * bottomScale;
			}
			ENDCG
		}
	}
}
