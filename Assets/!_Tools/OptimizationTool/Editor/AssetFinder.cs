using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Android;
using TMPro;

namespace OptimisationTool
{
    public sealed class AssetFinder
    {
        private HashSet<string> _allAssets;
        private List<TMP_FontAsset> _allFonts;
        private List<string> _fontsPath;
        private HashSet<string> _usedAssets;
        private Object[] _resources;

        private void Initialize()
        {
            var allAssets = AssetDatabase.FindAssets("t:texture t:material t:audioClip t:prefab", new[] { "Assets" });
            _allFonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>().ToList();
            _fontsPath = new List<string>();
            foreach (var font in _allFonts)
            {
                string assetPath = AssetDatabase.GetAssetPath(font);
                _fontsPath.Add(assetPath);
            }
            _allAssets = allAssets.ToHashSet();
            _allAssets.UnionWith(_fontsPath);
            _usedAssets = new HashSet<string>();
            _resources = Resources.FindObjectsOfTypeAll(typeof(GameObject));
        }

        public List<string> FindUnusedAssets()
        {
            Initialize();

            FindUsedAssetsInResources();
            FindUsedAssetsInScriptableObjects();
            FindUsedAssetsInIcons();
            FindUsedAssetsInAnimations();
            FindUnusedAssetsInFonts();
            FindUsedAssetsInPrefabInstances();
            FindUsedAssetsInMaterials();

            List<string> unusedAssets = new List<string>();

            foreach (string assetGuid in _allAssets)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);

                if (!IsInResourcesFolder(assetPath) && !_usedAssets.Contains(assetPath))
                {
                    unusedAssets.Add(assetPath);
                }
            }

