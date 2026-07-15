using System.Collections.Generic;
using System.Linq;
using System;
using KK.UI.UMG.Editor.Manifests;
using KK.UI.UMG.Editor.Pipeline;

namespace KK.UI.UMG.Editor.Validators
{
    public sealed class BindingValidator : IManifestValidator
    {
        private static readonly Dictionary<string, Dictionary<string, HashSet<string>>> SupportedProperties =
            new Dictionary<string, Dictionary<string, HashSet<string>>>
            {
                ["Panel"] = new Dictionary<string, HashSet<string>>
                {
                    ["color"] = new HashSet<string> { "Color", "string" },
                    ["alpha"] = new HashSet<string> { "float", "int" },
                    ["raycastTarget"] = new HashSet<string> { "bool" }
                },
                ["Text"] = new Dictionary<string, HashSet<string>>
                {
                    ["text"] = new HashSet<string> { "string", "int", "float", "bool" },
                    ["color"] = new HashSet<string> { "Color", "string" },
                    ["alpha"] = new HashSet<string> { "float", "int" },
                    ["fontSize"] = new HashSet<string> { "float", "int" }
                },
                ["Image"] = new Dictionary<string, HashSet<string>>
                {
                    ["sprite"] = new HashSet<string> { "Sprite" },
                    ["color"] = new HashSet<string> { "Color", "string" },
                    ["alpha"] = new HashSet<string> { "float", "int" },
                    ["fillAmount"] = new HashSet<string> { "float", "int" },
                    ["fillClockwise"] = new HashSet<string> { "bool" },
                    ["fillOrigin"] = new HashSet<string> { "int" },
                    ["preserveAspect"] = new HashSet<string> { "bool" },
                    ["raycastTarget"] = new HashSet<string> { "bool" }
                },
                ["RawImage"] = new Dictionary<string, HashSet<string>>
                {
                    ["texture"] = new HashSet<string> { "Texture" },
                    ["color"] = new HashSet<string> { "Color", "string" },
                    ["alpha"] = new HashSet<string> { "float", "int" },
                    ["raycastTarget"] = new HashSet<string> { "bool" }
                },
                ["Button"] = new Dictionary<string, HashSet<string>>
                {
                    ["interactable"] = new HashSet<string> { "bool" },
                    ["color"] = new HashSet<string> { "Color", "string" },
                    ["alpha"] = new HashSet<string> { "float", "int" },
                    ["raycastTarget"] = new HashSet<string> { "bool" }
                },
                ["Toggle"] = new Dictionary<string, HashSet<string>> { ["interactable"] = new HashSet<string> { "bool" }, ["isOn"] = new HashSet<string> { "bool" } },
                ["Slider"] = new Dictionary<string, HashSet<string>>
                {
                    ["interactable"] = new HashSet<string> { "bool" },
                    ["value"] = new HashSet<string> { "float", "int" },
                    ["minValue"] = new HashSet<string> { "float", "int" },
                    ["maxValue"] = new HashSet<string> { "float", "int" }
                },
                ["Scrollbar"] = new Dictionary<string, HashSet<string>>
                {
                    ["interactable"] = new HashSet<string> { "bool" },
                    ["value"] = new HashSet<string> { "float", "int" },
                    ["size"] = new HashSet<string> { "float", "int" }
                },
                ["InputField"] = new Dictionary<string, HashSet<string>> { ["interactable"] = new HashSet<string> { "bool" }, ["text"] = new HashSet<string> { "string" } },
                ["Dropdown"] = new Dictionary<string, HashSet<string>> { ["interactable"] = new HashSet<string> { "bool" }, ["value"] = new HashSet<string> { "int" } },
                ["VerticalList"] = new Dictionary<string, HashSet<string>> { ["items"] = new HashSet<string> { "IReadOnlyList<MessagePayload>" } }
            };

        private static readonly Dictionary<string, HashSet<string>> SupportedEvents =
            new Dictionary<string, HashSet<string>>
            {
                ["Button"] = new HashSet<string> { "onClick" },
                ["Toggle"] = new HashSet<string> { "onValueChanged" },
                ["Slider"] = new HashSet<string> { "onValueChanged" },
                ["InputField"] = new HashSet<string> { "onValueChanged", "onEndEdit" },
                ["Dropdown"] = new HashSet<string> { "onValueChanged" },
                ["VerticalList"] = new HashSet<string> { "onItemClick" }
            };

