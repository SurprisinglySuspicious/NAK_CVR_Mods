﻿using MelonLoader;
using NAK.ASTExtension.Properties;
using System.Reflection;

[assembly: AssemblyVersion(AssemblyInfoParams.Version)]
[assembly: AssemblyFileVersion(AssemblyInfoParams.Version)]
[assembly: AssemblyInformationalVersion(AssemblyInfoParams.Version)]
[assembly: AssemblyTitle(nameof(NAK.ASTExtension))]
[assembly: AssemblyCompany(AssemblyInfoParams.Author)]
[assembly: AssemblyProduct(nameof(NAK.ASTExtension))]

[assembly: MelonInfo(
    typeof(NAK.ASTExtension.ASTExtensionMod),
    nameof(NAK.ASTExtension),
    AssemblyInfoParams.Version,
    AssemblyInfoParams.Author,
    downloadLink: "https://github.com/NotAKidoS/NAK_CVR_Mods/tree/main/ASTExtension"
)]

[assembly: MelonGame("Alpha Blend Interactive", "ChilloutVR")]
[assembly: MelonPlatform(MelonPlatformAttribute.CompatiblePlatforms.WINDOWS_X64)]
[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.MONO)]
[assembly: MelonColor(255, 246, 25, 99)] // red-pink
[assembly: MelonAuthorColor(255, 158, 21, 32)] // red
[assembly: HarmonyDontPatchAll]

namespace NAK.ASTExtension.Properties;
internal static class AssemblyInfoParams
{
    public const string Version = "1.0.0";
    public const string Author = "NotAKidoS";
}