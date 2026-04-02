using Xunit.Abstractions;
using Xunit.Sdk;

namespace MesTech.Integration.Tests.E2E;

/// <summary>Test sıralama attribute'u — Step01, Step02... sırayla çalışsın.</summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class TestPriorityAttribute : Attribute
{
    public int Priority { get; }
    public TestPriorityAttribute(int priority) => Priority = priority;
}

/// <summary>xUnit test sıralama — TestPriority attribute'una göre sıralar.</summary>
public sealed class PriorityOrderer : ITestCaseOrderer
{
    public IEnumerable<TTestCase> OrderTestCases<TTestCase>(IEnumerable<TTestCase> testCases)
        where TTestCase : ITestCase
    {
        return testCases.OrderBy(tc =>
        {
            var attr = tc.TestMethod.Method
                .GetCustomAttributes(typeof(TestPriorityAttribute).AssemblyQualifiedName)
                .FirstOrDefault();
            return attr?.GetNamedArgument<int>("Priority") ?? 999;
        });
    }
}
