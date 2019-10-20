using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Negotiate.Client
{
    class Program : IDisposable
    {
        static async Task Main(string[] args)
        {
            var v11 = new Version(1, 1);
            var v20 = new Version(2, 0);

            var combos = new[]
            {
                new { Version = v11, CredMode = CredentialMode.None },
                new { Version = v11, CredMode = CredentialMode.DefaultCredential },
                new { Version = v11, CredMode = CredentialMode.CachedDefaultCredential },
                new { Version = v11, CredMode = CredentialMode.CachedDefaultNetworkCredential },
                new { Version = v20, CredMode = CredentialMode.None },
                new { Version = v20, CredMode = CredentialMode.DefaultCredential },
                new { Version = v20, CredMode = CredentialMode.CachedDefaultCredential },
                new { Version = v20, CredMode = CredentialMode.CachedDefaultNetworkCredential },
            };

            foreach (var combo in combos)
            {
                using var prog = new Program("https://EC2AMAZ-7RM8TFE:5001",
                    combo.Version, combo.CredMode);
                Console.WriteLine("==========================");
                Console.WriteLine("Using HTTP/{0} with Credential Mode = {1}",
                    prog.HttpVersion, combo.CredMode);
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

        HttpClientHandler _handler;
        HttpClient _client;

        public enum CredentialMode
        {
            None = 0,
            DefaultCredential,
            CachedDefaultCredential,
            CachedDefaultNetworkCredential,
        }

        public Program(string baseAddress, Version httpVersion = null,
            CredentialMode credMode = CredentialMode.None)
        {
            _handler = new HttpClientHandler();
            _handler.ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

            switch (credMode)
            {
                case CredentialMode.DefaultCredential:
                    _handler.UseDefaultCredentials = true;
                    break;
                case CredentialMode.CachedDefaultCredential:
                    _handler.Credentials = CredentialCache.DefaultCredentials;
                    break;
                case CredentialMode.CachedDefaultNetworkCredential:
                    _handler.Credentials = CredentialCache.DefaultNetworkCredentials;
                    break;
            }

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
            requ.Version = HttpVersion;
            

            using var resp = await _client.SendAsync(requ);
            resp.EnsureSuccessStatusCode();

            return await resp.Content.ReadAsStringAsync();
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
