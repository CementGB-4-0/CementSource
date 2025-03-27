using System.Reflection;
using CementGB.Mod;
using MelonLoader;

[assembly: AssemblyCompany(MyPluginInfo.Author)]
[assembly: AssemblyProduct(MyPluginInfo.Name)]
[assembly: AssemblyCopyright("Created by " + MyPluginInfo.Author)]
[assembly: AssemblyTrademark(MyPluginInfo.Author)]
[assembly: AssemblyVersion(MyPluginInfo.Version)]
[assembly: AssemblyFileVersion(MyPluginInfo.Version)]

[assembly: MelonInfo(typeof(Mod), MyPluginInfo.Name, MyPluginInfo.Version, MyPluginInfo.Author, null)]
[assembly: MelonColor(0, 99, 198, 255)]
[assembly: MelonGame("Boneloaf", "Gang Beasts")]
[assembly: MelonPriority(-1000)]