        public void Validate(KKUIPipelineContext context)
        {
            var nodesById = new Dictionary<string, UiLayoutNode>();
            var parentsById = new Dictionary<string, UiLayoutNode>();
            IndexLayout(context.Layout.Root, null, nodesById, parentsById);
            var fields = context.Bindings.Mvvm != null && context.Bindings.Mvvm.Fields != null
                ? context.Bindings.Mvvm.Fields
                : new List<UiViewModelFieldSpec>();
            var fieldsById = fields
                .Where(field => !string.IsNullOrWhiteSpace(field.Id))
                .GroupBy(field => field.Id)
                .ToDictionary(group => group.Key, group => group.First());

            foreach (var binding in context.Bindings.Bindings)
            {
                if (!nodesById.TryGetValue(binding.ControlId, out var node))
                {
                    context.Add(KKUIPipelineIssueSeverity.Error, "BND001", $"Binding control '{binding.ControlId}' does not exist.");
                    continue;
                }

                if (!fieldsById.TryGetValue(binding.FieldId, out var field))
                {
                    context.Add(KKUIPipelineIssueSeverity.Error, "BND002", $"Binding field '{binding.FieldId}' is not declared.");
                    continue;
                }

                if (binding.Mode != "OneWay")
                {
                    context.Add(KKUIPipelineIssueSeverity.Error, "BND003", $"Binding mode '{binding.Mode}' is not supported.");
                }

                ValidateProperty(context, node, binding, field, parentsById);
            }

            foreach (var evt in context.Bindings.Events)
            {
                if (!nodesById.TryGetValue(evt.ControlId, out var node))
                {
                    context.Add(KKUIPipelineIssueSeverity.Error, "BND004", $"Event control '{evt.ControlId}' does not exist.");
                    continue;
                }

                ValidateEvent(context, node, evt);

                if (!ValidatorUtility.IsCSharpIdentifier(evt.Handler))
                {
                    context.Add(KKUIPipelineIssueSeverity.Error, "BND006", $"Handler '{evt.Handler}' is not a valid C# method name.");
                }

                foreach (var update in evt.Updates ?? new List<UiEventUpdateSpec>())
                {
                    if (!fieldsById.ContainsKey(update.FieldId))
                    {
                        context.Add(KKUIPipelineIssueSeverity.Error, "BND007", $"Event '{evt.Handler}' updates undeclared field '{update.FieldId}'.");
                    }
                }

                if (node.Type != "Button" && (((evt.Updates?.Count ?? 0) > 0) || !string.IsNullOrWhiteSpace(evt.Channel)))
                {
                    context.Add(KKUIPipelineIssueSeverity.Warning, "BND010", $"Event '{evt.Handler}' is parameterized; manifest updates/channel are ignored in v0.5.2. Use a hand-written Controller partial.");
                }
            }

            ValidateVerticalListSpecs(context, context.Layout.Root, fieldsById);
        }

        private static void ValidateProperty(KKUIPipelineContext context, UiLayoutNode node, UiBindingSpec binding, UiViewModelFieldSpec field, Dictionary<string, UiLayoutNode> parentsById)
        {
            if (!SupportedProperties.TryGetValue(node.Type, out var properties) ||
                !properties.TryGetValue(binding.Property, out var allowedTypes))
            {
                context.Add(KKUIPipelineIssueSeverity.Error, "BND008", $"Property '{binding.Property}' is not supported for control '{binding.ControlId}' of type '{node.Type}'.");
                return;
            }

            if (node.Type == "Panel" &&
                (binding.Property == "color" || binding.Property == "alpha" || binding.Property == "raycastTarget") &&
                node.Image == null)
            {
                context.Add(KKUIPipelineIssueSeverity.Error, "BND008", $"Panel '{binding.ControlId}' property '{binding.Property}' requires an image spec so the generated prefab has an Image component.");
                return;
            }

            if (node.Type == "Text" &&
                binding.Property == "text" &&
                !string.IsNullOrWhiteSpace(node.Text?.LocKey))
            {
                if (HasAncestorOfType(node, parentsById, "Button"))
                {
                    context.Add(KKUIPipelineIssueSeverity.Warning, "TXT004", $"Static button text node '{node.Id}' uses locKey '{node.Text.LocKey}' and should not generate Store field '{binding.FieldId}'.");
                }

                context.Add(KKUIPipelineIssueSeverity.Error, "TXT003", $"Text node '{node.Id}' cannot define both locKey '{node.Text.LocKey}' and a dynamic text binding.");
            }

            if (!allowedTypes.Contains(field.Type))
            {
                context.Add(KKUIPipelineIssueSeverity.Error, "BND009", $"Binding field '{binding.FieldId}' type '{field.Type}' is not valid for {node.Type}.{binding.Property}.");
            }
        }

