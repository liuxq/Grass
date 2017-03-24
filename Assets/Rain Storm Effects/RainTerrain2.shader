Shader "Custom/RainTerrain2"
{
	Properties
	{
		[Toggle]_Rain ("下雨", Float) = 0
		_MainTex("Base", 2D) = "white" {}
		_NormalTex ("NormalTex", 2D) = "white" {}
        _GradTex("Gradient", 2D) = "white" {}
        _Seg("波纹间隔", Float) = 20
		_Radius("波纹半径", Float) = 0.3
		_Speed("波纹速度", Float) = 8
		
        _lightDir("光线方向", Vector) = (0,-1,0)
		_Shininess("高光范围", Float) = 1
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
				float2 uv : TEXCOORD0;
				float2 originUv : TEXCOORD1;
				UNITY_FOG_COORDS(2)
				fixed3 viewDir : TEXCOORD3;  
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			fixed4 _MainTex_ST;

			sampler2D _GradTex;
			sampler2D _NormalTex;

			fixed3 _lightDir;
			fixed _Shininess;
			fixed _Rain;
			fixed _Seg;
			fixed _Radius;
			fixed _Speed;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.originUv = v.uv;
				o.viewDir = normalize(WorldSpaceViewDir(v.vertex));  

				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}
			
			fixed hash(fixed2 p)
			{
				p  = frac( p*0.3183099+.1 );
				p *= 17.0;
				return frac( p.x*p.y*(p.x+p.y) );
			}

			fixed wave(fixed2 position, fixed2 origin)
			{
				fixed h = hash(origin);
				fixed d = length(position - origin);
				fixed t = _Time.x * _Speed - d * 0.8 + h;
				return (d > _Radius)? 0 : (tex2D(_GradTex, fixed2(t, 0)).a - 0.5f) * 4 * (_Radius - d);
			}

			fixed2 allwave(fixed2 position)
			{
				fixed2 intP = floor(position);
				fixed2 center = intP + 0.5f;
				
				//if(fmod((intP.x * intP.y), 3) < 1)
				//	return fixed2(0,0);

				const fixed2 dx = fixed2(0.01f, 0);
				const fixed2 dy = fixed2(0, 0.01f);
				fixed w = wave(position, center);
				return fixed2(wave(position + dx, center) - w, wave(position + dy, center) - w);
			}

			fixed4 frag(v2f i) : SV_Target
			{
				half4 c = tex2D(_MainTex, i.uv);

				if(_Rain == 0)
					return c;

				fixed2 p = i.originUv * _Seg;
				fixed2 duv = allwave(p) * 0.2f;

				fixed3 vec = tex2D(_NormalTex, i.uv * 5).rgb;
				fixed3 normal = tex2D(_NormalTex, i.uv * 0.5 + vec.xz + duv).rgb;

				fixed nh = saturate(dot(normal, normalize(i.viewDir + _lightDir)));
                fixed3 spec = pow(nh, _Shininess);
				c.rgb += spec + dot(normal,_lightDir) - 0.5;

				UNITY_APPLY_FOG(i.fogCoord, col);
				return c;
			}

			ENDCG
		}
	}
}
