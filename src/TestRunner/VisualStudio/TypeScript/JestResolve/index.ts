/// <reference types="node" />

import * as fs from "fs";
import * as path from "path";
import { FindNearest } from "typescript-sdk-utils";

const JestResolver = require("!jest-resolve");

class Resolver
{
    private static readonly BypassedPackages: string[] = [ "jest" ];

    private innerResolver: any;

    public constructor(p1: any, p2: any)
    {
        this.innerResolver = new JestResolver(p1, p2);
    }

    public resolveModule(from, moduleName, options?): any
    {
        return this.resolve(moduleName, from);
    }

    public isCoreModule(...args: any[]): any
    {
        return this.innerResolver.isCoreModule(...args);
    }

    public getModule(...args: any[]): any
    {
        return this.innerResolver.getModule(...args);
    }

    public getModulePath(...args: any[]): any
    {
        return this.innerResolver.getModulePath(...args);
    }

    public getPackage(...args: any[]): any
    {
        return this.innerResolver.getPackage(...args);
    }

    public getMockModule(...args: any[]): any
    {
        return this.innerResolver.getMockModule(...args);
    }

    public getModulePaths(...args: any[]): any
    {
        return this.innerResolver.getModulePaths(...args);
    }

    public getModuleID(virtualMocks, from, moduleName): any
    {
        try
        {
            return this.innerResolver.getModuleID(virtualMocks, from, moduleName);
        }
        catch
        {
            return this.resolveModule(moduleName, from);
        }
    }

    public static findNodeModule(path: string, options: any): string
    {
        return require.resolve(path);
    }

    private resolve(requiredPath: string, from: string): string
    {
        if (path.isAbsolute(requiredPath) || requiredPath[0] === '.')
        {
            return path.isAbsolute(requiredPath)
                ? requiredPath
                : this.resolveRelative(requiredPath, from);   
        }

        if (Resolver.BypassedPackages.indexOf(requiredPath) >= 0) return require.resolve(requiredPath, { paths: [ from ]});

        let fromRootPath = FindNearest(path.parse(from), "tsconfig.json");

        return fromRootPath
            ? this.resolveFromProjectConfig(requiredPath, path.join(fromRootPath.dir, "tsconfig.json"), from)
            : require.resolve(requiredPath, { paths: [ from ]});
    }

    private resolveRelative(requiredPath: string, from: string): string
    {
        let resolvedFilePath = path.resolve(path.parse(from).dir, requiredPath);

        if (fs.existsSync(resolvedFilePath) && fs.lstatSync(resolvedFilePath).isFile()) return resolvedFilePath;

        let jsFilePath = resolvedFilePath + ".js";

        if (fs.existsSync(jsFilePath)) return jsFilePath;

        if (fs.existsSync(resolvedFilePath) && fs.lstatSync(resolvedFilePath).isDirectory()) return path.join(resolvedFilePath, "index.js");

        return jsFilePath;
    }

    private resolveFromProjectConfig(requiredPath: string, tsConfigFilePath: string, from: string): string
    {
        let config = JSON.parse(fs.readFileSync(tsConfigFilePath).toString());
        let compilerOptions = config["compilerOptions"] || { };
        let paths = compilerOptions["paths"] || { };

        let projectPaths: string[] = paths[requiredPath];

        if (!projectPaths) return require.resolve(requiredPath, { paths: [ from ]});

        if (projectPaths[0].indexOf("/obj/typings/")) return this.resolveFromProjectReference(projectPaths[0], from);
        if (projectPaths[0].indexOf("/lib/typings/")) return this.resolveFromPackageReference(projectPaths[0], from);

        return require.resolve(requiredPath, { paths: [ from ]});
    }

    private resolveFromProjectReference(projectPath: string, from: string): string
    {
        let [ projectRootPath, requiredPackageName ] = projectPath.split("/obj/typings/");
        let projectNpmFormattedPackageName = projectRootPath.substring(projectRootPath.lastIndexOf('/') + 1).replace('.', '-').toLocaleLowerCase();

        return projectNpmFormattedPackageName === requiredPackageName
            ? require.resolve(path.join(projectRootPath, "obj/Debug/js"), { paths: [ from ]})
            : require.resolve(path.join(projectRootPath, "node_modules", requiredPackageName), { paths: [ from ]});
    }

    private resolveFromPackageReference(packagePath: string, from: string): string
    {
        throw new Error("Package References are not supported");
    }
}

export = Resolver;