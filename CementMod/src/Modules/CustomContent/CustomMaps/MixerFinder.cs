using System;
using System.Linq;
using MelonLoader;
using UnityEngine;
using UnityEngine.Audio;

namespace CementGB.Mod.src.Modules.CustomContent.CustomMaps;

[RegisterTypeInIl2Cpp]
internal class MixerFinder : MonoBehaviour
{
    internal static AudioMixer mainMusicMixer;

    private void Awake()
    {
        var mixers = Resources.FindObjectsOfTypeAll<AudioMixer>().ToArray();

        foreach (var mixer in mixers)
        {
            if (mixer.name == "Mixer")
            {
                Mod.Logger.Msg(ConsoleColor.Green,
                    "Main mixer found. Maps will now fallback onto this mixer if one isn't assigned.");
                mainMusicMixer = mixer;
            }
        }

        if (mixers == null)
            Mod.Logger.Msg(ConsoleColor.Red, "Main mixer was not found. Maps will not mix right with game audio.");
    }
}