using Service.Model;
using System.Collections.Concurrent;

namespace Service.Services
{
    /// <summary>
    /// 使用ConcurrentDictionary  SortedSet方案
    /// </summary>
    public class LeaderboardService: ILeaderboardService
    {
        //存储客户ID 分数，实现线程安全的快速查找
        private readonly ConcurrentDictionary<long, decimal> _customerScores = new ConcurrentDictionary<long, decimal>();
        
        // 使用 SortedSet 存储客户对象，根据分数和客户ID自动排序，用于生成排行榜。
        private readonly SortedSet<Customer> _leaderboard = new SortedSet<Customer>(new CustomerComparer());

        // 使用 ReaderWriterLockSlim 来保护对 _leaderboard 的并发访问，允许多个读线程或一个写线程。
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

        public decimal UpdateScore(long customerId, decimal scoreDelta)
        {
            if (scoreDelta < -1000 || scoreDelta > 1000)
            {
                throw new ArgumentOutOfRangeException(nameof(scoreDelta), "Score delta out of range");
            }

            _lock.EnterWriteLock();
            try
            {
                decimal currentScore = _customerScores.TryGetValue(customerId, out var s) ? s : 0m;
                // 如果客户当前分数大于0，说明他们在排行榜上，需要先移除旧的记录。
                if (currentScore > 0)
                {
                    _leaderboard.Remove(new Customer(customerId, currentScore));
                }

                // 计算新分数。
                decimal newScore = currentScore + scoreDelta;
                //更新
                _customerScores[customerId] = newScore;

                // 如果新分数大于0，将客户添加到排行榜中。
                if (newScore > 0)
                {
                    _leaderboard.Add(new Customer(customerId, newScore));
                }
                return newScore;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public List<object> GetByRank(int start, int end)
        {
            if (start < 1 || end < start)
            {
                throw new ArgumentException("Invalid start or end rank");
            }

            //(读写锁)允许多个线程同时获得读锁
            //如果不加锁，当 UpdateScore 正在修改 _leaderboard 集合时,可能会抛出 InvalidOperationException 异常导致崩溃
            //为了保证数据正确性和程序稳定性 而必须做出的权衡
            _lock.EnterReadLock();
            try
            {
                var result = new List<object>();
                int rank = 1;
                foreach (var cust in _leaderboard)
                {
                    // 如果当前排名已超过结束排名，则停止遍历。
                    if (rank > end) break;

                    // 如果当前排名在指定的范围内，则添加到结果列表。
                    if (rank >= start)
                    {
                        result.Add(new
                        {
                            customerid = cust.ID,
                            score = cust.Score,
                            rank
                        });
                    }
                    rank++;
                }
                return result;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public List<object> GetByCustomer(long customerId, int high, int low)
        {
            if (high < 0 || low < 0)
            {
                throw new ArgumentException("High and low must be non-negative");
            }

            _lock.EnterReadLock();
            try
            {
                // 如果客户不存在或分数为0（即不在排行榜上），则返回null。
                if (!_customerScores.TryGetValue(customerId, out decimal sc) || sc <= 0)
                {
                    return null;
                }

                var result = new List<object>();

                // 使用队列作为滑动窗口，暂存排名在目标客户之前的客户。
                var buffer = new Queue<object>();

                int rank = 1;
                bool foundTarget = false;
                int remainingLow = low;

                foreach (var cust in _leaderboard)
                {
                    var item = new { customerid = cust.ID, score = cust.Score, rank };

                    if (foundTarget)
                    {
                        // 如果已找到目标客户，则开始添加排名较低的客户。
                        result.Add(item);
                        remainingLow--;
                        if (remainingLow < 0) break;
                    }
                    else
                    {
                        // 找到了目标客户。
                        // 将缓冲区中存储的高排名客户全部移到结果列表中。
                        if (cust.ID == customerId)
                        {
                            while (buffer.Count > 0)
                            {
                                result.Add(buffer.Dequeue());
                            }
                            // 添加目标客户自己。
                            result.Add(item);
                            foundTarget = true;
                            if (low == 0) break;
                        }
                        else
                        {
                            // 在找到目标客户之前，将客户信息存入缓冲区。
                            buffer.Enqueue(item);

                            // 保持缓冲区的大小不超过 high 参数指定的值。
                            if (buffer.Count > high)
                            {
                                buffer.Dequeue();
                            }
                        }
                    }
                    rank++;
                }

                // 如果遍历完排行榜都未找到目标客户（理论上在前面已检查，此处为保险），返回null。
                if (!foundTarget) return null;

                return result;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public int GetTotalCount()
        {
            _lock.EnterReadLock();
            try
            {
                return _leaderboard.Count;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
    }
}
