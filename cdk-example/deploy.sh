#!/bin/bash
set -e

echo "=== Oproto.Lambda.GraphQL CDK Deployment Script ==="
echo ""

# Check prerequisites
command -v dotnet >/dev/null 2>&1 || { echo "Error: dotnet CLI not found. Install .NET 6.0+ SDK."; exit 1; }
command -v node >/dev/null 2>&1 || { echo "Error: node not found. Install Node.js 18+."; exit 1; }
command -v npm >/dev/null 2>&1 || { echo "Error: npm not found. Install Node.js 18+."; exit 1; }

# Step 1: Build Oproto.Lambda.GraphQL.Examples
echo "Step 1: Building Oproto.Lambda.GraphQL.Examples..."
cd ../Oproto.Lambda.GraphQL.Examples
dotnet build -c Release

if [ ! -f "schema.graphql" ]; then
    echo "Error: schema.graphql not generated. Check build output."
    exit 1
fi

if [ ! -f "resolvers.json" ]; then
    echo "Error: resolvers.json not generated. Check build output."
    exit 1
fi

echo "✓ Generated schema.graphql ($(wc -l < schema.graphql) lines)"
echo "✓ Generated resolvers.json ($(jq '.resolvers | length' resolvers.json) resolvers)"
echo ""

# Step 2: Copy generated files to CDK project
echo "Step 2: Copying generated files to CDK project..."
cd ../cdk-example
mkdir -p lib
cp ../Oproto.Lambda.GraphQL.Examples/schema.graphql lib/
cp ../Oproto.Lambda.GraphQL.Examples/resolvers.json lib/

echo "✓ Copied schema.graphql to cdk-example/lib/"
echo "✓ Copied resolvers.json to cdk-example/lib/"
echo ""

# Step 3: Install CDK dependencies
echo "Step 3: Installing CDK dependencies..."
if [ ! -d "node_modules" ]; then
    npm install
else
    echo "✓ Dependencies already installed (run 'npm install' to update)"
fi
echo ""

# Step 4: Build TypeScript
echo "Step 4: Building TypeScript..."
npm run build
echo "✓ TypeScript compiled"
echo ""

# Step 5: Synthesize CloudFormation template
echo "Step 5: Synthesizing CloudFormation template..."
npx cdk synth > /dev/null
echo "✓ CloudFormation template generated in cdk.out/"
echo ""

# Step 6: Deploy (optional)
if [ "$1" == "--deploy" ]; then
    echo "Step 6: Deploying to AWS..."
    echo "WARNING: This will create AWS resources that may incur costs."
    read -p "Continue? (y/N) " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        npx cdk deploy --require-approval never
        echo ""
        echo "=== Deployment Complete ==="
        echo "Check the outputs above for:"
        echo "  - GraphQL API URL"
        echo "  - API Key"
        echo "  - Example query command"
    else
        echo "Deployment cancelled."
    fi
else
    echo "=== Ready to Deploy ==="
    echo ""
    echo "To deploy to AWS, run:"
    echo "  ./deploy.sh --deploy"
    echo ""
    echo "Or manually:"
    echo "  npx cdk deploy"
    echo ""
    echo "To destroy resources:"
    echo "  npx cdk destroy"
fi
