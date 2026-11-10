Shader "Hidden/HOne/PauseBlur"
{
    Properties { _MainTex ("Texture", 2D) = "white" {} }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 frag(v2f_img i) : SV_Target
            {
                float2 step = _MainTex_TexelSize.xy * 2;
                fixed4 c = 0;
                for (int y = -2; y <= 2; y++)
                    for (int x = -2; x <= 2; x++)
                        c += tex2D(_MainTex, i.uv + float2(x, y) * step);
                return c / 25;
            }
            ENDHLSL
        }
    }
}
