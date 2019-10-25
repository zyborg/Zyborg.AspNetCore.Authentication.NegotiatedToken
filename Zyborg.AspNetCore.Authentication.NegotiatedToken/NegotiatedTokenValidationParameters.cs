using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;

namespace Zyborg.AspNetCore.Authentication.NegotiatedToken
{
    public class NegotiatedTokenValidationParameters : TokenValidationParameters
    {
        public override ClaimsIdentity CreateClaimsIdentity(SecurityToken securityToken, string issuer)
        {
            var claimsId = base.CreateClaimsIdentity(securityToken, issuer);
            return new WindowsIdentity(claimsId.Name);
        }
    }
}
