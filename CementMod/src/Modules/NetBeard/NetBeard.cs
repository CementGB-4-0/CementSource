using MelonLoader;
using UnityEngine;

namespace CementGB.Mod.Modules.NetBeard;

[RegisterTypeInIl2Cpp]
internal class NetBeard : MonoBehaviour
{
    public void Start()
    {
        LobbyCommunicator.Awake();
    }
}