using MelonLoader;
using UnityEngine;

namespace CementGB.Mod.Modules.NetBeard.Utilities;

/// <summary>
///     Ensures the object this script is attached to will always be inactive.
/// </summary>
[RegisterTypeInIl2Cpp]
public class AlwaysDisable : MonoBehaviour
{
    private void Update()
    {
        gameObject.SetActive(false); // Update only runs if the object is active so this wont tank performance
    }
}