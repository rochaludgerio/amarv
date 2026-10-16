using SQLite;

namespace maui_amarv.Models
{
    public class Backup
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public DateTime DataBackup { get; set; }

        public string Status { get; set; }

        public string LocalArquivo { get; set; }

        public void RealizarBackup()
        {
            DataBackup = DateTime.Now;
            Status = "Backup realizado";
        }

        public void RestaurarBackup()
        {
            Status = "Backup restaurado";
        }

        public bool ValidarBackup()
        {
            return !string.IsNullOrWhiteSpace(LocalArquivo);
        }
    }
}