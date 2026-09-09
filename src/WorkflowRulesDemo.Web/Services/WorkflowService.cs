using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WorkflowRulesDemo.Web.Data;
using WorkflowRulesDemo.Web.Models;

namespace WorkflowRulesDemo.Web.Services;

/// <summary>流程定义的 CRUD 服务，负责领域模型与 EF 实体之间的 JSON 序列化转换。</summary>
public class WorkflowService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public WorkflowService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<WorkflowDefinition>> GetAllAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var entities = await db.WorkflowDefinitions.AsNoTracking().OrderBy(w => w.Name).ToListAsync();
        return entities.Select(ToModel).ToList();
    }

    public async Task<WorkflowDefinition?> GetAsync(string id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var entity = await db.WorkflowDefinitions.AsNoTracking().FirstOrDefaultAsync(w => w.Id == id);
        return entity is null ? null : ToModel(entity);
    }

    public async Task SaveAsync(WorkflowDefinition workflow)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var entity = await db.WorkflowDefinitions.FirstOrDefaultAsync(w => w.Id == workflow.Id);
        if (entity is null)
        {
            entity = new WorkflowDefinitionEntity { Id = workflow.Id, CreatedAt = DateTime.UtcNow };
            db.WorkflowDefinitions.Add(entity);
        }

        entity.Name = workflow.Name;
        entity.Version = workflow.Version;
        entity.Description = workflow.Description;
        entity.NodesJson = JsonSerializer.Serialize(workflow.Nodes);
        entity.EdgesJson = JsonSerializer.Serialize(workflow.Edges);
        entity.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(string id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var entity = await db.WorkflowDefinitions.FirstOrDefaultAsync(w => w.Id == id);
        if (entity is not null)
        {
            db.WorkflowDefinitions.Remove(entity);
            await db.SaveChangesAsync();
        }
    }

    private static WorkflowDefinition ToModel(WorkflowDefinitionEntity entity)
    {
        return new WorkflowDefinition
        {
            Id = entity.Id,
            Name = entity.Name,
            Version = entity.Version,
            Description = entity.Description,
            Nodes = JsonSerializer.Deserialize<List<WorkflowNode>>(entity.NodesJson) ?? new List<WorkflowNode>(),
            Edges = JsonSerializer.Deserialize<List<WorkflowEdge>>(entity.EdgesJson) ?? new List<WorkflowEdge>()
        };
    }
}
