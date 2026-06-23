using System;

namespace TestAPI.Models
{
    /// <summary>
    /// Representa a entidade da API que será monitorada.
    /// Contém regras de validação estritas no Setter para garantir a segurança da aplicação.
    /// </summary>
    public class ApiFoco
    {
        private string _nome;
        private string _url;

        // ===== CAMPO: NOME =====
        public string Nome 
        { 
            get => _nome; 
            set
            {
                // DEFESA: Impede que o nome da API venha vazio ou nulo
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentException("O nome da API não pode estar vazio.");
                }
                _nome = value.Trim();
            }
        }
        
        // ===== CAMPO: URL (SESSÃO SECURITY) =====
        public string Url
        {
            get => _url;
            set
            {
                // DEFESA 1: Validação de Nulidade precoce (Fail-Fast)
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentException("A URL da API não pode estar vazia.");
                }

                // SANITIZAÇÃO: Remove espaços invisíveis nas pontas
                string urlLimpa = value.Trim();

                // DEFESA 2: Proteção contra SSRF (Server-Side Request Forgery)
                // Impede que o atacante use nosso servidor para escanear a rede interna (ex: localhost, loops de IP)
                if (urlLimpa.Contains("localhost") || 
                    urlLimpa.Contains("127.0.0.1") || 
                    urlLimpa.StartsWith("http://[::1]") || 
                    urlLimpa.Contains("0.0.0.0"))
                {
                    throw new ArgumentException("Por motivos de segurança, não é permitido testar endereços locais ou de loopback.");
                }

                // DEFESA 3: Validação estrita de formato e protocolo
                // Garante que é uma URL web absoluta bem formada e limita apenas a HTTP/HTTPS (bloqueia caminhos locais file:// ou scripts)
                bool urlValida = Uri.TryCreate(urlLimpa, UriKind.Absolute, out Uri uriResult) 
                                 && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

                if (!urlValida)
                {
                    throw new ArgumentException("A URL fornecida é inválida ou usa um protocolo inseguro. Use apenas HTTP ou HTTPS.");
                }

                // Armazena o valor limpo e validado
                _url = urlLimpa;
            }
        }

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