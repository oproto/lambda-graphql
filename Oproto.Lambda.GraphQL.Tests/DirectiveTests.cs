using FluentAssertions;
using Oproto.Lambda.GraphQL.Attributes;
using Xunit;

namespace Oproto.Lambda.GraphQL.Tests;

public class DirectiveTests
{
    [Fact]
    public void GraphQLDirectiveAttribute_ShouldHaveCorrectProperties()
    {
        // Arrange & Act
        var directive = new GraphQLDirectiveAttribute("auth")
        {
            Description = "Authentication directive",
            Locations = DirectiveLocation.FieldDefinition | DirectiveLocation.Object,
            Arguments = "requires: String!"
        };

        // Assert
        directive.Name.Should().Be("auth");
        directive.Description.Should().Be("Authentication directive");
        directive.Locations.Should().Be(DirectiveLocation.FieldDefinition | DirectiveLocation.Object);
        directive.Arguments.Should().Be("requires: String!");
    }

    [Fact]
    public void GraphQLApplyDirectiveAttribute_ShouldHaveCorrectProperties()
    {
        // Arrange & Act
        var applyDirective = new GraphQLApplyDirectiveAttribute("auth")
        {
            Arguments = "requires: \"ADMIN\""
        };

        // Assert
        applyDirective.DirectiveName.Should().Be("auth");
        applyDirective.Arguments.Should().Be("requires: \"ADMIN\"");
    }

    [Fact]
    public void GraphQLAuthDirectiveAttribute_ShouldHaveCorrectProperties()
    {
        // Arrange & Act
        var authDirective = new GraphQLAuthDirectiveAttribute(AuthMode.UserPools)
        {
            CognitoGroups = "admin,user",
            IamResource = "arn:aws:dynamodb:*"
        };

        // Assert
        authDirective.AuthMode.Should().Be(AuthMode.UserPools);
        authDirective.CognitoGroups.Should().Be("admin,user");
        authDirective.IamResource.Should().Be("arn:aws:dynamodb:*");
    }

    [Fact]
    public void GraphQLTimestampAttribute_ShouldHaveCorrectUsage()
    {
        // Arrange & Act
        var timestampAttr = new GraphQLTimestampAttribute();

        // Assert - just verify it can be instantiated
        timestampAttr.Should().NotBeNull();
    }

    [Theory]
    [InlineData(AuthMode.ApiKey)]
    [InlineData(AuthMode.UserPools)]
    [InlineData(AuthMode.IAM)]
    [InlineData(AuthMode.OpenIDConnect)]
    [InlineData(AuthMode.Lambda)]
    public void AuthMode_ShouldHaveAllExpectedValues(AuthMode authMode)
    {
        // Act & Assert - just verify the enum values exist
        authMode.Should().BeDefined();
    }
}
