Shader "Blend/PSBlendMode"
{
    Properties
    {
        [IntRange]_ModeID ("ModeID", Range(0.0, 26.0)) = 0.0
        // [Header(A is Dst Texture)]
        // [Space(10)]
        _Color1 ("TextureColor_A", Color) = (1.0, 1.0, 1.0, 1.0)
        _MainTex1 ("Texture_1", 2D) = "white" { }

        [HideInInspector]_IDChoose ("", float) = 0.0
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent" "IgnoreProjector"="true" "Queue" = "Transparent"
        }
        // ZWrite On

        Blend One Zero //Normal      or blend off

        GrabPass{}

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
                float4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float2 bguv : TEXCOORD1;
            };

            uniform sampler2D _MainTex1;//, _MainTex2, _MainTex3, _MainTex4, _MainTex5, _MainTex6;
            uniform float4 _MainTex1_ST;//,_MainTex2_ST;
            uniform float4 _Color1;//,_Color2,_Color3,_Color4,_Color5,_Color6;
            uniform float _ModeID;
            sampler2D _GrabTexture;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex1);
                o.color = v.color;
                o.bguv = ComputeGrabScreenPos(o.vertex);
                return o;
            }
            float4 mixAlpha(float4 S,float4 D,float mod){
                return D + (float4(OutPutMode(S,D,mod),1.0)-D)*S.a;
            }
            float4 frag(v2f i) : SV_Target
            {
                float4 D = tex2D(_GrabTexture, i.bguv) ;
                float4 S = tex2D(_MainTex1, i.uv) * _Color1;


                float4 ans1 = mixAlpha(S,D,_ModeID);

                return ans1;
            }
            ENDCG
        }
    }
    CustomEditor "BlendModeGUI"
}