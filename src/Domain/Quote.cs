using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OFICINACARDOZO.EXECUTIONSERVICE.Domain
{
    public enum StatusOrcamento { Pendente, Enviado, Aprovado, Rejeitado }
    [Table("orcamento")]
    public class Orcamento
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }
        [Column("ordem_servico_id")]
        public int OrdemServicoId { get; set; }
        [Column("valor")]
        public decimal Valor { get; set; }
        [Column("email_cliente")]
        public string EmailCliente { get; set; } = string.Empty;
        [Column("status")]
        public StatusOrcamento Status { get; set; }
        [Column("criado_em")]
        public DateTime CriadoEm { get; set; }
    }
}