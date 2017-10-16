using System;

namespace NJpegOptim
{
    [Flags]
    public enum StripProperty
    {
        NotSpecified = 0,
        All,
        None,
        Comments,
        Exif,
        Iptc,
        Icc,
        Xmp
    }
}