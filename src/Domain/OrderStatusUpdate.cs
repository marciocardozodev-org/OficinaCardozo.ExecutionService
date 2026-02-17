using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OFICINACARDOZO.EXECUTIONSERVICE.Domain
{
    [Table("atualizacao_status_os")]
    public class AtualizacaoStatusOs
    {
        [Key]
        [Column("id")]
        public int Id { get; set; } // Chave primária
        [Column("ordem_servico_id")]
        public int OrdemServicoId { get; set; }
        [Column("novo_status")]
        public string NovoStatus { get; set; } = string.Empty;
        [Column("atualizado_em")]
        public DateTime AtualizadoEm { get; set; }
    }
}