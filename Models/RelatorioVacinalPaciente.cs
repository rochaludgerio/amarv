
namespace maui_amarv.Models
{
    public class RelatorioVacinalPaciente : Relatorio
    {
        public int PacienteId { get; set; }

        public string StatusVacinal { get; set; } = string.Empty;

        public Relatorio GerarRelatorioVacinalPaciente(
            Paciente paciente,
            Pessoa pessoa)
        {
            if (paciente == null)
                throw new ArgumentNullException(nameof(paciente));

            if (pessoa == null)
                throw new ArgumentNullException(nameof(pessoa));

            DataGeracao = DateTime.Now;
            PacienteId = paciente.Id;
            StatusVacinal = "Relatório individual gerado";

            Conteudo =
                "Relatório vacinal do paciente: " + pessoa.Nome;

            return this;
        }
    }
}
