namespace RaceDay.Api.Helpers;

public static class SessionKeys
{
    public const string UserId = "UserId";
    public const string UserRole = "UserRole";
}

public static class RoleNames
{
    public const string Organiser = "Organiser";
    public const string Participant = "Participant";
}

public static class EventTypes
{
    public const string Running = "Running";
    public const string Walking = "Walking";
    public const string Cycling = "Cycling";

    public static string? Normalise(string? value)
    {
        switch (value?.Trim().ToLower())
        {
            case "running":
            case "run":
                return Running;
            case "walking":
            case "walk":
                return Walking;
            case "cycling":
            case "cycle":
                return Cycling;
            default:
                return null;
        }
    }
}

public static class EnrolmentStatuses
{
    public const string Confirmed = "Confirmed";
    public const string Cancelled = "Cancelled";
}

public static class ResultStatuses
{
    public const string Finished = "Finished";
    public const string Dnf = "DNF";
    public const string Dns = "DNS";

    public static string? Normalise(string? value)
    {
        switch (value?.Trim().ToLower())
        {
            case "finished":
                return Finished;
            case "dnf":
                return Dnf;
            case "dns":
                return Dns;
            default:
                return null;
        }
    }
}