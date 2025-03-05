using Il2CppGB.Gamemodes;
using Il2CppInterop.Runtime.InteropTypes.Fields;
using MelonLoader;
using UnityEngine;

namespace GBMDK;

[RegisterTypeInIl2Cpp]
public class CustomMapInfo : ScriptableObject
{
    public Il2CppValueField<GameModeEnum> allowedGamemodes;
}