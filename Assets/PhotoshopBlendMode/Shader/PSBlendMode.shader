Shader "Blend/PSBlendMode"
{
    Properties
    {
        [IntRange]_ModeID ("ModeID", Range(0.0, 26.0)) = 0.0
        [Header(A is Dst Texture)]
        [Space(10)]
        _Color1 ("TextureColor_A", Color) = (1.0, 1.0, 1.0, 0.5)
        _MainTex1 ("Texture_1", 2D) = "white" { }
        _MainTex2 ("Texture_2", 2D) = "white" { }
        _MainTex3 ("Texture_3", 2D) = "white" { }
        _MainTex4 ("Texture_4", 2D) = "white" { }
        _MainTex5 ("Texture_5", 2D) = "white" { }
        _MainTex6 ("Texture_6", 2D) = "white" { }
        [Space(100)]
        [Header(B is Src Texture)]
        [Space(10)]
        _Color2 ("TextureColor_B", Color) = (1.0, 1.0, 1.0, 1.0)
        [HideInInspector]_IDChoose ("", float) = 0.0
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque" "Queue" = "Geometry"
        }
        ZWrite On

        Blend One Zero //Normal      or blend off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "./Include/PhotoshopBlendMode.cginc"

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

            uniform sampler2D _MainTex1, _MainTex2, _MainTex3, _MainTex4, _MainTex5, _MainTex6;
            uniform float4 _MainTex1_ST;
            uniform float4 _Color1,_Color2,_Color3,_Color4,_Color5,_Color6;
            uniform float _ModeID;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex1);
                return o;
            }
            float4 mixAlpha(float4 S,float4 D,float mod){
                return D + (float4(OutPutMode(S,D,mod),1.0)-D)*S.a;
            }
            float4 frag(v2f i) : SV_Target
            {
                float4 D = tex2D(_MainTex1, i.uv) ;
                float4 S = tex2D(_MainTex2, i.uv) ;
                float4 S1 = tex2D(_MainTex3, i.uv);
                float4 S2 = tex2D(_MainTex4, i.uv) ;
                float4 S3 = tex2D(_MainTex5, i.uv) ;
                float4 S4 = tex2D(_MainTex6, i.uv) * float4(0.6,0.6,0.6,0.7);

                float4 ans1 = mixAlpha(S,D,3);
                float4 ans2 = mixAlpha(S1,ans1,3);
                float4 ans3 = mixAlpha(S2,ans2,15);
                float4 ans4 = mixAlpha(S3,ans3,15);
                float4 ans5 = mixAlpha(S4,ans4,9);
                return ans5;
            }
            ENDCG
        }
    }
    CustomEditor "BlendModeGUI"
}