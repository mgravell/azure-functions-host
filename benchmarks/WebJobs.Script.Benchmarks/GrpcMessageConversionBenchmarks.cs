// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Google.Protobuf.Collections;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Azure.WebJobs.Script.Grpc;
using Microsoft.Azure.WebJobs.Script.Grpc.Messages;
using Microsoft.Azure.WebJobs.Script.Workers.Rpc;
using System.Linq;

namespace Microsoft.Azure.WebJobs.Script.Benchmarks
{
    public class GrpcMessageConversionBenchmarks
    {
        private static byte[] _byteArray = new byte[2000];
        private static string _str = new string('-', 2000);
        private static double _dbl = 2000;
        private static byte[][] _byteJaggedArray = new byte[1000][];
        private static string[] _strArray = new string[]{ new string('-', 1000), new string('-', 1000) };
        private static double[] _dblArray = new double[1000];
        private static long[] _longArray = new long[1000];
        private static JObject _jObj = JObject.Parse(@"{'name': 'lilian'}");
        internal GrpcCapabilities grpcCapabilities = new GrpcCapabilities(NullLogger.Instance);

        // Not easy to benchmark
        // public static HttpRequest _httpRequest;

        [Benchmark]
        public Task ToRpc_Null() => InvokeToRpc((object)null);
        [Benchmark]
        public Task ToRpc_Null2() => InvokeToRpc2((string)null);

        [Benchmark]
        public Task ToRpc_ByteArray() => InvokeToRpc(_byteArray);

        [Benchmark]
        public Task ToRpc_ByteArray2() => InvokeToRpc2(_byteArray);

        [Benchmark]
        public Task ToRpc_String() => InvokeToRpc(_str);
        [Benchmark]
        public Task ToRpc_String2() => InvokeToRpc2(_str);

        [Benchmark]
        public Task ToRpc_Double() => InvokeToRpc(_dbl);
        [Benchmark]
        public Task ToRpc_Double2() => InvokeToRpc2(_dbl);

        [Benchmark]
        public Task ToRpc_ByteJaggedArray() => InvokeToRpc(_byteJaggedArray);
        [Benchmark]
        public Task ToRpc_ByteJaggedArray2() => InvokeToRpc2(_byteJaggedArray);

        [Benchmark]
        public Task ToRpc_StringArray() => InvokeToRpc(_strArray);
        [Benchmark]
        public Task ToRpc_StringArray2() => InvokeToRpc2(_strArray);

        [Benchmark]
        public Task ToRpc_DoubleArray() => InvokeToRpc(_dblArray);
        [Benchmark]
        public Task ToRpc_DoubleArray2() => InvokeToRpc2(_dblArray);

        [Benchmark]
        public Task ToRpc_LongArray() => InvokeToRpc(_longArray);
        [Benchmark]
        public Task ToRpc_LongArray2() => InvokeToRpc2(_longArray);

        [Benchmark]
        public Task ToRpc_JObject() => InvokeToRpc(_jObj);
        [Benchmark]
        public Task ToRpc_JObject2() => InvokeToRpc2(_jObj);

        public Task InvokeToRpc(object obj) => obj.ToRpc(NullLogger.Instance, grpcCapabilities).AsTask();

        public Task InvokeToRpc2(byte[] value) => value.ToRpc2(NullLogger.Instance, grpcCapabilities).AsTask();
        public Task InvokeToRpc2(string value) => value.ToRpc2(NullLogger.Instance, grpcCapabilities).AsTask();
        public Task InvokeToRpc2(double value) => value.ToRpc2(NullLogger.Instance, grpcCapabilities).AsTask();
        public Task InvokeToRpc2(string[] value) => value.ToRpc2(NullLogger.Instance, grpcCapabilities).AsTask();
        public Task InvokeToRpc2(double[] value) => value.ToRpc2(NullLogger.Instance, grpcCapabilities).AsTask();
        public Task InvokeToRpc2(long[] value) => value.ToRpc2(NullLogger.Instance, grpcCapabilities).AsTask();
        public Task InvokeToRpc2(JObject value) => value.ToRpc2(NullLogger.Instance, grpcCapabilities).AsTask();
        public Task InvokeToRpc2(byte[][] value) => value.ToRpc2(NullLogger.Instance, grpcCapabilities).AsTask();

        [GlobalSetup]
        public void Setup()
        {
            MapField<string, string> addedCapabilities = new MapField<string, string>
            {
                { RpcWorkerConstants.TypedDataCollection, "1" }
            };
            grpcCapabilities.UpdateCapabilities(addedCapabilities);
        }
    }

    static class PbNetExtensions
    {
        public static ValueTask<Grpc.Messages2.TypedData> ToRpc2(this byte[] value, ILogger logger, GrpcCapabilities capabilities)
            => new(value is null ? new Grpc.Messages2.TypedData() : new Grpc.Messages2.TypedData { Bytes = value });
        public static ValueTask<Grpc.Messages2.TypedData> ToRpc2(this double value, ILogger logger, GrpcCapabilities capabilities)
            => new(new Grpc.Messages2.TypedData { Double = value });
        public static ValueTask<Grpc.Messages2.TypedData> ToRpc2(this string value, ILogger logger, GrpcCapabilities capabilities)
            => new(value is null ? new Grpc.Messages2.TypedData() : new Grpc.Messages2.TypedData { String = value });
        public static ValueTask<Grpc.Messages2.TypedData> ToRpc2(this JObject value, ILogger logger, GrpcCapabilities capabilities)
            => new(value is null ? new Grpc.Messages2.TypedData() : new Grpc.Messages2.TypedData { Json = value.ToString(Formatting.None) });
        public static ValueTask<Grpc.Messages2.TypedData> ToRpc2(this string[] value, ILogger logger, GrpcCapabilities capabilities)
            => new(value is null ? new Grpc.Messages2.TypedData() : new Grpc.Messages2.TypedData { CollectionString = new Grpc.Messages2.CollectionString { Strings = value } });
        public static ValueTask<Grpc.Messages2.TypedData> ToRpc2(this double[] value, ILogger logger, GrpcCapabilities capabilities)
            => new(value is null ? new Grpc.Messages2.TypedData() : new Grpc.Messages2.TypedData { CollectionDouble = new Grpc.Messages2.CollectionDouble { Doubles = value } });
        public static ValueTask<Grpc.Messages2.TypedData> ToRpc2(this long[] value, ILogger logger, GrpcCapabilities capabilities)
            => new(value is null ? new Grpc.Messages2.TypedData() : new Grpc.Messages2.TypedData { CollectionSint64 = new Grpc.Messages2.CollectionSInt64 { Sint64s = value } });
        public static ValueTask<Grpc.Messages2.TypedData> ToRpc2(this byte[][] value, ILogger logger, GrpcCapabilities capabilities)
            => new(value is null ? new Grpc.Messages2.TypedData() : new Grpc.Messages2.TypedData { CollectionBytes = new Grpc.Messages2.CollectionBytes { Bytes = value } });
    }
}
