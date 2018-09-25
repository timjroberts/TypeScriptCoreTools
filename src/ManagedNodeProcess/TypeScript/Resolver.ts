/// <reference types="node" />

import
{ 
    parse as parsePath,
    join as joinPath,
    isAbsolute as isAbsolutePath,
    resolve as resolvePath
} from "path";

const Module = require("module");

const BlackList: string[] = [
	"freelist",
	"sys"
];

const BuiltInModules =
	Object.keys(
        (<any>process).binding("natives")).filter((moduleEntry) =>
        {
			return !/^_|^internal|\//.test(moduleEntry) && BlackList.indexOf(moduleEntry) === -1;
        }
    ).sort();

function SetResolveRootPath(rootPath: string): void
{
    const currentResolveFilenameFunc = Module._resolveFilename;

    Module._resolveFilename = function (requiredPath: string, module: NodeModule)
    {
        const requestingPath = parsePath(module.filename).dir;

        if (BuiltInModules.indexOf(requiredPath) >= 0)
        {
            return currentResolveFilenameFunc(requiredPath, module);
        }

        if (isAbsolutePath(requiredPath) || requiredPath[0] === '.')
        {
            const absoluteRequiredPath = isAbsolutePath(requiredPath)
                ? requiredPath
                : resolvePath(requestingPath, requiredPath);

            return currentResolveFilenameFunc(absoluteRequiredPath, module);
        }

        return currentResolveFilenameFunc(resolvePath(rootPath, "node_modules", requiredPath), module);
    }
}
global["SetResolveRootPath"] = SetResolveRootPath;
