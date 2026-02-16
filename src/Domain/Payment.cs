namespace OFICINACARDOZO.BILLINGSERVICE.Domain
{
    public enum StatusPagamento { Pendente, Confirmado, Falhou }
    public class Pagamento
    {
        public int Id { get; set; }
        public int OrdemServicoId { get; set; }
        public decimal Valor { get; set; }
        public string Metodo { get; set; } = string.Empty;
        public StatusPagamento Status { get; set; }
        public DateTime CriadoEm { get; set; }
    }
}