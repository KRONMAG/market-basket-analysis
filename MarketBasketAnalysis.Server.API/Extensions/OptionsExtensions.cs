using Grpc.AspNetCore.Server;
using System.IO.Compression;

namespace MarketBasketAnalysis.Server.API.Extensions;

public static class OptionsExtensions
{
    public static void ConfigureGrpc(this GrpcServiceOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.ResponseCompressionLevel = CompressionLevel.Optimal;
    }
}
