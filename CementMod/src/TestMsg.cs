using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Networking;
using MelonLoader;
using Il2CppInterop.Runtime.Injection;

namespace CementGB.Mod;

[RegisterTypeInIl2Cpp]
internal class TestMsg : MessageBase
{
    public TestMsg(IntPtr ptr) : base(ptr) { }
    public TestMsg() : base(ClassInjector.DerivedConstructorPointer<TestMsg>()) { ClassInjector.DerivedConstructorBody(this); }

    public override void Serialize(NetworkWriter writer)
    {
        writer.Write(127);
    }

    public override void Deserialize(NetworkReader reader)
    {
        Mod.Logger.Msg(ConsoleColor.Magenta, reader.ReadByte().ToString() + "!!");
    }
}
