/**
 * Describes a test case.
 */
export type TestCase =
{
    fullyQualifiedName: string;
    codeFilePath: string;
    source: string;
    displayName: string;
    lineNumber: number;
};