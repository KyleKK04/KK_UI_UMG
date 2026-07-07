using UnityEngine;

namespace KK.UI.UMG.Samples.Inventory
{
    public sealed class InventoryItemModel
    {
        public InventoryItemModel(
            string id,
            string name,
            string description,
            string category,
            float quality,
            bool equipped,
            int count,
            Sprite icon)
        {
            Id = id;
            Name = name;
            Description = description;
            Category = category;
            Quality = quality;
            Equipped = equipped;
            Count = count;
            Icon = icon;
        }

        public string Id { get; }
        public string Name { get; }
        public string Description { get; }
        public string Category { get; }
        public float Quality { get; }
        public bool Equipped { get; }
        public int Count { get; }
        public Sprite Icon { get; }

        public InventoryItemModel WithCount(int count)
        {
            return new InventoryItemModel(Id, Name, Description, Category, Quality, Equipped, count, Icon);
        }
    }
}
