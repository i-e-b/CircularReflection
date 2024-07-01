using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
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
/// Add this to the <c>.csproj</c> file of the destination project
/// <code><![CDATA[
///    <UsingTask TaskName="SimpleTask"
///               AssemblyFile="C:\code\datawaterfall\TestBuildTask\bin\Debug\netstandard2.1\TestBuildTask.dll" />
///
///    <Target Name="CircularReflectionTask" BeforeTargets="PrepareForBuild" Outputs="$(MSBuildProjectDirectory)\Test.generated.cs">
///        <CircularReflectionTask InputBase="$(MSBuildProjectDirectory)\..\Path\To\Sources">
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
        if (!Directory.Exists(InputBaseName)) throw new Exception("Rewriter input directory not found. Define 'InputBase' in your .csproj");

        var files = new FileSource(InputBaseName, "*.cs");
        GeneratedFile = $"{OutputBaseName}/CircularReflection.generated.cs";

        TransformFiles(InputBaseName, AdditionalUsingPaths, files, new FileTarget(GeneratedFile));

        Log.LogWarning($"Task ran ok; {InputBaseName} -> {GeneratedFile};");

        return true;
    }

    internal static void TransformFiles(string basePath, string additionalUsings, IFileSource src, IFileTarget dst)
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
        header.Append($"// Generated from *.cs files in {basePath}\r\n");
        header.Append("// ReSharper disable All \r\n"); // turn off ReSharper for the file
        header.Append("#pragma nullable disable\r\n"); // turn null testing off
        header.Append("#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member\r\n"); // just in case
        header.Append("#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor\r\n"); // suppress null warning
        foreach (var fileInfo in src.GetFiles())
        {
            var tree = CSharpSyntaxTree.ParseText(fileInfo.BodyText);

            var root = tree.GetRoot();
            var newRoot = rewriter.Visit(root);
            body.Append($"\r\n// From '{fileInfo.Path}': \r\n");
            body.Append(newRoot + "\r\n");
        }

        body.AppendLine("// Dummy namespaces, in case the 'using' imports are for implementation code only");
        foreach (var useDirective in rewriter.UsingDirectives)
        {
            header.AppendLine($"using {useDirective};"); // write the `using ...;` directive into the header

            // write a stub use of the namespace, to silence compiler problems for unused or internal usings
            body.AppendLine($"namespace {useDirective} {{ internal abstract class CircularReferenceStub {{ }} }}");
        }
        
        dst.Append(header.ToString());
        dst.Append(body.ToString());
    }
}