﻿using System;
using System.Linq;
using UnityEditor;
using UnityEngine;


namespace UniGLTF.UniUnlit
{
    public class UniUnlitEditor : ShaderGUI
    {
        private MaterialProperty _mainTex;
        private MaterialProperty _color;
        private MaterialProperty _cutoff;
        private MaterialProperty _blendMode;
        private MaterialProperty _cullMode;
        private MaterialProperty _vColBlendMode;
//        private MaterialProperty _srcBlend;
//        private MaterialProperty _dstBlend;
//        private MaterialProperty _zWrite;
        
        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            _mainTex = FindProperty(UnlitExtension.PropNameMainTex, properties);
            _color = FindProperty(UnlitExtension.PropNameColor, properties);
            _cutoff = FindProperty(UnlitExtension.PropNameCutoff, properties);
            _blendMode = FindProperty(UnlitExtension.PropNameBlendMode, properties);
            _cullMode = FindProperty(UnlitExtension.PropNameCullMode, properties);
            _vColBlendMode = FindProperty(UnlitExtension.PropeNameVColBlendMode, properties);
//            _srcBlend = FindProperty(PropNameSrcBlend, properties);
//            _dstBlend = FindProperty(PropNameDstBlend, properties);
//            _zWrite = FindProperty(PropNameZWrite, properties);

            var materials = materialEditor.targets.Select(x => x as Material).ToArray();
            
            EditorGUI.BeginChangeCheck();
            {
                DrawRenderingBox(materialEditor, materials);
                DrawColorBox(materialEditor, materials);
                DrawOptionsBox(materialEditor, materials);
            }
            EditorGUI.EndChangeCheck();
        }

        public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
        {
            var blendMode = UniUnlitRenderMode.Opaque;
            if (material.HasProperty(UnlitExtension.PropNameStandardShadersRenderMode)) // from Standard shader
            {
                blendMode = (UniUnlitRenderMode) Math.Min(2f, material.GetFloat(UnlitExtension.PropNameStandardShadersRenderMode));
            }

            // assigns UniUnlit's properties...
            base.AssignNewShaderToMaterial(material, oldShader, newShader);

            // take over old value
            material.SetFloat(UnlitExtension.PropNameBlendMode, (float) blendMode);

            material.ValidateProperties(isRenderModeChangedByUser: true);
        }

        private void DrawRenderingBox(MaterialEditor materialEditor, Material[] materials)
        {
            EditorGUILayout.LabelField("Rendering", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(GUI.skin.box);
            {
                if (PopupEnum<UniUnlitRenderMode>("Rendering Type", _blendMode, materialEditor))
                {
                    ModeChanged(materials, isRenderModeChangedByUser: true);
                }
                if (PopupEnum<UniUnlitCullMode>("Cull Mode", _cullMode, materialEditor))
                {
                    ModeChanged(materials, isRenderModeChangedByUser: true);
                }
                EditorGUILayout.Space();

                switch ((UniUnlitRenderMode) _blendMode.floatValue)
                {
                    case UniUnlitRenderMode.Cutout:
                        materialEditor.ShaderProperty(_cutoff, "Cutoff");
                        break;
                    case UniUnlitRenderMode.Opaque:
                    case UniUnlitRenderMode.Transparent:
                        break;
                }
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }
        
        private void DrawColorBox(MaterialEditor materialEditor, Material[] materials)
        {
            EditorGUILayout.LabelField("Color", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(GUI.skin.box);
            {
                materialEditor.TexturePropertySingleLine(new GUIContent("Main Tex", "(RGBA)"), _mainTex, _color);
                materialEditor.TextureScaleOffsetProperty(_mainTex);
                EditorGUILayout.Space();
                
                if (PopupEnum<UniUnlitVertexColorBlendOp>("Vertex Color Blend Mode", _vColBlendMode, materialEditor))
                {
                    ModeChanged(materials, isRenderModeChangedByUser: true);
                }
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        private void DrawOptionsBox(MaterialEditor materialEditor, Material[] materials)
        {
            EditorGUILayout.LabelField("Options", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(GUI.skin.box);
            {
                #if UNITY_5_6_OR_NEWER
//                    materialEditor.EnableInstancingField();
                    materialEditor.DoubleSidedGIField();
                #endif
                    materialEditor.RenderQueueField();
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }
        
        private static bool PopupEnum<T>(string name, MaterialProperty property, MaterialEditor editor) where T : struct
        {
            EditorGUI.showMixedValue = property.hasMixedValue;
            EditorGUI.BeginChangeCheck();
            var ret = EditorGUILayout.Popup(name, (int) property.floatValue, Enum.GetNames(typeof(T)));
            var changed = EditorGUI.EndChangeCheck();
            if (changed)
            {
                editor.RegisterPropertyChangeUndo("EnumPopUp");
                property.floatValue = ret;
            }
            EditorGUI.showMixedValue = false;
            return changed;
        }


        private static void ModeChanged(Material[] materials, bool isRenderModeChangedByUser = false)
        {
            foreach (var material in materials)
            {
                material.ValidateProperties(isRenderModeChangedByUser);
            }
        }
    }
}
