using CementGB.CustomContent;
using GBMDK;
using Il2CppCS.CorePlatform;
using Il2CppInterop.Runtime.Injection;

namespace CementGB.Modules.CustomContent;

public class CustomContentModule : InstancedCementModule
{
    protected override void OnInitialize()
    {
        ClassInjector.RegisterTypeInIl2Cpp<CustomMapInfo>();
        
        PlatformEvents.add_OnPlatformInitializedEvent((PlatformEvents.PlatformVoidEventDel)CustomAddressableRegistration.Initialize);
    }
}