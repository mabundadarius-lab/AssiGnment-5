using System.Collections.ObjectModel;
using System.Collections.Specialized;
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
        public decimal Subtotal => Price * Quantity;
    }

    public class ShoppingCartViewModel : BaseViewModel
    {
        private readonly SupabaseService _service;
        private readonly Guid _profileId;

        public ObservableCollection<CartItemDisplay> CartItems { get; } = new();

        // Total of all items in cart
        private decimal _cartTotal;
        public decimal CartTotal
        {
            get => _cartTotal;
            private set
            {
                _cartTotal = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CartTotalDisplay));
                OnPropertyChanged(nameof(HasItems));
            }
        }

        public string CartTotalDisplay => $"Total: R{CartTotal:N2}";
        public bool HasItems => CartItems.Count > 0;

        public ShoppingCartViewModel()
        {
            _service = new SupabaseService();
            var stored = Preferences.Get("UserId", string.Empty);
            _profileId = string.IsNullOrEmpty(stored) ? Guid.NewGuid() : Guid.Parse(stored);

            // Recalculate total whenever the collection changes
            CartItems.CollectionChanged += (_, _) => RecalculateTotal();
        }

        private void RecalculateTotal()
        {
            CartTotal = CartItems.Sum(i => i.Subtotal);
            OnPropertyChanged(nameof(HasItems));
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
                RecalculateTotal();
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

        public async Task RemoveOneAsync(int cartId)
        {
            try
            {
                await _service.RemoveOneFromCartAsync(cartId);
                await InitAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RemoveOne Error] {ex.Message}");
            }
        }

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
                RecalculateTotal();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ClearCart Error] {ex.Message}");
            }
        }
    }
}

