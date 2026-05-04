using AssiGnment_5.ViewModel;

namespace AssiGnment_5.View
{
    public partial class ShoppingItems : ContentPage
    {
        private readonly ShoppingItemsViewModel _vm;

        public ShoppingItems()
        {
            InitializeComponent();
            _vm = new ShoppingItemsViewModel();
            BindingContext = _vm;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _vm.InitAsync();
        }

        // Handles Add to Cart click — uses CommandParameter (ItemId) directly
        private async void OnAddToCartClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is int itemId)
            {
                bool success = await _vm.AddToCartAsync(itemId);

                if (success)
                    await DisplayAlert("Added!", "Item added to your cart.", "OK");
                else
                    await DisplayAlert("Out of Stock", "Cannot add more — stock limit reached.", "OK");
            }
        }

        private async void OnViewCartClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(ShoppingCart));
        }
    }
}