namespace HouseholdExpenses.Models;

public class ExpenseShare
{
    public int Id { get; set; }
    public int ExpenseId { get; set; }
    public string UserId { get; set; } = "";
    public decimal Amount { get; set; }

    public Expense? Expense { get; set; }
}