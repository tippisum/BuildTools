# BuildTools

A CSharp solution for custom building action.

## Features

* Use CSharp for implementing custom building actions, such as source file pre-processing and binary file manipulation.
* Built-in support for binary I/O.

## Requirements

* .NET Core 3.1
* [Microsoft.CodeAnalysis.CSharp.Scripting](https://www.nuget.org/packages/Microsoft.CodeAnalysis.CSharp.Scripting/)
* (Optional) [Mono.Cecil](https://www.nuget.org/packages/Mono.Cecil/)

## Build

* Use .NET Core SDK to build the project.

## Usage

1. Execute a command early in the build process to compile custom build scripts: `bldtl builtin csc -out <compiled module> scripts ...`
2. Execute commands at appropriate time for invoking actions in the compiled module: `bldtl <compiled module> <action> [options] ...`

## License

BuildTools is licensed under the AGPL v3 License.
