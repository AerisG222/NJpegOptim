using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace NJpegOptim;

public class JpegOptim
{
    const int NUM_OUTPUT_FIELDS = 8;

    readonly Options _options;

    public JpegOptim(Options options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public Task<Result> RunAsync(string srcPath)
    {
        ValidateSourceFile(srcPath);

        return InternalRunAsync(srcPath, null, null);
    }

    public Task<Result> RunAsync(string srcPath, Stream dstStream)
    {
        ValidateSourceFile(srcPath);
        ValidateDestinationStream(dstStream);

        return InternalRunAsync(srcPath, null, dstStream);
    }

    public Task<Result> RunAsync(Stream srcStream, Stream dstStream)
    {
        ValidateSourceStream(srcStream);
        ValidateDestinationStream(dstStream);

        return InternalRunAsync(null, srcStream, dstStream);
    }

    public Task<IList<Result>> RunAsync(string[] srcFiles)
    {
        if(srcFiles == null || srcFiles.Length == 0)
        {
            throw new Exception("No files specified to process.");
        }

        var args = _options.GetArguments(srcFiles);

        return RunProcessAsync(args, null, null);
    }

    async Task<Result> InternalRunAsync(string srcPath, Stream srcStream, Stream dstStream)
    {
        var args = _options.GetArguments(srcPath, dstStream != null);

        var results = await RunProcessAsync(args, srcStream, dstStream).ConfigureAwait(false);

        return results[0];
    }

    async Task<IList<Result>> RunProcessAsync(string[] args, Stream srcStream, Stream dstStream)
    {
        using var process = new Process();

        process.StartInfo.FileName = _options.JpegOptimPath;
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.StandardInputEncoding = Console.InputEncoding;
        process.StartInfo.StandardErrorEncoding = Console.OutputEncoding;
        process.StartInfo.StandardOutputEncoding = Console.OutputEncoding;
        process.StartInfo.RedirectStandardInput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.RedirectStandardOutput = true;

        foreach(var arg in args)
        {
            process.StartInfo.ArgumentList.Add(arg);
        }

        try
        {
            StringBuilder stdOut = null;

            process.Start();

            if(srcStream != null)
            {
                await srcStream.CopyToAsync(process.StandardInput.BaseStream).ConfigureAwait(false);
                await process.StandardInput.FlushAsync().ConfigureAwait(false);
                process.StandardInput.Close();
            }

            var stdErr = new StringBuilder();
            process.ErrorDataReceived += (sender, e) => RecordMeaningfulOutput(e.Data, stdErr);
            process.BeginErrorReadLine();

            if(dstStream == null)
            {
                stdOut = new StringBuilder();
                process.OutputDataReceived += (sender, e) => RecordMeaningfulOutput(e.Data, stdOut);
                process.BeginOutputReadLine();
            }
            else
            {
                await process.StandardOutput.BaseStream.CopyToAsync(dstStream).ConfigureAwait(false);
                await process.StandardOutput.BaseStream.FlushAsync().ConfigureAwait(false);
                process.StandardOutput.Close();
            }

            await process.WaitForExitAsync().ConfigureAwait(false);

            if(dstStream == null)
            {
                return ParseOutput(GetLines(stdOut.ToString()));
            }
            else
            {
                return ParseOutput(GetLines(stdErr.ToString()));
            }
        }
        catch (Win32Exception ex)
        {
            throw new Exception("Error when trying to start the jpegoptim process.  Please make sure it is installed, and its path is properly specified in the options.", ex);
        }
    }

    void RecordMeaningfulOutput(string output, StringBuilder buffer)
    {
        if(!string.IsNullOrWhiteSpace(output))
        {
            buffer.AppendLine(output);
        }
    }

    IEnumerable<string> GetLines(string input)
    {
        string line;
        var reader = new StringReader(input);

        while((line = reader.ReadLine()) != null)
        {
            yield return line;
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

    void ValidateSourceFile(string srcPath)
    {
        if(!File.Exists(srcPath))
        {
            throw new FileNotFoundException("Please make sure the image exists.", srcPath);
        }
    }

    void ValidateSourceStream(Stream sourceStream)
    {
        if(sourceStream == null)
        {
            throw new ArgumentNullException(nameof(sourceStream));
        }

        if(!sourceStream.CanRead)
        {
            throw new InvalidOperationException("Unable to read from source stream!");
        }
    }

    void ValidateDestinationStream(Stream dstStream)
    {
        if(dstStream == null)
        {
            throw new ArgumentNullException(nameof(dstStream));
        }

        if(!dstStream.CanWrite)
        {
            throw new InvalidOperationException("Unable to write to destination stream!");
        }
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
