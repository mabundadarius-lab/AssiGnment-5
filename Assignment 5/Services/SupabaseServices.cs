using AssiGnment_5.Models;
using Supabase;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ShoppingCart = AssiGnment_5.Models.ShoppingCart;
using UserProfile = AssiGnment_5.Models.UserProfile;

namespace AssiGnment_5.Services
{
    public class SupabaseService
    {
        private readonly Client _client;
        private bool _initialized = false;

        private const string SupabaseUrl = "https://bnwknqhmujnnddrkipmr.supabase.co";
        private const string SupabaseKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImJud2tucWhtdWpubmRkcmtpcG1yIiwicm9sZSI6ImFub24iLCJpYXQiOjE3Nzc0NTU3OTIsImV4cCI6MjA5MzAzMTc5Mn0.rV2amFYFnsQNSWlz_BnezCLivqS_rv_GpUidgw6taCc";
        private const string BucketName = "avatars";

        public SupabaseService()
        {
            var options = new SupabaseOptions { AutoConnectRealtime = false };
            _client = new Client(SupabaseUrl, SupabaseKey, options);
        }

        private async Task EnsureInitializedAsync()
        {
            if (!_initialized)
            {
                await _client.InitializeAsync();
                _initialized = true;
            }
        }

        // ─── PROFILE ─────────────────────────────────────────────────────────────

        public async Task<UserProfile?> GetProfileByIdAsync(Guid userId)
        {
            await EnsureInitializedAsync();
            var response = await _client.From<UserProfile>()
                                        .Where(p => p.Id == userId)
                                        .Get();
            return response.Models.FirstOrDefault();
        }

        public async Task SaveProfileAsync(UserProfile profile)
        {
            await EnsureInitializedAsync();
            await _client.From<UserProfile>().Upsert(profile);
        }

        // ─── PROFILE PICTURE ─────────────────────────────────────────────────────

        public async Task<string?> UploadProfilePictureAsync(Guid userId, string localImagePath)
        {
            try
            {
                await EnsureInitializedAsync();

                string extension = Path.GetExtension(localImagePath).ToLower();
                string fileName = $"{userId}/avatar{extension}";

                string mimeType = extension switch
                {
                    ".jpg" or ".jpeg" => "image/jpeg",
                    ".png" => "image/png",
                    ".gif" => "image/gif",
                    ".webp" => "image/webp",
                    _ => "application/octet-stream"
                };

                byte[] fileBytes = await File.ReadAllBytesAsync(localImagePath);

                try { await _client.Storage.From(BucketName).Remove(new List<string> { fileName }); }
                catch { }

                await _client.Storage
                             .From(BucketName)
                             .Upload(fileBytes, fileName, new Supabase.Storage.FileOptions
                             {
                                 ContentType = mimeType,
                                 Upsert = true
                             });

                return _client.Storage.From(BucketName).GetPublicUrl(fileName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Upload Error] {ex.Message}");
                return null;
            }
        }

        // ─── SHOPPING ITEMS ──────────────────────────────────────────────────────

        public async Task<List<ShoppingItem>> GetShoppingItemsAsync()
        {
            await EnsureInitializedAsync();
            var response = await _client.From<ShoppingItem>().Get();
            return response.Models;
        }

        public async Task<ShoppingItem?> GetShoppingItemByIdAsync(int itemId)
        {
            await EnsureInitializedAsync();
            var response = await _client.From<ShoppingItem>()
                                        .Filter("item_id", Supabase.Postgrest.Constants.Operator.Equals, itemId.ToString())
                                        .Get();
            return response.Models.FirstOrDefault();
        }

        private async Task DecreaseStockAsync(int itemId, int qty)
        {
            var item = await GetShoppingItemByIdAsync(itemId);
            if (item == null) return;
            item.Quantity = Math.Max(0, item.Quantity - qty);
            await _client.From<ShoppingItem>().Upsert(item);
        }

