using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using TMPro;
using System.Text.RegularExpressions;

namespace OptimisationTool
{
    public enum AlgorithmState
    {
        Starting,
        SetupFont,
        UnusedAssetsHandling,
        FindingDuplicates,
        DuplicatesHandling,
        OptimizationHandling
    }
    public sealed class OptimisationToolWindow : EditorWindow
    {
        #region Values
        private string _mainFolder = "Assets";
        private bool _formatGroupEnabled;
        private bool _pngFormatEnabled = true;
        private bool _jpegFormatEnabled = true;
        private bool _jpgFormatEnabled = true;
        private long _thresholdComputational;
        private float _threshold = 1;
        private static readonly float _minWidth = 800f;
        private static readonly float _minHeight = 450f;

        private static readonly float _maxWidth = 800f;
        private static readonly float _maxHeight = 450f;
        private string _titleMain = "OPTIMIZATION TOOLS";
        #endregion

        #region Additional Classes
        private FileSearch _fileSearch;
        private DuplicatesFinder _finder;
        private TextureOptimizer _textureOptimizer;
        private AssetFinder _assetFinder;
        #endregion

        #region Other Fields
        private Vector2 _verticalScrollPosition = Vector2.zero;

        private List<string> _filesToOptimize;
        private Dictionary<string, List<string>> _duplicateFilesGroups = new();
        private List<string> _unusedAssets;
        private TMP_FontAsset _selectedFont;
        private List<string> _selectedAssets = new List<string>();
        private AlgorithmState _currentState;
        private string[] _buttonLabels = { "Duplicates", "Optimization", "Font", "Unused Assets" };
        private AlgorithmState[] _buttonStates = {
            AlgorithmState.FindingDuplicates,
            AlgorithmState.OptimizationHandling,
            AlgorithmState.SetupFont,
            AlgorithmState.Starting
        };
        #endregion

        #region Base Methods

        [MenuItem("Window/Optimization Tool")]
        public static void ShowWindow()
        {
            OptimisationToolWindow window = GetWindow<OptimisationToolWindow>();
            window.titleContent = new GUIContent("Optimization Tool");
            window.minSize = new Vector2(_minWidth, _minHeight);
            window.maxSize = new Vector2(_maxWidth, _maxHeight);
            window.Show();
        }

        private void Awake()
        {
            _currentState = AlgorithmState.Starting;

            _fileSearch = new FileSearch(_pngFormatEnabled, _jpegFormatEnabled, _jpgFormatEnabled, _mainFolder);
            _finder = new DuplicatesFinder();
            _textureOptimizer = new TextureOptimizer(_thresholdComputational);
        }

        private void OnEnable()
        {
            OpenAllScenesInBuildSettings();
        }


        private void OnGUI()
        {
            ShowMainGUI();

            if (string.IsNullOrEmpty(_mainFolder))
            {
                return;
            }

            if (!_pngFormatEnabled && !_jpegFormatEnabled && !_jpgFormatEnabled)
            {
                return;
            }

            HandleStates();

        }
        #endregion

        #region GUI Methods
        private void ShowMainGUI()
        {
            Rect titleRect = GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.boldLabel);
            titleRect.x += (position.width - EditorStyles.boldLabel.CalcSize(new GUIContent(_titleMain)).x) / 2;
            EditorGUI.LabelField(titleRect, _titleMain, EditorStyles.boldLabel);
            ShowMainButtons();
        }

        private void ShowFontSetupGUI()
        {
            GUILayout.BeginHorizontal("box");
            GUILayout.Label("Select the default font:", EditorStyles.boldLabel);
            _selectedFont = EditorGUILayout.ObjectField("TMP Font", _selectedFont, typeof(TMP_FontAsset), false) as TMP_FontAsset;

            if (_selectedFont != null)
            {
                if (GUILayout.Button("Setup Font"))
                {
                    ApplyFont(_selectedFont);
                }
            }

            GUILayout.EndHorizontal();
        }

        private void ShowUnusedAssetsSearchGUI()
        {
            GUILayout.BeginHorizontal("box");

            if (GUILayout.Button("Find Unused Assets"))
            {
                if (!Directory.Exists(_mainFolder))
                {
                    EditorUtility.DisplayDialog("Error", $"Current folder '{_mainFolder}' could not be found", "Ok");
                    return;
                }

                _unusedAssets?.Clear();

                try
                {
                    HandleUnusedAssets();
                    _currentState = AlgorithmState.UnusedAssetsHandling;
                }
                catch(System.Exception e)
                {
                    Debug.Log("There are no unused assets in this project");
                } 
            }

            GUILayout.EndHorizontal();
        }

        private void ShowDuplicatesSearchGUI()
        {
            GUILayout.BeginHorizontal("box");

            if (GUILayout.Button("Find Duplicates"))
            {
                if (!Directory.Exists(_mainFolder))
                {
                    EditorUtility.DisplayDialog("Error", $"Current folder '{_mainFolder}' could not be found", "Ok");
                    return;
                }

                _duplicateFilesGroups?.Clear();

                _fileSearch = new FileSearch(_pngFormatEnabled, _jpegFormatEnabled, _jpgFormatEnabled, _mainFolder);

                HandleDuplicates();

                _currentState = AlgorithmState.DuplicatesHandling;
            }

            GUILayout.EndHorizontal();
        }

