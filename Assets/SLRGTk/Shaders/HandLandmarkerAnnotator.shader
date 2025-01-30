Shader "Nana/HandLandmarkAnnotator"
{
    Properties
    {
        _PointColor ("Point Color", Color) = (1, 0, 0, 1)
        _LineColor ("Line Color", Color) = (0, 0, 1, 1)
        _Radius ("Point Radius", Float) = 0.005
        _StrokeWidth ("Line Stroke Width", Float) = 0.005
        _MainTex ("Main Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Overlay" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 pos : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _PointColor;
            float4 _LineColor;
            float _Radius;
            float _StrokeWidth;
            float4 _Dims;

            int _LandmarksPresent;
            float4 _Points[21];

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float2 adjustedUV = i.uv;
                
                float imageAspect = _Dims.x / _Dims.y;
                float screenAspect = _Dims.z / _Dims.w;
                
                if (imageAspect > screenAspect) {
                    adjustedUV.x = (adjustedUV.x - 0.5) * screenAspect / imageAspect + 0.5;
                } else {
                    adjustedUV.y = (adjustedUV.y - 0.5) * imageAspect / screenAspect + 0.5;
                }
                
                float4 color = tex2D(_MainTex, adjustedUV);

                if (_LandmarksPresent != 0) {
                    static const int2 connections[21] = {
                        int2(0, 1), int2(1, 2), int2(2, 3), int2(3, 4),
                        int2(0, 5), int2(5, 9), int2(9, 13), int2(13, 17),
                        int2(0, 17), int2(5, 6), int2(6, 7), int2(7, 8),
                        int2(9, 10), int2(10, 11), int2(11, 12), int2(13, 14),
                        int2(14, 15), int2(15, 16), int2(17, 18), int2(18, 19),
                        int2(19, 20)
                    };
                
                    // Draw skeleton lines
                    for (int j = 0; j < 21; j++)
                    {
                        float2 startPos = _Points[connections[j].x].xy;
                        float2 endPos = _Points[connections[j].y].xy;
                
                        float2 lineDir = normalize(endPos - startPos);
                        float2 pointToUV = adjustedUV - startPos;
                        float projection = clamp(dot(pointToUV, lineDir), 0.0, length(endPos - startPos));
                        float2 closestPoint = startPos + lineDir * projection;
                        float distToLine = length(adjustedUV - closestPoint);
                
                        if (distToLine < _StrokeWidth)
                        {
                            color = _LineColor;
                        }
                    }
                
                    // Draw landmark points
                    for (int k = 0; k < 21; k++)
                    {
                        float2 pointPos = _Points[k].xy;
                        float distToPoint = distance(adjustedUV, pointPos);
                
                        if (distToPoint < 0.005)
                        {
                            color = _PointColor;
                        }
                    }
                }

                return color;
            }
            ENDCG
        }
    }
}
