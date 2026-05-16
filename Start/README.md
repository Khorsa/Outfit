# Start Module

This module is responsible for managing application updates and module loading for the OutfitTool application.

## Functionality

The Start module performs the following operations:

1. **Parameter Parsing**: Parses command-line arguments to extract parameters and identify modules to delete
2. **Module Management**: 
   - Deletes specified modules
   - Updates/installs new modules from the updates directory
3. **Application Update**: 
   - Updates the main application from the updates directory
   - Preserves important files during updates
4. **Resource Management**: Handles file locking issues by retrying operations when resources are busy
5. **Application Launch**: Starts the main OutfitTool application after updates are complete

## Key Features

- **Safe Directory Clearing**: The `ClearMainDirectory` method removes all files and directories except for important ones like "modules", "Start.exe", "updates", and "settings.yaml"
- **Recursive Copying**: The `DirectoryRecursiveCopy` method handles copying directory structures while preserving existing files that shouldn't be overwritten
- **Error Resilience**: Operations that fail due to locked resources are retried multiple times with delays
- **Logging**: Comprehensive logging through the FileLogger implementation

## Usage

The module is typically executed as part of the application startup process:

```
Start.exe [parameter1=value1] [parameter2=value2] [delete_module=module_name]
```

## Architecture

The module follows a simple procedural approach with the following components:

- `Program`: Main class containing the update and launch logic
- `FileLogger`: Implementation of ILoggerInterface for logging operations
- `ILoggerInterface`: Interface defining the logging contract

## Integration

This module is designed to work with the OutfitTool application, serving as the update and launch mechanism that ensures the main application and its modules are up-to-date before launching.

## License

This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.