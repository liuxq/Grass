Shader "Custom/RainTerrain"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_NormalTex ("NormalTex", 2D) = "white" {}
		_lightDir("lightDir", Vector) = (0,-1,0)
		_Shininess("Shininess", Float) = 1
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
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
				fixed3 viewDir : TEXCOORD2;  
			};

			sampler2D _MainTex;
			sampler2D _NormalTex;
			float4 _MainTex_ST;

			fixed3 _lightDir;
			fixed _Shininess;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.viewDir = normalize(WorldSpaceViewDir(v.vertex));  

				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				fixed4 col = tex2D(_MainTex, i.uv);

				fixed3 vec = tex2D(_NormalTex, i.uv * 5 + _Time.x).rgb;
				fixed3 normal = tex2D(_NormalTex, i.uv * 0.5 + vec.xz).rgb;

				fixed nh = saturate(dot(normal, normalize(i.viewDir + _lightDir)));
                fixed3 spec = pow(nh, _Shininess);
                col.rgb += spec + dot(normal,_lightDir);
				
				// just copy
				//fixed2 uv = fragCoord.xy / iResolution.xy;
				//fragColor = texture(iChannel0, uv);

				//col.rgb += normal;
				// apply fog
				UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
			}
			ENDCG
		}
	}
}
