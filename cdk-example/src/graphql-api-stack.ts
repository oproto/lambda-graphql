import * as cdk from 'aws-cdk-lib';
import * as appsync from 'aws-cdk-lib/aws-appsync';
import * as lambda from 'aws-cdk-lib/aws-lambda';
import * as iam from 'aws-cdk-lib/aws-iam';
import { Construct } from 'constructs';
import * as fs from 'fs';
import * as path from 'path';

interface ResolverManifest {
  resolvers: ResolverConfig[];
  dataSources: DataSourceConfig[];
}

interface ResolverConfig {
  typeName: string;
  fieldName: string;
  kind: string;
  dataSource: string;
  lambdaFunctionName: string;
  lambdaFunctionLogicalId: string;
  // Lambda Annotations configuration
  resourceName?: string;
  memorySize?: number;
  timeout?: number;
  role?: string;
  policies?: string[];
  // Resolver behavior
  usesLambdaContext?: boolean;
}

interface DataSourceConfig {
  name: string;
  type: string;
}

export class GraphQLApiStack extends cdk.Stack {
  constructor(scope: Construct, id: string, props?: cdk.StackProps) {
    super(scope, id, props);

    // Schema path for validation and API creation
    const schemaPath = path.join(__dirname, '../lib/schema.graphql');
    
    if (!fs.existsSync(schemaPath)) {
      throw new Error(`Schema file not found: ${schemaPath}. Run 'dotnet build' in Oproto.Lambda.GraphQL.Examples first.`);
    }

    // Read resolver manifest
    const resolversPath = path.join(__dirname, '../lib/resolvers.json');

    if (!fs.existsSync(resolversPath)) {
      throw new Error(`Resolvers manifest not found: ${resolversPath}. Run 'dotnet build' in Oproto.Lambda.GraphQL.Examples first.`);
    }

    const resolverManifest: ResolverManifest = JSON.parse(fs.readFileSync(resolversPath, 'utf-8'));

    // Create AppSync GraphQL API
    const api = new appsync.GraphqlApi(this, 'Api', {
      name: 'lambda-graphql-example-api',
      schema: appsync.SchemaFile.fromAsset(schemaPath),
      authorizationConfig: {
        defaultAuthorization: {
          authorizationType: appsync.AuthorizationType.API_KEY,
          apiKeyConfig: {
            expires: cdk.Expiration.after(cdk.Duration.days(365)),
          },
        },
        additionalAuthorizationModes: [
          {
            authorizationType: appsync.AuthorizationType.IAM,
          },
        ],
      },
      xrayEnabled: true,
      logConfig: {
        fieldLogLevel: appsync.FieldLogLevel.ALL,
        excludeVerboseContent: false,
      },
    });

    // Create Lambda execution role
    const lambdaRole = new iam.Role(this, 'LambdaExecutionRole', {
      assumedBy: new iam.ServicePrincipal('lambda.amazonaws.com'),
      managedPolicies: [
        iam.ManagedPolicy.fromAwsManagedPolicyName('service-role/AWSLambdaBasicExecutionRole'),
      ],
    });

    // Create Lambda functions and data sources
    const lambdaFunctions = new Map<string, lambda.Function>();
    const dataSources = new Map<string, appsync.LambdaDataSource>();

    // Create one Lambda function per resolver (required for Lambda Annotations routing)
    for (const resolver of resolverManifest.resolvers) {
      const functionId = `${resolver.lambdaFunctionLogicalId}`;
      
      // Lambda Annotations uses assembly name as handler with ANNOTATIONS_HANDLER env var
      const lambdaFunction = new lambda.Function(this, functionId, {
        runtime: lambda.Runtime.DOTNET_6,
        handler: 'Oproto.Lambda.GraphQL.Examples', // Assembly name for Lambda Annotations
        code: lambda.Code.fromAsset(path.join(__dirname, '../../Oproto.Lambda.GraphQL.Examples/bin/Release/net6.0')),
        role: lambdaRole,
        timeout: cdk.Duration.seconds(resolver.timeout || 30), // Use Lambda Annotations config or default
        memorySize: resolver.memorySize || 512, // Use Lambda Annotations config or default
        environment: {
          ANNOTATIONS_HANDLER: resolver.lambdaFunctionName, // Routes to specific C# method
        },
        description: `${resolver.typeName}.${resolver.fieldName} resolver`,
      });

      lambdaFunctions.set(resolver.lambdaFunctionLogicalId, lambdaFunction);

      // Each Lambda function gets its own data source (Lambda Annotations = 1 function per method)
      const dataSource = api.addLambdaDataSource(resolver.dataSource, lambdaFunction, {
        description: `Lambda data source for ${resolver.typeName}.${resolver.fieldName}`,
      });
      dataSources.set(resolver.dataSource, dataSource);
    }

    // Create resolvers from manifest
    for (const resolverConfig of resolverManifest.resolvers) {
      const lambdaFunction = lambdaFunctions.get(resolverConfig.lambdaFunctionLogicalId);
      if (!lambdaFunction) {
        throw new Error(`Lambda function not found: ${resolverConfig.lambdaFunctionLogicalId}`);
      }

      const dataSource = dataSources.get(resolverConfig.dataSource);
      if (!dataSource) {
        throw new Error(`Data source not found: ${resolverConfig.dataSource}`);
      }

      // Create resolver
      new appsync.Resolver(this, `${resolverConfig.typeName}${resolverConfig.fieldName}Resolver`, {
        api,
        typeName: resolverConfig.typeName,
        fieldName: resolverConfig.fieldName,
        dataSource,
        runtime: appsync.FunctionRuntime.JS_1_0_0,
        code: appsync.Code.fromInline(
          resolverConfig.usesLambdaContext
            ? // Full context mode - Lambda uses ILambdaContext or AppSync-specific types
              `export function request(ctx) {
                return {
                  operation: 'Invoke',
                  payload: {
                    field: '${resolverConfig.fieldName}',
                    arguments: ctx.arguments,
                    source: ctx.source,
                    identity: ctx.identity,
                    request: ctx.request,
                  },
                };
              }
              export function response(ctx) {
                if (ctx.error) {
                  util.error(ctx.error.message, ctx.error.type);
                }
                return ctx.result;
              }`
            : // Arguments-only mode - send as single value if one arg, object if multiple
              `export function request(ctx) {
                const argKeys = Object.keys(ctx.arguments);
                return {
                  operation: 'Invoke',
                  payload: argKeys.length === 1 ? ctx.arguments[argKeys[0]] : ctx.arguments,
                };
              }
              export function response(ctx) {
                if (ctx.error) {
                  util.error(ctx.error.message, ctx.error.type);
                }
                return ctx.result;
              }`
        ),
      });
    }

    // Outputs
    new cdk.CfnOutput(this, 'GraphQLApiUrl', {
      value: api.graphqlUrl,
      description: 'GraphQL API URL',
      exportName: 'GraphQLApiUrl',
    });

    new cdk.CfnOutput(this, 'GraphQLApiKey', {
      value: api.apiKey || 'N/A',
      description: 'GraphQL API Key',
      exportName: 'GraphQLApiKey',
    });

    new cdk.CfnOutput(this, 'GraphQLApiId', {
      value: api.apiId,
      description: 'GraphQL API ID',
      exportName: 'GraphQLApiId',
    });

    new cdk.CfnOutput(this, 'Region', {
      value: this.region,
      description: 'AWS Region',
      exportName: 'Region',
    });

    // Output example query
    new cdk.CfnOutput(this, 'ExampleQuery', {
      value: `curl -X POST ${api.graphqlUrl} -H "Content-Type: application/json" -H "x-api-key: \${API_KEY}" -d '{"query":"query { getProduct(id: \\"123\\") { Id displayName Price } }"}'`,
      description: 'Example GraphQL query (replace ${API_KEY} with actual key)',
    });
  }
}
