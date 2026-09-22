namespace FireDepartmentMvp.Services;

public class AppState
{
    public event Action? Changed;
    public void NotifyChanged() => Changed?.Invoke();
}
