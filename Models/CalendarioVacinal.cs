using SQLite;

namespace maui_amarv.Models
{
    public class CalendarioVacinal
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string FaixaEtaria { get; set; }

        public string VacinaIndicada { get; set; }

        public string DoseRecomendada { get; set; }

        public virtual List<CalendarioVacinal> ConsultarCalendario()
        {
            return new List<CalendarioVacinal>();
        }

        public virtual List<string> ListarVacinasDisponiveis()
        {
            return new List<string>();
        }
    }
}