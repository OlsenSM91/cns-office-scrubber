using OfficeScrubber.Cli;

namespace OfficeScrubber.Cli.Tests;

public sealed class CommandLineParserTests
{
    [Fact]
    public void Analyze_accepts_all_supported_options()
    {
        var result = CommandLineParser.Parse(
            ["analyze", "--verbose", "--debug", "--json", "--log", "logs"]);

        Assert.True(result.IsSuccess);
        Assert.Equal(CommandKind.Analyze, result.Value!.Command);
        Assert.True(result.Value.Verbose);
        Assert.True(result.Value.Debug);
        Assert.True(result.Value.Json);
        Assert.Equal("logs", result.Value.LogDirectory);
    }

    [Theory]
    [InlineData()]
    [InlineData("remove")]
    [InlineData("analyze", "--unknown")]
    [InlineData("analyze", "--log")]
    [InlineData("analyze", "--verbose", "--verbose")]
    [InlineData("--json")]
    public void Invalid_input_has_a_useful_error(params string[] arguments)
    {
        var result = CommandLineParser.Parse(arguments);

        Assert.False(result.IsSuccess);
        Assert.False(string.IsNullOrWhiteSpace(result.Error));
    }

    [Theory]
    [InlineData("--help", CommandKind.Help)]
    [InlineData("--version", CommandKind.Version)]
    public void Informational_options_are_standalone(string option, CommandKind command)
    {
        var result = CommandLineParser.Parse([option]);

        Assert.True(result.IsSuccess);
        Assert.Equal(command, result.Value!.Command);
    }
}
