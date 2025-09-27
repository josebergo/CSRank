using Service.Extens;

namespace Service.Services
{
    /// <summary>
    /// 跳表方案比较复杂,模仿REDIS Zset有序存储 
    /// 借助AI优化的代码
    /// </summary>
    public class SkipCaseService: ISkipCaseService
    {
        private readonly Dictionary<long, decimal> _scores = new();
        private readonly Dictionary<long, SkipListNode> _nodeMap = new();
        private readonly SkipList _skipList = new();
        private readonly object _lock = new();

        public decimal UpdateScore(long customerId, decimal delta)
        {
            lock (_lock)
            {
                decimal current = _scores.TryGetValue(customerId, out var s) ? s : 0m;
                decimal newScore = current + delta;
                _scores[customerId] = newScore;

                // Remove from skip list if present
                if (_nodeMap.TryGetValue(customerId, out var node))
                {
                    _skipList.Delete(node.Entry);
                    _nodeMap.Remove(customerId);
                }

                // Add to skip list if new score > 0
                if (newScore > 0m)
                {
                    var entry = new CustomerEntry(customerId, newScore);
                    var newNode = _skipList.Insert(entry);
                    _nodeMap[customerId] = newNode;
                }

                return newScore;
            }
        }

        public List<object> GetCustomersByRank(int start, int end)
        {
            lock (_lock)
            {
                var result = new List<object>();
                var node = _skipList.GetElementByRank((ulong)start);
                int currentRank = start;
                while (node != null && currentRank <= end)
                {
                    result.Add(new
                    {
                        customerid = node.Entry.CustomerID,
                        score = node.Entry.Score,
                        rank = currentRank
                    });
                    node = node.Forwards[0];
                    currentRank++;
                }
                return result;
            }
        }

        public List<object> GetNeighbors(long customerId, int high, int low)
        {
            lock (_lock)
            {
                if (!_nodeMap.TryGetValue(customerId, out var node))
                {
                    return new List<object>();
                }

                ulong myRank = _skipList.GetRank(node.Entry);
                var result = new List<object>();

                // Higher rank neighbors (backward)

                var current = node.Backward;
                for (int i = 0; i < high; i++)
                {
                    if (current == _skipList.Head) break;
                    result.Insert(0, new
                    {
                        customerid = current.Entry.CustomerID,
                        score = current.Entry.Score,
                        rank = (long)myRank - (i + 1)
                    });
                    current = current.Backward;
                }

                // Self
                result.Add(new
                {
                    customerid = customerId,
                    score = node.Entry.Score,
                    rank = (long)myRank
                });

                // Lower rank neighbors (forward)
                current = node.Forwards[0];
                for (int i = 0; i < low; i++)
                {
                    if (current == null) break;
                    result.Add(new
                    {
                        customerid = current.Entry.CustomerID,
                        score = current.Entry.Score,
                        rank = (long)myRank + (i + 1)
                    });
                    current = current.Forwards[0];
                }

                return result;
            }
        }
    }
 
}
