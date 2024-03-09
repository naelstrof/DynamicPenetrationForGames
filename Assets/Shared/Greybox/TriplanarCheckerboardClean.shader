// Made with Amplify Shader Editor v1.9.3.2
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "TriplanarCheckerboardClean"
{
	Properties
	{
		_BaseColor("BaseColor", Color) = (0.5,0.5,0.5,1)
		_Frequency("Frequency", Range( 0 , 1)) = 0
		_BaseColorMap("BaseColorMap", 2D) = "white" {}
		_Normal("Normal", 2D) = "bump" {}
		_MaskMap("MaskMap", 2D) = "black" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" }
		Cull Back
		CGINCLUDE
		#include "UnityPBSLighting.cginc"
		#include "Lighting.cginc"
		#pragma target 3.0
		#ifdef UNITY_PASS_SHADOWCASTER
			#undef INTERNAL_DATA
			#undef WorldReflectionVector
			#undef WorldNormalVector
			#define INTERNAL_DATA half3 internalSurfaceTtoW0; half3 internalSurfaceTtoW1; half3 internalSurfaceTtoW2;
			#define WorldReflectionVector(data,normal) reflect (data.worldRefl, half3(dot(data.internalSurfaceTtoW0,normal), dot(data.internalSurfaceTtoW1,normal), dot(data.internalSurfaceTtoW2,normal)))
			#define WorldNormalVector(data,normal) half3(dot(data.internalSurfaceTtoW0,normal), dot(data.internalSurfaceTtoW1,normal), dot(data.internalSurfaceTtoW2,normal))
		#endif
		struct Input
		{
			float3 worldPos;
			float3 worldNormal;
			INTERNAL_DATA
		};

		uniform sampler2D _Normal;
		uniform float _Frequency;
		uniform sampler2D _BaseColorMap;
		uniform float4 _BaseColor;
		uniform sampler2D _MaskMap;


		inline float3 TriplanarSampling621( sampler2D topTexMap, float3 worldPos, float3 worldNormal, float falloff, float2 tiling, float3 normalScale, float3 index )
		{
			float3 projNormal = ( pow( abs( worldNormal ), falloff ) );
			projNormal /= ( projNormal.x + projNormal.y + projNormal.z ) + 0.00001;
			float3 nsign = sign( worldNormal );
			half4 xNorm; half4 yNorm; half4 zNorm;
			xNorm = tex2D( topTexMap, tiling * worldPos.zy * float2(  nsign.x, 1.0 ) );
			yNorm = tex2D( topTexMap, tiling * worldPos.xz * float2(  nsign.y, 1.0 ) );
			zNorm = tex2D( topTexMap, tiling * worldPos.xy * float2( -nsign.z, 1.0 ) );
			xNorm.xyz  = half3( UnpackNormal( xNorm ).xy * float2(  nsign.x, 1.0 ) + worldNormal.zy, worldNormal.x ).zyx;
			yNorm.xyz  = half3( UnpackNormal( yNorm ).xy * float2(  nsign.y, 1.0 ) + worldNormal.xz, worldNormal.y ).xzy;
			zNorm.xyz  = half3( UnpackNormal( zNorm ).xy * float2( -nsign.z, 1.0 ) + worldNormal.xy, worldNormal.z ).xyz;
			return normalize( xNorm.xyz * projNormal.x + yNorm.xyz * projNormal.y + zNorm.xyz * projNormal.z );
		}


		inline float4 TriplanarSampling619( sampler2D topTexMap, float3 worldPos, float3 worldNormal, float falloff, float2 tiling, float3 normalScale, float3 index )
		{
			float3 projNormal = ( pow( abs( worldNormal ), falloff ) );
			projNormal /= ( projNormal.x + projNormal.y + projNormal.z ) + 0.00001;
			float3 nsign = sign( worldNormal );
			half4 xNorm; half4 yNorm; half4 zNorm;
			xNorm = tex2D( topTexMap, tiling * worldPos.zy * float2(  nsign.x, 1.0 ) );
			yNorm = tex2D( topTexMap, tiling * worldPos.xz * float2(  nsign.y, 1.0 ) );
			zNorm = tex2D( topTexMap, tiling * worldPos.xy * float2( -nsign.z, 1.0 ) );
			return xNorm * projNormal.x + yNorm * projNormal.y + zNorm * projNormal.z;
		}


		inline float4 TriplanarSampling625( sampler2D topTexMap, float3 worldPos, float3 worldNormal, float falloff, float2 tiling, float3 normalScale, float3 index )
		{
			float3 projNormal = ( pow( abs( worldNormal ), falloff ) );
			projNormal /= ( projNormal.x + projNormal.y + projNormal.z ) + 0.00001;
			float3 nsign = sign( worldNormal );
			half4 xNorm; half4 yNorm; half4 zNorm;
			xNorm = tex2D( topTexMap, tiling * worldPos.zy * float2(  nsign.x, 1.0 ) );
			yNorm = tex2D( topTexMap, tiling * worldPos.xz * float2(  nsign.y, 1.0 ) );
			zNorm = tex2D( topTexMap, tiling * worldPos.xy * float2( -nsign.z, 1.0 ) );
			return xNorm * projNormal.x + yNorm * projNormal.y + zNorm * projNormal.z;
		}


		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float2 temp_cast_0 = (_Frequency).xx;
			float3 ase_worldPos = i.worldPos;
			float3 ase_worldNormal = WorldNormalVector( i, float3( 0, 0, 1 ) );
			float3 ase_worldTangent = WorldNormalVector( i, float3( 1, 0, 0 ) );
			float3 ase_worldBitangent = WorldNormalVector( i, float3( 0, 1, 0 ) );
			float3x3 ase_worldToTangent = float3x3( ase_worldTangent, ase_worldBitangent, ase_worldNormal );
			float3 triplanar621 = TriplanarSampling621( _Normal, ase_worldPos, ase_worldNormal, 1.0, temp_cast_0, 1.0, 0 );
			float3 tanTriplanarNormal621 = mul( ase_worldToTangent, triplanar621 );
			o.Normal = tanTriplanarNormal621;
			float2 temp_cast_1 = (_Frequency).xx;
			float4 triplanar619 = TriplanarSampling619( _BaseColorMap, ase_worldPos, ase_worldNormal, 1.0, temp_cast_1, 1.0, 0 );
			o.Albedo = ( triplanar619 * _BaseColor ).xyz;
			float2 temp_cast_4 = (_Frequency).xx;
			float4 triplanar625 = TriplanarSampling625( _MaskMap, ase_worldPos, ase_worldNormal, 1.0, temp_cast_4, 1.0, 0 );
			o.Metallic = triplanar625.x;
			o.Smoothness = triplanar625.a;
			o.Occlusion = triplanar625.g;
			o.Alpha = 1;
		}

		ENDCG
		CGPROGRAM
		#pragma surface surf Standard keepalpha fullforwardshadows 

		ENDCG
		Pass
		{
			Name "ShadowCaster"
			Tags{ "LightMode" = "ShadowCaster" }
			ZWrite On
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			#pragma multi_compile_shadowcaster
			#pragma multi_compile UNITY_PASS_SHADOWCASTER
			#pragma skip_variants FOG_LINEAR FOG_EXP FOG_EXP2
			#include "HLSLSupport.cginc"
			#if ( SHADER_API_D3D11 || SHADER_API_GLCORE || SHADER_API_GLES || SHADER_API_GLES3 || SHADER_API_METAL || SHADER_API_VULKAN )
				#define CAN_SKIP_VPOS
			#endif
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "UnityPBSLighting.cginc"
			struct v2f
			{
				V2F_SHADOW_CASTER;
				float4 tSpace0 : TEXCOORD1;
				float4 tSpace1 : TEXCOORD2;
				float4 tSpace2 : TEXCOORD3;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};
			v2f vert( appdata_full v )
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID( v );
				UNITY_INITIALIZE_OUTPUT( v2f, o );
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( o );
				UNITY_TRANSFER_INSTANCE_ID( v, o );
				float3 worldPos = mul( unity_ObjectToWorld, v.vertex ).xyz;
				half3 worldNormal = UnityObjectToWorldNormal( v.normal );
				half3 worldTangent = UnityObjectToWorldDir( v.tangent.xyz );
				half tangentSign = v.tangent.w * unity_WorldTransformParams.w;
				half3 worldBinormal = cross( worldNormal, worldTangent ) * tangentSign;
				o.tSpace0 = float4( worldTangent.x, worldBinormal.x, worldNormal.x, worldPos.x );
				o.tSpace1 = float4( worldTangent.y, worldBinormal.y, worldNormal.y, worldPos.y );
				o.tSpace2 = float4( worldTangent.z, worldBinormal.z, worldNormal.z, worldPos.z );
				TRANSFER_SHADOW_CASTER_NORMALOFFSET( o )
				return o;
			}
			half4 frag( v2f IN
			#if !defined( CAN_SKIP_VPOS )
			, UNITY_VPOS_TYPE vpos : VPOS
			#endif
			) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID( IN );
				Input surfIN;
				UNITY_INITIALIZE_OUTPUT( Input, surfIN );
				float3 worldPos = float3( IN.tSpace0.w, IN.tSpace1.w, IN.tSpace2.w );
				half3 worldViewDir = normalize( UnityWorldSpaceViewDir( worldPos ) );
				surfIN.worldPos = worldPos;
				surfIN.worldNormal = float3( IN.tSpace0.z, IN.tSpace1.z, IN.tSpace2.z );
				surfIN.internalSurfaceTtoW0 = IN.tSpace0.xyz;
				surfIN.internalSurfaceTtoW1 = IN.tSpace1.xyz;
				surfIN.internalSurfaceTtoW2 = IN.tSpace2.xyz;
				SurfaceOutputStandard o;
				UNITY_INITIALIZE_OUTPUT( SurfaceOutputStandard, o )
				surf( surfIN, o );
				#if defined( CAN_SKIP_VPOS )
				float2 vpos = IN.pos;
				#endif
				SHADOW_CASTER_FRAGMENT( IN )
			}
			ENDCG
		}
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=19302
Node;AmplifyShaderEditor.WorldPosInputsNode;3;-178.8653,-456.9666;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RangedFloatNode;51;-489.4362,-276.6308;Inherit;True;Property;_Frequency;Frequency;2;0;Create;True;0;0;0;False;0;False;0;0.5;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.TexturePropertyNode;620;91.65508,-580.5987;Inherit;True;Property;_BaseColorMap;BaseColorMap;5;0;Create;True;0;0;0;False;0;False;None;None;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.TriplanarNode;619;410.5086,-347.6723;Inherit;True;Spherical;World;False;Top Texture 0;_TopTexture0;white;-1;None;Mid Texture 0;_MidTexture0;white;-1;None;Bot Texture 0;_BotTexture0;white;-1;None;Triplanar Sampler;Tangent;10;0;SAMPLER2D;;False;5;FLOAT;1;False;1;SAMPLER2D;;False;6;FLOAT;0;False;2;SAMPLER2D;;False;7;FLOAT;0;False;9;FLOAT3;0,0,0;False;8;FLOAT;1;False;3;FLOAT2;1,1;False;4;FLOAT;1;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TexturePropertyNode;626;95.55046,84.71613;Inherit;True;Property;_MaskMap;MaskMap;7;0;Create;True;0;0;0;False;0;False;None;None;False;black;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.TexturePropertyNode;622;105.6533,-142.2602;Inherit;True;Property;_Normal;Normal;6;0;Create;True;0;0;0;False;0;False;None;None;True;bump;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.ColorNode;47;1172.313,-27.41449;Inherit;False;Property;_BaseColor;BaseColor;0;0;Create;True;0;0;0;False;0;False;0.5,0.5,0.5,1;1,1,1,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.WorldPosInputsNode;495;-3762.215,-5236.341;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimpleAddOpNode;544;-1940.874,-5081.91;Inherit;True;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode;551;-1943.727,-4799.07;Inherit;True;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode;552;-1946.427,-4494.65;Inherit;True;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimplifiedFModOpNode;496;-2881.614,-5955.077;Inherit;True;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;529;-2881.614,-5747.077;Inherit;False;Constant;_Color3;Color 3;6;0;Create;True;0;0;0;False;0;False;1,0,0,0;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;535;-2881.614,-5587.077;Inherit;False;Constant;_Color6;Color 3;6;0;Create;True;0;0;0;False;0;False;0,1,0,0;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;530;-2625.614,-5955.077;Inherit;True;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;536;-2625.614,-5747.077;Inherit;True;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimplifiedFModOpNode;497;-2881.614,-4675.079;Inherit;True;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;537;-2881.614,-5107.078;Inherit;False;Constant;_Color4;Color 3;6;0;Create;True;0;0;0;False;0;False;1,0,0,0;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;532;-2881.614,-4947.078;Inherit;False;Constant;_Color5;Color 3;6;0;Create;True;0;0;0;False;0;False;0,1,0,0;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;538;-2625.614,-5315.078;Inherit;True;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;533;-2625.614,-5107.078;Inherit;True;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;540;-2881.614,-4467.08;Inherit;False;Constant;_Color8;Color 3;6;0;Create;True;0;0;0;False;0;False;1,0,0,0;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;539;-2881.614,-4307.081;Inherit;False;Constant;_Color7;Color 3;6;0;Create;True;0;0;0;False;0;False;0,1,0,0;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;541;-2625.614,-4675.079;Inherit;True;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;542;-2625.614,-4467.08;Inherit;True;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.WorldNormalVector;547;-2881.614,-4035.081;Inherit;True;False;1;0;FLOAT3;0,0,1;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.AbsOpNode;548;-2625.614,-4035.081;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.AbsOpNode;549;-2625.614,-3827.081;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.AbsOpNode;550;-2625.614,-3619.081;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;556;-1306.564,-4405.644;Inherit;True;Property;_TextureSample2;Texture Sample 0;7;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;557;-1305.065,-4180.648;Inherit;True;Property;_TextureSample3;Texture Sample 0;8;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;558;-875.8207,-3890.16;Inherit;True;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;560;-885.6843,-3455.362;Inherit;True;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;559;-871.6406,-3673.034;Inherit;True;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.TexturePropertyNode;563;-1802.804,-4148.731;Inherit;True;Property;_Texture3;Texture 3;4;0;Create;True;0;0;0;False;0;False;None;None;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.SamplerNode;546;-1298.917,-4610.784;Inherit;True;Property;_TextureSample1;Texture Sample 0;6;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimplifiedFModOpNode;503;-2881.614,-5315.078;Inherit;True;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;566;-2299.315,-4202.515;Inherit;True;False;2;0;FLOAT;0;False;1;FLOAT;4;False;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode;567;-2174.837,-3911.951;Inherit;True;3;0;FLOAT;0;False;1;FLOAT;0.4;False;2;FLOAT;0.6;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;570;-2572.359,-3379.154;Inherit;False;Constant;_Float23;Float 23;8;0;Create;True;0;0;0;False;0;False;0.4;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;571;-2574.359,-3293.154;Inherit;False;Constant;_Float24;Float 24;8;0;Create;True;0;0;0;False;0;False;0.6;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode;568;-2165.397,-3705.818;Inherit;True;3;0;FLOAT;0;False;1;FLOAT;0.5;False;2;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode;569;-2179.145,-3459.541;Inherit;True;3;0;FLOAT;0;False;1;FLOAT;0.5;False;2;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;573;-1763.156,-3341.654;Inherit;True;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;561;-582.6855,-3514.827;Inherit;True;3;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;579;-279.7007,-3104.17;Inherit;True;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;398;286.97,720.2654;Inherit;False;Constant;_Float18;Float 18;3;0;Create;True;0;0;0;False;0;False;0.1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;35;-807.1137,406.0789;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;34;-816.8974,170.3282;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;36;-775.1555,595.6514;Inherit;True;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.WorldNormalVector;9;-1407.347,1038.88;Inherit;False;False;1;0;FLOAT3;0,0,1;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.AbsOpNode;17;-1040.853,821.0663;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.AbsOpNode;22;-1039.853,1037.066;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.AbsOpNode;32;-1057.441,1252.353;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;50;-2525.606,784.3188;Inherit;False;Constant;_Float11;Float 11;1;0;Create;True;0;0;0;False;0;False;0.8;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;85;-1966.993,-111.8282;Inherit;False;Checkerboard;-1;;54;43dad715d66e03a4c8ad5f9564018081;0;4;1;FLOAT2;0,0;False;2;COLOR;0,0,0,0;False;3;COLOR;0,0,0,0;False;4;FLOAT2;0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.FunctionNode;83;-1966.993,48.17136;Inherit;False;Checkerboard;-1;;55;43dad715d66e03a4c8ad5f9564018081;0;4;1;FLOAT2;0,0;False;2;COLOR;0,0,0,0;False;3;COLOR;0,0,0,0;False;4;FLOAT2;0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.FunctionNode;84;-1966.993,-271.8279;Inherit;False;Checkerboard;-1;;56;43dad715d66e03a4c8ad5f9564018081;0;4;1;FLOAT2;0,0;False;2;COLOR;0,0,0,0;False;3;COLOR;0,0,0,0;False;4;FLOAT2;0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.FunctionNode;29;-1966.993,400.1714;Inherit;False;Checkerboard;-1;;57;43dad715d66e03a4c8ad5f9564018081;0;4;1;FLOAT2;0,0;False;2;COLOR;0,0,0,0;False;3;COLOR;0,0,0,0;False;4;FLOAT2;0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;49;-2348.605,674.3185;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;93;-2244.847,-531.9739;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;94;-2404.846,-371.9738;Inherit;False;Constant;_Float12;Float 11;1;0;Create;True;0;0;0;False;0;False;0.9;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;99;-1321.235,594.6136;Inherit;True;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SwizzleNode;15;-2349.647,-9.823984;Inherit;False;FLOAT2;0;2;2;3;1;0;FLOAT3;0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SwizzleNode;27;-2348.379,112.7408;Inherit;False;FLOAT2;2;1;2;3;1;0;FLOAT3;0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SwizzleNode;20;-2346.776,251.2406;Inherit;False;FLOAT2;0;1;2;3;1;0;FLOAT3;0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.FunctionNode;2;-1989.993,190.1713;Inherit;True;Checkerboard;-1;;59;43dad715d66e03a4c8ad5f9564018081;0;4;1;FLOAT2;0,0;False;2;COLOR;0,0,0,0;False;3;COLOR;0,0,0,0;False;4;FLOAT2;0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;98;-1327.235,355.6134;Inherit;True;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;97;-1331.148,48.56717;Inherit;True;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;100;-3006.28,-275.4514;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;101;-3174.962,-307.0911;Inherit;False;Constant;_Float14;Float 13;4;0;Create;True;0;0;0;False;0;False;5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;64;-2589.931,246.7972;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0.5,0.5,0.5;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SaturateNode;38;-246.9515,496.4087;Inherit;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;380;33.82064,605.8434;Inherit;True;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;4;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;580;-2008.367,-1744.306;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;581;-2018.151,-1980.056;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;582;-1976.409,-1554.733;Inherit;True;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.WorldNormalVector;583;-2608.601,-1111.505;Inherit;False;False;1;0;FLOAT3;0,0,1;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.AbsOpNode;584;-2242.107,-1329.318;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.AbsOpNode;585;-2241.107,-1113.319;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.AbsOpNode;586;-2258.695,-898.032;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;587;-3726.861,-1366.066;Inherit;False;Constant;_Float25;Float 11;1;0;Create;True;0;0;0;False;0;False;0.4;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;597;-2522.489,-1555.771;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SwizzleNode;598;-3550.902,-2160.209;Inherit;False;FLOAT2;0;2;2;3;1;0;FLOAT3;0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SwizzleNode;599;-3549.634,-2037.644;Inherit;False;FLOAT2;2;1;2;3;1;0;FLOAT3;0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SwizzleNode;600;-3548.031,-1899.144;Inherit;False;FLOAT2;0;1;2;3;1;0;FLOAT3;0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.FunctionNode;601;-3191.249,-1960.213;Inherit;False;Checkerboard;-1;;65;43dad715d66e03a4c8ad5f9564018081;0;4;1;FLOAT2;0,0;False;2;COLOR;0,0,0,0;False;3;COLOR;0,0,0,0;False;4;FLOAT2;0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;602;-2528.489,-1794.772;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;603;-2532.402,-2101.818;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;604;-5118.956,-1735.22;Inherit;True;Property;_Frequency1;Frequency;3;0;Create;True;0;0;0;False;0;False;0;0.5;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;605;-4207.535,-2425.837;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;607;-4376.217,-2457.477;Inherit;False;Constant;_Float27;Float 13;4;0;Create;True;0;0;0;False;0;False;5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;608;-3791.186,-1903.588;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0.5,0.5,0.5;False;1;FLOAT3;0
Node;AmplifyShaderEditor.WorldPosInputsNode;609;-4003.141,-1912.484;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimpleAddOpNode;611;-1712.539,-1755.586;Inherit;True;3;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;618;-3716.754,-1491.425;Inherit;False;Constant;_Float29;Float 11;1;0;Create;True;0;0;0;False;0;False;0.5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;610;-1454.16,-1661.916;Inherit;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;596;-3716.101,-2475.359;Inherit;False;Constant;_Float26;Float 11;1;0;Create;True;0;0;0;False;0;False;0.9;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;617;-3714.17,-2585.282;Inherit;False;Constant;_Float28;Float 11;1;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;595;-3474.102,-2606.359;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;590;-3168.249,-2482.214;Inherit;False;Checkerboard;-1;;66;43dad715d66e03a4c8ad5f9564018081;0;4;1;FLOAT2;0,0;False;2;COLOR;0,0,0,0;False;3;COLOR;0,0,0,0;False;4;FLOAT2;0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.FunctionNode;588;-3168.249,-2348.214;Inherit;False;Checkerboard;-1;;67;43dad715d66e03a4c8ad5f9564018081;0;4;1;FLOAT2;0,0;False;2;COLOR;0,0,0,0;False;3;COLOR;0,0,0,0;False;4;FLOAT2;0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.FunctionNode;589;-3180.249,-2205.213;Inherit;True;Checkerboard;-1;;68;43dad715d66e03a4c8ad5f9564018081;0;4;1;FLOAT2;0,0;False;2;COLOR;0,0,0,0;False;3;COLOR;0,0,0,0;False;4;FLOAT2;0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.FunctionNode;591;-3178.249,-1819.214;Inherit;False;Checkerboard;-1;;69;43dad715d66e03a4c8ad5f9564018081;0;4;1;FLOAT2;0,0;False;2;COLOR;0,0,0,0;False;3;COLOR;0,0,0,0;False;4;FLOAT2;0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.FunctionNode;592;-3186.249,-1690.213;Inherit;True;Checkerboard;-1;;70;43dad715d66e03a4c8ad5f9564018081;0;4;1;FLOAT2;0,0;False;2;COLOR;0,0,0,0;False;3;COLOR;0,0,0,0;False;4;FLOAT2;0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.FunctionNode;30;-1960.993,567.1716;Inherit;False;Checkerboard;-1;;71;43dad715d66e03a4c8ad5f9564018081;0;4;1;FLOAT2;0,0;False;2;COLOR;0,0,0,0;False;3;COLOR;0,0,0,0;False;4;FLOAT2;0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;92;-2468.846,-531.9739;Inherit;False;Property;_Color2;Color;1;0;Create;True;0;0;0;False;0;False;0.5,0.5,0.5,1;1,1,1,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;37;-511.2856,394.7987;Inherit;True;3;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;271;1257.703,377.3322;Inherit;False;Constant;_Float15;Float 13;4;0;Create;True;0;0;0;False;0;False;0.2;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TriplanarNode;621;400.9157,-123.0865;Inherit;True;Spherical;World;True;Top Texture 1;_TopTexture1;white;-1;None;Mid Texture 1;_MidTexture1;white;-1;None;Bot Texture 1;_BotTexture1;white;-1;None;Triplanar Sampler;Tangent;10;0;SAMPLER2D;;False;5;FLOAT;1;False;1;SAMPLER2D;;False;6;FLOAT;0;False;2;SAMPLER2D;;False;7;FLOAT;0;False;9;FLOAT3;0,0,0;False;8;FLOAT;1;False;3;FLOAT2;1,1;False;4;FLOAT;1;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TriplanarNode;625;419.0136,118.9671;Inherit;True;Spherical;World;False;Top Texture 2;_TopTexture2;white;-1;None;Mid Texture 2;_MidTexture2;white;-1;None;Bot Texture 2;_BotTexture2;white;-1;None;Triplanar Sampler;Tangent;10;0;SAMPLER2D;;False;5;FLOAT;1;False;1;SAMPLER2D;;False;6;FLOAT;0;False;2;SAMPLER2D;;False;7;FLOAT;0;False;9;FLOAT3;0,0,0;False;8;FLOAT;1;False;3;FLOAT2;1,1;False;4;FLOAT;1;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.FunctionNode;396;720.1035,537.4513;Inherit;False;Normal From Height;-1;;72;1942fe2c5f1a1f94881a33d532e4afeb;0;2;20;FLOAT;0;False;110;FLOAT;1;False;2;FLOAT3;40;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;624;1172.916,-327.0865;Inherit;False;2;2;0;FLOAT4;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;409;-1503.955,3920.728;Inherit;True;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;410;-1503.955,4176.728;Inherit;True;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;411;-1503.955,4432.729;Inherit;True;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;412;-1247.955,3920.728;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;413;-1247.955,4176.728;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;414;-1247.955,4432.729;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;427;-3422.269,4810.387;Inherit;False;Constant;_Float19;Float 16;3;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;435;-3579.712,4350.404;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;436;-3579.712,4574.403;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;437;-3451.713,4350.404;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;4;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldPosInputsNode;438;-4373.732,4295.847;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimplifiedFModOpNode;439;-4125.886,4117.865;Inherit;True;2;0;FLOAT;0;False;1;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimplifiedFModOpNode;440;-4125.886,4565.866;Inherit;True;2;0;FLOAT;0;False;1;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;441;-3851.71,4606.403;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;442;-3723.712,4574.403;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;443;-3723.712,4350.404;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;445;-3323.713,4350.404;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimplifiedFModOpNode;446;-4125.886,4341.866;Inherit;True;2;0;FLOAT;0;False;1;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;448;-3908.708,4117.402;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;449;-3451.713,4574.403;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;4;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;450;-4235.243,4016.527;Inherit;False;2;0;FLOAT;1;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;451;-3894.908,4358.404;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;452;-3323.713,4574.403;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;455;-4543.344,4122.126;Inherit;False;Constant;_Float21;Float 13;3;0;Create;True;0;0;0;False;0;False;0.25;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode;417;-2957.356,4291.747;Inherit;True;3;0;FLOAT;0;False;1;FLOAT;0.05;False;2;FLOAT;0.1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode;418;-2957.356,4515.747;Inherit;True;3;0;FLOAT;0;False;1;FLOAT;0.05;False;2;FLOAT;0.1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;419;-2631.037,4303.3;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;4;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;420;-2651.037,4527.3;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;4;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;421;-2636.037,4050.989;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;4;False;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode;422;-2950.356,4082.751;Inherit;True;3;0;FLOAT;0;False;1;FLOAT;0.05;False;2;FLOAT;0.1;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldNormalVector;423;-2353.204,3977.245;Inherit;True;False;1;0;FLOAT3;0,0,1;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.AbsOpNode;424;-2105.985,3974.355;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.AbsOpNode;425;-2106.456,4195.48;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.AbsOpNode;426;-2109.124,4419.297;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;415;-788.236,4175.577;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;416;-990.2615,4176.728;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;4;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;453;-3263.713,4125.402;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;454;-3430.713,4125.402;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;4;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;444;-3770.712,4125.402;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;447;-3619.712,4127.402;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;461;-3621.809,4461.158;Inherit;False;Constant;_Float22;Float 22;3;0;Create;True;0;0;0;False;0;False;4;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;428;-3459.56,5063.38;Inherit;False;Constant;_Float20;Float 16;3;0;Create;True;0;0;0;False;0;False;0.1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;299;-3433.01,3220.315;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;305;-3433.01,3444.314;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldPosInputsNode;276;-4227.04,3165.759;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimplifiedFModOpNode;279;-3979.195,2987.777;Inherit;True;2;0;FLOAT;0;False;1;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimplifiedFModOpNode;303;-3979.195,3435.777;Inherit;True;2;0;FLOAT;0;False;1;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;304;-3705.01,3476.314;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;306;-3577.01,3444.314;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;300;-3577.01,3220.315;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;281;-3577.01,2996.314;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;284;-3433.01,2996.314;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;283;-3762.01,2987.314;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;307;-3305.011,3444.314;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;4;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;385;-4088.551,2886.439;Inherit;False;2;0;FLOAT;1;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;298;-3748.21,3228.315;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;308;-3177.011,3444.314;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;287;-3156.011,2995.314;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;285;-3284.011,2995.314;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;4;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;384;-4396.652,2992.038;Inherit;False;Constant;_Float13;Float 13;3;0;Create;True;0;0;0;False;0;False;2;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode;320;-2832.211,3146.565;Inherit;True;3;0;FLOAT;0;False;1;FLOAT;0.05;False;2;FLOAT;0.1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode;322;-2832.211,3370.565;Inherit;True;3;0;FLOAT;0;False;1;FLOAT;0.05;False;2;FLOAT;0.1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;346;-2525.892,3382.118;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;4;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;345;-2507.892,3158.118;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;4;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;344;-2510.892,2905.802;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;4;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;365;-848.6346,3080.451;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;4;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;382;-661.8347,3215.951;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;358;-1357.254,2790.64;Inherit;True;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;362;-1357.254,3046.64;Inherit;True;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;364;-1357.254,3302.64;Inherit;True;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;360;-1101.254,2790.64;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;361;-1101.254,3046.64;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;363;-1101.254,3302.64;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldNormalVector;339;-2228.058,2832.058;Inherit;True;False;1;0;FLOAT3;0,0,1;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.AbsOpNode;342;-1980.839,2829.167;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.AbsOpNode;341;-1981.31,3050.298;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.AbsOpNode;343;-1983.979,3274.115;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode;321;-2825.211,2937.564;Inherit;True;3;0;FLOAT;0;False;1;FLOAT;0.05;False;2;FLOAT;0.1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;302;-3116.099,3226.406;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;301;-3283.083,3203.259;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;4;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimplifiedFModOpNode;297;-3979.195,3211.778;Inherit;True;2;0;FLOAT;0;False;1;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;393;-3223.885,3582.93;Inherit;False;Constant;_Float16;Float 16;3;0;Create;True;0;0;0;False;0;False;0.025;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;394;-3187.952,3653.064;Inherit;False;Constant;_Float17;Float 16;3;0;Create;True;0;0;0;False;0;False;0.1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;629;-3738.189,2269.093;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;630;-3738.189,2493.092;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldPosInputsNode;631;-4532.219,2214.537;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimplifiedFModOpNode;632;-4284.375,2036.555;Inherit;True;2;0;FLOAT;0;False;1;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimplifiedFModOpNode;633;-4284.375,2484.555;Inherit;True;2;0;FLOAT;0;False;1;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;634;-4010.189,2525.092;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;635;-3882.189,2493.092;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;636;-3882.189,2269.093;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;637;-3882.189,2045.092;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;638;-3738.189,2045.092;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;639;-4067.189,2036.092;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;640;-3610.19,2493.092;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;4;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;641;-4393.73,1935.217;Inherit;False;2;0;FLOAT;1;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;642;-4053.389,2277.093;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;643;-3482.19,2493.092;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;644;-3461.19,2044.092;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;645;-3589.19,2044.092;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;4;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;646;-4701.831,2040.816;Inherit;False;Constant;_Float30;Float 13;3;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode;647;-3137.39,2195.343;Inherit;True;3;0;FLOAT;0;False;1;FLOAT;0.05;False;2;FLOAT;0.1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode;648;-3137.39,2419.343;Inherit;True;3;0;FLOAT;0;False;1;FLOAT;0.05;False;2;FLOAT;0.1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;649;-2831.071,2430.896;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;4;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;650;-2813.071,2206.896;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;4;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;651;-2816.071,1954.58;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;4;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;652;-1153.814,2129.229;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;4;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;653;-967.0137,2264.729;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;654;-1662.433,1839.418;Inherit;True;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;655;-1662.433,2095.418;Inherit;True;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;656;-1662.433,2351.418;Inherit;True;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;657;-1406.433,1839.418;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;658;-1406.433,2095.418;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;659;-1406.433,2351.418;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldNormalVector;660;-2533.237,1880.836;Inherit;True;False;1;0;FLOAT3;0,0,1;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.AbsOpNode;661;-2286.018,1877.945;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.AbsOpNode;662;-2286.489,2099.076;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.AbsOpNode;663;-2289.158,2322.893;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode;664;-3130.39,1986.342;Inherit;True;3;0;FLOAT;0;False;1;FLOAT;0.05;False;2;FLOAT;0.1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;665;-3421.278,2275.184;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;666;-3588.262,2252.037;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;4;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimplifiedFModOpNode;667;-4284.375,2260.556;Inherit;True;2;0;FLOAT;0;False;1;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;668;-3881.489,2359.437;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;8;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;670;-3529.064,2631.708;Inherit;False;Constant;_Float31;Float 16;3;0;Create;True;0;0;0;False;0;False;0.025;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;671;-3493.131,2701.842;Inherit;False;Constant;_Float32;Float 16;3;0;Create;True;0;0;0;False;0;False;0.1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;456;-166.2893,3509.588;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;4;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;672;479.9814,2712.287;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;4;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMaxOpNode;628;-461.7849,3346.153;Inherit;True;2;0;FLOAT;0;False;1;FLOAT;0.2;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMaxOpNode;669;-769.9639,2405.931;Inherit;True;2;0;FLOAT;0;False;1;FLOAT;0.6;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMaxOpNode;460;-576.6646,4090.946;Inherit;True;2;0;FLOAT;0;False;1;FLOAT;0.8;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;627;-3576.31,3310.659;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;4;False;1;FLOAT;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;673;1551.928,-233.0815;Float;False;True;-1;2;ASEMaterialInspector;0;0;Standard;TriplanarCheckerboardClean;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;False;;0;False;;False;0;False;;0;False;;False;0;Opaque;0.5;True;True;0;False;Opaque;;Geometry;All;12;all;True;True;True;True;0;False;;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;2;15;10;25;False;0.5;True;0;0;False;;0;False;;0;0;False;;0;False;;0;False;;0;False;;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;;-1;0;False;;0;0;0;False;0.1;False;;0;False;;False;17;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;16;FLOAT4;0,0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;619;0;620;0
WireConnection;619;9;3;0
WireConnection;619;3;51;0
WireConnection;544;0;530;0
WireConnection;544;1;533;0
WireConnection;551;0;541;0
WireConnection;551;1;536;0
WireConnection;552;0;541;0
WireConnection;552;1;533;0
WireConnection;496;0;495;1
WireConnection;530;0;496;0
WireConnection;530;1;529;0
WireConnection;536;0;496;0
WireConnection;536;1;535;0
WireConnection;497;0;495;3
WireConnection;538;0;503;0
WireConnection;538;1;537;0
WireConnection;533;0;503;0
WireConnection;533;1;532;0
WireConnection;541;0;497;0
WireConnection;541;1;540;0
WireConnection;542;0;497;0
WireConnection;542;1;539;0
WireConnection;548;0;547;2
WireConnection;549;0;547;1
WireConnection;550;0;547;3
WireConnection;556;0;563;0
WireConnection;556;1;551;0
WireConnection;557;0;563;0
WireConnection;557;1;552;0
WireConnection;558;0;546;0
WireConnection;558;1;569;0
WireConnection;560;0;557;0
WireConnection;560;1;568;0
WireConnection;559;0;556;0
WireConnection;559;1;567;0
WireConnection;546;0;563;0
WireConnection;546;1;544;0
WireConnection;503;0;495;2
WireConnection;566;0;548;0
WireConnection;567;0;548;0
WireConnection;567;1;570;0
WireConnection;567;2;571;0
WireConnection;568;0;549;0
WireConnection;568;1;570;0
WireConnection;568;2;571;0
WireConnection;569;0;550;0
WireConnection;569;1;570;0
WireConnection;569;2;571;0
WireConnection;573;0;567;0
WireConnection;573;1;568;0
WireConnection;573;2;569;0
WireConnection;561;0;558;0
WireConnection;561;1;559;0
WireConnection;561;2;560;0
WireConnection;579;0;561;0
WireConnection;579;1;573;0
WireConnection;35;0;98;0
WireConnection;35;1;22;0
WireConnection;34;0;97;0
WireConnection;34;1;17;0
WireConnection;36;0;99;0
WireConnection;36;1;32;0
WireConnection;17;0;9;2
WireConnection;22;0;9;1
WireConnection;32;0;9;3
WireConnection;85;1;27;0
WireConnection;85;2;92;0
WireConnection;85;3;93;0
WireConnection;85;4;100;0
WireConnection;83;1;20;0
WireConnection;83;2;92;0
WireConnection;83;3;93;0
WireConnection;83;4;100;0
WireConnection;84;1;15;0
WireConnection;84;2;92;0
WireConnection;84;3;93;0
WireConnection;84;4;100;0
WireConnection;29;1;27;0
WireConnection;29;3;49;0
WireConnection;49;1;50;0
WireConnection;93;0;92;0
WireConnection;93;1;94;0
WireConnection;99;0;83;0
WireConnection;99;1;30;0
WireConnection;15;0;64;0
WireConnection;27;0;64;0
WireConnection;20;0;64;0
WireConnection;2;1;15;0
WireConnection;2;3;49;0
WireConnection;98;0;85;0
WireConnection;98;1;29;0
WireConnection;97;0;84;0
WireConnection;97;1;2;0
WireConnection;100;1;101;0
WireConnection;38;0;37;0
WireConnection;380;0;38;0
WireConnection;380;1;382;0
WireConnection;580;0;602;0
WireConnection;580;1;585;0
WireConnection;581;0;603;0
WireConnection;581;1;584;0
WireConnection;582;0;597;0
WireConnection;582;1;586;0
WireConnection;584;0;583;2
WireConnection;585;0;583;1
WireConnection;586;0;583;3
WireConnection;597;0;589;0
WireConnection;597;1;592;0
WireConnection;598;0;608;0
WireConnection;599;0;608;0
WireConnection;600;0;608;0
WireConnection;601;1;598;0
WireConnection;601;2;618;0
WireConnection;601;3;587;0
WireConnection;601;4;604;0
WireConnection;602;0;588;0
WireConnection;602;1;591;0
WireConnection;603;0;590;0
WireConnection;603;1;601;0
WireConnection;605;0;604;0
WireConnection;605;1;607;0
WireConnection;608;0;609;0
WireConnection;611;0;581;0
WireConnection;611;1;580;0
WireConnection;611;2;582;0
WireConnection;610;0;611;0
WireConnection;595;0;617;0
WireConnection;595;1;596;0
WireConnection;590;1;598;0
WireConnection;590;2;617;0
WireConnection;590;3;596;0
WireConnection;590;4;605;0
WireConnection;588;1;599;0
WireConnection;588;2;617;0
WireConnection;588;3;595;0
WireConnection;588;4;605;0
WireConnection;589;1;600;0
WireConnection;589;2;617;0
WireConnection;589;3;596;0
WireConnection;589;4;605;0
WireConnection;591;1;599;0
WireConnection;591;2;618;0
WireConnection;591;3;587;0
WireConnection;591;4;604;0
WireConnection;592;1;600;0
WireConnection;592;2;618;0
WireConnection;592;3;587;0
WireConnection;592;4;604;0
WireConnection;30;1;20;0
WireConnection;30;3;49;0
WireConnection;37;0;34;0
WireConnection;37;1;35;0
WireConnection;37;2;36;0
WireConnection;621;0;622;0
WireConnection;621;9;3;0
WireConnection;621;3;51;0
WireConnection;625;0;626;0
WireConnection;625;9;3;0
WireConnection;625;3;51;0
WireConnection;396;20;456;0
WireConnection;396;110;398;0
WireConnection;624;0;619;0
WireConnection;624;1;47;0
WireConnection;409;0;426;0
WireConnection;409;1;421;0
WireConnection;410;0;424;0
WireConnection;410;1;419;0
WireConnection;411;0;425;0
WireConnection;411;1;420;0
WireConnection;412;0;409;0
WireConnection;413;0;410;0
WireConnection;414;0;411;0
WireConnection;435;0;443;0
WireConnection;435;1;451;0
WireConnection;436;0;442;0
WireConnection;436;1;441;0
WireConnection;437;0;435;0
WireConnection;437;1;461;0
WireConnection;439;0;438;1
WireConnection;439;1;455;0
WireConnection;440;0;438;3
WireConnection;440;1;455;0
WireConnection;441;0;440;0
WireConnection;441;1;450;0
WireConnection;442;0;441;0
WireConnection;443;0;451;0
WireConnection;445;0;437;0
WireConnection;446;0;438;2
WireConnection;446;1;455;0
WireConnection;448;0;439;0
WireConnection;448;1;450;0
WireConnection;449;0;436;0
WireConnection;449;1;461;0
WireConnection;450;1;455;0
WireConnection;451;0;446;0
WireConnection;451;1;450;0
WireConnection;452;0;449;0
WireConnection;417;0;445;0
WireConnection;417;1;427;0
WireConnection;417;2;428;0
WireConnection;418;0;452;0
WireConnection;418;1;427;0
WireConnection;418;2;428;0
WireConnection;419;0;422;0
WireConnection;419;1;418;0
WireConnection;420;0;417;0
WireConnection;420;1;418;0
WireConnection;421;0;422;0
WireConnection;421;1;417;0
WireConnection;422;0;453;0
WireConnection;422;1;427;0
WireConnection;422;2;428;0
WireConnection;424;0;423;2
WireConnection;425;0;423;1
WireConnection;426;0;423;3
WireConnection;415;0;416;0
WireConnection;416;0;412;0
WireConnection;416;1;413;0
WireConnection;416;2;414;0
WireConnection;453;0;454;0
WireConnection;454;0;447;0
WireConnection;454;1;461;0
WireConnection;444;0;448;0
WireConnection;447;0;444;0
WireConnection;447;1;448;0
WireConnection;299;0;300;0
WireConnection;299;1;298;0
WireConnection;305;0;306;0
WireConnection;305;1;304;0
WireConnection;279;0;276;1
WireConnection;279;1;384;0
WireConnection;303;0;276;3
WireConnection;303;1;384;0
WireConnection;304;0;303;0
WireConnection;304;1;385;0
WireConnection;306;0;304;0
WireConnection;300;0;298;0
WireConnection;281;0;283;0
WireConnection;284;0;281;0
WireConnection;284;1;283;0
WireConnection;283;0;279;0
WireConnection;283;1;385;0
WireConnection;307;0;305;0
WireConnection;307;1;627;0
WireConnection;385;1;384;0
WireConnection;298;0;297;0
WireConnection;298;1;385;0
WireConnection;308;0;307;0
WireConnection;287;0;285;0
WireConnection;285;0;284;0
WireConnection;285;1;627;0
WireConnection;320;0;302;0
WireConnection;320;1;393;0
WireConnection;320;2;394;0
WireConnection;322;0;308;0
WireConnection;322;1;393;0
WireConnection;322;2;394;0
WireConnection;346;0;320;0
WireConnection;346;1;322;0
WireConnection;345;0;321;0
WireConnection;345;1;322;0
WireConnection;344;0;321;0
WireConnection;344;1;320;0
WireConnection;365;0;360;0
WireConnection;365;1;361;0
WireConnection;365;2;363;0
WireConnection;382;0;365;0
WireConnection;358;0;343;0
WireConnection;358;1;344;0
WireConnection;362;0;342;0
WireConnection;362;1;345;0
WireConnection;364;0;341;0
WireConnection;364;1;346;0
WireConnection;360;0;358;0
WireConnection;361;0;362;0
WireConnection;363;0;364;0
WireConnection;342;0;339;2
WireConnection;341;0;339;1
WireConnection;343;0;339;3
WireConnection;321;0;287;0
WireConnection;321;1;393;0
WireConnection;321;2;394;0
WireConnection;302;0;301;0
WireConnection;301;0;299;0
WireConnection;301;1;627;0
WireConnection;297;0;276;2
WireConnection;297;1;384;0
WireConnection;629;0;636;0
WireConnection;629;1;642;0
WireConnection;630;0;635;0
WireConnection;630;1;634;0
WireConnection;632;0;631;1
WireConnection;632;1;646;0
WireConnection;633;0;631;3
WireConnection;633;1;646;0
WireConnection;634;0;633;0
WireConnection;634;1;641;0
WireConnection;635;0;634;0
WireConnection;636;0;642;0
WireConnection;637;0;639;0
WireConnection;638;0;637;0
WireConnection;638;1;639;0
WireConnection;639;0;632;0
WireConnection;639;1;641;0
WireConnection;640;0;630;0
WireConnection;640;1;668;0
WireConnection;641;1;646;0
WireConnection;642;0;667;0
WireConnection;642;1;641;0
WireConnection;643;0;640;0
WireConnection;644;0;645;0
WireConnection;645;0;638;0
WireConnection;645;1;668;0
WireConnection;647;0;665;0
WireConnection;647;1;670;0
WireConnection;647;2;671;0
WireConnection;648;0;643;0
WireConnection;648;1;670;0
WireConnection;648;2;671;0
WireConnection;649;0;647;0
WireConnection;649;1;648;0
WireConnection;650;0;664;0
WireConnection;650;1;648;0
WireConnection;651;0;664;0
WireConnection;651;1;647;0
WireConnection;652;0;657;0
WireConnection;652;1;658;0
WireConnection;652;2;659;0
WireConnection;653;0;652;0
WireConnection;654;0;663;0
WireConnection;654;1;651;0
WireConnection;655;0;661;0
WireConnection;655;1;650;0
WireConnection;656;0;662;0
WireConnection;656;1;649;0
WireConnection;657;0;654;0
WireConnection;658;0;655;0
WireConnection;659;0;656;0
WireConnection;661;0;660;2
WireConnection;662;0;660;1
WireConnection;663;0;660;3
WireConnection;664;0;644;0
WireConnection;664;1;670;0
WireConnection;664;2;671;0
WireConnection;665;0;666;0
WireConnection;666;0;629;0
WireConnection;666;1;668;0
WireConnection;667;0;631;2
WireConnection;667;1;646;0
WireConnection;668;0;646;0
WireConnection;456;0;628;0
WireConnection;456;1;460;0
WireConnection;672;0;669;0
WireConnection;672;1;456;0
WireConnection;628;0;382;0
WireConnection;669;0;653;0
WireConnection;460;0;415;0
WireConnection;627;0;384;0
WireConnection;673;0;624;0
WireConnection;673;1;621;0
WireConnection;673;3;625;1
WireConnection;673;4;625;4
WireConnection;673;5;625;2
ASEEND*/
//CHKSM=E705C93FD741E4E2BE9E865421166BD75477B4BE