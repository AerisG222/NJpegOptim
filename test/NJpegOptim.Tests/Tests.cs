using System.Collections.Generic;
using System.IO;
using Xunit;

namespace NJpegOptim.Tests;

public class Tests
{
    static readonly List<string> Files = new List<string>() {
        "banner_lg.jpg",
        "DSC_3982.jpg",
        "DSC,t.jpg",
        "leonardo.jpg"
    };

    [Fact]
    public async void CanReportOptimizationForSingleFile()
    {
        var opts = new Options {
            NoAction = true
        };

        var jo = new JpegOptim(opts);
        var result = await jo.RunAsync(Files[0]);

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal("banner_lg.jpg", result.SourceFile);
        Assert.Equal("2160x600", result.Resolution);
        Assert.Equal("24bit", result.ColorDepth);
        Assert.Equal("P", result.NormalOrProgressive);
        Assert.Equal(139591, result.SourceSize);
        Assert.Equal(139591, result.OptimizedSize);
        Assert.Equal(0, result.PercentImprovement);
        Assert.False(result.WasOptimized);
    }

    [Fact]
    public async void CanReportForManyFiles()
    {
        var opts = new Options {
            NoAction = true
        };

        var jo = new JpegOptim(opts);
        var result = await jo.RunAsync(Files.ToArray());

        Assert.NotNull(result);
        Assert.InRange(result.Count, Files.Count, Files.Count);
    }

    [Fact]
    public async void CanProcessToNewDirectory()
    {
        var dir = "can_process_to_new_dir";
        var opts = new Options {
            DestinationFolder = dir,
            OverwriteInDestinationFolder = true,  // we use the same directory multiple times, must allow overwriting
            MaxQuality = 72  // ensure files will be optimized
        };

        Directory.CreateDirectory(dir);

        var jo = new JpegOptim(opts);
        var result = await jo.RunAsync(Files[0]);

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.True(result.WasOptimized);
        Assert.True(File.Exists(Path.Combine(dir, Files[0])));

        var result2 = await jo.RunAsync(Files.ToArray());
        Assert.NotNull(result);

        foreach(var res in result2)
        {
            if(res.Success)
            {
                if(res.WasOptimized)
                {
                    Assert.True(File.Exists(Path.Combine(dir, res.SourceFile)));
                }
                else
                {
                    Assert.False(File.Exists(Path.Combine(dir, res.SourceFile)));
                }
            }
        }
    }

    [Fact]
    void CheckAllCommandlineOptions()
    {
        var opts = new Options {
            DestinationFolder = "folder",
            Force = true,
            MaxQuality = 72,
            NoAction = true,
            TargetSize = 200,
            TargetSizeUnit = TargetSizeUnit.Kilobytes,
            ThresholdPercent = 4,
            OverwriteInDestinationFolder = true,
            PreserveFileTimestamps = true,
            PreservePermissions = true,
            StripProperties = StripProperty.All,
            ProgressiveMode = ProgressiveMode.ForceNormal
        };

        var args = opts.GetArguments("dummy.jpg");

        var argLine = string.Join(" ", args);

        Assert.Contains("-dfolder", argLine);
        Assert.Contains("-f", argLine);
        Assert.Contains("-m72", argLine);
        Assert.Contains("-n", argLine);
        Assert.Contains("-S200", argLine);
        Assert.Contains("-T4", argLine);
        Assert.Contains("-o", argLine);
        Assert.Contains("-p", argLine);
        Assert.Contains("-P", argLine);
        Assert.Contains("-s", argLine);
        Assert.Contains("--all-normal", argLine);
        Assert.Contains("dummy.jpg", argLine);
    }

    [Fact]
    async void StandardInputTest()
    {
        var opts = new Options {
            MaxQuality = 72,
            StripProperties = StripProperty.All
        };

        var jo = new JpegOptim(opts);

        var f = new FileStream(Files[0], FileMode.Open, FileAccess.Read, FileShare.Read);

        var result = await jo.RunAsync(f);

        Assert.NotNull(result);
        Assert.NotNull(result.OutputStream);
        Assert.Equal("stdin", result.SourceFile);
        Assert.Equal(91324, result.OptimizedSize);

        /*
        var fs = new FileStream("stream_test.jpg", FileMode.Create);
        result.OutputStream.CopyTo(fs);
        await fs.FlushAsync();
        fs.Close();
        */
    }
}
