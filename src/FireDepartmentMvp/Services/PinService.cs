namespace FireDepartmentMvp.Services;

public class PinService(IConfiguration configuration)
{
    private readonly string _leaderPin = configuration["Demo:LeaderPin"] ?? "1234";
    public bool Validate(string pin) => pin == _leaderPin;
}
