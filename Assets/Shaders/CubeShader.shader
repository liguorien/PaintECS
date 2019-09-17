// Shader targeted for low end devices. Single Pass Forward Rendering.
Shader "PaintECS/PixelCube"
{
    // Keep properties of StandardSpecular shader for upgrade reasons.
    Properties
    {
        _BaseColor("Base Color", Color) = (0.5, 0.5, 0.5, 1)
        _BaseMap("Base Map (RGB) Smoothness / Alpha (A)", 2D) = "white" {}

        _Cutoff("Alpha Clipping", Range(0.0, 1.0)) = 0.5

        _SpecColor("Specular Color", Color) = (0.5, 0.5, 0.5, 0.5)
        _SpecGlossMap("Specular Map", 2D) = "white" {}
        [Enum(Specular Alpha,0,Albedo Alpha,1)] _SmoothnessSource("Smoothness Source", Float) = 0.0
        [ToggleOff] _SpecularHighlights("Specular Highlights", Float) = 1.0

        [HideInInspector] _BumpScale("Scale", Float) = 1.0
        [NoScaleOffset] _BumpMap("Normal Map", 2D) = "bump" {}

        _EmissionColor("Emission Color", Color) = (0,0,0)
        [NoScaleOffset]_EmissionMap("Emission Map", 2D) = "white" {}

        // Blending state
        [HideInInspector] _Surface("__surface", Float) = 0.0
        [HideInInspector] _Blend("__blend", Float) = 0.0
        [HideInInspector] _AlphaClip("__clip", Float) = 0.0
        [HideInInspector] _SrcBlend("__src", Float) = 1.0
        [HideInInspector] _DstBlend("__dst", Float) = 0.0
        [HideInInspector] _ZWrite("__zw", Float) = 1.0
        [HideInInspector] _Cull("__cull", Float) = 2.0

        [ToogleOff] _ReceiveShadows("Receive Shadows", Float) = 1.0

        // Editmode props
        [HideInInspector] _QueueOffset("Queue offset", Float) = 0.0
        [HideInInspector] _Smoothness("SMoothness", Float) = 0.5

        // ObsoleteProperties
        [HideInInspector] _MainTex("BaseMap", 2D) = "white" {}
        [HideInInspector] _Color("Base Color", Color) = (0.5, 0.5, 0.5, 1)
        [HideInInspector] _Shininess("Smoothness", Float) = 0.0
        [HideInInspector] _GlossinessSource("GlossinessSource", Float) = 0.0
        [HideInInspector] _SpecSource("SpecularHighlights", Float) = 0.0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True"}
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            // Use same blending / depth states as Standard shader
            Blend[_SrcBlend][_DstBlend]
            ZWrite[_ZWrite]
            Cull[_Cull]

            HLSLPROGRAM
            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature _ALPHATEST_ON
            #pragma shader_feature _ALPHAPREMULTIPLY_ON
            #pragma shader_feature _ _SPECGLOSSMAP _SPECULAR_COLOR
            #pragma shader_feature _GLOSSINESS_FROM_BASE_ALPHA
            #pragma shader_feature _NORMALMAP
            #pragma shader_feature _EMISSION
            #pragma shader_feature _RECEIVE_SHADOWS_OFF

            // -------------------------------------
            // Universal Pipeline keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fog

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing

            #pragma vertex LitPassVertexSimple
            #pragma fragment LitPassFragmentSimple2
            #define BUMP_SCALE_NOT_SUPPORTED 1
            
//            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
           
         
            
            #include "Assets/Shaders/PixelCubeInput.hlsl"
            //#include "Packages/com.unity.render-pipelines.universal/Shaders/SimpleLitInput.hlsl"
          //  #include "Packages/com.unity.render-pipelines.universal/Shaders/SimpleLitForwardPass.hlsl"
            
            // Used for StandardSimpleLighting shader
            half4 LitPassFragmentSimple2(Varyings input) : SV_Target
            {
            /*
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            
                float2 uv = input.uv;
                half4 diffuseAlpha = SampleAlbedoAlpha(uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap));
                half3 diffuse = diffuseAlpha.rgb * UNITY_ACCESS_INSTANCED_PROP(PerInstance, _BaseColor).rgb;
            
                half alpha = diffuseAlpha.a * UNITY_ACCESS_INSTANCED_PROP(PerInstance, _BaseColor).a;
                AlphaDiscard(alpha, _Cutoff);
            #ifdef _ALPHAPREMULTIPLY_ON
                diffuse *= alpha;
            #endif
            
                half3 normalTS = SampleNormal(uv, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap));
                half3 emission = SampleEmission(uv, _EmissionColor.rgb, TEXTURE2D_ARGS(_EmissionMap, sampler_EmissionMap));
                half4 specular = SampleSpecularSmoothness(uv, alpha, _SpecColor, TEXTURE2D_ARGS(_SpecGlossMap, sampler_SpecGlossMap));
                half smoothness = specular.a;
            
                InputData inputData;
                InitializeInputData(input, normalTS, inputData);
            
                half4 color = UniversalFragmentBlinnPhong(inputData, diffuse, specular, smoothness, emission, alpha);
                color.rgb = MixFog(color.rgb, inputData.fogCoord);
              */  
                 return UNITY_ACCESS_INSTANCED_PROP(PerInstance, _BaseColor);
             //   return color;
            };
            
            
            //UNITY_INSTANCING_BUFFER_START(InstanceProperties)
            //UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
            //UNITY_INSTANCING_BUFFER_END(InstanceProperties)
            ENDHLSL
        }

//        Pass
//        {
//            Name "ShadowCaster"
//            Tags{"LightMode" = "ShadowCaster"}
//
//            ZWrite On
//            ZTest LEqual
//            Cull[_Cull]
//
//            HLSLPROGRAM
//            // Required to compile gles 2.0 with standard srp library
//            #pragma prefer_hlslcc gles
//            #pragma exclude_renderers d3d11_9x
//            #pragma target 2.0
//
//            // -------------------------------------
//            // Material Keywords
//            #pragma shader_feature _ALPHATEST_ON
//            #pragma shader_feature _GLOSSINESS_FROM_BASE_ALPHA
//
//            //--------------------------------------
//            // GPU Instancing
//            #pragma multi_compile_instancing
//
//            #pragma vertex ShadowPassVertex
//            #pragma fragment ShadowPassFragment
//       //     #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
////             UNITY_INSTANCING_BUFFER_START(Props)
////            UNITY_DEFINE_INSTANCED_PROP(float, _BaseColor)
////            UNITY_INSTANCING_BUFFER_END(Props)
//
//            #include "Assets/Shaders/PixelCubeInput.hlsl"
//            
////                   UNITY_INSTANCING_BUFFER_START(Props)
////            UNITY_DEFINE_INSTANCED_PROP(float, _BaseColor)
////            UNITY_INSTANCING_BUFFER_END(Props)
//            
//            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
//            
//       
//            
//            ENDHLSL
//        }
//
//        Pass
//        {
//            Name "DepthOnly"
//            Tags{"LightMode" = "DepthOnly"}
//
//            ZWrite On
//            ColorMask 0
//            Cull[_Cull]
//
//            HLSLPROGRAM
//            // Required to compile gles 2.0 with standard srp library
//            #pragma prefer_hlslcc gles
//            #pragma exclude_renderers d3d11_9x
//            #pragma target 2.0
//
//            #pragma vertex DepthOnlyVertex
//            #pragma fragment DepthOnlyFragment
//
//            // -------------------------------------
//            // Material Keywords
//            #pragma shader_feature _ALPHATEST_ON
//            #pragma shader_feature _GLOSSINESS_FROM_BASE_ALPHA
//
//            //--------------------------------------
//            // GPU Instancing
//            #pragma multi_compile_instancing
////#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
//         
//
//            #include "Assets/Shaders/PixelCubeInput.hlsl"
//            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
//            
////                UNITY_INSTANCING_BUFFER_START(InstanceProperties)
////            UNITY_DEFINE_INSTANCED_PROP(float, _BaseColor)
////            UNITY_INSTANCING_BUFFER_END(InstanceProperties)
//            
//            ENDHLSL
//        }
//
//        // This pass it not used during regular rendering, only for lightmap baking.
//        Pass
//        {
//            Name "Meta"
//            Tags{ "LightMode" = "Meta" }
//
//            Cull Off
//
//            HLSLPROGRAM
//            // Required to compile gles 2.0 with standard srp library
//            #pragma prefer_hlslcc gles
//            #pragma exclude_renderers d3d11_9x
//
//            #pragma vertex UniversalVertexMeta
//            #pragma fragment UniversalFragmentMetaSimple
//
//            #pragma shader_feature _EMISSION 
//            #pragma shader_feature _SPECGLOSSMAP
//
//
//            #include "Assets/Shaders/PixelCubeInput.hlsl"
//            #include "Packages/com.unity.render-pipelines.universal/Shaders/SimpleLitMetaPass.hlsl"
////            
////              UNITY_INSTANCING_BUFFER_START(InstanceProperties)
////            UNITY_DEFINE_INSTANCED_PROP(float, _BaseColor)
////            UNITY_INSTANCING_BUFFER_END(InstanceProperties)
//
//            ENDHLSL
//        }
//        Pass
//        {
//            Name "Universal2D"
//            Tags{ "LightMode" = "Universal2D" }
//            Tags{ "RenderType" = "Transparent" "Queue" = "Transparent" }
//
//            HLSLPROGRAM
//            // Required to compile gles 2.0 with standard srp library
//            #pragma prefer_hlslcc gles
//            #pragma exclude_renderers d3d11_9x
//
//            #pragma vertex vert
//            #pragma fragment frag
//            #pragma shader_feature _ALPHATEST_ON
//            #pragma shader_feature _ALPHAPREMULTIPLY_ON
//
//
//            #include "Assets/Shaders/PixelCubeInput.hlsl"
//            #include "Packages/com.unity.render-pipelines.universal/Shaders/Utils/Universal2D.hlsl"
////            
////              UNITY_INSTANCING_BUFFER_START(InstanceProperties)
////            UNITY_DEFINE_INSTANCED_PROP(float, _BaseColor)
////            UNITY_INSTANCING_BUFFER_END(InstanceProperties)
//            ENDHLSL
//        }
    }
    Fallback "Hidden/InternalErrorShader"
    CustomEditor "UnityEditor.Rendering.Universal.ShaderGUI.SimpleLitShader"
}
