Shader "LittleColony/PlacementGhost"
{
    Properties { _Color("Tint",Color)=(.5,1,.7,.4) }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent"}
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Color;
            struct v2f {float4 pos:SV_POSITION;};
            v2f vert(appdata_base v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);return o;}
            fixed4 frag(v2f i):SV_Target{return _Color;}
            ENDCG
        }
    }
}
