using System;
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
        if (base.VisitUsingDirective(node) is not UsingDirectiveSyntax { Name: not null } next) return null;

        if (next.Alias is null) // normal `using my.namespace;`
        {
            var usingName = next.Name.ToString();

            UsingDirectives.Add(usingName);
        }
        else // alias `using className = my.namespace.className;`
        {
            var bits = next.Name.ToString().Split('.', StringSplitOptions.RemoveEmptyEntries);

            UsingDirectives.Add(string.Join(".", bits[..^1]));
        }

        return null;
    }

    public override SyntaxNode? VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
    {
        // Add a prefix to namespaces to prevent collisions
        if (base.VisitNamespaceDeclaration(node) is not NamespaceDeclarationSyntax next) return null;
        
        return next
            .WithName(SyntaxFactory.ParseName("Reflection." + next.Name));
    }

    public override SyntaxNode? VisitFileScopedNamespaceDeclaration(FileScopedNamespaceDeclarationSyntax node)
    {
        // Change file-scoped namespace to block-scoped (as we may be merging multiple files with namespaces)
        if (base.VisitFileScopedNamespaceDeclaration(node) is not FileScopedNamespaceDeclarationSyntax next) return null;

        var scoped = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName(" Reflection." + next.Name));
        
        return scoped
            .WithOpenBraceToken(SyntaxFactory.ParseToken("{\r\n"))
            .AddMembers(next.Members.ToArray());
    }

    public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        // Make classes `abstract`
        if (base.VisitClassDeclaration(node) is not ClassDeclarationSyntax next) return null;
        
        if (next.Modifiers.All(m => m.Text != "public" )) return null; // remove non-public classes

        return next
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.ParseToken("public "), SyntaxFactory.ParseToken("abstract "))) // replace all modifiers with `public abstract`
            .WithBaseList(null); // remove base classes
    }
    
    public override SyntaxNode? VisitArrowExpressionClause(ArrowExpressionClauseSyntax node)
    {
        return null; // Remove "expression bodies" from methods
    }

    public override SyntaxNode? VisitPropertyDeclaration(PropertyDeclarationSyntax node)
    {
        // If the property has an arrow expression body, we need to change it
        if (base.VisitPropertyDeclaration(node) is not PropertyDeclarationSyntax next) return null;

        if (next.AccessorList is null)
        {
            // add accessors, remove body/expression
            return next
                .WithExpressionBody(null)
                .WithAccessorList(SyntaxFactory.AccessorList(
                    SyntaxFactory.List(new List<AccessorDeclarationSyntax>
                    {
                        SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.ParseToken(";")),
                        SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.ParseToken(";"))
                    })))
                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.None))
                .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);
        }

        return next;
    }

    public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        // Make all public methods 'public abstract', non-async. Remove non public methods.
        if (base.VisitMethodDeclaration(node) is not MethodDeclarationSyntax next) return null;

        if (next.Identifier.ToString() == "ToString") return null; // don't include string overrides
        if (next.Modifiers.All(m => m.Text != "public")) return null; // remove private methods
        
        return next
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.ParseToken("public "), SyntaxFactory.ParseToken("abstract ")))
            .WithBody(null) // remove method body
            .WithoutTrivia()
            .WithSemicolonToken(SyntaxFactory.ParseToken(";")) // add a semicolon: MyMethod(){...}  ->  MyMethod();
            .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);
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
        return null; // Remove `#region ...` lines
    }

    public override SyntaxNode? VisitEndRegionDirectiveTrivia(EndRegionDirectiveTriviaSyntax node)
    {
        return null; // Remove `#endregion ...` lines
    }
}