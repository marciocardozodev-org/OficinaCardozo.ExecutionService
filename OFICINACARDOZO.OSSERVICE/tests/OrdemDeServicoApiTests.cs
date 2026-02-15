using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using OFICINACARDOZO.OSSERVICE.Domain;
using Xunit;

namespace OFICINACARDOZO.OSSERVICE.Tests
{
    public class OrdemDeServicoApiTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public OrdemDeServicoApiTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        private async Task<string> GetJwtTokenAsync(HttpClient client)
        {
            var login = new { Username = "admin", Password = "123456" };
            var response = await client.PostAsJsonAsync("/api/Auth/login", login);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            return json.GetProperty("token").GetString();
        }

        private void AddJwt(HttpClient client, string token)
        {
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        [Fact]
        public async Task Post_DeveCriarOrdemComDescricaoValida()
        {
            var client = _factory.CreateClient();
            var token = await GetJwtTokenAsync(client);
            AddJwt(client, token);
            var response = await client.PostAsJsonAsync("/api/OrdemDeServico", "Nova OS");
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var ordem = await response.Content.ReadFromJsonAsync<OrdemDeServico>();
            Assert.NotNull(ordem);
            Assert.Equal("Nova OS", ordem.Descricao);
        }

        [Fact]
        public async Task Post_DeveRetornarBadRequest_DescricaoVazia()
        {
            var client = _factory.CreateClient();
            var token = await GetJwtTokenAsync(client);
            AddJwt(client, token);
            var response = await client.PostAsJsonAsync("/api/OrdemDeServico", "");
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Get_DeveListarOrdens()
        {
            var client = _factory.CreateClient();
            var token = await GetJwtTokenAsync(client);
            AddJwt(client, token);
            await client.PostAsJsonAsync("/api/OrdemDeServico", "OS Teste");
            var response = await client.GetAsync("/api/OrdemDeServico");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var lista = await response.Content.ReadFromJsonAsync<OrdemDeServico[]>();
            Assert.NotNull(lista);
            Assert.Contains(lista, o => o.Descricao == "OS Teste");
        }
    }
}
