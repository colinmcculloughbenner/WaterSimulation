Shader "Custom/Boat"{
	Properties{
		_Color ("Tint", Color) = (0, 0, 0, 1)
		_MainTex ("Texture", 2D) = "white" {}
		
		_GridX ("Grid X Coordinate", float) = 0
		_MaxX ("Grid Width", float) = 1
		_GridY ("Grid Y Coordinate", float) = 0
		_MaxY ("Grid Height", float) = 1
		_Heightmap ("Water Heightmap", 2D) = "gray" {}
		_CellWidthMaxY ("Scale Factor at Maximum Y", float) = 0
		_CellWidthMinY ("Scale Factor at Minimum Y", float) = 1
		_WaterTopY ("Water Top Y", float) = 2
		_WaterBottomY ("Water Bottom Y", float) = -5
		
		_WaterEffectScale ("Water Effect Scale", float) = 1
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

			float _GridX;
			float _MaxX;
			float _GridY;
			float _MaxY;

			float _WaterEffectScale;
			float _CellWidthMaxY;
			float _CellWidthMinY;
			float _WaterTopY;
			float _WaterBottomY;

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
				col *= _Color;
				col *= i.color;

				// Get water height from heightmap based on boat's position
				const float3 baseWorldPos = unity_ObjectToWorld._m03_m13_m23;
				const float2 gridPosition = float2((_GridX + i.obj_pos.x + baseWorldPos.x) / _MaxX, _GridY / _MaxY);
				const float waterHeight = tex2D(_Heightmap, gridPosition).x;
				const float cellWidth = lerp(_CellWidthMinY, _CellWidthMaxY, _GridY / _MaxY);
				const float waterWorldHeight = waterHeight * cellWidth * _WaterEffectScale;
				const float gridCoordinateWorldY = _WaterTopY - (_WaterTopY - _WaterBottomY) * (1 - _GridY / _MaxY) * (1 - _GridY / _MaxY);

				// Compare boat's position to the water height, and don't draw underwater parts.
				const float3 worldPos = mul(unity_ObjectToWorld, i.obj_pos).xyz;
				if (worldPos.y < waterWorldHeight + gridCoordinateWorldY)
				{
					col *= 0;
				}
				return col;
			}

			ENDCG
		}
	}
}
