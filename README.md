# 排行榜服务

这是一个基于 ASP.NET Core 的排行榜服务。

## 功能

*   **更新客户分数**: 允许通过 API 更新客户的分数。
*   **查询排行榜**: 提供多种方式查询排行榜数据。
    *   按排名范围查询。
    *   按特定客户及其邻近排名查询。
*   **高性能**: 针对大数据量场景进行了性能优化，例如百万级数据的快速插入和查询。

## API 端点

### 更新分数

*   `POST /customer/{customerid}/score/{score}`
    *   **描述**: 为指定 `customerid` 的客户增加 `score` 分数。
    *   **参数**:
        *   `customerid`: 客户的唯一标识 (long)。
        *   `score`: 要增加的分数 (decimal)。
    *   **返回**: 更新后的总分数。

### 按排名查询排行榜

*   `GET /leaderboard?start={start}&end={end}`
    *   **描述**: 获取从 `start` 排名到 `end` 排名的客户列表。
    *   **参数**:
        *   `start`: 开始排名 (int)。
        *   `end`: 结束排名 (int)。
    *   **返回**: 客户列表。

### 按客户查询排行榜

*   `GET /leaderboard/{customerid}?high={high}&low={low}`
    *   **描述**: 获取指定 `customerid` 的客户，以及其前面 `high` 个和后面 `low` 个排名的客户。
    *   **参数**:
        *   `customerid`: 客户的唯一标识 (long)。
        *   `high`: 前面的客户数量 (int, 可选)。
        *   `low`: 后面的客户数量 (int, 可选)。
    *   **返回**: 客户列表。

## 如何运行

1.  **安装 .NET 8 SDK**: 确保您的开发环境中已安装 [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)。
2.  **克隆仓库**:
    ```bash
    git clone https://github.com/josebergo/CSRank.git
    cd CSRank
    ```
3.  **运行项目**:
    在 `Rank` 目录下运行以下命令:
    ```bash
    dotnet run
    ```
4.  **访问 API**:
    项目启动后，您可以通过 `http://localhost:5000` (或您配置的其他地址) 访问 API。您可以通过 Swagger UI (`http://localhost:5000/swagger`) 来方便地测试 API。

## 性能测试

项目中包含了一些用于性能测试的 API 端点:

*   `POST /test/insert-1m`: 插入一百万个客户数据以进行测试。
*   `GET /test/rank-500-10000`: 测试获取排名在 500 到 10000 之间的客户。
*   `GET /test/customer-neighbors-25000`: 测试获取 ID 为 25000 的客户及其邻近的客户。

这些测试端点可以帮助评估服务在不同场景下的性能表现。