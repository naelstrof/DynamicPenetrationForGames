// Made with Amplify Shader Editor v1.9.3.2
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "DickShader"
{
	Properties
	{
		_Cutoff( "Mask Clip Value", Float ) = 0
		[HideInInspector]_PenetratorRootWorld("PenetratorRootWorld", Vector) = (0,0,0,0)
		[HideInInspector]_PenetratorStartWorld("PenetratorStartWorld", Vector) = (0,0,0,0)
		[HideInInspector]_PenetratorForwardWorld("PenetratorForwardWorld", Vector) = (0,0,0,0)
		[HideInInspector]_PenetratorRightWorld("PenetratorRightWorld", Vector) = (0,0,0,0)
		[HideInInspector]_PenetratorUpWorld("PenetratorUpWorld", Vector) = (0,0,0,0)
		[HideInInspector]_TruncateLength("TruncateLength", Float) = 999
		[HideInInspector]_StartClip("StartClip", Float) = 0
		[HideInInspector]_EndClip("EndClip", Float) = 0
		[HideInInspector]_SquashStretchCorrection("SquashStretchCorrection", Float) = 1
		[HideInInspector]_DistanceToHole("DistanceToHole", Float) = 0
		[HideInInspector]_PenetratorWorldLength("PenetratorWorldLength", Float) = 1
		[HideInInspector]_PenetratorOffsetLength("PenetratorOffsetLength", Float) = 0
		[Toggle(_TRUNCATESPHERIZE_ON)] _TruncateSpherize("TruncateSpherize", Float) = 0
		_GirthRadius("GirthRadius", Float) = 0.1
		_MainTex("MainTex", 2D) = "white" {}
		_MetallicGlossMap("MetallicGlossMap", 2D) = "gray" {}
		_BumpMap("BumpMap", 2D) = "bump" {}
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "TransparentCutout"  "Queue" = "AlphaTest+0" }
		Cull Back
		CGPROGRAM
		#include "UnityShaderVariables.cginc"
		#pragma target 3.0
		#pragma multi_compile_local __ _TRUNCATESPHERIZE_ON
		#include "Packages/com.naelstrof-raliv.dynamic-penetration-for-games/Penetration.cginc"
		#pragma surface surf Standard keepalpha addshadow fullforwardshadows vertex:vertexDataFunc 
		struct Input
		{
			float2 uv_texcoord;
			float3 vertexToFrag189_g1;
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
		uniform float _GirthRadius;
		uniform float _PenetratorOffsetLength;
		uniform sampler2D _BumpMap;
		uniform float4 _BumpMap_ST;
		uniform sampler2D _MainTex;
		uniform float4 _MainTex_ST;
		uniform sampler2D _MetallicGlossMap;
		uniform float4 _MetallicGlossMap_ST;
		uniform float _StartClip;
		uniform float _EndClip;
		uniform float _Cutoff = 0;


		float3x3 ChangeOfBasis169_g1( float3 right, float3 up, float3 forward )
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
			float3 penetratorRootWorld122_g1 = _PenetratorRootWorld;
			float3 worldPenetratorRootPos56_g1 = penetratorRootWorld122_g1;
			float3 penetratorRightWorld139_g1 = _PenetratorRightWorld;
			float3 right169_g1 = penetratorRightWorld139_g1;
			float3 penetratorUpWorld134_g1 = _PenetratorUpWorld;
			float3 up169_g1 = penetratorUpWorld134_g1;
			float3 penetratorForwardWorld126_g1 = _PenetratorForwardWorld;
			float3 forward169_g1 = penetratorForwardWorld126_g1;
			float3x3 localChangeOfBasis169_g1 = ChangeOfBasis169_g1( right169_g1 , up169_g1 , forward169_g1 );
			float3 right9_g1 = penetratorRightWorld139_g1;
			float3 up9_g1 = penetratorUpWorld134_g1;
			float3 forward9_g1 = penetratorForwardWorld126_g1;
			float3x3 localChangeOfBasis9_g1 = ChangeOfBasis9_g1( right9_g1 , up9_g1 , forward9_g1 );
			float3 ase_vertex3Pos = v.vertex.xyz;
			float4 appendResult67_g1 = (float4(ase_vertex3Pos , 1.0));
			float4 transform66_g1 = mul(unity_ObjectToWorld,appendResult67_g1);
			float3 localPenetratorSpaceVertexPosition142_g1 = ( (transform66_g1).xyz - ( _PenetratorStartWorld - penetratorRootWorld122_g1 ) );
			float3 temp_output_12_0_g1 = mul( localChangeOfBasis9_g1, ( localPenetratorSpaceVertexPosition142_g1 - penetratorRootWorld122_g1 ) );
			float3 break15_g1 = temp_output_12_0_g1;
			float temp_output_18_0_g1 = ( break15_g1.z * _SquashStretchCorrection );
			float3 appendResult26_g1 = (float3(break15_g1.x , break15_g1.y , temp_output_18_0_g1));
			float3 appendResult25_g1 = (float3(( break15_g1.x / _SquashStretchCorrection ) , ( break15_g1.y / _SquashStretchCorrection ) , temp_output_18_0_g1));
			float distanceToHole180_g1 = _DistanceToHole;
			float temp_output_17_0_g1 = ( distanceToHole180_g1 * 0.5 );
			float smoothstepResult23_g1 = smoothstep( 0.0 , temp_output_17_0_g1 , temp_output_18_0_g1);
			float smoothstepResult22_g1 = smoothstep( distanceToHole180_g1 , temp_output_17_0_g1 , temp_output_18_0_g1);
			float3 lerpResult31_g1 = lerp( appendResult26_g1 , appendResult25_g1 , min( smoothstepResult23_g1 , smoothstepResult22_g1 ));
			float3 lerpResult32_g1 = lerp( lerpResult31_g1 , ( temp_output_12_0_g1 + ( ( distanceToHole180_g1 - ( ( distanceToHole180_g1 / ( _SquashStretchCorrection * _PenetratorWorldLength ) ) * _PenetratorWorldLength ) ) * float3(0,0,1) ) ) , step( distanceToHole180_g1 , temp_output_18_0_g1 ));
			float3 squashStretchedPosition44_g1 = lerpResult32_g1;
			float3 temp_output_150_0_g1 = ( float3(0,0,1) * _TruncateLength );
			float3 temp_output_149_0_g1 = ( squashStretchedPosition44_g1 - temp_output_150_0_g1 );
			float3 normalizeResult156_g1 = normalize( temp_output_149_0_g1 );
			float3 lerpResult152_g1 = lerp( temp_output_149_0_g1 , ( normalizeResult156_g1 * min( length( temp_output_149_0_g1 ) , _GirthRadius ) ) , saturate( ( temp_output_149_0_g1.z * ( 1.0 / _GirthRadius ) ) ));
			#ifdef _TRUNCATESPHERIZE_ON
				float3 staticSwitch116_g1 = ( lerpResult152_g1 + temp_output_150_0_g1 );
			#else
				float3 staticSwitch116_g1 = squashStretchedPosition44_g1;
			#endif
			float3 TruncatedPosition147_g1 = ( penetratorRootWorld122_g1 + mul( transpose( localChangeOfBasis169_g1 ), staticSwitch116_g1 ) );
			float3 worldPosition56_g1 = ( TruncatedPosition147_g1 + ( penetratorForwardWorld126_g1 * _PenetratorOffsetLength ) );
			float3 worldPenetratorForward56_g1 = penetratorForwardWorld126_g1;
			float3 worldPenetratorUp56_g1 = penetratorUpWorld134_g1;
			float3 worldPenetratorRight56_g1 = penetratorRightWorld139_g1;
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
			ToCatmullRomSpace_float(worldPenetratorRootPos56_g1,worldPosition56_g1,worldPenetratorForward56_g1,worldPenetratorUp56_g1,worldPenetratorRight56_g1,worldNormal56_g1,worldTangent56_g1,worldPositionOUT56_g1,worldNormalOUT56_g1,worldTangentOUT56_g1);
			}
			float4 appendResult73_g1 = (float4(worldPositionOUT56_g1 , 1.0));
			float4 transform72_g1 = mul(unity_WorldToObject,appendResult73_g1);
			v.vertex.xyz = (transform72_g1).xyz;
			v.vertex.w = 1;
			float4 appendResult75_g1 = (float4(worldNormalOUT56_g1 , 0.0));
			float3 normalizeResult76_g1 = normalize( (mul( unity_WorldToObject, appendResult75_g1 )).xyz );
			v.normal = normalizeResult76_g1;
			o.vertexToFrag189_g1 = squashStretchedPosition44_g1;
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
			clip( ( 1.0 - ( step( _StartClip , i.vertexToFrag189_g1.z ) * step( i.vertexToFrag189_g1.z , _EndClip ) ) ) - _Cutoff );
		}

		ENDCG
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=19302
Node;AmplifyShaderEditor.SamplerNode;3;-587.004,67.5;Inherit;True;Property;_MetallicGlossMap;MetallicGlossMap;17;0;Create;True;0;0;0;False;0;False;-1;None;f920b22d535aa2546a12d783e1be0338;True;0;False;gray;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;2;-604.5,-149.5;Inherit;True;Property;_MainTex;MainTex;16;0;Create;True;0;0;0;False;0;False;-1;None;f9c4c3b88bc300c4ca69f83d9e36bf9b;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;4;-582.0278,289.9303;Inherit;True;Property;_BumpMap;BumpMap;18;0;Create;True;0;0;0;False;0;False;-1;None;0248ebb3931f38c4ea6664623270933a;True;0;True;bump;Auto;True;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.FunctionNode;66;-769.4998,515.1;Inherit;False;PenetratorDeformation;1;;1;ac383a8a454dc764caec4e7e5816beae;0;3;64;FLOAT3;0,0,0;False;69;FLOAT3;0,0,0;False;71;FLOAT4;0,0,0,0;False;4;FLOAT3;61;FLOAT3;62;FLOAT4;63;FLOAT;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;0,0;Float;False;True;-1;2;ASEMaterialInspector;0;0;Standard;DickShader;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;False;;0;False;;False;0;False;;0;False;;False;0;Masked;0;True;True;0;False;TransparentCutout;;AlphaTest;All;12;all;True;True;True;True;0;False;;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;2;15;10;25;False;0.5;True;0;0;False;;0;False;;0;0;False;;0;False;;0;False;;0;False;;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Absolute;0;;0;-1;-1;-1;0;False;0;0;False;;-1;0;False;;0;0;0;False;0.1;False;;0;False;;False;17;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;16;FLOAT4;0,0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;0;0;2;0
WireConnection;0;1;4;0
WireConnection;0;3;3;1
WireConnection;0;4;3;4
WireConnection;0;10;66;0
WireConnection;0;11;66;61
WireConnection;0;12;66;62
ASEEND*/
//CHKSM=FC69F790967E78B5ED77530F418BD7F248F52CD5