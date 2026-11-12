namespace DWIMS.Service.Process.Dtos;

public record ConfirmStepItem(
    string Title,
    int Role,
    int Page,
    double X,
    double Y,
    double Width, 
    double Height);