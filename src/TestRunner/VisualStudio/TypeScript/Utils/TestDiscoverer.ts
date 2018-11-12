/// <reference types="node" />

import * as fs from "fs";
import * as path from "path";
import { createSourceFile, forEachChild, ScriptTarget, SyntaxKind, Node, CallExpression, isIdentifier, SourceFile } from "typescript";
import { TestCase } from "./TestCase";
import { GetRelativePath } from "./PathUtils";

const glob = require("glob");

/**
 * Discovers and reports Jest test cases by parsing '.ts' files.
 */
export class TestDiscoverer
{
    private static JestDescribeIdentiferName: string = "describe";
    private static JestTestIdentiferName: string = "test";
    private static JestTestAliasIdentiferName: string = "it";

    private static PackageNameCache: Map<string, string> = new Map<string, string>();

    private _rootPath: string;

    /**
     * Initializes a new TestDiscoverer object.
     * 
     * @param rootPath The root path for which test cases are required.
     */
    public constructor(rootPath: string)
    {
        this._rootPath = rootPath;
    }

    /**
     * Discovers and reports on any test cases.
     * 
     * @returns An array of TestCase objects.
     */
    public DiscoverTests(): TestCase[]
    {
        let sourceFiles = glob.sync("**/*.ts", { cwd: this._rootPath, ignore: ["obj/**", "node_modules/**"] });

        return sourceFiles.map(sourceFile =>
        {
            let sourceFilePath = path.join(this._rootPath, sourceFile);

            let sourceFileNode = createSourceFile(
                sourceFilePath, fs.readFileSync(sourceFilePath).toString(),
                ScriptTarget.Latest, true
            );
    
            return TestDiscoverer.ParseSourceNode(sourceFilePath, this._rootPath, sourceFilePath, sourceFileNode);
        }).reduce((acc, v) => acc.concat(v), [ ]);
    }

    private static ParseSourceNode(containerPath: string, rootPath: string, sourceFilePath: string, sourceFile: SourceFile): TestCase[]
    {
        let testCases: TestCase[] = [ ];
        let stack: string[] = [ ];
    
        let visitNode = (node: Node) =>
        {
            if (node.kind === SyntaxKind.CallExpression)
            {
                let callExp = node as CallExpression;
                let callIdentifier = callExp.getFirstToken();
    
                if (isIdentifier(callIdentifier))
                {
                    if (callIdentifier.getText() === TestDiscoverer.JestDescribeIdentiferName)
                    {
                        stack.push(callExp.arguments[0].getText().replace(/"/g, ''));
                        forEachChild(node, visitNode);
                        stack.pop();

                        return;
                    }

                    if (callIdentifier.getText() === TestDiscoverer.JestTestIdentiferName || callIdentifier.getText() === TestDiscoverer.JestTestAliasIdentiferName)
                    {
                        let testName = callExp.arguments[0].getText().replace(/"/g, '');
                        let fullyQualifiedName = TestDiscoverer.GetFullyQualifiedName(
                            rootPath,
                            sourceFilePath,
                            stack.length > 0 ? stack.join('+') + "+" + testName : testName
                        );
        
                        testCases.push({
                            source: containerPath,
                            fullyQualifiedName,
                            codeFilePath: sourceFilePath,
                            displayName: fullyQualifiedName,
                            lineNumber: sourceFile.getLineAndCharacterOfPosition(callIdentifier.getStart()).line + 1
                        });
                    }
                }
            }

            forEachChild(node, visitNode);
        };
    
        visitNode(sourceFile);
    
        return testCases;
    }

    private static GetFullyQualifiedName(rootPath: string, sourceFilePath: string, testName: string): string
    {
        return TestDiscoverer.GetNormalizedPackageName(rootPath)
            + "." + GetRelativePath(rootPath, sourceFilePath).replace(/\.ts|\.tsx/g, '').replace(/\/|\\/g, '.')
            + "." + testName;
    }

    /**
     * Retrieves a package name from the '.csproj' filename in the root path.
     * 
     * @param rootPath The root path.
     * 
     * @returns A string representing the package name derived from the project file found
     * in the given root path.
     * 
     * @remarks
     * The package name for a given root path is returned from an internal cache once it has
     * been discovered.
     */
    private static GetNormalizedPackageName(rootPath: string): string
    {
        if (TestDiscoverer.PackageNameCache.has(rootPath)) return TestDiscoverer.PackageNameCache.get(rootPath);

        let rootProjectFile = fs.readdirSync(rootPath)
            .map(file => path.parse(file))
            .find(file => file.ext === ".csproj");

        let packageName = rootProjectFile.name.replace(/\./g, "-").toLocaleLowerCase();

        TestDiscoverer.PackageNameCache.set(rootPath, packageName);

        return packageName;
    }
}