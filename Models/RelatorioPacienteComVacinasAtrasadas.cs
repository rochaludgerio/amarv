namespace maui_amarv.Models
{
    public class RelatorioPacienteComVacinasAtrasadas : Relatorio
    {
        public int QuantidadePacientes { get; set; }

        public Relatorio GerarRelatorioPacientesAtrasados(int quantidade)
        {
            DataGeracao = DateTime.Now;
            QuantidadePacientes = quantidade;
            Conteudo = "Relatório de pacientes com vacinas atrasadas. Total: " + quantidade;

            return this;
        }
    }
}