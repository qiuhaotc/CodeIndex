# Visual Studio Extension Debugging Guide

## Issue Resolution: F5 Debugging

The issue where F5 debugging was using the old marketplace extension instead of the development version has been fixed.

### What was fixed:

1. **Separate Debug Extension ID**: The debug build now uses a different extension ID (`CodeIndex.VisualStudioExtension.3fee47f7-831c-4577-8b21-f6505d9699f5.Debug`) to avoid conflicts with the installed marketplace version.

2. **Enhanced Debug Configuration**: 
   - Debug symbols are now included in VSIX container
   - Full debug information enabled for Debug builds
   - Proper experimental instance configuration

3. **Debug vs Release Build Differences**:
   - **Debug**: Extension name shows as "Code Index Search (Development)" with unique ID
   - **Release**: Extension name shows as "Code Index Search" with original ID for marketplace

## How to Debug:

### Method 1: Visual Studio F5 Debugging
1. Open the solution in Visual Studio 2022
2. Set the CodeIndex.VisualStudioExtension project as startup project
3. Ensure you're in Debug configuration
4. Press F5 or click "Start Debugging"
5. A new VS experimental instance will open with your development extension

### Method 2: Manual VSIX Installation
1. Build the project in Debug mode
2. Locate the generated .vsix file in `bin/Debug/`
3. Close all Visual Studio instances
4. Double-click the .vsix file to install
5. Open Visual Studio - you should see "Code Index Search (Development)" in Extensions

### Troubleshooting:

If you still see the old extension:

1. **Reset Experimental Instance**:
   ```
   devenv.exe /resetSettings Exp
   devenv.exe /rootSuffix Exp /setup
   ```

2. **Uninstall marketplace extension** from the main VS instance if there are conflicts

3. **Clear Extension Cache**:
   Delete folder: `%localappdata%\Microsoft\VisualStudio\{version}_Config\Extensions`

4. **Verify Debug Build**: 
   - Check the extension name shows "(Development)" suffix
   - Verify the extension ID in the manifest contains ".Debug"

### Extension Identification:
- **Development Version**: "Code Index Search (Development)"
- **Marketplace Version**: "Code Index Search"

The development version will now load properly when debugging with F5.