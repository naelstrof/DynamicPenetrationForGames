// Made with Amplify Shader Editor v1.9.3.2
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "PenetrableShader"
{
	Properties
	{
		[Toggle(_PENETRATION_DEFORMATION_DETAIL_ON)] _PENETRATION_DEFORMATION_DETAIL("_PENETRATION_DEFORMATION_DETAIL", Float) = 0
		[Toggle(_PENETRATION_DEFORMATION_ON)] _PENETRATION_DEFORMATION("_PENETRATION_DEFORMATION", Float) = 0
		_CompressibleDistance("CompressibleDistance", Range( 0 , 10)) = 1
		_BaseColor("BaseColor", Color) = (1,1,1,1)
		_Smoothness("Smoothness", Range( 0 , 10)) = 4
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Transparent"  "Queue" = "Transparent+0" "IgnoreProjector" = "True" }
		Cull Back
		CGINCLUDE
		#include "UnityPBSLighting.cginc"
		#include "Lighting.cginc"
		#pragma target 3.0
		#pragma shader_feature_local _PENETRATION_DEFORMATION_DETAIL_ON
		#pragma multi_compile_local __ _PENETRATION_DEFORMATION_ON
		#include "Packages/com.naelstrof-raliv.dynamic-penetration-for-games/Penetration.cginc"
		struct Input
		{
			half filler;
		};

		uniform float _CompressibleDistance;
		uniform float _Smoothness;
		uniform float4 _BaseColor;

		void vertexDataFunc( inout appdata_full v, out Input o )
		{
			UNITY_INITIALIZE_OUTPUT( Input, o );
			float3 ase_vertex3Pos = v.vertex.xyz;
			float3 temp_output_10_0_g7 = ase_vertex3Pos;
			float localGetDeformationFromPenetrators_float8_g7 = ( 0.0 );
			float4 appendResult17_g7 = (float4(temp_output_10_0_g7 , 1.0));
			float4 transform16_g7 = mul(unity_ObjectToWorld,appendResult17_g7);
			float3 worldPosition8_g7 = (transform16_g7).xyz;
			float4 uv28_g7 = v.texcoord2;
			float compressibleDistance8_g7 = _CompressibleDistance;
			float smoothness8_g7 = _Smoothness;
			float3 deformedPosition8_g7 = float3( 0,0,0 );
			{
			GetDeformationFromPenetrators_float(worldPosition8_g7,uv28_g7,compressibleDistance8_g7,smoothness8_g7,deformedPosition8_g7);
			}
			float4 appendResult21_g7 = (float4(deformedPosition8_g7 , 1.0));
			float4 transform19_g7 = mul(unity_WorldToObject,appendResult21_g7);
			#ifdef _PENETRATION_DEFORMATION_ON
				float3 staticSwitch24_g7 = (transform19_g7).xyz;
			#else
				float3 staticSwitch24_g7 = temp_output_10_0_g7;
			#endif
			float3 lerpResult55 = lerp( ase_vertex3Pos , staticSwitch24_g7 , v.color.r);
			v.vertex.xyz = lerpResult55;
			v.vertex.w = 1;
		}

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			o.Albedo = _BaseColor.rgb;
			o.Smoothness = 0.81;
			o.Alpha = _BaseColor.a;
		}

		ENDCG
		CGPROGRAM
		#pragma surface surf Standard alpha:fade keepalpha fullforwardshadows vertex:vertexDataFunc 

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
			sampler3D _DitherMaskLOD;
			struct v2f
			{
				V2F_SHADOW_CASTER;
				float3 worldPos : TEXCOORD1;
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
				Input customInputData;
				vertexDataFunc( v, customInputData );
				float3 worldPos = mul( unity_ObjectToWorld, v.vertex ).xyz;
				half3 worldNormal = UnityObjectToWorldNormal( v.normal );
				o.worldPos = worldPos;
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
				float3 worldPos = IN.worldPos;
				half3 worldViewDir = normalize( UnityWorldSpaceViewDir( worldPos ) );
				SurfaceOutputStandard o;
				UNITY_INITIALIZE_OUTPUT( SurfaceOutputStandard, o )
				surf( surfIN, o );
				#if defined( CAN_SKIP_VPOS )
				float2 vpos = IN.pos;
				#endif
				half alphaRef = tex3D( _DitherMaskLOD, float3( vpos.xy * 0.25, o.Alpha * 0.9375 ) ).a;
				clip( alphaRef - 0.01 );
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
Node;AmplifyShaderEditor.RangedFloatNode;51;-959.2499,612.3;Inherit;False;Property;_CompressibleDistance;CompressibleDistance;3;0;Create;True;0;0;0;False;0;False;1;1.81;0;10;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;52;-992.2499,729.3;Inherit;False;Property;_Smoothness;Smoothness;5;0;Create;True;0;0;0;False;0;False;4;4.07;0;10;0;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;53;-588.2499,514.3;Inherit;False;PenetrableDeformation;0;;7;7ff1b70ed2c7b9e43aecbec8a912cc8c;0;4;10;FLOAT3;0,0,0;False;11;FLOAT4;0,0,0,0;False;12;FLOAT;0;False;13;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.PosVertexDataNode;56;-649.7416,199.2038;Inherit;False;0;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.VertexColorNode;57;-671.4126,357.4;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;54;-531.6357,-83.59955;Inherit;False;Property;_BaseColor;BaseColor;4;0;Create;True;0;0;0;False;0;False;1,1,1,1;0.3988175,0.004716992,1,0.5372549;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.LerpOp;55;-339.8501,270.717;Inherit;False;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;58;-409.1961,129.8573;Inherit;False;Constant;_SurfaceSmoothness;SurfaceSmoothness;4;0;Create;True;0;0;0;False;0;False;0.81;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;0,0;Float;False;True;-1;2;ASEMaterialInspector;0;0;Standard;PenetrableShader;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;False;False;False;False;False;False;Back;0;False;;0;False;;False;0;False;;0;False;;False;0;Transparent;0;True;True;0;False;Transparent;;Transparent;All;12;all;True;True;True;True;0;False;;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;2;15;10;25;False;0.5;True;2;5;False;;10;False;;0;0;False;;0;False;;0;False;;0;False;;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Absolute;0;;-1;-1;-1;-1;0;False;0;0;False;;-1;0;False;;0;0;0;False;0.1;False;;0;False;;False;17;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;16;FLOAT4;0,0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;53;12;51;0
WireConnection;53;13;52;0
WireConnection;55;0;56;0
WireConnection;55;1;53;0
WireConnection;55;2;57;1
WireConnection;0;0;54;0
WireConnection;0;4;58;0
WireConnection;0;9;54;4
WireConnection;0;11;55;0
ASEEND*/
//CHKSM=AF8626AC9C23C088BB7CC460F45A7DF972757806