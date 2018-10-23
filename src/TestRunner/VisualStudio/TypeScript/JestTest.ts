/// <reference types="node" />

import * as path from "path";
import { FindNearest, TestCase, GroupedTestCase, TestDiscoverer, TestRunner } from "typescript-sdk-utils";
import { TestResult } from "./Utils/TestResult";

const Module = require("module");

const currentResolveFilenameFunc = Module._resolveFilename;

Module._resolveFilename = function (requiredPath: string, module: NodeModule)
{
    if (requiredPath[0] === '!') return currentResolveFilenameFunc(requiredPath.substring(1), module);

    // Resolve any imports for 'jest-resolve' to our own so that we can decorate it with our own
    // resolve semantics
    return currentResolveFilenameFunc(
        requiredPath === "jest-resolve"
            ? "typescript-sdk-jest-resolve"
            : requiredPath,
        module
    );
}

function DiscoverTests(containerPaths: string[]): TestCase[]
{
    return containerPaths.map(containerPath =>
    {
        let projectRootPath = FindNearest(path.parse(containerPath), "tsconfig.json");

        if (!projectRootPath) return [];

        return new TestDiscoverer(projectRootPath.dir).DiscoverTests();
    }).reduce((acc, v) => acc.concat(v), [ ]);
}

async function RunTests(groupedTestCases: GroupedTestCase[]): Promise<TestResult[]>
{
    let results: TestResult[] = [ ];

    for (let groupedTestCase of groupedTestCases)
    {
        let projectRootPath = FindNearest(path.parse(groupedTestCase.codeFilePath), "tsconfig.json");

        if (!projectRootPath) continue;

        results = [
            ...results,
            ...await new TestRunner(projectRootPath.dir).RunTests(groupedTestCase)
        ];
    }

    return results;
}

global["DiscoverTests"] = DiscoverTests;
global["RunTests"] = RunTests;