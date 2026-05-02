using CementGB.Modules.CustomContent.Utilities;
using Il2CppCostumes;
using Il2CppGB.Game.Data;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using Object = Il2CppSystem.Object;
using Random = UnityEngine.Random;

namespace GBMDK;

[Serializable]
public class WavesDataWrapper : Object
{
    public bool createWavesData;
    public int numWaves = 4;
    public int beastPerWaveLimit = 16;

    public string gameOverText = "GAME_WAVES_WAVE_LOST";
    public Color gameOverColor = Color.white;
    public Il2CppStringArray? costumePresets;

    public WavesData? Result => ConstructResult();
    // TODO: Add wave win and start text codes

    private WavesData? ConstructResult()
    {
        if (!createWavesData) return null;

        var res = ScriptableObject.CreateInstance<WavesData>();
        var costumeList = new Il2CppSystem.Collections.Generic.List<string>();
        var setCostumePresets = costumePresets;
        if (setCostumePresets == null || setCostumePresets.Length == 0)
        {
            foreach (var costu in BeastUtilities.GetAllCostumeStrings())
            {
                if (string.IsNullOrWhiteSpace(costu)) continue;
                costumeList.Add(costu);
            }
        }
        else
        {
            foreach (var costu in setCostumePresets)
            {
                costumeList.Add(costu);
            }
        }

        var beastTypePref = res.beastTypePref;
        beastTypePref.Add(BeastUtilities.RetrieveBeastPrefab(BeastTypeSelector.BIG));
        beastTypePref.Add(BeastUtilities.RetrieveBeastPrefab(BeastTypeSelector.MEDIUM));
        beastTypePref.Add(BeastUtilities.RetrieveBeastPrefab(BeastTypeSelector.TINY));

        res.fallbackCostumes = costumeList;
        res.fallbackColourIndex = new Il2CppSystem.Collections.Generic.List<int>();
        var playerColors = CostumePool.I.PlayerColorDatabase;

        for (var col = 1; col < 9; col++)
            res.fallbackColourIndex.Add(col);

        res.overCode = gameOverText;
        res.overColour = gameOverColor;

        var beastSetups = new Il2CppSystem.Collections.Generic.List<BeastSetup>();
        for (var waveNum = 0; waveNum < numWaves; waveNum++)
        {
            var output = new Wave();
            if (!(beastSetups.Count < beastPerWaveLimit)) continue;

            beastSetups.Add(new BeastSetup
            {
                type = Random.Range(0, 3),
                gangID = 10,
                colour = -1,
                costume = costumeList.Count > 1
                    ? costumeList[Random.Range(0, costumeList.Count - 1)]
                    : costumeList.ToArray().FirstOrDefault()
            });

            output.beasts =
                new Il2CppSystem.Collections.Generic.List<BeastSetup>(beastSetups
                    .Cast<Il2CppSystem.Collections.Generic.IEnumerable<BeastSetup>>());

            res.levelWaves.Add(output);
        }

        res.MakePersistent();
        return res;
    }
}