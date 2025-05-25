using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace CementGB.Mod.Utilities;

public static class LoggingUtilities
{
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    internal static extern IntPtr MessageBox(int hWnd, string text, string caption, uint type);
    
    public static void VerboseLog(ConsoleColor color, string message, [CallerMemberName] string callerName = null,
        [CallerLineNumber] int lineNumber = 0)
    {
        if (!CementPreferences.VerboseMode)
        {
            return;
        }

        var fullCallerName = callerName;
        foreach (var method in new StackTrace().GetFrames())
        {
            if (method.GetMethod().Name == callerName)
            {
                fullCallerName = $"{method.GetMethod().ReflectedType.Name}.{callerName}";
            }
        }

        Mod.Logger.Msg(color, callerName == null ? $"{message}" : $"[{fullCallerName}] {message} | Ln {lineNumber}");
    }

    public static void VerboseLog(string message, [CallerMemberName] string callerName = null,
        [CallerLineNumber] int lineNumber = 0)
    {
        VerboseLog(ConsoleColor.DarkGray, message, callerName, lineNumber);
    }
}