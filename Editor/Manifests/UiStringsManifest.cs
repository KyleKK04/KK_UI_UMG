using System.Collections.Generic;
using Newtonsoft.Json;

namespace KK.UI.UMG.Editor.Manifests
{
    public sealed class UiStringsManifest
    {
        [JsonProperty("schemaVersion")] public string SchemaVersion { get; set; }
        [JsonProperty("defaultCulture")] public string DefaultCulture { get; set; }
        [JsonProperty("cultures")] public List<string> Cultures { get; set; } = new List<string>();
        [JsonProperty("strings")] public Dictionary<string, Dictionary<string, string>> Strings { get; set; } =
            new Dictionary<string, Dictionary<string, string>>();
    }
}
