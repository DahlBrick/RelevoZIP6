// Follaje de RELEVO: árboles, matas, flores y hierba de los packs de Quaternius.
//
// Hace dos cosas que URP/Lit no hace:
//  - Viento: dobla la geometría en el vértice, más cuanto más alta está sobre su base, con un
//    desfase por posición de mundo para que cada planta se meza a su ritmo y no a coro.
//  - Luz envolvente: el término difuso se "envuelve" para que la cara en sombra de la copa no se
//    vaya a negro; es lo que da el aspecto suave de diorama ilustrado.
//
// Las hojas de Quaternius usan recorte por alfa y se ven por las dos caras.
Shader "RELEVO/Follaje"
{
    Properties
    {
        _BaseMap ("Textura", 2D) = "white" {}
        _BaseColor ("Tinte", Color) = (1, 1, 1, 1)
        _Cutoff ("Recorte alfa", Range(0, 1)) = 0.4
        _Viento ("Amplitud del viento", Float) = 0.08
        _VelocidadViento ("Velocidad del viento", Float) = 1.4
        _AlturaReferencia ("Altura de referencia (unidades de mundo)", Float) = 2.0
        _Envoltura ("Luz envolvente", Range(0, 1)) = 0.45
        _ColorSombra ("Color de sombra", Color) = (0.32, 0.38, 0.45, 1)
    }

    SubShader
    {
        Tags { "RenderType" = "TransparentCutout" "Queue" = "AlphaTest" "RenderPipeline" = "UniversalPipeline" }
        Cull Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half _Cutoff;
            float _Viento;
            float _VelocidadViento;
            float _AlturaReferencia;
            half _Envoltura;
            half4 _ColorSombra;
        CBUFFER_END

        // Desplazamiento de viento en espacio de mundo para un vértice en espacio de objeto.
        float3 ConViento(float3 positionOS)
        {
            float3 positionWS = TransformObjectToWorld(positionOS);
            float3 origenWS = TransformObjectToWorld(float3(0, 0, 0));

            // La altura se mide en unidades de MUNDO sobre la base del objeto: el mismo material
            // de hojas lo comparten árboles y matas de tamaños muy distintos, y así una mata baja
            // se mece poco y la copa de un árbol mucho, sin un material por modelo.
            float altura = saturate((positionWS.y - origenWS.y) / max(0.001, _AlturaReferencia));
            float peso = pow(altura, 1.5);

            float t = _Time.y * _VelocidadViento;
            float fase = origenWS.x * 0.37 + origenWS.z * 0.51;

            float rafaga = sin(t + fase) * 0.7 + sin(t * 2.3 + fase * 1.7) * 0.3;
            float temblor = sin(t * 6.1 + positionWS.x * 3.1 + positionWS.z * 2.7) * 0.18;

            positionWS.x += (rafaga + temblor) * _Viento * peso;
            positionWS.z += (rafaga * 0.6 - temblor) * _Viento * peso * 0.7;
            return positionWS;
        }

        half Recortar(float2 uv)
        {
            half alfa = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv).a * _BaseColor.a;
            clip(alfa - _Cutoff);
            return alfa;
        }
        ENDHLSL

        Pass
        {
            Name "Follaje"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

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
                float3 positionWS = ConViento(entrada.positionOS.xyz);
                salida.positionWS = positionWS;
                salida.positionCS = TransformWorldToHClip(positionWS);
                salida.normalWS = TransformObjectToWorldNormal(entrada.normalOS);
                salida.uv = TRANSFORM_TEX(entrada.uv, _BaseMap);
                salida.fog = ComputeFogFactor(salida.positionCS.z);
                return salida;
            }

            half4 Frag(Variables entrada, bool frontal : SV_IsFrontFace) : SV_Target
            {
                half4 textura = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, entrada.uv) * _BaseColor;
                clip(textura.a - _Cutoff);

                float3 normal = normalize(entrada.normalWS) * (frontal ? 1.0 : -1.0);

                float4 coordSombra = TransformWorldToShadowCoord(entrada.positionWS);
                Light sol = GetMainLight(coordSombra);

                half lambert = dot(normal, sol.direction);
                half envuelto = saturate((lambert + _Envoltura) / (1.0 + _Envoltura));
                half luz = envuelto * sol.shadowAttenuation;

                half3 ambiente = SampleSH(normal);

                #if defined(_SCREEN_SPACE_OCCLUSION)
                    float2 uvPantalla = GetNormalizedScreenSpaceUV(entrada.positionCS);
                    AmbientOcclusionFactor oclusion = GetScreenSpaceAmbientOcclusion(uvPantalla);
                    ambiente *= oclusion.indirectAmbientOcclusion;
                #endif

                half3 sombreado = lerp(_ColorSombra.rgb, half3(1, 1, 1), luz);
                half3 color = textura.rgb * (sol.color * luz + ambiente) * 0.85 + textura.rgb * sombreado * 0.15;

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
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Variables
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Variables Vert(Atributos entrada)
            {
                UNITY_SETUP_INSTANCE_ID(entrada);
                Variables salida;
                float3 positionWS = ConViento(entrada.positionOS.xyz);
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

                salida.positionCS = positionCS;
                salida.uv = TRANSFORM_TEX(entrada.uv, _BaseMap);
                return salida;
            }

            half4 Frag(Variables entrada) : SV_Target
            {
                Recortar(entrada.uv);
                return 0;
            }
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
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Variables
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Variables Vert(Atributos entrada)
            {
                UNITY_SETUP_INSTANCE_ID(entrada);
                Variables salida;
                salida.positionCS = TransformWorldToHClip(ConViento(entrada.positionOS.xyz));
                salida.uv = TRANSFORM_TEX(entrada.uv, _BaseMap);
                return salida;
            }

            half4 Frag(Variables entrada) : SV_Target
            {
                Recortar(entrada.uv);
                return entrada.positionCS.z;
            }
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
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Variables
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            Variables Vert(Atributos entrada)
            {
                UNITY_SETUP_INSTANCE_ID(entrada);
                Variables salida;
                salida.positionCS = TransformWorldToHClip(ConViento(entrada.positionOS.xyz));
                salida.uv = TRANSFORM_TEX(entrada.uv, _BaseMap);
                salida.normalWS = TransformObjectToWorldNormal(entrada.normalOS);
                return salida;
            }

            half4 Frag(Variables entrada, bool frontal : SV_IsFrontFace) : SV_Target
            {
                Recortar(entrada.uv);
                float3 normal = normalize(entrada.normalWS) * (frontal ? 1.0 : -1.0);
                return half4(NormalizeNormalPerPixel(normal), 0);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
