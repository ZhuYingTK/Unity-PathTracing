Shader "Hidden/AddShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        
        Cull Off
        ZWrite OFF
        ZTEST ALWAYS

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            
            CGPROGRAM
            
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            
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

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Mask;
            float _Sample;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float4 color = tex2D(_MainTex,i.uv);
                float2 mask;
                mask.x = step(i.uv.x,_Mask.x + _Mask.z) * step(_Mask.x,i.uv.x);
                mask.y = step(i.uv.y,_Mask.y + _Mask.w) * step(_Mask.y,i.uv.y);
                return float4(color.rgb,color.a * 1.0f/(_Sample + 1.0f) * mask.x * mask.y);
            }
            ENDCG
        }
    }
}
