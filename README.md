# SmartLockTcp
# Branch 20190805 
# 20190805
  1.添加 HttpHandlers 为app提交数据
  
  2.tcp服务器逻辑修改
# 20190809
  1.完成基本框架
  
  2.剩余 考虑问题：
    a.数据安全性
    
    b.云端问题
    
    c.硬件实际测试
    
    d.后期逻辑修改
# 20190812
  Smart Lock Sleep
# Branch Master
# 20190801
  1.添加Http 服务器

  2.线程锁逻辑

  3.异常断开逻辑

  4.服务器主动断开逻辑
# 20190731
  1.服务器只负责 gps 传递数据 以及其他 结果的转发

  2.逻辑以及线程已测试

  3.linux上已测试tcp部分

  4.去除 目前没有用的成员变量 和 结构体
# 20190730
  1.根据需求更改process thread 的逻辑 

  2.优化成员变量 删除一些重复的 成员变量 共用同一个 pair ref

  3.修改FLockInfo结构体 将固定的因素和Multiple human 因素 加入结构体

  4.修改json数据格式 例子：{"type":"0","result":"true"}

  5.添加 try catch 到 process 的 switch 中 保证json数据不完整时 服务器不会崩溃

  6.已经过window .net core 测试 工具(serial tool--Tcp client)
# 20190726
  1.添加 静态全局类 保存一些常量

  2.为单一实例添加单例模板类

  3.用信号量控制处理线程个数(考虑是否需要改成线程池)
# 20190725
  1.修复了Client异常断开 服务器崩溃

  2.修复了多个数据处理线程冲突的问题

  3.现在每个端口只有一个监听线程实例，由监听线程 开启处理线程(处理线程可以是多个)
# 20190724
  1..Net core Tcp服务器项目

  2.目标平台：Linux

  3.数据包格式：Json

  3.远程数据MySQL(Windows)

  4.处理端口：7000(App),8000(SmartLock)
  
