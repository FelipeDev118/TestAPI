using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TestAPI.Models;
using TestAPI.Services;

namespace TestAPI.Controllers
{
    /// <summary>
    /// Expõe os endpoints HTTP para o monitoramento de APIs.
    /// Herda de ControllerBase, ideal para APIs REST puras (sem suporte a Views HTML).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")] // Rota automática baseada no nome: api/monitor
    public class MonitorController : ControllerBase
    {
        // BOA PRÁTICA: Usamos a injeção do serviço via campo privado.
        // Evita dar 'new MonitorService()' dentro do método, desacoplando o código.
        private readonly MonitorService _monitorService;

        // O container de DI do ASP.NET Core entrega a instância registrada no Startup
        public MonitorController(MonitorService monitorService)
        {
            _monitorService = monitorService;
        }

        /// <summary>
        /// Recebe uma URL dinâmica informada pelo usuário, aplica validações de segurança
        /// e executa a varredura assíncrona de conectividade.
        /// </summary>
        /// <param name="apiDigitada">Objeto ApiFoco recebido e desserializado automaticamente do corpo do JSON.</param>
        /// <returns>Objeto ApiFoco atualizado com o status do semáforo.</returns>
        [HttpPost] // Alterado para POST: Criação/Envio de dados sob demanda
        public async Task<ActionResult<ApiFoco>> TestarApiDinamica([FromBody] ApiFoco apiDigitada)
        {
            // Validação defensiva rápida para checar se o payload JSON veio nulo
            if (apiDigitada == null)
            {
                return BadRequest("Os dados enviados estão inválidos ou corrompidos.");
            }

            try
            {
                // NOTA DE SECURITY: a validação roda AQUI, dentro do try, e não no setter do modelo.
                // No setter ela disparava durante a desserialização do JSON, fora deste try/catch,
                // e o usuário recebia HTTP 500 vazio em vez do 400 com o motivo.
                await ValidadorDeAlvo.ValidarAsync(apiDigitada);

                // O nosso serviço espera uma estrutura de lista, encapsulamos o objeto único
                var listaTemporaria = new List<ApiFoco> { apiDigitada };

                // Dispara o motor assíncrono em paralelo
                var resultado = await _monitorService.ExecutarVarreduraAsync(listaTemporaria);

                // Retorna HTTP 200 OK com o primeiro elemento da lista (nossa API testada)
                return Ok(resultado[0]);
            }
            catch (ArgumentException ex)
            {
                // Captura violações das regras de negócio/security (como validação de URL do Model)
                // Retorna HTTP 400 Bad Request com o motivo exato da rejeição
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                // Captura falhas inesperadas de infraestrutura interna do servidor
                // Retorna HTTP 500 Internal Server Error protegendo detalhes confidenciais da StackTrace
                return StatusCode(500, $"Erro interno ao processar a varredura: {ex.Message}");
            }
        }
    }
}