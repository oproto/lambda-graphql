using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using Oproto.Lambda.GraphQL.Attributes;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]
[assembly: GraphQLSchema("ExampleAPI", Description = "Example GraphQL API for Oproto.Lambda.GraphQL")]
