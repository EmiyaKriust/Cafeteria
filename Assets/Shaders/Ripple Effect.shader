Shader "UI/Ripple Effect"
{
    Properties
    {
        _MainTex("Base", 2D) = "white" {}
        _GradTex("Gradient", 2D) = "white" {}
        _Reflection("Reflection Color", Color) = (0,0,0,0)
        _Params1("Parameters 1", Vector) = (1,1,0.8,0)
        _Params2("Parameters 2", Vector) = (1,1,1,0)
        _Drop1("Drop 1", Vector) = (0.5,0.5,1,0)
    }

        SubShader
        {
            Tags
            {
                "Queue" = "Overlay"
                "RenderType" = "Transparent"
                "IgnoreProjector" = "True"
                "PreviewType" = "Plane"
            }

            Cull Off
            Lighting Off
            ZWrite Off
            ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha

            Pass
            {
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #include "UnityCG.cginc"

                sampler2D _MainTex;
                sampler2D _GradTex;
                half4  _Reflection;
                float4 _Params1;
                float4 _Params2;
                float4 _Drop1;   // xy=位置, z=时间, w=激活

                struct appdata
                {
                    float4 vertex : POSITION;
                    float2 uv     : TEXCOORD0;
                    float4 color  : COLOR;
                };

                struct v2f
                {
                    float4 pos   : SV_POSITION;
                    float2 uv    : TEXCOORD0;
                    float4 color : COLOR;
                };

                v2f vert(appdata v)
                {
                    v2f o;
                    o.pos = UnityObjectToClipPos(v.vertex);
                    o.uv = v.uv;
                    o.color = v.color;
                    return o;
                }

                // 单条波纹在 position 处的波形值
                float wave(float2 position, float2 origin, float time, float active)
                {
                    float d = length(position - origin);
                    float t = time - d * _Params1.z;   // 传播延迟
                    float w = (tex2D(_GradTex, float2(t, 0)).a - 0.5f) * 2.0f;
                    return w * active;
                }

                float allwave(float2 position)
                {
                    return wave(position, _Drop1.xy, _Drop1.z, _Drop1.w);
                }

                half4 frag(v2f i) : SV_Target
                {
                    const float2 dx = float2(0.01f, 0);
                    const float2 dy = float2(0, 0.01f);

                    float2 p = i.uv * _Params1.xy;    // 考虑宽高比

                    float  w = allwave(p);
                    float2 dw = float2(allwave(p + dx) - w, allwave(p + dy) - w);

                    // 折射偏移
                    float2 duv = dw * _Params2.xy * 0.2f * _Params2.z;

                    half4 c = tex2D(_MainTex, i.uv + duv);

                    // 反射强度
                    float fr = pow(length(dw) * 3.0 * _Params2.w, 3.0);
                    half4 outc = lerp(c, _Reflection, fr);

                    outc.a *= c.a;        // 保留原 UI 透明度
                    outc *= i.color;    // 支持 CanvasGroup / 顶点色
                    return outc;
                }
                ENDCG
            }
        }
}