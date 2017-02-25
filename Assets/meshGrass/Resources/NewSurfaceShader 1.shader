Shader "Terrain/NewSurfaceShader 1" {
	Properties {
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Cutoff ("Alpha cutoff", Range(0,1)) = 0.5
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Lambert vertex:vert alphatest:_Cutoff
		 
		//#pragma target 3.0

		sampler2D _MainTex;

		struct Input {
			float2 uv_MainTex;
			float4 wpos_dir;
		};

		void vert (inout appdata_full v, out Input o)
		{
			UNITY_INITIALIZE_OUTPUT(Input, o);
			float4 wpos = mul(UNITY_MATRIX_M, v.vertex);
			o.wpos_dir.xyz = wpos.xyz;
			o.wpos_dir.w = v.normal.x;

			float2 offset = float2(0,0);
				
			if(v.normal.y == 0)
			{
				if(v.normal.x > 0)//x方向
				{
					float2 delta = _WorldSpaceCameraPos.xy - wpos.xy;
					float tan = abs(delta.y / delta.x);
					offset.x = 0.15;
					offset.x *= (delta.x > 0 ? -1 : 1);
				}
				else//z方向
				{
					float2 delta = _WorldSpaceCameraPos.yz - wpos.yz;
					float tan = abs(delta.x / delta.y);
					offset.y = 0.15;
					offset.y *= (delta.y > 0 ? -1 : 1);
				}
				wpos.x -= offset.x;
				wpos.z -= offset.y;
			}
				
			v.vertex = mul(UNITY_MATRIX_VP, wpos);
		}

		void surf (Input IN, inout SurfaceOutput o) {
			fixed4 c = tex2D (_MainTex, IN.uv_MainTex);
			o.Albedo = fixed3(1,1,1);//c.rgb;

			o.Alpha = c.a;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
