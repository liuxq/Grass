// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

////////////////////////////////////////////////////////////////////////////
//
// IBL configuration section
// use to sync with skyshop sky rotation
//
//#define SKYSHOP_SKY_ROTATION
// if not defined we will decode LDR cubemaps (RGB only)
#define IBL_HDR_RGBM

// when not defined (commented out) we're in Gamma color space
//#define COLORSPACE_LINEAR
//
//////////////////////////////////////////////////////////////////////////// 

#define TRANSPARENCY_ZTEST_VALUE 0.6
#define TRANSPARENCY_BREAK_VALUE 0.95

//#define CUSTOM_HASH_FUNCTION (frac(fmod(rayPos_tmp.x,39))*fmod(rayPos_tmp.x,39)*9.15)
#define FADE_PARALLELS

#define BOTTOM_COLORING
#define BOTTOM_COLORING_NOISE_DRIVEN

#define NOISE_CHANNEL_WINDX r
#define NOISE_CHANNEL_WINDY g
#define NOISE_CHANNEL_BCOLORING b	

//#define DONT_USE_SPHERICAL_WIND
//#define DONT_USE_MAIN_WIND

//////////////////////////////////////////////////////////////////

float PLANE_NUM;
float GRASS_SLICE_NUM;
float HEIGHT;

float4 UVbounds;

half4 _FloorColor;
half4 _BladesColor;
half _GlobalColoringAdd;
half _GlobalColoringMult;
half _FloorSaturation;
half _BladesSaturation;

sampler2D _MainTex; // floor tex
sampler2D _GlobalColorTex;
sampler2D _BladesTex;
float4 _BladesTex_TexelSize;
sampler2D _BladesBackTex;
float4 _BladesBackTex_TexelSize;
sampler2D _NoiseTex;
sampler2D _NoiseTexHash;

#ifndef WRITE_INTO_DEPTH
sampler2D _GrassDepthTex;
float _VGZBufferParamA;
float _VGZBufferParamB;
float _VGZBufferFarClip;
#endif

float _floortiling;
float _view_angle_damper;

float _mod;
float _far_distance;
float _far_distance_transition;
half _AO_value;
half _AO_strength_steep;
float _AO_noise_tiling;
float _AO_far;
float _AO_border_damp;

float _wind_amp;
float2 _wind_dir;
float _wind_speed;
float _wind_freq;

float4 _wind_affector;		
float _wind_const_bend_affector;
float _wind_amp_affector;
float _wind_speed_affector;
float _wind_freq_affector;
float _wind_to_normal;
float affector_blend_darkening; // accessed dynamically from soccerball script

int MAX_RAYDEPTH;
float _MIP_BIAS;

samplerCUBE _CubemapDiff;
samplerCUBE _CubemapSpec;
float4x4	SkyMatrix;// set globaly by skyshop

float IBL_DiffStrength;
float IBL_DiffDirectStrength;
float IBL_SpecStrength;
float IBL_Gloss;
float _DiffFresnel;
float IBL_SpecFresnel;

struct Input {
	float4 worldPosDepth; // xyz - worldPos, z - eye depth
	float3 viewDir;
	float4 screenPos;
	float3 worldNormal;
	float3 worldRefl;
	
	fixed4 color:COLOR;
	INTERNAL_DATA
};

struct C2E2f_Output {
	float4 col:COLOR;
	#ifdef WRITE_INTO_DEPTH	
	float dep:DEPTH;
	#endif
};	

// quick gamma to linear approx of pow(n,2.2) function
inline float FastToLinear(float t) {
		t *= t * (t * 0.305306011 + 0.682171111) + 0.012522878;
		return t;
}

half3 DecodeRGBM(float4 rgbm)
{
	#ifdef IBL_HDR_RGBM
		// gamma/linear RGBM decoding
		#if defined(COLORSPACE_LINEAR)
	    	return rgbm.rgb * FastToLinear(rgbm.a) * 8;
	    #else
	    	return rgbm.rgb * rgbm.a * 8;
	    #endif
	#else
    	return rgbm.rgb;
	#endif
}

