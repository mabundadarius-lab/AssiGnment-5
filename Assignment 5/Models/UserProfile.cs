using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using ColumnAttribute = Supabase.Postgrest.Attributes.ColumnAttribute;
using TableAttribute = Supabase.Postgrest.Attributes.TableAttribute;

namespace AssiGnment_5.Models
{
    [Table("profiles")]
    public class UserProfile : BaseModel
    {
        [PrimaryKey("id", false)]
        public Guid Id { get; set; }

        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("surname")]
        public string Surname { get; set; } = string.Empty;

        [Column("email")]                       
        public string EmailAddress { get; set; } = string.Empty;

        [Column("bio")]
        public string Bio { get; set; } = string.Empty;

        [Column("profile_icon_path")]              // ← confirmed from your screenshot
        public string ProfileIconPath { get; set; } = string.Empty;
    }
}
