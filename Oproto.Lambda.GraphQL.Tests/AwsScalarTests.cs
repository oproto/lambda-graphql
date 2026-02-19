using FluentAssertions;
using Oproto.Lambda.GraphQL.SourceGenerator;
using System;
using Xunit;

namespace Oproto.Lambda.GraphQL.Tests;

public class AwsScalarTests
{
    [Theory]
    [InlineData("System.DateTime", "AWSDateTime")]
    [InlineData("System.DateTimeOffset", "AWSDateTime")]
    [InlineData("System.DateOnly", "AWSDate")]
    [InlineData("System.TimeOnly", "AWSTime")]
    [InlineData("System.Guid", "ID")]
    [InlineData("System.Text.Json.JsonElement", "AWSJSON")]
    [InlineData("System.Net.Mail.MailAddress", "AWSEmail")]
    [InlineData("System.Uri", "AWSURL")]
    [InlineData("System.Net.IPAddress", "AWSIPAddress")]
    public void GetAwsScalarType_ShouldMapCSharpTypesToAwsScalars(string csharpType, string expectedAwsScalar)
    {
        // Act
        var result = AwsScalarMapper.GetAwsScalarType(csharpType);

        // Assert
        result.Should().Be(expectedAwsScalar);
    }

    [Theory]
    [InlineData("System.String")]
    [InlineData("System.Int32")]
    [InlineData("System.Int64")]
    [InlineData("System.Boolean")]
    [InlineData("CustomType")]
    public void GetAwsScalarType_ShouldReturnNullForNonAwsTypes(string csharpType)
    {
        // Act
        var result = AwsScalarMapper.GetAwsScalarType(csharpType);

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData("System.DateTime")]
    [InlineData("System.Guid")]
    [InlineData("System.Text.Json.JsonElement")]
    public void HasAwsScalarMapping_ShouldReturnTrueForMappedTypes(string csharpType)
    {
        // Act
        var result = AwsScalarMapper.HasAwsScalarMapping(csharpType);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("System.String")]
    [InlineData("System.Int32")]
    [InlineData("System.Int64")]
    [InlineData("CustomType")]
    public void HasAwsScalarMapping_ShouldReturnFalseForUnmappedTypes(string csharpType)
    {
        // Act
        var result = AwsScalarMapper.HasAwsScalarMapping(csharpType);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetSupportedAwsScalars_ShouldReturnAllAwsScalarTypes()
    {
        // Act
        var scalars = AwsScalarMapper.GetSupportedAwsScalars();

        // Assert
        scalars.Should().Contain(new[]
        {
            "AWSDate",
            "AWSTime",
            "AWSDateTime",
            "AWSTimestamp",
            "AWSEmail",
            "AWSJSON",
            "AWSPhone",
            "AWSURL",
            "AWSIPAddress"
        });
    }
}
