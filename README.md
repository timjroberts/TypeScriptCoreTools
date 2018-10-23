# TypeScript Core Tools (alpha)

Provides tooling for building TypeScript class libraries in a .NET Core world.

The TypeScript Core Tools are a collection of SDKs and tools that make working with modern day TypeScript
projects more natural to .NET Core developers. Large TypeScript projects can be restored, built, and tested
using familiar `dotnet` infrastructure including calls to the CLI's `restore`, `build`, and `test`
commands. Also, because you're building for the web, WebPack is managed automatically by the SDK. You can
build bundles from ordinary npm packages (i.e., vendor bundles), or group them together in bundles including
your own projects.

*Quick Links*

* [Getting Started](#gettingstarted)
  * [Create a TypeScript Class Library](#createclasslib)
  * [Project References](#projectrefs)
  * [Create a TypeScript (Jest) Test Project](#createtestlib)
* [Recipes](#recipes)
  * [Creating a 'Vendor' bundle](#vendorbundle)

## <a name="gettingstarted"></a> Getting Started

TypeScript Core Tools allow you to build TypeScript class libraries and test projects by extending the
`Microsoft.NET.Sdk` MSBuild SDK and by providing a custom Test Adapter for Test projects that discovers and
executes Jest tests written in TypeScript. Currently, you have to manually create and then modify the projects,
but the changes are small.

### <a name="createclasslib"></a> Create a TypeScript Class Library

Create a standard C# class library project. For example, using the CLI run the following command:
`dotnet new classlib`. Delete the generated `.cs` file, and then open the `.csproj` file so that you can make
the following edits:

1. Add `TypeScript.Sdk` as an additional MSBuild SDK:  
   Your top level `Project` element should now look like this: `<Project Sdk="Microsoft.NET.Sdk;TypeScript.Sdk">`

2. Add an `index.ts` file.  
   During a build, TypeScript.Sdk will look for this module and treat it as the exporting module for your package.

3. Add other `.ts` files as required by your project, ensuring to export anything that is consumable by other packages from your `index.ts` module.

When you now run `dotnet build` your TypeScript files will be compiled using your globally installed version of
TypeScript, and a WebPack bundle and manifest file will be generated in the `bin` folder. You can now drop a reference to this
bundle into a script tag.

### <a name="projectrefs"></a> Project References

As your project begins to grow you'll naturally want to begin modularizing your code and moving types between projects. Do this as
you normally would by [creating additional class libraries](#createclasslib) and then making reference to them as any other 
project reference. For example, edit a `.csproj` file and add a `<ProjectReference />` element to an `<ItemGroup />`.

When the project is restored, the `tsconfig.json` file will be updated with path information to support your IDE's intellisense features.
During a build, the WebPack process will see the dependant project as an external reference which is resolvable via data found in
the dependant project's `manifest.json` file.

You now need to drop references to your bundles into script tags in order of their dependency, however, it is also possible to
create a project that has the sole responsibility of creating a bundle from all its project references and then make reference to this
_meta_ bundle in a single script tag. This helps keep your script tags to a minimum, and more manageable in large projects.

### <a name="createtestlib"></a> Create a TypeScript (Jest) Test Project

Create a standard C# XUnit project. For example, using the CLI run the following command: `dotnet new xunit`. Delete the generated
`.cs` file, and then open the `.csproj` file so that you can make the following edits:

1. Add `TypeScript.Sdk` as an additional MSBuild SDK:  
   Your top level `Project` element should now look like this: `<Project Sdk="Microsoft.NET.Sdk;TypeScript.Sdk">`

2. Remove the `<PackageReference />` elements that refer to both the `xunit` and `xunit.runner.visualstudio` packages.

3. Add a `<PackageReference />` element to the `TypeScript.Sdk.TestRunner.VisualStudio` package.

4. Add `.ts` files that contain your Jest tests. You can use an import of Jest (i.e., `import 'jest';`) to automatically bring in
the Jest declarations such as `describe`, `test`, etc.

5. Additional `<ProjectReference />` elements can be used to reference projects that should be included in your test definitions.

When you run `dotnet build` and `dotnet test` the TypeScript.Sdk test adapater will now be invoked that discovers and runs your
unit tests. You can run `dotnet test` as you would for any other Test project. For example, running `dotnet test -t` will display the
fully qualified test names in the format '_packageName_.folder1.folderN._describeBlock_._testName_', or by applying a filter
such as `dotnet test --filter "FullyQualifiedName~testname"`.

## <a name="recipes"></a> Recipes

### <a name="vendorbundle"></a> Creating a 'Vendor' bundle

It's common to create a bundle containing all the third-party packages that your website will need, for example, Reaact, Redux, React-DOM.
Your other packages will likely depend on these libraries one way or the other so it makes sense to bundle these dependencies into a single
package that can be downloaded only once on your webpage.

1. [Create a class library](#createclasslib) project.

2. Create a `package.json` file in the project and add `dependencies` to the packages you want to bundle.
   The TypeScript.Sdk tools will ensure that any types are included provided that the referenced packages contains its typings, or that an associated '@types' package can be found.

3. Add an `exportDependencies` array to the `package.json` file that names each of the packages that is to be exported from the compiled bundle.

The compiled WebPack bundle produced by this project will now include all the exported packages listed in the steps above. Furthermore, you
can now add a `<ProjectReference />` to this project to begin using its exported packages as you would normally. Once the reference has been
added, simply make an `import` to that package to begin using it. The referencing project will have its `tsconfig.json` file updated
during a project restore to include paths to all the exported packages.