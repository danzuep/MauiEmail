# OutlookClone

A .NET 10 MAUI email application. Install only the workloads for the platforms you intend to build.

Run the commands below from this folder. The repository's `global.json` selects the .NET SDK; workload installations are SDK-specific. Check the active SDK and its workloads with:

```powershell
dotnet --version
dotnet workload list
```

## Windows-only development

The project currently enables Windows when building on Windows and leaves the other platforms commented out. Install Windows MAUI support without installing Android or Apple workloads:

```powershell
dotnet workload install maui-windows
dotnet build src/OutlookClone/OutlookClone.csproj #-f net10.0-windows10.0.19041.0
dotnet run --project src/OutlookClone/OutlookClone.csproj
```

To explicitly keep the project Windows-only, replace the target framework declarations in [OutlookClone.csproj](OutlookClone.csproj) with:

```xml
<TargetFrameworks>$(NetVersion)-windows10.0.19041.0</TargetFrameworks>
```

## Other platforms

Replace the target framework declarations with the matching entry below for a single-platform project. These entries use the existing `NetVersion` property. Install the corresponding workload on a supported build host.

| Platform | TargetFrameworks value | Install command | Build host and prerequisites |
| --- | --- | --- | --- |
| Windows | `$(NetVersion)-windows10.0.19041.0` | `dotnet workload install maui-windows` | Windows with the required Windows SDK. |
| Android | `$(NetVersion)-android` | `dotnet workload install maui-android` | Windows or macOS; configure the supported Android SDK and JDK, plus an emulator or device. |
| iOS | `$(NetVersion)-ios` | `dotnet workload install maui-ios` | A Mac with a compatible Xcode installation is required, either locally or as a paired build host. |
| Mac Catalyst | `$(NetVersion)-maccatalyst` | `dotnet workload install maui-maccatalyst` | macOS with a compatible Xcode installation. |
| Tizen | `$(NetVersion)-tizen` | `dotnet workload install maui-tizen` | Follow the [Samsung Tizen .NET setup instructions](https://github.com/Samsung/Tizen.NET) for the additional SDK and tooling. |

For example, to build only Android:

```xml
<TargetFrameworks>$(NetVersion)-android</TargetFrameworks>
```

```powershell
dotnet workload install maui-android
dotnet build OutlookClone.csproj -f net10.0-android
```

For multiple platforms, separate framework values with semicolons and install each corresponding workload. Use OS conditions for targets that require a particular host. The commented Apple target list in the project includes `macios`; do not enable that entry, since it is not a valid MAUI target platform. Use `ios` and/or `maccatalyst` instead.

Selecting `-f` when building chooses a framework already declared in the project; it does not remove other declared frameworks from project-wide restore or workload checks. Restrict `TargetFrameworks` itself to avoid unnecessary workload downloads.

To see the workload choices supported by your active SDK:

```powershell
dotnet workload search maui
```

To test locally use something like [smtp4dev](https://github.com/rnwood/smtp4dev) in Docker, e.g. `docker run -d -p 3000:80 -p 25:25 -p 143:143 rnwood/smtp4dev`.
