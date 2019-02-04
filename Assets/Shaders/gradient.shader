Shader "Unlit/Gradient"
{
	Properties
	{
		_ColorStart("ColorStart", Color) = (1.0,0.0,0.0,0.0)
		_ColorMiddle("ColorMiddle", Color) = (0.0,1.0,0.0,0.0)
		_ColorEnd("ColorEnd", Color) = (0.0,0.0,1.0,0.0)
		_Steps("Steps", Range(0,1)) = 0.1
		//_MainTex ("Texture", 2D) = "white" {}
	}

	CGINCLUDE
	half3 _ColorStart;
	half3 _ColorMiddle;
	half3 _ColorEnd;
	half _Steps;
	
	struct appdata
	{
		float4 position : POSITION;
		float3 texcoord : TEXCOORD0;
	};

	struct v2f
	{
		float4 position : SV_POSITION;
		float3 texcoord : TEXCOORD0;
	};

	v2f vert(appdata v)
	{
		v2f o;
		o.position = UnityObjectToClipPos(v.position);
		o.texcoord = v.texcoord;
		return o;
	}
	half4 frag(v2f i) : COLOR
	{
		int step = i.texcoord.y / _Steps;
		float y = step * _Steps;
		float firstPct = max(1 - (y * 2), 0);
		float endPct = max((y - 0.5) * 2,0);
		float middlePct = min( 1 - firstPct, 1 - endPct );

		return half4(_ColorStart*firstPct + _ColorMiddle * middlePct + _ColorEnd * endPct, 0);
	}
	ENDCG
	SubShader
	{
		Tags{ "RenderType" = "Background" "Queue" = "Background" }

		Pass
		{
			ZWrite Off
			Cull Off
			Fog { Mode Off }

			CGPROGRAM
			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma vertex vert
			#pragma fragment frag
			ENDCG
		}
	}
}
