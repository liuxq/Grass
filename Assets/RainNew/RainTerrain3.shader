Shader "Custom/RainTerrain3"
{
	Properties
	{
		[Toggle]_Rain ("下雨", Float) = 0
		_MainTex("Base", 2D) = "white" {}
		_Normal("Normal", 2D) = "bump" {}
        _Ripple("Ripple", 2D) = "white" {}
		
        _Seg("波纹间隔", Float) = 20
		_Radius("波纹半径", Range(0.1, 1)) = 0.5
		_Speed("波纹速度", Float) = 1
		_Intensity ("波纹亮度", Range (0.5, 4.0)) = 1.5

		_WaterValue("地面波浪强度", Range(0, 0.005)) = 0.0025
		_DirectionUv("地面波浪方向 (两个方向)", Vector) = (1.0,1.0, -0.2,-0.2)
		_TexAtlasTiling("地面波浪scale", Vector) = (8.0,8.0, 4.0,4.0)	
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		lighting Off

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
				float2 rippleUv : TEXCOORD1;
				UNITY_FOG_COORDS(2)
				fixed4 normalScrollUv : TEXCOORD3;  
			};

			sampler2D _MainTex;
			fixed4 _MainTex_ST;

			sampler2D _Ripple;
			sampler2D _Normal;

			fixed _Rain;
			fixed _Seg;
			fixed _Radius;
			fixed _Speed;
			fixed _Intensity;
			half4 _TexAtlasTiling;
			half4 _DirectionUv;
			fixed _WaterValue;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.rippleUv = v.uv * _Seg;

				o.normalScrollUv.xyzw = v.uv.xyxy * _TexAtlasTiling + _Time.xxxx * _DirectionUv;

				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}
		
			fixed hash(fixed2 p)
			{
				p  = frac( p*0.3183099+.1 );
				p *= 17.0;
				return frac( p.x*p.y*(p.x+p.y) );
			}

			fixed4 ripple(fixed2 position)
			{
				fixed2 intP = floor(position);

				if(fmod((intP.x * intP.y), 3) < 1.5)
					return fixed4(0,0,0,0);

				fixed2 curUv = position - intP;
				curUv = saturate(curUv / _Radius);

				fixed h = hash(intP);

				half timeVal = frac(_Time.y * _Speed + h) * 1.0;
			
				// animation of 6 frames:
				curUv.x = curUv.x / 6  + floor(timeVal * 6) / 6;
				return tex2D(_Ripple, curUv) * saturate(1.0 - timeVal) * _Intensity;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				half3 nrml = UnpackNormal(tex2D(_Normal, i.normalScrollUv.xy));
				nrml += UnpackNormal(tex2D(_Normal, i.normalScrollUv.zw));
				nrml.xy *= _WaterValue;

				half4 c = tex2D(_MainTex, i.uv + nrml.xy * _MainTex_ST.x);

				if(_Rain == 0)
					return c;

				c.rgb += ripple(i.rippleUv).rgb;

				UNITY_APPLY_FOG(i.fogCoord, c);
				return c;
			}

			ENDCG
		}
	}
}