        private async Task IncreaseStockAsync(int itemId, int qty)
        {
            var item = await GetShoppingItemByIdAsync(itemId);
            if (item == null) return;
            item.Quantity += qty;
            await _client.From<ShoppingItem>().Upsert(item);
        }

        // ─── SHOPPING CART ───────────────────────────────────────────────────────

        public async Task<List<ShoppingCart>> GetCartAsync(Guid profileId)
        {
            await EnsureInitializedAsync();
            var response = await _client.From<ShoppingCart>()
                                        .Filter("profile_id", Supabase.Postgrest.Constants.Operator.Equals, profileId.ToString())
                                        .Get();
            return response.Models;
        }

        public async Task<bool> AddToCartAsync(Guid profileId, int itemId, int qty)
        {
            await EnsureInitializedAsync();

            var stockItem = await GetShoppingItemByIdAsync(itemId);
            if (stockItem == null || stockItem.Quantity < qty) return false;

            var existingCart = await GetCartAsync(profileId);
            var existing = existingCart.FirstOrDefault(c => c.ItemId == itemId);

            if (existing != null)
            {
                existing.Quantity += qty;
                await _client.From<ShoppingCart>().Upsert(existing);
            }
            else
            {
                await _client.From<ShoppingCart>().Insert(new ShoppingCart
                {
                    ProfileId = profileId,
                    ItemId = itemId,
                    Quantity = qty
                });
            }

            await DecreaseStockAsync(itemId, qty);
            return true;
        }

        /// <summary>
        /// Removes ONE unit from the cart row.
        /// If quantity drops to 0, the row is deleted entirely.
        /// Stock is always restored by 1.
        /// </summary>
        public async Task RemoveOneFromCartAsync(int cartId)
        {
            await EnsureInitializedAsync();

            // Fetch the cart row
            var cartResponse = await _client.From<ShoppingCart>()
                                            .Filter("cart_id", Supabase.Postgrest.Constants.Operator.Equals, cartId.ToString())
                                            .Get();
            var cartRow = cartResponse.Models.FirstOrDefault();
            if (cartRow == null) return;

            if (cartRow.Quantity > 1)
            {
                // Decrease cart quantity by 1
                cartRow.Quantity -= 1;
                await _client.From<ShoppingCart>().Upsert(cartRow);
            }
            else
            {
                // Last unit — delete the row
                await _client.From<ShoppingCart>()
                             .Filter("cart_id", Supabase.Postgrest.Constants.Operator.Equals, cartId.ToString())
                             .Delete();
            }

            // Always restore 1 unit back to stock
            await IncreaseStockAsync(cartRow.ItemId, 1);
        }

        /// <summary>
        /// Removes the entire cart row regardless of quantity.
        /// Restores full quantity back to stock.
        /// </summary>
        public async Task RemoveAllFromCartAsync(int cartId)
        {
            await EnsureInitializedAsync();

            var cartResponse = await _client.From<ShoppingCart>()
                                            .Filter("cart_id", Supabase.Postgrest.Constants.Operator.Equals, cartId.ToString())
                                            .Get();
            var cartRow = cartResponse.Models.FirstOrDefault();

            await _client.From<ShoppingCart>()
                         .Filter("cart_id", Supabase.Postgrest.Constants.Operator.Equals, cartId.ToString())
                         .Delete();

            if (cartRow != null)
                await IncreaseStockAsync(cartRow.ItemId, cartRow.Quantity);
        }

        public async Task ClearCartAsync(Guid profileId)
        {
            await EnsureInitializedAsync();

            var cartRows = await GetCartAsync(profileId);

            await _client.From<ShoppingCart>()
                         .Filter("profile_id", Supabase.Postgrest.Constants.Operator.Equals, profileId.ToString())
                         .Delete();

            foreach (var row in cartRows)
                await IncreaseStockAsync(row.ItemId, row.Quantity);
        }
    }
}