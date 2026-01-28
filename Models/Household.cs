namespace HouseholdExpenses.Models;

public class Household
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string OwnerUserId { get; set; } = "";

    public List<HouseholdMember> Members { get; set; } = new();
    public List<Expense> Expenses { get; set; } = new();
}