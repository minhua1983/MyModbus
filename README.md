# 项目名称：MyModbus

## 1.项目简介
一个基于自研Modbus-TCPw的demo。

- 目标：学习Modbus-TCP底层通信原理
- 适用场景：学习练习项目
- 简要说明：从零开始存自研的Modbus-TCP demo

## 2.技术栈
|分类|内容|
|---|---|
|框架|.NET Framework 4.7.2|
|UI| WinForm|
|通信|自研Modbus‑TCP / 原生socket|
|数据库|SQLite System.Data.SQLite|
|其他Nuget|NLog、Dapper等|

## 3.主要功能清单

- Modbus‑TCP连接、心跳断线自动重连
- UI日志、物理日志、数据库采集日志
- 阶梯式预警和报警高亮显示(正常<->预警<->报警)
- 点位轮询采集、单点位的实时曲线
- 手动发送固定的测试报文
- 手动发送2个点位值的更新报文
- 关闭程序时确保入库数量和采集数量一致后才能关闭


## 4.运行前置条件
- .NET Framework 4.7.2
- 测试工具：Modbus‑Slave 7.5.1

## 5.快速启动步骤
2. 用 git clone 命令下载源码到本地
3. 用VS打开源码中的 MyModbus.slnx
5. 启动 Modbus‑Slave 打开 resume0.msw
6. 直接start项目，它会自动启动MyModbus.UI.exe


## 6.关键设计说明
> 写核心思路，面试重点看这一段

### 6.1 通信层
- 封装了以下数据区域及功能码的访问：
    - 0区 (01、05、0F功能码)
    - 1区 (02功能码)
    - 3区 (04功能码)
    - 4区 (03、06，10功能码)
- 实现了如下功能：
    - modbus各种数据类型和byte之间的转换工具类封装
    - 大小字、高低字节处理
    - 异常处理、采集超时处理
    - 心跳，断线重连机制
    - 自研ModbusTCP类多Task分别处理发送，接收，心跳
    - 自研ModbusContext上下文类来控制自研ModbusTCP的连接和断开，以及处理采集Task
    - 主Form中处理持久化Task来入库每个点位的采集结果
    - 接收Task中半包、粘包的处理，以及循环切帧(单个完整请求报文)，每帧发送TCS的结果
    - 基于CancellationTokenSource(CTS)的cancel信号
    - 基于TaskCompletionSouce(TCS)的异步等待信号处理
    - 基于Key为TransactionId的字典，存TCS和CTS信息
    - 发送队列、持久化(入库)队列由各自Task循环出列处理

### 6.2 数据存储
- SQLite数据库: 所有采集数值统一由持久化队列存入数据库的collect_data表
- 日志：用NLog做物理日志存储。

### 6.3 UI架构（WinForm）
- WinForm：事件驱动，后台轮询跨线程更新UI处理。