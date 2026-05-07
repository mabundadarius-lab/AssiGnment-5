using AssiGnment_5.View;

namespace AssiGnment_5
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // Register all navigation routes
            Routing.RegisterRoute(nameof(MainPage), typeof(MainPage));
            Routing.RegisterRoute(nameof(ShoppingCart), typeof(ShoppingCart));
        }
    }
}
