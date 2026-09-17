namespace Keues.API.Dtos.Requests.UserGroups;

public class UpdateUserGroupRequest
{
    public string Name { get; set; }
    public string Color { get; set; }
    public Guid LocationId { get; set; }
    public List<Guid> UserIds { get; set; } = new List<Guid>();
    
}