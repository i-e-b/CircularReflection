using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.CodeAnalysis.CSharp;

[assembly:InternalsVisibleTo("CircularReflectionTests")]

namespace CircularReflection;

/// <summary>
/// MSBuild task that re-writes C# files into <c>abstract</c> definitions.
/// </summary>
/// <example>
/// If you are using the NuGet package, follow the ReadMe file.
/// If you are running this as code directly in your project,
/// add this to the <c>.csproj</c> file of the destination project
/// <code><![CDATA[
///    <UsingTask TaskName="CircularReflectionTask"
///               AssemblyFile="C:\path\to\CircularReflection.dll" />
///
///    <Target Name="CircularReflectionTask" BeforeTargets="PrepareForBuild" Outputs="$(MSBuildProjectDirectory)\Test.generated.cs">
///        <CircularReflectionTask 
///                                InputBaseName="$(ReflectionInputBase)" OutputBaseName="$(ReflectionOutputBase)"
///                                AdditionalUsingPaths="$(ReflectionAdditionalUsings)" IncludeNamespaceStubs="$(ReflectionNamespaceStubs)">
///            <Output TaskParameter="GeneratedFile" PropertyName="GeneratedClassesFile" />
///        </CircularReflectionTask>
///        <ItemGroup>
///            <Compile Remove="$(GeneratedClassesFile)" />
///            <Compile Include="$(GeneratedClassesFile)" />
///        </ItemGroup>
///    </Target>
/// ]]></code></example>
[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public class CircularReflectionTask : Task
{
    /// <summary>
    /// Location of files to rewrite into the current project.
    /// This can be multiple directories separated by <c>;</c>
    /// </summary>
    [Required]
    public string InputBaseName { get; set; } = "";
    
    /// <summary>
    /// Location of the project to write files into
    /// </summary>
    [Required]
    public string OutputBaseName { get; set; } = "";

    /// <summary>
    /// Optional, extra using directives.
    /// This should be a <c>;</c> delimited list of namespaces
    /// </summary>
    public string AdditionalUsingPaths { get; set; } = "";
    
    /// <summary>
    /// Optional, using directives to exclude from the output
    /// This should be a <c>;</c> delimited list of namespaces
    /// </summary>
    public string ExcludeUsingPaths { get; set; } = "";

    /// <summary>
    /// Optional, default <c>true</c>.
    /// <p/>
    /// If <c>true</c>, each 'using ...;' namespace will have
    /// a stub class defined for it. This means the generated
    /// code should compile without requiring additional code
    /// to get generated code to build, but can result in the
    /// compiled assembly containing leaked namespaces.
    /// <p/>
    /// If <c>false</c>, no stub are added, so namespaces are
    /// not leaked into generated assemblies. You may need to
    /// add some 'fake' namespaced classes to have a reliable
    /// build.
    /// <p/>
    /// Generally, choose <c>false</c> for libraries or NuGet
    /// package, and <c>true</c> for executables.
    /// </summary>
    public string IncludeNamespaceStubs { get; set; } = "true";

    /// <summary>
    /// The filename where the class was generated
    /// </summary>
    [Output]
    public string GeneratedFile { get; set; } = "";

    /// <summary>
    /// Run the MSBuild task.
    /// This is called by MSBuild directly, based on the file
    /// <c>CircularReflection/build/CircularReflection.targets</c>
    /// in the NuGet package.
    /// </summary>
    public override bool Execute()
    {
        var paths = InputBaseName.Split(';', StringSplitOptions.RemoveEmptyEntries);
        var files = new FileSource();

        foreach (var inputPath in paths)
        {
            if (string.IsNullOrWhiteSpace(inputPath)) continue;
            
            if (!Directory.Exists(Path.GetFullPath(inputPath)))
            {
                Log.LogWarning($"CircularReflection task: Source directory not found: '{inputPath}'");
                continue;
            }

            files.AddDirectory(inputPath, "*.cs");
        }

        var addStubs = IncludeNamespaceStubs.Contains("true", StringComparison.OrdinalIgnoreCase);
        GeneratedFile = $"{OutputBaseName}/CircularReflection.generated.cs";
        var target = new FileTarget(GeneratedFile);
        
        TransformFiles(
            InputBaseName, AdditionalUsingPaths, ExcludeUsingPaths,
            addStubs, files, target);
        
        Log.LogMessage($"CircularReflection task ran ok; {InputBaseName} -> {GeneratedFile};");
        return true;
    }

    internal static void TransformFiles(string basePaths, string additionalUsings, string excludeUsings, bool addStubs, IFileSource src, IFileTarget dst)
    {
        var rewriter = new AbstractifyRewriter();

        var extraUsings = additionalUsings.Split(';', StringSplitOptions.RemoveEmptyEntries);
        foreach (var use in extraUsings)
        {
            if (string.IsNullOrWhiteSpace(use)) continue;
            rewriter.UsingDirectives.Add(use);
        }

        var body = new StringBuilder();
        var header = new StringBuilder();
        header.AppendLine($"// Generated from *.cs files in: {basePaths}");
        header.AppendLine("// ReSharper disable All "); // turn off ReSharper for the file
        header.AppendLine("#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member"); // just in case
        header.AppendLine("#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor"); // suppress null warnings
        
        // wrap everything in our 'Reflection' namespace
        header.AppendLine("namespace Reflection {");
        
        foreach (var fileInfo in src.GetFiles())
        {
            var tree = CSharpSyntaxTree.ParseText(fileInfo.BodyText);

            var root = tree.GetRoot();
            var newRoot = rewriter.Visit(root);
            body.AppendLine($"\r\n// From '{fileInfo.Path}':");
            body.AppendLine(newRoot.ToString());
        }
        
        // Remove any using directives requested
        var unwantedUsings = excludeUsings.Split(';', StringSplitOptions.RemoveEmptyEntries);
        foreach (var use in unwantedUsings)
        {
            if (string.IsNullOrWhiteSpace(use)) continue;
            rewriter.UsingDirectives.Remove(use);
        }

        // Output the de-duplicated using directives (into header, at the top of 'Reflection' namespace)
        foreach (var useDirective in rewriter.UsingDirectives)
        {
            header.AppendLine($"using {useDirective};"); // write the `using ...;` directive into the header
        }
        
        // end the 'Reflection' namespace
        body.AppendLine("}");
        
        // Add stubs if required
        if (addStubs)
        {
            body.AppendLine("// Dummy namespaces, in case the 'using' imports are for implementation code only");

            foreach (var useDirective in rewriter.UsingDirectives.Where(line => !line.Contains('=')))
            {
                body.AppendLine($"namespace Reflection.{useDirective} {{ internal abstract class CircularReferenceStub {{ }} }}");
            }
        }

        // Output generated file
        dst.Append(header.ToString());
        dst.Append(body.ToString());
    }
}