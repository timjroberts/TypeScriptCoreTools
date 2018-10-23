/// <reference types="node" />

const Resolver = require("typescript-sdk-jest-resolve");

function resolver(path: string, options: any): string
{
    return Resolver.findNodeModule(path, options);
}

export = resolver;