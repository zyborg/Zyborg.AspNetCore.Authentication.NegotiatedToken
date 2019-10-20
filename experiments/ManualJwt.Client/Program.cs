using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace ManualJwt.Client
{
    class Program : IDisposable
    {
        static async Task Main(string[] args)
        {
            using var prog1_1 = new Program("https://localhost:5001");
            await GetBeforeAndAfterSamples(prog1_1);

            using var prog2_0 = new Program("https://localhost:5001", new Version(2, 0));
            await GetBeforeAndAfterSamples(prog2_0);

            static async Task GetBeforeAndAfterSamples(Program prog)
            {
                Console.WriteLine("==========================");
                Console.WriteLine("Using HTTP/{0}", prog.HttpVersion);

                Console.WriteLine("  --------------------------");
                Console.WriteLine("  BEFORE Refreshing Token...");
                await GetUnprotectedAndProtectedSamples(prog);

                await prog.RefreshTokenAsync("/token");

                Console.WriteLine("  --------------------------");
                Console.WriteLine("  AFTER Refreshing Token...");
                await GetUnprotectedAndProtectedSamples(prog);
            }

            static async Task GetUnprotectedAndProtectedSamples(Program prog)
            {
                try
                {
                    Console.WriteLine("    GETTING /unprotected:");
                    Console.WriteLine("      {0}", await prog.GetAsync("/unprotected"));
                    Console.WriteLine("    GETTING /protected:");
                    Console.WriteLine("      {0}", await prog.GetAsync("/protected"));
                }
                catch (Exception ex)
                {
                    Console.WriteLine("      *** EXCEPTION: ***");
                    Console.WriteLine("      {0}", ex.Message);
                }
            }
        }

        AuthenticationHeaderValue _tokenHeader;
        HttpClientHandler _handler;
        HttpClient _client;

        public Program(string baseAddress, Version httpVersion = null)
        {
            _handler = new HttpClientHandler();
            _handler.ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

            _client = new HttpClient(_handler)
            {
                BaseAddress = new Uri(baseAddress),
            };

            if (httpVersion != null)
            {
                HttpVersion = httpVersion;
            }
        }

        public Version HttpVersion { get; } = new Version(1, 1);

        public async Task<string> GetAsync(string path)
        {
            using var requ = new HttpRequestMessage(HttpMethod.Get, path);
            requ.Headers.Authorization = _tokenHeader;
            requ.Version = HttpVersion;

            using var resp = await _client.SendAsync(requ);
            resp.EnsureSuccessStatusCode();

            return await resp.Content.ReadAsStringAsync();
        }

        public async Task RefreshTokenAsync(string path)
        {
            var token = await GetAsync(path);
            _tokenHeader = new AuthenticationHeaderValue("Bearer", token);
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).

                    _client?.Dispose();
                    _handler?.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~Program()
        // {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
