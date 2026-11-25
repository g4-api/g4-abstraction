# G4.Abstraction Solution

A collection of libraries for streamlined command-line processing and structured logging in .NET projects. The **G4.Abstraction** solution consists of two main components:

1. **[G4.Abstraction.Cli](./docs/packages-readme/G4.Abstraction.Cli.md)**
2. **[G4.Abstraction.Logging](./docs/packages-readme/G4.Abstraction.Logging.md)**

`G4.Abstraction.WebDriver` moved to [g4-engine](https://github.com/g4-api/g4-engine). It keeps its package ID and namespace, and the engine now publishes it with the engine version, so the engine no longer depends on this repository for drivers. The driver table and the driver-plugin guide moved with it.

Each library is designed to work independently or together, providing flexibility for developers who need:

- **Robust command-line argument parsing** and validation
- **Configurable, structured logging** with multiple output formats

## Quick Start

1. Choose the library or libraries you need:
   - **CLI parsing** with [G4.Abstraction.Cli](./docs/packages-readme/G4.Abstraction.Cli.md)
   - **Structured logging** with [G4.Abstraction.Logging](./docs/packages-readme/G4.Abstraction.Logging.md)

2. Install the corresponding NuGet package(s):
   ```bash
   dotnet add package G4.Abstraction.Cli
   dotnet add package G4.Abstraction.Logging
   ```

3. Configure and use the libraries as described in each project's documentation (linked above).

## Contributing

Contributions, bug reports, and feature requests are welcome. Please open an issue or submit a pull request in the appropriate project directory.

## License

All libraries in the G4.Abstraction solution are released under the [Apache License 2.0](https://www.apache.org/licenses/LICENSE-2.0).

## Contributing and builds

Run `dotnet build` from `src/` with the .NET 10 SDK. The release pipeline builds and publishes the NuGet packages
from `main`. Changes are listed in the [changelog](docs/CHANGELOG.md).
