// Generated from *.cs files in: C:\sourceFiles
// ReSharper disable All 
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor
namespace Reflection {
    using System.Threading;
    using CircularReflection;
    using System.Diagnostics.CodeAnalysis;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

// From 'File1.cs':
    namespace CircularReflection{
        /// <summary>
        /// MSBuild task that re-writes C# files into <c>abstract</c> definitions.
        /// </summary>
        [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
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
    namespace Common.Containers {
        internal abstract class AsyncRepeater     {
            public abstract void Dispose();
        }
    }


// From 'EdgeCases.cs':
    namespace SomethingElse{
        internal abstract class AnnoyingClass    {
            // Tricky case: expression bodied property
            public string MyExpressionProperty {get;set;}
            public abstract string AbstractMethod();
            public abstract void ExpressionMethod() ;
            public abstract string? NullableMethod(string? input);
        }
    }

}