// Made with Amplify Shader Editor v1.9.3.2
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "DickShader"
{
	Properties
	{
		[HideInInspector]_DickRootWorld("DickRootWorld", Vector) = (0,0,0,0)
		[HideInInspector]_DickForwardWorld("DickForwardWorld", Vector) = (0,0,0,0)
		[HideInInspector]_DickRightWorld("DickRightWorld", Vector) = (0,0,0,0)
		[HideInInspector]_DickUpWorld("DickUpWorld", Vector) = (0,0,0,0)
		[HideInInspector]_SquashStretchCorrection("_SquashStretchCorrection", Float) = 1
		[HideInInspector]_DistanceToHole("_DistanceToHole", Float) = 0
		[HideInInspector]_DickWorldLength("_DickWorldLength", Float) = 1
		[HideInInspector]_DickOffsetLength("_DickOffsetLength", Float) = 0
		_MainTex("MainTex", 2D) = "white" {}
		_MetallicGlossMap("MetallicGlossMap", 2D) = "white" {}
		_BumpMap("BumpMap", 2D) = "bump" {}
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" }
		Cull Back
		CGPROGRAM
		#include "UnityShaderVariables.cginc"
		#pragma target 3.0
		#include "Packages/com.naelstrof-raliv.dynamic-penetration-for-games/Penetration.cginc"
		#pragma surface surf Standard keepalpha addshadow fullforwardshadows vertex:vertexDataFunc 
		struct Input
		{
			float2 uv_texcoord;
		};

		uniform float3 _DickRootWorld;
		uniform float3 _DickRightWorld;
		uniform float3 _DickUpWorld;
		uniform float3 _DickForwardWorld;
		uniform float _SquashStretchCorrection;
		uniform float _DistanceToHole;
		uniform float _DickWorldLength;
		uniform float _DickOffsetLength;
		uniform sampler2D _BumpMap;
		uniform float4 _BumpMap_ST;
		uniform sampler2D _MainTex;
		uniform float4 _MainTex_ST;
		uniform sampler2D _MetallicGlossMap;
		uniform float4 _MetallicGlossMap_ST;


		float3x3 ChangeOfBasis9_g1( float3 right, float3 up, float3 forward )
		{
			float3x3 basisTransform = 0;
			    basisTransform[0][0] = right.x;
			    basisTransform[0][1] = right.y;
			    basisTransform[0][2] = right.z;
			    basisTransform[1][0] = up.x;
			    basisTransform[1][1] = up.y;
			    basisTransform[1][2] = up.z;
			    basisTransform[2][0] = forward.x;
			    basisTransform[2][1] = forward.y;
			    basisTransform[2][2] = forward.z;
			return basisTransform;
		}


		void vertexDataFunc( inout appdata_full v, out Input o )
		{
			UNITY_INITIALIZE_OUTPUT( Input, o );
			float localToCatmullRomSpace_float56_g1 = ( 0.0 );
			float3 worldDickRootPos56_g1 = _DickRootWorld;
			float3 right9_g1 = _DickRightWorld;
			float3 up9_g1 = _DickUpWorld;
			float3 forward9_g1 = _DickForwardWorld;
			float3x3 localChangeOfBasis9_g1 = ChangeOfBasis9_g1( right9_g1 , up9_g1 , forward9_g1 );
			float3 ase_vertex3Pos = v.vertex.xyz;
			float4 appendResult67_g1 = (float4(ase_vertex3Pos , 1.0));
			float4 transform66_g1 = mul(unity_ObjectToWorld,appendResult67_g1);
			float3 temp_output_68_0_g1 = (transform66_g1).xyz;
			float3 temp_output_12_0_g1 = mul( localChangeOfBasis9_g1, ( temp_output_68_0_g1 - _DickRootWorld ) );
			float3 break15_g1 = temp_output_12_0_g1;
			float temp_output_18_0_g1 = ( break15_g1.z * _SquashStretchCorrection );
			float3 appendResult26_g1 = (float3(break15_g1.x , break15_g1.y , temp_output_18_0_g1));
			float3 appendResult25_g1 = (float3(( break15_g1.x / _SquashStretchCorrection ) , ( break15_g1.y / _SquashStretchCorrection ) , temp_output_18_0_g1));
			float temp_output_17_0_g1 = ( _DistanceToHole * 0.5 );
			float smoothstepResult23_g1 = smoothstep( 0.0 , temp_output_17_0_g1 , temp_output_18_0_g1);
			float smoothstepResult22_g1 = smoothstep( _DistanceToHole , temp_output_17_0_g1 , temp_output_18_0_g1);
			float3 lerpResult31_g1 = lerp( appendResult26_g1 , appendResult25_g1 , min( smoothstepResult23_g1 , smoothstepResult22_g1 ));
			float3 lerpResult32_g1 = lerp( lerpResult31_g1 , ( temp_output_12_0_g1 + ( ( _DistanceToHole - ( _DickWorldLength * ( _DistanceToHole / ( _SquashStretchCorrection * _DickWorldLength ) ) ) ) * float3(0,0,1) ) ) , step( _DistanceToHole , temp_output_18_0_g1 ));
			float3 newPosition44_g1 = ( _DickRootWorld + mul( transpose( localChangeOfBasis9_g1 ), lerpResult32_g1 ) );
			float3 worldPosition56_g1 = ( newPosition44_g1 + ( _DickForwardWorld * _DickOffsetLength ) );
			float3 worldDickForward56_g1 = _DickForwardWorld;
			float3 worldDickUp56_g1 = _DickUpWorld;
			float3 worldDickRight56_g1 = _DickRightWorld;
			float3 ase_vertexNormal = v.normal.xyz;
			float4 appendResult86_g1 = (float4(ase_vertexNormal , 0.0));
			float3 normalizeResult87_g1 = normalize( (mul( unity_ObjectToWorld, appendResult86_g1 )).xyz );
			float3 worldNormal56_g1 = normalizeResult87_g1;
			float4 ase_vertexTangent = v.tangent;
			float4 break93_g1 = ase_vertexTangent;
			float4 appendResult89_g1 = (float4(break93_g1.x , break93_g1.y , break93_g1.z , 0.0));
			float3 normalizeResult91_g1 = normalize( (mul( unity_ObjectToWorld, appendResult89_g1 )).xyz );
			float4 appendResult94_g1 = (float4(normalizeResult91_g1 , break93_g1.w));
			float4 worldTangent56_g1 = appendResult94_g1;
			float3 worldPositionOUT56_g1 = float3( 0,0,0 );
			float3 worldNormalOUT56_g1 = float3( 0,0,0 );
			float4 worldTangentOUT56_g1 = float4( 0,0,0,0 );
			{
			ToCatmullRomSpace_float(worldDickRootPos56_g1,worldPosition56_g1,worldDickForward56_g1,worldDickUp56_g1,worldDickRight56_g1,worldNormal56_g1,worldTangent56_g1,worldPositionOUT56_g1,worldNormalOUT56_g1,worldTangentOUT56_g1);
			}
			float4 appendResult73_g1 = (float4(worldPositionOUT56_g1 , 1.0));
			float4 transform72_g1 = mul(unity_WorldToObject,appendResult73_g1);
			v.vertex.xyz = (transform72_g1).xyz;
			v.vertex.w = 1;
			float4 appendResult75_g1 = (float4(worldNormalOUT56_g1 , 0.0));
			float3 normalizeResult76_g1 = normalize( (mul( unity_WorldToObject, appendResult75_g1 )).xyz );
			v.normal = normalizeResult76_g1;
			float4 break79_g1 = worldTangentOUT56_g1;
			float4 appendResult77_g1 = (float4(break79_g1.x , break79_g1.y , break79_g1.z , 0.0));
			float3 normalizeResult80_g1 = normalize( (mul( unity_WorldToObject, appendResult77_g1 )).xyz );
			float4 appendResult83_g1 = (float4(normalizeResult80_g1 , break79_g1.w));
			v.tangent = appendResult83_g1;
		}

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float2 uv_BumpMap = i.uv_texcoord * _BumpMap_ST.xy + _BumpMap_ST.zw;
			o.Normal = UnpackNormal( tex2D( _BumpMap, uv_BumpMap ) );
			float2 uv_MainTex = i.uv_texcoord * _MainTex_ST.xy + _MainTex_ST.zw;
			o.Albedo = tex2D( _MainTex, uv_MainTex ).rgb;
			float2 uv_MetallicGlossMap = i.uv_texcoord * _MetallicGlossMap_ST.xy + _MetallicGlossMap_ST.zw;
			float4 tex2DNode3 = tex2D( _MetallicGlossMap, uv_MetallicGlossMap );
			o.Metallic = tex2DNode3.r;
			o.Smoothness = tex2DNode3.a;
			o.Alpha = 1;
		}

		ENDCG
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=19302
Node;AmplifyShaderEditor.SamplerNode;2;-604.5,-149.5;Inherit;True;Property;_MainTex;MainTex;11;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;4;-582.0278,289.9303;Inherit;True;Property;_BumpMap;BumpMap;13;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;bump;Auto;True;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;3;-587.004,67.5;Inherit;True;Property;_MetallicGlossMap;MetallicGlossMap;12;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.FunctionNode;10;-660.4998,516.1;Inherit;False;PenetratorDeformation;0;;1;ac383a8a454dc764caec4e7e5816beae;0;3;64;FLOAT3;0,0,0;False;69;FLOAT3;0,0,0;False;71;FLOAT4;0,0,0,0;False;4;FLOAT3;61;FLOAT3;62;FLOAT4;63;FLOAT;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;0,0;Float;False;True;-1;2;ASEMaterialInspector;0;0;Standard;DickShader;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;False;;0;False;;False;0;False;;0;False;;False;0;Opaque;0.5;True;True;0;False;Opaque;;Geometry;All;12;all;True;True;True;True;0;False;;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;2;15;10;25;False;0.5;True;0;0;False;;0;False;;0;0;False;;0;False;;0;False;;0;False;;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Absolute;0;;-1;-1;-1;-1;0;False;0;0;False;;-1;0;False;;0;0;0;False;0.1;False;;0;False;;False;17;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;16;FLOAT4;0,0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;0;0;2;0
WireConnection;0;1;4;0
WireConnection;0;3;3;1
WireConnection;0;4;3;4
WireConnection;0;11;10;61
WireConnection;0;12;10;62
WireConnection;0;16;10;63
ASEEND*/
//CHKSM=C45689C30E595DD836EE3983A2E080C37BC1B4C0