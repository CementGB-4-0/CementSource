using MelonLoader;
using System;
using System.Runtime.CompilerServices;

namespace CementGB.Mod.Utilities;

public static class LoggingUtilities
{
    public static MelonLogger.Instance Logger => Melon<Mod>.Logger; // For if you're tired of the singleton pattern I guess

    public static void VerboseLog(ConsoleColor color, string message, [CallerMemberName] string callerName = null, [CallerLineNumber] int lineNumber = 0)
    {
        if (!CementPreferences.VerboseMode) return;
        Logger.Msg(color, callerName == null ? $"{message}" : $"[{callerName.ToUpper()}] {message} : Ln {lineNumber}");
    }

    public static void VerboseLog(string message, [CallerMemberName] string callerName = null, [CallerLineNumber] int lineNumber = 0)
    {
        if (!CementPreferences.VerboseMode) return;
        Logger.Msg(ConsoleColor.DarkGray, callerName == null ? $"{message}" : $"[{callerName.ToUpper()}] {message} : Ln {lineNumber}");
    }
}