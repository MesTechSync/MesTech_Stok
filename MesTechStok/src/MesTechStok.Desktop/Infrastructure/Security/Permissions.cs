namespace MesTechStok.Desktop.Infrastructure.Security;

public static class ProductPermissions
{
    public const string Read   = "product.read";
    public const string Write  = "product.write";
    public const string Delete = "product.delete";
    public const string Import = "product.import";
    public const string Export = "product.export";
}

public static class StockPermissions
{
    public const string Read     = "stock.read";
    public const string Write    = "stock.write";
    public const string Transfer = "stock.transfer";
    public const string Adjust   = "stock.adjust";
}

public static class OrderPermissions
{
    public const string Read   = "order.read";
    public const string Write  = "order.write";
    public const string Cancel = "order.cancel";
    public const string Refund = "order.refund";
}

public static class CrmPermissions
{
    public const string LeadRead     = "crm.lead.read";
    public const string LeadWrite    = "crm.lead.write";
    public const string LeadDelete   = "crm.lead.delete";
    public const string DealRead     = "crm.deal.read";
    public const string DealWrite    = "crm.deal.write";
    public const string DealManage   = "crm.deal.manage";
    public const string ContactRead  = "crm.contact.read";
    public const string ContactWrite = "crm.contact.write";
}

public static class FinancePermissions
{
    public const string ExpenseRead    = "finance.expense.read";
    public const string ExpenseWrite   = "finance.expense.write";
    public const string ExpenseApprove = "finance.expense.approve";
    public const string BankRead       = "finance.bank.read";
    public const string BankWrite      = "finance.bank.write";
    public const string ReportRead     = "finance.report.read";
}
