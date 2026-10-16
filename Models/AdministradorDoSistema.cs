
using SQLite;

namespace maui_amarv.Models
{
    [Table("AdministradorDoSistema")]
    public class AdministradorDoSistema
    {
        // Mesmo Id do UsuarioDoSistema correspondente.
        [PrimaryKey]
        public int Id { get; set; }

        public string NivelAcesso { get; set; } = string.Empty;

        public void GerenciarUsuarios()
        {
        }

        public void AtualizarVacinasEDoses()
        {
        }

        public void AtualizarCalendarioSUS()
        {
        }

        public void AtualizarCalendarioRedePrivada()
        {
        }

        public void RealizarBackup()
        {
        }
    }
}
