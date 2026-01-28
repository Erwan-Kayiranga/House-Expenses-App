using HouseholdExpenses.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HouseholdExpenses.GraphQl;

public class Query
{
    // =========================
    // 🔐 AUTH / ROLES
    // =========================
    [Authorize]
    public async Task<List<string>> MyRoles(
        ClaimsPrincipal user,
        UserManager<IdentityUser> userManager)
    {
        var u = await userManager.GetUserAsync(user);
        if (u == null) return new();
        return (await userManager.GetRolesAsync(u)).ToList();
    }

    // =========================
    // 🏠 HOUSEHOLDS
    // =========================
    public async Task<List<HouseholdDto>> Households(
        AppDbContext db,
        ClaimsPrincipal user)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);

        var memberHouseholdIds = userId == null
            ? new HashSet<int>()
            : (await db.HouseholdMembers
                .Where(m => m.UserId == userId)
                .Select(m => m.HouseholdId)
                .ToListAsync())
              .ToHashSet();

        var list = await db.Households
            .OrderBy(h => h.Id)
            .ToListAsync();

        return list.Select(h => new HouseholdDto
        {
            Id = h.Id,
            Name = h.Name,
            IsMember = memberHouseholdIds.Contains(h.Id)
        }).ToList();
    }

    // =========================
    // 👥 HOUSEHOLD MEMBERS
    // =========================
    [Authorize]
    public async Task<List<HouseholdMemberDto>> HouseholdMembers(
        int householdId,
        AppDbContext db,
        UserManager<IdentityUser> userManager)
    {
        var members = await db.HouseholdMembers
            .Where(m => m.HouseholdId == householdId)
            .ToListAsync();

        var result = new List<HouseholdMemberDto>();

        foreach (var m in members)
        {
            var u = await userManager.FindByIdAsync(m.UserId);
            result.Add(new HouseholdMemberDto
            {
                UserId = m.UserId,
                Email = u?.Email ?? u?.UserName ?? m.UserId
            });
        }

        return result;
    }

    // =========================
    // 💸 EXPENSES
    // =========================
    [Authorize]
    public async Task<List<ExpenseDto>> Expenses(
        int householdId,
        AppDbContext db,
        UserManager<IdentityUser> userManager)
    {
        var expenses = await db.Expenses
            .Where(e => e.HouseholdId == householdId)
            .OrderByDescending(e => e.Date)
            .ToListAsync();

        var allShares = await db.ExpenseShares
            .Where(s => expenses.Select(e => e.Id).Contains(s.ExpenseId))
            .ToListAsync();

        var result = new List<ExpenseDto>();

        foreach (var e in expenses)
        {
            var payer = await userManager.FindByIdAsync(e.PaidByUserId);

            var dto = new ExpenseDto
            {
                Id = e.Id,
                HouseholdId = e.HouseholdId,
                Amount = e.Amount,
                Date = e.Date,
                Description = e.Description,
                PaidByUserId = e.PaidByUserId,
                PaidByEmail = payer?.Email ?? payer?.UserName ?? e.PaidByUserId,
                Shares = new List<ExpenseShareDto>()
            };

            foreach (var s in allShares.Where(x => x.ExpenseId == e.Id))
            {
                var su = await userManager.FindByIdAsync(s.UserId);
                dto.Shares.Add(new ExpenseShareDto
                {
                    UserId = s.UserId,
                    Email = su?.Email ?? su?.UserName ?? s.UserId,
                    Amount = s.Amount
                });
            }

            result.Add(dto);
        }

        return result;
    }

    // =========================
    // 📊 BALANCES
    // =========================
    [Authorize]
    public async Task<List<BalanceSummaryDto>> Balances(
        int householdId,
        AppDbContext db,
        UserManager<IdentityUser> userManager)
    {
        var shares = await db.ExpenseShares
            .Include(s => s.Expense)
            .Where(s => s.Expense!.HouseholdId == householdId)
            .ToListAsync();

        var paid = await db.Expenses
            .Where(e => e.HouseholdId == householdId)
            .GroupBy(e => e.PaidByUserId)
            .Select(g => new { UserId = g.Key, Total = g.Sum(x => x.Amount) })
            .ToListAsync();

        var users = shares.Select(s => s.UserId).Distinct().ToList();

        var result = new List<BalanceSummaryDto>();

        foreach (var uid in users)
        {
            var u = await userManager.FindByIdAsync(uid);

            var totalOwed = shares.Where(s => s.UserId == uid).Sum(s => s.Amount);
            var totalPaid = paid.FirstOrDefault(p => p.UserId == uid)?.Total ?? 0m;

            result.Add(new BalanceSummaryDto
            {
                UserId = uid,
                Email = u?.Email ?? u?.UserName ?? uid,
                TotalPaid = totalPaid,
                TotalOwed = totalOwed,
                Balance = totalPaid - totalOwed
            });
        }

        return result;
    }

    // =========================
    // 📅 MONTHLY SUMMARY
    // =========================
    [Authorize]
    public async Task<List<MonthlySummaryDto>> MonthlySummary(
        int householdId,
        AppDbContext db)
    {
        var list = await db.Expenses
            .Where(e => e.HouseholdId == householdId)
            .GroupBy(e => new { e.Date.Year, e.Date.Month })
            .OrderByDescending(g => g.Key.Year)
            .ThenByDescending(g => g.Key.Month)
            .Select(g => new MonthlySummaryDto
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                TotalSpent = g.Sum(x => x.Amount)
            })
            .ToListAsync();

        return list;
    }
}