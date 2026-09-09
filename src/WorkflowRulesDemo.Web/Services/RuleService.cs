using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WorkflowRulesDemo.Web.Data;
using WorkflowRulesDemo.Web.Models;

namespace WorkflowRulesDemo.Web.Services;

/// <summary>规则库的 CRUD 服务，负责领域模型与 EF 实体之间的 JSON 序列化转换。</summary>
public class RuleService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public RuleService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<RuleDefinition>> GetAllAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var entities = await db.RuleDefinitions.AsNoTracking().OrderBy(r => r.Name).ToListAsync();
        return entities.Select(ToModel).ToList();
    }

    public async Task<RuleDefinition?> GetAsync(string id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var entity = await db.RuleDefinitions.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
        return entity is null ? null : ToModel(entity);
    }

    public async Task<Dictionary<string, RuleDefinition>> GetLookupAsync()
    {
        var all = await GetAllAsync();
        return all.ToDictionary(r => r.Id, r => r);
    }

    public async Task SaveAsync(RuleDefinition rule)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var entity = await db.RuleDefinitions.FirstOrDefaultAsync(r => r.Id == rule.Id);
        if (entity is null)
        {
            entity = new RuleDefinitionEntity { Id = rule.Id, CreatedAt = DateTime.UtcNow };
            db.RuleDefinitions.Add(entity);
        }

        entity.Name = rule.Name;
        entity.Description = rule.Description;
        entity.ConditionGroupJson = JsonSerializer.Serialize(rule.ConditionGroup);
        entity.ActionsJson = JsonSerializer.Serialize(rule.Actions);
        entity.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(string id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var entity = await db.RuleDefinitions.FirstOrDefaultAsync(r => r.Id == id);
        if (entity is not null)
        {
            db.RuleDefinitions.Remove(entity);
            await db.SaveChangesAsync();
        }
    }

    private static RuleDefinition ToModel(RuleDefinitionEntity entity)
    {
        return new RuleDefinition
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            ConditionGroup = JsonSerializer.Deserialize<ConditionGroup>(entity.ConditionGroupJson) ?? new ConditionGroup(),
            Actions = JsonSerializer.Deserialize<List<RuleAction>>(entity.ActionsJson) ?? new List<RuleAction>()
        };
    }
}
