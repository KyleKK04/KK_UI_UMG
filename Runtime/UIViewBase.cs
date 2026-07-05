using UnityEngine;

namespace KK.UI.UMG
{
    public abstract class UIViewBase : MonoBehaviour
    {
        public UIControllerBase Controller { get; internal set; }
        internal CanvasGroup LayerCanvasGroup { get; private set; }

        protected virtual void Awake()
        {
            LayerCanvasGroup = GetComponent<CanvasGroup>();
        }

        protected virtual void OnEnable()
        {
            BindEvents();
        }

        protected virtual void OnDisable()
        {
            UnbindEvents();
        }

        protected abstract void BindEvents();
        protected abstract void UnbindEvents();

        internal void SetInteraction(bool interactable, bool blocksRaycasts)
        {
            EnsureCanvasGroup();
            LayerCanvasGroup.interactable = interactable;
            LayerCanvasGroup.blocksRaycasts = blocksRaycasts;
        }

        internal void SetAlpha(float alpha)
        {
            EnsureCanvasGroup();
            LayerCanvasGroup.alpha = alpha;
        }

        private void EnsureCanvasGroup()
        {
            if (LayerCanvasGroup != null)
            {
                return;
            }

            LayerCanvasGroup = GetComponent<CanvasGroup>();
            if (LayerCanvasGroup == null)
            {
                LayerCanvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }
    }
}
