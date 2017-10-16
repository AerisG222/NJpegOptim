[![MIT licensed](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/AerisG222/NJpegOptim/blob/master/LICENSE.md)
[![NuGet](https://buildstats.info/nuget/NJpegOptim)](https://www.nuget.org/packages/NJpegOptim/)
[![Travis](https://img.shields.io/travis/AerisG222/NJpegOptim.svg)](https://travis-ci.org/AerisG222/NJpegOptim)
[![Coverity Scan](https://img.shields.io/coverity/scan/14029.svg)](https://scan.coverity.com/projects/aerisg222-njpegoptim)

# NJpegOptim

A .Net library to wrap the functionality of jpegoptim.  Please note that by default
jpegoptim modifies the original input files.  If you do not want that behavior,
please look at the option to set the destination folder.

## Motivation
To create a simple wrapper to allow .Net applications to easily use this program.

## Using
- Install jpegoptim
- Add a package reference to NJpegOptim in your project
- Bring down the packages for your project via `dotnet restore`

Use it:

````c#
var opts = new Options {
    MaxQuality = 72,
    StripProperties = StripProperty.All
};

var jo = new JpegOptim(opts);
var result = await jo.RunAsync("photo.jpg");
````

## Contributing
I'm happy to accept pull requests.  By submitting a pull request, you
must be the original author of code, and must not be breaking
any laws or contracts.

Otherwise, if you have comments, questions, or complaints, please file
issues to this project on the github repo.
  
## License
NJpegOptim is licensed under the MIT license.  See LICENSE.md for more
information.

## Reference
- JpegOptim: [https://github.com/tjko/jpegoptim](https://github.com/tjko/jpegoptim)
