using MultiConverter.Lib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace MultiConverterLib
{
    /// <summary>
    /// Options mirroring the ones exposed by the GUI.
    /// </summary>
    public class ConverterOptions
    {
        public bool FixHelm = true;
        public bool FixLiquids = true;
        public bool FixModels = true;
        public bool Wod = false;
    }

    public enum ConvertStatus
    {
        Ok,
        Skipped,
        Failed
    }

    public struct ConvertResult
    {
        public ConvertStatus Status;
        public string Error;
    }

    /// <summary>
    /// Conversion logic shared between the GUI and the CLI.
    /// This replicates the per-file dispatch of the GUI's FixList method.
    /// </summary>
    public static class ConverterRunner
    {
        private static bool listfileInitialized;

        /// <summary>
        /// Enumerate all the supported files from the given files and/or directories.
        /// Directories are scanned recursively.
        /// </summary>
        public static List<string> GatherFiles(IEnumerable<string> paths)
        {
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            List<string> files = new List<string>();

            foreach (string s in paths)
            {
                if (Directory.Exists(s))
                {
                    foreach (string file in Directory.EnumerateFiles(s, "*.*", SearchOption.AllDirectories))
                    {
                        if (Utils.IsCorrectFile(file) && seen.Add(file))
                        {
                            files.Add(file);
                        }
                    }
                }
                else if (Utils.IsCorrectFile(s) && seen.Add(s))
                {
                    files.Add(s);
                }
            }

            return files;
        }

        /// <summary>
        /// Create the converter matching the file.
        /// Returns null when there is nothing to do for this file (unsupported format
        /// or WMO root file in WoD mode).
        /// </summary>
        public static IConverter? CreateConverter(string file, ConverterOptions options)
        {
            IConverter? converter = null;

            if (EndsWith(file, ".m2"))
            {
                InitializeListfile();
                converter = new M2Converter(file, options.FixHelm);
            }
            else if (EndsWith(file, ".adt"))
            {
                converter = new AdtConverter(file, options.FixLiquids, options.FixModels);
            }
            else if (EndsWith(file, ".wdt"))
            {
                converter = new WDTConverter(file);
            }
            else if (Regex.IsMatch(file, @".*_[0-9]{3}(_(lod[0-9]))?\.(wmo)", RegexOptions.IgnoreCase))
            {
                converter = new WMOGroupConverter(file, options.Wod);
            }
            else if (EndsWith(file, ".wmo"))
            {
                if (options.Wod)
                {
                    return null; // nothing to do
                }
                converter = new WMORootConverter(file);
            }
            else if (EndsWith(file, ".anim"))
            {
                converter = new AnimConverter(file);
            }

            return converter;
        }

        /// <summary>
        /// Convert a single file. Exceptions are caught and reported in the result.
        /// </summary>
        public static ConvertResult Convert(string file, ConverterOptions options)
        {
            IConverter? converter;
            try
            {
                converter = CreateConverter(file, options);
            }
            catch (Exception e)
            {
                return new ConvertResult { Status = ConvertStatus.Failed, Error = e.Message };
            }

            if (converter == null)
            {
                return new ConvertResult { Status = ConvertStatus.Skipped };
            }

            try
            {
                if (converter.Fix())
                {
                    converter.Save();
                }
                return new ConvertResult { Status = ConvertStatus.Ok };
            }
            catch (Exception e)
            {
                return new ConvertResult { Status = ConvertStatus.Failed, Error = e.Message };
            }
        }

        private static bool EndsWith(string s, string value)
        {
            return s.Length >= value.Length && s.EndsWith(value, StringComparison.OrdinalIgnoreCase);
        }

        private static void InitializeListfile()
        {
            if (listfileInitialized)
            {
                return;
            }
            listfileInitialized = true;

            try
            {
                if (!Listfile.IsInitialized)
                {
                    Listfile.Initialize();
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("warning: could not initialize the listfile (" + e.Message + ")");
            }
        }
    }
}
