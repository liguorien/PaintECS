// When creating shaders for Lightweight Pipeline you can you the ShaderGraph which is super AWESOME!
// However, if you want to author shaders in shading language you can use this simplified version as a base.
// Please not this should be only use for reference only.
// It doesn't match neither performance not feature completeness of Lightweight Pipeline Standard shader.
Shader "LightweightPipeline/Physically Based Example"
{
    Properties
    {
        // Specular vs Metallic workflow
        [HideInInspector] _WorkflowMode("WorkflowMode", Float) = 1.0

        _Color("Color", Color) = (0.5,0.5,0.5,1)
        _MainTex("Albedo", 2D) = "white" {}

        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        _Glossiness("Smoothness", Range(0.0, 1.0)) = 0.5
        _GlossMapScale("Smoothness Scale", Range(0.0, 1.0)) = 1.0
        _SmoothnessTextureChannel("Smoothness texture channel", Float) = 0

        [Gamma] _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        _MetallicGlossMap("Metallic", 2D) = "white" {}

        _SpecColor("Specular", Color) = (0.2, 0.2, 0.2)
        _SpecGlossMap("Specular", 2D) = "white" {}

        [ToggleOff] _SpecularHighlights("Specular Highlights", Float) = 1.0
        [ToggleOff] _GlossyReflections("Glossy Reflections", Float) = 1.0

        _BumpScale("Scale", Float) = 1.0
        _BumpMap("Normal Map", 2D) = "bump" {}

        _OcclusionStrength("Strength", Range(0.0, 1.0)) = 1.0
        _OcclusionMap("Occlusion", 2D) = "white" {}

        _EmissionColor("Color", Color) = (0,0,0)
        _EmissionMap("Emission", 2D) = "white" {}

        // Blending state
        [HideInInspector] _Surface("__surface", Float) = 0.0
        [HideInInspector] _Blend("__blend", Float) = 0.0
        [HideInInspector] _AlphaClip("__clip", Float) = 0.0
        [HideInInspector] _SrcBlend("__src", Float) = 1.0
        [HideInInspector] _DstBlend("__dst", Float) = 0.0
        [HideInInspector] _ZWrite("__zw", Float) = 1.0
        [HideInInspector] _Cull("__cull", Float) = 2.0
    }

    SubShader
    {
        // Lightweight Pipeline tag is required. If Lightweight pipeline is not set in the graphics settings
        // this Subshader will fail. One can add a subshader below or fallback to Standard built-in to make this
        // material work with both Lightweight Pipeline and Builtin Unity Pipeline
        Tags{"RenderType" = "Opaque" "RenderPipeline" = "LightweightPipeline" "IgnoreProjector" = "True"}
        LOD 300

        // ------------------------------------------------------------------
        //  Forward pass. Shades all light in a single pass. GI + emission + Fog
        Pass
        {
            // Lightmode matches the ShaderPassName set in LightweightPipeline.cs. SRPDefaultUnlit and passes with
            // no LightMode tag are also rendered by Lightweight Pipeline
            Name "StandardLit"
            Tags{"LightMode" = "LightweightForward"}

            Blend[_SrcBlend][_DstBlend]
            ZWrite[_ZWrite]
            Cull[_Cull]

            HLSLPROGRAM
            // Required to compile gles 2.0 with standard SRP library
            // All shaders must be compiled with HLSLcc and currently only gles is not using HLSLcc by default
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature _NORMALMAP
            #pragma shader_feature _ALPHATEST_ON
            #pragma shader_feature _ALPHAPREMULTIPLY_ON
            #pragma shader_feature _EMISSION
            #pragma shader_feature _METALLICSPECGLOSSMAP
            #pragma shader_feature _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            #pragma shader_feature _OCCLUSIONMAP

            #pragma shader_feature _SPECULARHIGHLIGHTS_OFF
            #pragma shader_feature _GLOSSYREFLECTIONS_OFF
            #pragma shader_feature _SPECULAR_SETUP

            // -------------------------------------
            // Lightweight Pipeline keywords
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _VERTEX_LIGHTS
            #pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE
            #pragma multi_compile _ _SHADOWS_ENABLED
            #pragma multi_compile _ _LOCAL_SHADOWS_ENABLED
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ _SHADOWS_CASCADE

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fog

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing

            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment

            // Including the following two function is enought for shading with Lightweight Pipeline. Everything is included in them.
            // Core.hlsl will include SRP shader library, all constant buffers not related to materials (perobject, percamera, perframe).
            // It also includes matrix/space conversion functions and fog.
            // Lighting.hlsl will include the light functions/data to abstract light constants. You should use GetMainLight and GetLight functions
            // that initialize Light struct. Lighting.hlsl also include GI, Light BDRF functions. It also includes Shadows.
            #include "LWRP/ShaderLibrary/Core.hlsl"
            #include "LWRP/ShaderLibrary/Lighting.hlsl"

            // Not required but included here for simplicity. This defines all material related constants for the Standard surface shader like _Color, _MainTex, and so on.
            // These are specific to this shader. You should define your own constants.
            #include "LWRP/ShaderLibrary/InputSurfacePBR.hlsl"

            struct VertexInput
            {
                float4 vertex       : POSITION;
                float3 normal       : NORMAL;
                float4 tangent      : TANGENT;
                float2 texcoord     : TEXCOORD0;
                float2 lightmapUV   : TEXCOORD1;
            };

            struct VertexOutput
            {
                float2 uv                       : TEXCOORD0;
                float2 lightmapUV               : TEXCOORD1;
                float4 positionWSAndFogFactor   : TEXCOORD2;
                half3  normal                   : TEXCOORD3;

#if _NORMALMAP
                half3 tangent                   : TEXCOORD4;
                half3 binormal                  : TEXCOORD5;
#endif

#ifdef _SHADOWS_ENABLED
                float4 shadowCoord              : TEXCOORD6;
#endif
                float4 clipPos                  : SV_POSITION;
            };

            VertexOutput LitPassVertex(VertexInput v)
            {
                VertexOutput o = (VertexOutput)0;

                // Pretty much same as builtin Unity shader library.
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.lightmapUV = v.lightmapUV.xy * unity_LightmapST.xy + unity_LightmapST.zw;

                // SRP shader library adds some functions to convert between spaces.
                // TransformObjectToHClip and some other functions are defined.
                
                float3 positionWS = TransformObjectToWorld(v.vertex.xyz);
                o.clipPos = TransformWorldToHClip(positionWS);
                
                float fogFactor = ComputeFogFactor(o.clipPos.z);
                o.positionWSAndFogFactor = float4(positionWS, fogFactor);

#ifdef _NORMALMAP
                OutputTangentToWorld(v.tangent, v.normal, o.tangent, o.binormal, o.normal);
#else
                o.normal = TransformObjectToWorldNormal(v.normal);
#endif

#ifdef _SHADOWS_ENABLED
        #if SHADOWS_SCREEN
                o.shadowCoord = ComputeShadowCoord(o.clipPos);
        #else
                o.shadowCoord = TransformWorldToShadowCoord(positionWS);
        #endif
#endif
                return o;
            }

            half4 LitPassFragment(VertexOutput IN) : SV_Target
            {
                // Surface data contains albedo, metallic, specular, smoothness, occlusion, emission and alpha
                // InitializeStandarLitSurfaceData initializes based on the rules for standard shader.
                // You can write your own function to initialize the surface data of your shader.
                SurfaceData surfaceData;
                InitializeStandardLitSurfaceData(IN.uv, surfaceData);

#if _NORMALMAP
                half3 normalWS = TangentToWorldNormal(surfaceData.normalTS, IN.tangent, IN.binormal, IN.normal);
#else
                half3 normalWS = normalize(IN.normal);
#endif

#ifdef LIGHTMAP_ON
                half3 bakedGI = SampleLightmap(IN.lightmapUV, normalWS);
#else
                half3 bakedGI = SampleSH(normalWS);
#endif

                float3 positionWS = IN.positionWSAndFogFactor.xyz;
                half3 viewDirectionWS = SafeNormalize(GetCameraPositionWS() - positionWS);

                // BRDFData holds energy conserving diffuse and specular material reflections and its roughness.
                // It's easy to plugin your own shading fuction. You just need replace LightingPhysicallyBased function
                // below with your own.
                BRDFData brdfData;
                InitializeBRDFData(surfaceData.albedo, surfaceData.metallic, surfaceData.specular, surfaceData.smoothness, surfaceData.alpha, brdfData);

                // Main light is the brighest directional light
                // Main light uses a separate set of constant buffers for light to avoid indexing and have tight memory access.
                Light mainLight = GetMainLight();
#ifdef _SHADOWS_ENABLED
                mainLight.attenuation = MainLightRealtimeShadowAttenuation(IN.shadowCoord);
#endif

                half3 color = GlobalIllumination(brdfData, bakedGI, surfaceData.occlusion, normalWS, viewDirectionWS);
                color += LightingPhysicallyBased(brdfData, mainLight, normalWS, viewDirectionWS);

#ifdef _ADDITIONAL_LIGHTS
                int pixelLightCount = GetPixelLightCount();
                for (int i = 0; i < pixelLightCount; ++i)
                {
                    Light light = GetLight(i, positionWS);
                    light.attenuation *= LocalLightRealtimeShadowAttenuation(light.index, positionWS);
                    color += LightingPhysicallyBased(brdfData, light, normalWS, viewDirectionWS);
                }
#endif

                color += surfaceData.emission;

                float fogFactor = IN.positionWSAndFogFactor.w;
                ApplyFog(color, fogFactor);
                return half4(color, surfaceData.alpha);
            }
            ENDHLSL
        }

        // Used for shadows
        UsePass "LightweightPipeline/Standard (Physically Based)/ShadowCaster"
        
        // Used for depth prepass
        // If shadows cascade are enabled we need to perform a depth prepass. 
        // We also need to use a depth prepass in some cases camera require depth texture
        // (e.g, MSAA is enabled and we can't resolve with Texture2DMS
        UsePass "LightweightPipeline/Standard (Physically Based)/DepthOnly"

        // Used for Baking GI. This pass is stripped from build.
        UsePass "LightweightPipeline/Standard (Physically Based)/Meta"
    }

    FallBack "Hidden/InternalErrorShader"
    CustomEditor "LightweightStandardGUI"
}