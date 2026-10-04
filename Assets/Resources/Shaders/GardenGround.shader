Shader "LittleColony/GardenGround"
{
    Properties { _Color("Grass",Color)=(.40,.56,.25,1) _Patch("Sunlit grass",Color)=(.57,.65,.33,1) }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        CGPROGRAM
        #pragma surface surf Standard
        #pragma target 3.0
        fixed4 _Color,_Patch;
        float4 _ExpansionLocks;
        float _ExpansionDepth;
        struct Input {float3 worldPos;};
        float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
        float noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);}
        void surf(Input IN,inout SurfaceOutputStandard o)
        {
            float2 p=IN.worldPos.xz;
            float n=noise(p*.55)*.72+noise(p*3)*.20+noise(p*24)*.08;
            o.Albedo=lerp(_Color.rgb,_Patch.rgb,n);
            float locked=(p.x<0?_ExpansionLocks.x:_ExpansionLocks.y)
                *step(_ExpansionLocks.z,abs(p.x))*step(abs(p.x),_ExpansionLocks.w)
                *step(abs(p.y),_ExpansionDepth);
            o.Albedo=lerp(o.Albedo,o.Albedo*.34,locked);
            o.Smoothness=.05;o.Alpha=1;
        }
        ENDCG
    }
    Fallback "Diffuse"
}
