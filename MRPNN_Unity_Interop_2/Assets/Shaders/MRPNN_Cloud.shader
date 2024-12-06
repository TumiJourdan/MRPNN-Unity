Shader "Unlit/MRPNN_Cloud"
{
    Properties
    {
        _MainTex("Main Texture", 2D) = "white" {}
        _Transmittance("Main Texture", 2D) = "white" {}
        _CloudTex("Cloud Texture", 2D) = "black" {}
        _Threshold("Black Threshold", Range(0, 1)) = 0.001
        _BlendFactor("Cloud Blend Factor", Range(0, 1)) = 0.5
        _AccumTex("Accumulated Transmittance",2D) = "black" {}
       
    }

        SubShader
        {
            Tags { "RenderType" = "Opaque" }
            LOD 100

            Pass
            {
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag

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
                    float3 viewVector : TEXCOORD1;
                };

                sampler2D _MainTex;
                sampler2D _CloudTex;
                float4 _MainTex_ST;
                float _Threshold;
                float _BlendFactor;
                sampler2D _Transmittance;
                sampler2D _CameraDepthTexture;
                sampler2D _AccumTex;
                bool _Predict = true;

                v2f vert(appdata v)
                {
                    v2f o;
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                    float3 viewVector = mul(unity_CameraInvProjection, float4(v.uv * 2 - 1, 0, -1));
                    o.viewVector = mul(unity_CameraToWorld, float4(viewVector, 0));
                    return o;
                }
                float squeeze(float value)
                {
                    float scale = 5.0;
                    return 1.0 / (1.0 + exp(-value * scale));
                }

                fixed4 frag(v2f i) : SV_Target
                {
                    // Sample both textures
                    fixed4 mainColor = tex2D(_MainTex, i.uv);
                    fixed4 cloudColor = tex2D(_CloudTex, i.uv);
                    cloudColor.rgb = GammaToLinearSpace(cloudColor.rgb);
                    float2 transmittance_depth = tex2D(_Transmittance, i.uv).rg;
                    float accum = tex2D(_AccumTex, i.uv).r;

                    // Use lerp to blend between main color and cloud color based on brightness and threshold
                    fixed4 finalColor = lerp(cloudColor, mainColor, transmittance_depth.r);

                    float viewLength = length(i.viewVector);
                    float non_linear_depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv);
                    float depth = LinearEyeDepth(non_linear_depth)*viewLength;


                    if (transmittance_depth.g > depth) {
                        finalColor = mainColor;
                    }

                    // Ensure the color stays within valid range
                    return finalColor;
                }
                ENDCG
            }
        }
}
