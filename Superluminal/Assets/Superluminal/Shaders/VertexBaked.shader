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
			
				half3 specColor;
				half oneMinusReflectivity;

				// Setup surface shader output
				SurfaceOutputStandard s;
				s.Albedo = DiffuseAndSpecularFromMetallic(color.rgb, _Metallic, specColor, oneMinusReflectivity);
				s.Normal = normalize(i.normal);
				s.Emission = _EmissionColor;
				s.Metallic = _Metallic;
				s.Smoothness = _Glossiness;
				s.Occlusion = 1.0;
				s.Alpha = color.a;
	
				// Setup GI input
				UnityGIInput d;
				UNITY_INITIALIZE_OUTPUT(UnityGIInput, d);

				d.worldPos = i.worldPos;
				d.worldViewDir = i.viewDir;

				d.probeHDR[0] = unity_SpecCube0_HDR;
				d.probeHDR[1] = unity_SpecCube1_HDR;

				// Setup glossy environment for GI
				Unity_GlossyEnvironmentData glossEnv = UnityGlossyEnvironmentSetup(s.Smoothness, i.viewDir, normalize(i.worldNormal), specColor);

				// Setup GI output data
				UnityGI gi;
				UNITY_INITIALIZE_OUTPUT(UnityGI, gi);

				gi.indirect.diffuse = i.indirectDiffuse;
				gi.indirect.specular = UnityGI_IndirectSpecular(d, s.Occlusion, glossEnv);

				half4 output = LightingStandard(s, i.viewDir, gi);
				output.rgb += _EmissionColor;

				// Apply fog
				UNITY_APPLY_FOG(i.fogCoord, output.rgb);
				
				return output;
			}
			ENDCG
		}
	}
}
