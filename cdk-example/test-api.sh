#!/bin/bash
# Test script for deployed GraphQL API

if [ -z "$1" ] || [ -z "$2" ]; then
    echo "Usage: ./test-api.sh <API_URL> <API_KEY>"
    echo ""
    echo "Example:"
    echo "  ./test-api.sh https://xxxxx.appsync-api.us-east-1.amazonaws.com/graphql da2-xxxxx"
    exit 1
fi

API_URL="$1"
API_KEY="$2"

echo "=== Testing Oproto.Lambda.GraphQL API ==="
echo "API URL: $API_URL"
echo ""

# Test 1: Get Product
echo "Test 1: Query getProduct"
curl -s -X POST "$API_URL" \
  -H "Content-Type: application/json" \
  -H "x-api-key: $API_KEY" \
  -d '{"query":"query { getProduct(id: \"test-123\") { Id displayName Price } }"}' | jq '.'
echo ""

# Test 2: Create Product
echo "Test 2: Mutation createProduct"
curl -s -X POST "$API_URL" \
  -H "Content-Type: application/json" \
  -H "x-api-key: $API_KEY" \
  -d '{"query":"mutation { createProduct(input: { Name: \"Test Product\", Price: 29.99 }) { Id displayName Price } }"}' | jq '.'
echo ""

# Test 3: Search (Union type)
echo "Test 3: Query search (union type)"
curl -s -X POST "$API_URL" \
  -H "Content-Type: application/json" \
  -H "x-api-key: $API_KEY" \
  -d '{"query":"query { search(term: \"test\", limit: 10) { __typename ... on Product { Id displayName } ... on User { Id Email } } }"}' | jq '.'
echo ""

# Test 4: Get User (with AWS scalars)
echo "Test 4: Query getUser (AWS scalars)"
curl -s -X POST "$API_URL" \
  -H "Content-Type: application/json" \
  -H "x-api-key: $API_KEY" \
  -d '{"query":"query { getUser(id: \"user-123\") { Id Email CreatedAt BirthDate } }"}' | jq '.'
echo ""

# Test 5: Introspection query
echo "Test 5: Introspection query (schema types)"
curl -s -X POST "$API_URL" \
  -H "Content-Type: application/json" \
  -H "x-api-key: $API_KEY" \
  -d '{"query":"{ __schema { types { name kind } } }"}' | jq '.data.__schema.types | map(select(.name | startswith("__") | not)) | .[0:5]'
echo ""

echo "=== Tests Complete ==="
