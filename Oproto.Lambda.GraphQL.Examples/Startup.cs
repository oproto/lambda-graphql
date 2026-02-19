using Amazon.Lambda.Annotations;

[assembly: LambdaGlobalProperties(GenerateMain = true)]

namespace Oproto.Lambda.GraphQL.Examples;

[LambdaStartup]
public class Startup
{
    // Lambda Annotations will generate the Main method and bootstrap code
    // The ANNOTATIONS_HANDLER environment variable routes to specific methods
}
