namespace Service.Extens;

public class CustomerEntry
{
    public long CustomerID { get; }
    public decimal Score { get; set; }

    public CustomerEntry(long customerId, decimal score)
    {
        CustomerID = customerId;
        Score = score;
    }
}

public class CustomerComparer : IComparer<CustomerEntry>
{
    public int Compare(CustomerEntry a, CustomerEntry b)
    {
        if (a.Score != b.Score)
        {
            return b.Score.CompareTo(a.Score); 
        }
        return a.CustomerID.CompareTo(b.CustomerID);
    }
}


/// <summary>
/// 通过维护多个层级的链表来提高查找效率的概率性数据
/// </summary>
public class SkipListNode
{
    public CustomerEntry Entry { get; }
    public SkipListNode[] Forwards { get; }
    public long[] Spans { get; }
    public SkipListNode Backward { get; set; }

    public SkipListNode(int level, CustomerEntry entry = null)
    {
        Entry = entry;
        Forwards = new SkipListNode[level];
        Spans = new long[level];
    }
}

public class SkipList
{
    private const int MaxLevel = 32; //最大层级限制（通常 32）

    private const double Probability = 0.25;  //随机生成新节点层高的辅助方法

    private readonly IComparer<CustomerEntry> _comparer = new CustomerComparer(); //比较器
    public SkipListNode Head { get; } //头节点（哨兵）

    private SkipListNode _tail; //尾节点

    private int _level; //当前最高层级

    private long _length;//列表长度
    private readonly Random _random = new();

    public SkipList()
    {
        Head = new SkipListNode(MaxLevel);
        _level = 1;
        _length = 0;
        _tail = null; 

    }

    private int RandomLevel()
    {
        int level = 1;
        while (_random.NextDouble() < Probability && level < MaxLevel)
        {
            level++;
        }
        return level;
    }

    //将新节点插入到跳表的每一层中 
    //通过一个循环 调整了链表的指针，并将新节点“编织”进去
    public SkipListNode Insert(CustomerEntry entry)
    {
        ///为即将插入的新节点，在跳表的每一层中找到了其前驱节点，并保存在 update 数组中
        var update = new SkipListNode[MaxLevel];
        var rank = new long[MaxLevel];

        //计算出了每个前驱节点 update[i] 的“排名”（即从头节点到它之间有多少个节点），并保存在 rank 数组中
        var x = Head;
        for (int i = _level - 1; i >= 0; i--)
        {
            //表示从 Head 到 update[i] 的节点数（不包括 update[i] 本身）
            rank[i] = i == (_level - 1) ? 0 : rank[i + 1];

            //在每层中，从当前 x 向前跳跃，只要下一个节点 x.Forwards[i] 的 Entry < entry
            while (x.Forwards[i] != null && _comparer.Compare(x.Forwards[i].Entry, entry) < 0)
            {
                //跳过该跨度
                rank[i] += x.Spans[i];
                //移动 
                x = x.Forwards[i];
            }
            update[i] = x;
        }

        //通过 RandomLevel() 方法为新节点随机生成了一个层高 newLevel。
        int newLevel = RandomLevel();
        if (newLevel > _level)
        {
            for (int i = _level; i < newLevel; i++)
            {
                // 设置为 Head
                update[i] = Head;
                //因为更高层为空，从头开始）
                rank[i] = 0;
            }
            _level = newLevel;
        }

        //创建一个新的跳表节点 newNode。
        var newNode = new SkipListNode(newLevel, entry);

        //这个循环从跳表的最低层 (level 0) 开始，一直到新节点所拥有的最高层 (newLevel - 1)。
        for (int i = 0; i < newLevel; i++)
        {
            //新节点 newNode 在第 i 层的 Forwards (前进) 指针，指向其前驱节点 update[i] 原本指向的下一个节点
            //把新节点连接到后续的链表中
            newNode.Forwards[i] = update[i].Forwards[i];

            //更新指针 插入到了第 i 层链表的 update[i] 节点之后
            update[i].Forwards[i] = newNode;

            //用 update[i] 原来的跨度，减去从 update[i] 到新插入位置之间的节点数，得到的就是新节点 newNode 到它下一个节点之间的跨度
            newNode.Spans[i] = update[i].Spans[i] - (rank[0] - rank[i]);

            //更新前驱节点的跨度：
            //(rank[0] - rank[i]) 同样是 update[i] 到 update[0] 之间的节点数。
            //加 1 是因为现在 update[i] 的下一个节点是 newNode，所以它的新跨度就是从它自己到 newNode 之间的距离。这个距离等于 update[i] 和 update[0] 之间的节点数，再加上 newNode 本身这 1 个节点。
            update[i].Spans[i] = (rank[0] - rank[i]) + 1;
        }

        for (int i = newLevel; i < _level; i++)
        {
            update[i].Spans[i]++;
        }

        newNode.Backward = update[0] == Head ? null : update[0];
        if (newNode.Forwards[0] != null)
        {
            newNode.Forwards[0].Backward = newNode;
        }
        else
        {
            _tail = newNode;
        }

        _length++;
        return newNode;
    }

