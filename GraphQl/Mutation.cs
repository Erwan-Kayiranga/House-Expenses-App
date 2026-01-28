using HouseholdExpenses.Auth;
using HouseholdExpenses.Data;
using HouseholdExpenses.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HouseholdExpenses.GraphQl;

public class Mutation
{
    // ✅ Create household
    [Authorize]
    public async Task<HouseholdDto> CreateHousehold(
        string name,
        ClaimsPrincipal user,
        AppDbContext db)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

        var h = new Household
        {
            Name = name,
            OwnerUserId = userId
        };

        db.Households.Add(h);
        await db.SaveChangesAsync();

        // auto-join owner as member
        var alreadyMember = await db.HouseholdMembers.AnyAsync(m =>
            m.HouseholdId == h.Id && m.UserId == userId);

        if (!alreadyMember)
        {
            db.HouseholdMembers.Add(new HouseholdMember
            {
                HouseholdId = h.Id,
                UserId = userId
            });
            await db.SaveChangesAsync();
        }

        return new HouseholdDto
        {
            Id = h.Id,
            Name = h.Name,
            IsMember = true
        };
    }

    // ✅ Join household
    [Authorize]
    public async Task<bool> JoinHousehold(
        int householdId,
        ClaimsPrincipal user,
        AppDbContext db)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

        var exists = await db.HouseholdMembers.AnyAsync(m =>
            m.HouseholdId == householdId && m.UserId == userId);

        if (exists) return true;

        db.HouseholdMembers.Add(new HouseholdMember
        {
            HouseholdId = householdId,
            UserId = userId
        });

        await db.SaveChangesAsync();
        return true;
    }

    // ✅ Add expense (split equally or custom shares)
    [Authorize]
    public async Task<ExpenseDto> CreateExpense(
        CreateExpenseInput input,
        ClaimsPrincipal user,
        AppDbContext db,
        UserManager<IdentityUser> userManager)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

        // must be member
        var isMember = await db.HouseholdMembers.AnyAsync(m =>
            m.HouseholdId == input.HouseholdId && m.UserId == userId);

        if (!isMember)
            throw new Exception("Not a member of this household.");

        // payer = current user unless input.PaidByUserId provided
        var payerId = string.IsNullOrWhiteSpace(input.PaidByUserId) ? userId : input.PaidByUserId!;

        var expense = new Expense
        {
            HouseholdId = input.HouseholdId,
            Amount = input.Amount,
            Date = input.Date,
            Description = input.Description,
            PaidByUserId = payerId
        };

        db.Expenses.Add(expense);
        await db.SaveChangesAsync();

        // members in household
        var memberIds = await db.HouseholdMembers
            .Where(m => m.HouseholdId == input.HouseholdId)
            .Select(m => m.UserId)
            .ToListAsync();

        if (memberIds.Count == 0)
            throw new Exception("Household has no members.");

        // shares
        var shares = new List<ExpenseShare>();

        if (input.SplitEqually || input.Shares == null || input.Shares.Count == 0)
        {
            var per = Math.Round(input.Amount / memberIds.Count, 2);
            var remainder = input.Amount - (per * memberIds.Count);

            for (int i = 0; i < memberIds.Count; i++)
            {
                var amt = per + (i == 0 ? remainder : 0m);
                shares.Add(new ExpenseShare
                {
                    ExpenseId = expense.Id,
                    UserId = memberIds[i],
                    Amount = amt
                });
            }
        }
        else
        {
            foreach (var s in input.Shares)
            {
                shares.Add(new ExpenseShare
                {
                    ExpenseId = expense.Id,
                    UserId = s.UserId,
                    Amount = s.Amount
                });
            }
        }

        db.ExpenseShares.AddRange(shares);
        await db.SaveChangesAsync();

        // return dto (with shares)
        var payer = await userManager.FindByIdAsync(payerId);
        var payerEmail = payer?.Email ?? payer?.UserName ?? payerId;

        var dto = new ExpenseDto
        {
            Id = expense.Id,
            HouseholdId = expense.HouseholdId,
            Amount = expense.Amount,
            Date = expense.Date,
            Description = expense.Description,
            PaidByUserId = payerId,
            PaidByEmail = payerEmail,
            Shares = new List<ExpenseShareDto>()
        };

        foreach (var s in shares)
        {
            var su = await userManager.FindByIdAsync(s.UserId);
            dto.Shares.Add(new ExpenseShareDto
            {
                UserId = s.UserId,
                Email = su?.Email ?? su?.UserName ?? s.UserId,
                Amount = s.Amount
            });
        }

        return dto;
    }

    // 🛡️ Admin delete user
    [Authorize(Roles = Roles.Admin)]
    public async Task<bool> DeleteUser(
        string userId,
        UserManager<IdentityUser> userManager)
    {
        var u = await userManager.FindByIdAsync(userId);
        if (u == null) return false;

        var res = await userManager.DeleteAsync(u);
        return res.Succeeded;
    }
}