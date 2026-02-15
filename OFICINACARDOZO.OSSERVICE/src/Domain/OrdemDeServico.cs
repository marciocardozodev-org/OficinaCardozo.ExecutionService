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

    public class OrdemDeServico
    {
        public Guid Id { get; set; }
        public string Descricao { get; set; }
        public DateTime DataCriacao { get; set; }
        public StatusOrdemServico Status { get; set; }

        public OrdemDeServico(string descricao)
        {
            Id = Guid.NewGuid();
            Descricao = descricao;
            DataCriacao = DateTime.UtcNow;
            Status = StatusOrdemServico.Aberta;
        }

        public void AlterarStatus(StatusOrdemServico novoStatus)
        {
            Status = novoStatus;
        }
    }
}
