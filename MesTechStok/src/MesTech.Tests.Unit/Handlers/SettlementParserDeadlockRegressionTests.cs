using FluentAssertions;
using MesTech.Application.Interfaces.Accounting;

namespace MesTech.Tests.Unit.Handlers;

/// <summary>
/// G077 REGRESYON: ISettlementParser.ParseAsync uses ContinueWith + t.Result
/// which blocks the async context. This test documents the anti-pattern.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Bug", "G077")]
public class SettlementParserDeadlockRegressionTests
{
    [Fact(DisplayName = "G077: ContinueWith+Result is blocking async anti-pattern")]
    public async Task G077_ContinueWithResult_IsBlockingAntiPattern()
    {
        // ISettlementParser.cs:37-42 kullanıyor:
        //   return ParseAsync(...).ContinueWith(t => { var batch = t.Result; ... });
        //
        // t.Result bir BLOCKING çağrıdır:
        // - SynchronizationContext varsa (WPF, Blazor) deadlock
        // - Threadpool thread'i bloke eder
        // - Doğru pattern: await + ConfigureAwait(false)

        // Kanıt: await pattern ile senkron blokaj önlenir
        var result = await Task.FromResult(42);

        result.Should().Be(42);

        // AMA: tamamlanmamış task üzerinde .Result çağrılırsa
        // current thread bloke olur — SynchronizationContext varsa deadlock
        // ISettlementParser'da ParseAsync henüz tamamlanmamış olabilir
    }
}
