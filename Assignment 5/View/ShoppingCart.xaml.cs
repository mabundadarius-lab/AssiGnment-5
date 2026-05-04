using AssiGnment_5.ViewModel;

namespace AssiGnment_5.View
{
    public partial class ShoppingCart : ContentPage
    {
        private readonly ShoppingCartViewModel _vm;

        public ShoppingCart()
        {
            InitializeComponent();
            _vm = new ShoppingCartViewModel();
            BindingContext = _vm;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _vm.InitAsync();
        }

        // Removes ONE unit — qty goes from e.g. 3 → 2, row deleted at 0
        private async void OnRemoveOneClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is int cartId)
                await _vm.RemoveOneAsync(cartId);
        }

        // Removes the entire row regardless of quantity
        private async void OnRemoveAllClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is int cartId)
                await _vm.RemoveAllAsync(cartId);
        }

        private async void OnClearCartClicked(object sender, EventArgs e)
        {
            bool confirm = await DisplayAlert("Clear Cart", "Remove all items from cart?", "Yes", "No");
            if (confirm)
            {
                await _vm.ClearCartAsync();
                await _vm.InitAsync();
            }
        }

        private async void OnBackToShoppingClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//ShoppingItems");
        }

        private async void OnViewProfileClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//MainPage");
        }
    }
}
