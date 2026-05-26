# What is the .NET Ecosystem?

* made by Microsoft for building:
    * Desktop apps , Web apps , APIs , Mobile apps , Games , Cloud services , AI tools
* **Languages** → mainly C#, then F# or VB.NET
* **the CLR Runtime** → translates the IL (intermediate lang) + executes your program
* **built-in Libraries** → ready-made functionality
* **SDKs & dotnet CLI tools** → build, test, publish apps
* **Frameworks** → ASP.NET, MAUI, Entity Framework, Avalonia ..


# evolution on timeline

1. **.NET Framework** (old Windows-only era)
2. **.NET Core** (modern cross-platform reboot)
3. **Modern .NET (5/6/7/8+)** (unified platform)


# 1. .NET Framework

| Feature        | .NET Framework       |
| OS Support     | Windows only         |
| Open Source    | Mostly no            |
| Performance    | Older/slower         |
| Cross-platform | No                   |
| Current Status | Legacy / maintenance, still supported but not active |

## Common Technologies

* Windows Forms , WPF = Windows Presentation Foundation , ASP.NET MVC (old) , WebForms , WCF = Windows Communication Foundation

# 2. .NET Core { first step into a better future }

| Feature        | .NET Core   |
| Cross-platform | Yes         |
| Open source    | Yes         |
| Fast           | Much faster |
| Lightweight    | Yes         |
| Cloud-ready    | Yes         |
| Docker support | Excellent   |

> After .NET core 3.1, Microsoft renamed everything.

# 3. Modern .NET (.NET 5/6/7/8..) { a more modern continuation }
> it's all .NET only no more `Core`

Features:
    * LTS (Long-Term Support), Very stable, Better performance
    * Minimal APIs
    * Hot Reload
    * cloud improvements
    * Native AOT = Ahead-Of-Time compilation
    * Better ASP.NET
    * Better Blazor
    * Better container support
    * Improved MAUI
    * AI integrations


# Famous Parts of Modern .NET

* ASP.NET Core
    * backend servers
    * websites
    * APIs
* Entity Framework Core
    * ORM ( object relational mapping )
* CLI `dotnet`


# what is the CLR (common lang runtime)
> the engine doing these
    * memory management, garbage collection
    * security
    * JIT/AOT compilation
        * just-in-time {while the app running you compile some parts}
        * ahead-of-time {before the app runs you compile some parts}
    * threading


# diff between .net framwork & .net core


| criteria                               | .net framework                                                                                         | .net core/.net                                                                                                                |
| flow                                   | App Code > .net framework class libraries (FCL/BCL) > compiler (csc) > IL > CLR > windows machine code | app code > .net core base libraries (coreFX) + nuget packages >Roslyn compiler > IL > coreCLR > OS proper native machine code |
| design                                 | bulky, huge per install                                                                                | features are modular , package segmeneted                                                                                     |
| size                                   | huge                                                                                                   | smaller, faster to deploy                                                                                                     |
| performance                            | old, slower                                                                                            | fast, modern                                                                                                                  |
| platform                               | only windows                                                                                           | cross-platform                                                                                                                |
| apps with diff version on same machine | not possible, cuz the install was system-wide                                                          | possible, cuz apps can be self-contained/backward compatibility                                                               |
| lightweight deployment                 | no                                                                                                     | yes                                                                                                                           |
| open source                            | no                                                                                                     | yes                                                                                                                           |
| containers,clouds                      | no                                                                                                     | yes                                                                                                                           |


# NuGet: the package/dependency manager
## why? NuGet 
it automates:
    * downloading
    * updates
    * versioning
    * dependency resolution
* easy project file integrations
* resolves conflicts a bit

## a package structure

```text 
metadata
DLL assemblies
dependencies
version
build scripts
```
## package restore meaning

`dotnet restore`
    NuGet restores missing packages automatically. Usually also happens during:
        * build
        * run
        * publish
