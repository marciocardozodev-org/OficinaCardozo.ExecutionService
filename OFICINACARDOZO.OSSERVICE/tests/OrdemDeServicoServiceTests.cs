using System.Linq;
using OFICINACARDOZO.OSSERVICE.Application;
using Xunit;

namespace OFICINACARDOZO.OSSERVICE.Tests
{
    public class OrdemDeServicoServiceTests
    {
        [Fact]
        public void Criar_DeveAdicionarOrdemNaLista()
        {
            var service = new OrdemDeServicoService();
            var ordem = service.Criar("Teste de OS");
            var todas = service.Listar();
            Assert.Contains(ordem, todas);
        }

        [Fact]
        public void Listar_DeveRetornarTodasOrdens()
        {
            var service = new OrdemDeServicoService();
            service.Criar("OS 1");
            service.Criar("OS 2");
            var todas = service.Listar().ToList();
            Assert.Equal(2, todas.Count);
        }
    }
}
