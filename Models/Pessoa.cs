using SQLite;

namespace maui_amarv.Models
{
    [Table("Pessoa")]
    public class Pessoa
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [NotNull]
        public string Nome { get; set; } = string.Empty;

        public string Cpf { get; set; } = string.Empty;

        public string Nome_da_Mae { get; set; } = string.Empty;

        public string Telefone { get; set; } = string.Empty;

        public string Endereco { get; set; } = string.Empty;
    }
}