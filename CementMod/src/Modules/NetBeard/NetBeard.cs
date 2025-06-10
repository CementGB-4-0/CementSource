using MelonLoader;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace CementGB.Mod.Modules.NetBeard;


[RegisterTypeInIl2Cpp]
internal class NetBeard : MonoBehaviour
{
    public void Start()
    {
        ServerChecker.Awake();
    }
}


