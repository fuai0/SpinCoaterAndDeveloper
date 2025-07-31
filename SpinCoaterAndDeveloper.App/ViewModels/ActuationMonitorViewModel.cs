using LogServiceInterface;
using MotionCardServiceInterface;
using MotionControlActuation;
using Prism.Commands;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using SpinCoaterAndDeveloper.Actuation.Actuation;
using SpinCoaterAndDeveloper.App.Common.Models;
using SpinCoaterAndDeveloper.App.Extensions;
using SpinCoaterAndDeveloper.Shared;
using SpinCoaterAndDeveloper.Shared.Extensions;
using SpinCoaterAndDeveloper.Shared.Models.ActuationGraphModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace SpinCoaterAndDeveloper.App.ViewModels
{
    public class ActuationMonitorViewModel : BindableBase, IDialogAware
    {
        private readonly IMotionCardService motionCardService;
        private readonly ILogService logService;
        private readonly ActuationGlobleResetGroupManager resetGroupManager;
        private readonly ActuationRunningGroupManager runningGroupManager;
        private readonly ActuationBurnInGroupManager burnInGroupManager;

        private CancellationTokenSource cancellationTokenSource;
        private List<ActuationManagerAbs> actuationGroupManagerList = new List<ActuationManagerAbs>();
        public ObservableCollection<ActuationMonitorModel> ActuationCollection { get; set; } = new ObservableCollection<ActuationMonitorModel>();
        public ObservableCollection<NodeModel> Nodes { get; set; } = new ObservableCollection<NodeModel>();
        public ObservableCollection<ConnectionModel> Connections { get; set; } = new ObservableCollection<ConnectionModel>();
        public ActuationMonitorViewModel(IContainerProvider containerProvider)
        {
            this.motionCardService = containerProvider.Resolve<IMotionCardService>();
            this.logService = containerProvider.Resolve<ILogService>();
            this.resetGroupManager = containerProvider.Resolve<ActuationGlobleResetGroupManager>();
            this.runningGroupManager = containerProvider.Resolve<ActuationRunningGroupManager>();
            this.burnInGroupManager = containerProvider.Resolve<ActuationBurnInGroupManager>();
            //状态机发生变更时需要将其加入groupManagerList,否则无法监视到指令状态
            actuationGroupManagerList.Add(resetGroupManager);
            actuationGroupManagerList.Add(runningGroupManager);
            actuationGroupManagerList.Add(burnInGroupManager);
        }

        public string Title { get; set; } = "ActuationMonitor".TryFindResourceEx();

#pragma warning disable 0067
        public event Action<IDialogResult> RequestClose;
#pragma warning restore 0067

        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed()
        {
            cancellationTokenSource?.Cancel();
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            cancellationTokenSource = new CancellationTokenSource();
            Task.Run(() =>
            {
                int tempDataActuationStsNums = 0;

                while (!cancellationTokenSource.IsCancellationRequested)
                {
                    Thread.Sleep(200);
                    try
                    {
                        int actuationStsNums = 0;
                        for (int i = 0; i < actuationGroupManagerList.Count; i++)
                        {
                            //获取groupManager的IsBusy状态,利用位移运算来判断当前运行的GroupManager是否有变化
                            int listStatus = actuationGroupManagerList[i].GetAsyncWorkerStatus() ? 1 << i : 0;
                            actuationStsNums += listStatus;
                        }
                        //重新初始化
                        if (tempDataActuationStsNums != actuationStsNums)
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                //初始化列表
                                ActuationCollection.Clear();
                                foreach (var groupManager in actuationGroupManagerList)
                                {
                                    foreach (var item in groupManager.GetActuationGroup())
                                    {
                                        ActuationCollection.Add(new ActuationMonitorModel()
                                        {
                                            Name = item.Value.ActuationName,
                                            Status = item.Value.GetActionStatus(),
                                            Times = item.Value.GetRunCycleTimes(),
                                            AsyncKeyWord = item.Value.GetActuaionKeyword(),
                                            AsyncRedirectName = item.Value.GetAsyncTaskResultActutaionRedirectName(),
                                            AsyncStatus = item.Value.GetAsyncTaskResultActionStatus(),
                                            StartTime = item.Value.GetActutaionStartTime(),
                                        });
                                    }
                                }
                                //添加节点
                                Nodes.Clear();
                                Connections.Clear();
                                foreach (var groupManager in actuationGroupManagerList)
                                {
                                    foreach (var item in groupManager.GetActuationGroup())
                                    {
                                        NodeModel node = new NodeModel()
                                        {
                                            ActuationName = item.Value.ActuationName,
                                            Status = item.Value.GetActionStatus(),
                                            Row = item.Value.GetGraphRow(),
                                            Col = item.Value.GetGraphColumn(),
                                        };
                                        var inputs = item.Value.GetAbsInputList();
                                        var outpts = item.Value.GetAbsOutputList();

                                        if (inputs.Count != 0)
                                        {
                                            foreach (var input in inputs)
                                            {
                                                node.Inputs.Add(new ConnectorModel() { ParentNode = node, ActuationName = input, IsConnected = true });
                                            }
                                        }
                                        if (outpts.Count != 0)
                                        {
                                            foreach (var outpt in outpts)
                                            {
                                                node.Outputs.Add(new ConnectorModel() { ParentNode = node, ActuationName = outpt, IsConnected = true, Redirect = false }); ;
                                            }
                                        }

                                        Nodes.Add(node);
                                    }
                                }
                                //添加跳转输入输出
                                foreach (var groupManager in actuationGroupManagerList)
                                {
                                    foreach (var item in groupManager.GetActuationGroup())
                                    {
                                        var redirectList = item.Value.GetAbsRedirectList();
                                        if (redirectList.Count != 0)
                                        {
                                            foreach (var redirect in redirectList)
                                            {
                                                var redirectOutput = Nodes.Where(x => x.ActuationName == item.Value.ActuationName).FirstOrDefault();
                                                redirectOutput.Outputs.Add(new ConnectorModel() { ActuationName = redirect, IsConnected = true, Redirect = true });
                                                var redirectInput = Nodes.Where(x => x.ActuationName == redirect).FirstOrDefault();
                                                redirectInput.Inputs.Add(new ConnectorModel() { ActuationName = item.Value.ActuationName, IsConnected = true, Redirect = true });
                                            }
                                        }
                                    }
                                }
                                //连线
                                foreach (var node in Nodes)
                                {
                                    foreach (var output in node.Outputs)
                                    {
                                        var nodeSourceOutput = node.Outputs.Where(x => x.ActuationName == output.ActuationName).FirstOrDefault();

                                        var nodeTarget = Nodes.Where(x => x.ActuationName == output.ActuationName).FirstOrDefault();
                                        var nodeTargetInput = nodeTarget.Inputs.Where(x => x.ActuationName == node.ActuationName).FirstOrDefault();

                                        Connections.Add(new ConnectionModel()
                                        {
                                            Source = nodeSourceOutput,
                                            Target = nodeTargetInput,
                                            Redirect = nodeSourceOutput.Redirect,
                                        });
                                    }
                                }
                                //排序
                                SortNodes(Nodes.ToList(), 400, 150);
                            });
                            tempDataActuationStsNums = actuationStsNums;
                        }
                        //刷新指令状态
                        foreach (var groupManager in actuationGroupManagerList)
                        {
                            foreach (var item in groupManager.GetActuationGroup().Values)
                            {
                                foreach (var itemShow in ActuationCollection)
                                {
                                    if (itemShow.Name == item.ActuationName)
                                    {
                                        itemShow.Status = item.GetActionStatus();
                                        itemShow.Times = item.GetRunCycleTimes();
                                        itemShow.AsyncKeyWord = item.GetActuaionKeyword();
                                        itemShow.AsyncRedirectName = item.GetAsyncTaskResultActutaionRedirectName();
                                        itemShow.AsyncStatus = item.GetAsyncTaskResultActionStatus();
                                        itemShow.StartTime = item.GetActutaionStartTime();
                                    }
                                }
                                foreach (var node in Nodes)
                                {
                                    if (node.ActuationName == item.ActuationName)
                                    {
                                        node.Status = item.GetActionStatus();
                                        node.Times = item.GetRunCycleTimes();
                                        node.AsyncKeyWord = item.GetActuaionKeyword();
                                        node.AsyncRedirectName = item.GetAsyncTaskResultActutaionRedirectName();
                                        node.AsyncStatus = item.GetAsyncTaskResultActionStatus();
                                        node.StartTime = item.GetActutaionStartTime();
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logService.WriteLog(LogTypes.DB.ToString(), $@"逻辑指令监视页面刷新指令状态线程异常:{ex.Message}", ex);
                    }
                }
            }, cancellationTokenSource.Token);
        }

        private void SortNodes(List<NodeModel> nodes, double colSpacing_X, double rowSpacing_Y)
        {
            if (nodes.Count == 0) return;

            //如果不为double.NaN则为用户手动指定位置=>不需要自动排序
            if (!double.IsNaN(nodes[0].Row))
            {
                foreach (var node in nodes)
                {
                    node.Location = new Point(colSpacing_X * node.Col, rowSpacing_Y * node.Row);
                }
            }
            //如果为double.NaN,自动排序DFS
            if (double.IsNaN(nodes[0].Row))
            {
                HashSet<string> visited = new HashSet<string>();
                double usedCol_X = 0, usedRow_Y = 0;
                foreach (var node in nodes)
                {
                    var firstNodes = nodes.Where(x => x.Inputs.Count == 0 && !visited.Contains(x.ActuationName)).FirstOrDefault();
                    if (firstNodes != null)
                    {
                        visited.Add(firstNodes.ActuationName);
                        DFS(nodes, firstNodes, visited, usedCol_X, usedRow_Y, colSpacing_X, rowSpacing_Y, ref usedCol_X, ref usedRow_Y);
                        usedRow_Y++;
                    }
                }
            }
        }

        private void DFS(List<NodeModel> nodes, NodeModel node, HashSet<string> visited, double nodeCol_X, double nodeRow_Y, double colSpacing_X, double rowSpacing_Y, ref double maxNodeCol_X, ref double maxNodeRow_Y)
        {
            node.Col = nodeCol_X;
            node.Row = nodeRow_Y;
            node.Location = new Point(node.Col * colSpacing_X, node.Row * rowSpacing_Y);

            foreach (var nextNodeConnector in node.Outputs)
            {
                double tempNodeCol = node.Col;
                double tempNodeRow = node.Row;
                if (visited.Contains(nextNodeConnector.ActuationName)) continue;
                visited.Add(nextNodeConnector.ActuationName);
                var nextNode = nodes.Where(x => x.ActuationName == nextNodeConnector.ActuationName).FirstOrDefault();
                bool rowNoUsed = true;
                foreach (var currentNodeOutput in node.Outputs)
                {
                    if (visited.Contains(currentNodeOutput.ActuationName))
                    {
                        var existNode = nodes.Where(x => x.ActuationName == currentNodeOutput.ActuationName).FirstOrDefault();
                        if (existNode.Row == tempNodeRow)
                        {
                            rowNoUsed = false;
                            break;
                        }
                    }
                }
                double nextNodeRow = rowNoUsed == true ? node.Row : ++maxNodeRow_Y;
                DFS(nodes, nextNode, visited, ++tempNodeCol, nextNodeRow, colSpacing_X, rowSpacing_Y, ref maxNodeCol_X, ref maxNodeRow_Y);
            }

            foreach (var perviousNodeConnector in node.Inputs)
            {
                double tempNodeCol = node.Col;
                double tempNodeRow = node.Row;
                if (visited.Contains(perviousNodeConnector.ActuationName)) continue;
                visited.Add(perviousNodeConnector.ActuationName);
                var perviousNode = nodes.Where(x => x.ActuationName == perviousNodeConnector.ActuationName).FirstOrDefault();
                bool rowNoUsed = true;
                foreach (var currentNodeIntput in node.Inputs)
                {
                    if (visited.Contains(currentNodeIntput.ActuationName))
                    {
                        var existNode = nodes.Where(x => x.ActuationName == currentNodeIntput.ActuationName).FirstOrDefault();
                        if (existNode.Row == tempNodeRow)
                        {
                            rowNoUsed = false;
                            break;
                        }
                    }
                }
                double perviousNodeRow = rowNoUsed == true ? node.Row : ++maxNodeRow_Y;
                DFS(nodes, perviousNode, visited, --tempNodeCol, perviousNodeRow, colSpacing_X, rowSpacing_Y, ref maxNodeCol_X, ref maxNodeRow_Y);
            }
        }
    }
}
