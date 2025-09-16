using CementGB.Mod.Modules.CustomContent.Utilities;
using Il2CppGB.Gamemodes;
using Il2CppInterop.Runtime.InteropTypes.Fields;
using MelonLoader;
using UnityEngine;

namespace GBMDK;

[RegisterTypeInIl2Cpp]
public class CustomMapInfo : ScriptableObject
{
    public const GameModeEnum DefaultModes = GameModeEnum.Melee;

    public Il2CppValueField<GameModeEnum>? allowedGamemodes;

    public static CustomMapInfo CreateDefault(string mapName)
    {
        var newMapInfo = CreateInstance<CustomMapInfo>();
        newMapInfo.name = mapName + "-Info";

        newMapInfo.allowedGamemodes?.Set(DefaultModes);
        newMapInfo.MakePersistent();
        return newMapInfo;
    }
}