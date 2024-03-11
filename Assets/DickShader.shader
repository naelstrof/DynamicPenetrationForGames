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
		_TruncateLength("TruncateLength", Range( 0 , 1)) = 1
		[HideInInspector]_SquashStretchCorrection("SquashStretchCorrection", Float) = 1
		[HideInInspector]_DistanceToHole("DistanceToHole", Float) = 0
		[HideInInspector]_PenetratorWorldLength("PenetratorWorldLength", Float) = 1
		[HideInInspector]_PenetratorOffsetLength("PenetratorOffsetLength", Float) = 0
		[Toggle(_TRUNCATESPHERIZE_ON)] _TruncateSpherize("TruncateSpherize", Float) = 0
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
		#pragma shader_feature_local _TRUNCATESPHERIZE_ON
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
		uniform float _TruncateLength;
		uniform float _PenetratorOffsetLength;
		uniform sampler2D _BumpMap;
		uniform float4 _BumpMap_ST;
		uniform sampler2D _MainTex;
		uniform float4 _MainTex_ST;
		uniform sampler2D _MetallicGlossMap;
		uniform float4 _MetallicGlossMap_ST;


		float3x3 ChangeOfBasis169_g23( float3 right, float3 up, float3 forward )
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


		float3x3 ChangeOfBasis9_g23( float3 right, float3 up, float3 forward )
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
			float localToCatmullRomSpace_float56_g23 = ( 0.0 );
			float3 penetratorRootWorld122_g23 = _PenetratorRootWorld;
			float3 worldPenetratorRootPos56_g23 = penetratorRootWorld122_g23;
			float3 penetratorRightWorld139_g23 = _PenetratorRightWorld;
			float3 right169_g23 = penetratorRightWorld139_g23;
			float3 penetratorUpWorld134_g23 = _PenetratorUpWorld;
			float3 up169_g23 = penetratorUpWorld134_g23;
			float3 penetratorForwardWorld126_g23 = _PenetratorForwardWorld;
			float3 forward169_g23 = penetratorForwardWorld126_g23;
			float3x3 localChangeOfBasis169_g23 = ChangeOfBasis169_g23( right169_g23 , up169_g23 , forward169_g23 );
			float3 right9_g23 = penetratorRightWorld139_g23;
			float3 up9_g23 = penetratorUpWorld134_g23;
			float3 forward9_g23 = penetratorForwardWorld126_g23;
			float3x3 localChangeOfBasis9_g23 = ChangeOfBasis9_g23( right9_g23 , up9_g23 , forward9_g23 );
			float3 ase_vertex3Pos = v.vertex.xyz;
			float4 appendResult67_g23 = (float4(ase_vertex3Pos , 1.0));
			float4 transform66_g23 = mul(unity_ObjectToWorld,appendResult67_g23);
			float3 localPenetratorSpaceVertexPosition142_g23 = ( (transform66_g23).xyz - ( _PenetratorStartWorld - penetratorRootWorld122_g23 ) );
			float3 temp_output_12_0_g23 = mul( localChangeOfBasis9_g23, ( localPenetratorSpaceVertexPosition142_g23 - penetratorRootWorld122_g23 ) );
			float3 break15_g23 = temp_output_12_0_g23;
			float temp_output_18_0_g23 = ( break15_g23.z * _SquashStretchCorrection );
			float3 appendResult26_g23 = (float3(break15_g23.x , break15_g23.y , temp_output_18_0_g23));
			float3 appendResult25_g23 = (float3(( break15_g23.x / _SquashStretchCorrection ) , ( break15_g23.y / _SquashStretchCorrection ) , temp_output_18_0_g23));
			float temp_output_17_0_g23 = ( _DistanceToHole * 0.5 );
			float smoothstepResult23_g23 = smoothstep( 0.0 , temp_output_17_0_g23 , temp_output_18_0_g23);
			float smoothstepResult22_g23 = smoothstep( _DistanceToHole , temp_output_17_0_g23 , temp_output_18_0_g23);
			float3 lerpResult31_g23 = lerp( appendResult26_g23 , appendResult25_g23 , min( smoothstepResult23_g23 , smoothstepResult22_g23 ));
			float3 lerpResult32_g23 = lerp( lerpResult31_g23 , ( temp_output_12_0_g23 + ( ( _DistanceToHole - ( ( _DistanceToHole / ( _SquashStretchCorrection * _PenetratorWorldLength ) ) * _PenetratorWorldLength ) ) * float3(0,0,1) ) ) , step( _DistanceToHole , temp_output_18_0_g23 ));
			float3 squashStretchedPosition44_g23 = lerpResult32_g23;
			float3 temp_output_150_0_g23 = ( float3(0,0,1) * _TruncateLength );
			float3 temp_output_149_0_g23 = ( squashStretchedPosition44_g23 - temp_output_150_0_g23 );
			float3 normalizeResult156_g23 = normalize( temp_output_149_0_g23 );
			float3 lerpResult152_g23 = lerp( temp_output_149_0_g23 , ( normalizeResult156_g23 * min( length( temp_output_149_0_g23 ) , 0.1 ) ) , saturate( ( temp_output_149_0_g23.z * 10.0 ) ));
			#ifdef _TRUNCATESPHERIZE_ON
				float3 staticSwitch116_g23 = ( lerpResult152_g23 + temp_output_150_0_g23 );
			#else
				float3 staticSwitch116_g23 = squashStretchedPosition44_g23;
			#endif
			float3 TruncatedPosition147_g23 = ( penetratorRootWorld122_g23 + mul( transpose( localChangeOfBasis169_g23 ), staticSwitch116_g23 ) );
			float3 worldPosition56_g23 = ( TruncatedPosition147_g23 + ( penetratorForwardWorld126_g23 * _PenetratorOffsetLength ) );
			float3 worldPenetratorForward56_g23 = penetratorForwardWorld126_g23;
			float3 worldPenetratorUp56_g23 = penetratorUpWorld134_g23;
			float3 worldPenetratorRight56_g23 = penetratorRightWorld139_g23;
			float3 ase_vertexNormal = v.normal.xyz;
			float4 appendResult86_g23 = (float4(ase_vertexNormal , 0.0));
			float3 normalizeResult87_g23 = normalize( (mul( unity_ObjectToWorld, appendResult86_g23 )).xyz );
			float3 worldNormal56_g23 = normalizeResult87_g23;
			float4 ase_vertexTangent = v.tangent;
			float4 break93_g23 = ase_vertexTangent;
			float4 appendResult89_g23 = (float4(break93_g23.x , break93_g23.y , break93_g23.z , 0.0));
			float3 normalizeResult91_g23 = normalize( (mul( unity_ObjectToWorld, appendResult89_g23 )).xyz );
			float4 appendResult94_g23 = (float4(normalizeResult91_g23 , break93_g23.w));
			float4 worldTangent56_g23 = appendResult94_g23;
			float3 worldPositionOUT56_g23 = float3( 0,0,0 );
			float3 worldNormalOUT56_g23 = float3( 0,0,0 );
			float4 worldTangentOUT56_g23 = float4( 0,0,0,0 );
			{
			ToCatmullRomSpace_float(worldPenetratorRootPos56_g23,worldPosition56_g23,worldPenetratorForward56_g23,worldPenetratorUp56_g23,worldPenetratorRight56_g23,worldNormal56_g23,worldTangent56_g23,worldPositionOUT56_g23,worldNormalOUT56_g23,worldTangentOUT56_g23);
			}
			float4 appendResult73_g23 = (float4(worldPositionOUT56_g23 , 1.0));
			float4 transform72_g23 = mul(unity_WorldToObject,appendResult73_g23);
			v.vertex.xyz = (transform72_g23).xyz;
			v.vertex.w = 1;
			float4 appendResult75_g23 = (float4(worldNormalOUT56_g23 , 0.0));
			float3 normalizeResult76_g23 = normalize( (mul( unity_WorldToObject, appendResult75_g23 )).xyz );
			v.normal = normalizeResult76_g23;
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
Node;AmplifyShaderEditor.SamplerNode;2;-604.5,-149.5;Inherit;True;Property;_MainTex;MainTex;14;0;Create;True;0;0;0;False;0;False;-1;None;f9c4c3b88bc300c4ca69f83d9e36bf9b;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;4;-582.0278,289.9303;Inherit;True;Property;_BumpMap;BumpMap;16;0;Create;True;0;0;0;False;0;False;-1;None;0248ebb3931f38c4ea6664623270933a;True;0;True;bump;Auto;True;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;3;-587.004,67.5;Inherit;True;Property;_MetallicGlossMap;MetallicGlossMap;15;0;Create;True;0;0;0;False;0;False;-1;None;f920b22d535aa2546a12d783e1be0338;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;0,0;Float;False;True;-1;2;ASEMaterialInspector;0;0;Standard;DickShader;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;False;;0;False;;False;0;False;;0;False;;False;0;Opaque;0.5;True;True;0;False;Opaque;;Geometry;All;12;all;True;True;True;True;0;False;;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;2;15;10;25;False;0.5;True;0;0;False;;0;False;;0;0;False;;0;False;;0;False;;0;False;;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Absolute;0;;-1;-1;-1;-1;0;False;0;0;False;;-1;0;False;;0;0;0;False;0.1;False;;0;False;;False;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
Node;AmplifyShaderEditor.FunctionNode;35;-769.4998,515.1;Inherit;False;PenetratorDeformation;0;;23;ac383a8a454dc764caec4e7e5816beae;0;3;64;FLOAT3;0,0,0;False;69;FLOAT3;0,0,0;False;71;FLOAT4;0,0,0,0;False;4;FLOAT3;61;FLOAT3;62;FLOAT4;63;FLOAT;0
WireConnection;0;0;2;0
WireConnection;0;1;4;0
WireConnection;0;3;3;1
WireConnection;0;4;3;4
WireConnection;0;11;35;61
WireConnection;0;12;35;62
ASEEND*/
//CHKSM=51CE23F1C4C33556004C93198934D7826B3CF8BF