using System;
using System.Collections.Generic;

namespace NJpegOptim;

public class Options
{
    public string JpegOptimPath { get; set; } = "jpegoptim";
    public string DestinationFolder { get; set; }
    public bool Force { get; set; }
    public short? MaxQuality { get; set; }
    public bool NoAction { get; set; }
    public int? TargetSize { get; set; }
    public TargetSizeUnit TargetSizeUnit { get; set; }
    public short? ThresholdPercent { get; set; }
    public bool OverwriteInDestinationFolder { get; set; }
    public bool PreserveFileTimestamps { get; set; }
    public bool PreservePermissions { get; set; }
    public StripProperty StripProperties { get; set; }
    public ProgressiveMode ProgressiveMode { get; set; }

    public string[] GetArguments(string sourceFile, bool outputToStream)
    {
        var args = GetArgs();

        if(string.IsNullOrWhiteSpace(sourceFile))
        {
            args.Add("--stdin");
        }
        else
        {
            args.Add(sourceFile);
        }

        if(outputToStream)
        {
            args.Add("--stdout");
        }

        return args.ToArray();
    }

    public string[] GetArguments(string[] filenames)
    {
        var args = GetArgs();

        args.AddRange(filenames);

        return args.ToArray();
    }

    List<string> GetArgs()
    {
        var args = new List<string>();

        args.Add("-b");

        if(!string.IsNullOrWhiteSpace(DestinationFolder))
        {
            args.Add($"-d{DestinationFolder}");

            if(OverwriteInDestinationFolder)
            {
                args.Add("-o");
            }
        }

        if(Force)
        {
            args.Add("-f");
        }

        if(MaxQuality != null) {
            if(MaxQuality < 0)
            {
                throw new InvalidOperationException($"{nameof(MaxQuality)} must be >= 0");
            }
            if(MaxQuality > 100)
            {
                throw new InvalidOperationException($"{nameof(MaxQuality)} must be <= 100");
            }

            args.Add($"-m{MaxQuality}");
        }

        if(NoAction)
        {
            args.Add("-n");
        }

        if(TargetSize != null)
        {
            switch(TargetSizeUnit)
            {
                case TargetSizeUnit.Kilobytes:
                    args.Add($"-S{TargetSize}");
                    break;
                case TargetSizeUnit.Percent:
                    args.Add($"-S{TargetSize}%");
                    break;
                default:
                    throw new InvalidOperationException($"{nameof(TargetSizeUnit)} must be specified");
            }
        }

        if(ThresholdPercent != null)
        {
            args.Add($"-T{ThresholdPercent}");
        }

        if(PreserveFileTimestamps)
        {
            args.Add("-p");
        }

        if(PreservePermissions)
        {
            args.Add("-P");
        }

        if(StripProperties != StripProperty.NotSpecified)
        {
            if(StripProperties.HasFlag(StripProperty.None))
            {
                args.Add("--strip-none");
            }
            else if(StripProperties.HasFlag(StripProperty.All))
            {
                args.Add("-s");
            }
            else
            {
                if(StripProperties.HasFlag(StripProperty.Comments))
                {
                    args.Add("--strip-com");
                }
                if(StripProperties.HasFlag(StripProperty.Exif))
                {
                    args.Add("--strip-exif");
                }
                if(StripProperties.HasFlag(StripProperty.Icc))
                {
                    args.Add("--strip-icc");
                }
                if(StripProperties.HasFlag(StripProperty.Iptc))
                {
                    args.Add("--strip-iptc");
                }
                if(StripProperties.HasFlag(StripProperty.Xmp))
                {
                    args.Add("--strip-xmp");
                }
            }
        }

        if(ProgressiveMode != ProgressiveMode.NotSpecified)
        {
            if(ProgressiveMode == ProgressiveMode.ForceNormal)
            {
                args.Add("--all-normal");
            }
            if(ProgressiveMode == ProgressiveMode.ForceProgressive)
            {
                args.Add("--all-progressive");
            }
        }

        return args;
    }
}
