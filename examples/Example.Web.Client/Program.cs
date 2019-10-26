using System;
using System.Net.Http;
using System.Threading.Tasks;
using Zyborg.NegotiatedToken.Client;

namespace Example.Web.Client
{
    class Program
    {
        static async Task Main(string[] args)
        {
            using var handler = new HttpClientHandler();
            handler.UseDefaultCredentials = true;
            handler.ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

            using var ntHandler = new NegotiatedTokenHandler("/token", handler);
            using var client = new HttpClient(ntHandler)
            {
                BaseAddress = new Uri("https://localhost:5001"),
            };

            using (var resp = await client.GetAsync(new Uri("/unprotected", UriKind.RelativeOrAbsolute)))
            {
                Console.WriteLine("UNPROTECTED:");
                resp.EnsureSuccessStatusCode();
                Console.WriteLine(await resp.Content.ReadAsStringAsync());
                Console.WriteLine();
            }

            using (var resp = await client.GetAsync("/protected"))
            {
                Console.WriteLine("PROTECTED:");
                resp.EnsureSuccessStatusCode();
                Console.WriteLine(await resp.Content.ReadAsStringAsync());
                Console.WriteLine();
            }
        }
    }
}
