using System;
using UnityEngine;
using UnityEngine.Audio;

namespace CementGB.Mod.Modules.CustomContent.CustomMaps;

internal static class MixerFinder
{
    internal static AudioMixer MainMixer { get; private set; }

    internal static void AssignMainMixer()
    {
        Mod.Logger.Msg("Assigning main level audio mixer. . .");
        var mixers = Resources.FindObjectsOfTypeAll<AudioMixer>();
        if (mixers == null || mixers.Length == 0)
        {
            Mod.Logger.Msg(ConsoleColor.Red, "No mixers were found. Maps will not mix right with game audio.");
            return;
        }
        
        if (mixers.Length > 1)
            Mod.Logger.Warning("More than one mixer already exists! Found mixer may not be main... Deleting excess mixers after one is chosen as main.");

        foreach (var mixer in mixers)
        {
            if (mixer.name != "JukeboxMixer") continue; // Main mixer is always called "JukeboxMixer" it seems
            
            Mod.Logger.Msg(ConsoleColor.Green,
                "Main mixer found. Maps will now fallback onto this mixer if one isn't assigned.");
            MainMixer = mixer;
            return;
        }
    }
}