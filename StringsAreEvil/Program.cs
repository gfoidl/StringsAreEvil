﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace StringsAreEvil
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            CultureInfo defaultCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
#if !NETCOREAPP
            AppDomain.MonitoringIsEnabled = true;
#endif
            var dict = new Dictionary<string, Func<Variant>>
            {
                ["1"] = () =>
                {
                    Console.WriteLine("#1 ViaStreamReader");
                    return new ViaStreamReader(new LineParserV01());
                },
                ["2"] = () =>
                {
                    Console.WriteLine("#2 ViaStreamReader");
                    return new ViaStreamReader(new LineParserV02());
                },
                ["3"] = () =>
                {
                    Console.WriteLine("#3 ViaStreamReader");
                    return new ViaStreamReader(new LineParserV03());
                },
                ["4"] = () =>
                {
                    Console.WriteLine("#4 ViaStreamReader");
                    return new ViaStreamReader(new LineParserV04());
                },
                ["5"] = () =>
                {
                    Console.WriteLine("#5 ViaStreamReader");
                    return new ViaStreamReader(new LineParserV05());
                },
                ["6"] = () =>
                {
                    Console.WriteLine("#6 ViaStreamReader");
                    return new ViaStreamReader(new LineParserV06());
                },
                ["7"] = () =>
                {
                    Console.WriteLine("#7 ViaStreamReader");
                    return new ViaStreamReader(new LineParserV07());
                },
                ["8"] = () =>
                {
                    Console.WriteLine("#8 ViaStreamReader");
                    return new ViaStreamReader(new LineParserV08());
                },
                ["9"] = () =>
                {
                    Console.WriteLine("#9 ViaStreamReader");
                    return new ViaStreamReader(new LineParserV09());
                },
                ["10"] = () =>
                {
                    Console.WriteLine("#10 ViaStreamReader");
                    return new ViaStreamReader(new LineParserV10());
                },
                ["11"] = () =>
                {
                    Console.WriteLine("#11 ViaRawStream");
                    return new ViaRawStream(new LineParserV11());
                },
                ["12"] = () =>
                {
                    Console.WriteLine("#12 ViaRawStream2");
                    return new ViaRawStream2(new LineParserV12());
                },
                ["13"] = () =>
                {
                    Console.WriteLine("#13 ViaPipeReader");
                    return new ViaPipeReader(new LineParserPipelinesAndSpan());
                },
#if NETCOREAPP
                ["14"] = () =>
                {
                    Console.WriteLine("#14 ViaPipeReader2");
                    return new ViaPipeReader2(new LineParserPipelinesAndSpan2());
                },
#endif
#if NETCOREAPP3_0
                ["15"] = () =>
                {
                    Console.WriteLine("#15 ViaPipeReader3");
                    return new ViaPipeReaderSequenceReader(new LineParserPipelinesAndSpan3());
                }
#endif
            };

#if NETCOREAPP
            var stopWatch = Stopwatch.StartNew();
#endif
            Func<Variant> variantFactory = null;
            string fileName = null;

            if (args.Length == 1 && dict.TryGetValue(args[0], out variantFactory))
            {
                fileName = "example-input.csv";
            }
            else if (args.Length == 2 && dict.TryGetValue(args[0], out variantFactory))
            {
                fileName = args[1];
            }
            else
            {
                Console.WriteLine("Incorrect parameters");
                Environment.Exit(1);
            }

            Variant variant = variantFactory();
            await variant.ParseAsync(fileName);
#if DEBUG
            variant.LineParser.Dump();
#endif

            Thread.CurrentThread.CurrentCulture = defaultCulture;

#if !NETCOREAPP
            Console.WriteLine($"Took: {AppDomain.CurrentDomain.MonitoringTotalProcessorTime.TotalMilliseconds:#,###} ms");
            Console.WriteLine($"Allocated: {AppDomain.CurrentDomain.MonitoringTotalAllocatedMemorySize / 1024:#,#} kb");
#else
            stopWatch.Stop();
            Console.WriteLine($"Took: {stopWatch.ElapsedMilliseconds:#,###} ms");
            Console.WriteLine("Allocated: --- no API available in .NET Core ---");
#endif
            Console.WriteLine($"Peak Working Set: {Process.GetCurrentProcess().PeakWorkingSet64 / 1024:#,#} kb");

            for (var index = 0; index <= GC.MaxGeneration; index++)
            {
                Console.WriteLine($"Gen {index} collections: {GC.CollectionCount(index)}");
            }

            Console.WriteLine($"\nparsed lines: {variant.LineParser.Count}");

            Console.WriteLine(Environment.NewLine);
            if (Debugger.IsAttached) Console.ReadKey();
        }
    }
}
