using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KK.UI.UMG;

namespace KK.UI.UMG.Samples.Inventory
{
    public partial class KkSampleInventoryPanelController : UIControllerBase
    {
        private const string FieldSearchText = "SearchText";
        private const string FieldCategoryIndex = "CategoryIndex";
        private const string FieldEquippedOnly = "EquippedOnly";
        private const string FieldMinQuality = "MinQuality";
        private const string FieldWeightRatio = "WeightRatio";
        private const string FieldUseButtonEnabled = "UseButtonEnabled";
        private const string FieldInventoryItems = "InventoryItems";
        private const string FieldSelectedName = "SelectedName";
        private const string FieldSelectedDescription = "SelectedDescription";
        private const string FieldStatus = "Status";
        private static readonly IReadOnlyList<InventoryItemModel> EmptyItems = new InventoryItemModel[0];

        private IInventoryService _inventoryService;
        private string _selectedItemId;
        private string _searchText = string.Empty;
        private int _categoryIndex;
        private bool _equippedOnly;
        private float _minQuality;
        private bool _handlingServiceCommand;

        protected override void OnGeneratedInitialize(MessagePayload payload)
        {
            _inventoryService = RequireService<IInventoryService>();
            _inventoryService.InventoryChanged += HandleInventoryChanged;
            TrackSubscription(UISubscription.Create(() => _inventoryService.InventoryChanged -= HandleInventoryChanged));

            _selectedItemId = null;
            _searchText = string.Empty;
            _categoryIndex = 0;
            _equippedOnly = false;
            _minQuality = 0f;

            Store.Update(FieldSearchText, string.Empty);
            Store.Update(FieldCategoryIndex, 0);
            Store.Update(FieldEquippedOnly, false);
            Store.Update(FieldMinQuality, 0f);
            Store.Update(FieldWeightRatio, 0.42f);
            Store.Update(FieldUseButtonEnabled, false);

            RefreshItems("Ready", false);
        }

        protected override void OnGeneratedEvent(string handler, object[] args)
        {
            switch (handler)
            {
                case "OnCloseRequested":
                    HandleCloseRequested();
                    break;
                case "OnRefreshRequested":
                    HandleRefreshRequested();
                    break;
                case "OnUseSelectedRequested":
                    HandleUseSelectedRequested();
                    break;
                case "OnSearchSubmitted":
                    HandleSearchSubmitted(args != null && args.Length > 0 ? args[0] as string : string.Empty);
                    break;
                case "OnEquippedOnlyChanged":
                    HandleEquippedOnlyChanged(args != null && args.Length > 0 && args[0] is bool isEquippedOnly && isEquippedOnly);
                    break;
                case "OnQualityChanged":
                    HandleQualityChanged(args != null && args.Length > 0 && args[0] is float quality ? quality : 0f);
                    break;
                case "OnCategoryChanged":
                    HandleCategoryChanged(args != null && args.Length > 0 && args[0] is int category ? category : 0);
                    break;
                case "OnInventoryItemClicked":
                    var index = args != null && args.Length > 0 && args[0] is int itemIndex ? itemIndex : -1;
                    var itemId = args != null && args.Length > 1 ? args[1] as string : string.Empty;
                    HandleInventoryItemClicked(index, itemId);
                    break;
            }
        }

        private void HandleCloseRequested()
        {
            if (UIManager != null)
            {
                _ = UIManager.CloseAsync(SystemId);
            }
        }

        private void HandleRefreshRequested()
        {
            _selectedItemId = null;
            RefreshItems("Inventory refreshed");
        }

        private void HandleUseSelectedRequested()
        {
            var item = GetItemsSnapshot().FirstOrDefault(candidate => candidate.Id == _selectedItemId);
            if (item == null)
            {
                Store.Update(FieldStatus, "Select an item first.");
                Flush();
                return;
            }

            var used = false;
            _handlingServiceCommand = true;
            try
            {
                used = _inventoryService.TryUseItem(item.Id);
            }
            finally
            {
                _handlingServiceCommand = false;
            }

            if (!used)
            {
                RefreshItems($"Could not use {item.Name}.");
                return;
            }

            if (item.Category == "Consumable" && item.Count <= 1)
            {
                _selectedItemId = null;
            }

            RefreshItems($"Used {item.Name}.");
        }

