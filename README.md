# TransactionalSessionAnalyzer

A C# Roslyn analyzer spike that detects usage of `IMessageSession` and suggests replacing it with `ITransactionalSession` for better transactional consistency in NServiceBus applications.

## Overview

This analyzer helps developers migrate from `IMessageSession` to `ITransactionalSession` by:

- Detecting all usages of `IMessageSession` in parameters, fields, properties, and variable declarations
- Providing automatic code fixes to replace `IMessageSession` with `ITransactionalSession`
- Ensuring the proper `using NServiceBus;` directive is present

## Features

- **Comprehensive Detection**: Identifies `IMessageSession` usage in:
  - Constructor parameters (primary constructors and regular constructors)
  - Method parameters
  - Field declarations
  - Property declarations
  - Variable declarations
- **Automatic Code Fixes**: Provides quick fixes to replace `IMessageSession` with `ITransactionalSession`
- **Smart Using Directives**: Automatically adds `using NServiceBus;` if not already present

## Usage

### Building the Analyzer

```bash
dotnet build TransactionalSessionAnalyzer.sln
```

### Running Tests

```bash
dotnet test TransactionalSessionAnalyzer.Test
```

### Using the Analyzer

1. Build the analyzer project
2. Reference the analyzer NuGet package in your target project
3. The analyzer will automatically detect `IMessageSession` usage and suggest fixes

### Example

The demo project (`DemoProject/Program.cs`) shows the analyzer in action with real NServiceBus interfaces:

**Before (flagged by analyzer):**

```csharp
using NServiceBus;

// This class triggers 5 analyzer warnings
class MyClass(IMessageSession session)  // Warning: TSA001
{
    private IMessageSession _session = session;  // Warning: TSA001
    
    public IMessageSession SessionProperty { get; set; } = session;  // Warning: TSA001
    
    public Task SomeAction()
    {
        IMessageSession localSession = _session;  // Warning: TSA001
        return Task.CompletedTask;
    }
    
    public Task AnotherAction(IMessageSession methodSession)  // Warning: TSA001
    {
        return Task.CompletedTask;
    }
}
```

**After (requires NServiceBus.TransactionalSession package):**

```csharp
using NServiceBus;

// Add this to your .csproj file:
// <PackageReference Include="NServiceBus.TransactionalSession" Version="2.0.0" />

// This class produces no warnings
class GoodClass(ITransactionalSession session)
{
    private ITransactionalSession _session = session;
    
    public ITransactionalSession SessionProperty { get; set; } = session;
    
    public async Task SomeAction()
    {
        using var transaction = await _session.Open();
        
        // Your business logic here
        
        // Send messages within the transaction
        await transaction.Send(new MyMessage());
        
        // Commit the transaction
        await transaction.Commit();
    }
}
```

### Testing the Analyzer

To see the analyzer in action, build the demo project:

```bash
dotnet build DemoProject/DemoProject.csproj
```

You should see warnings like:

```text
warning TSA001: Consider using ITransactionalSession instead of IMessageSession for better transactional consistency. Requires NServiceBus.TransactionalSession package.
```

The analyzer also provides **multiple code fix options** that users can choose from:

- **Replace with ITransactionalSession (recommended)** - Direct replacement
- **Add TODO comment to review IMessageSession usage** - Defer decision with reminder
- **Suppress warning (add justification comment)** - Intentionally keep IMessageSession with documentation
- **More replacement options...** - Nested menu with advanced options:
  - Replace and add package reference comment
  - Replace all occurrences in file

This gives developers flexibility in how they handle the migration from `IMessageSession` to `ITransactionalSession`.

## Diagnostic Information

- **Diagnostic ID**: TSA001
- **Category**: Usage
- **Severity**: Warning
- **Message**: "Consider using ITransactionalSession instead of IMessageSession for better transactional consistency"

## Development

### Project Structure

- `TransactionalSessionAnalyzer/` - Main analyzer project
  - `IMessageSessionAnalyzer.cs` - The analyzer implementation
  - `IMessageSessionCodeFixProvider.cs` - Code fix provider with package reference guidance
- `TransactionalSessionAnalyzer.Test/` - Unit tests
  - `BasicAnalyzerTest.cs` - Basic analyzer tests
- `DemoProject/` - Demonstration project showing the analyzer in action
  - `Program.cs` - Sample code that triggers analyzer warnings
  - `TransactionalSessionExample.cs` - Example showing proper ITransactionalSession usage

### Testing

The project includes a comprehensive test suite with 15 tests covering:

### Analyzer Tests

- **Detection scenarios**: Parameters, fields, properties, local variables
- **Multiple occurrences**: Detection of multiple `IMessageSession` usages in the same class
- **Negative cases**: Ensures no false positives for `ITransactionalSession` or unrelated types
- **Edge cases**: Nested classes, interface implementations

### Code Fix Provider Tests

- **Provider structure**: Instantiation, diagnostic ID support, fix-all provider availability
- **Integration**: End-to-end testing with the analyzer

### Performance Tests

- **Scalability**: Testing with large files (100+ method declarations)

### Running the Tests

```bash
# Run all tests
dotnet test

# Run with detailed output
dotnet test --verbosity normal

# Run specific test class
dotnet test --filter "IMessageSessionAnalyzerTests"
```

All tests pass successfully, providing confidence in the analyzer's reliability and performance.

For detailed test coverage information, see [TestSummary.md](TestSummary.md).

### Contributing

1. Ensure all tests pass
2. Add tests for new functionality
3. Follow existing code patterns and conventions
4. Update documentation as needed
