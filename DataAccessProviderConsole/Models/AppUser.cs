namespace DataAccessProviderConsole.Models;

public class AppUser : IdentityUser<int>
{
    public AppUser()
    {
    }

    public AppUser(string userName)
        : base(userName)
    {
    }

    public int MemberId
    {
        get => Id;
        set => Id = value;
    }

    public MemberGroupEnum Group { get; set; } = MemberGroupEnum.regular;
    public string? InviteCode { get; set; }
    public int DateJoined { get; set; }
    public int? LastVisit { get; set; }
    public int? LastTopup { get; set; }
    public int? LastInviteSent { get; set; }
}