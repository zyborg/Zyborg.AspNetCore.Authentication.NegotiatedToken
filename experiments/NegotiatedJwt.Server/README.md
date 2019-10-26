# README - experiments/NegotiatedJwt.Server

In this experiment we combine the use of the Negotiated authentication scheme
and the JWT Bearer Token authentication scheme, from the previous 2 experiments.

The Negotiated scheme is used to protect a "token issue" endpoint so it
requires successful Windows Authentication to retrieve a JWT _surrogate token_.

The JWT Token scheme is used as the default authentication scheme to protect all
subsequent protected endpoints.

This experiment forms the basis of the solution implemented in this repo.
