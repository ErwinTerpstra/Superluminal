Shader "Superluminal/Wireframe"
{
	SubShader
	{
		Pass
		{
			Blend SrcAlpha OneMinusSrcAlpha
			BindChannels { Bind "Color", color }
			ZWrite Off
			ZTest Always
			Cull Front
			Fog { Mode Off }
		}
	}
}