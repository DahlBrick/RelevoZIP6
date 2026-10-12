// Agua estilizada de RELEVO.
//
// La superficie del canal y del mar son cajas planas (DioramaBuilder): no tienen vértices
// suficientes para mover la geometría, así que toda la vida del agua se dibuja en el fragmento:
//  - Color por profundidad: compara la profundidad de la escena (depth texture de URP) con la del
//    propio fragmento, de modo que el agua se aclara donde toca muelles, rocas y pilotes.
//  - Espuma de orilla: una banda animada donde la profundidad es casi cero.
//  - Vetas de espuma que derivan con la corriente y destellos de sol que parpadean.
//  - Recibe la sombra del sol para que los muelles se proyecten sobre el agua.
//
// La cámara del juego es ortográfica; la profundidad lineal se reconstruye en los dos casos.
Shader "RELEVO/Agua"
{
    Properties
    {
        _ColorSomero ("Color somero", Color) = (0.45, 0.78, 0.74, 1)
        _ColorProfundo ("Color profundo", Color) = (0.10, 0.36, 0.42, 1)
        _ColorEspuma ("Color espuma", Color) = (0.95, 0.97, 0.92, 1)
        _Profundidad ("Distancia de transición", Float) = 1.2
        _AnchoEspuma ("Ancho de espuma de orilla", Float) = 0.28
        _EscalaOndas ("Escala de ondas", Float) = 0.55
        _Velocidad ("Velocidad de corriente", Vector) = (0.18, 0.07, 0, 0)
        _Destellos ("Intensidad de destellos", Float) = 1.6
        _Opacidad ("Opacidad mínima", Range(0, 1)) = 0.82
        _Sombra ("Fuerza de la sombra", Range(0, 1)) = 0.55
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent-10" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "Agua"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _ColorSomero;
                half4 _ColorProfundo;
                half4 _ColorEspuma;
                float _Profundidad;
                float _AnchoEspuma;
                float _EscalaOndas;
                float4 _Velocidad;
                float _Destellos;
                half _Opacidad;
                half _Sombra;
            CBUFFER_END

            struct Atributos
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Variables
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float fog : TEXCOORD3;
            };

            Variables Vert(Atributos entrada)
            {
                Variables salida;
                VertexPositionInputs posiciones = GetVertexPositionInputs(entrada.positionOS.xyz);
                salida.positionCS = posiciones.positionCS;
                salida.positionWS = posiciones.positionWS;
                salida.screenPos = ComputeScreenPos(posiciones.positionCS);
                salida.normalWS = TransformObjectToWorldNormal(entrada.normalOS);
                salida.fog = ComputeFogFactor(posiciones.positionCS.z);
                return salida;
            }

            // Ruido de valor 2D barato y estable: suficiente para vetas y destellos estilizados.
            float Hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float Ruido(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                float a = Hash(i);
                float b = Hash(i + float2(1, 0));
                float c = Hash(i + float2(0, 1));
                float d = Hash(i + float2(1, 1));
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            // Profundidad lineal en unidades de mundo a lo largo de la vista, válida para cámara
            // ortográfica (la del juego) y perspectiva (vista de escena del editor).
            float ProfundidadLineal(float crudo)
            {
                if (unity_OrthoParams.w > 0.5)
                {
                    #if UNITY_REVERSED_Z
                        crudo = 1.0 - crudo;
                    #endif
                    return lerp(_ProjectionParams.y, _ProjectionParams.z, crudo);
                }
                return LinearEyeDepth(crudo, _ZBufferParams);
            }

            half4 Frag(Variables entrada) : SV_Target
            {
                float2 uvPantalla = entrada.screenPos.xy / entrada.screenPos.w;
                float escena = ProfundidadLineal(SampleSceneDepth(uvPantalla));
                float propia = ProfundidadLineal(entrada.positionCS.z);
                float diferencia = max(0.0, escena - propia);

                float t = _Time.y;
                float2 p = entrada.positionWS.xz * _EscalaOndas;

                // Dos capas de ruido que derivan en sentidos distintos: es lo que da la corriente.
                float capaA = Ruido(p + _Velocidad.xy * t);
                float capaB = Ruido(p * 1.9 - _Velocidad.yx * t * 1.3 + 7.1);
                float ondas = (capaA + capaB) * 0.5;

                // Color por profundidad, con las ondas moviendo ligeramente la transición.
                float fondo = saturate(diferencia / max(0.01, _Profundidad) + (ondas - 0.5) * 0.35);
                half3 color = lerp(_ColorSomero.rgb, _ColorProfundo.rgb, fondo);

                // Bandas de luz suaves en la cresta de las ondas.
                float cresta = smoothstep(0.6, 0.8, ondas);
                color += cresta * 0.035;

                // Espuma de orilla: una banda que respira contra muelles y rocas.
                float respiro = 0.75 + 0.25 * sin(t * 1.6 + entrada.positionWS.x * 0.9 + entrada.positionWS.z * 0.7);
                float bordeEspuma = _AnchoEspuma * respiro;
                float espumaOrilla = 1.0 - smoothstep(bordeEspuma * 0.45, bordeEspuma, diferencia + (capaB - 0.5) * 0.12);

                // Vetas finas que viajan con la corriente: la línea donde dos capas de ruido se
                // cruzan, enmascarada por un ruido muy lento para que solo aparezcan en algunas
                // zonas. Un umbral sobre una sola capa daba manchas blancas en vez de líneas.
                float lineaA = Ruido(p * 1.3 + _Velocidad.xy * t);
                float lineaB = Ruido(p * 1.43 - _Velocidad.xy * t * 0.8 + 3.3);
                float linea = 1.0 - smoothstep(0.0, 0.03, abs(lineaA - lineaB));
                float zona = smoothstep(0.55, 0.8, Ruido(p * 0.3 + t * 0.02));
                float vetas = linea * zona * 0.32;

                float espuma = saturate(espumaOrilla + vetas);

                // Sol: sombra recibida y destellos que parpadean sobre las crestas.
                float4 coordSombra = TransformWorldToShadowCoord(entrada.positionWS);
                Light sol = GetMainLight(coordSombra);
                half sombra = lerp(1.0, sol.shadowAttenuation, _Sombra);

                float chispa = Ruido(p * 9.0 + float2(t * 0.9, -t * 0.6));
                float destello = smoothstep(0.955, 0.99, chispa) * smoothstep(0.5, 0.72, ondas) * _Destellos;

                color = color * (0.55 + 0.45 * sombra) * (sol.color * 0.35 + 0.75);
                color = lerp(color, _ColorEspuma.rgb, espuma);
                color += destello * sol.color * sombra;

                half alfa = lerp(_Opacidad, 1.0, saturate(fondo + espuma));

                color = MixFog(color, entrada.fog);
                return half4(color, alfa);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
