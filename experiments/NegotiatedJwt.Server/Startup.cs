using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace NegotiatedJwt.Server
{
    public class Startup
    {
        string _password = "$uper53cret";
        string _issuer = typeof(Startup).Namespace;
        string _audience = typeof(Startup).Namespace;
        string _authType = "Manual-JWT-Token";
        SecurityKey _jwtKey;
        TokenValidationParameters _jwtParams;

        public Startup()
        {
            using var sha = SHA256.Create();
            var passwordBytes = Encoding.UTF8.GetBytes(_password);
            var passwordHash = sha.ComputeHash(passwordBytes);

            _jwtKey = new SymmetricSecurityKey(passwordHash);
            _jwtParams = new TokenValidationParameters
            {
                ValidIssuer = _issuer,
                ValidAudience = _audience,
                IssuerSigningKey = _jwtKey,
                AuthenticationType = _authType,
                NameClaimType = ClaimTypes.Name,
            };
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = _jwtParams;
                })
                .AddNegotiate();

            services.AddAuthorization();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("Hello World!");
                });

                endpoints.MapGet("/token", async context =>
                {
                    var claims = new[]
                    {
                        new Claim(ClaimTypes.Name, context.User.Identity.Name),
                    };
                    var sigCreds = new SigningCredentials(_jwtKey, SecurityAlgorithms.HmacSha256);
                    var handler = new JwtSecurityTokenHandler();
                    var token = handler.CreateJwtSecurityToken(
                        issuer: _issuer,
                        audience: _audience,
                        subject: new ClaimsIdentity(claims),
                        notBefore: DateTime.Now.AddMinutes(-60),
                        expires: DateTime.Now.AddMinutes(60),
                        signingCredentials: sigCreds);
                    var tokenCompact = handler.WriteToken(token);
                    await context.Response.WriteAsync(tokenCompact);
                })
                    .RequireAuthorization(new AuthorizeAttribute
                    {
                        AuthenticationSchemes = NegotiateDefaults.AuthenticationScheme
                    });

                endpoints.MapGet("/unprotected", async context => await WriteDetailsAsync(context));
                endpoints.MapGet("/protected", async context => await WriteDetailsAsync(context))
                    .RequireAuthorization();

                static async Task WriteDetailsAsync(HttpContext context)
                {
                    var groupSids = context.User.Claims.Where(c =>
                        c.Type == ClaimTypes.GroupSid).Count();
                    var claims = context.User.Claims.Where(c =>
                        c.Type != ClaimTypes.GroupSid).Select(c => c.ToString());

                    var details = new
                    {
                        context.Request.Protocol,
                        context.Request.Path.Value,
                        context.User.Identity?.IsAuthenticated,
                        context.User.Identity?.Name,
                        context.User.Identity?.AuthenticationType,
                        Claims = claims,
                        GroupSidCount = groupSids,
                    };
                    await context.Response.WriteAsync(JsonSerializer.Serialize(details));
                }
            });
        }
    }
}
