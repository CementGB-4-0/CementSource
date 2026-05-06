using CementGB.Utilities;
using Il2CppCostumes;
using UnityEngine;

namespace CementGB.Modules.CustomContent.Utilities;

public enum BeastTypeSelector
{
    MEDIUM = 1,
    BIG = 2,
    TINY = 3
}

public static class BeastUtilities
{
    public static IEnumerable<string> GetAllCostumeStrings()
    {
        return CostumePool.I.CostumePresetDatabase.CostumePresets.ToArray().Select(x => x.CostumeSaveEntry.Name);
    }

    public static void DumpCostumeStrings()
    {
        var enumer = GetAllCostumeStrings().GetEnumerator();
        while (enumer.MoveNext())
        {
            var item = enumer.Current;
            CustomContentModule.Logger?.VerboseLog($"Costume String: {item}");
        }

        enumer.Dispose();
    }

    public static GameObject? RetrieveBeastPrefab(BeastTypeSelector beastType)
    {
        return CostumePool.I.BeastResources.GetData<GameObject>((int)beastType);
    }
}