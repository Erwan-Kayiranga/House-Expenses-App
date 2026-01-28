namespace HouseholdExpenses.GraphQl;

// =====================
// OUTPUT DTOs
// =====================

public class HouseholdDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public bool IsMember { get; set; }  // used by UI to show "Open" button etc.
}

public class HouseholdMemberDto
{
    public string UserId { get; set; } = "";
    public string Email { get; set; } = "";
}

public class ExpenseShareDto
{
    public string UserId { get; set; } = "";
    public string Email { get; set; } = "";
    public decimal Amount { get; set; }
}

public class ExpenseDto
{
    public int Id { get; set; }
    public int HouseholdId { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string Description { get; set; } = "";

    public string PaidByUserId { get; set; } = "";
    public string PaidByEmail { get; set; } = "";

    public List<ExpenseShareDto> Shares { get; set; } = new();
}

public class BalanceSummaryDto
{
    public string UserId { get; set; } = "";
    public string Email { get; set; } = "";
    public decimal TotalPaid { get; set; }
    public decimal TotalOwed { get; set; }
    public decimal Balance { get; set; }
}

public class MonthlySummaryDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal TotalSpent { get; set; }
}

// =====================
// INPUT DTOs (GraphQL inputs)
// =====================

public class CreateExpenseShareInput
{
    public string UserId { get; set; } = "";
    public decimal Amount { get; set; }
}

public class CreateExpenseInput
{
    public int HouseholdId { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = "";
    public DateTime Date { get; set; }

    // ✅ optional: if null/empty => use current logged user as payer
    public string? PaidByUserId { get; set; }

    public bool SplitEqually { get; set; } = true;
    public List<CreateExpenseShareInput>? Shares { get; set; }
}