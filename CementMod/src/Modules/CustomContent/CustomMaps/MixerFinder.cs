using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using MelonLoader;
using UnityEngine.Audio;

namespace CementGB.Mod.src.Modules.CustomContent.CustomMaps;
[RegisterTypeInIl2Cpp]
internal class MixerFinder : MonoBehaviour
{
    internal static AudioMixer mainMusicMixer;

    private void Awake()
    {
        AudioMixer[] mixers = Resources.FindObjectsOfTypeAll<AudioMixer>().ToArray();
        
        foreach (AudioMixer mixer in mixers)
        {
            if (mixer.name == "Mixer")
            {
                Mod.Logger.Msg(ConsoleColor.Green, "Main mixer found. Maps will now fallback onto this mixer if one isn't assigned.");
                mainMusicMixer = mixer;
            }
        }

        if (mixers == null) Mod.Logger.Msg(ConsoleColor.Red, "Main mixer was not found. Maps will not mix right with game audio.");
    }
}
