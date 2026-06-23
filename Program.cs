using System.IO; // Necessário para o Directory.GetCurrentDirectory()
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
        // Método estático principal - O primeiro a ser executado pelo sistema operacional
        public static void Main(string[] args)
        {
            // Constrói o Host configurado e inicia o servidor Web para escutar requisições HTTP
            CreateHostBuilder(args).Build().Run();
        }

        /// <summary>
        /// Configura os padrões do Host Web da aplicação (Logs, variáveis de ambiente e o servidor web Kestrel).
        /// </summary>
        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    // ===== RESOLUÇÃO DE DIRETÓRIO PARA DOCKER (PRODUÇÃO) =====
                    // Força o .NET a usar o diretório atual de execução como raiz do conteúdo.
                    // Isso impede que o Kestrel fique cego e perca a pasta wwwroot de vista no Linux.
                    webBuilder.UseContentRoot(Directory.GetCurrentDirectory());

                    // No ambiente isolado do Docker, travamos o Kestrel na porta interna 5000.
                    webBuilder.UseUrls("http://*:5000");

                    // Vincula a classe Startup para desenhar a esteira de Middlewares
                    webBuilder.UseStartup<Startup>();
                });
        }
    }
}