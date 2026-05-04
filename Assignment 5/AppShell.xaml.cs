using AssiGnment_5.View;

namespace AssiGnment_5
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // Register routes for pages not in the Shell hierarchy

            Routing.RegisterRoute(nameof(ShoppingCart), typeof(ShoppingCart));
        }
    }
}
