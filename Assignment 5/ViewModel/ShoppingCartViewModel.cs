using System.Collections.ObjectModel;
using AssiGnment_5.Models;
using AssiGnment_5.Services;
using Microsoft.Maui.Storage;

namespace AssiGnment_5.ViewModel
{
    public class CartItemDisplay
    {
        public int CartId { get; set; }
        public int ItemId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }

        public string PriceDisplay => $"R{Price:N2} each";
        public string SubtotalDisplay => $"Subtotal: R{Price * Quantity:N2}";
        public string QuantityDisplay => $"Qty: {Quantity}";
    }

    public class ShoppingCartViewModel : BaseViewModel
    {
        private readonly SupabaseService _service;
        private readonly Guid _profileId;

        public ObservableCollection<CartItemDisplay> CartItems { get; } = new();

        public ShoppingCartViewModel()
        {
            _service = new SupabaseService();
            var stored = Preferences.Get("UserId", string.Empty);
            _profileId = string.IsNullOrEmpty(stored) ? Guid.NewGuid() : Guid.Parse(stored);
        }

        public async Task InitAsync()
        {
            IsBusy = true;
            try
            {
                CartItems.Clear();
                var cartRows = await _service.GetCartAsync(_profileId);
                foreach (var row in cartRows)
                {
                    var item = await _service.GetShoppingItemByIdAsync(row.ItemId);
                    CartItems.Add(new CartItemDisplay
                    {
                        CartId = row.CartId,
                        ItemId = row.ItemId,
                        Name = item?.Name ?? $"Item #{row.ItemId}",
                        Description = item?.Description ?? string.Empty,
                        Price = item?.Price ?? 0,
                        Quantity = row.Quantity
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LoadCart Error] {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        // Removes ONE unit — decreases qty by 1, deletes row at 0
        public async Task RemoveOneAsync(int cartId)
        {
            try
            {
                await _service.RemoveOneFromCartAsync(cartId);
                await InitAsync(); // reload to reflect updated qty or removal
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RemoveOne Error] {ex.Message}");
            }
        }

        // Removes the entire row regardless of quantity
        public async Task RemoveAllAsync(int cartId)
        {
            try
            {
                await _service.RemoveAllFromCartAsync(cartId);
                await InitAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RemoveAll Error] {ex.Message}");
            }
        }

        public async Task ClearCartAsync()
        {
            try
            {
                CartItems.Clear();
                await _service.ClearCartAsync(_profileId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ClearCart Error] {ex.Message}");
            }
        }
    }
}

