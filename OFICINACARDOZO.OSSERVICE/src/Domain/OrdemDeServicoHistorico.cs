using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace OFICINACARDOZO.OSSERVICE.Domain
{
    [Table("OFICINA_ORDEM_SERVICO_HISTORICO")]
    public class OrdemDeServicoHistorico
    {
        [Column("ID")]
        public int Id { get; set; }

        [Column("ID_ORDEM_SERVICO")]
        public int IdOrdemServico { get; set; }

        [Column("ID_STATUS")]
        public int IdStatus { get; set; }

        [Column("DATA_ALTERACAO")]
        public DateTime DataAlteracao { get; set; }
    }
}
