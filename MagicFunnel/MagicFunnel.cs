using Il2Cpp;
using Il2CppNewtonsoft.Json;
using Il2CppSystem.Text;
using MelonLoader;
using MelonLoader.Utils;
using System.Net.Http.Json;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;

[assembly: MelonInfo(typeof(MagicFunnel.MagicFunnel), "MagicFunnel", "0.0.1", "Cement Team", "")]
namespace MagicFunnel;

public class MagicFunnel : MelonPlugin
{
    public static string MAGIC_FUNNEL_PATH => Path.Combine(MelonEnvironment.GameRootDirectory, "MagicFunnel");

    public override void OnPreModsLoaded()
    {
        if (!Directory.Exists(MAGIC_FUNNEL_PATH))
            Directory.CreateDirectory(MAGIC_FUNNEL_PATH);

        // update mods that need updating
        FetchLatestMods();

        // perform the magic funnel routine TM
        LetsGoOnTheMagicFunnelRide();
    }

    private void LetsGoOnTheMagicFunnelRide()
    {

    }

    private async void FetchLatestMods()
    {
        MelonInfoAttribute?[] modInfos = GetModInfos();

        HttpClient client = new();
        client.DefaultRequestHeaders.Add("User-Agent", "curl/7.64.1");
        Dictionary<string, ReleaseInfo[]?> cachedGitHubRespones = new(); // in case mods are from the same repo

        foreach (var modInfo in modInfos)
        {
            if (modInfo == null || modInfo.DownloadLink == null) continue;

            ReleaseInfo[]? releaseInfo;
            if (cachedGitHubRespones.ContainsKey(modInfo.DownloadLink))
            {
                releaseInfo = cachedGitHubRespones[modInfo.DownloadLink];
                // it's failed before trying to access the link
                if (releaseInfo == null)
                    continue;
            }
            else
            {
                /*
                if (!response.IsSuccessStatusCode)
                {
                    Melon<MagicFunnel>.Logger.Error($"HttpClient returned a non-OK status code for mod '{modInfo.Name}', " +
                        $"github link '{modInfo.DownloadLink}'");
                    Melon<MagicFunnel>.Logger.Error($"Response: {response.Content.ReadAsStringAsync().Result}");
                    cachedGitHubRespones[modInfo.DownloadLink] = null;
                    continue;
                }*/
                var responseStream = await client.GetStreamAsync(modInfo.DownloadLink).ConfigureAwait(false);
                MemoryStream stream = new();
                responseStream.CopyTo(stream);

                releaseInfo = JsonConvert.DeserializeObject<List<ReleaseInfo>>(Encoding.UTF8.GetString(stream.ToArray())).ToArray();
                cachedGitHubRespones[modInfo.DownloadLink] = releaseInfo;
            }

            SemVer? version = SemVer.TryParse(modInfo.Version);
            if (object.Equals(version, null))
            {
                Melon<MagicFunnel>.Logger.Error($"Mod '{modInfo.Name}' doesn't have a correctly formatted semantic version.");
                continue;
            }


            if (releaseInfo == null)
            {
                Melon<MagicFunnel>.Logger.Error($"Returned release for '{modInfo.Name}' couldn't be parsed.");
            }

            string updateLink = GetUpdateLink(modInfo.Name, version, releaseInfo!);
            if (updateLink != "")
            {
                var stream = client.GetStreamAsync(updateLink).Result;
                byte[] bytes = new byte[stream.Length];
                stream.Read(bytes, 0, (int)stream.Length);


                File.WriteAllBytes(Path.Combine(MAGIC_FUNNEL_PATH, GetSafeFileName(updateLink)), bytes);

                Melon<MagicFunnel>.Logger.Error($"Got updated file for '{modInfo.Name}'!");
            }
        }

        client.Dispose();
    }

    private string GetSafeFileName(string unsafeName)
    {
        foreach(char inv in Path.GetInvalidFileNameChars())
            unsafeName = unsafeName.Replace(inv, '_');

        return unsafeName;
    }

    // if returns blank if shouldn't update
    private string GetUpdateLink(string modName, SemVer version, ReleaseInfo[] releaseInfos)
    {
        foreach (ReleaseInfo releaseInfo in releaseInfos)
        {
            int zipCount = 0;
            foreach (AssetInfo assetInfo in releaseInfo.assets)
            {
                if (!assetInfo.name.EndsWith(".zip")) continue;
                ++zipCount;

                if (!assetInfo.name.Contains(modName)) continue;

                SemVer? latestVersion = SemVer.ParseBetwixt(assetInfo.name);
                if (latestVersion == null) continue;

                return version < latestVersion ? assetInfo.browser_download_url : "";
            }

            // fallback in case no asset was seen to have a name that matches
            if (zipCount == 1 && releaseInfo.name.Contains(modName))
            {
                SemVer? latestVersion = SemVer.ParseBetwixt(releaseInfo.name);
                if (latestVersion != null && version < latestVersion)
                {
                    return releaseInfo.assets[0].browser_download_url;
                }
            }
        }

        return "";
    }

    private string[] GetAllDLLs(string directory)
    {
        List<string> dlls = new();
        dlls.AddRange(Directory.GetFiles(directory, "*.dll"));
        foreach (string subdir in Directory.GetDirectories(directory))
            dlls.AddRange(GetAllDLLs(subdir));
        return dlls.ToArray();
    }

    private MelonInfoAttribute?[] GetModInfos()
    {
        var files = GetAllDLLs(MelonEnvironment.ModsDirectory);
        MelonInfoAttribute?[] melonInfos = new MelonInfoAttribute?[files.Length];

        AssemblyLoadContext loadContext = new("MagicFunnelExtractor", true);
        for (int i = 0; i < files.Length; ++i)
        {
            string file = files[i];
            MelonLogger.Msg("LOADING " + file);
            Assembly asm = loadContext.LoadFromAssemblyPath(file);
            var attr = GetMelonInfoAttribute(asm);
            melonInfos[i] = attr;
        }
        loadContext.Unload();

        return melonInfos;
    }

    public static MelonInfoAttribute? GetMelonInfoAttribute(Assembly asm)
    {
        return (MelonInfoAttribute?)Attribute.GetCustomAttribute(asm, typeof(MelonInfoAttribute));
    }

}