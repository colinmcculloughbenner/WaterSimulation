Shader "Custom/StackedWavesShader" {
    Properties{
		_Color ("Tint", Color) = (0, 0, 0, 1)
		_MainTex ("Texture", 2D) = "white" {}
		
		_GridWidth ("Grid Width", float) = 1
		_GridHeight ("Grid Height", float) = 1
		_Heightmap ("Water Heightmap", 2D) = "gray" {}
		_CellWidthMaxY ("Cell Width at Maximum Y", float) = 0
		_CellWidthMinY ("Cell Width at Minimum Y", float) = 1
		_WaterTopY ("Water Top Y", float) = 2
		_WaterBottomY ("Water Bottom Y", float) = -5
		_WaterEffectScale ("Water Effect Scale", float) = 1
		
		_WaterLighteningThreshold ("Water Lightening Threshold", float) = 0.99
    	
    	_TopColor ("Color at top of gradient", Color) = (0, 1, 1, 1)
    	_BottomColor ("Color at bottom of gradient", Color) = (0, 0.1, 0.1, 1)
    	_Bands ("Number of bands in gradient", int) = 5
    	
    	_WhitecapColor ("Color of white wave peaks", Color) = (1,1,1,1)
    	_ScaleBandwidth ("Scale factor for bandwidth", Float) = 1
    }

	SubShader{
		Tags{ 
			"RenderType"="Transparent" 
			"Queue"="Transparent"
		}

		Blend SrcAlpha OneMinusSrcAlpha

		ZWrite off
		Cull off

		Pass{

			CGPROGRAM


			#include "UnityCG.cginc"

			#pragma vertex vert
			#pragma fragment frag

			sampler2D _MainTex;
			float4 _MainTex_ST;

			sampler2D _Heightmap;
			float4 _Heightmap_ST;

			float _GridWidth;
			float _GridHeight;

			float _WaterEffectScale;
			float _CellWidthMaxY;
			float _CellWidthMinY;
			float _WaterTopY;
			float _WaterBottomY;

			float _WaterLighteningThreshold;

			fixed4 _Color;
			fixed4 _TopColor;
			fixed4 _BottomColor;
			fixed4 _WhitecapColor;

			float _ScaleBandwidth;
			
			int _Bands;

			struct appdata{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				fixed4 color : COLOR;
			};

			struct v2f{
				float4 position : SV_POSITION;
				float2 uv : TEXCOORD0;
				fixed4 color : COLOR;
				float4 obj_pos : TEXCOORD1;
			};

			v2f vert(appdata v){
				v2f o;
				o.position = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.color = v.color;
				o.obj_pos = v.vertex;
				return o;
			}

			fixed4 frag(v2f i) : SV_TARGET{
				const float2 worldPos = mul(unity_ObjectToWorld, i.obj_pos).xy;

				// Default the color to the horizon color
				fixed4 col = _TopColor;
				const fixed4 texCol = tex2D(_MainTex, i.uv);
				
				// Go through each band testing whether in band, updating color, until outside band
				const float bandsFloat = float(_Bands);
				
				[unroll(50)]
				for (int b = _Bands - 1; b >= 0; --b)
				{
					// Calculate grid depth of the band
					const float normalizedGridY = float(b) / bandsFloat;

					// Find x-value of screen point at that depth
					const float cellWidth = lerp(_CellWidthMinY, _CellWidthMaxY, normalizedGridY);
					const float normalizedGridX = (0.5 * (_GridWidth + 1) + worldPos.x / cellWidth) / (_GridWidth + 1);

					// Find water height at that point and scale it for perspective and by global scale factor
					const float heightmapValue = tex2D(_Heightmap, float2(normalizedGridX, normalizedGridY)).x;
					float height = heightmapValue - 0.5;
					height *= cellWidth * _WaterEffectScale;

					// Find world height of that point
					float baseWorldY = _WaterTopY - (_WaterTopY - _WaterBottomY) * (1 - normalizedGridY) * (1 - normalizedGridY);
					float waveWorldY = baseWorldY + height;

					// Break out of loop if the world/screen position is not overlapped by that band
					if (worldPos.y > waveWorldY)
					{
						break;
					}

					// Otherwise, update its base color

					float nextNormalizedGridY = float (b - 1) /bandsFloat;
					float nextBaseWorldY = _WaterTopY - (_WaterTopY - _WaterBottomY) * (1 - nextNormalizedGridY) * (1 - nextNormalizedGridY);

					float bandWidth = baseWorldY - nextBaseWorldY;
					bandWidth *= _ScaleBandwidth;

					fixed4 bandColor = lerp(_BottomColor, _TopColor, normalizedGridY);
					fixed4 nextColor = lerp(_BottomColor, _TopColor, float(b + 1) / bandsFloat);

					float distFromBandTop = waveWorldY - worldPos.y;
					float normalizedDistFromTop = distFromBandTop / bandWidth;

					fixed4 baseColor = nextColor;
			
					if (distFromBandTop > 0)
					{
						baseColor = lerp(nextColor, bandColor, normalizedDistFromTop);
					}

					col = baseColor;
				}

				// Superimpose heightmap on colored scene
				const float cellWidth = lerp(_CellWidthMinY, _CellWidthMaxY, (worldPos.y - _WaterBottomY) / (_WaterTopY - _WaterBottomY));
				const float gridPosX = 0.5 * (_GridWidth + 1) + worldPos.x / cellWidth;
				const float gridPosY = lerp(_GridHeight + 1, 0, sqrt((_WaterTopY - worldPos.y) / (_WaterTopY - _WaterBottomY)));
				const float2 normalizedGridPos = float2(gridPosX / (_GridWidth + 1), gridPosY / (_GridHeight + 1));
				const float waterHeight = tex2D(_Heightmap, normalizedGridPos).x;
				if (waterHeight >= _WaterLighteningThreshold)
				{
					float t = (1 - waterHeight) / (1 - _WaterLighteningThreshold);
					col = lerp(_WhitecapColor, col, t);
				}
				
				col *= texCol;
				
				return col;
			}

			ENDCG
		}
	}
}