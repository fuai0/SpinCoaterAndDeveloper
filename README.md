# SpinCoaterAndDeveloper 项目说明文档  
## 一、项目整体介绍  
### 1. 项目背景与目标  
SpinCoaterAndDeveloper 是一个面向涂胶显影设备（如半导体制造中的涂胶机、显影机）的控制系统，旨在实现对设备核心模块的自动化控制与监控。项目基于 Prism 框架开发，采用模块化设计，支持多设备协同工作、参数配置管理、实时状态监控等核心功能，适用于高精度工业生产场景。  
### 2. 核心模块组成  
项目采用模块化架构，目前包含以下核心模块:  
主程序模块:SpinCoaterAndDeveloper.App  
状态机模块:SpinCoaterAndDeveloper.FSM  
共享模块:SpinCoaterAndDeveloper.Shared  
串口通信模块:SerialPortService  
驱动控制模块:SpinCoaterAndDeveloper.Actuation  
温控器模块:TemperatureControllerService  
胶泵模块:GluePumpService  
台达指令集模块:MotionCardServiceDelta  
### 3. 技术架构  
框架：Prism（MVVM 模式，支持模块化、依赖注入、导航等）。  
通信：基于串口通信（RS485/RS232）、Modbus 协议等实现硬件交互。  
配置：XML 配置文件管理设备参数，支持动态加载与持久化。  
UI：WPF + MaterialDesignInXAML，提供现代化交互界面。  
## 二、个人负责工作  
在项目中，我目前负责温控器模块与胶泵模块的设计与实现，具体工作如下：  
### 1. 温控器模块（TemperatureControllerService）---目前已开发完毕,正在微调阶段  
该模块主要负责通过 Modbus 协议与温控设备进行通信，实现温度的读取、设置及设备管理功能，基于 Prism 框架构建，包含配置管理、UI 交互和业务逻辑处理等核心功能。  
#### (1) 核心功能  
设备配置管理：通过 XML 配置文件定义温控器的串口通信参数（端口号、波特率、校验位等），支持多设备配置。  
Modbus 通信：基于 Modbus 协议与温控设备交互，实现温度读取和目标温度设置。  
UI 交互：提供可视化界面，展示设备连接状态、当前温度、目标温度，并支持连接 / 断开、读取 / 设置温度等操作。  
状态监控：记录设备操作历史（如读取、设置、错误信息），实时更新设备状态。  
#### (2) 主要组件  
##### 模块配置（TemperatureControllerServiceModule）：  
初始化时加载多语言资源（zh-cn、en-us）。  
注册视图与视图模型的导航关系。  
从配置文件加载设备信息，并将 Modbus 客户端实例注册到依赖注入容器。  
##### 配置文件（TemperatureControllerService.config）：  
定义设备集合（TemperatureControllerConfigGroup），每个设备包含唯一标识（iocName）和串口参数（portName、baudRate等）。  
示例配置了两个设备：ATemperatureController（COM1）和BTemperatureController（COM3）。  
视图与视图模型：  
##### 视图（TemperatureControllerView.xaml）：  
展示设备列表，每个设备包含配置区域（串口参数）、操作按钮（连接 / 断开、读取 / 设置温度）和状态显示（当前温度、目标温度）。  
通过数据绑定关联视图模型的命令（如ConnectCommand、ReadTemperatureCommand）和属性（如IsConnected、CurrentTemperature）。  
##### 视图模型（TemperatureControllerViewModel）：  
实现核心业务逻辑，包括设备连接、断开、温度读取 / 设置等命令。  
通过ModbusHelper与设备通信，解析返回数据（处理字节序转换）并更新 UI。  
##### 数据模型（TemperatureControllerModel）：  
存储设备配置（串口参数、从机地址等）、运行状态（连接状态、当前温度、目标温度）和操作历史。  
维护按钮可用性（如OpenButtonIsEnabled、ControlButtonsIsEnabled），基于连接状态动态更新。  
##### Modbus 通信服务：  
通过ModbusHelper类封装 Modbus 协议操作，如初始化串口连接、读取保持寄存器（ReadHoldingRegisters）等。  
#### (3) 工作流程  
模块加载时，从配置文件读取设备列表，初始化TemperatureControllerModel实例并添加到集合。  
用户在 UI 选择设备，配置串口参数后点击 “连接”，通过ModbusClient.Init建立连接。  
连接成功后，可点击 “读取温度” 触发ReadTemperatureCommand，通过 Modbus 读取寄存器数据并解析为当前温度和目标温度。  
用户输入目标温度后点击 “设置温度”，通过SetTemperatureCommand将值写入设备寄存器。  
所有操作状态（成功 / 失败）记录到历史列表，并在 UI 实时展示。  
  
该模块通过模块化设计实现了设备的灵活配置和通信管理，适用于多温控设备的集中监控场景。  
### 2. 胶泵模块（GluePumpService）---正在开发过程中  
#### （1）模块功能设计  
实现胶泵设备的核心控制：启停控制、流量调节、回原点操作。  
支持配方管理（预设流量、速度等参数组合），满足不同工艺需求。  
集成报警机制（如过载、流量异常报警）。  
#### （2）核心实现  
通信协议适配：根据 KOGANEI F-PMCS02-7W 控制器手册，封装 GluePumpHelper 类，实现 @ORG（回原点）、@MOVR（运行配方）等专用指令。  
状态管理：设计 GluePumpModel 记录设备运行状态（当前流量、目标流量、报警信息等），通过 INotifyPropertyChanged 实现 UI 实时更新。  
配方管理：支持通过 @WRCP 指令写入配方参数，@?R 指令读取配方数据，满足工艺参数复用需求。  
异常处理：通过 @?ERR 指令查询设备错误码，实现报警信息解析与用户提示。  
