using System.ComponentModel.DataAnnotations.Schema;
using System;

namespace OFICINACARDOZO.OSSERVICE.Domain
{
    public enum StatusOrdemServico
    {
        Aberta,
        EmAndamento,
        Finalizada,
        Cancelada
    }

    [Table("OFICINA_ORDEM_SERVICO")]
    public class OrdemDeServico
    {
        [Column("ID")]
        public int Id { get; set; }

        [Column("DATA_SOLICITACAO")]
        public DateTime DataSolicitacao { get; set; }

        [Column("ID_VEICULO")]
        public int IdVeiculo { get; set; }

        [Column("ID_STATUS")]
        public int IdStatus { get; set; }

        [Column("DATA_FINALIZACAO")]
        public DateTime? DataFinalizacao { get; set; }

        [Column("DATA_ENTREGA")]
        public DateTime? DataEntrega { get; set; }

        public OrdemDeServico() { }

        public OrdemDeServico(DateTime dataSolicitacao, int idVeiculo, int idStatus)
        {
            DataSolicitacao = dataSolicitacao;
            IdVeiculo = idVeiculo;
            IdStatus = idStatus;
        }
    }
}
