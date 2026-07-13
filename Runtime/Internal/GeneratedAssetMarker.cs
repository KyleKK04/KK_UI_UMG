using UnityEngine;

namespace KK.UI.UMG.Internal
{
    [DisallowMultipleComponent]
    public sealed class GeneratedAssetMarker : MonoBehaviour
    {
        [SerializeField] private string pipelineVersion = "0.5.0";
        [SerializeField] private string packageId;
        [SerializeField] private string sourceManifestPath;

        public string PipelineVersion => pipelineVersion;
        public string PackageId => packageId;
        public string SourceManifestPath => sourceManifestPath;

        public void Initialize(string value, string manifestPath = null)
        {
            packageId = value;
            sourceManifestPath = manifestPath;
        }
    }
}
