using System;
using System.Collections.Generic;
using UnityEngine;

namespace KK.UI.UMG.Samples.Inventory
{
    public sealed class KkSampleInventoryService : MonoBehaviour, IInventoryService
    {
        [SerializeField] private Sprite _swordIcon;
        [SerializeField] private Sprite _shieldIcon;
        [SerializeField] private Sprite _potionIcon;

        private readonly List<InventoryItemModel> _items = new List<InventoryItemModel>();
        private bool _seeded;

        public event Action InventoryChanged;

        private void Awake()
        {
            ResetItems();
        }

        public IReadOnlyList<InventoryItemModel> GetItems()
        {
            EnsureItems();
            return _items;
        }

        public bool TryUseItem(string itemId)
        {
            EnsureItems();
            var index = _items.FindIndex(item => item.Id == itemId);
            if (index < 0)
            {
                return false;
            }

            var item = _items[index];
            if (item.Category == "Consumable")
            {
                if (item.Count > 1)
                {
                    _items[index] = item.WithCount(item.Count - 1);
                }
                else
                {
                    _items.RemoveAt(index);
                }
            }

            InventoryChanged?.Invoke();
            return true;
        }

        public void ResetItems()
        {
            var swordIcon = _swordIcon != null ? _swordIcon : DemoInventoryIcons.Sword;
            var shieldIcon = _shieldIcon != null ? _shieldIcon : DemoInventoryIcons.Shield;
            var potionIcon = _potionIcon != null ? _potionIcon : DemoInventoryIcons.Potion;

            _items.Clear();
            _items.Add(new InventoryItemModel("iron_sword", "Iron Sword", "A dependable blade for close combat.", "Weapon", 0.36f, true, 1, swordIcon));
            _items.Add(new InventoryItemModel("tower_shield", "Tower Shield", "Heavy shield for holding the line.", "Armor", 0.72f, true, 1, shieldIcon));
            _items.Add(new InventoryItemModel("health_potion", "Health Potion", "Restores health over time.", "Consumable", 0.18f, false, 6, potionIcon));
            _items.Add(new InventoryItemModel("ember_sword", "Ember Sword", "A rare sword with a warm edge.", "Weapon", 0.82f, false, 1, swordIcon));
            _items.Add(new InventoryItemModel("scout_buckler", "Scout Buckler", "Light armor for quick movement.", "Armor", 0.48f, false, 1, shieldIcon));
            _items.Add(new InventoryItemModel("focus_potion", "Focus Potion", "Improves skill recovery.", "Consumable", 0.58f, false, 3, potionIcon));

            _seeded = true;
            InventoryChanged?.Invoke();
        }

        private void EnsureItems()
        {
            if (!_seeded)
            {
                ResetItems();
            }
        }

        private static class DemoInventoryIcons
        {
            private static Sprite _sword;
            private static Sprite _shield;
            private static Sprite _potion;

            public static Sprite Sword => _sword ?? (_sword = CreateSword());
            public static Sprite Shield => _shield ?? (_shield = CreateShield());
            public static Sprite Potion => _potion ?? (_potion = CreatePotion());

            private static Sprite CreateSword()
            {
                return Create("KkSampleSwordIcon", texture =>
                {
                    FillRect(texture, 15, 6, 18, 24, new Color32(235, 241, 248, 255));
                    FillRect(texture, 13, 24, 20, 27, new Color32(218, 166, 64, 255));
                    FillRect(texture, 16, 27, 17, 31, new Color32(132, 88, 42, 255));
                    SetPixel(texture, 14, 7, new Color32(235, 241, 248, 255));
                    SetPixel(texture, 19, 7, new Color32(235, 241, 248, 255));
                });
            }

            private static Sprite CreateShield()
            {
                return Create("KkSampleShieldIcon", texture =>
                {
                    for (var y = 5; y <= 27; y++)
                    {
                        var inset = y < 17 ? Mathf.Abs(12 - y) / 2 : (y - 17) / 2;
                        FillRect(texture, 9 + inset, y, 24 - inset, y, new Color32(84, 150, 220, 255));
                    }

                    FillRect(texture, 15, 7, 17, 25, new Color32(179, 218, 255, 255));
                });
            }

            private static Sprite CreatePotion()
            {
                return Create("KkSamplePotionIcon", texture =>
                {
                    FillRect(texture, 14, 5, 19, 9, new Color32(102, 210, 182, 255));
                    FillRect(texture, 13, 10, 20, 12, new Color32(229, 236, 244, 255));
                    for (var y = 13; y <= 27; y++)
                    {
                        var inset = y < 18 ? 18 - y : 0;
                        FillRect(texture, 8 + inset, y, 25 - inset, y, new Color32(215, 72, 96, 255));
                    }

                    FillRect(texture, 11, 17, 22, 23, new Color32(248, 109, 132, 255));
                });
            }

            private static Sprite Create(string name, Action<Texture2D> draw)
            {
                var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
                texture.name = name;
                texture.hideFlags = HideFlags.HideAndDontSave;
                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Clamp;
                for (var y = 0; y < texture.height; y++)
                {
                    for (var x = 0; x < texture.width; x++)
                    {
                        texture.SetPixel(x, y, Color.clear);
                    }
                }

                draw(texture);
                texture.Apply();
                var sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 32f);
                sprite.name = name;
                sprite.hideFlags = HideFlags.HideAndDontSave;
                return sprite;
            }

            private static void FillRect(Texture2D texture, int left, int top, int right, int bottom, Color32 color)
            {
                for (var y = top; y <= bottom; y++)
                {
                    for (var x = left; x <= right; x++)
                    {
                        SetPixel(texture, x, y, color);
                    }
                }
            }

            private static void SetPixel(Texture2D texture, int x, int y, Color32 color)
            {
                if (x < 0 || y < 0 || x >= texture.width || y >= texture.height)
                {
                    return;
                }

                texture.SetPixel(x, texture.height - 1 - y, color);
            }
        }
    }
}
