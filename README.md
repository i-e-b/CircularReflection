# CircularReflection

MSBuild task that re-writes classes as abstract definitions, to allow circular references

This is useful for:
- Auto-generation of references (such as URLs from ASPNet)
- Making `nameof(...)` references
- Making `<see cref="..."/>` references in doc comments

## Applying the build task to your project

1. Reference the NuGet package in the project that wants to _read_ the types
2. Add `<InputBase>$(MSBuildProjectDirectory)\..\SourceProject\CsFileDirectory</InputBase>` into a `<PropertyGroup>` of any project referencing this package
3. Rebuild, and start referencing types

```xml
    <PropertyGroup>
        <!-- CircularReflection settings -->
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
        <!-- CircularReflection settings -->
        <ReflectionAdditionalUsings>System;System.Collections.Generic;System.Threading.Tasks;</ReflectionAdditionalUsings>
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
