using System;
using System.Collections.Generic;

namespace KK.UI.UMG.Samples.Inventory
{
    public interface IInventoryService
    {
        event Action InventoryChanged;

        IReadOnlyList<InventoryItemModel> GetItems();

        bool TryUseItem(string itemId);
    }
}
