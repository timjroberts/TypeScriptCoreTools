import { TestCase } from "./TestCase";

/**
 * Represents a collection of [[TestCase]] objects that are contained in the
 * same source file.
 */
export type GroupedTestCase =
{
    codeFilePath: string;
    testCases: TestCase[];
}