void vert (inout appdata_full v, out Input o) {
	UNITY_INITIALIZE_OUTPUT(Input,o);
	o.worldPosDepth.xyz=mul(unity_ObjectToWorld, v.vertex).xyz;
	v.tangent.xyz = cross(v.normal, float3(0,0,1));
	v.tangent.w = -1;
	
	v.vertex.xyz -= v.normal * HEIGHT * v.color.g*1.04;
	COMPUTE_EYEDEPTH(o.worldPosDepth.w);
}

void surf (Input IN, inout SurfaceOutput o) {
	o.Normal=float3(0,0,1);
	o.Alpha=1;
	o.Emission=0;
	
	float PLANE_NUM_INV=1.0/PLANE_NUM;
	float GRASS_SLICE_NUM_INV=1.0/GRASS_SLICE_NUM;
	float TILING_FACTOR=HEIGHT*GRASS_SLICE_NUM;
	float PREMULT=PLANE_NUM*GRASS_SLICE_NUM_INV;

	float dist=distance(IN.worldPosDepth.xyz, _WorldSpaceCameraPos);// IN.worldPosDepth.w;//
	float distanceT=saturate((dist-_far_distance)/_far_distance_transition);
	float2 mip_selector_0=saturate(distanceT-1);

	float2 coordsDetail=IN.worldPosDepth.xz/TILING_FACTOR;
	float4 _Range=float4(UVbounds.xz, (UVbounds.yw-UVbounds.xz));

 	float zoffset = IN.color.r+IN.color.g;
 	zoffset = (zoffset>1) ? 1 : zoffset;
	
	float3 EyeDirTan = -normalize(IN.viewDir); // eye vector in tangent space

	// bend slices
	float angle_fade=EyeDirTan.z;
	angle_fade*=angle_fade;
	angle_fade=1-angle_fade;
	EyeDirTan.z*=lerp(1, angle_fade, _view_angle_damper);
	EyeDirTan = normalize(EyeDirTan);		

	float3 mipcoords=(IN.worldPosDepth.xzy*float3(1,1,GRASS_SLICE_NUM)/TILING_FACTOR)*_BladesTex_TexelSize.w*_MIP_BIAS; // 3d filtering
	float3 dx = ddx( mipcoords );
	float3 dy = ddy( mipcoords );
	float d = min( dot( dx, dx ), dot( dy, dy ));
	float2 mip_selector=min(log2(d), float2(4,4)).xx-(1.5*angle_fade-1.5);

	float2 global_uv;
	
	#if defined(WIND) && !(defined(DONT_USE_MAIN_WIND) && defined(DONT_USE_SPHERICAL_WIND))
		float4 wtmp;
		float2 wind_coords;
		float2 wind_offset_main=0;
		float2 wind_offset_affector=0;
		
		#ifndef DONT_USE_MAIN_WIND
			float _wind_dir_magnitude=length(_wind_dir);
			float2 wind_dir=(_wind_dir_magnitude==0) ? float2(1,1) : -_wind_dir/_wind_dir_magnitude;
			wind_coords=coordsDetail*_wind_freq+_Time.xx*_wind_speed*wind_dir;
			wtmp=tex2Dlod(_NoiseTex, float4(wind_coords.xy,0,0));
			wind_offset_main=float2(wtmp.NOISE_CHANNEL_WINDX, wtmp.NOISE_CHANNEL_WINDY)-0.5;
			wind_offset_main-=_wind_dir;
			wind_offset_main*=_wind_amp;
		#endif
		
		#ifndef DONT_USE_SPHERICAL_WIND
			float2 wind_dir_affector=normalize(_wind_affector.xz-IN.worldPosDepth.xz);
			float affector_blend=saturate(1-distance(_wind_affector.xyz, IN.worldPosDepth.xyz)/_wind_affector.w);
			wind_coords=coordsDetail*_wind_freq_affector+_Time.xx*_wind_speed_affector;
			wtmp=tex2Dlod(_NoiseTex, float4(wind_coords.xy,0,0));
			wind_offset_affector=float2(wtmp.NOISE_CHANNEL_WINDX, wtmp.NOISE_CHANNEL_WINDY)-0.5;
			wind_coords=coordsDetail*_wind_freq_affector*0.5-float2(_Time.x*0.1, _Time.x*2)*_wind_speed_affector;
			wtmp=tex2Dlod(_NoiseTex, float4(wind_coords.xy,0,0));
			wind_offset_affector=lerp(wind_offset_affector, float2(wtmp.NOISE_CHANNEL_WINDX, wtmp.NOISE_CHANNEL_WINDY)-0.5, 0.5);
			wind_offset_affector+=wind_dir_affector*_wind_const_bend_affector*(1-affector_blend);
			wind_offset_affector*=_wind_amp_affector;
		#endif

		float wind_to_normal=_wind_to_normal;
		#ifndef DONT_USE_SPHERICAL_WIND
			wind_to_normal*=lerp(0.3, 1, affector_blend);
		#endif
		float2 wind_offset=saturate(1-IN.color.r*2);
		#ifdef DONT_USE_MAIN_WIND
			// spherical only
			wind_offset*=wind_offset_affector*affector_blend;
		#endif
		#ifdef DONT_USE_SPHERICAL_WIND
			// main only
			wind_offset*=wind_offset_main;
		#endif
		#if !defined(DONT_USE_MAIN_WIND) && !defined(DONT_USE_SPHERICAL_WIND)
			// both
			wind_offset*=lerp(wind_offset_main, wind_offset_affector, affector_blend);
		#endif
		
		o.Normal.xy+=wind_offset.xy*wind_to_normal;
		EyeDirTan.xy += wind_offset*EyeDirTan.z/(1-zoffset);
	 	float3 rayPos = float3(coordsDetail+wind_offset*GRASS_SLICE_NUM_INV, -zoffset*GRASS_SLICE_NUM_INV);
		//global_uv=(rayPos.xy*TILING_FACTOR-_Range.xy)/_Range.zw;
		EyeDirTan=normalize(EyeDirTan);	 			
	#else
	 	float3 rayPos = float3(coordsDetail, -zoffset*GRASS_SLICE_NUM_INV);
		//global_uv=(IN.worldPosDepth.xz-_Range.xy)/_Range.zw;
	#endif		
	//float2 global_uv_mult=TILING_FACTOR/_Range.zw;
	
 	float hgt=GRASS_SLICE_NUM_INV*saturate(IN.color.g-IN.color.r);

	float3 EyeDirTanAbs=abs(EyeDirTan);
	#ifdef FADE_PARALLELS
		float2 fade_parallels=angle_fade*EyeDirTanAbs.xy-angle_fade+1;
		fade_parallels+=IN.color.z*(1-fade_parallels);
	#endif
	
	float rayLength=0;
	float3 delta_next=float3(PLANE_NUM_INV,PLANE_NUM_INV,GRASS_SLICE_NUM_INV);
	
	// evaluated pixel color
	half4 c = half4(0.0,0.0,0.0,0.0);
	half4 cbump = half4(0.5,0.5,0.5,0.5);
	
 	float3 rayPosN=float3(rayPos.xy*PLANE_NUM, rayPos.z*GRASS_SLICE_NUM);
	float3 delta=-frac(rayPosN);
	delta=(EyeDirTan>0) ? frac(-rayPosN) : delta;
	delta*=delta_next;
	delta_next/=EyeDirTanAbs;
	delta/=EyeDirTan;
	delta.z=(rayPos.z<0)?delta.z:delta_next.z;
	
	float2 uv_bottom=rayPos.xy+delta.z*EyeDirTan.xy;
	#ifdef BOTTOM_COLORING
		float bottom_cut=_AO_value*(1-zoffset*_AO_border_damp);
		#ifdef BOTTOM_COLORING_NOISE_DRIVEN
			float bval=tex2Dlod(_NoiseTex, float4(uv_bottom*_AO_noise_tiling,0,0)).NOISE_CHANNEL_BCOLORING;
			bval=0.3*bval+0.7*tex2Dlod(_NoiseTex, float4(uv_bottom*_AO_noise_tiling*0.2,0,0)).NOISE_CHANNEL_BCOLORING;
			bottom_cut*=bval*0.8+0.2;
		#endif
		float bcoloring_fct=(1-angle_fade);
		bcoloring_fct*=bcoloring_fct;
		bcoloring_fct*=bcoloring_fct;
		bottom_cut*=lerp(1, _AO_strength_steep, bcoloring_fct);
		bottom_cut*=GRASS_SLICE_NUM;
		bottom_cut*=(1-IN.color.g*0.7);
		float bottom_ao_damp=1;
	#endif			
	global_uv = ((rayPos.xy+delta.z*EyeDirTan.xy*0.5)*TILING_FACTOR-_Range.xy)/_Range.zw; // bie¿emy namiar w œrodku
	
	// resolve far distance color
	#ifdef GLOBAL_COLORING
	fixed4 global_col=tex2D(_GlobalColorTex, global_uv);
	#endif
	fixed3 base_col=tex2D(_MainTex, uv_bottom/_floortiling).rgb;
	#ifdef BOTTOM_COLORING
	float AO_bottom=saturate(1-bottom_cut*lerp(1, _AO_far, distanceT) );
	base_col*=AO_bottom;
	#endif
	base_col=lerp(dot(half3(0.33,0.33,0.33), base_col).xxx, base_col, _FloorSaturation);
	base_col*=_FloorColor.rgb*4;
	#ifdef GLOBAL_COLORING
	 	base_col=lerp(base_col, base_col*global_col.rgb*2, _GlobalColoringMult)+global_col.rgb*_GlobalColoringAdd;
	#endif	

	float cval=1;
	float fresnelFct=(1+EyeDirTan.z);
	fresnelFct*=fresnelFct;
	fresnelFct*=fresnelFct;
	fresnelFct*=fresnelFct;
	float diffuseScatteringFactor=1.0 + fresnelFct*_DiffFresnel*(distanceT*0.7*(1-zoffset)+0.3);
	
	#ifdef IBL_DIFFUSE
		float3 normalW = WorldNormalVector(IN,o.Normal);
		#if defined(SKYSHOP_SKY_ROTATION)
			normalW = SkyMatrix[0].xyz*normalW.x + SkyMatrix[1].xyz*normalW.y + SkyMatrix[2].xyz*normalW.z;
		#endif	
		half3 IBLDiffuseCol = DecodeRGBM(texCUBElod(_CubemapDiff, float4(normalW,0)))*IBL_DiffStrength;
		#ifdef GLOBAL_COLORING
			IBLDiffuseCol.rgb*=global_col.a; // AO from global colormap
		#endif		
	#endif

	// early exit at far distance
 	if (distanceT>0.999) { 
		o.Albedo=base_col;
		o.Albedo *= diffuseScatteringFactor;
		#ifdef IBL_DIFFUSE
			IBLDiffuseCol*=o.Albedo*(1-IN.color.b); // AO (baked in albedo +additional from vertex color)
			o.Emission = IBLDiffuseCol.rgb;
			o.Albedo*=IBL_DiffDirectStrength;
		#endif
		#ifdef BOTTOM_COLORING_NOISE_DRIVEN
			o.Alpha=(1-IN.color.g)*(2.5+bval);
		#endif
		#ifdef WRITE_INTO_DEPTH
			float depth01 = (1.0 - IN.worldPosDepth.w * _ZBufferParams.w) / (IN.worldPosDepth.w * _ZBufferParams.z);
			o.Specular=depth01;			
		#endif
 		return;
	}
				
	float removeVerticalWrap=-1.0f/_BladesTex_TexelSize.y*GRASS_SLICE_NUM_INV / exp2(mip_selector.x);
	float2 _uv;
	half4 _col;
	int hitcount;
	
	bool zhit=false;
//	#if defined(UNITY_COMPILER_HLSL)
//	[loop]
//	#endif
	#if !SHADER_API_D3D11
	// variable number of steps doesn't work on Mac either (crashes...)
 	for(hitcount=0; hitcount < 10; hitcount++) {
 	#else
 	for(hitcount=0; hitcount < MAX_RAYDEPTH; hitcount++) {
 	#endif
		bool xy_flag=delta.x<delta.y;
		float delta_tmp=xy_flag ? delta.x : delta.y;
		zhit=(delta.z<delta_tmp);
		delta_tmp=zhit ? delta.z : delta_tmp;

		rayLength=(c.w>TRANSPARENCY_ZTEST_VALUE) ? rayLength : (rayLength+delta_tmp);
		float3 _delta=delta_tmp*EyeDirTan;
		rayPos+=_delta;
		//global_uv+=_delta.xy*global_uv_mult;
	
 		if (!zhit) {
 			float3 rayPos_tmp = xy_flag ? rayPos.xyz : rayPos.yxz;
 			
			#ifdef CUSTOM_HASH_FUNCTION
				#define HASH_OFFSET CUSTOM_HASH_FUNCTION
			#else
			float2 htmp=tex2Dlod(_NoiseTexHash, float4(rayPos_tmp.x*0.03+0.001,0,0,0)).rg;
				float HASH_OFFSET=(xy_flag ? htmp.x : htmp.y);
			#endif
			
			_uv=rayPos_tmp.yz+float2(HASH_OFFSET,rayPos_tmp.x*PREMULT+hgt);
	 		_col=tex2Dlod(_BladesTex, float4(_uv.x*_BladesTex_TexelSize.x*_BladesTex_TexelSize.w, _uv.y, mip_selector));
	 		_col.a*=saturate( (rayPos_tmp.z*GRASS_SLICE_NUM+hgt)*removeVerticalWrap );

			_col.rgb*=_col.a;
 			#ifdef FADE_PARALLELS
			float fade_parallelsXY=xy_flag ? fade_parallels.x : fade_parallels.y;
 				_col*=fade_parallelsXY;
 			#endif
 					 			
 			#ifdef BOTTOM_COLORING
 				cval=1+(rayPos.z*GRASS_SLICE_NUM)*bottom_cut;
 				cval=saturate(cval);
 				bottom_ao_damp*=lerp(cval, 1, c.w);
 				_col.rgb*=cval;
 			#endif

 			c+=(1-c.w)*_col;
 			
 			delta.xyz=xy_flag ? float3(delta_next.x, delta.yz-delta.x) : float3(delta.x-delta.y, delta_next.y, delta.z-delta.y);
 		}
 		if (zhit || c.w>=TRANSPARENCY_BREAK_VALUE) break;
	}
	
	_uv.y*=GRASS_SLICE_NUM;
	//mip_selector+=log2(_BladesTex_TexelSize.w*_BladesBackTex_TexelSize.y);
	_col=tex2Dlod(_BladesBackTex, float4(_uv, mip_selector ));
	#ifdef BOTTOM_COLORING
		bottom_ao_damp*=lerp(cval, 1, saturate(c.w));
		_col.rgb*=cval;
	#endif
	float floor_cw=zhit ? c.w : 1;
	c=(c+(1-c.w)*_col); 			

	half3 blades_desat=dot(half3(0.33,0.33,0.33), c.rgb).xxx;
	c.rgb=lerp(blades_desat, c.rgb, _BladesSaturation);
	c.rgb*=_BladesColor.rgb*2;
	
	#if defined(WIND) && !defined(DONT_USE_SPHERICAL_WIND)
		c.rgb*=lerp(1, 1-affector_blend_darkening, affector_blend);
	#endif
	
	#ifdef GLOBAL_COLORING
	//global_col=tex2Dlod(_GlobalColorTex, float4(global_uv, mip_selector_0));
	c.rgb=lerp(c.rgb, c.rgb*global_col.rgb*2, _GlobalColoringMult)+global_col.rgb*_GlobalColoringAdd;
	#endif
	
	c.rgb=lerp(base_col, c.rgb, floor_cw); 
 	o.Albedo=lerp(c.rgb, base_col, distanceT);
	o.Albedo *= diffuseScatteringFactor;
 	
	o.Alpha=floor_cw;
	#ifdef BOTTOM_COLORING_NOISE_DRIVEN
	o.Alpha+=(0.5+bval)*(1-IN.color.g);
	#endif
	o.Alpha=(rayPos.z>-0.001) ? floor_cw : o.Alpha;

	#ifdef WRITE_INTO_DEPTH
		float grassDepth = IN.worldPosDepth.w + rayLength * TILING_FACTOR * angle_fade * (1-distanceT) * (zhit ? 0.7:1);
		float depth01 = (1.0 - grassDepth * _ZBufferParams.w) / (grassDepth * _ZBufferParams.z);
		o.Specular=depth01;	  	
	#else
		float sceneDepth;
		// custom LinearEyeDepth() parametrization
		sceneDepth = 1.0 / ( (_VGZBufferParamA * tex2Dproj(_GrassDepthTex,UNITY_PROJ_COORD(IN.screenPos)).r) + _VGZBufferParamB );
		sceneDepth=(sceneDepth>_VGZBufferFarClip)?10000:sceneDepth; // don't clip beyond depth buffer distance
		float grassDepth = IN.worldPosDepth.w + rayLength * TILING_FACTOR;
		grassDepth = IN.worldPosDepth.w > sceneDepth+0.05 ? 0 : grassDepth;
		clip(sceneDepth-grassDepth);
	#endif
	
	#ifdef IBL_DIFFUSE
		IBLDiffuseCol*=o.Albedo*(1-IN.color.b); // AO (baked in albedo +additional from vertex color)
		#ifndef IBL_SPEC
		o.Emission = IBLDiffuseCol.rgb;
		#endif
	#endif
	
	#ifdef IBL_SPEC
		float exponential=fresnelFct;
		float3 reflVec = WorldReflectionVector(IN, o.Normal);
		#if defined(SKYSHOP_SKY_ROTATION)
		reflVec = SkyMatrix[0].xyz*reflVec.x + SkyMatrix[1].xyz*reflVec.y + SkyMatrix[2].xyz*reflVec.z;
		#endif
		// skyshop fit (I'd like people to get similar results in gamma / linear)
		#if defined(COLORSPACE_LINEAR)
			exponential=0.03+0.97*exponential;
		#else
			exponential=0.25+0.75*exponential;
		#endif
		float spec_fresnel = lerp (1.0f, exponential, IBL_SpecFresnel);
		float o_SpecularInvSquared = (1-IBL_Gloss)*(1-IBL_Gloss);
		half3 IBLSpecCol = DecodeRGBM(texCUBElod (_CubemapSpec, float4(reflVec, o_SpecularInvSquared*(6-exponential*IBL_SpecFresnel*3))))*IBL_SpecStrength;
		IBLSpecCol.rgb*=spec_fresnel * _SpecColor.rgb;
		#ifdef BOTTOM_COLORING
		IBLSpecCol.rgb*=AO_bottom*0.5+0.5;
		#endif
		IBLSpecCol.rgb*=saturate(1.5+rayPos.z*GRASS_SLICE_NUM);
		blades_desat=saturate(blades_desat+0.3);
		blades_desat*=blades_desat;
		blades_desat*=blades_desat;
		IBLSpecCol.rgb*=blades_desat*0.9+0.1;
		IBLSpecCol.rgb*=(1-distanceT);
		#ifdef GLOBAL_COLORING
			IBLSpecCol.rgb*=global_col.a; // AO from global colormap
		#endif
		#ifdef IBL_DIFFUSE
			// link diffuse and spec IBL together with energy conservation
			o.Emission += saturate(1-IBLSpecCol.rgb) * IBLDiffuseCol + IBLSpecCol.rgb;
		#else
			o.Emission=IBLSpecCol.rgb;		
		#endif
	#endif	
	
	#ifdef IBL_DIFFUSE
		o.Albedo*=IBL_DiffDirectStrength;
	#endif
}
