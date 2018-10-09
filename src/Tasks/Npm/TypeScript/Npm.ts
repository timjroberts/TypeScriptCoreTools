/// <reference types="node" />

import * as path from "path";
import * as fs from "fs";

/**
 * Represents information about a resolved package.
 */
type ResolvedPackage = {
    resolvedDirectoryPath: string;
    resolvedVersion: string;
    typesRootDirectoryPath?: string;
    hasResolvedEntryPoint: boolean;
};

/**
 * Finds the nearest file in a given directory hierarchy.
 * 
 * @param parsedPath A child path to begin searching for the required file.
 * @param fileName The file name of the file to find in the given directory hierarchy.
 */
function FindNearest(parsedPath: path.ParsedPath, fileName: string): path.ParsedPath
{
    if (fs.existsSync(path.join(parsedPath.dir, fileName))) return parsedPath;

    var parentDirectoryPath = path.parse(path.resolve(parsedPath.dir + path.sep + parsedPath.name, ".."));

    if (!parentDirectoryPath.name) return null;

    return FindNearest(path.parse(parentDirectoryPath.dir + path.sep + parentDirectoryPath.name), fileName);
}

/**
 * Resolves the root directory path of a given types package (i.e., a package in the '@types' scope).
 * 
 * @param typesPackageName The types package name.
 */
function ResolveTypesPackageDirectoryPath(typesPackageName: string): string
{
    var typesRootDirectoryPath = path.join(process.cwd(), "node_modules", typesPackageName.replace('/', path.sep));

    return fs.existsSync(typesRootDirectoryPath)
        ? typesRootDirectoryPath
        : "";
}

function ResolvePackage(packageName: string): ResolvedPackage
{
    try
    {
        var packageRootDirectoryPath = null;
        var hasResolvedEntryPoint = true;

        try
        {
            packageRootDirectoryPath = FindNearest(path.parse(require.resolve(packageName)), "package.json");
        }
        catch
        { }

        if (!packageRootDirectoryPath)
        {
            // We'll try and resolve the package directly to the node_modules folder, and if that is found with a 'package.json'
            // file, then we'll identify it as a resolved package that hasn't got an entry point.
            //
            // react uses a types package called 'csstype' that works like this. We want its typings, but it doesn't use the
            // the usual @type scope
            packageRootDirectoryPath = FindNearest(path.parse(path.join(process.cwd(), "node_modules", packageName, "package.json")), "package.json");

            hasResolvedEntryPoint = false;
        }

        if (!packageRootDirectoryPath) return null;

        var packageObj = JSON.parse(
            fs.readFileSync(path.join(packageRootDirectoryPath.dir, "package.json"), { flag: "r" }).toString()
        );

        var typesIndexFilePath = path.join(
            packageRootDirectoryPath.dir,
            packageObj["types"] || packageObj["typings"] || "./index.d.ts"
        );

        var typesRootDirectoryPath = fs.existsSync(typesIndexFilePath)
            ? FindNearest(path.parse(typesIndexFilePath), "package.json").dir
            : ResolveTypesPackageDirectoryPath(`@types/${packageName}`);

        return {
            resolvedDirectoryPath: packageRootDirectoryPath.dir,
            resolvedVersion: packageObj["version"],
            typesRootDirectoryPath: typesRootDirectoryPath,
            hasResolvedEntryPoint: hasResolvedEntryPoint
        }
    }
    catch
    {
        return null;
    }
}

global["ResolvePackage"] = ResolvePackage;