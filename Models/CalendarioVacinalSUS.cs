namespace maui_amarv.Models
{
    public class CalendarioVacinalSUS : CalendarioVacinal
    {
        public string FonteOficial { get; set; }

        public List<CalendarioVacinalSUS> ConsultarCalendarioSUS()
        {
            return new List<CalendarioVacinalSUS>();
        }

        public void AtualizarCalendarioSUS()
        {
        }
    }
}