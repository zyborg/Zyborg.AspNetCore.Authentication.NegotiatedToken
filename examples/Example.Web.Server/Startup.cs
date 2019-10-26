using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Zyborg.AspNetCore.Authentication.NegotiatedToken;

namespace Example.Web.Server
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication(NegotiatedTokenDefaults.JwtBearerAuthenticationScheme)
                .AddNegotiatedToken(options =>
                {
                    using var sha = SHA256.Create();
                    var password = "$uper53cret";
                    var passwordBytes = Encoding.UTF8.GetBytes(password);
                    var passwordHash = sha.ComputeHash(passwordBytes);

                    options.IssuerSigningKey = new SymmetricSecurityKey(passwordHash);
                    options.ClaimsIssuer = "https://localhost:5001";
                    options.Audience = "https://localhost:5001";
                });

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

                endpoints.MapGet("/unprotected", async context => await WriteDetailsAsync(context));
                endpoints.MapGet("/protected", async context => await WriteDetailsAsync(context))
                    .RequireAuthorization();
                endpoints.MapGet("/protected_negotiate", async context => await WriteDetailsAsync(context))
                    .RequireAuthorization(new AuthorizeAttribute
                    {
                        AuthenticationSchemes = NegotiatedTokenDefaults.NegotiateAuthenticationScheme
                    });

                endpoints.MapGetNegotiatedToken("/token");
            });
        }

        static async Task WriteDetailsAsync(HttpContext context)
        {
            var groupSids = context.User.Claims.Where(c =>
                c.Type == ClaimTypes.GroupSid).Count();

            var claims = context.User.Claims;

            if (!context.Request.Query.ContainsKey("keep_groupsid"))
            {
                // Unless requested to keep them, we filter out the
                // Group SIDs because the list is potentially very long
                // and their raw representation is not very infromative
                claims = claims.Where(c =>
                    c.Type != ClaimTypes.GroupSid);
            }
            
            var claimsToString = claims.Select(c => c.ToString());

            var details = new
            {
                context.Request.Protocol,
                context.Request.Path.Value,
                context.User.Identity?.IsAuthenticated,
                context.User.Identity?.Name,
                context.User.Identity?.AuthenticationType,
                Claims = claimsToString,
                GroupSidCount = groupSids,
            };
            await context.Response.WriteAsync(JsonSerializer.Serialize(details));
        }
    }
}
