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


| Criteria | .NET Framework | .NET Core / .NET |
|----------|----------------|------------------|
| **Flow** | App Code > .NET Framework class libraries (FCL/BCL) > compiler (csc) > IL > CLR > Windows machine code | App code > .NET Core base libraries (CoreFX) + NuGet packages > Roslyn compiler > IL > CoreCLR > OS proper native machine code |
| **Design** | Bulky, huge per install | Features are modular, package segmented |
| **Size** | Huge | Smaller, faster to deploy |
| **Performance** | Old, slower | Fast, modern |
| **Platform** | Only Windows | Cross-platform |
| **Apps with different versions on same machine** | Not possible, because the install was system-wide | Possible, because apps can be self-contained / backward compatible |
| **Lightweight deployment** | No | Yes |
| **Open source** | No | Yes |
| **Containers, clouds** | No | Yes |


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
