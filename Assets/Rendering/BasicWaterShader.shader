Shader "Custom/Water"{
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

			float _MaxX;
			float _GridHeight;

			float _WaterEffectScale;
			float _CellWidthMaxY;
			float _CellWidthMinY;
			float _WaterTopY;
			float _WaterBottomY;

			float _WaterLighteningThreshold;

			fixed4 _Color;

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
				fixed4 col = tex2D(_MainTex, i.uv);
				// col *= _Color;
				col *= i.color;

				// Calculate water height from height map
				const float2 worldPos = mul(unity_ObjectToWorld, i.obj_pos).xy;
				const float cellWidth = lerp(_CellWidthMinY, _CellWidthMaxY, (worldPos.y - _WaterBottomY) / (_WaterTopY - _WaterBottomY));
				const float gridPosX = 0.5 * (_MaxX + 1) + worldPos.x / cellWidth;
				const float gridPosY = lerp(_GridHeight + 1, 0, sqrt((_WaterTopY - worldPos.y) / (_WaterTopY - _WaterBottomY)));
				const float2 normalizedGridPos = float2(gridPosX / (_MaxX + 1), gridPosY / (_GridHeight + 1));
				const float waterHeight = tex2D(_Heightmap, normalizedGridPos).x;

				// Lighten the color if height exceeds lightening threshold
				if (waterHeight >= _WaterLighteningThreshold)
				{
					col = lerp(fixed4(1, 1, 1, 1), col, (1 - waterHeight) / (1 - _WaterLighteningThreshold));
				}
				
				return col;
			}

			ENDCG
		}
	}
}
