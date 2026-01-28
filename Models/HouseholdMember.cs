namespace HouseholdExpenses.Models;

public class HouseholdMember
{
    public int Id { get; set; }
    public int HouseholdId { get; set; }
    public string UserId { get; set; } = "";

    public Household? Household { get; set; }
}