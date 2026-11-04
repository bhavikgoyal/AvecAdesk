namespace AvecADeskApi.Model.Agent;

public class AgentDto
{
    public int AgentId { get; set; }
    public string AgentName { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? PhoneNo { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedOn { get; set; }
}

public class AgentLookupDto
{
    public int AgentId { get; set; }
    public string AgentName { get; set; } = string.Empty;
}

public class AgentUpsertRequest
{
    public string? AgentName { get; set; }
    public string? ContactPerson { get; set; }
    public string? PhoneNo { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}
