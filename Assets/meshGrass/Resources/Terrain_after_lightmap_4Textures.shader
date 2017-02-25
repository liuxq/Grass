// Upgrade NOTE: commented out 'float4 unity_LightmapST', a built-in variable

Shader "Terrain/After LightMap/4Textures" {
Properties {
	_Splat0 ("Layer 1", 2D) = "white" {}
	_Splat1 ("Layer 2", 2D) = "white" {}
	_Splat2 ("Layer 3", 2D) = "white" {}
	_Splat3 ("Layer 4", 2D) = "white" {}
	_Control ("Control (RGBA)", 2D) = "white" {}
}
                
SubShader {
	Tags { "Queue" = "Geometry" "RenderType"="Opaque"}
	
	Lod 200
	lighting Off

	pass{
        CGPROGRAM
        #pragma vertex vert
        #pragma fragment frag
        #include "UnityCG.cginc"

		sampler2D _Splat0;
		float4 _Splat0_ST;
		sampler2D _Splat1;
		float4 _Splat1_ST;
		sampler2D _Splat2;
		float4 _Splat2_ST;
		sampler2D _Splat3;
		float4 _Splat3_ST;
		sampler2D _Control;
		float4 _Control_ST;
		float _globalDarkFactor;

		struct v2f {
            float4 pos : SV_POSITION;
            float2 uv_Control : TEXCOORD0;
			float2 uv_Splat0 : TEXCOORD1;
			float2 uv_Splat1 : TEXCOORD2;
			float2 uv_Splat2 : TEXCOORD3;
			float2 uv_Splat3 : TEXCOORD4;
			#ifndef LIGHTMAP_OFF
            float2 lmap : TEXCOORD5;
            #endif
        };

		v2f vert (appdata_full v) 
        {
            v2f o;
            o.pos = mul (UNITY_MATRIX_MVP, v.vertex);
            o.uv_Splat0 = TRANSFORM_TEX(v.texcoord.xy,_Splat0);
            o.uv_Splat1 = TRANSFORM_TEX(v.texcoord.xy,_Splat1);
			o.uv_Splat2 = TRANSFORM_TEX(v.texcoord.xy,_Splat2);
			o.uv_Splat3 = TRANSFORM_TEX(v.texcoord.xy,_Splat3);
			o.uv_Control = TRANSFORM_TEX(v.texcoord.xy,_Control);
			#ifndef LIGHTMAP_OFF
            o.lmap = v.texcoord1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
            #endif
            return o;
        }

		fixed4 frag(v2f i) : color
        {
			half4 col;
			fixed4 splat_control = tex2D(_Control, i.uv_Control).rgba;
			
			col.rgb = tex2D(_Splat0, i.uv_Splat0) * splat_control.r;
			col.rgb += tex2D(_Splat1, i.uv_Splat1) * splat_control.g;
			col.rgb += tex2D(_Splat2, i.uv_Splat2) * splat_control.b;
			col.rgb += tex2D(_Splat3, i.uv_Splat3) * splat_control.a;

			#ifndef LIGHTMAP_OFF
            fixed3 lm = DecodeLightmap(UNITY_SAMPLE_TEX2D(unity_Lightmap, i.lmap));
            col.rgb *= lm;
            #endif

			return fixed4(lm,1);

			col.a = 0;
			col.rgb = lerp(col,fixed4(0,0,0,0),_globalDarkFactor);
			return col;
		}
		ENDCG 
		}
	}
	Fallback "Diffuse"
}
