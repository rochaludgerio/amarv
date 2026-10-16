
using SQLite;

namespace maui_amarv.Models
{
    public class ColunaSQLite
    {
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("type")]
        public string Type { get; set; } = string.Empty;

        [Column("pk")]
        public int PrimaryKey { get; set; }
    }
}
