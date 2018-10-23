/// <reference types="node" />

import * as os from "os";
import * as path from "path";
import { GroupedTestCase } from "./GroupedTestCase";
import { TestResult } from "./TestResult";
import { GetRelativePath } from "./PathUtils";

const jest = require("jest");

export class TestRunner
{
    private _rootPath: string;

    /**
     * Initializes a new TestRunner object.
     * 
     * @param rootPath The root path for which test cases are to be run.
     */
    public constructor(rootPath: string)
    {
        this._rootPath = rootPath;
    }

    public async RunTests(groupedTestCase: GroupedTestCase): Promise<TestResult[]>
    {
        let tsFilePath = path.parse(GetRelativePath(this._rootPath, groupedTestCase.codeFilePath));
        let jsFilePath = path.join(this._rootPath, "obj/Debug/js", tsFilePath.name + ".js");

        let result = await jest.runCLI(
            {
                config: "{}",
                resolver: "typescript-sdk-jest-resolver",
                runInBand: true,
                runTestsByPath: [ jsFilePath ],
                testNamePattern: new RegExp(`^${groupedTestCase.testCases.map(t => t.fullyQualifiedName).map(t => TestRunner.GetNamePattern(t)).join('|')}$`),
                silent: true,
                reporters: []
            },
            [this._rootPath]
        );

        let results: TestResult[] = [ ];

        if (!result.results.success) throw new Error(result.results.testResults[0].failureMessage);

        for (let testResult of result.results.testResults)
        {
            let assertions: any[] = testResult.testResults;

            results = [
                ...results,
                ...assertions
                    .filter(r => r.status !== "pending")
                    .map<TestResult>(r =>
                        {
                            let qualifiedTitle = r.ancestorTitles.length > 0 ? r.ancestorTitles.join('+') + "+" + r.title : r.title;
                            let testCase = groupedTestCase.testCases.find(testCase => testCase.fullyQualifiedName.endsWith(qualifiedTitle));
            
                            return {
                                testCase,
                                outcome: r.status,
                                errorMessage: r.failureMessages.join(os.EOL)
                            };
                        }
                    )
            ];         
        }

        return results;
    }

    private static GetNamePattern(fullyQualifiedName: string): string
    {
        let segments = fullyQualifiedName.split('.');

        return segments[segments.length - 1].replace(/\+/g, ' ');
    }
}