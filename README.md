# CircularReflection

MSBuild task that re-writes classes as abstract definitions, to allow circular references

This is useful for:
- Auto-generation of references (such as URLs from ASPNet)
- Making `nameof(...)` references
- Making `<see cref="..."/>` references in doc comments

The resulting types will be available under the `Reflection.` namespace

## Applying the build task to your project

1. Reference the NuGet package in the project that wants to _read_ the types
2. Add `<InputBase>$(MSBuildProjectDirectory)\..\SourceProject\CsFileDirectory</InputBase>` into a `<PropertyGroup>` of any project referencing this package
3. Rebuild, and start referencing types

```xml
    <PropertyGroup>
        <ReflectionInputBase>$(MSBuildProjectDirectory)\..\SourceProject\CsFileDirectory</ReflectionInputBase>
    </PropertyGroup>
```

## Referencing types in your project

The types in the source files will be added with the namespace prefix `Reflection`.
So if the source has a class `Me.MyPackage.MyClass`, the generated type will be `Reflection.Me.MyPackage.MyClass`

## Adding more using directives

If you are referencing files between different targets, you may need to include extra namespaces into the output.

```xml
    <PropertyGroup>
        <ReflectionAdditionalUsings>System;System.Collections.Generic;System.Threading.Tasks;</ReflectionAdditionalUsings>
    </PropertyGroup>
```

## Removing using directives

To exclude using directives from the output, add to this list:

```xml
    <PropertyGroup>
        <ReflectionExcludeUsings>System;System.Threading.Task</ReflectionExcludeUsings>
    </PropertyGroup>
```

## Removing `namespace` Stubs

By default, each `using ...;` namespace will have a stub class defined for it. This means the generated
code should compile without requiring additional code to get generated code to build, but can result in the
compiled assembly containing leaked namespaces.

If you set `ReflectionNamespaceStubs` to  `false`, no stub are added, so namespaces are not leaked into generated assemblies. You may need to
add some 'fake' namespaced classes to have a reliable build.

```xml
    <PropertyGroup>
        <ReflectionNamespaceStubs>false</ReflectionNamespaceStubs>
    </PropertyGroup>
```

## Generated files

The generated files are written to the project's `obj` directory
in a file named `CircularReflection.generated.cs`.
This will be linked into the sources as part of the build.

If required, the output can be moved with the `ReflectionOutputBase` setting

# Development of the package

## Building the NuGet package

First, **always** do a clean and rebuild.

Update the versions to match in the project properties ( assembly and nuget), and the nuspec file.

Open a terminal in the directory of `CircularReflection.csproj`,
and run the command `dotnet pack -o ..`.

## References

- https://learn.microsoft.com/en-us/visualstudio/msbuild/tutorial-custom-task-code-generation?view=vs-2022
- https://github.com/dotnet/samples/tree/main/msbuild/custom-task-code-generation
