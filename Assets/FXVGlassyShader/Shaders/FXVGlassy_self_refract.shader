
Shader "FXV/FXVGlassySelfRefract"
{
	Properties
	{
		_Color ("Main Color", Color) = (0.5,0.5,0.5,1)

		_TransparencyRimMin("TransparencyRimMin", Range(-4,4)) = 0.6
		_TransparencyRimMax("TransparencyRimMax", Range(-4,4)) = 1.0

		_MainTex ("Base", 2D) = "white" {}
		_MainTexColor("MainTexColor", Color) = (1,1,1,1)
	
		_TextureRimMin("TextureRimMin", Range(-2,1)) = 0.6
		_TextureRimMax("TextureRimMax", Range(0,4)) = 1.0

		_Enviro ("Enviro", 2D) = "white" {}

		_Normal("Normal", 2D) = "black" {}

		_ReflectionColorR("ReflectionColorR", Color) = (1,1,1,1)
		_ReflectionColorG("ReflectionColorG", Color) = (1,1,1,1)
		_ReflectionColorB("ReflectionColorB", Color) = (1,1,1,1)
		_ReflectionTime("ReflectionTime", Range(0,1)) = 0.0

		_RefractionStrength("RefractionStrength", Range(0,1)) = 1.0
		_RefractionFromNormal("RefractionFromNormal", Range(0,1)) = 1.0
			
		_SubsurfaceOffset("SubsurfaceOffset", Range(0,1)) = 0.5
		_SubsurfacePower("SubsurfacePower", Range(1,128)) = 16.0
	}
	
	Subshader
	{
		Tags{ "Queue" = "Transparent" "RenderType" = "Transparent" }

		GrabPass
		{

		}


		CGPROGRAM
		#pragma multi_compile USE_MAIN_TEXTURE __
		#pragma multi_compile USE_NORMAL_MAP __
		#pragma multi_compile USE_ENVIRO_MAP __
		#pragma multi_compile USE_SUBSURFACE_SCATTERING __

		#pragma surface surf SimpleLambert 
		#pragma target 3.0



		struct Input 
		{
#ifdef USE_MAIN_TEXTURE
			float2 uv_MainTex;
#endif

#ifdef USE_NORMAL_MAP
			float2 uv_Normal;
#endif
			float4 screenPos;
			float3 viewDir;
			float3 worldPos;
			float3 worldNormal; INTERNAL_DATA
		};

		uniform float4 _Color;
		float _TransparencyRimMin;
		float _TransparencyRimMax;

		uniform sampler2D _MainTex;
		uniform sampler2D _Enviro;
		uniform sampler2D _Normal;
		uniform sampler2D _GrabTexture;

		uniform float4 _MainTexColor;

		uniform float4 _ReflectionColorR;
		uniform float4 _ReflectionColorG;
		uniform float4 _ReflectionColorB;
		float _ReflectionTime;

		float _TextureRimMin;
		float _TextureRimMax;

		float _RefractionStrength;
		float _RefractionFromNormal;

		float _SubsurfaceOffset;
		float _SubsurfacePower;

		half4 LightingSimpleLambert(SurfaceOutput s, half3 lightDir, half3 viewDir, half atten)
		{
			half NdotL = max(0.0, dot(s.Normal, lightDir));
			half4 c;
			c.rgb = s.Albedo * _LightColor0.rgb * (NdotL * atten);

#ifdef USE_SUBSURFACE_SCATTERING
			float3 L = lightDir;
			float3 V = viewDir;
			float3 N = s.Normal;

			float3 H = normalize(L + N * _SubsurfaceOffset);
			float VdotH = pow(saturate(dot(V, -H)), _SubsurfacePower);

			c.rgb += s.Albedo * _LightColor0.rgb * VdotH;
#endif
			c.a = s.Alpha;

			return c;
		}

		void surf(Input i, inout SurfaceOutput o)
		{
#ifdef USE_NORMAL_MAP
			o.Normal = UnpackNormal(tex2D(_Normal, i.uv_Normal));
#endif

			float vdn = 1.0 - max(dot(i.viewDir, o.Normal), 0.0);
			float textureRim = smoothstep(_TextureRimMin, _TextureRimMax, vdn);

			float4 grabCoords = UNITY_PROJ_COORD(i.screenPos);

			float3 worldNormal = WorldNormalVector(i, o.Normal);
			float3 viewNorm = normalize(mul((float3x3)UNITY_MATRIX_V, worldNormal));

#ifdef USE_ENVIRO_MAP
			float3 viewDir = mul(UNITY_MATRIX_V, float4(i.worldPos, 1.0)).xyz;
			float3 viewCross = cross(normalize(viewDir), viewNorm);

			float3 capNormal = float3(-viewCross.y, viewCross.x, 0.0);
			float2 cap = capNormal.xy * 0.5 + 0.5;

			fixed4 enviroTex = tex2D(_Enviro, cap);
			fixed4 reflectionTex = tex2D(_Enviro, 0.5*cap + float2(0, (cap.x - 1.0) * (cap.y - 0.5) + _ReflectionTime));

			grabCoords.xy += (enviroTex.r - 0.5) * _RefractionStrength;
#endif

			grabCoords.xy += viewNorm.xy * _RefractionFromNormal;

			fixed4 ballColor = fixed4(0, 0, 0, 0);

			ballColor += _Color * _Color.a;

#ifdef USE_ENVIRO_MAP
			fixed4 reflectionColor = _ReflectionColorR.a * _ReflectionColorR * enviroTex.r + enviroTex.g * _ReflectionColorG * _ReflectionColorG.a + reflectionTex.b * _ReflectionColorB * _ReflectionColorB.a;
			reflectionColor.a = max(_ReflectionColorR.a, _ReflectionColorG.a) * enviroTex.g;
			ballColor.rgb += reflectionColor.rgb;

			//ballColor += _ReflectionColorR.a * _ReflectionColorR * enviroTex.r + _ReflectionColorG.a * _ReflectionColorG * enviroTex.g + _ReflectionColorB.a * _ReflectionColorB * reflectionTex.b;
#endif

#ifdef USE_MAIN_TEXTURE
			fixed4 mainTex = tex2D(_MainTex, i.uv_MainTex);
			ballColor += textureRim * _MainTexColor * mainTex;
#endif

			fixed4 grabColor = tex2Dproj(_GrabTexture, grabCoords);

			float transparencyRim = smoothstep(_TransparencyRimMin, _TransparencyRimMax, vdn);

			float alpha = _Color.a * transparencyRim;
#ifdef USE_ENVIRO_MAP
			alpha = max(alpha, reflectionColor.a);
#endif

			o.Albedo = grabColor.rgb * (1.0 - alpha) + ballColor.rgb * alpha;

			o.Alpha = _Color.a * transparencyRim;
		}
		ENDCG
	}
	CustomEditor "FXVGlassyMaterialEditor"

	Fallback "VertexLit"
}