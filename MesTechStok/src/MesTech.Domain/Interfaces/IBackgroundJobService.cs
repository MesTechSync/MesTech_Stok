using System.Linq.Expressions;

namespace MesTech.Domain.Interfaces;

/// <summary>
/// Background job servisi — Hangfire implementasyonu ile kullanılacak.
/// </summary>
public interface IBackgroundJobService
{
    string Enqueue(Expression<Func<Task>> methodCall);
    string Schedule(Expression<Func<Task>> methodCall, TimeSpan delay);
    void AddOrUpdateRecurring(string jobId, Expression<Func<Task>> methodCall, string cronExpression);
    void RemoveRecurring(string jobId);
}
