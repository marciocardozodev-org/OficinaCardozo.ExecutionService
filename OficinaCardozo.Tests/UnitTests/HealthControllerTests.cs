using Xunit;
using Microsoft.AspNetCore.Mvc;
using OficinaCardozo.API.Controllers;

namespace OficinaCardozo.Tests.UnitTests.Controllers;

public class HealthControllerTests
{
    [Fact]
    public void Live_ReturnsOk()
    {
        // Arrange
        var controller = new HealthController();

        // Act
        var result = controller.Live();

        // Assert
        Assert.IsType<OkObjectResult>(result);
        var okResult = result as OkObjectResult;
        Assert.NotNull(okResult);
        Assert.Equal(200, okResult.StatusCode ?? 200);
    }
}