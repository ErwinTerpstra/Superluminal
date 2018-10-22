Shader "Superluminal/VertexBaked"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Color ("Color", Color) = (1.0, 1.0, 1.0, 1.0)
		_EmissionColor("Emission", Color) = (0.0, 0.0, 0.0, 1.0)

		_Glossiness("Smoothness", Range(0,1)) = 0.5
		_Metallic("Metallic", Range(0,1)) = 0.0
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Blend One OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#pragma multi_compile_fog
			
			#include "UnityCG.cginc"
			#include "UnityPBSLighting.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float4 color : COLOR;
				float3 normal : NORMAL;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float3 normal : NORMAL;
				float2 uv : TEXCOORD0;
				float3 viewDir : TEXCOORD1;
				float3 worldPos : TEXCOORD2;
				float3 worldNormal : TEXCOORD3;
				float3 indirectDiffuse : TEXCOORD4;

				UNITY_FOG_COORDS(7)
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;

			fixed4 _Color;
			fixed4 _EmissionColor;

			half _Glossiness;
			half _Metallic;
			
			UnityGIInput SetupGIInput(UnityLight light, float3 worldPos, half3 viewDir)
			{
				UnityGIInput d;
				UNITY_INITIALIZE_OUTPUT(UnityGIInput, d);

				d.light = light;
				d.worldPos = worldPos;
				d.worldViewDir = viewDir;

				d.probeHDR[0] = unity_SpecCube0_HDR;
				d.probeHDR[1] = unity_SpecCube1_HDR;

#if defined(UNITY_SPECCUBE_BLENDING) || defined(UNITY_SPECCUBE_BOX_PROJECTION)
				d.boxMin[0] = unity_SpecCube0_BoxMin; // .w holds lerp value for blending
#endif
#ifdef UNITY_SPECCUBE_BOX_PROJECTION
				d.boxMax[0] = unity_SpecCube0_BoxMax;
				d.probePosition[0] = unity_SpecCube0_ProbePosition;
				d.boxMax[1] = unity_SpecCube1_BoxMax;
				d.boxMin[1] = unity_SpecCube1_BoxMin;
				d.probePosition[1] = unity_SpecCube1_ProbePosition;
#endif

				return d;
			}

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.normal = v.normal;
				o.indirectDiffuse = v.color.rgb;
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);

				float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
				o.worldPos = worldPos.xyz;
				o.worldNormal = UnityObjectToWorldNormal(v.normal);
				o.viewDir = normalize(_WorldSpaceCameraPos - worldPos.xyz);

				UNITY_TRANSFER_FOG(o, o.vertex);

				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 color = tex2D(_MainTex, i.uv) * _Color;
			
				// Setup surface shader output
				SurfaceOutputStandard s;
				s.Albedo = color.rgb;
				s.Normal = normalize(i.normal);
				s.Emission = _EmissionColor;
				s.Metallic = _Metallic;
				s.Smoothness = _Glossiness;
				s.Occlusion = 1.0;
				s.Alpha = color.a;

				// Setup main light
				UnityLight mainLight;
				mainLight.color = _LightColor0.rgb;
				mainLight.dir = _WorldSpaceLightPos0.xyz;

				// Setup GI input
				UnityGIInput giInput = SetupGIInput(mainLight, i.worldPos, i.viewDir);

				// Setup glossy environment for GI
				
				// Specular color is not used by UnityGlossyEnvironmentSetup, so we don't need to calculate it
				/*
				half3 specColor;
				half oneMinusReflectivity;
				DiffuseAndSpecularFromMetallic(color.rgb, _Metallic, specColor, oneMinusReflectivity);
				Unity_GlossyEnvironmentData glossEnv = UnityGlossyEnvironmentSetup(s.Smoothness, i.viewDir, i.worldNormal, specColor);
				*/

				Unity_GlossyEnvironmentData glossEnv = UnityGlossyEnvironmentSetup(s.Smoothness, i.viewDir, i.worldNormal, float3(0, 0, 0));

				// Setup GI output data
				UnityGI giOutput;
				UNITY_INITIALIZE_OUTPUT(UnityGI, giOutput);

				giOutput.light = mainLight;
				giOutput.indirect.diffuse = i.indirectDiffuse;
				giOutput.indirect.specular = UnityGI_IndirectSpecular(giInput, s.Occlusion * 0.5, glossEnv);

				half4 output = LightingStandard(s, i.viewDir, giOutput);
				output.rgb += _EmissionColor;

				// Apply fog
				UNITY_APPLY_FOG(i.fogCoord, output.rgb);
				
				return output;
			}
			ENDCG
		}
	}
}
