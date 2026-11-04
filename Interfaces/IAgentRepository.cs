using AvecADeskApi.Model.Agent;

namespace AvecADeskApi.Interfaces;

public interface IAgentRepository
{
    Task<IEnumerable<AgentDto>> GetAllAsync();
    Task<IEnumerable<AgentLookupDto>> GetActiveNamesAsync();
    Task<AgentDto?> GetByIdAsync(int agentId);
    /// <exception cref="InvalidOperationException">An agent with the same name already exists.</exception>
    Task<int> CreateAsync(AgentUpsertRequest request, int? userId);
    /// <exception cref="InvalidOperationException">An agent with the same name already exists.</exception>
    Task<bool> UpdateAsync(int agentId, AgentUpsertRequest request, int? userId);
    Task<bool> DeleteAsync(int agentId, int? userId);
}
