using System.Collections.Generic;
using Newtonsoft.Json;

namespace KK.UI.UMG.Editor.Manifests
{
    public sealed class UiPackageManifest
    {
        [JsonProperty("schemaVersion")] public string SchemaVersion { get; set; }
        [JsonProperty("packageId")] public string PackageId { get; set; }
        [JsonProperty("namespace")] public string Namespace { get; set; }
        [JsonProperty("version")] public string Version { get; set; }
        [JsonProperty("designResolution")] public UiDesignResolution DesignResolution { get; set; }
        [JsonProperty("manifests")] public UiManifestRefs Manifests { get; set; }
        [JsonProperty("addressablesGroup")] public string AddressablesGroup { get; set; }
        [JsonProperty("sharedAssetRoots")] public List<string> SharedAssetRoots { get; set; }
        [JsonProperty("v1")] public UiV1Options V1 { get; set; }
    }

    public sealed class UiDesignResolution
    {
        [JsonProperty("width")] public int Width { get; set; }
        [JsonProperty("height")] public int Height { get; set; }
    }

    public sealed class UiManifestRefs
    {
        [JsonProperty("layout")] public string Layout { get; set; }
        [JsonProperty("assets")] public string Assets { get; set; }
        [JsonProperty("bindings")] public string Bindings { get; set; }
        [JsonProperty("codegen")] public string Codegen { get; set; }
        [JsonProperty("strings")] public string Strings { get; set; }
    }

    public sealed class UiV1Options
    {
        [JsonProperty("controls")] public List<string> Controls { get; set; }
    }
}
