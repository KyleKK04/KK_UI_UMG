using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using KK.UI.UMG.Editor.Manifests;
using KK.UI.UMG.Editor.Pipeline;
using KK.UI.UMG.Editor.Validators;

namespace KK.UI.UMG.Editor.Tests
{
    public sealed class BusChannelValidatorTests
    {
        [Test]
        public void InboundChannelMustStartWithIn()
        {
            var context = CreateContext();
            context.Codegen.Bus.Routes[0].Channel = "out.open_requested";

            new BusChannelValidator().Validate(context);

            Assert.That(context.Issues.Any(issue => issue.Code == "BUS001"), Is.True);
        }

        [Test]
        public void OutboundChannelMustStartWithOut()
        {
            var context = CreateContext();
            context.Bindings.Events[0].Channel = "in.confirm_result";

            new BusChannelValidator().Validate(context);

            Assert.That(context.Issues.Any(issue => issue.Code == "BUS005"), Is.True);
        }

        [Test]
        public void PayloadFieldsMustExist()
        {
            var context = CreateContext();
            context.Bindings.Events[0].ChannelPayloadFields = new List<string> { "Missing" };

            new BusChannelValidator().Validate(context);

            Assert.That(context.Issues.Any(issue => issue.Code == "BUS007"), Is.True);
        }

        [Test]
        public void DuplicateInboundChannelFails()
        {
            var context = CreateContext();
            context.Codegen.Bus.Routes.Add(new UiBusRouteSpec { Channel = "in.open_requested", Action = "Close" });

            new BusChannelValidator().Validate(context);

            Assert.That(context.Issues.Any(issue => issue.Code == "BUS004"), Is.True);
        }

        [Test]
        public void DuplicateConstantNameFails()
        {
            var context = CreateContext();
            context.Bindings.Events.Add(new UiEventSpec
            {
                ControlId = "ConfirmButton",
                Event = "onClick",
                Handler = "OnOtherRequested",
                Channel = "out.confirm__result"
            });

            new BusChannelValidator().Validate(context);

            Assert.That(context.Issues.Any(issue => issue.Code == "BUS008"), Is.True);
        }

        private static KKUIPipelineContext CreateContext()
        {
            return new KKUIPipelineContext
            {
                Package = new UiPackageManifest
                {
                    PackageId = "TestBox",
                    Namespace = "Game.UI.Tests"
                },
                Codegen = new UiCodegenManifest
                {
                    Bus = new UiBusCodegen
                    {
                        Routes = new List<UiBusRouteSpec>
                        {
                            new UiBusRouteSpec { Channel = "in.open_requested", Action = "Open" }
                        }
                    }
                },
                Bindings = new UiBindingsManifest
                {
                    Mvvm = new UiMvvmSection
                    {
                        Fields = new List<UiViewModelFieldSpec>
                        {
                            new UiViewModelFieldSpec { Id = "Message", Type = "string" }
                        }
                    },
                    Events = new List<UiEventSpec>
                    {
                        new UiEventSpec
                        {
                            ControlId = "ConfirmButton",
                            Event = "onClick",
                            Handler = "OnConfirmRequested",
                            Channel = "out.confirm_result",
                            ChannelPayloadFields = new List<string> { "Message" }
                        }
                    }
                }
            };
        }
    }
}
