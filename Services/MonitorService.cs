using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using TestAPI.Models;

namespace TestAPI.Services
{
    public class MonitorService
    {
        // BOA PRÁTICA: HttpClient único e estático evita o esgotamento de portas (Socket Exhaustion).
        // No Java, você veria um comportamento similar ao gerenciar conexões com o OkHttpClient ou RestTemplate como Singletons.
        // SECURITY: redirecionamento automático DESLIGADO. Senão uma URL pública validada
        // responderia "302 -> http://169.254.169.254/" e o HttpClient seguiria sozinho,
        // furando o ValidadorDeAlvo. Um 3xx já prova que o servidor está no ar.
        private static readonly HttpClient _clienteHttp = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });

        public async Task<List<ApiFoco>> ExecutarVarreduraAsync(List<ApiFoco> listaApis)
        {
            if (listaApis == null || listaApis.Count == 0)
            {
                return listaApis ?? new List<ApiFoco>();
            }

            // Lista que agrupa as tarefas que rodarão em paralelo
            var tarefasDeTeste = new List<Task>();

            foreach (var api in listaApis)
            {
                api.Status = StatusSemaforo.Processando;
                api.MensagemRetorno = "Disparando requisição...";

                // Joga a tarefa na esteira de execução concorrente
                tarefasDeTeste.Add(TestarApiIndividualAsync(api));
            }

            // ===== O PULO DO GATO DO ASSINCRONISMO =====
            // Aguarda todas as tarefas da lista finalizarem em paralelo.
            // Em Java, o equivalente moderno seria usar o CompletableFuture.allOf().
            await Task.WhenAll(tarefasDeTeste);

            return listaApis;
        }

        private async Task TestarApiIndividualAsync(ApiFoco api)
        {
            // RECO DE SECURITY & RESILIÊNCIA: Token de cancelamento (Timeout de 4 segundos)
            // Impede que requisições presas derrubem ou gerem negação de serviço (DoS) no nosso próprio servidor.
            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(4)))
            {
                try
                {
                    // ===== CORREÇÃO DE BUG CRÍTICO (Race Condition) =====
                    // Modificar '_clienteHttp.DefaultRequestHeaders' em métodos paralelos causa bugs de concorrência,
                    // pois uma tarefa limpa o cabeçalho da outra no meio da requisição.
                    // BOA PRÁTICA: Usar 'HttpRequestMessage' para configurar cabeçalhos isolados por requisição!
                    using (var requisicao = new HttpRequestMessage(HttpMethod.Get, api.Url))
                    {
                        requisicao.Headers.UserAgent.Clear();
                        requisicao.Headers.UserAgent.ParseAdd("TestAPI-Agent");

                        // Dispara a requisição HTTP passando o token de segurança
                        using (HttpResponseMessage resposta = await _clienteHttp.SendAsync(requisicao, cts.Token))
                        {
                            int codigo = (int)resposta.StatusCode;
                            if (resposta.IsSuccessStatusCode)
                            {
                                api.Status = StatusSemaforo.Aberta;
                                api.MensagemRetorno = $"Online. Código de resposta: {codigo}";
                            }
                            else if (codigo >= 300 && codigo < 400)
                            {
                                api.Status = StatusSemaforo.Aberta;
                                api.MensagemRetorno = $"Online. Redireciona (código {codigo}) para {resposta.Headers.Location}";
                            }
                            else
                            {
                                api.Status = StatusSemaforo.Fechada;
                                api.MensagemRetorno = $"Erro no Servidor. Código: {(int)resposta.StatusCode}";
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Captura o estouro dos 4 segundos de tolerância
                    api.Status = StatusSemaforo.Fechada;
                    api.MensagemRetorno = "Tempo limite esgotado (Timeout). API muito lenta ou inacessível.";
                }
                catch (Exception ex)
                {
                    // Captura falhas físicas (DNS fora, internet caida, queda de handshake SSL)
                    api.Status = StatusSemaforo.Fechada;
                    api.MensagemRetorno = $"Falha de conexão física: {ex.Message}";
                }
            }
        }
    }
}