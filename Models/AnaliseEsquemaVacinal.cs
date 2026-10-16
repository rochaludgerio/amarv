using SQLite;

namespace maui_amarv.Models
{
    public class AnaliseEsquemaVacinal
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public int PacienteId { get; set; }

        public string NomePaciente { get; set; } = string.Empty;

        public string Resultado { get; set; } = string.Empty;

        public DateTime DataAnalise { get; set; }

        public string Status { get; set; } = string.Empty;
    }
}