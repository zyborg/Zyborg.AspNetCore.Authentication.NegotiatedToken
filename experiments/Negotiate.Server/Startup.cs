using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Negotiate.Server
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
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
