using Keues.API.Common;
using Xunit;

namespace Keues.Tests.Api;

public class QrServerAddressTests
{
  [Theory]
  [InlineData("http://localhost:5125", 5125)]
  [InlineData("http://+:8080", 8080)]
  [InlineData("https://localhost:7102", 7102)]
  [InlineData("http://+:8080;https://localhost:7102", 8080)]
  public void ParsePort_returns_the_first_http_port(string urls, int expected)
  {
    Assert.Equal(expected, QrServerAddress.ParsePort(urls));
  }

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  [InlineData("not-a-url")]
  public void ParsePort_returns_null_for_invalid_input(string? urls)
  {
    Assert.Null(QrServerAddress.ParsePort(urls));
  }

  [Fact]
  public void Build_returns_the_lan_ip_and_public_port()
  {
    var result = QrServerAddress.Build("192.168.1.50", 8081);

    Assert.Equal("http://192.168.1.50:8081", result);
  }

  [Fact]
  public void Build_uses_the_default_public_port()
  {
    var result = QrServerAddress.Build("192.168.1.50");

    Assert.Equal("http://192.168.1.50:8081", result);
  }

  [Fact]
  public void Build_returns_null_without_a_lan_ip()
  {
    Assert.Null(QrServerAddress.Build(null, 8081));
  }
}
