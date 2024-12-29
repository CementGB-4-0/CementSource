using CementGB.Mod;
using MelonLoader;
using System.Reflection;
using BuildInfo = CementGB.Mod.BuildInfo;

[assembly: AssemblyTitle(BuildInfo.Description)]
[assembly: AssemblyDescription(BuildInfo.Description)]
[assembly: AssemblyCompany(BuildInfo.Company)]
[assembly: AssemblyProduct(BuildInfo.Name)]
[assembly: AssemblyCopyright("Created by " + BuildInfo.Author)]
[assembly: AssemblyTrademark(BuildInfo.Company)]
[assembly: AssemblyVersion(BuildInfo.Version)]
[assembly: AssemblyFileVersion(BuildInfo.Version)]

[assembly: MelonInfo(typeof(Mod), BuildInfo.Name, BuildInfo.Version, BuildInfo.Author, BuildInfo.DownloadLink)]
[assembly: MelonColor(0, 99, 198, 255)]
[assembly: MelonGame("Boneloaf", "Gang Beasts")]
[assembly: MelonPriority(-1000)]
