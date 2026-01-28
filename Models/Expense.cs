namespace HouseholdExpenses.Models;

public class Expense
{
    public int Id { get; set; }
    public int HouseholdId { get; set; }

    public decimal Amount { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public string Description { get; set; } = "";

    public string PaidByUserId { get; set; } = "";

    public Household? Household { get; set; }
    public List<ExpenseShare> Shares { get; set; } = new();
}