        private static void ValidateEvent(KKUIPipelineContext context, UiLayoutNode node, UiEventSpec evt)
        {
            if (!SupportedEvents.TryGetValue(node.Type, out var events) || !events.Contains(evt.Event))
            {
                context.Add(KKUIPipelineIssueSeverity.Error, "BND005", $"Event '{evt.Event}' is not supported for control '{evt.ControlId}' of type '{node.Type}'.");
            }
        }

        private static void IndexLayout(UiLayoutNode node, UiLayoutNode parent, Dictionary<string, UiLayoutNode> nodesById, Dictionary<string, UiLayoutNode> parentsById)
        {
            if (node == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(node.Id))
            {
                nodesById[node.Id] = node;
                if (parent != null)
                {
                    parentsById[node.Id] = parent;
                }
            }

            foreach (var child in node.Children ?? new List<UiLayoutNode>())
            {
                IndexLayout(child, node, nodesById, parentsById);
            }
        }

        private static bool HasAncestorOfType(UiLayoutNode node, Dictionary<string, UiLayoutNode> parentsById, string type)
        {
            var current = node;
            while (current != null && !string.IsNullOrWhiteSpace(current.Id) && parentsById.TryGetValue(current.Id, out var parent))
            {
                if (parent.Type == type)
                {
                    return true;
                }

                current = parent;
            }

            return false;
        }

        private static void ValidateVerticalListSpecs(KKUIPipelineContext context, UiLayoutNode root, Dictionary<string, UiViewModelFieldSpec> fieldsById)
        {
            ValidatorUtility.Walk(root, node =>
            {
                if (node.Type != "VerticalList")
                {
                    return;
                }

                var list = node.VerticalList;
                if (list == null)
                {
                    context.Add(KKUIPipelineIssueSeverity.Error, "BND011", $"VerticalList '{node.Id}' must define verticalList spec.");
                    return;
                }

                if (!fieldsById.TryGetValue(list.ItemSourceField, out var sourceField))
                {
                    context.Add(KKUIPipelineIssueSeverity.Error, "BND012", $"VerticalList '{node.Id}' itemSourceField '{list.ItemSourceField}' is not declared.");
                }
                else if (sourceField.Type != "IReadOnlyList<MessagePayload>")
                {
                    context.Add(KKUIPipelineIssueSeverity.Error, "BND013", $"VerticalList '{node.Id}' itemSourceField '{list.ItemSourceField}' must be IReadOnlyList<MessagePayload>.");
                }

                if (list.ItemTemplate == null)
                {
                    context.Add(KKUIPipelineIssueSeverity.Error, "BND014", $"VerticalList '{node.Id}' must define itemTemplate.");
                    return;
                }

                var templateIds = new HashSet<string>(StringComparer.Ordinal);
                ValidatorUtility.Walk(list.ItemTemplate, itemNode =>
                {
                    if (!string.IsNullOrWhiteSpace(itemNode.Id))
                    {
                        templateIds.Add(itemNode.Id);
                    }
                });

                foreach (var itemBinding in list.ItemBindings ?? new List<UiListItemBindingSpec>())
                {
                    if (!templateIds.Contains(itemBinding.ControlId))
                    {
                        context.Add(KKUIPipelineIssueSeverity.Error, "BND015", $"VerticalList '{node.Id}' item binding control '{itemBinding.ControlId}' does not exist in itemTemplate.");
                    }
                }

                foreach (var itemEvent in list.ItemEvents ?? new List<UiListItemEventSpec>())
                {
                    if (!templateIds.Contains(itemEvent.ControlId))
                    {
                        context.Add(KKUIPipelineIssueSeverity.Error, "BND016", $"VerticalList '{node.Id}' item event control '{itemEvent.ControlId}' does not exist in itemTemplate.");
                    }

                    if (itemEvent.Event != "onItemClick")
                    {
                        context.Add(KKUIPipelineIssueSeverity.Error, "BND017", $"VerticalList '{node.Id}' item event '{itemEvent.Event}' is not supported.");
                    }

                    if (!ValidatorUtility.IsCSharpIdentifier(itemEvent.Handler))
                    {
                        context.Add(KKUIPipelineIssueSeverity.Error, "BND018", $"VerticalList item handler '{itemEvent.Handler}' is not a valid C# method name.");
                    }
                }
            });
        }
    }
}
