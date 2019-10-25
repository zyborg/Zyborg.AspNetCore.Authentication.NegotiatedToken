using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.Negotiate;
using System;
using System.Collections.Generic;
using System.Text;

namespace Zyborg.AspNetCore.Authentication.NegotiatedToken
{
    public static class NegotiatedTokenDefaults
    {
        public const string NegotiateAuthenticationScheme = NegotiateDefaults.AuthenticationScheme;
        public const string JwtBearerAuthenticationScheme = JwtBearerDefaults.AuthenticationScheme;
    }
}
