using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using ColumnAttribute = Supabase.Postgrest.Attributes.ColumnAttribute;
using TableAttribute = Supabase.Postgrest.Attributes.TableAttribute;

namespace AssiGnment_5.Models
{
    [Table("shopping_items")]
    public class ShoppingItem : BaseModel
    {
        [PrimaryKey("item_id", false)]
        public int ItemId { get; set; }

        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("description")]
        public string Description { get; set; } = string.Empty;

        [Column("price")]
        public decimal Price { get; set; }

        [Column("quantity")]
        public int Quantity { get; set; }

        [Column("image_url")]
        public string ImageUrl { get; set; } = string.Empty;
    }
}