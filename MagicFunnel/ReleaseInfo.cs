using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MagicFunnel;

public struct ReleaseInfo
{
    [JsonInclude]
    public string name;
    [JsonInclude]
    public AssetInfo[] assets;
}

public struct AssetInfo
{
    [JsonInclude]
    public string name;
    [JsonInclude]
    public string browser_download_url;
}