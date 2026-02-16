namespace OFICINACARDOZO.BILLINGSERVICE.Domain
{
    public class AtualizacaoStatusOs
    {
        public int OrdemServicoId { get; set; }
        public string NovoStatus { get; set; } = string.Empty;
        public DateTime AtualizadoEm { get; set; }
    }
}