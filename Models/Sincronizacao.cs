using SQLite;

namespace maui_amarv.Models
{
    public class Sincronizacao
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public DateTime DataSincronizacao { get; set; }

        public string Status { get; set; }

        public int QuantidadeRegistros { get; set; }

        public bool VerificarConexao()
        {
            return true;
        }

        public bool VerificarPendencias()
        {
            return QuantidadeRegistros > 0;
        }

        public void SincronizarDados()
        {
            DataSincronizacao = DateTime.Now;
            Status = "Sincronizado";
        }
    }
}