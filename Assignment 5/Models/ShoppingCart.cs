using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using ColumnAttribute = Supabase.Postgrest.Attributes.ColumnAttribute;
using TableAttribute = Supabase.Postgrest.Attributes.TableAttribute;

namespace AssiGnment_5.Models
{
    [Table("shopping_cart")]
    public class ShoppingCart : BaseModel
    {
        [PrimaryKey("cart_id", false)]
        public int CartId { get; set; }

        [Column("profile_id")]
        public Guid ProfileId { get; set; }

        [Column("item_id")]
        public int ItemId { get; set; }

        [Column("quantity")]
        public int Quantity { get; set; }
    }
}