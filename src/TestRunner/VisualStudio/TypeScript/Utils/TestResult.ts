import { TestCase } from "./TestCase";

/**
 * Represents the result of a test.
 */
export type TestResult =
{
    testCase: TestCase;
    outcome: "passed" | "failed";
    errorMessage: string;
}