        private void ShowGraphicsCheckGUI()
        {
            ShowSettings();

            GUILayout.BeginHorizontal("box");

            if (GUILayout.Button("Check Graphics"))
            {
                if (!Directory.Exists(_mainFolder))
                {
                    EditorUtility.DisplayDialog("Error", $"Current folder '{_mainFolder}' could not be found", "Ok");
                    return;
                }

                _filesToOptimize?.Clear();

                _fileSearch = new FileSearch(_pngFormatEnabled, _jpegFormatEnabled, _jpgFormatEnabled, _mainFolder);
                _textureOptimizer = new TextureOptimizer(_thresholdComputational);

                HandleFilesToOptimize();
            }

            GUILayout.EndHorizontal();
        }

        private void ShowMainButtons()
        {
            int buttonsPerRow = 2;

            for (int i = 0; i < _buttonLabels.Length; i += buttonsPerRow)
            {
                GUILayout.BeginHorizontal(GUILayout.Height(_maxHeight / 10));

                float buttonWidth = EditorGUIUtility.currentViewWidth / buttonsPerRow;

                for (int j = i; j < Mathf.Min(i + buttonsPerRow, _buttonLabels.Length); j++)
                {
                    GUILayout.BeginVertical(GUILayout.Width(buttonWidth));

                    if (GUILayout.Button(_buttonLabels[j], GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true)))
                    {
                        _currentState = _buttonStates[j];
                    }

                    GUILayout.EndVertical();
                }

                GUILayout.EndHorizontal();
            }
        }

        private void ShowSettings()
        {
            _formatGroupEnabled = EditorGUILayout.BeginToggleGroup("Settings", _formatGroupEnabled);
            _pngFormatEnabled = EditorGUILayout.Toggle("PNG Format Toggle", _pngFormatEnabled);
            _jpegFormatEnabled = EditorGUILayout.Toggle("JPEG Format Toggle", _jpegFormatEnabled);
            _jpgFormatEnabled = EditorGUILayout.Toggle("JPG Format Toggle", _jpgFormatEnabled);
            _threshold = EditorGUILayout.Slider("Size Threshold in MB", _threshold, 0.5f, 1.5f);
            EditorGUILayout.EndToggleGroup();

            _thresholdComputational = (long)((_threshold + 0.7f) * 1024f * 1024f);
        }
        #endregion

        #region Navigation Methods
        private void ShowUnusedAssetsNavigationButtons()
        {
            try
            {
                GUILayout.BeginVertical("box");

                if (GUILayout.Button("Select All Unused Assets"))
                {
                    SelectAllUnusedAssets();
                }

                if (GUILayout.Button("Deselect All Unused Assets"))
                {
                    _selectedAssets.Clear();
                }

                if (GUILayout.Button("Delete Selected Assets"))
                {
                    DeleteSelectedUnusedAssets();
                    if (_unusedAssets.Count == 0)
                    {
                        _currentState = AlgorithmState.Starting;
                    }
                    GUILayout.EndVertical();
                    return;
                }

                GUILayout.Label("Unused Assets:");
                _verticalScrollPosition = EditorGUILayout.BeginScrollView(_verticalScrollPosition, GUILayout.Height(_maxHeight / 2));

                foreach (var file in _unusedAssets)
                {
                    if (!File.Exists(file))
                    {
                        HandleUnusedAssets();
                        continue;
                    }

                    GUILayout.BeginHorizontal();

                    bool isSelected = _selectedAssets.Contains(file);
                    bool newSelection = GUILayout.Toggle(isSelected, "", GUILayout.Width(20));

                    if (newSelection && !isSelected)
                    {
                        _selectedAssets.Add(file);
                    }
                    else if (!newSelection && isSelected)
                    {
                        _selectedAssets.Remove(file);
                    }

                    if (GUILayout.Button(Path.GetFileName(file)))
                    {
                        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(file).GetInstanceID());
                    }

                    GUILayout.EndHorizontal();
                }

                EditorGUILayout.EndScrollView();
                GUILayout.EndVertical();
            }
            catch (System.Exception e)
            {
                Debug.Log("There are no unused assets in this project");
                GUILayout.Label("There are no unused assets in this project");
            }
            
        }


        private void ShowDuplicatesNavigationButtons()
        {
            try 
            {
                GUILayout.BeginVertical("box");

                GUILayout.Label("Duplicate Files:");
                _verticalScrollPosition = EditorGUILayout.BeginScrollView(_verticalScrollPosition, GUILayout.Height(_maxHeight / 2));
                foreach (var duplicateGroup in _duplicateFilesGroups)
                {
                    GUILayout.Label(duplicateGroup.Key);
                    foreach (var file in duplicateGroup.Value)
                    {
                        if (!File.Exists(file))
                        {
                            HandleDuplicates();
                            continue;
                        }

                        if (GUILayout.Button(Path.GetFileName(file)))
                        {
                            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(file).GetInstanceID());
                        }
                    }
                }
                EditorGUILayout.EndScrollView();
                GUILayout.EndVertical();
            }
            catch (System.Exception e)
            {
                Debug.Log("There are no duplicates in this project");
                GUILayout.Label("There are no duplicates in this project");
            }
            
        }

        private void ShowGraphicsNavigationButtons()
        {
            if (_filesToOptimize != null && _filesToOptimize.Count > 0)
            {
                GUILayout.BeginVertical("box");
                GUILayout.Label("Files to Optimize:");
                _verticalScrollPosition = EditorGUILayout.BeginScrollView(_verticalScrollPosition, GUILayout.Height(_maxHeight / 2));
                foreach (var file in _filesToOptimize)
                {
                    if (!File.Exists(file))
                    {
                        continue;
                    }

                    if (GUILayout.Button(Path.GetFileName(file)))
                    {
                        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(file).GetInstanceID());
                    }
                }
                EditorGUILayout.EndScrollView();
                GUILayout.EndVertical();
                return;
            }
        }
        #endregion

        #region Additional Methods
        private void HandleDuplicates()
        {
            var files = _fileSearch.Search();
            _duplicateFilesGroups = _finder.FindDuplicates(files);
        }

        private void HandleFilesToOptimize()
        {
            var files = _fileSearch.Search();
            _filesToOptimize = _textureOptimizer.GetNotOptimizedFiles(files);
        }

        private void HandleUnusedAssets()
        {
            _assetFinder = new AssetFinder();
            _unusedAssets = _assetFinder.FindUnusedAssets();
        }

        private void HandleStates()
        {
            switch (_currentState)
            {
                case AlgorithmState.Starting:
                    ShowUnusedAssetsSearchGUI();
                    break;
                case AlgorithmState.SetupFont:
                    ShowFontSetupGUI(); 
                    break;
                case AlgorithmState.UnusedAssetsHandling:
                    ShowUnusedAssetsNavigationButtons();
                    break;
                case AlgorithmState.FindingDuplicates:
                    ShowDuplicatesSearchGUI();
                    break;
                case AlgorithmState.DuplicatesHandling:
                    ShowDuplicatesNavigationButtons();
                    break;
                case AlgorithmState.OptimizationHandling:
                    ShowGraphicsCheckGUI();
                    ShowGraphicsNavigationButtons();
                    break;
            }
        }

        private void ApplyFont(TMP_FontAsset font)
        {
            TMP_Text[] textComponents = FindObjectsOfType<TMP_Text>();

            if (textComponents == null || textComponents.Length == 0)
            {
                return;
            }

            foreach (TMP_Text textComponent in textComponents)
            {
                Undo.RecordObject(textComponent, "Change TMP Font");
                textComponent.font = font;
                EditorUtility.SetDirty(textComponent);
            }

            ChangeTMPSettingsFile(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(font)));

            EditorSceneManager.MarkAllScenesDirty();

            Debug.Log("Default TMP font updated to: " + font.name);
        }

        private void ChangeTMPSettingsFile(string fontAssetGuid)
        {
            TMP_Settings settings = Resources.Load<TMP_Settings>("TMP Settings");
            
            if (settings == null)
            {
                return;
            }

            var filePath = AssetDatabase.GetAssetPath(settings);

            string fileContent = File.ReadAllText(filePath);
            Debug.Log("FILE CONTENT: " + fileContent);
            string pattern = @"(m_defaultFontAsset: \{fileID: \d+, guid: )\w+(, type: \d+\})";

            string replacedText = Regex.Replace(fileContent, pattern, m =>
            {
                return m.Groups[1].Value + fontAssetGuid + m.Groups[2].Value;
            });
            Debug.Log("FILE CONTENT: " + replacedText);
            File.WriteAllText(filePath, replacedText);
            AssetDatabase.Refresh();
        }

        private void OpenAllScenesInBuildSettings()
        {
            int sceneCount = SceneManager.sceneCountInBuildSettings;

            for (int i = 0; i < SceneManager.loadedSceneCount; i++)
            {
                Scene currentScene = EditorSceneManager.GetSceneAt(i);
                EditorSceneManager.CloseScene(currentScene, true);
            }

            for (int i = 0; i < sceneCount; i++)
            {
                string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            }
        }

        private void SelectAllUnusedAssets()
        {   
            _selectedAssets.Clear();
            foreach (var file in _unusedAssets)
            {
                if (!File.Exists(file))
                {
                    HandleUnusedAssets();
                    continue;
                }
                _selectedAssets.Add(file);
            }
        }

        private void DeleteSelectedUnusedAssets()
        {
            foreach (var file in _selectedAssets)
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                    AssetDatabase.Refresh();
                }
            }
            _unusedAssets.RemoveAll(asset => _selectedAssets.Contains(asset));
            _selectedAssets.Clear();
        }
        #endregion
    }
}
