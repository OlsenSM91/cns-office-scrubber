using System.Text.Json;
using OfficeScrubber.Core;

namespace OfficeScrubber.Cli.Tests;

public sealed class AnalyzeCommandTests
{
    [Fact]
    public async Task Reports_privilege_as_json_through_read_only_abstraction()
    {
        var output = new StringWriter();
        var command = new AnalyzeCommand(new StubPrivilegeProvider(PrivilegeState.Elevated), output);

        var exitCode = await command.ExecuteAsync(
            new CommandLine(CommandKind.Analyze, Json: true), CancellationToken.None);

        Assert.Equal(ExitCodes.Success, exitCode);
        using var document = JsonDocument.Parse(output.ToString());
        Assert.Equal("elevated", document.RootElement.GetProperty("privilege").GetString());
        Assert.True(document.RootElement.GetProperty("elevated").GetBoolean());
    }

    [Fact]
    public async Task Honors_an_already_cancelled_token_before_querying()
    {
        var provider = new StubPrivilegeProvider(PrivilegeState.StandardUser);
        var command = new AnalyzeCommand(provider, TextWriter.Null);

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            command.ExecuteAsync(new CommandLine(CommandKind.Analyze), new CancellationToken(true)));

        Assert.Equal(0, provider.CallCount);
    }

    private sealed class StubPrivilegeProvider(PrivilegeState state) : IPrivilegeStateProvider
    {
        public int CallCount { get; private set; }

        public PrivilegeState GetCurrent()
        {
            CallCount++;
            return state;
        }
    }
}
