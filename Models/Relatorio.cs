using SQLite;

namespace maui_amarv.Models
{
    public class Relatorio
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public DateTime DataGeracao { get; set; }

        public string Periodo { get; set; }

        public string Conteudo { get; set; }

        public virtual void ExportarPDF()
        {
        }

        public virtual void ImprimirRelatorio()
        {
        }
    }
}