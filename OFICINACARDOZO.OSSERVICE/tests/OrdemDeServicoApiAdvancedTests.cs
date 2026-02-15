using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using OFICINACARDOZO.OSSERVICE.Domain;
using Xunit;

namespace OFICINACARDOZO.OSSERVICE.Tests
{
    public class OrdemDeServicoApiAdvancedTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public OrdemDeServicoApiAdvancedTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        private async Task<string> GetJwtTokenAsync(HttpClient client)
        {
            var login = new { Username = "admin", Password = "123456" };
            var response = await client.PostAsJsonAsync("/api/Auth/login", login);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
            return json.GetProperty("token").GetString();
        }

        private void AddJwt(HttpClient client, string token)
        {
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        [Fact]
        public async Task Patch_DeveAlterarStatusParaFinalizada()
        {
            var client = _factory.CreateClient();
            var token = await GetJwtTokenAsync(client);
            AddJwt(client, token);
            var post = await client.PostAsJsonAsync("/api/OrdemDeServico", "OS para finalizar");
            var ordem = await post.Content.ReadFromJsonAsync<OrdemDeServico>();
            Assert.NotNull(ordem);
            var patch = await client.PatchAsJsonAsync($"/api/OrdemDeServico/{ordem.Id}/status", StatusOrdemServico.Finalizada);
            Assert.Equal(HttpStatusCode.NoContent, patch.StatusCode);
            var get = await client.GetFromJsonAsync<OrdemDeServico>($"/api/OrdemDeServico/{ordem.Id}");
            Assert.Equal(StatusOrdemServico.Finalizada, get.Status);
        }

        [Fact]
        public async Task Get_DeveFiltrarPorStatus()
        {
            var client = _factory.CreateClient();
            var token = await GetJwtTokenAsync(client);
            AddJwt(client, token);
            await client.PostAsJsonAsync("/api/OrdemDeServico", "OS Aberta");
            var post2 = await client.PostAsJsonAsync("/api/OrdemDeServico", "OS Em Andamento");
            var ordem2 = await post2.Content.ReadFromJsonAsync<OrdemDeServico>();
            await client.PatchAsJsonAsync($"/api/OrdemDeServico/{ordem2.Id}/status", StatusOrdemServico.EmAndamento);
            var response = await client.GetAsync("/api/OrdemDeServico/status/EmAndamento");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var lista = await response.Content.ReadFromJsonAsync<OrdemDeServico[]>();
            Assert.Single(lista);
            Assert.Equal(StatusOrdemServico.EmAndamento, lista[0].Status);
        }

        [Fact]
        public async Task Get_DeveFiltrarPorData()
        {
            var client = _factory.CreateClient();
            var token = await GetJwtTokenAsync(client);
            AddJwt(client, token);
            await client.PostAsJsonAsync("/api/OrdemDeServico", "OS Data Teste");
            var hoje = System.DateTime.UtcNow.ToString("yyyy-MM-dd");
            var response = await client.GetAsync($"/api/OrdemDeServico/data/{hoje}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var lista = await response.Content.ReadFromJsonAsync<OrdemDeServico[]>();
            Assert.NotEmpty(lista);
        }
    }
}
