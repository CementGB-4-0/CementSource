using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine.AddressableAssets.ResourceLocators;

namespace CementGB.Mod.Modules.AssetModding;
public class ContentPack
{
    public const string CustomContentFilename = "custom_content.json";

    private static readonly List<ContentPack> _registeredPacks = [];
   
    private readonly List<IResourceLocator> _loadedCatalogs = [];

    private string _ccFilePath;

    public static void RegisterPack(ContentPack pack)
    {
        _registeredPacks.Add(pack);
    }

    internal static void ReadAutoloads()
    {
        foreach (var dir in Directory.EnumerateDirectories(Mod.CustomContentPath))
        {
            var newPack = new ContentPack
            {
                _ccFilePath = Path.Combine(dir, CustomContentFilename)
            };

            if (!File.Exists(newPack._ccFilePath)) return;

            RegisterPack(newPack);
        }
    }

    private static bool IsValidContentPack(string dirPath)
    {
        throw new NotImplementedException();
    }
}
