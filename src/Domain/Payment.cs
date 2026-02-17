using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OFICINACARDOZO.EXECUTIONSERVICE.Domain
{
    public enum StatusPagamento { Pendente, Confirmado, Falhou }
    [Table("pagamento")]
    public class Pagamento
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }
        [Column("ordem_servico_id")]
        public int OrdemServicoId { get; set; }
        [Column("valor")]
        public decimal Valor { get; set; }
        [Column("metodo")]
        public string Metodo { get; set; } = string.Empty;
        [Column("status")]
        public StatusPagamento Status { get; set; }
        [Column("criado_em")]
        public DateTime CriadoEm { get; set; }
    }
}