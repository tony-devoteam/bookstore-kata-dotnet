using Bookstore.Api.Application;

namespace Bookstore.Tests;

internal sealed class TestBookIdGenerator : IBookIdGenerator
{
    private static int _counter;

    public string NewId() => Interlocked.Increment(ref _counter).ToString("x24");
}
