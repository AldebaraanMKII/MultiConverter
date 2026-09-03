using MultiConverterLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MultiConverterCLI
{
    public static class Program
    {
        private const string Name = "MultiConverterCLI";
        private const string Version = "1.0.0";

        private const int ExitOk = 0;
        private const int ExitFailure = 1;
        private const int ExitUsage = 2;

        public static int Main(string[] args)
        {
            ConverterOptions options = new ConverterOptions();
            List<string> paths = new List<string>();
            bool showHelp = false;
            bool showVersion = false;
            bool parseError = false;

            foreach (string arg in args)
            {
                switch (arg)
                {
                    case "-h":
                    case "--help":
                        showHelp = true;
                        break;
                    case "--version":
                        showVersion = true;
                        break;
                    case "--helm":
                        options.FixHelm = true;
                        break;
                    case "--no-helm":
                        options.FixHelm = false;
                        break;
                    case "--liquids":
                        options.FixLiquids = true;
                        break;
                    case "--no-liquids":
                        options.FixLiquids = false;
                        break;
                    case "--models":
                        options.FixModels = true;
                        break;
                    case "--no-models":
                        options.FixModels = false;
                        break;
                    case "--wod":
                        options.Wod = true;
                        break;
                    default:
                        if (arg.StartsWith("-"))
                        {
                            Console.Error.WriteLine("error: unknown option '" + arg + "'");
                            parseError = true;
                        }
                        else
                        {
                            paths.Add(arg);
                        }
                        break;
                }
            }

            if (showVersion)
            {
                Console.WriteLine(Name + " " + Version);
                return ExitOk;
            }

            if (parseError)
            {
                PrintHelp(Console.Error);
                return ExitUsage;
            }

            if (showHelp || paths.Count == 0)
            {
                if (!showHelp)
                {
                    Console.Error.WriteLine("error: no input file or directory given");
                }
                PrintHelp(showHelp ? Console.Out : Console.Error);
                return showHelp ? ExitOk : ExitUsage;
            }

            foreach (string path in paths)
            {
                if (!File.Exists(path) && !Directory.Exists(path))
                {
                    Console.Error.WriteLine("error: file or directory not found: " + path);
                    return ExitUsage;
                }
            }

            List<string> files = ConverterRunner.GatherFiles(paths);
            files.Sort(StringComparer.OrdinalIgnoreCase);

            Console.WriteLine("Found " + files.Count + " file" + (files.Count == 1 ? "" : "s") + " to convert.");
            if (options.Wod)
            {
                Console.WriteLine("WMO mode: Legion to WoD (root WMO files will be skipped).");
            }

            int failed = 0;
            foreach (string file in files)
            {
                ConvertResult result = ConverterRunner.Convert(file, options);
                switch (result.Status)
                {
                    case ConvertStatus.Ok:
                        Console.WriteLine("[OK]   " + file);
                        break;
                    case ConvertStatus.Skipped:
                        Console.WriteLine("[SKIP] " + file + " (nothing to do)");
                        break;
                    case ConvertStatus.Failed:
                        Console.Error.WriteLine("[FAIL] " + file + ": " + result.Error);
                        failed++;
                        break;
                }
            }

            Console.WriteLine("Done. " + (files.Count - failed) + " file" + ((files.Count - failed) == 1 ? "" : "s") + " converted, " + failed + " failed.");
            return failed > 0 ? ExitFailure : ExitOk;
        }

        private static void PrintHelp(TextWriter writer)
        {
            writer.WriteLine(Name + " " + Version + " - Shadowlands to Wotlk model converter (command line).");
            writer.WriteLine();
            writer.WriteLine("Usage: " + Name + " [options] <file-or-directory> [<file-or-directory> ...]");
            writer.WriteLine();
            writer.WriteLine("Each file is converted in place. Directories are scanned recursively and only");
            writer.WriteLine("supported files (m2, anim, wmo, wdt and adt tiles) are processed. Note that");
            writer.WriteLine("ADT conversion removes the split _obj/_tex files once merged.");
            writer.WriteLine();
            writer.WriteLine("Options:");
            writer.WriteLine("  --helm / --no-helm         Fix helm offset on M2 files (default: --helm)");
            writer.WriteLine("  --liquids / --no-liquids   Fix liquids on ADT files (default: --liquids)");
            writer.WriteLine("  --models / --no-models     Fix models on ADT files (default: --models)");
            writer.WriteLine("  --wod                      WMO: convert Legion to WoD group files (default: off)");
            writer.WriteLine("  -h, --help                 Show this help and exit");
            writer.WriteLine("  --version                  Show version and exit");
            writer.WriteLine();
            writer.WriteLine("Exit codes:");
            writer.WriteLine("  0  all files converted");
            writer.WriteLine("  1  one or more files failed");
            writer.WriteLine("  2  usage error");
        }
    }
}
