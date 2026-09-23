using PrTray.Core.GitHub;

namespace PrTray.Core.Tests;

internal sealed class FakeProcessRunner(Func<IReadOnlyList<string>, CancellationToken, Task<ProcessResult>> handler) : IProcessRunner
{
    public List<(string FileName, IReadOnlyList<string> Arguments)> Calls { get; } = [];

    public Task<ProcessResult> RunAsync(string fileName, IReadOnlyList<string> arguments, CancellationToken cancellationToken)
    {
        Calls.Add((fileName, arguments));
        return handler(arguments, cancellationToken);
    }

    public static FakeProcessRunner Returning(ProcessResult result) => new((_, _) => Task.FromResult(result));

    public static FakeProcessRunner ReturningInOrder(params ProcessResult[] results)
    {
        var queue = new Queue<ProcessResult>(results);
        return new FakeProcessRunner((_, _) => Task.FromResult(queue.Dequeue()));
    }

    public static ProcessResult Ok(string standardOutput) => new(0, standardOutput, "");
}
