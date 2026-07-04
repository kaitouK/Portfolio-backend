namespace MyPortfolio.Service.Interface
{
    public interface IAuthService
    {
        string GenerateToken(string username);
    }
}