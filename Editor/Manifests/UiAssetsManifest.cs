using System.Collections.Generic;
using Newtonsoft.Json;

namespace KK.UI.UMG.Editor.Manifests
{
    public sealed class UiAssetsManifest
    {
        [JsonProperty("assets")] public List<UiAssetSpec> Assets { get; set; } = new List<UiAssetSpec>();
    }

    public sealed class UiAssetSpec
    {
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("type")] public string Type { get; set; }
        [JsonProperty("source")] public string Source { get; set; }
        [JsonProperty("target")] public string Target { get; set; }
        [JsonProperty("contentHash")] public string ContentHash { get; set; }
    }
}
