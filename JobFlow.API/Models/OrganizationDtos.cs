namespace JobFlow.API.Models;

public sealed record MarkMilestoneRequest(string Milestone);
public sealed record SetTeamSizeRequest(string OrgSize, int? SeatLimit);
