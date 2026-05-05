using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using AssiGnment_5.Models;
using AssiGnment_5.Services;
using Microsoft.Maui.Storage;

namespace AssiGnment_5.ViewModel
{
    public class ShoppingItemDisplay : INotifyPropertyChanged
    {
        private int _quantity;

        public int ItemId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string ImageUrl { get; set; } = string.Empty;

        public int Quantity
        {
            get => _quantity;
            set
            {
                _quantity = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StockDisplay));
            }
        }

        public string StockDisplay => $"In stock: {Quantity}";

        // Show placeholder if no image URL set
        public bool HasImage => !string.IsNullOrWhiteSpace(ImageUrl);

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class ShoppingItemsViewModel : BaseViewModel
    {
        private readonly SupabaseService _service;
        private readonly Guid _profileId;

        public ObservableCollection<ShoppingItemDisplay> Items { get; } = new();

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
                {
                    Items.Add(new ShoppingItemDisplay
                    {
                        ItemId = item.ItemId,
                        Name = item.Name,
                        Description = item.Description,
                        Price = item.Price,
                        Quantity = item.Quantity,
                        ImageUrl = item.ImageUrl
                    });
                }
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
                if (success)
                {
                    var display = Items.FirstOrDefault(i => i.ItemId == itemId);
                    if (display != null)
                        display.Quantity = Math.Max(0, display.Quantity - 1);
                }
                return success;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AddToCart Error] {ex.Message}");
                return false;
            }
        }

        public async Task RefreshStockAsync()
        {
            try
            {
                var latest = await _service.GetShoppingItemsAsync();
                foreach (var updated in latest)
                {
                    var display = Items.FirstOrDefault(i => i.ItemId == updated.ItemId);
                    if (display != null)
                        display.Quantity = updated.Quantity;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RefreshStock Error] {ex.Message}");
            }
        }
    }
}