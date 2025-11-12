Shader "Sprites/Outline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0

        // Add values to determine if outlining is enabled and outline color.
        [PerRendererData] _Outline("Outline", Float) = 0
        [PerRendererData] _OutlineColor("Outline Color", Color) = (1,1,1,1)
        // Change Outline Size to a float, a Range is often user-friendly.
        [PerRendererData] _OutlineSize("Outline Size", Range(0, 5)) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ PIXELSNAP_ON
            #pragma shader_feature ETC1_EXTERNAL_ALPHA
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            fixed4 _Color;
            float _Outline;
            fixed4 _OutlineColor;
            // Use a float for Outline Size to allow for thinner lines.
            float _OutlineSize;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                #ifdef PIXELSNAP_ON
                OUT.vertex = UnityPixelSnap(OUT.vertex);
                #endif

                return OUT;
            }

            sampler2D _MainTex;
            sampler2D _AlphaTex;
            float4 _MainTex_TexelSize;

            fixed4 SampleSpriteTexture(float2 uv)
            {
                fixed4 color = tex2D(_MainTex, uv);

                #if ETC1_EXTERNAL_ALPHA
                color.a = tex2D(_AlphaTex, uv).r;
                #endif

                return color;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 c = SampleSpriteTexture(IN.texcoord) * IN.color;

                // If outline is enabled and the current pixel has some alpha, check for outline.
                if (_Outline > 0 && c.a > 0)
                {
                    // Calculate the offset based on the float size.
                    float2 outlineOffset = _OutlineSize * _MainTex_TexelSize.xy;

                    // Sample in 8 directions to get a smoother outline.
                    fixed4 pixelUp = tex2D(_MainTex, IN.texcoord + float2(0, outlineOffset.y));
                    fixed4 pixelDown = tex2D(_MainTex, IN.texcoord - float2(0, outlineOffset.y));
                    fixed4 pixelRight = tex2D(_MainTex, IN.texcoord + float2(outlineOffset.x, 0));
                    fixed4 pixelLeft = tex2D(_MainTex, IN.texcoord - float2(outlineOffset.x, 0));
                    fixed4 pixelUpRight = tex2D(_MainTex, IN.texcoord + outlineOffset);
                    fixed4 pixelUpLeft = tex2D(_MainTex, IN.texcoord + float2(-outlineOffset.x, outlineOffset.y));
                    fixed4 pixelDownRight = tex2D(_MainTex, IN.texcoord + float2(outlineOffset.x, -outlineOffset.y));
                    fixed4 pixelDownLeft = tex2D(_MainTex, IN.texcoord - outlineOffset);

                    // Check the total alpha of surrounding pixels.
                    // If any of them are transparent, this pixel is on an edge.
                    float surroundingAlpha = pixelUp.a * pixelDown.a * pixelRight.a * pixelLeft.a * pixelUpRight.a * pixelUpLeft.a * pixelDownRight.a * pixelDownLeft.a;

                    if (surroundingAlpha == 0)
                    {
                        c.rgba = _OutlineColor;
                    }
                }

                c.rgb *= c.a;
                return c;
            }
        ENDCG
        }
    }
}