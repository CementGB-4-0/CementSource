using CementGB.Mod.CustomContent;
using Il2CppCS.CorePlatform;

namespace CementGB.Modules.CustomContent;

public class CustomContentModule : InstancedCementModule
{
    protected override void OnInitialize()
    {
        PlatformEvents.add_OnPlatformInitializedEvent((PlatformEvents.PlatformVoidEventDel)CustomAddressableRegistration.Initialize);
    }
}