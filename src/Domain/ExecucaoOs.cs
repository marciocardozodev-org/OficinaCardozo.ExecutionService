using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OFICINACARDOZO.EXECUTIONSERVICE.Domain
{
    [Table("execucao_os")]
    public class ExecucaoOs
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }
        [Column("ordem_servico_id")]
        public int OrdemServicoId { get; set; }
        [Column("status_atual")]
        public string StatusAtual { get; set; } = string.Empty;
        [Column("inicio_execucao")]
        public DateTime? InicioExecucao { get; set; }
        [Column("fim_execucao")]
        public DateTime? FimExecucao { get; set; }
        [Column("diagnostico")] 
        public string? Diagnostico { get; set; }
        [Column("reparo")]
        public string? Reparo { get; set; }
        [Column("finalizado")]
        public bool Finalizado { get; set; }
    }
}
