namespace maui_amarv.Models
{
    public class RelatorioPacientesEmSituacaoRegular : Relatorio
    {
        public int QuantidadePacientes { get; set; }

        public Relatorio GerarRelatorioSituacaoRegular(int quantidade)
        {
            DataGeracao = DateTime.Now;
            QuantidadePacientes = quantidade;
            Conteudo = "Relatório de pacientes em situação regular. Total: " + quantidade;

            return this;
        }
    }
}