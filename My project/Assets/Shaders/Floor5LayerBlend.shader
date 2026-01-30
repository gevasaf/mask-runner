Shader "Runner/Floor 5 Layer Blend"
{
    Properties
    {
        _Tex1 ("Texture 1", 2D) = "white" {}
        _Tex2 ("Texture 2", 2D) = "white" {}
        _Tex3 ("Texture 3", 2D) = "white" {}
        _Tex4 ("Texture 4", 2D) = "white" {}
        _Tex5 ("Texture 5", 2D) = "white" {}
        _Opacity1 ("Opacity 1", Range(0, 1)) = 1
        _Opacity2 ("Opacity 2", Range(0, 1)) = 0
        _Opacity3 ("Opacity 3", Range(0, 1)) = 0
        _Opacity4 ("Opacity 4", Range(0, 1)) = 0
        _Opacity5 ("Opacity 5", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 100

        Pass
        {
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
                float4 vertex : SV_POSITION;
            };

            sampler2D _Tex1, _Tex2, _Tex3, _Tex4, _Tex5;
            float4 _Tex1_ST, _Tex2_ST, _Tex3_ST, _Tex4_ST, _Tex5_ST;
            float _Opacity1, _Opacity2, _Opacity3, _Opacity4, _Opacity5;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = fixed4(0, 0, 0, 0);
                float totalWeight = 0;

                if (_Opacity1 > 0)
                {
                    fixed4 s = tex2D(_Tex1, i.uv * _Tex1_ST.xy + _Tex1_ST.zw);
                    col += s * _Opacity1;
                    totalWeight += _Opacity1;
                }
                if (_Opacity2 > 0)
                {
                    fixed4 s = tex2D(_Tex2, i.uv * _Tex2_ST.xy + _Tex2_ST.zw);
                    col += s * _Opacity2;
                    totalWeight += _Opacity2;
                }
                if (_Opacity3 > 0)
                {
                    fixed4 s = tex2D(_Tex3, i.uv * _Tex3_ST.xy + _Tex3_ST.zw);
                    col += s * _Opacity3;
                    totalWeight += _Opacity3;
                }
                if (_Opacity4 > 0)
                {
                    fixed4 s = tex2D(_Tex4, i.uv * _Tex4_ST.xy + _Tex4_ST.zw);
                    col += s * _Opacity4;
                    totalWeight += _Opacity4;
                }
                if (_Opacity5 > 0)
                {
                    fixed4 s = tex2D(_Tex5, i.uv * _Tex5_ST.xy + _Tex5_ST.zw);
                    col += s * _Opacity5;
                    totalWeight += _Opacity5;
                }

                if (totalWeight > 0)
                    col /= totalWeight;
                else
                    col = fixed4(0.2, 0.2, 0.2, 1);
                col.a = 1;
                return col;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
