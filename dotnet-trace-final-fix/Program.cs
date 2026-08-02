using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Diagnostics.Tools.Trace;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Etlx;

namespace Microsoft.Diagnostics.Tools.Trace;

class Program
{
    const string BASE_PATH = @"C:\Users\Daniel\Desktop\github\blog\dotnet-trace-final-fix\";

    static void Main(string[] args)
    {
            TraceFileFormatConverter.ConvertToFormat(
                Console.Out,
                Console.Error,
                TraceFileFormat.Chromium,
                "C:\\Users\\Daniel\\Desktop\\github\\blog\\dotnet-trace-final-fix\\finalboss\\dotnet_20260727_184408.nettrace",
                "C:\\Users\\Daniel\\Desktop\\github\\blog\\dotnet-trace-final-fix\\finalboss\\test.chromium.json",
                "Cmf*.Services.*Controller.*",
                "Cmf*"
            );
    }
}