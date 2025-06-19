# User Interaction Patterns in Roslyn Code Fix Providers

This document explains the different ways to prompt users or provide interactive experiences in Roslyn code fix providers.

## Overview

While Roslyn code fix providers cannot show traditional modal dialogs or prompt windows, there are several patterns to provide user choice and interaction:

## 1. Multiple Code Actions (Choice Menu)

The most common approach is to provide multiple code fix actions that users can choose from. This appears as a menu when users click the lightbulb or press `Ctrl+.`:

```csharp
public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
{
    // ... diagnostic detection code ...

    if (node is IdentifierNameSyntax identifierName)
    {
        // Option 1: Primary action (recommended)
        var replaceAction = CodeAction.Create(
            title: "Replace with ITransactionalSession (recommended)",
            createChangedDocument: c => ReplaceIMessageSessionAsync(context.Document, identifierName, c),
            equivalenceKey: "ReplaceWithITransactionalSession");

        // Option 2: Alternative action
        var todoAction = CodeAction.Create(
            title: "Add TODO comment to review IMessageSession usage",
            createChangedDocument: c => AddTodoCommentAsync(context.Document, identifierName, c),
            equivalenceKey: "AddTodoComment");

        // Option 3: Suppression action
        var suppressAction = CodeAction.Create(
            title: "Suppress warning (add justification comment)",
            createChangedDocument: c => SuppressWarningAsync(context.Document, identifierName, c),
            equivalenceKey: "SuppressWarning");

        // Register all options
        context.RegisterCodeFix(replaceAction, diagnostic);
        context.RegisterCodeFix(todoAction, diagnostic);
        context.RegisterCodeFix(suppressAction, diagnostic);
    }
}
```

**Benefits:**

- Native IDE integration
- No interruption to workflow
- Clear action descriptions
- Can be applied via keyboard shortcuts

## 2. Nested Code Actions (Sub-menus)

For more complex scenarios, you can create hierarchical menus:

```csharp
private static CodeAction CreateNestedCodeActions(Document document, IdentifierNameSyntax identifierName, Diagnostic diagnostic)
{
    var nestedActions = new[]
    {
        CodeAction.Create(
            title: "Replace with ITransactionalSession",
            createChangedDocument: c => ReplaceIMessageSessionAsync(document, identifierName, c),
            equivalenceKey: "ReplaceWithITransactionalSession_Nested"),
            
        CodeAction.Create(
            title: "Replace and add package reference comment",
            createChangedDocument: c => ReplaceWithPackageCommentAsync(document, identifierName, c),
            equivalenceKey: "ReplaceWithPackageComment"),
            
        CodeAction.Create(
            title: "Replace all occurrences in file",
            createChangedDocument: c => ReplaceAllInFileAsync(document, c),
            equivalenceKey: "ReplaceAllInFile")
    };

    return CodeAction.Create(
        title: "More replacement options...",
        nestedActions: nestedActions,
        isInlinable: false);
}
```

**Benefits:**

- Organizes related actions
- Reduces menu clutter
- Groups advanced options

## 3. Contextual Information in Titles

Use descriptive titles to guide user decisions:

```csharp
// Good: Descriptive titles that inform the user
"Replace with ITransactionalSession (requires NServiceBus.TransactionalSession package)"
"Add TODO comment to review IMessageSession usage"
"Suppress warning (add justification comment)"

// Bad: Vague titles
"Replace"
"Add comment"
"Suppress"
```

## 4. Progressive Enhancement Through Comments

Add contextual information through code comments:

```csharp
private static async Task<Document> ReplaceWithPackageCommentAsync(Document document, IdentifierNameSyntax identifierName, CancellationToken cancellationToken)
{
    // First perform the replacement
    var newDocument = await ReplaceIMessageSessionAsync(document, identifierName, cancellationToken);
    
    // Then add helpful guidance
    var root = await newDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
    
    if (root is CompilationUnitSyntax compilationUnit)
    {
        var packageComment = SyntaxFactory.Comment("// Package required: dotnet add package NServiceBus.TransactionalSession");
        var newLeadingTrivia = compilationUnit.GetLeadingTrivia().Add(packageComment).Add(SyntaxFactory.CarriageReturnLineFeed);
        var newRoot = compilationUnit.WithLeadingTrivia(newLeadingTrivia);
        return newDocument.WithSyntaxRoot(newRoot);
    }
    
    return newDocument;
}
```

## 5. Conditional Actions Based on Context

Provide different options based on the current code context:

```csharp
public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
{
    // ... detect the diagnostic ...
    
    // Check if the project already has the required package
    var hasTransactionalSessionPackage = await HasPackageReference(context.Document.Project, "NServiceBus.TransactionalSession");
    
    if (hasTransactionalSessionPackage)
    {
        // Simple replacement when package is available
        var replaceAction = CodeAction.Create(
            title: "Replace with ITransactionalSession",
            createChangedDocument: c => ReplaceIMessageSessionAsync(context.Document, identifierName, c),
            equivalenceKey: "ReplaceWithITransactionalSession");
            
        context.RegisterCodeFix(replaceAction, diagnostic);
    }
    else
    {
        // Guidance when package is missing
        var replaceWithGuidanceAction = CodeAction.Create(
            title: "Replace with ITransactionalSession (requires adding NuGet package)",
            createChangedDocument: c => ReplaceWithPackageCommentAsync(context.Document, identifierName, c),
            equivalenceKey: "ReplaceWithGuidance");
            
        context.RegisterCodeFix(replaceWithGuidanceAction, diagnostic);
    }
}
```

## 6. Equivalence Keys for Grouping

Use equivalence keys to group related fixes:

```csharp
// All these actions are related to the same fix concept
var action1 = CodeAction.Create("Option 1", ..., equivalenceKey: "MyFix");
var action2 = CodeAction.Create("Option 2", ..., equivalenceKey: "MyFix");
var action3 = CodeAction.Create("Option 3", ..., equivalenceKey: "MyFix");
```

This allows the IDE to:

- Apply the fix to all similar occurrences
- Group the actions logically
- Provide better "Fix All" functionality

## 7. Limitations and Alternatives

**What you CANNOT do:**

- Show modal dialogs
- Prompt for user input with text boxes
- Display custom UI windows
- Interrupt the user's workflow

**Alternatives for complex scenarios:**

- Use multiple diagnostic rules with different severities
- Create separate analyzers for different scenarios
- Provide detailed guidance through comments and documentation
- Use external tools or extensions for complex interactions

## 8. Best Practices

1. **Keep actions simple**: Each action should do one clear thing
2. **Use descriptive titles**: Help users understand what each action does
3. **Provide guidance**: Use comments to explain requirements or next steps
4. **Respect user choice**: Don't force a particular solution
5. **Be consistent**: Use similar patterns across your analyzers
6. **Test thoroughly**: Verify actions work in different code contexts

## Example Usage in Our TransactionalSession Analyzer

Our analyzer demonstrates several of these patterns:

- **Multiple choices**: Replace, add TODO, or suppress
- **Nested actions**: Advanced replacement options
- **Contextual guidance**: Package requirement information
- **Progressive enhancement**: Adding helpful comments and directives

When users encounter an `IMessageSession` warning, they see:

1. "Replace with ITransactionalSession (recommended)"
2. "Add TODO comment to review IMessageSession usage"
3. "Suppress warning (add justification comment)"
4. "More replacement options..." (nested menu)

This gives users flexibility while providing clear guidance about the recommended approach.
