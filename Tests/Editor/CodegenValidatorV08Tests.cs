using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using KK.UI.UMG.Editor.Manifests;
using KK.UI.UMG.Editor.Pipeline;
using KK.UI.UMG.Editor.Validators;

namespace KK.UI.UMG.Editor.Tests
{
    public sealed class CodegenValidatorV08Tests
    {
        [Test]
        public void RequiredServicesAcceptValidDeclaration()
        {
            var context = CreateContext();
            context.Codegen.RequiredServices.Add(new UiRequiredServiceSpec
            {
                Type = "Game.Inventory.IInventoryService",
                Property = "InventoryService"
            });

            new CodegenValidator().Validate(context);

            Assert.That(context.Issues.Any(issue => issue.Code.StartsWith("CG")), Is.False);
        }

        [Test]
        public void RequiredServicesRejectEmptyType()
        {
            var context = CreateContext();
            context.Codegen.RequiredServices.Add(new UiRequiredServiceSpec
            {
                Type = "",
                Property = "InventoryService"
            });

            new CodegenValidator().Validate(context);

            Assert.That(context.Issues.Any(issue => issue.Code == "CG020"), Is.True);
        }

        [Test]
        public void RequiredServicesRejectInvalidProperty()
        {
            var context = CreateContext();
            context.Codegen.RequiredServices.Add(new UiRequiredServiceSpec
            {
                Type = "Game.Inventory.IInventoryService",
                Property = "inventory-service"
            });

            new CodegenValidator().Validate(context);

            Assert.That(context.Issues.Any(issue => issue.Code == "CG021"), Is.True);
        }

        [Test]
        public void RequiredServicesRejectDuplicateProperty()
        {
            var context = CreateContext();
            context.Codegen.RequiredServices.Add(new UiRequiredServiceSpec
            {
                Type = "Game.Inventory.IInventoryService",
                Property = "InventoryService"
            });
            context.Codegen.RequiredServices.Add(new UiRequiredServiceSpec
            {
                Type = "Game.Inventory.IInventoryTooltipService",
                Property = "InventoryService"
            });

            new CodegenValidator().Validate(context);

            Assert.That(context.Issues.Any(issue => issue.Code == "CG022"), Is.True);
        }

        [Test]
        public void RequiredServicesRejectGeneratedMemberConflict()
        {
            var context = CreateContext();
            context.Codegen.RequiredServices.Add(new UiRequiredServiceSpec
            {
                Type = "Game.Inventory.IInventoryService",
                Property = "Store"
            });

            new CodegenValidator().Validate(context);

            Assert.That(context.Issues.Any(issue => issue.Code == "CG023"), Is.True);
        }

        private static KKUIPipelineContext CreateContext()
        {
            var sourceRoot = Path.GetFullPath("Assets/UI/Source/TestBox");
            return new KKUIPipelineContext
            {
                SourceRoot = sourceRoot,
                Package = new UiPackageManifest
                {
                    PackageId = "TestBox",
                    Namespace = "Game.UI.Tests"
                },
                Codegen = new UiCodegenManifest
                {
                    SchemaVersion = "1.0",
                    Namespace = "Game.UI.Tests",
                    OutputRoot = "../../Generated/TestBox",
                    AddressablesKey = "UI/TestBox/TestBoxView",
                    View = new UiViewCodegen { ClassName = "TestBoxView", BaseClass = "UIViewBase" },
                    Controller = new UiControllerCodegen { ClassName = "TestBoxController", BaseClass = "UIControllerBase" },
                    ViewModel = new UiViewModelCodegen { ClassName = "TestBoxViewModel" },
                    RequiredServices = new List<UiRequiredServiceSpec>()
                },
                Bindings = new UiBindingsManifest
                {
                    Events = new List<UiEventSpec>()
                }
            };
        }
    }
}
