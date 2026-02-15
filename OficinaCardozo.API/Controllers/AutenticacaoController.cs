using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OficinaCardozo.Application.DTOs;
using OficinaCardozo.Application.Services;
using System.Security.Claims;

namespace OficinaCardozo.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AutenticacaoController : ControllerBase
{
    // comentario de teste apresentação
    private readonly ILogger<AutenticacaoController> _logger;
    private readonly IAutenticacaoService _autenticacaoService;
    //comentario de teste apresentação
    public AutenticacaoController(IAutenticacaoService autenticacaoService, ILogger<AutenticacaoController> logger)
    {
        _autenticacaoService = autenticacaoService;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<ActionResult<TokenRespostaDto>> Login([FromBody] LoginDto loginDto)
    {
        _logger.LogInformation("Login endpoint chamado para usuário {User}", loginDto?.NomeUsuario);
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var resultado = await _autenticacaoService.FazerLoginAsync(loginDto);
            _logger.LogInformation("Login realizado com sucesso para usuário {User}", loginDto?.NomeUsuario);
            return Ok(resultado);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Login não autorizado para usuário {User}", loginDto?.NomeUsuario);
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro interno no login para usuário {User}", loginDto?.NomeUsuario);
            return StatusCode(500, new { message = "Erro interno do servidor", details = ex.Message });
        }
    }

    [HttpPost("login-cpf")]
    public async Task<ActionResult<TokenRespostaDto>> LoginPorCpf([FromBody] LoginCpfDto loginCpfDto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var resultado = await _autenticacaoService.FazerLoginPorCpfAsync(loginCpfDto);
            return Ok(resultado);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erro interno do servidor", details = ex.Message });
        }
    }

    
    [HttpPost("registro")]
    public async Task<ActionResult<UsuarioDto>> Registro([FromBody] CriarUsuarioDto criarUsuarioDto)
    {
        _logger.LogInformation("Registro endpoint chamado para email {Email}", criarUsuarioDto?.Email);
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var resultado = await _autenticacaoService.CriarUsuarioAsync(criarUsuarioDto);
            _logger.LogInformation("Usuário registrado com sucesso: {Email}", criarUsuarioDto?.Email);
            return CreatedAtAction(nameof(ObterPerfil), new { }, resultado);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Falha de operação no registro para email {Email}", criarUsuarioDto?.Email);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro interno no registro para email {Email}", criarUsuarioDto?.Email);
            return StatusCode(500, new { message = "Erro interno do servidor", details = ex.Message });
        }
    }

   
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IEnumerable<UsuarioDto>>> GetAll()
    {
        try
        {
            var usuarios = await _autenticacaoService.ObterTodosUsuariosAsync();
            return Ok(usuarios);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erro interno do servidor", details = ex.Message });
        }
    }

   
    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<UsuarioDto>> GetById(int id)
    {
        try
        {
            var usuario = await _autenticacaoService.ObterUsuarioPorIdAsync(id);
            if (usuario == null)
                return NotFound(new { message = "Usu�rio n�o encontrado" });

            return Ok(usuario);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erro interno do servidor", details = ex.Message });
        }
    }

    [HttpGet("perfil")]
    [Authorize]
    public ActionResult<object> ObterPerfil()
    {
        try
        {
            var idUsuario = User.FindFirst("idUsuario")?.Value;
            var nomeUsuario = User.FindFirst("nomeUsuario")?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value;

            return Ok(new
            {
                idUsuario,
                nomeUsuario,
                email,
                claims = User.Claims.Select(c => new { c.Type, c.Value }),
                mensagem = "Autentica��o JWT funcionando."
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erro interno do servidor", details = ex.Message });
        }
    }

    [HttpPost("validar-token")]
    public async Task<ActionResult<bool>> ValidarToken([FromBody] string token)
    {
        try
        {
            var ehValido = await _autenticacaoService.ValidarTokenAsync(token);
            return Ok(new { ehValido, mensagem = ehValido ? "Token v�lido" : "Token inv�lido" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erro interno do servidor", details = ex.Message });
        }
    }
}