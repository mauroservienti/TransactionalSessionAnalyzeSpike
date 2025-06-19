using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Immutable;
using System.Linq;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class IMessageSessionAnalyzer : DiagnosticAnalyzer
{
    public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        "TSA001",
        "Use ITransactionalSession instead of IMessageSession",
        "Consider using ITransactionalSession instead of IMessageSession for better transactional consistency. Requires NServiceBus.TransactionalSession package.",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "ITransactionalSession provides better transactional consistency and should be preferred over IMessageSession in most scenarios. To use ITransactionalSession, you need to install the NServiceBus.TransactionalSession NuGet package.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeParameter, SyntaxKind.Parameter);
        context.RegisterSyntaxNodeAction(AnalyzeVariableDeclaration, SyntaxKind.VariableDeclaration);
        context.RegisterSyntaxNodeAction(AnalyzeFieldDeclaration, SyntaxKind.FieldDeclaration);
        context.RegisterSyntaxNodeAction(AnalyzePropertyDeclaration, SyntaxKind.PropertyDeclaration);
    }

    private static void AnalyzeParameter(SyntaxNodeAnalysisContext context)
    {
        var parameter = (ParameterSyntax)context.Node;
        
        if (parameter.Type is IdentifierNameSyntax identifierName && 
            identifierName.Identifier.ValueText == "IMessageSession")
        {
            var diagnostic = Diagnostic.Create(Rule, parameter.Type.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static void AnalyzeVariableDeclaration(SyntaxNodeAnalysisContext context)
    {
        var variableDeclaration = (VariableDeclarationSyntax)context.Node;
        
        // Only handle local variable declarations, not field declarations
        if (variableDeclaration.Parent is FieldDeclarationSyntax)
            return;
        
        if (variableDeclaration.Type is IdentifierNameSyntax identifierName && 
            identifierName.Identifier.ValueText == "IMessageSession")
        {
            var diagnostic = Diagnostic.Create(Rule, variableDeclaration.Type.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static void AnalyzeFieldDeclaration(SyntaxNodeAnalysisContext context)
    {
        var fieldDeclaration = (FieldDeclarationSyntax)context.Node;
        
        if (fieldDeclaration.Declaration.Type is IdentifierNameSyntax identifierName && 
            identifierName.Identifier.ValueText == "IMessageSession")
        {
            var diagnostic = Diagnostic.Create(Rule, fieldDeclaration.Declaration.Type.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static void AnalyzePropertyDeclaration(SyntaxNodeAnalysisContext context)
    {
        var propertyDeclaration = (PropertyDeclarationSyntax)context.Node;
        
        if (propertyDeclaration.Type is IdentifierNameSyntax identifierName && 
            identifierName.Identifier.ValueText == "IMessageSession")
        {
            var diagnostic = Diagnostic.Create(Rule, propertyDeclaration.Type.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }
    }
}
