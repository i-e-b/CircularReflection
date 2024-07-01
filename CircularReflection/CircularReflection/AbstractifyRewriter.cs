using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CircularReflection;

/// <summary>
/// Rewrites C# files to reduce classes to <c>abstract</c> definitions
/// </summary>
internal class AbstractifyRewriter : CSharpSyntaxRewriter
{
    /// <summary>
    /// Using directives that have been gathered from all processed files
    /// </summary>
    public readonly HashSet<string> UsingDirectives = new();
        
    public AbstractifyRewriter() :base(visitIntoStructuredTrivia:true) {
        // Note: we must visit trivia, otherwise `#region` definitions get broken
    }

    public override SyntaxNode? VisitUsingDirective(UsingDirectiveSyntax node)
    {
        // Gather using directives together, and remove them from individual files
        if (base.VisitUsingDirective(node) is UsingDirectiveSyntax next) UsingDirectives.Add(next.ToString());
        return null;
    }

    public override SyntaxNode? VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
    {
        // Add a prefix to namespaces to prevent collisions
        var next = base.VisitNamespaceDeclaration(node) as NamespaceDeclarationSyntax;
        return next?.WithName(SyntaxFactory.ParseName("Reflection." + next.Name));
    }

    public override SyntaxNode? VisitFileScopedNamespaceDeclaration(FileScopedNamespaceDeclarationSyntax node)
    {
        // Change file-scoped namespace to block-scoped (as we may be merging multiple files with namespaces)
        var next = base.VisitFileScopedNamespaceDeclaration(node) as FileScopedNamespaceDeclarationSyntax;
        if (next is null) return null;

        var scoped = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName(" Reflection." + next.Name));
        return scoped.WithOpenBraceToken(SyntaxFactory.ParseToken("{\r\n")).AddMembers(next.Members.ToArray());
    }

    public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        // Make classes `abstract`
        var next = base.VisitClassDeclaration(node) as ClassDeclarationSyntax;
        return next?.AddModifiers(SyntaxFactory.ParseToken("abstract "));
    }

    public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        // Make all public methods 'public abstract', non-async. Remove non public methods.
        var next = base.VisitMethodDeclaration(node) as MethodDeclarationSyntax;
        if (next is null) return null;

        if (next.Modifiers.All(m => m.Text != "public")) return null; // remove private methods
        return next
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.ParseToken("public "), SyntaxFactory.ParseToken("abstract ")))
            .WithBody(null) // remove method body
            .WithSemicolonToken(SyntaxFactory.ParseToken(";\r\n")); // add a semicolon: MyMethod(){...}  ->  MyMethod();
    }

    public override SyntaxNode? VisitFieldDeclaration(FieldDeclarationSyntax node)
    {
        return null; // Remove fields
    }

    public override SyntaxNode? VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
    {
        return null; // Remove constructors
    }

    public override SyntaxNode? VisitDestructorDeclaration(DestructorDeclarationSyntax node)
    {
        return null; // Remove Destructors
    }

    public override SyntaxNode? VisitRegionDirectiveTrivia(RegionDirectiveTriviaSyntax node)
    {
        // Remove `#region ...` lines
        return null;
    }

    public override SyntaxNode? VisitEndRegionDirectiveTrivia(EndRegionDirectiveTriviaSyntax node)
    {
        // Remove `#endregion ...` lines
        return null;
    }
}