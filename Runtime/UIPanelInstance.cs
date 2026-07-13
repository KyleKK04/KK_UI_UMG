using UnityEngine;

namespace KK.UI.UMG
{
    internal sealed class UIPanelInstance
    {
        public UIPanelInstance(string systemId, GameObject root, UIViewBase view, UIControllerBase controller)
        {
            SystemId = systemId;
            Root = root;
            View = view;
            Controller = controller;
        }

        public string SystemId { get; }
        public GameObject Root { get; }
        public UIViewBase View { get; }
        public UIControllerBase Controller { get; }
        public bool IsHidden { get; set; }
        public bool IsActivated { get; set; }
    }
}