            return unusedAssets;
        }

        private void FindUsedAssetsInResources()
        {
            foreach (var obj in _resources.OfType<GameObject>())
            {
                FindUsedAssetsInGameObjectComponents(obj);
                FindUsedAssetsInRendererMaterials(obj);
            }
        }

        private void FindUsedAssetsInGameObjectComponents(GameObject go)
        {
            var components = go.GetComponentsInChildren<Component>(true);
            foreach (var component in components)
            {
                if (component == null)
                    continue;

                AnalyzeSerializedObject(component);
            }
        }

        private void FindUsedAssetsInRendererMaterials(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                if (renderer.sharedMaterials == null)
                    continue;

                foreach (var material in renderer.sharedMaterials)
                {
                    if (material == null || material.shader == null)
                        continue;

                    int propertyCount = ShaderUtil.GetPropertyCount(material.shader);
                    for (int i = 0; i < propertyCount; i++)
                    {
                        if (ShaderUtil.GetPropertyType(material.shader, i) != ShaderUtil.ShaderPropertyType.TexEnv)
                            continue;

                        string texturePropertyName = ShaderUtil.GetPropertyName(material.shader, i);
                        Texture texture = material.GetTexture(texturePropertyName);
                        if (texture != null)
                        {
                            AnalyzeSerializedObject(texture);
                        }
                    }
                }
            }
        }

        private void FindUsedAssetsInScriptableObjects()
        {
            var scriptableObjects = AssetDatabase.FindAssets("t:ScriptableObject", new[] { "Assets" });
            foreach (var scriptableObjectGuid in scriptableObjects)
            {
                string scriptableObjectPath = AssetDatabase.GUIDToAssetPath(scriptableObjectGuid);
                var scriptableObject = AssetDatabase.LoadAssetAtPath<ScriptableObject>(scriptableObjectPath);
                if (scriptableObject != null)
                {
                    AnalyzeSerializedObject(scriptableObject);
                }
            }
        }

        private void FindUsedAssetsInIcons()
        {
            _usedAssets.UnionWith(PlayerSettings.GetIcons(UnityEditor.Build.NamedBuildTarget.iOS, IconKind.Any).Select(icon => AssetDatabase.GetAssetPath(icon)));

            var androidIcons = new List<Texture2D>();
            androidIcons.AddRange(PlayerSettings.GetIcons(UnityEditor.Build.NamedBuildTarget.Unknown, IconKind.Any));
            androidIcons.AddRange(PlayerSettings.GetPlatformIcons(UnityEditor.Build.NamedBuildTarget.Android, AndroidPlatformIconKind.Adaptive).SelectMany(i => i.GetTextures()));
            androidIcons.AddRange(PlayerSettings.GetPlatformIcons(UnityEditor.Build.NamedBuildTarget.Android, AndroidPlatformIconKind.Round).SelectMany(i => i.GetTextures()));
            androidIcons.AddRange(PlayerSettings.GetPlatformIcons(UnityEditor.Build.NamedBuildTarget.Android, AndroidPlatformIconKind.Legacy).SelectMany(i => i.GetTextures()));

            _usedAssets.UnionWith(androidIcons.Select(icon => AssetDatabase.GetAssetPath(icon)));
        }

        private void FindUsedAssetsInAnimations()
        {
            var animationPaths = AssetDatabase.FindAssets("t:AnimationClip", new[] { "Assets" })
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                .ToArray();

            foreach (var animationPath in animationPaths)
            {
                AnimationClip animationClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(animationPath);
                if (animationClip == null)
                {
                    continue;
                }
                string[] dependencies = AssetDatabase.GetDependencies(animationPath);
                foreach (var dependency in dependencies)
                {
                    if (!IsInResourcesFolder(dependency))
                    {
                        _usedAssets.Add(dependency);
                    }
                }
            }
        }

        private void FindUsedAssetsInMaterials()
        {
            var allMaterials = AssetDatabase.FindAssets("t:material", new[] { "Assets" });
            foreach (var materialGuid in allMaterials)
            {
                string materialPath = AssetDatabase.GUIDToAssetPath(materialGuid);
                if (_fontsPath.Contains(materialPath))
                {
                    continue;
                }
                var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (material != null)
                {
                    AnalyzeSerializedObject(material);
                }
            }
        }

        private void AnalyzeSerializedObject(Object obj)
        {
            var serializedObject = new SerializedObject(obj);
            var iterator = serializedObject.GetIterator();
            while (iterator.NextVisible(true))
            {
                if (iterator.propertyType == SerializedPropertyType.ObjectReference && iterator.objectReferenceValue != null)
                {
                    string assetPath = AssetDatabase.GetAssetPath(iterator.objectReferenceValue);
                    if (!string.IsNullOrEmpty(assetPath))
                    {
                        _usedAssets.Add(assetPath);
                    }
                }
            }
        }

        private void FindUnusedAssetsInFonts()
        {
            GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();

            var usedFonts = new List<TMP_FontAsset>();

            foreach (GameObject rootObject in rootObjects)
            {
                TMP_Text[] textElements = rootObject.GetComponentsInChildren<TMP_Text>(true);

                if (textElements.Length == 0)
                {
                    break;
                }

                foreach (TMP_Text textElement in textElements)
                {
                    if (!usedFonts.Contains(textElement.font))
                    {
                        usedFonts.Add(textElement.font);
                    }
                }
            }

            var usedFontsAssetPaths = new HashSet<string>();

            foreach (var font in usedFonts)
            {
                string assetPath = AssetDatabase.GetAssetPath(font);
                usedFontsAssetPaths.Add(assetPath);
            }

            _usedAssets.UnionWith(usedFontsAssetPaths);
        }

        private void FindUsedAssetsInPrefabInstances()
        {
            var prefabs = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" })
                .Select(guid => AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid)))
                .ToArray();

            foreach (var prefab in prefabs)
            {
                if (PrefabUtility.IsAnyPrefabInstanceRoot(prefab))
                {
                    GameObject root = PrefabUtility.GetOutermostPrefabInstanceRoot(prefab);
                    if (root != null)
                    {
                        AnalyzePrefabInstance(root);
                    }
                }
            }
        }

        private void AnalyzePrefabInstance(GameObject root)
        {
            var components = root.GetComponentsInChildren<Component>(true);
            foreach (var component in components)
            {
                if (component == null)
                    continue;

                AnalyzeSerializedObject(component);
            }
        }

        private bool IsInResourcesFolder(string assetPath)
        {
            if (_fontsPath.Contains(assetPath))
            {
                return false;
            }
            return assetPath.Split('/').Any(segment => segment == "Resources");
        }
    }
}
