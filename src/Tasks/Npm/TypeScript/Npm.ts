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
        var packageRootDirectoryPath = FindNearest(path.parse(require.resolve(packageName)), "package.json");

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
            typesRootDirectoryPath: typesRootDirectoryPath
        }
    }
    catch
    {
        return null;
    }
}

global["ResolvePackage"] = ResolvePackage;