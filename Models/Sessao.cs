using SQLite;

namespace maui_amarv.Models
{
    public class Sessao
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string Token { get; set; }

        public DateTime DataLogin { get; set; }

        public bool Ativa { get; set; }

        public bool IniciarSessao()
        {
            Token = Guid.NewGuid().ToString();
            DataLogin = DateTime.Now;
            Ativa = true;

            return Ativa;
        }

        public bool ValidarSessao()
        {
            return Ativa;
        }

        public void EncerrarSessao()
        {
            Ativa = false;
        }
    }
}