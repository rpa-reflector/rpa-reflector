# RPA .NET Reflector

RPA .NET Reflector is a reflection-based integration bridge between RPA systems and ChipSoft HiX (versions 6.1, 6.2, and 6.3). It is not statically linked to any HiX .NET assembly; instead it fully relies on .NET reflection to discover and load the required assemblies at runtime.

Out of the box, RPA Reflector offers object/property retrieval by object-id through a simple HTTP API. For more advanced scenarios, C# *implementation modules* can be dynamically compiled against the installed HiX version and expose an XML request/response interface. These modules can create and populate custom data models by calling ChipSoft Logics and assemblies.

RPA .NET Reflector can also run in **mock mode** with no HiX installation present, which is useful for unit and integration testing.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

This project is moving towards [REUSE compliance](https://reuse.software/). All source files are being annotated with SPDX license and copyright headers:

```
// SPDX-FileCopyrightText: 2026 Reflector Maintainers
// SPDX-License-Identifier: MIT
```

## Third-Party Dependencies

See [DEPENDENCIES.md](DEPENDENCIES.md) for a full list of third-party NuGet packages and their licenses.

---

## Scope and Non-Responsibility

- RPA .NET Reflector is a **bridge tool**. The correctness of the data returned depends entirely on the installed HiX version and the implementation modules you supply.
- This project has **no affiliation with ChipSoft** and is not officially supported by them. HiX assemblies are proprietary software owned by ChipSoft; this project merely loads them via .NET reflection.
- Breaking changes in HiX assembly structure across minor or patch releases may require updates to this tool.
- Implementation modules loaded at runtime are **not** part of this project and are the sole responsibility of the team that authors them.
- No warranty is provided. Use in production environments is at your own risk.

---

## Requirements

- **OS**: Windows (x64 recommended)
- **Runtime**: .NET Framework 4.7.2
- **Build tools**: Visual Studio 2019 or later, or MSBuild 16+, plus the NuGet CLI
- **HiX** (optional): Required only for HiX-integrated operation. Not needed for mock/standalone mode.

---

## Building from Source

```powershell
nuget restore RPAReflector.sln
msbuild RPAReflector.sln /p:Configuration=Release /p:Platform="Any CPU"
```

All NuGet dependencies are committed under the `packages/` directory, so no additional package sources are required.

---

## Installation

### Debug (development)

A Debug build runs as a headless .NET Windows application. Start the executable directly. When a debugger is attached, some internal HiX method calls may throw handled exceptions — this is normal. Console output is written to any attached debugger output window.

### Release (Windows Service)

Release builds run as a Windows Service. Register the service with PowerShell:

```powershell
New-Service -Name "RPA .NET Reflector" -BinaryPathName "C:\path	o\RPAReflector.exe"
```

---

## Configuration

All configuration lives in `App.config`. Use `App.config.template` as a starting point.

### Mock / Standalone mode (no HiX)

Omit `SelectedEnvironment` and set `HixVersion` to `none` (or omit it entirely):

```xml
<appSettings>
  <add key="HixVersion" value="none" />
  <add key="ApiUsername" value="api" />
  <add key="ApiPassword" value="change-me" />
</appSettings>
```

In this mode no HiX assemblies are loaded and only implementation modules that do not depend on HiX will work.

### HiX 6.1

Provide the environment code and version, an `environmentsSection` listing all available environments, and a `connectionStrings` entry per environment:

```xml
<appSettings>
  <add key="SelectedEnvironment" value="MY_ENV" />
  <add key="HixVersion" value="6.1.0.0" />
  <add key="ApiUsername" value="api" />
  <add key="ApiPassword" value="change-me" />
</appSettings>

<environmentsSection>
  <environmentCollection>
    <add code="MY_ENV"     description="HiX Test environment" />
    <add code="MY_ENV_ACC" description="HiX Acceptance environment" />
  </environmentCollection>
</environmentsSection>

<connectionStrings>
  <add name="MY_ENV"
       connectionString="Data Source={sql-server};Initial Catalog={db};Packet Size=4096;Connection Reset=True;Application Name=ChipSoft.FCL.Base;User ID={user};Password={password};AttachDBFilename=;"
       providerName="System.Data.SqlClient" />
  <add name="MY_ENV_ACC"
       connectionString="Data Source={sql-server-acc};Initial Catalog={db-acc};Packet Size=4096;Connection Reset=True;Application Name=ChipSoft.FCL.Base;User ID={user};Password={password};AttachDBFilename=;"
       providerName="System.Data.SqlClient" />
</connectionStrings>
```

The environment code matches the way it is expressed in `DD.ini` and the HiX Environments configuration.

### HiX 6.2 and 6.3

HiX 6.2/6.3 use a network-based environment discovery service (HiX Environments). No XML environment section or connection strings are required in `App.config`. Instead:

1. Pass the environment category as a command-line argument when starting the service:
   ```
   RPAReflector.exe /HixEnvironmentsCategory=MY_ENV
   ```
2. Ensure the Windows account under which the service runs has access to the HiX Environments service. The default service account often does **not** have this access; configure the service `Log On` account accordingly.

Set the version in `App.config`:

```xml
<appSettings>
  <add key="SelectedEnvironment" value="MY_ENV" />
  <add key="HixVersion" value="6.3.0.0" />
  <add key="ApiUsername" value="api" />
  <add key="ApiPassword" value="change-me" />
</appSettings>
```

### Startup implementation modules

Implementation modules can be loaded automatically at startup by adding them to `startupImplementationsSection`. Each module is a single merged `.cs` file placed next to the executable:

```xml
<startupImplementationsSection>
  <startupImplementationCollection>
    <add name="myModule" source="myModule.cs" />
  </startupImplementationCollection>
</startupImplementationsSection>
```

If the source file is not found the service logs a warning and continues normally.

---

## API Usage

The service listens on **port 9001**. All endpoints require HTTP Basic Authentication using the credentials configured in `App.config`. Debug builds listen on `localhost` only; release builds listen on all interfaces (`0.0.0.0`).

### System info

```
GET /api/system/info
```

Returns the running status, loaded HiX assemblies, configured HiX version, and active implementation modules.

### Retrieve object field values (HiX only)

```
GET /api/object-field-value/{logicName}/{object-id-str}
```

Returns all fields of the object at the given id. Logic names follow the convention used in comEZ `.INI` files. The object id string may contain multiple field-value pairs if the object logic requires it.

To retrieve a specific set of fields, POST form-data key/value pairs where the key is your variable name and the value is the field path:

```
POST /api/object-field-value/{logicName}/{object-id-str}
Content-Type: application/x-www-form-urlencoded

myVar1=SomeField&myVar2=Nested.SubField
```

The response is a JSON key/value map using your supplied variable names as keys. Nested objects are not traversed recursively for performance reasons.

### Load an implementation module

```
POST /api/system/load-implementation
Content-Type: multipart/form-data
```

The multipart field name is used as the module name. The field value is the C# source file content. Multiple modules can coexist at runtime.

### Execute an implementation module

```
POST /api/implementation/{module}/execute
Content-Type: application/xml
```

Accepts and returns XML. The exact request/response schema is defined by the implementation module.

---

## Limitations

- Object field retrieval is **single-level only**; nested objects are not resolved recursively.
- HiX assembly loading relies on assemblies being registered in the Windows Global Assembly Cache (`C:\Windows\Microsoft.NETssembly` or `C:\Windowsssembly`). HiX must be installed on the same machine.
- Only HiX versions 6.1 and 6.3 are explicitly supported. HiX 6.2 follows the 6.3 environment-discovery model.
- Windows only (.NET Framework 4.7.2). No cross-platform support.

---

## Support

This is an open-source community project provided as-is under the MIT License. There is no official support channel. For questions or contributions, open an issue or pull request in the project repository.
