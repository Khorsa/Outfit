GNU GPL v3 license — you can do anything you want, but you must give me credit and distribute your changes under the same terms.

API1 is a modular framework for OutfitTool applications, providing a standardized interface for module development and integration. The API defines core interfaces for command execution, module metadata, logging, and settings management.

Key components:

- **ICommand**: Interface for executable commands with name, description, and optional context menu
- **IModuleInfo**: Interface defining module metadata including name, version, author, and API requirements
- **IModuleController**: Main module controller interface handling initialization, shutdown, command registration, and UI integration
- **SettingsManager<T>**: Generic YAML-based settings manager for persistent configuration storage
- **ModuleVersion**: Semantic versioning class with comparison operators for version management
- **Logger infrastructure**: ILogger interface and supporting classes for structured logging with different severity levels
- **DTO classes**: Data transfer objects for notifications, context menus, and other cross-module data

The API uses .NET 8.0 and follows C# 12 conventions, with all types in the `OutfitTool.API1` namespace. Modules must implement the required interfaces and reference this API assembly to ensure compatibility with the OutfitTool ecosystem.
