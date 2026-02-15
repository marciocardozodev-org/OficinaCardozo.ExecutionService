using System;
using System.Collections.Generic;
using OFICINACARDOZO.OSSERVICE.Domain;

namespace OFICINACARDOZO.OSSERVICE.Application
{
    public class OrdemDeServicoService
    {
        private readonly List<OrdemDeServico> _ordens = new();

        public OrdemDeServico Criar(string descricao)
        {
            var ordem = new OrdemDeServico(descricao);
            _ordens.Add(ordem);
            return ordem;
        }

        public IEnumerable<OrdemDeServico> Listar()
        {
            return _ordens;
        }
    }
}
