// Contorno de tinta de RELEVO: pase de pantalla completa (Full Screen Pass Renderer Feature del
// renderer Liria_Renderer) que dibuja líneas donde hay un salto de profundidad (siluetas) o de
// normal (aristas interiores). Así toda la geometría unificada de un bloque recibe el mismo trazo,
// sin depender de cómo vengan sus normales ni de duplicar mallas.
//
// La cámara de RELEVO es ortográfica: la profundidad se linealiza a mano, porque LinearEyeDepth
// solo vale para perspectiva.
Shader "RELEVO/ContornoTinta"
{
    Properties
    {
        _ColorTinta ("Color de la tinta", Color) = (0.24, 0.15, 0.13, 1)
        _Grosor ("Grosor (píxeles a 1080p)", Range(0.5, 5)) = 1.3
        _UmbralProfundidad ("Umbral de profundidad (m)", Float) = 0.3
        _UmbralNormal ("Umbral de normal", Range(0, 1)) = 0.35
        _Opacidad ("Opacidad", Range(0, 1)) = 0.7
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "ContornoTinta"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

            half4 _ColorTinta;
            float _Grosor;
            float _UmbralProfundidad;
            half _UmbralNormal;
            half _Opacidad;

            float ProfundidadLineal(float2 uv)
            {
                float cruda = SampleSceneDepth(uv);
                if (unity_OrthoParams.w > 0.5)
                {
                    #if UNITY_REVERSED_Z
                        cruda = 1.0 - cruda;
                    #endif
                    return lerp(_ProjectionParams.y, _ProjectionParams.z, cruda);
                }
                return LinearEyeDepth(cruda, _ZBufferParams);
            }

            half4 Frag(Varyings entrada) : SV_Target
            {
                float2 uv = entrada.texcoord;
                half4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

                float2 paso = _Grosor * (_ScreenParams.y / 1080.0) / _ScreenParams.xy;
                float2 desp[4] = { float2(paso.x, 0), float2(-paso.x, 0), float2(0, paso.y), float2(0, -paso.y) };

                // Donde no hay geometría (cielo) no hay nada que entintar: sin esto, sus normales
                // vacías contaban como arista y el cielo entero quedaba velado de marrón.
                #if UNITY_REVERSED_Z
                    if (SampleSceneDepth(uv) <= 0.000001) return color;
                #else
                    if (SampleSceneDepth(uv) >= 0.999999) return color;
                #endif

                float centro = ProfundidadLineal(uv);
                float3 normalCentro = SampleSceneNormals(uv);

                float saltoProfundidad = 0;
                float saltoNormal = 0;
                for (int i = 0; i < 4; i++)
                {
                    float p = ProfundidadLineal(uv + desp[i]);
                    // Solo cuenta si el vecino está MÁS LEJOS: la línea queda en el objeto de delante.
                    saltoProfundidad = max(saltoProfundidad, p - centro);
                    // Un vecino sin normal (cielo, o algo que no escribe DepthNormals) no es arista
                    // interior: ese borde ya lo marca el salto de profundidad.
                    float3 n = SampleSceneNormals(uv + desp[i]);
                    if (dot(n, n) > 0.25) saltoNormal = max(saltoNormal, 1.0 - dot(normalCentro, n));
                }

                // El umbral de profundidad crece con la distancia para no ensuciar suelos en rasante.
                float umbral = _UmbralProfundidad * (1.0 + centro * 0.02);
                half linea = max(step(umbral, saltoProfundidad), step(_UmbralNormal, saltoNormal));

                // La tinta se apaga con la distancia, como la perspectiva aérea: las montañas del
                // fondo quedan casi sin trazo y se leen lejos.
                linea *= 1.0 - smoothstep(120.0, 450.0, centro) * 0.85;

                color.rgb = lerp(color.rgb, _ColorTinta.rgb, linea * _Opacidad);
                return color;
            }
            ENDHLSL
        }
    }
}
