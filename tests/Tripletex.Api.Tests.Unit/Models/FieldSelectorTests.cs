using FluentAssertions;
using Tripletex.Api.Models;

namespace Tripletex.Api.Tests.Unit.Models;

public class FieldSelectorTests
{
    [Fact]
    public void SingleField()
    {
        var selector = new FieldSelector().Add("id");
        selector.ToString().Should().Be("id");
    }

    [Fact]
    public void MultipleFields()
    {
        var selector = new FieldSelector().Add("id").Add("name").Add("email");
        selector.ToString().Should().Be("id,name,email");
    }

    [Fact]
    public void NestedFields()
    {
        var selector = new FieldSelector().Add("employee", "id", "firstName", "lastName");
        selector.ToString().Should().Be("employee.id,employee.firstName,employee.lastName");
    }

    [Fact]
    public void ImplicitStringConversion()
    {
        string result = new FieldSelector().Add("id").Add("name");
        result.Should().Be("id,name");
    }
}
