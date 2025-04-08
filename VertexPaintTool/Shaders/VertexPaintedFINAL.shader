Shader "Custom/VertexPaintedFINAL" {
    Properties{
        _MainTex("Texture", 2D) = "white" {}
        _LightingScale("Lighting scale", Range(0, 1)) = 1
        [HideInInspector] _ShowTexture("Show texture", Float) = 1
        [HideInInspector] _ShowLighting("Show lighting", Float) = 1
        [HideInInspector] _ShowVertColors("Show vertex colors", Float) = 1
    }
        SubShader{
            Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
            LOD 100

            Pass {
                Lighting On
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #include "UnityCG.cginc"

                // Vertex color data
                float4 _VertexColorArray[1023];
                uniform float _LightingScale;
                uniform float _ShowTexture;
                uniform float _ShowLighting;
                uniform float _ShowVertColors;

                struct appdata {
                    float4 vertex : POSITION;
                    float3 normal : NORMAL;  // Include normal
                    float2 uv : TEXCOORD0;
                    uint vertexID : SV_VertexID;
                };

                struct v2f {
                    float4 vertex : SV_POSITION;
                    float3 normal : TEXCOORD1;  // Pass normal
                    float4 color : COLOR0;
                    float2 uv : TEXCOORD0;
                };

                sampler2D _MainTex;
                float4 _MainTex_ST;

                v2f vert(appdata v) {
                    v2f o;
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.normal = UnityObjectToWorldNormal(v.normal);  // Transform normal to world space
                    o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                    fixed3 light = lerp(float3(1, 1, 1), ShadeVertexLights(o.vertex, o.normal), _LightingScale);
                    o.color = fixed4(light, 1.0) * _VertexColorArray[v.vertexID];
                    return o;
                }

                fixed4 frag(v2f i) : SV_Target
                {
                    fixed4 white = fixed4(1,1,1,1);
                    fixed4 texColor = lerp(white, tex2D(_MainTex, i.uv), _ShowTexture);
                    fixed4 vertColor = lerp(white, i.color, _ShowVertColors);
                    return texColor * vertColor;
            }
            ENDCG
        }
    }
}