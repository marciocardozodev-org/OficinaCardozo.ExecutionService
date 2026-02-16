namespace OFICINACARDOZO.BILLINGSERVICE.Domain
{
    public enum StatusOrcamento { Pendente, Enviado, Aprovado, Rejeitado }
    public class Orcamento
    {
        public int Id { get; set; }
        public int OrdemServicoId { get; set; }
        public decimal Valor { get; set; }
        public string EmailCliente { get; set; } = string.Empty;
        public StatusOrcamento Status { get; set; }
        public DateTime CriadoEm { get; set; }
    }
}