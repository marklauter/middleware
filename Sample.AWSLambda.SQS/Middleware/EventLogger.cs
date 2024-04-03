﻿using Dialogue;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Sample.AWSLambda.SQS.Middleware;

// This sample event logger is similar to the Serilog web request logger that can be used in ASP.NET Core.
// Register the event logger ahead of the other middleware components.
internal sealed class EventLogger(
    Handler<SQSEventContext, Dialogue.Void> next,
    ILogger<EventLogger> logger)
    : IMiddleware<SQSEventContext, Dialogue.Void>
{
    private readonly ILogger<EventLogger> logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public Handler<SQSEventContext, Dialogue.Void> Next { get; } = next ?? throw new ArgumentNullException(nameof(next));

    public async Task InvokeAsync(RequestContext<SQSEventContext, Dialogue.Void> context)
    {
        using var logscope = logger.BeginScope(Guid.NewGuid().ToString());

        var timer = Stopwatch.StartNew();
        try
        {
            // always check for cancellation
            context.CancellationToken.ThrowIfCancellationRequested();
            await Next(context); // have to await because of the using block started on line 20
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "error processing SQS event");
            throw;
        }
        finally
        {
            logger.LogInformation("{MessageCount} SQS messages processed in {ElapsedMilliseconds}ms", context.Request.SQSEvent.Records.Count, timer.ElapsedMilliseconds);
        }
    }
}
