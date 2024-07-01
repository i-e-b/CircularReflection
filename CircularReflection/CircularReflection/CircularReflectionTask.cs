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
    /// Base of the solution build
    /// </summary>
    [Required]
    public string InputBase { get; set; } = "";

    /// <summary>
    /// The filename where the class was generated
    /// </summary>
    [Output]
    public string GeneratedFile { get; set; } = "";

    /// <summary>
    /// Run the MSBuild task
    /// </summary>
    public override bool Execute()
    {
        if (!Directory.Exists(InputBase)) throw new Exception("Build base directory not found");

        var files = new FileSource(InputBase, "*.cs");
        GeneratedFile = $"{InputBase}/obj/CircularReflection.generated.cs";

        TransformFiles(InputBase, files, new FileTarget(GeneratedFile));

        Log.LogWarning($"Task ran ok; {InputBase} -> {GeneratedFile};");

        return true;
    }

    internal static void TransformFiles(string basePath, IFileSource src, IFileTarget dst)
    {
        var rewriter = new AbstractifyRewriter();
        var body = new StringBuilder();
        var header = new StringBuilder();
        header.Append($"// Generated from *.cs files in {basePath}\r\n");
        header.Append("// ReSharper disable All \r\n"); // turn off ReSharper for the file
        header.Append("#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member\r\n"); // just in case
        foreach (var fileInfo in src.GetFiles())
        {
            var tree = CSharpSyntaxTree.ParseText(fileInfo.BodyText);

            var root = tree.GetRoot();
            var newRoot = rewriter.Visit(root);
            body.Append($"\r\n// From '{fileInfo.Path}': \r\n");
            body.Append(newRoot + "\r\n");
        }

        foreach (var useDir in rewriter.UsingDirectives)
        {
            header.AppendLine(useDir);
        }
        
        dst.Append(header.ToString());
        dst.Append(body.ToString());
    }
}