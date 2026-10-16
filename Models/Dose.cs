using SQLite;

namespace maui_amarv.Models
{
    public class Dose
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public int VacinaId { get; set; }

        public int NumeroDose { get; set; }

        public int IntervaloDias { get; set; }

        public string FaixaEtaria { get; set; }

        public bool Reforco { get; set; }

        public bool ValidarDose()
        {
            return NumeroDose > 0;
        }

        public int CalcularIntervalo()
        {
            return IntervaloDias;
        }

        public bool VerificarFaixaEtaria(int idadeMeses)
        {
            return true;
        }
    }
}