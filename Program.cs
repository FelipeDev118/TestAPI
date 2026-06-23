using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace TestAPI
{
    /// <summary>
    /// Ponto de entrada (Entry Point) principal da aplicação.
    /// Responsável por construir a infraestrutura do servidor Kestrel e iniciar o processo.
    /// </summary>
    public class Program
    {
        // Método estático principal - O primeiro a ser executado pelo sistema operacional (Igual ao Java public static void main)
        public static void Main(string[] args)
        {
            // Constrói o Host configurado e inicia o servidor Web para escutar requisições HTTP
            CreateHostBuilder(args).Build().Run();
        }

        /// <summary>
        /// Configura os padrões do Host Web da aplicação (Logs, variáveis de ambiente e o servidor web Kestrel).
        /// Equivalente à inicialização oculta do SpringApplication.run() do Java.
        /// </summary>
        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    // ===== AJUSTE PARA CONTAINERIZAÇÃO (DOCKER) =====
                    // No ambiente isolado do Docker, travamos o Kestrel na porta interna 5000.
                    // O Dockerfile expõe essa porta e a Render faz o mapeamento do tráfego externo para ela.
                    webBuilder.UseUrls("http://*:5000");

                    // Vincula a classe Startup para desenhar a esteira de Middlewares
                    webBuilder.UseStartup<Startup>();
                });
        }
    }
}