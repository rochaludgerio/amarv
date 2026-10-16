using SQLite;

namespace maui_amarv.Models
{
    public class Vacina
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string Nome { get; set; }

        public string Fabricante { get; set; }

        public string TipoVacina { get; set; }

        public int QuantidadeDoses { get; set; }

        public string TipoCalendario { get; set; }

        public Vacina ConsultarVacina()
        {
            return this;
        }

        public void AtualizarVacina()
        {
        }

        public bool ValidarVacina()
        {
            return !string.IsNullOrWhiteSpace(Nome) && QuantidadeDoses > 0;
        }
    }
}