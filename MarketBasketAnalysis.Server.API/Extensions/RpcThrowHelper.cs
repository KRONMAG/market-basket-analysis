using Grpc.Core;
using System.Diagnostics.CodeAnalysis;

namespace MarketBasketAnalysis.Server.API.Extensions;

public static class RpcThrowHelper
{
    [DoesNotReturn]
    public static void InvalidArgument(string detail) =>
        ThrowRpcException(StatusCode.InvalidArgument, detail);

    [DoesNotReturn]
    public static void Internal(string detail) =>
        ThrowRpcException(StatusCode.Internal, detail);

    [DoesNotReturn]
    public static void NotFound(string detail) =>
        ThrowRpcException(StatusCode.NotFound, detail);

    [DoesNotReturn]
    private static void ThrowRpcException(StatusCode statusCode, string detail) =>
        throw new RpcException(new Status(statusCode, detail));
}