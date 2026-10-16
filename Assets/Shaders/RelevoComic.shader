// Sombreado de cómic de RELEVO para la geometría de Liria (materiales LIRIA_*).
//
// Lo que hace distinto de URP/Lit:
//  - Luz en bandas: el término difuso se corta en dos escalones (luz, medio tono, sombra) en vez
//    de degradar, y la sombra proyectada también es de borde duro.
//  - La sombra no es negra sino un color propio (frío, violáceo), que es lo que da el aire de
//    ilustración cuando la luz del sol es cálida.
//  - Trama de tinta: en las zonas de sombra aparecen rayas diagonales en espacio de pantalla. La
//    cámara del juego es fija, así que no "nadan".
//  - Chapa ondulada (opcional, _ONDULADO): las ondas se dibujan en el sombreado, sin geometría.
//    Van a lo largo de la pendiente (o en vertical en las paredes), con una línea de tinta en cada
//    valle, juntas entre planchas y manchas de óxido por ruido en espacio de mundo, así que no
//    dependen de las UV de los packs.
// El contorno de tinta no se hace aquí: es un pase de pantalla completa (RELEVO/ContornoTinta)
// que lee la profundidad y las normales, por eso este shader escribe DepthNormals.
Shader "RELEVO/Comic"
{
    Properties
    {
        _BaseColor ("Color", Color) = (1, 1, 1, 1)
        _BaseMap ("Textura (opcional)", 2D) = "white" {}
        [HDR] _EmissionColor ("Emisión (ventanas, faroles)", Color) = (0, 0, 0, 1)
        _ColorSombra ("Color de sombra", Color) = (0.42, 0.38, 0.58, 1)
        _Medio ("Intensidad del medio tono", Range(0, 1)) = 0.55
        _Umbral1 ("Corte sombra / medio tono", Range(-1, 1)) = 0.0
        _Umbral2 ("Corte medio tono / luz", Range(-1, 1)) = 0.45
        _Suavidad ("Suavidad de los cortes", Range(0.001, 0.2)) = 0.02
        _Ambiente ("Luz ambiente", Range(0, 1)) = 0.35
        _Trama ("Intensidad de la trama", Range(0, 1)) = 0.22
        _PasoTrama ("Separación de la trama (píxeles)", Float) = 6
        _GrosorTrama ("Grosor de la trama (0-1)", Range(0.05, 0.9)) = 0.3

        [Toggle(_ONDULADO)] _Ondulado ("Chapa ondulada", Float) = 0
        _PasoOnda ("Separación entre ondas (m)", Float) = 0.3
        _RelieveOnda ("Relieve de las ondas", Range(0, 1.5)) = 0.7
        _TintaOnda ("Tinta en los valles", Range(0, 1)) = 0.35
        _LargoPlancha ("Largo de cada plancha (m)", Float) = 2.2
        _ColorOxido ("Color del óxido", Color) = (0.36, 0.17, 0.12, 1)
        _Oxido ("Cantidad de óxido", Range(0, 1)) = 0.45
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half4 _EmissionColor;
            half4 _ColorSombra;
            half _Medio;
            half _Umbral1;
            half _Umbral2;
            half _Suavidad;
            half _Ambiente;
            half _Trama;
            float _PasoTrama;
            half _GrosorTrama;
            float _PasoOnda;
            half _RelieveOnda;
            half _TintaOnda;
            float _LargoPlancha;
            half4 _ColorOxido;
            half _Oxido;
        CBUFFER_END

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
            return lerp(lerp(Hash(i), Hash(i + float2(1, 0)), u.x),
                        lerp(Hash(i + float2(0, 1)), Hash(i + float2(1, 1)), u.x), u.y);
        }
        ENDHLSL

        Pass
        {
            Name "Comic"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma shader_feature_local _ONDULADO

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Atributos
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Variables
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float fog : TEXCOORD3;
            };

            Variables Vert(Atributos entrada)
            {
                UNITY_SETUP_INSTANCE_ID(entrada);
                Variables salida;
                salida.positionWS = TransformObjectToWorld(entrada.positionOS.xyz);
                salida.positionCS = TransformWorldToHClip(salida.positionWS);
                salida.normalWS = TransformObjectToWorldNormal(entrada.normalOS);
                salida.uv = TRANSFORM_TEX(entrada.uv, _BaseMap);
                salida.fog = ComputeFogFactor(salida.positionCS.z);
                return salida;
            }

            half4 Frag(Variables entrada, bool frontal : SV_IsFrontFace) : SV_Target
            {
                half3 base = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, entrada.uv).rgb * _BaseColor.rgb;
                float3 normal = normalize(entrada.normalWS) * (frontal ? 1.0 : -1.0);
                half tintaChapa = 0;

                #if defined(_ONDULADO)
                    // Eje de la cumbrera: horizontal y dentro de la cara. Las ondas lo recorren.
                    float3 cumbrera = cross(normal, float3(0, 1, 0));
                    if (length(cumbrera) > 0.2)
                    {
                        cumbrera = normalize(cumbrera);
                        float3 pendiente = normalize(cross(cumbrera, normal));
                        float fase = dot(entrada.positionWS, cumbrera) / _PasoOnda * 6.2831853;
                        normal = normalize(normal + cumbrera * sin(fase) * _RelieveOnda);

                        float valle = smoothstep(0.86, 0.97, cos(fase));
                        float plancha = frac(dot(entrada.positionWS, pendiente) / _LargoPlancha);
                        float junta = 1.0 - smoothstep(0.0, 0.025, min(plancha, 1.0 - plancha));
                        tintaChapa = max(valle * _TintaOnda, junta * 0.8);

                        // Óxido: manchas que bajan por la pendiente (ruido estirado a lo largo).
                        float2 q = float2(dot(entrada.positionWS, cumbrera) * 0.9, dot(entrada.positionWS, pendiente) * 0.25);
                        float mancha = smoothstep(0.55, 0.75, Ruido(q) * 0.7 + Ruido(q * 3.1) * 0.3);
                        base = lerp(base, _ColorOxido.rgb, mancha * _Oxido);
                    }
                #endif

                Light sol = GetMainLight(TransformWorldToShadowCoord(entrada.positionWS));
                half lambert = dot(normal, sol.direction);

                // Dos escalones: sombra -> medio tono -> luz.
                half banda1 = smoothstep(_Umbral1 - _Suavidad, _Umbral1 + _Suavidad, lambert);
                half banda2 = smoothstep(_Umbral2 - _Suavidad, _Umbral2 + _Suavidad, lambert);
                half sombraDura = smoothstep(0.45, 0.55, sol.shadowAttenuation);
                half luz = (banda1 * _Medio + banda2 * (1.0 - _Medio)) * sombraDura;

                half3 iluminado = base * sol.color;
                half3 sombreado = base * _ColorSombra.rgb;
                half3 color = lerp(sombreado, iluminado, luz) + base * SampleSH(normal) * _Ambiente;

                // Trama diagonal solo donde no llega el sol.
                float2 pixel = entrada.positionCS.xy;
                half raya = step(frac((pixel.x + pixel.y) / _PasoTrama), _GrosorTrama);
                color *= 1.0 - raya * _Trama * (1.0 - luz);
                color *= 1.0 - tintaChapa;
                // La emisión no recibe sombra ni trama: es luz propia (ventanas, faroles, antena).
                color += _EmissionColor.rgb;

                color = MixFog(color, entrada.fog);
                return half4(color, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Atributos
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float4 Vert(Atributos entrada) : SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(entrada);
                float3 positionWS = TransformObjectToWorld(entrada.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(entrada.normalOS);

                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 direccion = normalize(_LightPosition - positionWS);
                #else
                    float3 direccion = _LightDirection;
                #endif

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, direccion));
                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                return positionCS;
            }

            half4 Frag() : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            struct Atributos
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float4 Vert(Atributos entrada) : SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(entrada);
                return TransformObjectToHClip(entrada.positionOS.xyz);
            }

            half4 Frag(float4 positionCS : SV_POSITION) : SV_Target { return positionCS.z; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            struct Atributos
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Variables
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
            };

            Variables Vert(Atributos entrada)
            {
                UNITY_SETUP_INSTANCE_ID(entrada);
                Variables salida;
                salida.positionCS = TransformObjectToHClip(entrada.positionOS.xyz);
                salida.normalWS = TransformObjectToWorldNormal(entrada.normalOS);
                return salida;
            }

            half4 Frag(Variables entrada, bool frontal : SV_IsFrontFace) : SV_Target
            {
                float3 normal = normalize(entrada.normalWS) * (frontal ? 1.0 : -1.0);
                return half4(NormalizeNormalPerPixel(normal), 0);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
