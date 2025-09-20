using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace CementGB.Mod.Utilities;

public static class LoggingUtilities
{
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr MessageBox(int hWnd, string text, string caption, uint type);

    /// <summary>
    ///     Logs a message to the console if Verbose Mode is enabled, appending all caller info and line number to the message.
    ///     Intended to provide a mode for developers to write otherwise "spammy" log lines intended for debugging.
    /// </summary>
    /// <param name="color">Color of the message in the console.</param>
    /// <param name="message"></param>
    /// <param name="callerName">
    ///     The caller method signature to use if the stack trace couldn't find a match for the calling
    ///     method. Optional.
    /// </param>
    /// <param name="lineNumber">The line number this method was called on. Optional.</param>
    public static void VerboseLog(
        ConsoleColor color,
        string message,
        [CallerMemberName] string? callerName = null,
        [CallerLineNumber] int lineNumber = 0)
    {
        if (!CementPreferences.VerboseMode)
        {
            return;
        }

        var fullCallerName = callerName;
        foreach (var method in new StackTrace().GetFrames())
        {
            var methodBase = method.GetMethod();
            if (methodBase == null)
            {
                continue;
            }

            if (methodBase.Name == callerName)
            {
                fullCallerName = $"{methodBase.ReflectedType?.Namespace}.{methodBase.ReflectedType?.Name}.{callerName}";
            }
        }

        Mod.Logger.Msg(color, callerName == null ? $"{message}" : $"[{fullCallerName}] {message} | Ln {lineNumber}");
    }

    /// <summary>
    ///     Logs a message to the console if Verbose Mode is enabled, appending all caller info and line number to the message.
    ///     Intended to provide a mode for developers to write otherwise "spammy" log lines intended for debugging.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="callerName">
    ///     The caller method signature to use if the stack trace couldn't find a match for the calling
    ///     method. Optional.
    /// </param>
    /// <param name="lineNumber">The line number this method was called on. Optional.</param>
    public static void VerboseLog(
        string message,
        [CallerMemberName] string? callerName = null,
        [CallerLineNumber] int lineNumber = 0)
    {
        VerboseLog(ConsoleColor.DarkGray, message, callerName, lineNumber);
    }
}