#!/usr/bin/env node
import 'source-map-support/register';
import * as cdk from 'aws-cdk-lib';
import { GraphQLApiStack } from './graphql-api-stack';

const app = new cdk.App();

new GraphQLApiStack(app, 'LambdaGraphQLExampleStack', {
  env: {
    account: process.env.CDK_DEFAULT_ACCOUNT,
    region: process.env.CDK_DEFAULT_REGION || 'us-east-1',
  },
  description: 'Oproto.Lambda.GraphQL Example - AppSync API with Lambda resolvers',
});

app.synth();
