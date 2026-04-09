using CementGB.HeadlessFixes;
using MelonLoader;

[assembly: MelonInfo(typeof(Entrypoint), "CementGB.HeadlessFixes", "1.0.0", "CementGB Team")]
[assembly: MelonGame("Boneloaf", "Gang Beasts")]

namespace CementGB.HeadlessFixes;

public class Entrypoint : MelonPlugin
{
    private static bool IsBatchMode => Environment.GetCommandLineArgs().Contains("-batchmode") ||
                                       Environment.GetCommandLineArgs().Contains("-nographics");

    public override void OnPreSupportModule()
    {
        if (!IsBatchMode) return;

        FindMelon("UnityExplorer", "Sinai, yukieiji").Unregister();
        FindMelon("UnityExplorer", "Sinai")?.Unregister();
    }
}