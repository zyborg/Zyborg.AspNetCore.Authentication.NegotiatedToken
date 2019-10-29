# Zyborg.AspNetCore.Authentication.NegotiatedToken

Implements a _compound_ authentication mechanism for ASP.NET Core using
Negotiate and JWT Bearer schemes to support Negotiate (Kerberos)
authentication in an HTTP/2 context. 

---

[![GitHub WorkFlow - CI](https://github.com/zyborg/Zyborg.AspNetCore.Authentication.NegotiatedToken/workflows/CI/badge.svg)](https://github.com/zyborg/Zyborg.AspNetCore.Authentication.NegotiatedToken/actions?CI)
[![GitHub Release Notes (latest by date)](https://img.shields.io/github/v/release/zyborg/Zyborg.AspNetCore.Authentication.NegotiatedToken)](https://github.com/zyborg/Zyborg.AspNetCore.Authentication.NegotiatedToken/releases/latest)
[![Nuget  Release](https://img.shields.io/nuget/v/Zyborg.AspNetCore.Authentication.NegotiatedToken)](https://www.nuget.org/packages/Zyborg.AspNetCore.Authentication.NegotiatedToken/)
[![GitHub Preview](https://img.shields.io/badge/github%20nuget-latest%20preview-orange)](https://github.com/zyborg/Zyborg.AspNetCore.Authentication.NegotiatedToken/packages)

---

This repo defines a pair of complementary .NET Core packages (one each for server-side,
and client-side) that implement a mechanism for _effectively_ using Windows 
Authentication (NTLM/Kerberos/Negotiate) with an HTTP/2 connection, such as in the case
of gRPC calls.

## The Problem

Currently the use of [Windows Authentication over HTTP](https://docs.microsoft.com/en-us/previous-versions/ms995330(v=msdn.10)?redirectedfrom=MSDN)
is limited to HTTP/1.x connections because the [Negotiate protocol](https://tools.ietf.org/html/rfc4559)
is undefined and [not supported for HTTP/2 connections](https://docs.microsoft.com/en-us/iis/get-started/whats-new-in-iis-10/http2-on-iis#when-is-http2-not-supported).

In some cases this can be overcome by supporting mixed protocol versions within a single
application, however this limitation poses a significant challenge for gRPC calls
because gRPC is explicitly defined atop HTTP/2.

## This Solution

In this repo we leverage the combination of two authentication schemes, Negotiate and
JWT Bearer tokens, to provide a workable solution to this problem.  The overall solution
works as follows.

On the server-side, in an ASP.NET Core application:

* Both HTTP/1.x _and_ HTTP/2 should be enabled.
* Both Negotiate and JWT Bearer authentication schemes are enabled and configured.
* The JWT Bearer scheme is configured as the default authentication scheme.
* A _token endpoint_ is exposed that is explicitly protected by the Negotiate scheme.
* The token endpoint returns a generated JWT token that contains some key identifying
details about the authenticated user context extracted from the Negotiate scheme.
* By default all other protected resources use the JWT Bearer scheme to authenticate.
* The JWT Bearer scheme is configured to reproduce the authenticated user context
extracted from the JWT token's user identifying details (i.e. reproduces the
claims that were originally captured from the Negotiate scheme)
* The JWT token has a relatively short lifetime (such as a few minutes) and so
this process should be repeated every so often to minimize security attacks
vulnerabilities

Because the JWT Bearer Token scheme is [fully supported under gRPC](https://docs.microsoft.com/en-us/aspnet/core/grpc/authn-and-authz?view=aspnetcore-3.0#other-authentication-mechanisms)
(and thus under HTTP/2) we can protect gRPC services with this _surrogate token_
and still have access to security context details from the Negotiate authentication
scheme.

On the client-side, an application would need to implement a flow that is compatible
with this setup:

* A client would need to maintain some state associated with the server, specifically
  a JWT token.
* The client needs to maintain a _valid token_ (i.e. the token exists and
  has not expired).
* For every call, regardless of which HTTP protocol version is being used
  for that call (HTTP/1.x or HTTP/2), the client checks for a valid token first;
  if the client does not have a valid token, it would first connect to the
  configured _token endpoint_ on the target server to retrieve a fresh
  _surrogate token_ to retrieve a fresh _surrogate token_. It would do so
  using HTTP/1.1 and with compatible Windows Authentication credentials.
* For every call (or every call to a known authenticated endpoint), the client
  injects the _surrogate token_.

## This Solution's Packages

This repo provides two nuget packages to implement the behavior above.

For the server-side, the `Zyborg.AspNetCore.Authentication.NegotiatedToken`
package is provided that configures the two authentication schemes,
[Negotiate](https://docs.microsoft.com/en-us/aspnet/core/security/authentication/windowsauth?view=aspnetcore-3.0&tabs=visual-studio#kestrel)
and
[JWT Bearer token](https://www.nuget.org/packages/Microsoft.AspNetCore.Authentication.JwtBearer/).
To work in a compatible fashion.  It includes support for generating the JWT
token with information extracted from the Negotiate scheme context, and for
reproducing a security context from the incomging JWT tokens.

For the client-side the `Zyborg.NegotiatedToken.Client` provides a
[_delegating HTTP handler_](https://docs.microsoft.com/en-us/dotnet/api/system.net.http.delegatinghandler?view=netcore-3.0)
that can be configured for an HTTP client that implements most of the
logic described above.  This client should be configured in concert with
your own HTTP handler to ensure that Windows Authentication credentials are
provided when prompted by the token endpoint authenticated by the
Negotiate scheme.

## Usage

Please see the examples provided for usage:

* Example.Web:
  * [Server](examples/Example.Web.Server)
  * [Client](examples/Example.Web.Client)
* Example.GRPC:
  * [Server](examples/Example.GRPC.Server)
  * [Client](examples/Example.GRPC.Client)

## BONUS -- Call Windows Authenticated gRPC Services from AWS Lambda

* Do you use AWS?
* Do you use serverless functions via AWS Lambda?
* Would you like to be able to call _Windows Authenticated gRPC Services
from your Lambda functions_?

Take a look at
[this sample project](https://github.com/zyborg/Zyborg.AWS.Lambda.Kerberos#sample4)
that combines the [Lambda Kerberos](https://github.com/zyborg/Zyborg.AWS.Lambda.Kerberos)
library with this NegotiatedToken compound authentication scheme to support
this exact scenario.
