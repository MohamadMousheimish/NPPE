using NPPE.Domain.Constants;
using NPPE.Domain.Enums;

namespace NPPE.Domain.Entities;

/// <summary>A business cost (hosting, domain, tooling, …) recorded by an admin.</summary>
public class Cost : BaseEntity
{
    public string Provider { get; set; } = string.Empty;   // e.g. "Azure", "GoDaddy"
    public CostCategory Category { get; set; } = CostCategory.Other;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = Currencies.Canadian;
    public DateTime IncurredOn { get; set; }               // the date the cost applies to
    public bool IsRecurring { get; set; }
    public string? Note { get; set; }
    public CostSource Source { get; set; } = CostSource.Manual;
}
