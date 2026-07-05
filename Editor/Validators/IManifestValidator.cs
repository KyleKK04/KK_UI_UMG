using KK.UI.UMG.Editor.Pipeline;

namespace KK.UI.UMG.Editor.Validators
{
    public interface IManifestValidator
    {
        void Validate(KKUIPipelineContext context);
    }
}
