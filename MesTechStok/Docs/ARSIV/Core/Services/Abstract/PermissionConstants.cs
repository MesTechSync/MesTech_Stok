namespace MesTechStok.Core.Services.Abstract;

public static class PermissionConstants
{
    public static class Modules
    {
        public const string Products = "Products";
        public const string Orders = "Orders";
        public const string Inventory = "Inventory";
        public const string Reports = "Reports";
        public const string Exports = "Exports";
        public const string Settings = "Settings";
        public const string OpenCart = "OpenCart";
    }

    public static class Permissions
    {
        public const string Create = "Create";
        public const string Edit = "Edit";
        public const string Delete = "Delete";
        public const string View = "View";
        public const string Export = "Export";
        public const string UpdateStock = "UpdateStock";
        public const string UpdatePrice = "UpdatePrice";
        public const string Cancel = "Cancel";
        public const string UpdateStatus = "UpdateStatus";
        // Inventory-specific granular actions
        public const string Add = "Add";
        public const string Remove = "Remove";
        public const string Transfer = "Transfer";
    }
}


