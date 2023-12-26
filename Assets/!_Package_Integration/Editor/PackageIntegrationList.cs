using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
[CreateAssetMenu(menuName = "PackageIntegration/New Packages List", fileName = "PackagesList")]
public class PackageIntegrationList : ScriptableObject
{
    [SerializeField] private List<string> _packages;
    [SerializeField] private List<string> _androidPackages;
    [SerializeField] private List<string> _iosPackages;

    public List<string> Packages => _packages;
    public List<string> AndroidPackages => _androidPackages;
    public List<string> IosPackages => _iosPackages;
}
