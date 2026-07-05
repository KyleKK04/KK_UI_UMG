using Newtonsoft.Json;
using System.Collections.Generic;

namespace KK.UI.UMG.Editor.Manifests
{
    public sealed class UiCodegenManifest
    {
        [JsonProperty("schemaVersion")] public string SchemaVersion { get; set; }
        [JsonProperty("namespace")] public string Namespace { get; set; }
        [JsonProperty("outputRoot")] public string OutputRoot { get; set; }
        [JsonProperty("addressablesKey")] public string AddressablesKey { get; set; }
        [JsonProperty("view")] public UiViewCodegen View { get; set; }
        [JsonProperty("controller")] public UiControllerCodegen Controller { get; set; }
        [JsonProperty("viewModel")] public UiViewModelCodegen ViewModel { get; set; }
        [JsonProperty("bus")] public UiBusCodegen Bus { get; set; }
        [JsonProperty("requiredServices")] public List<UiRequiredServiceSpec> RequiredServices { get; set; } =
            new List<UiRequiredServiceSpec>();
    }

    public sealed class UiViewCodegen
    {
        [JsonProperty("className")] public string ClassName { get; set; }
        [JsonProperty("baseClass")] public string BaseClass { get; set; }
    }

    public sealed class UiControllerCodegen
    {
        [JsonProperty("className")] public string ClassName { get; set; }
        [JsonProperty("baseClass")] public string BaseClass { get; set; }
    }

    public sealed class UiViewModelCodegen
    {
        [JsonProperty("className")] public string ClassName { get; set; }
    }

    public sealed class UiBusCodegen
    {
        [JsonProperty("routes")] public List<UiBusRouteSpec> Routes { get; set; } =
            new List<UiBusRouteSpec>();
    }

    public sealed class UiBusRouteSpec
    {
        [JsonProperty("channel")] public string Channel { get; set; }
        [JsonProperty("action")] public string Action { get; set; }
    }

    public sealed class UiRequiredServiceSpec
    {
        [JsonProperty("type")] public string Type { get; set; }
        [JsonProperty("property")] public string Property { get; set; }
    }
}
