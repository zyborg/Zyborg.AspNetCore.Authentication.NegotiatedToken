# README - experiments/ManualJwt.Server

The majority of the examples found on the web for using the ASP.NET Core
JWT Bearer Token authentication scheme do so in the context of integrating
with a separate IdP (such as IdentityServer4) that issues the tokens and
the ASP.NET Core app is configured to trust the externally generated token.

However, in this experiment we both generate and validate the JWT token
all within the app.  This is a useful when you have some custom logic to
perform authentication, perhaps from the context of a token generation
endpoint or converting one set of credentials into a token
(e.g. receiving a set of username+password credentials,
or receiving API key via a custom authentication header.)
