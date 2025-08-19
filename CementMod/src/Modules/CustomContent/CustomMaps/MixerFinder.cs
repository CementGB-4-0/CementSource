using UnityEngine.Audio;

namespace CementGB.Mod.Modules.CustomContent.CustomMaps;

internal static class MixerFinder
{
    internal static AudioMixer? MainMixer { get; private set; }

    // Could probably inline this somewhere someday
    internal static void AssignMainMixer()
    {
        MainMixer = new AudioMixer();
    }
}