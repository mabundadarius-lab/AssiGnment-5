using System.Collections.ObjectModel;
using AssiGnment_5.Models;
using AssiGnment_5.Services;
using Microsoft.Maui.Storage;

namespace AssiGnment_5.ViewModel
{
    public class ShoppingItemsViewModel : BaseViewModel
    {
        private readonly SupabaseService _service;
        private readonly Guid _profileId;

        public ObservableCollection<ShoppingItem> Items { get; } = new();

        public ShoppingItemsViewModel()
        {
            _service = new SupabaseService();
            var stored = Preferences.Get("UserId", string.Empty);
            _profileId = string.IsNullOrEmpty(stored) ? Guid.NewGuid() : Guid.Parse(stored);
        }

        public async Task InitAsync() => await LoadItemsAsync();

        private async Task LoadItemsAsync()
        {
            IsBusy = true;
            try
            {
                Items.Clear();
                var items = await _service.GetShoppingItemsAsync();
                foreach (var item in items)
                    Items.Add(item);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LoadItems Error] {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task<bool> AddToCartAsync(int itemId)
        {
            try
            {
                bool success = await _service.AddToCartAsync(_profileId, itemId, 1);

                // Reload items so stock count updates on screen immediately
                if (success)
                    await LoadItemsAsync();

                return success;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AddToCart Error] {ex.Message}");
                return false;
            }
        }
    }
}