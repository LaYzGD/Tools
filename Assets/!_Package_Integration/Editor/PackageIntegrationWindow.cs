using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

public enum PackageIntegrationState
{
    Starting,
    Integration
}

public class PackageIntegrationWindow : EditorWindow
{
    private PackageIntegrationList _packageList;

    private static readonly float _width = 800f;
    private static readonly float _height = 400f;

    private int _packagesAmount;
    private BuildTarget _buildTarget;
    private List<string> _allPackages;
    private List<string> _packagesToIntegrate;
    private PackageIntegrationState _state;

    private int _minPackagesAmount = 10;
    private int _maxPackagesAmount;

    private const string _prepareButtonString = "Prepare Packages";
    private const string _refreshButtonString = "Refresh";

    private string _buttonString;

    private Vector2 _verticalScrollPosition = Vector2.zero;

    [MenuItem("Window/Package Integration Tool")]
    public static void OnShowWindow()
    {
        PackageIntegrationWindow packageWindow = GetWindow<PackageIntegrationWindow>();
        packageWindow.titleContent = new GUIContent("Package Integration Tool");
        packageWindow.minSize = packageWindow.maxSize = new Vector2(_width, _height);
        packageWindow.Show();
    }

    private void OnEnable()
    {
        _buildTarget = EditorUserBuildSettings.activeBuildTarget;
        _state = PackageIntegrationState.Starting;
        _allPackages = new List<string>();
        _packagesToIntegrate = new List<string>();
        _buttonString = _prepareButtonString;
        _packageList = Resources.Load<PackageIntegrationList>("PackagesList");
    }

    private void OnGUI()
    {
        _packageList = (PackageIntegrationList)EditorGUILayout.ObjectField(_packageList, typeof(PackageIntegrationList), true);

        if (_packageList != null)
        {
            ShowMainGUI();

            switch (_state)
            {
                case PackageIntegrationState.Starting:
                    break;
                case PackageIntegrationState.Integration:
                    PreparePackages();
                    break;
            }
            
        }
    }

    private void ShowMainGUI()
    {
        EditorGUILayout.BeginVertical();

        var maxAmount = _buildTarget == BuildTarget.Android ? 
            _packageList.AndroidPackages.Count + _packageList.Packages.Count : 
            _packageList.IosPackages.Count + _packageList.Packages.Count;
        _maxPackagesAmount = EditorGUILayout.IntSlider("Max Packages Amount", _maxPackagesAmount, _minPackagesAmount, maxAmount);

        EditorGUILayout.EndVertical();

        if (GUILayout.Button(_buttonString))
        {
            _packagesAmount = Random.Range(_minPackagesAmount, _maxPackagesAmount + 1);
            SetPackages();
            _state = PackageIntegrationState.Integration;
            _buttonString = _refreshButtonString;
            Shuffle(_allPackages);
            _packagesToIntegrate = new List<string>();
            for (int i = 0; i < _packagesAmount; i++)
            {
                _packagesToIntegrate.Add(_allPackages[i]);
            }
        }
    }

    private void SetPackages()
    {
        _allPackages = new List<string>();

        foreach(var package in _packageList.Packages)
        {
            _allPackages.Add(package);
        }

        if (_buildTarget == BuildTarget.Android)
        {
            foreach (var package in _packageList.AndroidPackages)
            {
                _allPackages.Add(package);
            }
        }
        else
        {
            foreach (var package in _packageList.IosPackages)
            {
                _allPackages.Add(package);
            }
        }

    }

    private void PreparePackages()
    {
        GUILayout.BeginVertical("box");

        if (_packagesToIntegrate.Count() > 0)
        {
            _verticalScrollPosition = EditorGUILayout.BeginScrollView(_verticalScrollPosition, GUILayout.Height(_height / 2));
            foreach (var package in _packagesToIntegrate)
            {
                GUILayout.Label(package);
            }
            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("Integrate"))
            {
                IntegratePackages();
            }
        }
        GUILayout.EndVertical();
    }

    private void Shuffle<T>(List<T> list)
    {
        int elementsAmount = list.Count;
        for (int i = 0; i < elementsAmount; i++)
        {
            int randIndex = Random.Range(i, elementsAmount);
            T temp = list[i];
            list[i] = list[randIndex];
            list[randIndex] = temp;
        }
    }

    private void IntegratePackages()
    {
        foreach (var package in _packagesToIntegrate)
        {
            Client.Add(package);
        }
    }
}
