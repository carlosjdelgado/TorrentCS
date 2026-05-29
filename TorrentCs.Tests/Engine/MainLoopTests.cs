using TorrentCs.Engine;

namespace TorrentCs.Tests.Engine;

public class MainLoopTests
{
    [Fact]
    public void Start_IsRunning_ReturnsTrue()
    {
        var loop = new MainLoop();
        loop.Start();
        Assert.True(loop.IsRunning);
        loop.Stop();
    }

    [Fact]
    public void Stop_IsRunning_ReturnsFalse()
    {
        var loop = new MainLoop();
        loop.Start();
        loop.Stop();
        Thread.Sleep(50);
        Assert.False(loop.IsRunning);
    }

    [Fact]
    public async Task AddTask_ExecutesAction()
    {
        var loop = new MainLoop();
        loop.Start();

        var tcs = new TaskCompletionSource<bool>();
        loop.AddTask(() => tcs.TrySetResult(true));

        var completed = await Task.WhenAny(tcs.Task, Task.Delay(1000)) == tcs.Task;
        loop.Stop();

        Assert.True(completed);
    }

    [Fact]
    public async Task AddTask_ExecutesMultipleInOrder()
    {
        var loop = new MainLoop();
        loop.Start();

        var order = new List<int>();
        var tcs = new TaskCompletionSource<bool>();

        loop.AddTask(() => order.Add(1));
        loop.AddTask(() => order.Add(2));
        loop.AddTask(() => { order.Add(3); tcs.TrySetResult(true); });

        await Task.WhenAny(tcs.Task, Task.Delay(1000));
        loop.Stop();

        Assert.Equal([1, 2, 3], order);
    }

    [Fact]
    public async Task AddRegularTask_ExecutesPeriodically()
    {
        var loop = new MainLoop();
        loop.Start();

        int count = 0;
        var task = loop.AddRegularTask(() => Interlocked.Increment(ref count));

        await Task.Delay(350); // enough for 3 regular task ticks (100ms each)
        task.Dispose();
        loop.Stop();

        Assert.True(count >= 2);
    }

    [Fact]
    public async Task AddRegularTask_AfterDispose_StopsRunning()
    {
        var loop = new MainLoop();
        loop.Start();

        int count = 0;
        var task = loop.AddRegularTask(() => Interlocked.Increment(ref count));

        await Task.Delay(150);
        task.Dispose();
        int countAtDispose = count;
        await Task.Delay(200);

        loop.Stop();

        Assert.Equal(countAtDispose, count);
    }
}
