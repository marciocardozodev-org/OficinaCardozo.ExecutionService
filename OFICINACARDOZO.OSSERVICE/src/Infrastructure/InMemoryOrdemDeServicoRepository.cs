using System;
using System.Collections.Generic;
using System.Linq;
using OFICINACARDOZO.OSSERVICE.Domain;

namespace OFICINACARDOZO.OSSERVICE.Infrastructure
{
    public class InMemoryOrdemDeServicoRepository
    {
        private readonly List<OrdemDeServico> _ordens = new();

        public void Add(OrdemDeServico ordem)
        {
            _ordens.Add(ordem);
        }

        public IEnumerable<OrdemDeServico> GetAll()
        {
            return _ordens;
        }

        public OrdemDeServico GetById(Guid id)
        {
            return _ordens.FirstOrDefault(o => o.Id == id);
        }
    }
}
