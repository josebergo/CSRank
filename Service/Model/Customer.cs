namespace Service.Model
{
    /// <summary>
    ///  使用结构体，包含ID和分数。
    /// IEquatable<T> 接口用于高效地比较两个Customer实例是否相等
    /// </summary>
    public struct Customer : IEquatable<Customer>
    {
        public long ID { get; }
        public decimal Score { get; }

        public Customer(long id, decimal score)
        {
            ID = id;
            Score = score;
        }

        //实现 IEquatable<Customer> 接口的方法。
        public bool Equals(Customer other)
        {
            return ID == other.ID;
        }

        //重写基类的 Equals 方法，以处理与任意 object 类型的比较。
        public override bool Equals(object obj)
        {
            return obj is Customer other && Equals(other);
        }

        /// <summary>
        /// 重写 GetHashCode 方法。
        /// 因为重写了 Equals 方法，得重写 GetHashCode。
        /// 如果两个对象通过 Equals 判断是相等的，那么它们的哈希码（HashCode）也得相同。
        /// 因为这里的相等性只取决于ID，所以哈希码也只根据ID生成。
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return ID.GetHashCode();
        }
    }

    /// <summary>
    /// 比较器
    /// </summary>
    public class CustomerComparer : IComparer<Customer>
    {
        public int Compare(Customer x, Customer y)
        {
            // 1. 首先比较分数。y.Score.CompareTo(x.Score)，按分数“降序”排列（分数高的排在前面）。
            var scoreCmp = y.Score.CompareTo(x.Score);

            // 2. 如果分数不相同，直接返回分数比较的结果。
            if (scoreCmp != 0) return scoreCmp;

            // 3. 如果分数相同，则比较ID。 x.ID.CompareTo(y.ID)，按ID“升序”排列（ID小的排在前面）。
            return x.ID.CompareTo(y.ID);
        }
    }

}
