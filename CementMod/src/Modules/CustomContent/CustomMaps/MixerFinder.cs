using System;
using MelonLoader;
using UnityEngine;
using UnityEngine.Audio;
using Object = UnityEngine.Object;

namespace CementGB.Mod.Modules.CustomContent.CustomMaps;

internal static class MixerFinder
{
    static MixerFinder()
    {
        AssignMainMixer();
    }

    internal static AudioMixer MainMixer { get; private set; }

    private static void DeleteExcessMixers()
    {
        var mixers = Resources.FindObjectsOfTypeAll<AudioMixer>();
        if (!MainMixer || mixers.Length <= 1) return;

        foreach (var mixer in mixers)
        {
            if (mixer != MainMixer)
                Object.Destroy(mixer);
        }
    }

    private static void AssignMainMixer()
    {
        if (MainMixer) return;

        Mod.Logger.Msg("Assigning main level audio mixer. . .");
        var mixers = Resources.FindObjectsOfTypeAll<AudioMixer>();
        if (mixers == null || mixers.Length == 0)
        {
            Mod.Logger.Msg(ConsoleColor.Red, "No mixers were found. Maps will not mix right with game audio.");
            return;
        }

        if (mixers.Length > 1)
            Mod.Logger.Warning(
                "More than one mixer already exists! Found mixer may not be main... Deleting excess mixers after one is chosen as main.");

        foreach (var mixer in mixers)
        {
            if (mixer.name != "JukeboxMixer") continue; // Main mixer is always called "JukeboxMixer" it seems

            Mod.Logger.Msg(ConsoleColor.Green,
                "Main mixer found. Maps will now fallback onto this mixer if one isn't assigned.");
            MainMixer = mixer;
            MelonEvents.OnUpdate.Subscribe(DeleteExcessMixers);
            return;
        }
    }
}