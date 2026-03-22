using System.Data.Common;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace MesTech.Infrastructure.Persistence;

/// <summary>
/// PostgreSQL RLS (Row-Level Security) destegi icin DbCommandInterceptor.
/// Her SQL komutu oncesi SET app.current_tenant_id ile tenant context olusturur.
/// RLS policy'leri bu degiskeni kullanarak satir seviyesinde izolasyon saglar.
/// </summary>
public class TenantContextInterceptor : DbCommandInterceptor
{
    private readonly ITenantProvider _tenantProvider;

    public TenantContextInterceptor(ITenantProvider tenantProvider)
    {
        _tenantProvider = tenantProvider ?? throw new ArgumentNullException(nameof(tenantProvider));
    }

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
    {
        SetTenantContext(command);
        return base.ReaderExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        SetTenantContext(command);
        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> NonQueryExecuting(
        DbCommand command, CommandEventData eventData, InterceptionResult<int> result)
    {
        SetTenantContext(command);
        return base.NonQueryExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        SetTenantContext(command);
        return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override InterceptionResult<object> ScalarExecuting(
        DbCommand command, CommandEventData eventData, InterceptionResult<object> result)
    {
        SetTenantContext(command);
        return base.ScalarExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<object> result,
        CancellationToken cancellationToken = default)
    {
        SetTenantContext(command);
        return base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
    }

    private void SetTenantContext(DbCommand command)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();

        if (tenantId == Guid.Empty)
            return;

        if (command.Connection == null || command.Connection.State != System.Data.ConnectionState.Open)
            return;

        using var setCmd = command.Connection.CreateCommand();
        // Parameterized: Guid is safe but defense-in-depth against SQL injection
        var param = setCmd.CreateParameter();
        param.ParameterName = "@tenantId";
        param.Value = tenantId.ToString();
        setCmd.Parameters.Add(param);
        setCmd.CommandText = "SET app.current_tenant_id = @tenantId";
        setCmd.ExecuteNonQuery();
    }
}
