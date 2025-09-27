namespace Service.Services
{
    public interface ISkipCaseService
    {
       public decimal UpdateScore(long customerId, decimal delta);

        public List<object> GetCustomersByRank(int start, int end);

        public List<object> GetNeighbors(long customerId, int high, int low);
    }
}
