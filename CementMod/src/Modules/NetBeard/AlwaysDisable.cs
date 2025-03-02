using MelonLoader;
using UnityEngine;

namespace CementGB.Mod.Modules.NetBeard;

[RegisterTypeInIl2Cpp]
public class AlwaysDisable : MonoBehaviour
{
    private void Update()
    {
        gameObject.SetActive(false); // Update only runs if the object is active so this wont tank performance
    }
}