﻿using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Text;
using Zyborg.AspNetCore.Authentication.NegotiatedToken;

namespace Example.GRPC.Server
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
                    SecurityKey sigKey;
                    using (var sha = SHA256.Create())
                    {
                        var password = "$00PERsecret";
                        var passwordHash = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
                        sigKey = new SymmetricSecurityKey(passwordHash);
                    }

                    options.IssuerSigningKey = sigKey;
                });
            services.AddAuthorization();
            services.AddGrpc();
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
                endpoints.MapGetNegotiatedToken("/token");

                endpoints.MapGrpcService<GreeterService>();

                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
                });
            });
        }
    }
}
