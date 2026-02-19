# CDK Deployment Example

This example demonstrates how to deploy the Oproto.Lambda.GraphQL generated schema and resolvers to AWS AppSync using AWS CDK.

## Prerequisites

- AWS CDK installed: `npm install -g aws-cdk`
- AWS credentials configured
- .NET 6.0+ SDK
- Node.js 18+

## Project Structure

```
cdk-example/
├── src/                          # CDK TypeScript code
│   ├── app.ts                    # CDK app entry point
│   └── graphql-api-stack.ts      # AppSync API stack
├── lib/                          # Generated files (copied from Examples)
│   ├── schema.graphql            # Generated GraphQL schema
│   └── resolvers.json            # Generated resolver manifest
├── cdk.json                      # CDK configuration
├── package.json                  # Node dependencies
└── tsconfig.json                 # TypeScript configuration
```

## Setup

### 1. Build Oproto.Lambda.GraphQL.Examples

```bash
# From repository root
cd Oproto.Lambda.GraphQL.Examples
dotnet build

# Verify generated files
ls -la schema.graphql resolvers.json
```

### 2. Install CDK Dependencies

```bash
cd ../cdk-example
npm install
```

### 3. Copy Generated Files

```bash
# Copy generated schema and resolvers
cp ../Oproto.Lambda.GraphQL.Examples/schema.graphql lib/
cp ../Oproto.Lambda.GraphQL.Examples/resolvers.json lib/
```

## Deployment

### Bootstrap CDK (first time only)

```bash
cdk bootstrap
```

### Deploy Stack

```bash
# Synthesize CloudFormation template
cdk synth

# Deploy to AWS
cdk deploy

# Output will include:
# - GraphQL API URL
# - API Key (if using API_KEY auth)
```

### Test the API

```bash
# Get the API URL from CDK output
export API_URL="https://xxxxx.appsync-api.us-east-1.amazonaws.com/graphql"
export API_KEY="da2-xxxxx"

# Test query
curl -X POST $API_URL \
  -H "Content-Type: application/json" \
  -H "x-api-key: $API_KEY" \
  -d '{
    "query": "query { getProduct(id: \"123\") { Id displayName Price } }"
  }'
```

## Stack Components

The CDK stack creates:

1. **AppSync GraphQL API**
   - Schema from `schema.graphql`
   - API Key authentication (configurable)

2. **Lambda Functions**
   - One function per data source (from resolvers.json)
   - Handler format: Assembly name (for Lambda Annotations)
   - Configured with appropriate IAM roles
   - **Note**: Example uses placeholder Lambda code for demonstration
   - **Production**: Deploy actual Lambda implementation with proper handler routing

3. **Lambda Data Sources**
   - Connects AppSync to Lambda functions
   - Automatically configured from resolvers.json

4. **Unit Resolvers**
   - Maps GraphQL operations to Lambda functions
   - Configuration from resolvers.json

5. **IAM Roles**
   - AppSync → Lambda invocation permissions
   - Lambda → CloudWatch Logs permissions

### Lambda Annotations Handler Format

For production deployment with Lambda Annotations:
- Handler: `AssemblyName` (e.g., `Oproto.Lambda.GraphQL.Examples`)
- Environment variable: `ANNOTATIONS_HANDLER` set to method name
- Project must be `OutputType=Exe` with `LambdaStartup` class
- Requires `Amazon.Lambda.RuntimeSupport` package

The example project uses `OutputType=Library` for schema generation only.

## Customization

### Change Authentication Mode

Edit `src/graphql-api-stack.ts`:

```typescript
const api = new appsync.GraphqlApi(this, 'Api', {
  // Change from API_KEY to:
  authorizationConfig: {
    defaultAuthorization: {
      authorizationType: appsync.AuthorizationType.USER_POOL,
      userPoolConfig: {
        userPool: userPool,
      },
    },
  },
});
```

### Add Caching

```typescript
api.addCachingConfig({
  ttl: Duration.minutes(5),
  cachingKeys: ['$context.identity.sub'],
});
```

### Add X-Ray Tracing

```typescript
const api = new appsync.GraphqlApi(this, 'Api', {
  xrayEnabled: true,
  // ...
});
```

### Configure Lambda Memory/Timeout

```typescript
const lambdaFunction = new lambda.Function(this, 'ProductFunction', {
  memorySize: 512,
  timeout: Duration.seconds(30),
  // ...
});
```

## Cleanup

```bash
# Destroy all resources
cdk destroy
```

## Automated Deployment

For CI/CD integration:

```yaml
# .github/workflows/deploy.yml
name: Deploy GraphQL API

on:
  push:
    branches: [main]

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '6.0.x'
      
      - name: Build Oproto.Lambda.GraphQL.Examples
        run: |
          cd Oproto.Lambda.GraphQL.Examples
          dotnet build
      
      - name: Setup Node.js
        uses: actions/setup-node@v3
        with:
          node-version: '18'
      
      - name: Install CDK
        run: npm install -g aws-cdk
      
      - name: Deploy
        run: |
          cd cdk-example
          npm install
          cp ../Oproto.Lambda.GraphQL.Examples/schema.graphql lib/
          cp ../Oproto.Lambda.GraphQL.Examples/resolvers.json lib/
          cdk deploy --require-approval never
        env:
          AWS_ACCESS_KEY_ID: ${{ secrets.AWS_ACCESS_KEY_ID }}
          AWS_SECRET_ACCESS_KEY: ${{ secrets.AWS_SECRET_ACCESS_KEY }}
          AWS_REGION: us-east-1
```

## Troubleshooting

### Schema Upload Fails

**Error**: "Invalid schema syntax"

**Solution**: Verify generated schema is valid GraphQL:
```bash
# Install GraphQL CLI
npm install -g graphql-cli

# Validate schema
graphql-cli validate lib/schema.graphql
```

### Lambda Function Not Found

**Error**: "Function not found"

**Solution**: Ensure Lambda functions are deployed before creating resolvers:
```typescript
// In CDK stack, ensure functions are created before resolvers
const productFunction = new lambda.Function(/* ... */);
const dataSource = api.addLambdaDataSource('ProductsLambda', productFunction);
// Now create resolvers using dataSource
```

### Permission Denied

**Error**: "User is not authorized to perform: appsync:CreateGraphqlApi"

**Solution**: Ensure AWS credentials have required permissions:
```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": [
        "appsync:*",
        "lambda:*",
        "iam:*",
        "cloudformation:*"
      ],
      "Resource": "*"
    }
  ]
}
```

## Next Steps

- Add DynamoDB tables for data persistence
- Implement DataLoader for N+1 query prevention
- Add CloudWatch alarms for monitoring
- Configure custom domain name
- Add WAF rules for security

## Resources

- [AWS AppSync Documentation](https://docs.aws.amazon.com/appsync/)
- [AWS CDK Documentation](https://docs.aws.amazon.com/cdk/)
- [Oproto.Lambda.GraphQL Documentation](../docs/README.md)
