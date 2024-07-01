// Generated from *.cs files in C:\sourceFiles
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

// From 'File1.cs': 
namespace Reflection.CircularReflection{
/// <summary>
/// MSBuild task that re-writes C# files into <c>abstract</c> definitions.
/// </summary>
[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public abstract class CircularReflectionTask {
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
public abstract class AsyncRepeater     {
public abstract void Dispose();
    }
}


// From 'EdgeCases.cs': 
namespace Reflection.SomethingElse{
public abstract class AnnoyingClass    {
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
