using AssiGnment_5.Services;
using AssiGnment_5.ViewModel;
using AssiGnment_5.Models;

namespace AssiGnment_5.View
{
    public partial class ShoppingItems : ContentPage
    {
        private readonly SupabaseService _supabase;
        private List<ShoppingItem> _items = new();
        private bool _hasProfile = false;

        public ShoppingItems()
        {
            InitializeComponent();
            _supabase = new SupabaseService();
           
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadEverything();
            
        }

        private async Task LoadEverything()
        {
            try
            {
                Loader.IsRunning = true;
                StatusLabel.Text = "Loading items...";

                // Load items directly — no ViewModel in between
                _items = await _supabase.GetShoppingItemsAsync();

                // Set ItemsSource directly on the CollectionView
                ItemsCollection.ItemsSource = _items;

                StatusLabel.Text = _items.Count > 0
                    ? $"{_items.Count} items loaded"
                    : "No items found in database";

                // Check profile
                var stored = Microsoft.Maui.Storage.Preferences.Get("UserId", string.Empty);
                if (!string.IsNullOrEmpty(stored))
                {
                    var profile = await _supabase.GetProfileByIdAsync(Guid.Parse(stored));
                    _hasProfile = profile != null && !string.IsNullOrWhiteSpace(profile.Name);
                }
            }
            catch (Exception ex)
            {
                StatusLabel.Text = $"Error: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"❌ LoadEverything: {ex}");
            }
            finally
            {
                Loader.IsRunning = false;
            }
        }

        private async void OnAddToCartClicked(object sender, EventArgs e)
        {
            if (!_hasProfile)
            {
                bool go = await DisplayAlert("Profile Required",
                    "Please save your profile first.", "Go to Profile", "Cancel");
                if (go) await Shell.Current.GoToAsync(nameof(MainPage));
                return;
            }

            if (sender is Button btn && btn.CommandParameter is int itemId)
            {
                try
                {
                    var stored = Microsoft.Maui.Storage.Preferences.Get("UserId", string.Empty);
                    var profileId = Guid.Parse(stored);
                    bool success = await _supabase.AddToCartAsync(profileId, itemId, 1);

                    if (success)
                    {
                        // Update stock locally
                        var item = _items.FirstOrDefault(i => i.ItemId == itemId);
                        if (item != null)
                        {
                            item.Quantity = Math.Max(0, item.Quantity - 1);
                            ItemsCollection.ItemsSource = null;
                            ItemsCollection.ItemsSource = _items;
                        }
                        await DisplayAlert("Added!", "Item added to your cart.", "OK");
                    }
                    else
                    {
                        await DisplayAlert("Out of Stock", "No more stock available.", "OK");
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", ex.Message, "OK");
                }
            }
        }

        private async void OnViewCartClicked(object sender, EventArgs e)
            => await Shell.Current.GoToAsync(nameof(ShoppingCart));

        private async void OnProfileClicked(object sender, EventArgs e)
            => await Shell.Current.GoToAsync(nameof(MainPage));
    }
}