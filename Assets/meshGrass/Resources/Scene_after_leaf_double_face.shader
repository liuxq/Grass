Shader "Scene/After lightMap/leaf_double_face" {
Properties {
    _MainTex ("Base (RGB) Alpha (A)", 2D) = "white" {}
    _Cutoff("cut off ",range(0,1)) = 0.1
	_BloomValue("辉光阈值（alpha大于此值时的像素开启辉光）",range(0,1)) = 0.1
	_RudeColor("狂暴叠加颜色", Color) = (0.3,0.3,0.3,1)
}

SubShader {
    Tags {"Queue"="AlphaTest" "IgnoreProjector"="True" "RenderType"="TransparentCutout"}
    LOD 100
    cull back
    Pass {
        CGPROGRAM
        #pragma debug
        #pragma vertex vert
        #pragma fragment frag
        #pragma multi_compile_fog  
		#pragma multi_compile __ RUDE_ON

        #include "UnityCG.cginc"

	    sampler2D _MainTex;
	    half4 _MainTex_ST;
	    float _Cutoff;
		fixed _BloomValue;
	    
	    struct v2f {
	        float4 pos : SV_POSITION;
	        float2 uv : TEXCOORD0;
	        #ifndef LIGHTMAP_OFF
	        float2 lmap : TEXCOORD1;
	        #endif
	        UNITY_FOG_COORDS(2)
	    };

        v2f vert (appdata_full v)
	    {
	        v2f o;
	        o.pos = mul(UNITY_MATRIX_MVP,v.vertex);
	        o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
	        #ifndef LIGHTMAP_OFF
	        o.lmap = v.texcoord1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
	        #endif
	        UNITY_TRANSFER_FOG(o,o.pos);
	        return o;
	    }

                    
        fixed4 frag (v2f i) : COLOR
        {
            fixed4 tex = tex2D (_MainTex, i.uv);
            
            if(tex.a < _Cutoff)
            {
            	discard;
            }

            #ifndef LIGHTMAP_OFF
            fixed3 lm = DecodeLightmap (UNITY_SAMPLE_TEX2D(unity_Lightmap, i.lmap));
            //tex.rgb *= lm;
            #endif


            UNITY_APPLY_FOG(i.fogCoord,tex);
            tex.a = (tex.a > _BloomValue) ? 1.0 : 0.0;
            return tex;
        }
        ENDCG 
    }   
}
}



