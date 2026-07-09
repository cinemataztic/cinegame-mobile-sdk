Shader "UI Extensions/SoftMaskShader"
{
	Properties
	{
		[PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
		_Color("Tint", Color) = (1,1,1,1)

		_StencilComp("Stencil Comparison", Float) = 8
		_Stencil("Stencil ID", Float) = 0
		_StencilOp("Stencil Operation", Float) = 0
		_StencilWriteMask("Stencil Write Mask", Float) = 255
		_StencilReadMask("Stencil Read Mask", Float) = 255

		_ColorMask("Color Mask", Float) = 15
		_AlphaMask("AlphaMask - Must be Wrapped", 2D) = "white"{}
	}

	SubShader
	{
		Tags
		{
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
			"PreviewType" = "Plane"
			"CanUseSpriteAtlas" = "True"
		}

		Stencil
		{
			Ref[_Stencil]
			Comp[_StencilComp]
			Pass[_StencilOp]
			ReadMask[_StencilReadMask]
			WriteMask[_StencilWriteMask]
		}

		LOD 0

		Cull Off
		Lighting Off
		ZWrite Off
		ZTest[unity_GUIZTestMode]
		Blend SrcAlpha OneMinusSrcAlpha
		ColorMask[_ColorMask]

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct appdata_t
			{
				float4 vertex   : POSITION;
				float4 color    : COLOR;
				float2 texcoord : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex   : SV_POSITION;
				fixed4 color    : COLOR;
				half2 texcoord  : TEXCOORD0;
				float3 worldPos : TEXCOORD1; // Tracks absolute 3D World position
			};

			fixed4 _Color;
			fixed4 _TextureSampleAdd;
			sampler2D _MainTex;
			sampler2D _AlphaMask;

			// Layout data supplied dynamically from the parent Mask's transform
			float4x4 _MaskWorldToLocal;
			float4 _MaskRectMin;
			float4 _MaskRectSize;
			float4 _SpriteRect;   
			float4 _SpriteBorder; 
			float4 _TextureSize;  

			float3 UnityObjectToWorldPos(float4 LocalPos)
			{
				return mul (unity_ObjectToWorld, LocalPos).xyz;
			}

			v2f vert(appdata_t IN)
			{
				v2f OUT;
				OUT.vertex = UnityObjectToClipPos(IN.vertex);
				OUT.texcoord = IN.texcoord;
				
				// Computes absolute world positions across both Built-in and URP pipelines
				OUT.worldPos = UnityObjectToWorldPos(IN.vertex);

				#ifdef UNITY_HALF_TEXEL_OFFSET
					OUT.vertex.xy += (_ScreenParams.zw - 1.0) * float2(-1, 1);
				#endif

				OUT.color = IN.color * _Color;
				return OUT;
			}

			float Map9Slice(float x, float targetSize, float borderLeft, float borderRight, float sourceSize)
			{
				float totalBorder = borderLeft + borderRight;
				if (totalBorder > targetSize && totalBorder > 0.0)
				{
					float scale = targetSize / totalBorder;
					float bLeftScaled = borderLeft * scale;
					if (x < bLeftScaled)
					{
						return bLeftScaled > 0.0 ? (x / bLeftScaled) * borderLeft : 0.0;
					}
					else
					{
						float bRightScaled = borderRight * scale;
						float xFromRight = targetSize - x;
						return bRightScaled > 0.0 ? sourceSize - (xFromRight / bRightScaled) * borderRight : sourceSize;
					}
				}

				if (x < borderLeft)
				{
					return x;
				}
				else if (x > targetSize - borderRight)
				{
					return sourceSize - (targetSize - x);
				}
				else
				{
					float targetCenterSize = targetSize - borderLeft - borderRight;
					float sourceCenterSize = sourceSize - borderLeft - borderRight;
					return borderLeft + (targetCenterSize > 0.0 ? ((x - borderLeft) / targetCenterSize) * sourceCenterSize : 0.0);
				}
			}

			fixed4 frag(v2f IN) : SV_Target
			{
				// Step 1: Project the pixel's world coordinate back into the Mask's local space
				float3 localPos = mul(_MaskWorldToLocal, float4(IN.worldPos, 1.0)).xyz;
				
				// Step 2: Normalize local coordinates relative to the mask's local rect minimum
				float2 maskLocalPos = localPos.xy - _MaskRectMin.xy;

				// Step 3: Map layout using 9-slice non-linear interpolation
				float sourceX = Map9Slice(maskLocalPos.x, _MaskRectSize.x, _SpriteBorder.x, _SpriteBorder.z, _SpriteRect.x);
				float sourceY = Map9Slice(maskLocalPos.y, _MaskRectSize.y, _SpriteBorder.y, _SpriteBorder.w, _SpriteRect.y);

				float2 maskUV = float2(
					(_SpriteRect.z + sourceX) / max(1.0, _TextureSize.x), 
					(_SpriteRect.w + sourceY) / max(1.0, _TextureSize.y)
				);

				clip(step(float2(0.0, 0.0), maskLocalPos) * step(maskLocalPos, _MaskRectSize.xy) - 0.0001);

				half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;
				float a = tex2D(_AlphaMask, maskUV).a;

				color.a *= a;
				return color;
			}
			ENDCG
		}
	}
}