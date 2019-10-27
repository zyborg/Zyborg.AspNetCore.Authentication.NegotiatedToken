using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Zyborg.AspNetCore.Authentication.NegotiatedToken;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class NegotiatedTokenExtensions
    {
        public const string AuthenticationTypeClaimType =
            "$" + nameof(WindowsIdentity.AuthenticationType);

        public static AuthenticationBuilder AddNegotiatedToken(this AuthenticationBuilder builder,
            Action<NegotiatedTokenOptions> configureOptions = null)
        {
            var options = new NegotiatedTokenOptions();
            configureOptions?.Invoke(options);

            if (options.TokenValidationParameters == null)
            {
                options.TokenValidationParameters = new NegotiatedTokenValidationParameters
                {
                    IssuerSigningKey = options.IssuerSigningKey,
                    ValidateIssuerSigningKey = options.IssuerSigningKey != null
                        && !string.IsNullOrEmpty(options.SigningAlgorithm),

                    ValidIssuer = options.ClaimsIssuer,
                    ValidateIssuer = !string.IsNullOrEmpty(options.ClaimsIssuer),

                    ValidAudience = options.Audience,
                    ValidateAudience = !string.IsNullOrEmpty(options.Audience),

                    AuthenticationType = options.AuthenticationType,
                };
            }

            builder.AddNegotiate(options.NegotiateAuthenticationScheme,
                options.NegotiateDisplayName, negotiateOptions =>
                {
                    negotiateOptions.ClaimsIssuer = options.ClaimsIssuer;
                });
            builder.AddJwtBearer(options.JwtBearerAuthenticationScheme,
                options.JwtBearerDisplayName, jwtBearerOptions =>
                {
                    jwtBearerOptions.TokenValidationParameters = options.TokenValidationParameters;

                    // In case there was already a set of event handlers configured or 
                    // the handler we care about already has a handler configured we
                    // want to preserve that and call it later, otherwise create new
                    jwtBearerOptions.Events = options.Events ?? new JwtBearerEvents();
                    var originalOnTokenValidated = jwtBearerOptions.Events.OnTokenValidated;

                    jwtBearerOptions.Events.OnTokenValidated = ctx =>
                    {
                        // Swap out the ClaimsIdentity for a WindowsIdentity
                        var incomingPrincipal = ctx.Principal;
                        var incomingIdentity = incomingPrincipal.Identity;

                        var upn = incomingIdentity.Name;
                        var match = Regex.Match(upn, "([^\\\\]+)\\\\(.+)");
                        if (match.Success)
                        {
                            // We need to transform DOMAIN\username to username@DOMAIN
                            // to provide a property formatted user principal name (UPN)
                            upn = $"{match.Groups[2].Value}@{match.Groups[1].Value}";
                        }

                        var authType = incomingPrincipal.Claims
                            .FirstOrDefault(c => c.Type == AuthenticationTypeClaimType);

                        // Based on experimentation, the AuthenticationType does not always resolve
                        // and will throw a UnauthorizedAccessException if accessed from the created
                        // WindowsIdentity, so here we explicitly pass it in from the previously saved
                        var outgoingIdentity = new WindowsIdentity(upn);

                        if (authType != null)
                        {
                            // TODO: this is a cheap way to get the userToken, by creating
                            // the WinID twice but there should be a cleaner way
                            outgoingIdentity = new WindowsIdentity(outgoingIdentity.Token, authType.Value);
                        }

                        var outgoingPrincipal = new WindowsPrincipal(outgoingIdentity);
                        ctx.Principal = outgoingPrincipal;

                        // Preserve the original ClaimsIdentities as an additional identities
                        outgoingPrincipal.AddIdentities(incomingPrincipal.Identities);

                        // Invoke the original handler if there was one
                        return originalOnTokenValidated?.Invoke(ctx)
                            ?? Task.CompletedTask;
                    };
                });

            builder.Services.AddSingleton(options);
            return builder;
        }

        public static IEndpointConventionBuilder MapGetNegotiatedToken(
            this IEndpointRouteBuilder builder, string pattern)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            var options = builder.ServiceProvider.GetService<NegotiatedTokenOptions>();
            if (options == null)
            {
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                // We check to make sure this has been configured correctly so
                // that the options are accessible during the mapped GET handler
                throw new Exception("missing Negotiated Token options --"
                    + " did you forget to add scheme to Authentication?");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
            }

            var conventionBuilder = builder.MapGet(pattern, async context =>
            {
                // This should always succeed because we checked at time
                // of registration that the options were available
                var options = context.RequestServices.GetRequiredService<NegotiatedTokenOptions>();

                // Based on IdentityServer4 samples and usage, JwtSecurityTokenHandler
                // is not thread-safe so we need to create a new one with each request
                var tokenHandler = options.TokenHandlerBuilder?.Invoke(context);
                if (tokenHandler == null)
                {
                    tokenHandler = new JwtSecurityTokenHandler();
                }

                var tokenDescriptor = options.SecurityTokenDescriptorBuilder?.Invoke(context);
                if (tokenDescriptor == null)
                {
                    var incomingSubject = context.User.Identity as WindowsIdentity;
                    var incomingName = incomingSubject?.Name
                        ?? context.User.Identity.Name;
                    var incomingType = incomingSubject?.AuthenticationType
                        ?? context.User.Identity.AuthenticationType;
                    var claims = incomingSubject.Claims
                        .Where(c => c.Type == WindowsIdentity.DefaultNameClaimType
                            || c.Type == WindowsIdentity.DefaultRoleClaimType);
                    claims = claims.Concat(new[]
                    {
                        new Claim(AuthenticationTypeClaimType, incomingType),
                    });

                    //var outgoingSubject = new WindowsIdentity(incomingName);
                    var outgoingSubject = new ClaimsIdentity(claims,
                        options.AuthenticationType);

                    SigningCredentials sigCreds = null;
                    if (options.IssuerSigningKey != null && !string.IsNullOrEmpty(options.SigningAlgorithm))
                    {
                        sigCreds = new SigningCredentials(options.IssuerSigningKey, options.SigningAlgorithm);
                    }
                    tokenDescriptor = new SecurityTokenDescriptor
                    {
                        Issuer = options.ClaimsIssuer,
                        Audience = options.Audience,
                        Subject = outgoingSubject,
                        NotBefore = DateTime.Now,
                        Expires = DateTime.Now.Add(options.TokenMaxAge),
                        SigningCredentials = sigCreds,
                    };
                }

                var token = tokenHandler.CreateJwtSecurityToken(tokenDescriptor);
                var tokenCompact = tokenHandler.WriteToken(token);
                await context.Response.WriteAsync(tokenCompact).ConfigureAwait(false);
            });

            // Make sure the mapped token endpoint is authorized with the Negotiate scheme
            conventionBuilder.RequireAuthorization(new AuthorizeAttribute
            {
                AuthenticationSchemes = options.NegotiateAuthenticationScheme,
            });

            return conventionBuilder;
        }
    }
}
