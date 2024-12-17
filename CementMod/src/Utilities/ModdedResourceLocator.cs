using CementGB.Mod.Patches;
using Il2CppInterop.Runtime.Injection;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace CementGB.Mod.Utilities
{
    /// <summary>
    /// Miracle utility by @Lionmeow on GitHub. THANK YOU!
    /// https://github.com/Lionmeow/AcceleratorThings/blob/main/AcceleratorThings/ModdedResourceLocator.cs
    /// </summary>
/*
    public class ModdedResourceLocator : UnityEngine.Object
    {
        public ModdedResourceLocator() : base(ClassInjector.DerivedConstructorPointer<ModdedResourceLocator>()) => ClassInjector.DerivedConstructorBody(this);

        public bool Locate(Il2CppSystem.Object key, Il2CppSystem.Type type, out Il2CppSystem.Collections.Generic.IList<IResourceLocation> locations)
        {
            if (!.ContainsKey(key.ToString()))
            {
                locations = new Il2CppSystem.Collections.Generic.List<IResourceLocation>().Cast<Il2CppSystem.Collections.Generic.IList<IResourceLocation>>();
                return false;
            }

            var resourceLocators = new Il2CppSystem.Collections.Generic.List<IResourceLocation>();
            resourceLocators.Add(new ResourceLocationBase(key.ToString(), key.ToString(), typeof(BundledAssetProvider).FullName, type).Cast<IResourceLocation>());
            locations = resourceLocators.Cast<Il2CppSystem.Collections.Generic.IList<IResourceLocation>>();

            return true;
        }
    }
    */
}