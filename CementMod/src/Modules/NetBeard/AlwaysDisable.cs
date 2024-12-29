using UnityEngine;

namespace CementGB.Mod.Modules.NetBeard;

[MelonLoader.RegisterTypeInIl2Cpp]
public class AlwaysDisable : MonoBehaviour
{
    void Update()
    {
        gameObject.SetActive(false); // Update only runs if the object is active so this wont tank performance
    }
}
