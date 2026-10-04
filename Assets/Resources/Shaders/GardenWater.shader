Shader "LittleColony/FlowingWater"
{
    Properties { _Color("Deep water",Color)=(.18,.48,.49,1) _Shallow("Shallow water",Color)=(.48,.75,.65,1) }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Off
        CGPROGRAM
        #pragma surface surf Standard vertex:vert
        #pragma target 3.0
        fixed4 _Color,_Shallow;
        struct Input { float3 worldPos; };
        void vert(inout appdata_full v) { v.vertex.y+=sin(v.vertex.z*2.4+_Time.y*2)*.008; }
        void surf(Input IN,inout SurfaceOutputStandard o)
        {
            float2 p=IN.worldPos.xz;
            float t=_Time.y;
            float ripple=sin(p.y*9+t*3+sin(p.x*8+t)*1.5);
            float cross=sin(p.x*16+sin(p.y*3+t*1.8)*2);
            float light=smoothstep(.80,1,ripple)*smoothstep(.2,1,cross);
            float broad=.5+.5*sin(p.y*1.8+t*.6+p.x*3);
            o.Albedo=lerp(_Color.rgb,_Shallow.rgb,.25+broad*.36)+light*.24;
            o.Emission=light*.025;
            o.Smoothness=.65;o.Metallic=.10;o.Alpha=1;
        }
        ENDCG
    }
    Fallback "Diffuse"
}
