using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(IMessageSessionCodeFixProvider)), Shared]
public class IMessageSessionCodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(IMessageSessionAnalyzer.Rule.Id);

    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

        var diagnostic = context.Diagnostics.FirstOrDefault(d => FixableDiagnosticIds.Contains(d.Id));
        if (diagnostic == null)
            return;

        var diagnosticSpan = diagnostic.Location.SourceSpan;
        var node = root.FindNode(diagnosticSpan);

        if (node is IdentifierNameSyntax identifierName && identifierName.Identifier.ValueText == "IMessageSession")
        {
            // Option 1: Replace with ITransactionalSession (recommended)
            var replaceAction = CodeAction.Create(
                title: "Replace with ITransactionalSession (recommended)",
                createChangedDocument: c => ReplaceIMessageSessionAsync(context.Document, identifierName, c),
                equivalenceKey: "ReplaceWithITransactionalSession");

            // Option 2: Add a TODO comment to review later
            var todoAction = CodeAction.Create(
                title: "Add TODO comment to review IMessageSession usage",
                createChangedDocument: c => AddTodoCommentAsync(context.Document, identifierName, c),
                equivalenceKey: "AddTodoComment");

            // Option 3: Suppress the warning with justification
            var suppressAction = CodeAction.Create(
                title: "Suppress warning (add justification comment)",
                createChangedDocument: c => SuppressWarningAsync(context.Document, identifierName, c),
                equivalenceKey: "SuppressWarning");

            // Option 4: Nested actions for advanced scenarios
            var nestedAction = CreateNestedCodeActions(context.Document, identifierName, diagnostic);

            // Register all actions - user can choose which one to apply
            context.RegisterCodeFix(replaceAction, diagnostic);
            context.RegisterCodeFix(todoAction, diagnostic);
            context.RegisterCodeFix(suppressAction, diagnostic);
            context.RegisterCodeFix(nestedAction, diagnostic);
        }
    }

    private static CodeAction CreateNestedCodeActions(Document document, IdentifierNameSyntax identifierName, Diagnostic diagnostic)
    {
        var nestedActions = ImmutableArray.Create(
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
        );

        return CodeAction.Create(
            title: "More replacement options...",
            nestedActions: nestedActions,
            isInlinable: false);
    }

    private static async Task<Document> ReplaceIMessageSessionAsync(Document document, IdentifierNameSyntax identifierName, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        
        var newIdentifierName = SyntaxFactory.IdentifierName("ITransactionalSession")
            .WithTriviaFrom(identifierName);

        var newRoot = root.ReplaceNode(identifierName, newIdentifierName);
        
        // Add using statement if not already present
        newRoot = await AddUsingDirectiveIfNeededAsync(document, newRoot, cancellationToken);
        
        return document.WithSyntaxRoot(newRoot);
    }

    private static async Task<Document> AddTodoCommentAsync(Document document, IdentifierNameSyntax identifierName, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        
        // Find the containing member to add a TODO comment
        var containingMember = identifierName.Ancestors().FirstOrDefault(n => 
            n is MemberDeclarationSyntax || n is ParameterSyntax);
            
        if (containingMember != null)
        {
            var todoComment = SyntaxFactory.Comment("// TODO: Consider migrating from IMessageSession to ITransactionalSession for better transactional consistency");
            var leadingTrivia = containingMember.GetLeadingTrivia().Add(todoComment).Add(SyntaxFactory.CarriageReturnLineFeed);
            var newMember = containingMember.WithLeadingTrivia(leadingTrivia);
            var newRoot = root.ReplaceNode(containingMember, newMember);
            return document.WithSyntaxRoot(newRoot);
        }
        
        return document;
    }

    private static async Task<Document> SuppressWarningAsync(Document document, IdentifierNameSyntax identifierName, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        
        // Find the containing member to add a suppression comment
        var containingMember = identifierName.Ancestors().FirstOrDefault(n => 
            n is MemberDeclarationSyntax || n is ParameterSyntax);
            
        if (containingMember != null)
        {
            var suppressComment = SyntaxFactory.Comment("// Justification: IMessageSession is intentionally used here because [ADD YOUR REASON]");
            var pragmaComment = SyntaxFactory.Comment("#pragma warning disable TSA001 // Use ITransactionalSession instead of IMessageSession");
            var pragmaRestore = SyntaxFactory.Comment("#pragma warning restore TSA001");
            
            var leadingTrivia = containingMember.GetLeadingTrivia()
                .Add(suppressComment).Add(SyntaxFactory.CarriageReturnLineFeed)
                .Add(pragmaComment).Add(SyntaxFactory.CarriageReturnLineFeed);
            
            var trailingTrivia = containingMember.GetTrailingTrivia()
                .Add(SyntaxFactory.CarriageReturnLineFeed)
                .Add(pragmaRestore);
            
            var newMember = containingMember
                .WithLeadingTrivia(leadingTrivia)
                .WithTrailingTrivia(trailingTrivia);
                
            var newRoot = root.ReplaceNode(containingMember, newMember);
            return document.WithSyntaxRoot(newRoot);
        }
        
        return document;
    }

    private static async Task<Document> ReplaceWithPackageCommentAsync(Document document, IdentifierNameSyntax identifierName, CancellationToken cancellationToken)
    {
        // First replace the identifier
        var newDocument = await ReplaceIMessageSessionAsync(document, identifierName, cancellationToken);
        
        // Then add a package reference comment at the top of the file
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

    private static async Task<Document> ReplaceAllInFileAsync(Document document, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        
        // Find all IMessageSession identifiers in the file
        var identifiers = root.DescendantNodes()
            .OfType<IdentifierNameSyntax>()
            .Where(id => id.Identifier.ValueText == "IMessageSession")
            .ToList();
        
        if (!identifiers.Any())
            return document;
        
        var newRoot = root;
        foreach (var identifier in identifiers)
        {
            var newIdentifier = SyntaxFactory.IdentifierName("ITransactionalSession")
                .WithTriviaFrom(identifier);
            newRoot = newRoot.ReplaceNode(identifier, newIdentifier);
        }
        
        // Add using directive if needed
        newRoot = await AddUsingDirectiveIfNeededAsync(document, newRoot, cancellationToken);
        
        return document.WithSyntaxRoot(newRoot);
    }

    private static Task<SyntaxNode> AddUsingDirectiveIfNeededAsync(Document document, SyntaxNode root, CancellationToken cancellationToken)
    {
        if (root is CompilationUnitSyntax compilationUnit)
        {
            var hasNServiceBusUsing = compilationUnit.Usings.Any(u => 
                u.Name?.ToString() == "NServiceBus");

            if (!hasNServiceBusUsing)
            {
                var usingDirective = SyntaxFactory.UsingDirective(
                    SyntaxFactory.IdentifierName("NServiceBus"));

                var newCompilationUnit = compilationUnit.AddUsings(usingDirective);
                return Task.FromResult<SyntaxNode>(newCompilationUnit);
            }
        }

        return Task.FromResult(root);
    }
}
