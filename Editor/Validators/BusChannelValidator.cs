using System;
using System.Collections.Generic;
using System.Linq;
using KK.UI.UMG.Editor.Manifests;
using KK.UI.UMG.Editor.Pipeline;

namespace KK.UI.UMG.Editor.Validators
{
    public sealed class BusChannelValidator : IManifestValidator
    {
        public void Validate(KKUIPipelineContext context)
        {
            if (context.Package.PackageId != null && context.Package.PackageId.Contains("."))
            {
                context.Add(KKUIPipelineIssueSeverity.Error, "BUS009", "packageId must not contain '.' because it is used as one channel segment.");
            }

            var fieldIds = new HashSet<string>((context.Bindings.Mvvm?.Fields ?? new List<UiViewModelFieldSpec>())
                .Select(field => field.Id));
            var inboundChannels = new HashSet<string>();
            var constantNames = new HashSet<string>(StringComparer.Ordinal);

            foreach (var route in context.Codegen.Bus?.Routes ?? new List<UiBusRouteSpec>())
            {
                if (!BusChannelUtility.IsValidRelativeChannel(route.Channel, "in"))
                {
                    context.Add(KKUIPipelineIssueSeverity.Error, "BUS001", $"Inbound route channel '{route.Channel}' must match 'in.<event>' and use lowercase event names.");
                    continue;
                }

                if (route.Action != "Open" && route.Action != "Close")
                {
                    context.Add(KKUIPipelineIssueSeverity.Error, "BUS002", $"Inbound route action '{route.Action}' must be 'Open' or 'Close'.");
                }

                var fullChannel = BusChannelUtility.BuildFullChannel(context.Package.PackageId, route.Channel);
                if (!BusChannelUtility.IsValidFullChannel(context.Package.PackageId, fullChannel, "in"))
                {
                    context.Add(KKUIPipelineIssueSeverity.Error, "BUS003", $"Inbound route channel '{fullChannel}' must match 'ui.<packageId>.in.<event>'.");
                }

                if (!inboundChannels.Add(fullChannel))
                {
                    context.Add(KKUIPipelineIssueSeverity.Error, "BUS004", $"Inbound route channel '{fullChannel}' is registered more than once.");
                }

                ValidateConstantName(context, constantNames, route.Channel);
            }

            foreach (var evt in context.Bindings.Events)
            {
                if (string.IsNullOrWhiteSpace(evt.Channel))
                {
                    continue;
                }

                if (!BusChannelUtility.IsValidRelativeChannel(evt.Channel, "out"))
                {
                    context.Add(KKUIPipelineIssueSeverity.Error, "BUS005", $"Outbound event channel '{evt.Channel}' must match 'out.<event>' and use lowercase event names.");
                    continue;
                }

                var fullChannel = BusChannelUtility.BuildFullChannel(context.Package.PackageId, evt.Channel);
                if (!BusChannelUtility.IsValidFullChannel(context.Package.PackageId, fullChannel, "out"))
                {
                    context.Add(KKUIPipelineIssueSeverity.Error, "BUS006", $"Outbound event channel '{fullChannel}' must match 'ui.<packageId>.out.<event>'.");
                }

                foreach (var fieldId in evt.ChannelPayloadFields ?? new List<string>())
                {
                    if (!fieldIds.Contains(fieldId))
                    {
                        context.Add(KKUIPipelineIssueSeverity.Error, "BUS007", $"Event '{evt.Handler}' channel payload field '{fieldId}' is not declared in mvvm.fields.");
                    }
                }

                ValidateConstantName(context, constantNames, evt.Channel);
            }
        }

        private static void ValidateConstantName(KKUIPipelineContext context, HashSet<string> names, string relativeChannel)
        {
            var name = BusChannelUtility.ToConstantName(context.Package.PackageId, relativeChannel);
            if (!names.Add(name))
            {
                context.Add(KKUIPipelineIssueSeverity.Error, "BUS008", $"Generated bus constant name '{name}' is duplicated.");
            }
        }
    }
}