        private void HandleSearchSubmitted(string value)
        {
            _searchText = value?.Trim() ?? string.Empty;
            Store.Update(FieldSearchText, _searchText);
            RefreshItems(string.IsNullOrEmpty(_searchText) ? "Search cleared" : $"Search: {_searchText}");
        }

        private void HandleEquippedOnlyChanged(bool value)
        {
            _equippedOnly = value;
            Store.Update(FieldEquippedOnly, value);
            RefreshItems(value ? "Showing equipped items" : "Showing all equipment states");
        }

        private void HandleQualityChanged(float value)
        {
            _minQuality = Mathf.Clamp01(value);
            Store.Update(FieldMinQuality, _minQuality);
            RefreshItems($"Minimum quality: {_minQuality:0.00}");
        }

        private void HandleCategoryChanged(int value)
        {
            _categoryIndex = Mathf.Clamp(value, 0, 3);
            Store.Update(FieldCategoryIndex, _categoryIndex);
            RefreshItems($"Category: {CategoryName(_categoryIndex)}");
        }

        private void HandleInventoryItemClicked(int index, string itemId)
        {
            var item = GetItemsSnapshot().FirstOrDefault(candidate => candidate.Id == itemId);
            if (item == null)
            {
                Store.Update(FieldStatus, $"Missing item at index {index}.");
                Flush();
                return;
            }

            _selectedItemId = item.Id;
            RefreshItems($"Selected: {item.Name}");
        }

        private void HandleInventoryChanged()
        {
            if (_handlingServiceCommand)
            {
                return;
            }

            RefreshItems("Inventory changed");
        }

        private void RefreshItems(string status, bool flush = true)
        {
            var items = GetItemsSnapshot();
            var normalizedSearch = _searchText.ToLowerInvariant();
            var visible = items
                .Where(item => CategoryMatches(item, _categoryIndex))
                .Where(item => !_equippedOnly || item.Equipped)
                .Where(item => item.Quality >= _minQuality)
                .Where(item => string.IsNullOrEmpty(normalizedSearch) || (item.Name ?? string.Empty).ToLowerInvariant().Contains(normalizedSearch))
                .ToList();

            Store.Update<IReadOnlyList<MessagePayload>>(FieldInventoryItems, visible.Select(ToPayload).ToList());
            Store.Update(FieldWeightRatio, Mathf.Clamp01(items.Sum(item => item.Count) / 12f));
            UpdateSelectedDetails(items);
            Store.Update(FieldStatus, $"{status} ({visible.Count}/{items.Count})");
            if (flush)
            {
                Flush();
            }
        }

        private IReadOnlyList<InventoryItemModel> GetItemsSnapshot()
        {
            return _inventoryService?.GetItems() ?? EmptyItems;
        }

        private void UpdateSelectedDetails(IReadOnlyList<InventoryItemModel> items)
        {
            var selected = items.FirstOrDefault(item => item.Id == _selectedItemId);
            if (selected == null)
            {
                _selectedItemId = null;
                Store.Update(FieldSelectedName, "No item selected");
                Store.Update(FieldSelectedDescription, "Select an item to inspect details.");
                Store.Update(FieldUseButtonEnabled, false);
                return;
            }

            Store.Update(FieldSelectedName, selected.Name);
            Store.Update(FieldSelectedDescription, selected.Description);
            Store.Update(FieldUseButtonEnabled, true);
        }

        private static MessagePayload ToPayload(InventoryItemModel item)
        {
            var payload = new MessagePayload();
            payload.Set("id", item.Id);
            payload.Set("name", item.Name);
            payload.Set("count", $"x{item.Count}");
            payload.Set("icon", item.Icon);
            payload.Set("description", item.Description);
            payload.Set("quality", item.Quality);
            payload.Set("equipped", item.Equipped);
            return payload;
        }

        private static bool CategoryMatches(InventoryItemModel item, int categoryIndex)
        {
            return categoryIndex == 0 || item.Category == CategoryName(categoryIndex);
        }

        private static string CategoryName(int categoryIndex)
        {
            switch (categoryIndex)
            {
                case 1:
                    return "Weapon";
                case 2:
                    return "Armor";
                case 3:
                    return "Consumable";
                default:
                    return "All";
            }
        }
    }
}
