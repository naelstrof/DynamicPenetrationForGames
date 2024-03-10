// Made with Amplify Shader Editor v1.9.1.5
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "DickShader"
{
	Properties
	{
		[HideInInspector]_PenetratorRootWorld("PenetratorRootWorld", Vector) = (0,0,0,0)
		[HideInInspector]_PenetratorStartWorld("PenetratorStartWorld", Vector) = (0,0,0,0)
		[HideInInspector]_PenetratorForwardWorld("PenetratorForwardWorld", Vector) = (0,0,0,0)
		[HideInInspector]_PenetratorRightWorld("PenetratorRightWorld", Vector) = (0,0,0,0)
		[HideInInspector]_PenetratorUpWorld("PenetratorUpWorld", Vector) = (0,0,0,0)
		[HideInInspector]_SquashStretchCorrection("SquashStretchCorrection", Float) = 1
		[HideInInspector]_DistanceToHole("DistanceToHole", Float) = 0
		[HideInInspector]_PenetratorWorldLength("PenetratorWorldLength", Float) = 1
		[HideInInspector]_PenetratorOffsetLength("PenetratorOffsetLength", Float) = 0
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

		uniform float3 _PenetratorRootWorld;
		uniform float3 _PenetratorRightWorld;
		uniform float3 _PenetratorUpWorld;
		uniform float3 _PenetratorForwardWorld;
		uniform float3 _PenetratorStartWorld;
		uniform float _SquashStretchCorrection;
		uniform float _DistanceToHole;
		uniform float _PenetratorWorldLength;
		uniform float _PenetratorOffsetLength;
		uniform sampler2D _BumpMap;
		uniform float4 _BumpMap_ST;
		uniform sampler2D _MainTex;
		uniform float4 _MainTex_ST;
		uniform sampler2D _MetallicGlossMap;
		uniform float4 _MetallicGlossMap_ST;


		float3x3 ChangeOfBasis9_g4( float3 right, float3 up, float3 forward )
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
			float localToCatmullRomSpace_float56_g4 = ( 0.0 );
			float3 worldPenetratorRootPos56_g4 = _PenetratorRootWorld;
			float3 right9_g4 = _PenetratorRightWorld;
			float3 up9_g4 = _PenetratorUpWorld;
			float3 forward9_g4 = _PenetratorForwardWorld;
			float3x3 localChangeOfBasis9_g4 = ChangeOfBasis9_g4( right9_g4 , up9_g4 , forward9_g4 );
			float3 ase_vertex3Pos = v.vertex.xyz;
			float4 appendResult67_g4 = (float4(ase_vertex3Pos , 1.0));
			float4 transform66_g4 = mul(unity_ObjectToWorld,appendResult67_g4);
			float3 temp_output_68_0_g4 = (transform66_g4).xyz;
			float3 temp_output_113_0_g4 = ( temp_output_68_0_g4 - ( _PenetratorStartWorld - _PenetratorRootWorld ) );
			float3 temp_output_12_0_g4 = mul( localChangeOfBasis9_g4, ( temp_output_113_0_g4 - _PenetratorRootWorld ) );
			float3 break15_g4 = temp_output_12_0_g4;
			float temp_output_18_0_g4 = ( break15_g4.z * _SquashStretchCorrection );
			float3 appendResult26_g4 = (float3(break15_g4.x , break15_g4.y , temp_output_18_0_g4));
			float3 appendResult25_g4 = (float3(( break15_g4.x / _SquashStretchCorrection ) , ( break15_g4.y / _SquashStretchCorrection ) , temp_output_18_0_g4));
			float temp_output_17_0_g4 = ( _DistanceToHole * 0.5 );
			float smoothstepResult23_g4 = smoothstep( 0.0 , temp_output_17_0_g4 , temp_output_18_0_g4);
			float smoothstepResult22_g4 = smoothstep( _DistanceToHole , temp_output_17_0_g4 , temp_output_18_0_g4);
			float3 lerpResult31_g4 = lerp( appendResult26_g4 , appendResult25_g4 , min( smoothstepResult23_g4 , smoothstepResult22_g4 ));
			float3 lerpResult32_g4 = lerp( lerpResult31_g4 , ( temp_output_12_0_g4 + ( ( _DistanceToHole - ( _PenetratorWorldLength * ( _DistanceToHole / ( _SquashStretchCorrection * _PenetratorWorldLength ) ) ) ) * float3(0,0,1) ) ) , step( _DistanceToHole , temp_output_18_0_g4 ));
			float3 newPosition44_g4 = ( _PenetratorRootWorld + mul( transpose( localChangeOfBasis9_g4 ), lerpResult32_g4 ) );
			float3 worldPosition56_g4 = ( newPosition44_g4 + ( _PenetratorForwardWorld * _PenetratorOffsetLength ) );
			float3 worldPenetratorForward56_g4 = _PenetratorForwardWorld;
			float3 worldPenetratorUp56_g4 = _PenetratorUpWorld;
			float3 worldPenetratorRight56_g4 = _PenetratorRightWorld;
			float3 ase_vertexNormal = v.normal.xyz;
			float4 appendResult86_g4 = (float4(ase_vertexNormal , 0.0));
			float3 normalizeResult87_g4 = normalize( (mul( unity_ObjectToWorld, appendResult86_g4 )).xyz );
			float3 worldNormal56_g4 = normalizeResult87_g4;
			float4 ase_vertexTangent = v.tangent;
			float4 break93_g4 = ase_vertexTangent;
			float4 appendResult89_g4 = (float4(break93_g4.x , break93_g4.y , break93_g4.z , 0.0));
			float3 normalizeResult91_g4 = normalize( (mul( unity_ObjectToWorld, appendResult89_g4 )).xyz );
			float4 appendResult94_g4 = (float4(normalizeResult91_g4 , break93_g4.w));
			float4 worldTangent56_g4 = appendResult94_g4;
			float3 worldPositionOUT56_g4 = float3( 0,0,0 );
			float3 worldNormalOUT56_g4 = float3( 0,0,0 );
			float4 worldTangentOUT56_g4 = float4( 0,0,0,0 );
			{
			ToCatmullRomSpace_float(worldPenetratorRootPos56_g4,worldPosition56_g4,worldPenetratorForward56_g4,worldPenetratorUp56_g4,worldPenetratorRight56_g4,worldNormal56_g4,worldTangent56_g4,worldPositionOUT56_g4,worldNormalOUT56_g4,worldTangentOUT56_g4);
			}
			float4 appendResult73_g4 = (float4(worldPositionOUT56_g4 , 1.0));
			float4 transform72_g4 = mul(unity_WorldToObject,appendResult73_g4);
			v.vertex.xyz = (transform72_g4).xyz;
			v.vertex.w = 1;
			float4 appendResult75_g4 = (float4(worldNormalOUT56_g4 , 0.0));
			float3 normalizeResult76_g4 = normalize( (mul( unity_WorldToObject, appendResult75_g4 )).xyz );
			v.normal = normalizeResult76_g4;
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
Version=19105
Node;AmplifyShaderEditor.SamplerNode;2;-604.5,-149.5;Inherit;True;Property;_MainTex;MainTex;12;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;4;-582.0278,289.9303;Inherit;True;Property;_BumpMap;BumpMap;14;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;bump;Auto;True;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;3;-587.004,67.5;Inherit;True;Property;_MetallicGlossMap;MetallicGlossMap;13;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;0,0;Float;False;True;-1;2;ASEMaterialInspector;0;0;Standard;DickShader;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;False;;0;False;;False;0;False;;0;False;;False;0;Opaque;0.5;True;True;0;False;Opaque;;Geometry;All;12;all;True;True;True;True;0;False;;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;2;15;10;25;False;0.5;True;0;0;False;;0;False;;0;0;False;;0;False;;0;False;;0;False;;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Absolute;0;;-1;-1;-1;-1;0;False;0;0;False;;-1;0;False;;0;0;0;False;0.1;False;;0;False;;False;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
Node;AmplifyShaderEditor.FunctionNode;16;-769.4998,515.1;Inherit;False;PenetratorDeformation;0;;4;ac383a8a454dc764caec4e7e5816beae;0;3;64;FLOAT3;0,0,0;False;69;FLOAT3;0,0,0;False;71;FLOAT4;0,0,0,0;False;4;FLOAT3;61;FLOAT3;62;FLOAT4;63;FLOAT;0
WireConnection;0;0;2;0
WireConnection;0;1;4;0
WireConnection;0;3;3;1
WireConnection;0;4;3;4
WireConnection;0;11;16;61
WireConnection;0;12;16;62
ASEEND*/
//CHKSM=B0507448552CA20D817AB3C871D04E83121BDB66