Shader "LittleColony/SwayingGrass"
{
    Properties { _Color("Grass",Color)=(.52,.65,.31,1) _MainTex("Blade coordinates",2D)="white"{} }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Off
        CGPROGRAM
        #pragma surface surf Standard vertex:vert addshadow
        #pragma target 3.0
        fixed4 _Color;
        float4 _ExpansionLocks;
        float _ExpansionDepth;
        struct Input {float4 color:COLOR;float3 worldPos;float2 uv_MainTex;};
        void vert(inout appdata_full v)
        {
            float wind=sin(v.vertex.x*.9+v.vertex.z*1.4+_Time.y*1.7);
            v.vertex.x+=wind*v.texcoord.y*.12;
            v.vertex.z+=cos(v.vertex.x+_Time.y*1.3)*v.texcoord.y*.045;
        }
        void surf(Input IN,inout SurfaceOutputStandard o)
        {
            o.Albedo=_Color.rgb*IN.color.rgb*lerp(.72,1.18,IN.uv_MainTex.y);
            float2 p=IN.worldPos.xz;
            float locked=(p.x<0?_ExpansionLocks.x:_ExpansionLocks.y)
                *step(_ExpansionLocks.z,abs(p.x))*step(abs(p.x),_ExpansionLocks.w)
                *step(abs(p.y),_ExpansionDepth);
            o.Albedo=lerp(o.Albedo,o.Albedo*.34,locked);
            o.Emission=o.Albedo*.12;o.Smoothness=.03;o.Alpha=1;
        }
        ENDCG
    }
    Fallback "Diffuse"
}
