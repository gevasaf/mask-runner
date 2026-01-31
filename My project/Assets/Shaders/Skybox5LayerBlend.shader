Shader "Runner/Skybox 6 Layer Blend"
{
    Properties
    {
        _Tex1 ("Panorama 1", 2D) = "white" {}
        _Tex2 ("Panorama 2", 2D) = "white" {}
        _Tex3 ("Panorama 3", 2D) = "white" {}
        _Tex4 ("Panorama 4", 2D) = "white" {}
        _Tex5 ("Panorama 5", 2D) = "white" {}
        _Tex6 ("Panorama 6", 2D) = "white" {}
        _Opacity1 ("Opacity 1", Range(0, 1)) = 1
        _Opacity2 ("Opacity 2", Range(0, 1)) = 0
        _Opacity3 ("Opacity 3", Range(0, 1)) = 0
        _Opacity4 ("Opacity 4", Range(0, 1)) = 0
        _Opacity5 ("Opacity 5", Range(0, 1)) = 0
        _Opacity6 ("Opacity 6", Range(0, 1)) = 0
        _Rotation1 ("Rotation Y 1 (degrees)", Range(0, 360)) = 0
        _Rotation2 ("Rotation Y 2 (degrees)", Range(0, 360)) = 0
        _Rotation3 ("Rotation Y 3 (degrees)", Range(0, 360)) = 0
        _Rotation4 ("Rotation Y 4 (degrees)", Range(0, 360)) = 0
        _Rotation5 ("Rotation Y 5 (degrees)", Range(0, 360)) = 0
        _Rotation6 ("Rotation Y 6 (degrees)", Range(0, 360)) = 0
    }
    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" }
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 texcoord : TEXCOORD0;
            };

            sampler2D _Tex1, _Tex2, _Tex3, _Tex4, _Tex5, _Tex6;
            float4 _Tex1_ST, _Tex2_ST, _Tex3_ST, _Tex4_ST, _Tex5_ST, _Tex6_ST;
            float _Opacity1, _Opacity2, _Opacity3, _Opacity4, _Opacity5, _Opacity6;
            float _Rotation1, _Rotation2, _Rotation3, _Rotation4, _Rotation5, _Rotation6;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.vertex.xyz;
                return o;
            }

            // Rotate direction around Y axis (horizontal only). Angle in radians.
            float3 RotateY(float3 dir, float angleRad)
            {
                float c = cos(angleRad);
                float s = sin(angleRad);
                return float3(dir.x * c - dir.z * s, dir.y, dir.x * s + dir.z * c);
            }

            // Direction to equirectangular UV (panoramic / lat-long). V flipped so skybox is right-side up.
            float2 DirToPanoramicUV(float3 dir)
            {
                float3 d = normalize(dir);
                float u = 0.5 + atan2(d.z, d.x) / (2.0 * 3.14159265);
                float v = 0.5 + asin(d.y) / 3.14159265;
                return float2(u, v);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 dir = normalize(i.texcoord);
                fixed4 col = fixed4(0, 0, 0, 0);
                float totalWeight = 0;
                #define PI 3.14159265

                if (_Opacity1 > 0)
                {
                    float3 d1 = RotateY(dir, _Rotation1 * PI / 180.0);
                    float2 uv1 = DirToPanoramicUV(d1) * _Tex1_ST.xy + _Tex1_ST.zw;
                    fixed4 s = tex2D(_Tex1, uv1);
                    col += s * _Opacity1;
                    totalWeight += _Opacity1;
                }
                if (_Opacity2 > 0)
                {
                    float3 d2 = RotateY(dir, _Rotation2 * PI / 180.0);
                    float2 uv2 = DirToPanoramicUV(d2) * _Tex2_ST.xy + _Tex2_ST.zw;
                    fixed4 s = tex2D(_Tex2, uv2);
                    col += s * _Opacity2;
                    totalWeight += _Opacity2;
                }
                if (_Opacity3 > 0)
                {
                    float3 d3 = RotateY(dir, _Rotation3 * PI / 180.0);
                    float2 uv3 = DirToPanoramicUV(d3) * _Tex3_ST.xy + _Tex3_ST.zw;
                    fixed4 s = tex2D(_Tex3, uv3);
                    col += s * _Opacity3;
                    totalWeight += _Opacity3;
                }
                if (_Opacity4 > 0)
                {
                    float3 d4 = RotateY(dir, _Rotation4 * PI / 180.0);
                    float2 uv4 = DirToPanoramicUV(d4) * _Tex4_ST.xy + _Tex4_ST.zw;
                    fixed4 s = tex2D(_Tex4, uv4);
                    col += s * _Opacity4;
                    totalWeight += _Opacity4;
                }
                if (_Opacity5 > 0)
                {
                    float3 d5 = RotateY(dir, _Rotation5 * PI / 180.0);
                    float2 uv5 = DirToPanoramicUV(d5) * _Tex5_ST.xy + _Tex5_ST.zw;
                    fixed4 s = tex2D(_Tex5, uv5);
                    col += s * _Opacity5;
                    totalWeight += _Opacity5;
                }
                if (_Opacity6 > 0)
                {
                    float3 d6 = RotateY(dir, _Rotation6 * PI / 180.0);
                    float2 uv6 = DirToPanoramicUV(d6) * _Tex6_ST.xy + _Tex6_ST.zw;
                    fixed4 s = tex2D(_Tex6, uv6);
                    col += s * _Opacity6;
                    totalWeight += _Opacity6;
                }

                if (totalWeight > 0)
                    col /= totalWeight;
                else
                    col = fixed4(0.5, 0.5, 0.6, 1);
                col.a = 1;
                return col;
            }
            ENDCG
        }
    }
    FallBack Off
}
