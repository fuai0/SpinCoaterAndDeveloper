#SpinCoaterAndDeveloper 项目说明文档

##一、项目整体介绍
###1. 项目背景与目标
SpinCoaterAndDeveloper 是一个面向涂胶显影设备（如半导体制造中的涂胶机、显影机）的控制系统，旨在实现对设备核心模块的自动化控制与监控。项目基于 Prism 框架开发，采用模块化设计，支持多设备协同工作、参数配置管理、实时状态监控等核心功能，适用于高精度工业生产场景。
###2. 核心模块组成
项目采用模块化架构，目前包含以下核心模块:
主程序模块:SpinCoaterAndDeveloper.App
状态机模块:SpinCoaterAndDeveloper.FSM
共享模块:SpinCoaterAndDeveloper.Shared
串口通信模块:SerialPortService
驱动控制模块:SpinCoaterAndDeveloper.Actuation
温控器模块:TemperatureControllerService
胶泵模块:GluePumpService
台达指令集模块:MotionCardServiceDelta
###3. 技术架构
框架：Prism（MVVM 模式，支持模块化、依赖注入、导航等）。
通信：基于串口通信（RS485/RS232）、Modbus 协议等实现硬件交互。
配置：XML 配置文件管理设备参数，支持动态加载与持久化。
UI：WPF + MaterialDesignInXAML，提供现代化交互界面。
##二、个人负责工作
在项目中，我目前负责温控器模块与胶泵模块的设计与实现，具体工作如下：
###1. 温控器模块（TemperatureControllerService）---目前已开发完毕,正在微调阶段
####（1）模块功能设计
实现与温控器设备的串口通信，支持温度采集、目标温度设置、运行状态监控。
支持多温控器设备并行管理（通过 IOC 名称区分不同设备）。
集成异常处理机制（如通信超时、设备离线报警）。
####（2）核心实现
通信层：基于 Modbus 协议封装 ModbusHelper 类，实现寄存器读写、温度数据解析（支持字节序转换、数据校验）。
配置管理：设计 TemperatureControllerConfig 类，解析 XML 配置文件中的串口参数（波特率、校验位等），支持配置持久化。
业务逻辑：在 TemperatureControllerViewModel 中实现温度闭环控制、历史数据记录、UI 状态同步等功能。
模块化集成：通过 TemperatureControllerServiceModule 注册模块服务，实现与 Prism 框架的集成。
###2. 胶泵模块（GluePumpService）---正在开发过程中
####（1）模块功能设计
实现胶泵设备的核心控制：启停控制、流量调节、回原点操作。
支持配方管理（预设流量、速度等参数组合），满足不同工艺需求。
集成报警机制（如过载、流量异常报警）。
####（2）核心实现
通信协议适配：根据 KOGANEI F-PMCS02-7W 控制器手册，封装 GluePumpHelper 类，实现 @ORG（回原点）、@MOVR（运行配方）等专用指令。
状态管理：设计 GluePumpModel 记录设备运行状态（当前流量、目标流量、报警信息等），通过 INotifyPropertyChanged 实现 UI 实时更新。
配方管理：支持通过 @WRCP 指令写入配方参数，@?R 指令读取配方数据，满足工艺参数复用需求。
异常处理：通过 @?ERR 指令查询设备错误码，实现报警信息解析与用户提示。
