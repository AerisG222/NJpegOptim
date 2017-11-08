using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Medallion.Shell;


namespace NJpegOptim
{
    public class JpegOptim
    {
        const int NUM_OUTPUT_FIELDS = 8;


        public Options Options { get; private set; }


        public JpegOptim(Options options)
        {
            Options = options ?? throw new ArgumentNullException(nameof(options));
        }


        public async Task<Result> RunAsync(string srcPath)
        {
            if(!File.Exists(srcPath))
            {
                throw new FileNotFoundException("Please make sure the image exists.", srcPath);
            }

            var args = Options.GetArguments(srcPath);

            var results = await RunProcessAsync(args, null).ConfigureAwait(false);

            return results[0];
        }


        public Task<IList<Result>> RunAsync(string[] srcFiles)
        {
            if(srcFiles == null || srcFiles.Length == 0)
            {
                throw new Exception("No files specified to process.");
            }

            var args = Options.GetArguments(srcFiles);

            return RunProcessAsync(args, null);
        }


        public async Task<Result> RunAsync(Stream infile)
        {
            if(infile == null)
            {
                throw new ArgumentNullException(nameof(infile));
            }

            var args = Options.GetArguments(infile);

            var results = await RunProcessAsync(args, infile).ConfigureAwait(false);

            return results[0];
        }


        async Task<IList<Result>> RunProcessAsync(string[] args, Stream infile)
        {
            Command cmd = null;
            MemoryStream ms = null;

            try
            {
                if(infile == null)
                {
                    if(Options.OutputToStream)
                    {
                        ms = new MemoryStream();
                        cmd = Command.Run(Options.JpegOptimPath, args) > ms;
                    }
                    else
                    {
                        cmd = Command.Run(Options.JpegOptimPath, args);
                    }
                }
                else
                {
                    ms = new MemoryStream();
                    cmd = Command.Run(Options.JpegOptimPath, args) < infile > ms;
                }

                await cmd.Task.ConfigureAwait(false);

                if(ms != null)
                {
                    ms.Seek(0, SeekOrigin.Begin);

                    var result = ParseOutput(cmd.StandardError.GetLines());
                    result[0].OutputStream = ms;

                    return result;
                }
                else
                {
                    return ParseOutput(cmd.StandardOutput.GetLines());
                }
            }
            catch (Win32Exception ex)
            {
                throw new Exception("Error when trying to start the jpegoptim process.  Please make sure it is installed, and its path is properly specified in the options.", ex);
            }
        }


        IList<Result> ParseOutput(IEnumerable<string> lines)
        {
            var results = new List<Result>();

            foreach(var line in lines)
            {
                var arr = line.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                if(arr.Length < NUM_OUTPUT_FIELDS) {
                    // TODO:
                    // I've seen partial output under the following conditions:
                    //   - if a file does not get optimized
                    //   - if the file already exists in the destination folder w/o specifying overwrite
                    // Document this or find a better way to capture this
                    results.Add(new Result {
                        ErrorLine = line
                    });
                }

                results.Add(new Result {
                    Success = true,
                    WasOptimized = string.Equals(arr[arr.Length - 1], "optimized", StringComparison.OrdinalIgnoreCase),
                    PercentImprovement = float.TryParse(arr[arr.Length - 2], out float pi) ? 0 : pi,
                    OptimizedSize = int.Parse(arr[arr.Length - 3]),
                    SourceSize = int.Parse(arr[arr.Length - 4]),
                    NormalOrProgressive = arr[arr.Length - 5],
                    ColorDepth = arr[arr.Length - 6],
                    Resolution = arr[arr.Length - 7],
                    SourceFile = arr.Length == NUM_OUTPUT_FIELDS ? arr[0] : GetFilename(arr)
                });
            }

            return results;
        }


        // jpegoptim does not seem to escape commas, so we rebuild the file name here in the case of commas
        string GetFilename(string[] arr)
        {
            var sb = new StringBuilder();

            for(var i = 0; i + NUM_OUTPUT_FIELDS <= arr.Length; i++)
            {
                if(i > 0)
                {
                    sb.Append(',');
                }

                sb.Append(arr[i]);
            }

            return sb.ToString();
        }
    }
}
