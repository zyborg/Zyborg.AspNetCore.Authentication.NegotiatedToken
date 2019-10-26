using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Example.GRPC.Server
{
    public class GreeterService : Greeter.GreeterBase
    {
        private readonly ILogger<GreeterService> _logger;
        public GreeterService(ILogger<GreeterService> logger)
        {
            _logger = logger;
        }

        public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
        {
            return Task.FromResult(new HelloReply
            {
                Message = "Hello " + request?.Name
            });
        }

        public override Task<DetailsReply> GetUnprotectedDetails(DetailsInput request, ServerCallContext context)
        {
            return Task.FromResult(new DetailsReply
            {
                Details = GetDetails(context.GetHttpContext(), request?.KeepGroupSid ?? false)
            });
        }

        [Authorize]
        public override Task<DetailsReply> GetProtectedDetails(DetailsInput request, ServerCallContext context)
        {
            return Task.FromResult(new DetailsReply
            {
                Details = GetDetails(context.GetHttpContext(), request?.KeepGroupSid ?? false)
            });
        }

        static string GetDetails(HttpContext context, bool keepGroupSid)
        {
            var groupSids = context.User.Claims.Where(c =>
                c.Type == ClaimTypes.GroupSid).Count();

            var claims = context.User.Claims;

            if (!keepGroupSid)
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
            return JsonSerializer.Serialize(details);
        }
    }
}