    //删除指定 CustomerEntry 对应的节点
    //跳表删除的核心是找到目标节点的前驱节点（在每一层），然后更新指针和跨度（span），
    //以“跳过”被删除节点，而不破坏结构
    public void Delete(CustomerEntry entry)
    {
        //初始化前驱数组
        var update = new SkipListNode[MaxLevel];
        var x = Head;
        //从最高层（_level - 1）开始向下遍历每一层
        for (int i = _level - 1; i >= 0; i--)
        {
            //在每层中，从当前节点 x 向前跳跃，直到找到第一个不小于 entry 的节点的前驱
            while (x.Forwards[i] != null && _comparer.Compare(x.Forwards[i].Entry, entry) < 0)
            {
                x = x.Forwards[i];
            }
            update[i] = x;
        }

        x = x.Forwards[0];
        //切换到底层（level 0），检查 x 是否精确匹配 entry
        if (x == null || _comparer.Compare(x.Entry, entry) != 0)
        {
            //如果不存在，直接返回（不做任何修改）
            return; 
        }

        //更新前驱指针和跨度
        for (int i = 0; i < _level; i++)
        {
            if (update[i].Forwards[i] == x)
            {
                //将前驱的跨度 Spans[i] 增加 (x.Spans[i] - 1)，因为要“吸收”目标节点的跨度
                //减1 因为删除节点本身不计入跨度
                update[i].Spans[i] += x.Spans[i] - 1;

                //将前驱的 Forwards[i] 直接指向 x 的后继
                update[i].Forwards[i] = x.Forwards[i];
            }
            else
            {
                //该层的前驱不直接连到 x，可能是更高层的跳跃），
                //只需减少前驱的跨度 Spans[i]（表示路径上少了一个节点
                update[i].Spans[i]--;
            }
        }

        if (x.Forwards[0] != null)
        {
            //如果目标节点有后继（底层），将后继的 Backward 指向目标的前驱（x.Backward）
            x.Forwards[0].Backward = x.Backward;
        }
        else
        {
            //如果没有后继（目标是最后一个节点），更新全局 _tail 为目标的前驱
            _tail = x.Backward;
        }
        //调整层级和长度
        while (_level > 1 && Head.Forwards[_level - 1] == null)
        {
            _level--;
        }
        //减少列表长度
        _length--;
    }

    public SkipListNode GetElementByRank(ulong rank)
    {
        if (rank == 0)
        {
            return null;
        }

        ulong traversed = 0;
        var x = Head;

        //从最高层开始遍历
        for (int i = _level - 1; i >= 0; i--)
        {
            //在每层中：如果当前跨度 x.Spans[i] 加上已遍历 traversed 仍 ≤ rank，则跳过该跨度（traversed += x.Spans[i]），并移动到下一个节点 x = x.Forwards[i]。
            //这相当于在每层“贪婪”地跳过尽可能多的节点，直到累计排名刚好超过或等于目标
            while (x.Forwards[i] != null && traversed + (ulong)x.Spans[i] <= rank)
            {
                traversed += (ulong)x.Spans[i];
                x = x.Forwards[i];
            }
        }
        return x;
    }

    public ulong GetRank(CustomerEntry entry)
    {
        ulong rank = 0;
        var x = Head;
        for (int i = _level - 1; i >= 0; i--)
        {
            while (x.Forwards[i] != null && _comparer.Compare(x.Forwards[i].Entry, entry) < 0)
            {
                //累加的 rank 表示从头到当前 x 的节点数（不包括 x 本身）
                rank += (ulong)x.Spans[i];
                x = x.Forwards[i];
            }
        }

        //切换到底层
        x = x.Forwards[0];
        //检查并返回排名
        if (x != null && _comparer.Compare(x.Entry, entry) == 0)
        {
            return rank + 1; // 1-based
        }
        return 0;
    }
}