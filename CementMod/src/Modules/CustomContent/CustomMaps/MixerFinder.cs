using System;
using UnityEngine;
using UnityEngine.Audio;

namespace CementGB.Mod.Modules.CustomContent.CustomMaps;

internal static class MixerFinder
{
    internal static AudioMixer? MainMixer { get; private set; }

    internal static void AssignMainMixer()
    {
        Mod.Logger.Msg("Assigning main level audio mixer. . .");
        var mixers = Resources.FindObjectsOfTypeAll<AudioMixer>();
        if (mixers == null || mixers.Length == 0)
        {
            Mod.Logger.Msg(ConsoleColor.Red, "No mixers were found. Maps will not mix right with game audio.");
            return;
        }

        foreach (var mixer in mixers)
        {
            if (mixer.name != "JukeboxMixer")
            {
                continue; // Main mixer is always called "JukeboxMixer" it seems
            }

            Mod.Logger.Msg(
                ConsoleColor.Green,
                "Main mixer found. Maps will now fallback onto this mixer if one isn't assigned.");
            MainMixer = mixer;
            return;
        }
    }
}