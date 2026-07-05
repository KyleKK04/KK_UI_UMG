using System.Collections.Generic;
using Newtonsoft.Json;

namespace KK.UI.UMG.Editor.Manifests
{
    public sealed class UiBindingsManifest
    {
        [JsonProperty("mvvm")] public UiMvvmSection Mvvm { get; set; }
        [JsonProperty("bindings")] public List<UiBindingSpec> Bindings { get; set; } = new List<UiBindingSpec>();
        [JsonProperty("events")] public List<UiEventSpec> Events { get; set; } = new List<UiEventSpec>();
    }

    public sealed class UiMvvmSection
    {
        [JsonProperty("fields")] public List<UiViewModelFieldSpec> Fields { get; set; } = new List<UiViewModelFieldSpec>();
    }

    public sealed class UiViewModelFieldSpec
    {
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("type")] public string Type { get; set; }
        [JsonProperty("default")] public object Default { get; set; }
    }

    public sealed class UiBindingSpec
    {
        [JsonProperty("controlId")] public string ControlId { get; set; }
        [JsonProperty("fieldId")] public string FieldId { get; set; }
        [JsonProperty("mode")] public string Mode { get; set; }
        [JsonProperty("property")] public string Property { get; set; }
    }

    public sealed class UiEventSpec
    {
        [JsonProperty("controlId")] public string ControlId { get; set; }
        [JsonProperty("event")] public string Event { get; set; }
        [JsonProperty("handler")] public string Handler { get; set; }
        [JsonProperty("updates")] public List<UiEventUpdateSpec> Updates { get; set; } = new List<UiEventUpdateSpec>();
        [JsonProperty("channel")] public string Channel { get; set; }
        [JsonProperty("channelPayloadFields")] public List<string> ChannelPayloadFields { get; set; } = new List<string>();
    }

    public sealed class UiEventUpdateSpec
    {
        [JsonProperty("fieldId")] public string FieldId { get; set; }
        [JsonProperty("value")] public object Value { get; set; }
    }
}
