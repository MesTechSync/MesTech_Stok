using Xunit.Abstractions;
using Xunit.Sdk;

namespace MesTech.Tests.Integration.E2E;

/// <summary>
/// Test metotlarını Priority sırasına göre çalıştırır.
/// FullMonthE2ETests gibi sıralı akış testlerinde kullanılır.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class TestPriorityAttribute : Attribute
{
    public int Priority { get; }
    public TestPriorityAttribute(int priority) => Priority = priority;
}

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
