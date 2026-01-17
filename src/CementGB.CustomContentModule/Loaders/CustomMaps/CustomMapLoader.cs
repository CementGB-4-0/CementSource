using System.Diagnostics;
using CementGB.Utilities;
using Il2CppGB.Data.Loading;
using UnityEngine;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace CementGB.Modules.CustomContent;

public class CustomMapLoader : CustomContentRefLoader
{
    public override string CustomContentTypeString => "Stages";

    public override CustomContentRefHolder[] Load()
    {
        var stopwatch = new Stopwatch();
        CustomContentModule.Logger?.Msg("Registering custom stage references. . .");
        stopwatch.Start();

        var dataLocs = CustomAddressableRegistration.GetAllModdedResourceLocationsOfType<SceneData>();
        var ret = dataLocs.Select(LoadSingle).ToList();

        stopwatch.Stop();
        CustomContentModule.Logger?.Msg(ConsoleColor.Green,
            $"Finished registration of {ret.Count} custom stages in {stopwatch.ElapsedMilliseconds}ms");
        return ret.ToArray();
    }

    private CustomContentRefHolder LoadSingle(IResourceLocation dataLoc)
    {
        var sceneName = dataLoc.PrimaryKey.Replace("-Data", "");
        var infoLoc = CustomAddressableRegistration.GetAllModdedResourceLocationsOfType<ScriptableObject>()
            .FirstOrDefault(loc => loc.PrimaryKey == $"{sceneName}-Info");
        var ret = new CustomMapRefHolder(dataLoc, infoLoc);
        ExtendedStringLoader.Register($"STAGE_{sceneName.ToUpper()}", sceneName);
        return ret;
    }
}