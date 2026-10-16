using SQLite;

namespace maui_amarv.Models
{
    public class CondicaoClinica
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public int PacienteId { get; set; }

        public string Descricao { get; set; }

        public bool Alergia { get; set; }

        public bool Imunodeprimido { get; set; }

        public string Observacao { get; set; }

        public void RegistrarCondicao()
        {
        }

        public void AtualizarCondicao()
        {
        }

        public bool VerificarContraindicacao()
        {
            if (Alergia || Imunodeprimido)
                return true;

            if (!string.IsNullOrWhiteSpace(Descricao) &&
                Descricao.ToLower() != "nenhuma")
                return true;

            return false;
        }
    }
}