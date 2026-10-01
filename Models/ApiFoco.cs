namespace TestAPI.Models
{
    /// <summary>
    /// Representa a entidade da API que será monitorada.
    /// É um objeto de transporte puro: a validação mora no ValidadorDeAlvo, e não no setter.
    /// </summary>
    /// <remarks>
    /// Por que não validar no setter: o setter roda DURANTE a desserialização do JSON, antes
    /// de a requisição chegar ao controller. Uma exceção lançada ali escapa do try/catch da
    /// action e vira HTTP 500 com corpo vazio — o usuário nunca via a mensagem de segurança.
    /// </remarks>
    public class ApiFoco
    {
        public string Nome { get; set; }

        public string Url { get; set; }

        // Estado atual do semáforo da API (Valor padrão: Aguardando)
        public StatusSemaforo Status { get; set; } = StatusSemaforo.Aguardando;

        // Guarda a mensagem detalhada do retorno da varredura
        public string MensagemRetorno { get; set; } = "Pronta para varredura.";
    }

    /// <summary>
    /// Enumeração para garantir Type-Safety nos estados possíveis do Semáforo.
    /// No C#, por padrão, o primeiro item (Aguardando) recebe o valor numérico 0.
    /// </summary>
    public enum StatusSemaforo
    {
        Aguardando,   // 0
        Processando,  // 1
        Aberta,       // 2 -> Verde
        Fechada       // 3 -> Vermelho
    }
}
