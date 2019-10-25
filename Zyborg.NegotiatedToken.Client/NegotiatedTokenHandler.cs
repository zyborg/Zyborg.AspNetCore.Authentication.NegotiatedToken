using System;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Zyborg.NegotiatedToken.Client
{
    public class NegotiatedTokenHandler : DelegatingHandler
    {
        private Uri _tokenUrl;
        private string _jwtCompact;
        private JwtSecurityToken _jwt;
        private AuthenticationHeaderValue _tokenHeader;

        // This appears to be safe to use a single instance of this as long as we
        // are only performing read/validate operations, plus we plan to lock on it
        private JwtSecurityTokenHandler _jwtHandler = new JwtSecurityTokenHandler();

        public NegotiatedTokenHandler(Uri tokenUrl, HttpClientHandler innerHandler = null)
            : base(innerHandler)
        {
            _tokenUrl = tokenUrl;
        }

        public NegotiatedTokenHandler(string tokenUrl, HttpClientHandler innerHandler = null)
            : this(new Uri(tokenUrl, UriKind.RelativeOrAbsolute), innerHandler)
        { }

        public bool IsTokenExpired => _jwt == null || _jwt.ValidTo < DateTime.Now;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var now = DateTime.Now;
            if (IsTokenExpired)
            {
                await RefreshTokenAsync(request?.RequestUri, cancellationToken).ConfigureAwait(false);
            }

            // As long as there isn't already an authz header
            // we inject our own semi-custom Negotiate Token
            if (request != null && request.Headers.Authorization == null)
            {
                request.Headers.Authorization = _tokenHeader;
            }

            return await base.SendAsync(request, cancellationToken)
                .ConfigureAwait(false);
        }

        protected async Task RefreshTokenAsync(Uri baseUri = null,
            CancellationToken? cancellationToken = default)
        {
            var fullTokenUrl = _tokenUrl;
            if (baseUri != null && !fullTokenUrl.IsAbsoluteUri)
            {
                fullTokenUrl = new Uri(baseUri, fullTokenUrl);
            }

            var tokenRequ = new HttpRequestMessage(HttpMethod.Get, fullTokenUrl)
            {
                // Make sure this goes over HTTP/1.1 since
                // HTTP/2 does not support Negotiate
                Version = new Version(1, 1),
            };

            using (tokenRequ)
            using (var tokenResp = await base.SendAsync(tokenRequ,
                cancellationToken ?? CancellationToken.None).ConfigureAwait(false))
            {
                try
                {
                    tokenResp.EnsureSuccessStatusCode();
                    var jwtCompact = await tokenResp.Content.ReadAsStringAsync()
                        .ConfigureAwait(false);

                    // Only one instance should update the JWT
                    lock (_jwtHandler)
                    {
                        // Double-check to make sure we
                        // are still the first ones here
                        if (IsTokenExpired)
                        {
                            _tokenHeader = new AuthenticationHeaderValue("Bearer", jwtCompact);
                            _jwtCompact = jwtCompact; // Useful for troubleshooting
                            _jwt = _jwtHandler.ReadJwtToken(jwtCompact);
                        }
                    }
                }
                catch (Exception ex)
                {
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                    throw new Exception("failed to refresh Negotiate Token", ex);
#pragma warning restore CA1303 // Do not pass literals as localized parameters
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}
