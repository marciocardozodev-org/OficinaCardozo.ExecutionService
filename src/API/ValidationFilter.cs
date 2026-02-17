using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Linq;

namespace OFICINACARDOZO.EXECUTIONSERVICE.API
{
    public class ValidationFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.ModelState.IsValid)
            {
                var erros = context.ModelState
                    .Where(e => e.Value.Errors.Count > 0)
                    .Select(e => new { Campo = e.Key, Erros = e.Value.Errors.Select(x => x.ErrorMessage) });
                context.Result = new BadRequestObjectResult(new
                {
                    Erro = "Erro de validação.",
                    Detalhe = erros
                });
            }
        }

        public void OnActionExecuted(ActionExecutedContext context) { }
    }
}
