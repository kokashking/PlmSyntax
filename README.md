# PlmSyntax

A Windows desktop toolkit for working with Teamcenter **PLMXML** files offline: no Teamcenter server, VPN, or database connection needed.

Built with .NET 9, WPF, and [WPF UI](https://github.com/lepoco/wpfui) (Fluent design).

## Features

- **Importer**: stage one or more `.plmxml` / `.xml` files, parse them, and flatten each assembly tree into a list of leaf nodes. Large files are loaded in batches so the UI stays responsive.
- **Explorer**: browse the parsed assembly structure as a tree.
- **Cross-file collision audit**: when you import several files, IDs that collide between them are flagged before you merge.
- **Merge**: join several split PLMXML files into a single file under a master root. Colliding IDs are renamed (for example `id1` → `F1_id1`) so the output stays valid XML.
- **Parsing profiles**: control how files are parsed and merged:
  - Root search strategy: `Attribute`, `Heuristic`, `Flat`, or `Auto` (tries each in turn)
  - Merge strategy: `PrefixAll`, `UnifyByName`, `UnifyByPartNo`, `FlattenHierarchy`
  - Part number `UserData` title, merge prefix pattern, and indirect link resolution

  Profiles are saved as JSON in a `Profiles` folder next to the executable.

## Requirements

- Windows 10/11
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

## Build and run

```powershell
git clone https://github.com/kokashking/PlmSyntax.git
cd PlmSyntax
dotnet build
dotnet run --project PlmXmlSyntax.csproj
```

You can also open `PlmXmlSyntax.sln` in Visual Studio 2022.

## Tests

The xUnit tests are in `PlmXmlSyntax.Tests`, with sample PLMXML files under `PlmXmlSyntax.Tests/TestData` (broken references, circular references, multiple roots, flat fallback, and more).

```powershell
dotnet test
```

## Project structure

```
Models/        PLMXML object model, parsing profiles, audit types
Services/      Parsing, tree traversal, merge, audit, profile storage
ViewModels/    MVVM view models (CommunityToolkit.Mvvm)
Views/         WPF pages: Importer, Explorer, Settings
Helpers/       PLMXML helpers and value converters
PlmXmlSyntax.Tests/  xUnit tests and test data
```

## Roadmap

These are planned and not implemented yet:

- **Guard**: strip sensitive metadata (owners, costs), anonymize UIDs, and clean local file paths before sharing files externally.
- **Audit**: detect broken `instanceRefs`, check transforms for invalid values, and validate against the PLMXML XSD.
- **Report**: export the flattened BOM to Excel or JSON.
- World-transform calculation with `Matrix4x4`.
