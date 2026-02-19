# Product Overview

Oproto.Lambda.GraphQL is a .NET library that generates GraphQL schemas from AWS Lambda functions for AWS AppSync.

## Purpose
- Generate GraphQL SDL schemas from C# types and Lambda function attributes
- Track AppSync resolver configurations (unit and pipeline resolvers)
- Provide compile-time validation of GraphQL types and resolver mappings
- Enable CDK-compatible resolver configuration generation

## Key Outputs
- `schema.graphql` - GraphQL SDL schema file
- `resolvers.json` - Resolver configuration manifest for CDK deployment

## Target Users
.NET developers building GraphQL APIs with AWS AppSync and Lambda.
