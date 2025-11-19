namespace WheelApp.Application.DTOs;

public class ProjectWorkspaceDto
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public int DatasetId { get; set; }
    public int ProjectType { get; set; } // 0=Classification, 1=Detection, 2=Segmentation, 3=AnomalyDetection

    public List<ImageDto> Images { get; set; } = new();
    public List<ProjectClassDto> ProjectClasses { get; set; } = new();

    public List<RoleTypeDto> RoleTypes { get; set; } = new();
    public List<ProjectTypeDto> ProjectTypes { get; set; } = new();
}
