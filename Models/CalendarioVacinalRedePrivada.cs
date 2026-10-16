namespace maui_amarv.Models
{
    public class CalendarioVacinalRedePrivada : CalendarioVacinal
    {
        public string RedePrivada { get; set; }

        public List<CalendarioVacinalRedePrivada> ConsultarCalendarioPrivado()
        {
            return new List<CalendarioVacinalRedePrivada>();
        }

        public void AtualizarCalendarioPrivado()
        {
        }
    }
}