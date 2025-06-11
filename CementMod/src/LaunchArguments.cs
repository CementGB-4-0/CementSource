using System;
using System.Linq;
using Il2Cpp;

namespace CementGB.Mod;

public static class LaunchArguments
{
    public static string
        MapArg => CommandLineParser.Instance.GetValueForKey("-map", false);

    public static string
        ModeArg => CommandLineParser.Instance.GetValueForKey("-mode", false);

    public static bool
        DebugArg => Environment.GetCommandLineArgs().Contains("-debug");

    /// <summary>
    ///     True if the -SERVER argument is passed to the Gang Beasts executable.
    /// </summary>
    public static bool IsServerArg => Environment.GetCommandLineArgs().Contains("-SERVER");

    public static readonly string
        IpArg = CommandLineParser.Instance.GetValueForKey("-ip", false); // set to server via vanilla code

    public static readonly string
        PortArg = CommandLineParser.Instance.GetValueForKey("-port", false); // set to server via vanilla code

    /// <summary>
    ///     True if the -SERVER argument is not passed, but the -FWD argument is. Forwards a local game to an ip and port.
    /// </summary>
    public static bool IsForwardedHostArg => !LaunchArguments.IsServerArg && Environment.GetCommandLineArgs().Contains("-FWD");

    /// <summary>
    ///     True if the -DONT-AUTOSTART argument is passed. Will prevent the server or client from automatically joining the
    ///     server as soon as it can.
    /// </summary>
    public static bool DontAutoStartArg => Environment.GetCommandLineArgs().Contains("-DONT-AUTOSTART");
}