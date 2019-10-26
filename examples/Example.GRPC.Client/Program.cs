using Grpc.Net.Client;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Zyborg.NegotiatedToken.Client;

namespace Example.GRPC.Client
{
    class Program
    {
        static async Task Main()
        {
            using var httpHandler = new HttpClientHandler()
            {
                // For local dev and testing, we just use an untrusted
                // self-signed cert so ignore those errors
                ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,

                // We need to let the client provide our
                // default Win Auth credentials if prompted
                UseDefaultCredentials = true,
            };
            using var negtokHandler = new NegotiatedTokenHandler("/token", httpHandler);
            using var httpClient = new HttpClient(negtokHandler);

            var grpcChannelOptions = new GrpcChannelOptions
            {
                HttpClient = httpClient,
                DisposeHttpClient = false,
            };

            using var channel = GrpcChannel.ForAddress("https://localhost:5001", grpcChannelOptions);
            var client = new Greeter.GreeterClient(channel);

            var helloWorld = await client.SayHelloAsync(
                new HelloRequest { Name = "World" });

            Console.WriteLine(helloWorld.Message);

            var unprotectedReply = await client.GetUnprotectedDetailsAsync(
                new DetailsInput { KeepGroupSid = false });
            Console.WriteLine("UNPROTECTED:");
            Console.WriteLine(unprotectedReply.Details);
            Console.WriteLine();

            var protectedReply = await client.GetProtectedDetailsAsync(
                new DetailsInput { KeepGroupSid = false });
            Console.WriteLine("PROTECTED:");
            Console.WriteLine(protectedReply.Details);
            Console.WriteLine();
        }
    }
}
