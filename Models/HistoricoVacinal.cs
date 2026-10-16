using SQLite;

namespace maui_amarv.Models
{
    public class HistoricoVacinal
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public int PacienteId { get; set; }

        public string NomePaciente { get; set; }

        public int RegraVacinalId { get; set; }

        public string NomeVacina { get; set; }

        public int DosesTomadas { get; set; }

        public DateTime DataAplicacao { get; set; }

        public DateTime DataUltimaAplicacao { get; set; }

        public DateTime DataRegistro { get; set; }

        public string LocalAplicacao { get; set; }

        public string Lote { get; set; }

        public string ProfissionalResponsavel { get; set; }

        public string Observacao { get; set; }

        public string Resumo
        {
            get
            {
                return NomeVacina + " - " + DosesTomadas + " dose(s) - Última aplicação: " + DataUltimaAplicacao.ToString("dd/MM/yyyy");
            }
        }

        public void RegistrarVacinacao()
        {
            DataRegistro = DateTime.Now;
        }

        public List<HistoricoVacinal> ConsultarHistorico()
        {
            return new List<HistoricoVacinal>();
        }

        public void AtualizarHistorico()
        {
        }
    }
}