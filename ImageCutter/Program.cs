using CommandLine;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ImageCutter
{
    class Options
    {
        [Option('d', Required = true, HelpText = "Directory to be processed.")]
        public string Directory { get; set; }

        // Omitting long name, defaults to name of property, ie "--verbose"
        [Option(Default = false, HelpText = "Prints all messages to standard output.")]
        public bool Verbose { get; set; }

        [Option('l', Required = false, Default = null)]
        public int? Left { get; set; }

        [Option('r', Required = false, Default = null)]
        public int? Right { get; set; }

        [Option('t', Required = false, Default = null)]
        public int? Top { get; set; }

        [Option('b', Required = false, Default = null)]
        public int? Bottom { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(RunOptions);
        }

        private static void RunOptions(Options options)
        {
            var files = new HashSet<string>();

            Log($"Processing {options.Directory}.");

            if (Directory.Exists(options.Directory))
            {
                foreach (var file in GetFiles(options.Directory,
                    @"\.jpg|\.jpeg|\.png|\.bmp", SearchOption.AllDirectories))
                {
                    files.Add(file);
                }
            }
            else
            {
                Error($"File or directory {options.Directory} not exists.");
            }

            Log($"Found total {files.Count} files.");

            if (options.Verbose)
            {
                foreach (var file in files)
                {
                    Log(file);
                }
            }

            Parallel.ForEach(files, s =>
            {
                if (options.Verbose)
                {
                    Log($"Processing {s}.");
                }

                var fileInfo = new FileInfo(s);
                var newFile = Path.Combine(fileInfo.DirectoryName, "cut_" + fileInfo.Name);

                using var inputStream = File.OpenRead(fileInfo.FullName);
                using var outputStream = File.OpenWrite(newFile);

                using var image = Image.Load(inputStream, out var format);

                var x = options.Left ?? 0;
                var y = options.Top ?? 0;

                var width = (options.Right ?? image.Width) - x;
                var height = (options.Bottom ?? image.Height) - y;

                image.Mutate(c =>
                    c.Crop(new Rectangle(x, y, width, height)));
                image.Save(outputStream, format);

                if (options.Verbose)
                {
                    Log($"Processing {s} OK.");
                }
            });
        }

        private static void Log(string message)
        {
            Console.WriteLine($"Info - {DateTime.Now:HH:mm:ss} - {message}");
        }

        private static void Error(string message)
        {
            Console.WriteLine($"Error - {DateTime.Now:HH:mm:ss} - {message}");
        }

        public static IEnumerable<string> GetFiles(string path,
            string searchPatternExpression = "",
            SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            Regex reSearchPattern = new Regex(searchPatternExpression, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            return Directory.EnumerateFiles(path, "*", searchOption)
                .Where(file =>
                    reSearchPattern.IsMatch(Path.GetExtension(file)));
        }

        // Takes same patterns, and executes in parallel
        public static IEnumerable<string> GetFiles(string path,
            string[] searchPatterns,
            SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            return searchPatterns.AsParallel()
                .SelectMany(searchPattern =>
                    Directory.EnumerateFiles(path, searchPattern, searchOption));
        }
    }
}
