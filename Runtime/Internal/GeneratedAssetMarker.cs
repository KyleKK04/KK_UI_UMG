using UnityEngine;

namespace KK.UI.UMG.Internal
{
    [DisallowMultipleComponent]
    public sealed class GeneratedAssetMarker : MonoBehaviour
    {
        [SerializeField] private string pipelineVersion = "0.5.0";
        [SerializeField] private string packageId;

        public string PipelineVersion => pipelineVersion;
        public string PackageId => packageId;

        public void Initialize(string value)
        {
            packageId = value;
        }
    }
}
