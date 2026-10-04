Shader "LittleColony/WorkSiteFirefly"
{
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 position : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata input)
            {
                v2f output;
                output.position = UnityObjectToClipPos(input.vertex);
                output.color = input.color;
                output.uv = input.uv;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float2 coordinate = input.uv * 2 - 1;
                float2 abdomen = coordinate + float2(0, .12);
                float halo = exp(-dot(abdomen, abdomen) * 5) * .35;
                float core = 1 - smoothstep(.12, .3, length(abdomen));
                float body = 1 - smoothstep(.08, .15, length(coordinate * float2(1.6, 1) - float2(0, .18)));
                float wings = 1 - smoothstep(.6, 1, length(float2((abs(coordinate.x) - .3) * 4, (coordinate.y - .2) * 6)));
                float alpha = max(halo + core * .85, max(body * .8, wings * .4));
                fixed3 color = lerp(input.color.rgb, fixed3(1, 1, .82), core * .8);
                color = lerp(color, fixed3(.22, .28, .13), body);
                color = lerp(color, fixed3(.9, 1, .9), wings * .5);
                return fixed4(color, saturate(alpha) * input.color.a);
            }
            ENDCG
        }
    }
}
