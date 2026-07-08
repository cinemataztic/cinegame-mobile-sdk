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
		_CutOff("CutOff", Float) = 0
		[MaterialToggle] _HardBlend("HardBlend", Float) = 0
		_FlipAlphaMask("Flip Alpha Mask", Int) = 0
		_NoOuterClip("Outer Clip", Int) = 0
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
				float2 canvasPos : TEXCOORD1; // Passes the UI Canvas-space position
			};

			fixed4 _Color;
			fixed4 _TextureSampleAdd;
			sampler2D _MainTex;
			sampler2D _AlphaMask;

			// 9-Slice parameters passed from C# script
			float4 _MaskRectMin;
			float4 _MaskRectSize;
			float4 _SpriteRect;   // x: original width, y: original height, z: atlas X offset, w: atlas Y offset
			float4 _SpriteBorder; // x: left, y: bottom, z: right, w: top
			float4 _TextureSize;  // x: tex width, y: tex height

			float _CutOff;
			int _HardBlend;
			int _FlipAlphaMask;
			int _NoOuterClip;

			v2f vert(appdata_t IN)
			{
				v2f OUT;
				OUT.vertex = UnityObjectToClipPos(IN.vertex);
				OUT.texcoord = IN.texcoord;
				OUT.canvasPos = IN.vertex.xy; // UI geometry coordinates are pre-batched into Canvas Space

				#ifdef UNITY_HALF_TEXEL_OFFSET
					OUT.vertex.xy += (_ScreenParams.zw - 1.0) * float2(-1, 1);
				#endif

				OUT.color = IN.color * _Color;
				return OUT;
			}

			// Evaluates 9-slicing transformations mathematically along a single axis
			float Map9Slice(float x, float targetSize, float borderLeft, float borderRight, float sourceSize)
			{
				float totalBorder = borderLeft + borderRight;
				
				// Handle edge case where UI element is smaller than the sprite borders
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

				// Standard 9-slice mapping
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
				float2 maskLocalPos = IN.canvasPos - _MaskRectMin.xy;

				// Calculate pixel coordinate inside original un-sliced sprite dimensions
				float sourceX = Map9Slice(maskLocalPos.x, _MaskRectSize.x, _SpriteBorder.x, _SpriteBorder.z, _SpriteRect.x);
				float sourceY = Map9Slice(maskLocalPos.y, _MaskRectSize.y, _SpriteBorder.y, _SpriteBorder.w, _SpriteRect.y);

				// Convert to global texture/atlas UV positioning coordinates 
				float2 maskUV = float2(
					(_SpriteRect.z + sourceX) / max(1.0, _TextureSize.x), 
					(_SpriteRect.w + sourceY) / max(1.0, _TextureSize.y)
				);

				// Outer clipping boundary check
				if (_NoOuterClip == 0)
				{
					clip(step(float2(0.0, 0.0), maskLocalPos) * step(maskLocalPos, _MaskRectSize.xy) - 0.0001);
				}

				half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;
				float a = tex2D(_AlphaMask, maskUV).a;

				if (_FlipAlphaMask != 0) 
				{
					a = 1.0 - a;
				}

				if (a <= _CutOff)
					a = 0;
				else if (_HardBlend != 0) 
				{
					a = 1;
				}

				color.a *= a;
				return color;
			}
			ENDCG
		}
	}
}
