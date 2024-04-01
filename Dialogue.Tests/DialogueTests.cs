using Microsoft.Extensions.DependencyInjection;

namespace Dialogue.Tests;

public class DialogueTests
{
    [Fact]
    public async Task HandleRequestWithNoUserDefinedMiddlewareAsync()
    {
        var request = "Hello, World!";

        var handler = RequestHandlerBuilder.New<string, string>()
            .Build();

        var response = await handler.InvokeAsync(request);

        Assert.True(String.IsNullOrEmpty(response));
    }

    [Fact]
    public async Task HandleRequestWithMiddlewareAsync()
    {
        var request = "Hello, World!";

        var handler = RequestHandlerBuilder.New<string, string>()
            .Build()
            .Use(async (context, next) =>
            {
                context.Response = context.Request.ToUpperInvariant();
                await next(context);
            })
            .Prepare();

        var response = await handler.InvokeAsync(request);

        Assert.Equal(request.ToUpperInvariant(), response);
    }

    [Fact]
    public async Task HandleRequestLastMiddlewareWinsAsync()
    {
        var request = "Hello, World!";

        var handler = RequestHandlerBuilder.New<string, string>()
            .Build()
            .Use(async (context, next) =>
            {
                context.Response = context.Request.ToUpperInvariant();
                await next(context);
            })
            .Use(async (context, next) =>
            {
                context.Response = context.Request.ToLowerInvariant();
                await next(context);
            })
            .Prepare();

        var response = await handler.InvokeAsync(request);

        Assert.Equal(request.ToLowerInvariant(), response);
    }

    internal sealed class ToLowerMiddleware(Handler<string, string> next)
        : IMiddleware<string, string>
    {
        public Handler<string, string> Next { get; } = next
            ?? throw new ArgumentNullException(nameof(next));

        public Task InvokeAsync(Context<string, string> context)
        {
            context.Response = context.Request.ToLowerInvariant();
            return next(context);
        }
    }

    [Fact]
    public async Task HandleRequestWithMiddlewareClassAsync()
    {
        var request = "Hello, World!";

        var handler = RequestHandlerBuilder.New<string, string>()
            .Build()
            .Use<ToLowerMiddleware>()
            .Prepare();

        var response = await handler.InvokeAsync(request);

        Assert.Equal(request.ToLowerInvariant(), response);
    }

    internal sealed class CtorMiddleware(Handler<string, string> next)
        : IMiddleware<string, string>
    {
        public Handler<string, string> Next { get; } = next
            ?? throw new ArgumentNullException(nameof(next));

        public Task InvokeAsync(Context<string, string> context)
        {
            context.Response = context.Request.ToLowerInvariant();
            return next(context);
        }
    }

    /// <summary>
    /// prototype for constructing middleware with dependency injection
    /// </summary>
    [Fact]
    public async Task GetMiddlewareCtorAsync()
    {
        Handler<string, string> next = context =>
            context.CancellationToken.IsCancellationRequested
                ? Task.FromCanceled<Context<string, string>>(context.CancellationToken)
                : Task.FromResult(context);

        using var services = new ServiceCollection().BuildServiceProvider();

        // uses the service provider to create all arguments other than the next delegate
        var middleware = ActivatorUtilities.CreateInstance<CtorMiddleware>(services, next);
        var request = "Hello, World!";
        var context = new Context<string, string>(
            request,
            Ulid.NewUlid(),
            DateTime.UtcNow,
            services,
            CancellationToken.None);
        await middleware.InvokeAsync(context);

        Assert.Equal(request.ToLowerInvariant(), context.Response);
    }
}
