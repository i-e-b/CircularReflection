using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CircularReflection;
using NUnit.Framework;

namespace CircularReflectionTests;

[TestFixture]
public class RewriterTests
{
    [Test]
    public void path_test()
    {
        // C:\code\datawaterfall\DataWaterfallModels\..\State\Controllers;C:\code\datawaterfall\DataWaterfallModels\..\Command\Controllers
        Console.WriteLine(Path.GetFullPath("C:\\code\\datawaterfall\\DataWaterfallModels\\..\\State\\Controllers"));
    }

    [Test]
    public void can_rewrite_csharp_files()
    {
        var target = new TestFileOutput();
        var files = new TestFileInput();

        files.Add("File1.cs", @"
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.CodeAnalysis.CSharp;
using Namespace.Imported.In.Both.Files;

namespace CircularReflection;
/// <summary>
/// MSBuild task that re-writes C# files into <c>abstract</c> definitions.
/// </summary>
[SuppressMessage(""ReSharper"", ""ClassNeverInstantiated.Global"")]
[SuppressMessage(""ReSharper"", ""MemberCanBePrivate.Global"")]
public class CircularReflectionTask : Task
{
    /// <summary>
    /// The name of the class which is going to be generated
    /// </summary>
    [Required]
    public int BuildBase { get; set; }

    /// <summary>
    /// The filename where the class was generated
    /// </summary>
    [Output]
    public int GeneratedFile { get; set; }

    /// <summary>
    /// Run the MSBuild task
    /// </summary>
    public override bool Execute()
    {
        if (!Directory.Exists(BuildBase)) throw new Exception();
        return true;
    }

    internal static void TransformFiles(IFileSource src, IFileTarget dst)
    {
        // Should be removed, as it's not public
        var rewriter = new AbstractifyRewriter();
        var body = new StringBuilder();
        var header = new StringBuilder();
        
        dst.Append(header.ToString());
        dst.Append(body.ToString());
    }
}

internal class FileTarget : IFileTarget
{
    private readonly string _path;

    public FileTarget(string path)
    {
        _path = path;
        File.Delete(path);
    }

    public void Append(string content) => File.AppendAllText(_path, content);
}
");
        files.Add("File2.cs", @"
using System.Threading.Task;
using Namespace.Imported.In.Both.Files;

namespace Common.Containers {

    /// <summary>
    /// Repeats an async task until disposed.
    /// </summary>
    public abstract class AsyncRepeater : IDisposable
    {
        /// <summary>
        /// Run the periodic task.
        /// This should return the delay until the next run.
        /// </summary>
        protected abstract Task<TimeSpan> PeriodicTask();

        private readonly CancellationTokenSource _tokenSource = new();
        
        /// <summary>
        /// Create a repeater
        /// </summary>
        protected AsyncRepeater() { Task.Run(async () => { await TaskLoop(); }); }

        /// <summary>
        /// Dispose of resources if <c>using</c> was neglected.
        /// </summary>
        ~AsyncRepeater() => Dispose();

        /// <summary>
        /// Stop repeating, and dispose of any resources
        /// </summary>
        public void Dispose()
        {
            _tokenSource.Cancel();
            DisposeInternal();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Override to release resources.
        /// </summary>
        protected virtual void DisposeInternal() { }

        private async Task TaskLoop()
        {
            Log.Info($""Repeating task started: {GetType().Name}"");
            while (!_tokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    var delay = await PeriodicTask();

                    if (delay < TimeSpan.FromMinutes(1))
                    {
                        await Task.Delay(delay, _tokenSource.Token);
                    }
                    else
                    {
                        Log.Warn($""Repeating task '{GetType().Name}' requested abnormal delay time: {delay}"");
                        await Task.Delay(TimeSpan.FromSeconds(10), _tokenSource.Token);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($""Failure in periodic task of AsyncRepeater {GetType().Name}"", ex);
                    await Task.Delay(TimeSpan.FromSeconds(10), _tokenSource.Token);
                }
            }
            Log.Info($""Repeating task ended: {GetType().Name}"");
        }
    }
}
");
        files.Add("EdgeCases.cs", @"
using Client = Somewhere.Specific.Client; // disambiguation of overused names
namespace SomethingElse;
    public class AnnoyingClass: BaseClass, IHaveAnInterface
    {
        // Tricky case: expression bodied property
        public string MyExpressionProperty => string.Empty;

        // Method that is already abstract
        public abstract string AbstractMethod();

        // Expression bodied method
        public void ExpressionMethod() => throw new NotARealException();

        // Expression bodied method
        public string? NullableMethod(string? input) {
            return input ?? string.Empty;
        }

        // Method that is an override
        public override string ToString(){
            return string.Empty; // we can't remove the method, as we're taking off the base class
        } 
    }
    
    class IgnoreMe // Private class should not be exposed
    {
        public abstract void What();
    }

");
        
        CircularReflectionTask.TransformFiles("C:\\sourceFiles", "System.Threading.Task;CircularReflection;System.Diagnostics.CodeAnalysis", files, target);

        Console.WriteLine(target.Content.ToString());

        Assert.That(target.Content.ToString(), Is.EqualTo(@"// Generated from *.cs files in: C:\sourceFiles
// ReSharper disable All 
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor
using System.Threading.Task;
using CircularReflection;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.CodeAnalysis.CSharp;
using Namespace.Imported.In.Both.Files;
using Somewhere.Specific;

// From 'File1.cs': 
namespace Reflection.CircularReflection{
/// <summary>
/// MSBuild task that re-writes C# files into <c>abstract</c> definitions.
/// </summary>
[SuppressMessage(""ReSharper"", ""ClassNeverInstantiated.Global"")]
[SuppressMessage(""ReSharper"", ""MemberCanBePrivate.Global"")]
internal abstract class CircularReflectionTask {
    /// <summary>
    /// The name of the class which is going to be generated
    /// </summary>
    [Required]
    public int BuildBase { get; set; }

    /// <summary>
    /// The filename where the class was generated
    /// </summary>
    [Output]
    public int GeneratedFile { get; set; }
public abstract bool Execute();
}
}

// From 'File2.cs': 
namespace Reflection.Common.Containers{
internal abstract class AsyncRepeater     {
public abstract void Dispose();
    }
}


// From 'EdgeCases.cs': 
namespace Reflection.SomethingElse{
internal abstract class AnnoyingClass    {
        // Tricky case: expression bodied property
        public string MyExpressionProperty {get;set;}
public abstract string AbstractMethod();
public abstract void ExpressionMethod() ;
public abstract string? NullableMethod(string? input);
    }
}

// Dummy namespaces, in case the 'using' imports are for implementation code only
namespace System.Threading.Task { internal abstract class CircularReferenceStub { } }
namespace CircularReflection { internal abstract class CircularReferenceStub { } }
namespace System.Diagnostics.CodeAnalysis { internal abstract class CircularReferenceStub { } }
namespace Microsoft.Build.Framework { internal abstract class CircularReferenceStub { } }
namespace Microsoft.Build.Utilities { internal abstract class CircularReferenceStub { } }
namespace Microsoft.CodeAnalysis.CSharp { internal abstract class CircularReferenceStub { } }
namespace Namespace.Imported.In.Both.Files { internal abstract class CircularReferenceStub { } }
namespace Somewhere.Specific { internal abstract class CircularReferenceStub { } }
"));
    }
}

class TestFileOutput : IFileTarget
{
    public readonly StringBuilder Content = new();
    
    public void Append(string content)
    {
        Content.Append(content);
    }
}

class TestFileInput : IFileSource
{
    private readonly List<IFileProxy> _files = new();
    
    public void Add(string path, string content)
    {
        _files.Add(new TestFile(path, content));
    }

    public IEnumerable<IFileProxy> GetFiles()
    {
        return _files;
    }

    class TestFile : IFileProxy
    {
        public TestFile(string path, string content)
        {
            BodyText = content;
            Path = path;
        }

        public string BodyText { get; }
        public string Path { get; }
    }
}