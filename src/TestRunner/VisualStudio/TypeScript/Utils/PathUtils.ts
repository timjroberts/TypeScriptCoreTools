/// <reference types="node" />

import * as fs from "fs";
import * as path from "path";

export function FindNearest(parsedPath: path.ParsedPath, fileName: string): path.ParsedPath
{
    if (fs.existsSync(path.join(parsedPath.dir, fileName))) return parsedPath;

    var parentDirectoryPath = path.parse(path.resolve(parsedPath.dir + path.sep + parsedPath.name, ".."));

    if (!parentDirectoryPath.name) return null;

    return FindNearest(path.parse(parentDirectoryPath.dir + path.sep + parentDirectoryPath.name), fileName);
}

export function GetRelativePath(rootPath: string, absolutePath: string): string
{
    let idx = absolutePath.startsWith(rootPath) ? rootPath.length : 0;

    if (absolutePath[idx] === '\\' || absolutePath[idx] === '/') idx++;

    return absolutePath.substr(idx);
}