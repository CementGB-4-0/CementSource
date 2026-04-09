using Il2Cpp;
using Tomlet;
using Tomlet.Attributes;

namespace CementGB.Modules.NetBeardModule;

public class NetBeardConfig
{
    private const string DefaultIP = "127.0.0.1";
    private const int DefaultPort = 5999;

    public static readonly NetBeardConfig
        Default = new();

    public static readonly string ConfigFilePath = Path.Combine(Mod.UserDataPath, "netbeard.toml");

    private static readonly string?
        IpArg = CommandLineParser.Instance.GetValueForKey(CliFlagConstants.IPArg, false);

    private static readonly string?
        PortArg = CommandLineParser.Instance.GetValueForKey(CliFlagConstants.PortArg,
            false);

    [TomlInlineComment(
        "Gives all clients the ability to spawn debug objects by enabling NetMemberContext.LocalHostedGame. Not recommended for public dedicated servers.")]
    public bool AllowDebugSpawning = false;

    public bool AutoJoin = true;
    public bool Dedicated = false;
    public bool Fwd = false;

    public string IP = DefaultIP;
    public int Port = DefaultPort;

    public string ServerName = "Unnamed NetBeard Server";

    [TomlInlineComment("Case-insensitive. Can be any map selection name, including 'Modded' or 'Random'.")]
    public string StageName = "Random";

    public bool UpnpEnabled = false;
    public static NetBeardConfig Current { get; private set; } = Default;

    public static void DeserializeCurrent()
    {
        if (!File.Exists(ConfigFilePath))
            File.WriteAllText(ConfigFilePath, TomletMain.TomlStringFrom(Default));

        Current = TomletMain.To<NetBeardConfig>(File.ReadAllText(ConfigFilePath));
        Current.ProcessConfigArgOverrides();
    }

    private void ProcessConfigArgOverrides()
    {
        if (Environment.GetCommandLineArgs().Contains(CliFlagConstants.ServerArg)) Dedicated = true;
        if (Environment.GetCommandLineArgs().Contains(CliFlagConstants.FwdArg)) Fwd = true;
        if (Environment.GetCommandLineArgs().Contains(CliFlagConstants.UpnpArg)) UpnpEnabled = true;
        if (!string.IsNullOrWhiteSpace(IpArg)) IP = IpArg;
        if (!string.IsNullOrWhiteSpace(PortArg)) Port = int.Parse(PortArg);
    }
}