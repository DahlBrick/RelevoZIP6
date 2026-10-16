// Cielo de cómic de RELEVO (skybox) para Liria y sus cinemáticas.
//
// Mismo lenguaje que RELEVO/Comic y RELEVO/ContornoTinta, para que cielo y escenario parezcan
// dibujados por la misma mano:
//  - Degradado de horizonte a cenit en dos tramos, sin ruido.
//  - Nubes de ruido proyectadas sobre un techo plano: borde duro, cara iluminada hacia el sol y
//    cara en sombra violácea, con la misma trama diagonal de las sombras del escenario.
//  - Línea de tinta fina y marrón en el contorno de cada nube.
//  - Sol como disco plano con halo en bandas.
// Las nubes derivan despacio; bajo el horizonte todo es el color del horizonte (lo tapa el mar,
// y la niebla del escenario usa ese mismo color para fundir mar y cielo).
Shader "RELEVO/CieloComic"
{
    Properties
    {
        _ColorCenit ("Cenit", Color) = (0.24, 0.47, 0.72, 1)
        _ColorMedio ("Altura media", Color) = (0.45, 0.66, 0.82, 1)
        _ColorHorizonte ("Horizonte", Color) = (0.86, 0.85, 0.76, 1)
        _ColorNube ("Nube iluminada", Color) = (1.0, 0.97, 0.9, 1)
        _ColorNubeSombra ("Nube en sombra", Color) = (0.62, 0.62, 0.78, 1)
        _ColorTinta ("Tinta", Color) = (0.24, 0.15, 0.13, 1)
        _Cobertura ("Cobertura de nubes", Range(0, 1)) = 0.42
        _Escala ("Tamaño de las nubes", Float) = 1.1
        _Velocidad ("Deriva (x, z)", Vector) = (0.006, 0.002, 0, 0)
        _GrosorTinta ("Grosor de la tinta", Range(0, 0.08)) = 0.018
        _Trama ("Trama en la sombra", Range(0, 1)) = 0.18
        _PasoTrama ("Separación de la trama (píxeles)", Float) = 6
        _TamanoSol ("Tamaño del sol", Range(0, 0.2)) = 0.035
        _ColorSol ("Sol", Color) = (1.0, 0.92, 0.7, 1)
    }

    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" "RenderPipeline" = "UniversalPipeline" }
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _ColorCenit;
                half4 _ColorMedio;
                half4 _ColorHorizonte;
                half4 _ColorNube;
                half4 _ColorNubeSombra;
                half4 _ColorTinta;
                half _Cobertura;
                float _Escala;
                float4 _Velocidad;
                half _GrosorTinta;
                half _Trama;
                float _PasoTrama;
                half _TamanoSol;
                half4 _ColorSol;
            CBUFFER_END

            struct Atributos { float4 positionOS : POSITION; };
            struct Variables
            {
                float4 positionCS : SV_POSITION;
                float3 direccion : TEXCOORD0;
            };

            Variables Vert(Atributos entrada)
            {
                Variables salida;
                salida.positionCS = TransformObjectToHClip(entrada.positionOS.xyz);
                salida.direccion = entrada.positionOS.xyz;
                return salida;
            }

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

            float Nubes(float2 p)
            {
                float v = 0, a = 0.55;
                for (int i = 0; i < 4; i++)
                {
                    v += Ruido(p) * a;
                    p = p * 2.03 + float2(17.1, 9.3);
                    a *= 0.45;
                }
                return v;
            }

            half4 Frag(Variables entrada) : SV_Target
            {
                float3 dir = normalize(entrada.direccion);
                float altura = saturate(dir.y);

                half3 color = lerp(_ColorHorizonte.rgb, _ColorMedio.rgb, smoothstep(0.0, 0.18, altura));
                color = lerp(color, _ColorCenit.rgb, smoothstep(0.18, 0.75, altura));

                // Sol: disco plano y un halo en dos bandas.
                float3 sol = normalize(_MainLightPosition.xyz);
                float cerca = dot(dir, sol);
                float halo = smoothstep(1.0 - _TamanoSol * 2.2, 1.0 - _TamanoSol * 2.0, cerca) * 0.18
                           + smoothstep(1.0 - _TamanoSol * 1.3, 1.0 - _TamanoSol * 1.15, cerca) * 0.22;
                color = lerp(color, _ColorSol.rgb, halo);
                half disco = smoothstep(1.0 - _TamanoSol * 0.55, 1.0 - _TamanoSol * 0.5, cerca);
                color = lerp(color, _ColorSol.rgb * 1.15, disco);

                // Nubes en un techo plano: más pequeñas y apretadas hacia el horizonte.
                if (dir.y > 0.0)
                {
                    float2 uv = dir.xz / (dir.y + 0.12) * _Escala + _Velocidad.xy * _Time.y * 60.0;
                    float umbral = 1.0 - _Cobertura;
                    float densidad = Nubes(uv) - umbral;

                    // Cara al sol: la densidad un poco más hacia el sol es menor en el lado iluminado.
                    float2 haciaSol = normalize(sol.xz + 1e-4) * 0.12;
                    float delante = Nubes(uv + haciaSol) - umbral;
                    half iluminada = step(delante, densidad + 0.015);

                    half nube = step(0.0, densidad);
                    half desvanece = smoothstep(0.02, 0.12, dir.y);
                    nube *= desvanece;

                    half3 tono = lerp(_ColorNubeSombra.rgb, _ColorNube.rgb, iluminada);
                    float2 pixel = entrada.positionCS.xy;
                    half raya = step(frac((pixel.x + pixel.y) / _PasoTrama), 0.3);
                    tono *= 1.0 - raya * _Trama * (1.0 - iluminada);

                    color = lerp(color, tono, nube);

                    // Tinta en el contorno: la franja justo por encima del umbral.
                    half borde = (1.0 - smoothstep(0.0, _GrosorTinta, densidad)) * nube;
                    color = lerp(color, _ColorTinta.rgb, borde * 0.5);
                }

                return half4(color, 1);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
