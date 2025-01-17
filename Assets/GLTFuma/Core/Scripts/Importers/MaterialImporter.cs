﻿using UnityEngine;
using UniGLTF.UniUnlit;


namespace UMa.GLTF
{
    public interface IMaterialImporter
    {
        Material CreateMaterial(int i, GLTFMaterial src);
    }

    public class MaterialImporter : IMaterialImporter
    {
        IShaderStore m_shaderStore;

        private GLTFImporter m_context;

        public MaterialImporter(IShaderStore shaderStore, GLTFImporter context)
        {
            m_shaderStore = shaderStore;
            m_context = context;
        }

        private enum BlendMode
        {
            Opaque,
            Cutout,
            Fade,
            Transparent
        }

        /// StandardShader vaiables
        /// 
        /// _Color
        /// _MainTex
        /// _Cutoff
        /// _Glossiness
        /// _Metallic
        /// _MetallicGlossMap
        /// _BumpScale
        /// _BumpMap
        /// _Parallax
        /// _ParallaxMap
        /// _OcclusionStrength
        /// _OcclusionMap
        /// _EmissionColor
        /// _EmissionMap
        /// _DetailMask
        /// _DetailAlbedoMap
        /// _DetailNormalMapScale
        /// _DetailNormalMap
        /// _UVSec
        /// _EmissionScaleUI
        /// _EmissionColorUI
        /// _Mode
        /// _SrcBlend
        /// _DstBlend
        /// _ZWrite
        public virtual Material CreateMaterial(int i, GLTFMaterial x)
        {
            var shader = m_shaderStore.GetShader(x);
            //Debug.LogFormat("[{0}]{1}", i, shader.name);
            var material = new Material(shader);
#if UNITY_EDITOR
            // textureImporter.SaveAndReimport(); may destory this material
            material.hideFlags = HideFlags.DontUnloadUnusedAsset;
#endif

            material.name = (x == null || string.IsNullOrEmpty(x.name))
                ? string.Format("material_{0:00}", i)
                : x.name
                ;

            if (x == null)
            {
                Debug.LogWarning("glTFMaterial is empty");
                return material;
            }

            // unlit material
            if (x.extensions != null && x.extensions.KHR_materials_unlit != null)
            {
                // texture
                m_context.SetMaterialTexture(material,x.pbrMetallicRoughness.baseColorTexture.index,null);
                // var texture = m_context.GetTexture(x.pbrMetallicRoughness.baseColorTexture.index);
                // if (texture != null)
                // {
                //     material.mainTexture = texture.Texture;
                // }

                // color
                if (x.pbrMetallicRoughness.baseColorFactor != null && x.pbrMetallicRoughness.baseColorFactor.Length == 4)
                {
                    var color = x.pbrMetallicRoughness.baseColorFactor;
                    material.color = new Color(color[0], color[1], color[2], color[3]);
                }

                //renderMode
                if (x.alphaMode == "OPAQUE")
                {
                    material.SetRenderMode(UniUnlitRenderMode.Opaque);
                }
                else if (x.alphaMode == "BLEND")
                {
                    material.SetRenderMode(UniUnlitRenderMode.Transparent);
                }
                else if(x.alphaMode == "MASK")
                {
                    material.SetRenderMode(UniUnlitRenderMode.Cutout);
                }
                else
                {
                    // default OPAQUE
                    material.SetRenderMode(UniUnlitRenderMode.Opaque);
                }

                // culling
                if (x.doubleSided)
                {
                    material.SetCullMode(UniUnlitCullMode.Off);
                }
                else
                {
                    material.SetCullMode(UniUnlitCullMode.Back);
                }

                material.ValidateProperties(true);

                return material;
            }

            // PBR material
            if (x.pbrMetallicRoughness != null)
            {
                if (x.pbrMetallicRoughness.baseColorFactor != null && x.pbrMetallicRoughness.baseColorFactor.Length == 4)
                {
                    var color = x.pbrMetallicRoughness.baseColorFactor;
                    material.color = new Color(color[0], color[1], color[2], color[3]);
                }

                if (x.pbrMetallicRoughness.baseColorTexture != null && x.pbrMetallicRoughness.baseColorTexture.index != -1)
                {
                    m_context.SetMaterialTexture(material,x.pbrMetallicRoughness.baseColorTexture.index,null);
                    // var texture = m_context.GetTexture(x.pbrMetallicRoughness.baseColorTexture.index);
                    // if (texture != null)
                    // {
                    //     material.mainTexture = texture.Texture;
                    // }
                }

                if (x.pbrMetallicRoughness.metallicRoughnessTexture != null && x.pbrMetallicRoughness.metallicRoughnessTexture.index != -1)
                {
                    material.EnableKeyword("_METALLICGLOSSMAP");
                    m_context.SetMaterialTexture(material,x.pbrMetallicRoughness.metallicRoughnessTexture.index,"_MetallicGlossMap");
                    // var texture = Context.GetTexture(x.pbrMetallicRoughness.metallicRoughnessTexture.index);
                    // if (texture != null)
                    // {
                    //     var prop = "_MetallicGlossMap";
                    //     material.SetTexture(prop, texture.ConvertTexture(prop));
                    // }
                }

                material.SetFloat("_Metallic", x.pbrMetallicRoughness.metallicFactor);
                material.SetFloat("_Glossiness", 1.0f - x.pbrMetallicRoughness.roughnessFactor);
            }

            if (x.normalTexture != null && x.normalTexture.index != -1)
            {
                material.EnableKeyword("_NORMALMAP");
                if(m_context.SetMaterialTexture(material,x.normalTexture.index,"_BumpMap")){
                // var texture = Context.GetTexture(x.normalTexture.index);
                // if (texture != null)
                // {
                //     var prop = "_BumpMap";
                //     material.SetTexture(prop, texture.ConvertTexture(prop));
                    material.SetFloat("_BumpScale", x.normalTexture.scale);
                }
            }

            if (x.occlusionTexture != null && x.occlusionTexture.index != -1)
            {
                if(m_context.SetMaterialTexture(material,x.occlusionTexture.index,"_OcclusionMap")){
                // var texture = Context.GetTexture(x.occlusionTexture.index);
                // if (texture != null)
                // {
                //     var prop = "_OcclusionMap";
                //     material.SetTexture(prop, texture.ConvertTexture(prop));
                    material.SetFloat("_OcclusionStrength", x.occlusionTexture.strength);
                }
            }

            if (x.emissiveFactor != null
                || (x.emissiveTexture != null && x.emissiveTexture.index != -1))
            {
                material.EnableKeyword("_EMISSION");
                material.globalIlluminationFlags &= ~MaterialGlobalIlluminationFlags.EmissiveIsBlack;

                if (x.emissiveFactor != null && x.emissiveFactor.Length == 3)
                {
                    material.SetColor("_EmissionColor", new Color(x.emissiveFactor[0], x.emissiveFactor[1], x.emissiveFactor[2]));
                }

                if (x.emissiveTexture.index != -1)
                {
                    // var texture = Context.GetTexture(x.emissiveTexture.index);
                    // if (texture != null)
                    // {
                    //     material.SetTexture("_EmissionMap", texture.Texture);
                    // }
                    m_context.SetMaterialTexture(material,x.emissiveTexture.index,"_EmissionMap");
                }
            }

            BlendMode blendMode = BlendMode.Opaque;
            // https://forum.unity.com/threads/standard-material-shader-ignoring-setfloat-property-_mode.344557/#post-2229980
            switch (x.alphaMode)
            {
                case "BLEND":
                    blendMode = BlendMode.Fade;
                    material.SetOverrideTag("RenderType", "Transparent");
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    material.SetInt("_ZWrite", 0);
                    material.DisableKeyword("_ALPHATEST_ON");
                    material.EnableKeyword("_ALPHABLEND_ON");
                    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    material.renderQueue = 3000;
                    break;

                case "MASK":
                    blendMode = BlendMode.Cutout;
                    material.SetOverrideTag("RenderType", "TransparentCutout");
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    material.SetInt("_ZWrite", 1);
                    material.EnableKeyword("_ALPHATEST_ON");
                    material.DisableKeyword("_ALPHABLEND_ON");
                    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    material.renderQueue = 2450;
                        
                    break;

                default: // OPAQUE
                    blendMode = BlendMode.Opaque;
                    material.SetOverrideTag("RenderType", "");
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    material.SetInt("_ZWrite", 1);
                    material.DisableKeyword("_ALPHATEST_ON");
                    material.DisableKeyword("_ALPHABLEND_ON");
                    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    material.renderQueue = -1;
                    break;
            }

            material.SetFloat("_Mode", (float)blendMode);
            return material;
        }
    }
}
