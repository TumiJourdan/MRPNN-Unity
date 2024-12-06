Shader "Unlit/Invert_Depth"
{
    Properties{ }
    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            sampler2D _CameraDepthTexture;

            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(float4 pos : POSITION) {
                v2f o;
                o.pos = UnityObjectToClipPos(pos);
                o.uv = pos.xy;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target {
                float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv);
                return fixed4(depth,depth, depth, 1.0);
            }
            ENDCG
        }
    }
}
