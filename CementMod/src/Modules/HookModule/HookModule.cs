using HarmonyLib;
using MelonLoader;
using System;
using System.Reflection;

namespace CementGB.Mod.Modules;

/// <summary>
/// This is a really simple hooking library that uses Harmony, basically modular UltiLib.
/// It should be used to create custom functionality before or after vanilla methods.
/// </summary>
public static class HookModule
{
    /// <summary>
    /// A struct containing information required to create toggleable Harmony "hooks", or patches, with Cement.
    /// </summary>
    public struct CementHook
    {
        public MethodInfo original;
        public MethodInfo hook;
        public MelonMod callingMod;
        public bool isPrefix;

        /// <summary>
        /// Creates CementHook parameter info to later pass to <see cref="CreateHook(CementHook, bool)"/>.
        /// </summary>
        /// <param name="original">Original (typically base-game) method to patch.</param>
        /// <param name="callingMod">The mod making this patch. Used to toggle the patch on/off with the mod.</param>
        /// <param name="hook">The method containing the code to insert before or after the <paramref name="original"/> method.</param>
        /// <param name="isPrefix">Decides when the code will run: <c>true</c> if you want the code to run before the original, <c>false</c> if you want it to run after. Typically to guarantee other mod compatibility, you want to prefer running your code after the base game.</param>
        public CementHook(MethodInfo original, MethodInfo hook, bool isPrefix, MelonMod callingMod = null) : this()
        {
            this.original = original;
            this.callingMod = callingMod;
            this.hook = hook;
            this.isPrefix = isPrefix;
        }
    }

    /// <summary>
    /// Create a hook (before or after depending on CementHook's <c>isPrefix</c> boolean) on a method that will toggle on and off with the passed MelonMod (WIP).
    /// </summary>
    /// <param name="hook">The <see cref="CementHook"/> info to patch with.</param>
    /// <param name="canToggle">Whether the hook can be toggled on/off or not. Currently not functional.</param>
    public static void CreateHook(CementHook hook, bool canToggle = true)
    {
        Func<bool> doBeforeHook = () =>
        {
            // TODO: if callingMod is disabled or null, disable hook as well
            return true;
        };

        var prefix = hook.isPrefix ? new HarmonyMethod(hook.hook) : null;
        var postfix = hook.isPrefix ? null : new HarmonyMethod(hook.hook);

        HarmonyMethod beforeEitherFix = new(doBeforeHook.Method);

        var harmonyInstance = hook.callingMod is not null ? hook.callingMod.HarmonyInstance : Melon<Mod>.Instance.HarmonyInstance;
        harmonyInstance.Patch(hook.original, prefix, postfix);

        if (canToggle)
            harmonyInstance.Patch(hook.hook, beforeEitherFix);

        var resultString = $"New {(hook.isPrefix ? "PREFIX" : "POSTFIX")} hook on {hook.original.DeclaringType?.Name}.{hook.original.Name} registered to {hook.hook.DeclaringType?.Name}.{hook.hook.Name} with {typeof(HarmonyLib.Harmony)} instance {harmonyInstance.Id}";
        var fromModString = $"{resultString} from mod assembly {hook.callingMod.MelonAssembly.Assembly.FullName}";

        Melon<Mod>.Logger.Msg(hook.callingMod is null ? resultString : fromModString);
    }
}