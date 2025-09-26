namespace Service.Services
{
    public interface ILeaderboardService
    {
        decimal UpdateScore(long customerId, decimal scoreDelta);
        List<object> GetByRank(int start, int end);
        List<object> GetByCustomer(long customerId, int high, int low);

        int GetTotalCount();
    }
}
