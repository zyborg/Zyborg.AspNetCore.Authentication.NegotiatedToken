using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Principal;

namespace Zyborg.AspNetCore.Authentication.NegotiatedToken
{
    public class NegotiatedTokenOptions
    {
        public SecurityKey IssuerSigningKey { get; set; }

        public string SigningAlgorithm { get; set; } =
            SecurityAlgorithms.HmacSha256;

        public TimeSpan TokenMaxAge { get; set; } =
            TimeSpan.FromMinutes(15);

        public string ClaimsIssuer { get; set; } = WindowsIdentity.DefaultIssuer;

        public string Audience { get; set; }

        public string AuthenticationType { get; set; } = nameof(NegotiatedToken);

        public string NegotiateAuthenticationScheme { get; set; } =
            NegotiatedTokenDefaults.NegotiateAuthenticationScheme;

        public string NegotiateDisplayName { get; set; }

        public string JwtBearerAuthenticationScheme { get; set; } =
            NegotiatedTokenDefaults.JwtBearerAuthenticationScheme;

        public string JwtBearerDisplayName { get; set; }

        /// <summary>
        /// Optionally configure one or more event handlers for processing JWT Bearer resolution.
        /// Note, that this will be invoked after the Negotiated Token handling is completed.
        /// </summary>
        public JwtBearerEvents Events { get; set; }

        /// <summary>
        /// Optionally provide a custom handler for generating JWT tokens.
        /// </summary>
        public Func<HttpContext, JwtSecurityTokenHandler> TokenHandlerBuilder { get; set; }

        /// <summary>
        /// Optionally provide a descriptor used with the Token Handler for
        /// generating JWT toekns.
        /// </summary>
        public Func<HttpContext, SecurityTokenDescriptor> SecurityTokenDescriptorBuilder { get; set; }

        /// <summary>
        /// Optionally provide the complete set of validation parameters
        /// used to validate issued JWT tokens.
        /// </summary>
        public TokenValidationParameters TokenValidationParameters { get; set; }
    }
}
