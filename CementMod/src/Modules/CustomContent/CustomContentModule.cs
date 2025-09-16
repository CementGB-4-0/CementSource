using CementGB.Mod.CustomContent;
using Il2CppCS.CorePlatform;

namespace CementGB.Mod.Modules.CustomContent;

public class CustomContentModule : InstancedCementModule
{
    protected override void OnInitialize()
    {
        PlatformEvents.add_OnPlatformInitializedEvent((PlatformEvents.PlatformVoidEventDel)CustomAddressableRegistration.Initialize);
    }
}