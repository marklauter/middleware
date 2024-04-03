﻿using Amazon.Lambda.Core;

namespace Sample.AWSLambda.APIGateway;

// This is a sample context that combines the api gateway event and the Lambda context so they can be passed together to request delegates in the pipeline.
internal sealed record APIGatewayProxyContext(
    string Input,
    ILambdaContext LambdaContext);
