using AutoMapper;
using DataBaseServiceInterface;
using LogServiceInterface;
using Prism.Commands;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using SpinCoaterAndDeveloper.App.Common.Models;
using SpinCoaterAndDeveloper.Shared.DataEntity;
using SpinCoaterAndDeveloper.Shared.Extensions;
using SpinCoaterAndDeveloper.Shared.Models.MovementPointSecurityGraphModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace SpinCoaterAndDeveloper.App.ViewModels
{
    public class MCMovementPointSecurityViewModel : BindableBase, IDialogAware
    {
        private readonly int colSpacing = 260;
        private readonly int rowSpacing = 100;
        private readonly IMapper mapper;
        private readonly ILogService logService;
        private readonly IDataBaseService dataBaseService;

        private SecurityConnectorModel source;

        private MovementPointInfoModel _MovementPoint;
        public MovementPointInfoModel MovementPoint
        {
            get { return _MovementPoint; }
            set { SetProperty(ref _MovementPoint, value); }
        }
        private SecurityNodeModel _CurrentSelectedNode;
        public SecurityNodeModel CurrentSelectedNode
        {
            get { return _CurrentSelectedNode; }
            set { SetProperty(ref _CurrentSelectedNode, value); }
        }
        private string _GraphMessage;
        public string GraphMessage
        {
            get { return _GraphMessage; }
            set { SetProperty(ref _GraphMessage, value); }
        }
        public SecurityPendingConnectionModel PendingConnection { get; set; }
        public ObservableCollection<MovementPointInfoModel> MovementPointCollection { get; set; } = new ObservableCollection<MovementPointInfoModel>();
        public ObservableCollection<CylinderInfoModel> CylinderCollection { get; set; } = new ObservableCollection<CylinderInfoModel>();
        public ObservableCollection<IOOutputInfoModel> OutputCollection { get; set; } = new ObservableCollection<IOOutputInfoModel>();
        public ObservableCollection<AxisInfoModel> AxisCollection { get; set; } = new ObservableCollection<AxisInfoModel>();
        public ObservableCollection<AxisInfoModel> SafeAxisCollection { get; set; } = new ObservableCollection<AxisInfoModel>();
        public ObservableCollection<SecurityNodeModel> SecurityNodes { get; set; } = new ObservableCollection<SecurityNodeModel>();
        public ObservableCollection<SecurityConnectionModel> SecurityConnections { get; set; } = new ObservableCollection<SecurityConnectionModel>();
        public MCMovementPointSecurityViewModel(IContainerProvider containerProvider)
        {
            this.mapper = containerProvider.Resolve<IMapper>();
            this.logService = containerProvider.Resolve<ILogService>();
            this.dataBaseService = containerProvider.Resolve<IDataBaseService>("MainDb");

            PendingConnection = new SecurityPendingConnectionModel();
        }

        private DelegateCommand<MovementPointInfoModel> _AddMovementPointNodeCommand;
        public DelegateCommand<MovementPointInfoModel> AddMovementPointNodeCommand =>
            _AddMovementPointNodeCommand ?? (_AddMovementPointNodeCommand = new DelegateCommand<MovementPointInfoModel>(ExecuteAddMovementPointNodeCommand));

        void ExecuteAddMovementPointNodeCommand(MovementPointInfoModel parameter)
        {
            var node = new SecurityNodeModel() { NodeType = MovementPointSecurityTypes.MovementPoint, NodeName = parameter.Name, Location = new Point(SecurityNodes.Count * colSpacing, rowSpacing) };
            node.Inputs.Add(new SecurityConnectorModel() { IsConnected = true, IsInput = true, ParentNode = node });
            node.Outputs.Add(new SecurityConnectorModel() { IsConnected = true, IsInput = false, ParentNode = node });
            SecurityNodes.Add(node);
        }

        private DelegateCommand<CylinderInfoModel> _AddCylinderNodeCommand;
        public DelegateCommand<CylinderInfoModel> AddCylinderNodeCommand =>
            _AddCylinderNodeCommand ?? (_AddCylinderNodeCommand = new DelegateCommand<CylinderInfoModel>(ExecuteAddCylinderNodeCommand));

        void ExecuteAddCylinderNodeCommand(CylinderInfoModel parameter)
        {
            var node = new SecurityNodeModel() { NodeType = MovementPointSecurityTypes.Cylinder, NodeName = parameter.Name, Location = new Point(SecurityNodes.Count * colSpacing, rowSpacing) };
            node.Inputs.Add(new SecurityConnectorModel() { IsConnected = true, IsInput = true, ParentNode = node });
            node.Outputs.Add(new SecurityConnectorModel() { IsConnected = true, IsInput = false, ParentNode = node });
            SecurityNodes.Add(node);
        }

        private DelegateCommand<IOOutputInfoModel> _AddOutputNodeCommand;
        public DelegateCommand<IOOutputInfoModel> AddOutputNodeCommand =>
            _AddOutputNodeCommand ?? (_AddOutputNodeCommand = new DelegateCommand<IOOutputInfoModel>(ExecuteAddOutputNodeCommand));

        void ExecuteAddOutputNodeCommand(IOOutputInfoModel parameter)
        {
            var node = new SecurityNodeModel() { NodeType = MovementPointSecurityTypes.IOOutput, NodeName = parameter.Name, Location = new Point(SecurityNodes.Count * colSpacing, rowSpacing) };
            node.Inputs.Add(new SecurityConnectorModel() { IsConnected = true, IsInput = true, ParentNode = node });
            node.Outputs.Add(new SecurityConnectorModel() { IsConnected = true, IsInput = false, ParentNode = node });
            SecurityNodes.Add(node);
        }

        private DelegateCommand<AxisInfoModel> _AddAxisOriginalNodeCommand;
        public DelegateCommand<AxisInfoModel> AddAxisOriginalNodeCommand =>
            _AddAxisOriginalNodeCommand ?? (_AddAxisOriginalNodeCommand = new DelegateCommand<AxisInfoModel>(ExecuteAddAxisOriginalNodeCommand));

        void ExecuteAddAxisOriginalNodeCommand(AxisInfoModel parameter)
        {
            var node = new SecurityNodeModel() { NodeType = MovementPointSecurityTypes.AxisOriginal, NodeName = parameter.Name, Location = new Point(SecurityNodes.Count * colSpacing, rowSpacing) };
            node.Inputs.Add(new SecurityConnectorModel() { IsConnected = true, IsInput = true, ParentNode = node });
            node.Outputs.Add(new SecurityConnectorModel() { IsConnected = true, IsInput = false, ParentNode = node });
            SecurityNodes.Add(node);
        }

        private DelegateCommand<AxisInfoModel> _AddAxisSafeNodeCommand;
        public DelegateCommand<AxisInfoModel> AddAxisSafeNodeCommand =>
            _AddAxisSafeNodeCommand ?? (_AddAxisSafeNodeCommand = new DelegateCommand<AxisInfoModel>(ExecuteAddAxisSafeNodeCommand));

        void ExecuteAddAxisSafeNodeCommand(AxisInfoModel parameter)
        {
            var node = new SecurityNodeModel() { NodeType = MovementPointSecurityTypes.AxisSafePoint, NodeName = parameter.Name, Location = new Point(SecurityNodes.Count * colSpacing, rowSpacing) };
            node.Inputs.Add(new SecurityConnectorModel() { IsConnected = true, IsInput = true, ParentNode = node });
            node.Outputs.Add(new SecurityConnectorModel() { IsConnected = true, IsInput = false, ParentNode = node });
            SecurityNodes.Add(node);
        }

        private DelegateCommand<SecurityConnectorModel> _StartCommand;
        public DelegateCommand<SecurityConnectorModel> StartCommand =>
            _StartCommand ?? (_StartCommand = new DelegateCommand<SecurityConnectorModel>(ExecuteStartCommand));

        void ExecuteStartCommand(SecurityConnectorModel source)
        {
            if (source.IsInput)
            {
                PendingConnection.IsVisiable = false;
            }
            else if (source.HasConnected == true)
            {
                PendingConnection.IsVisiable = false;
            }
            else
            {
                PendingConnection.IsVisiable = true;
                this.source = source;
            }
        }

        private DelegateCommand<SecurityConnectorModel> _FinishCommand;

        public DelegateCommand<SecurityConnectorModel> FinishCommand =>
            _FinishCommand ?? (_FinishCommand = new DelegateCommand<SecurityConnectorModel>(ExecuteFinishCommand));

        void ExecuteFinishCommand(SecurityConnectorModel target)
        {
            if (target != null && target.IsInput == true && source.HasConnected == false && target.HasConnected == false)
            {
                source.HasConnected = true;
                target.HasConnected = true;
                source.ConnectorNode = target.ParentNode;
                target.ConnectorNode = source.ParentNode;

                SecurityConnections.Add(new SecurityConnectionModel()
                {
                    Source = source,
                    Target = target
                });
            }
        }

        private DelegateCommand<SecurityConnectionModel> _DeleteTransitionCommand;
        public DelegateCommand<SecurityConnectionModel> DeleteTransitionCommand =>
            _DeleteTransitionCommand ?? (_DeleteTransitionCommand = new DelegateCommand<SecurityConnectionModel>(ExecuteDeleteTransitionCommand));

        void ExecuteDeleteTransitionCommand(SecurityConnectionModel parameter)
        {
            parameter.Source.ParentNode.Outputs[0].ConnectorReset();
            parameter.Target.ParentNode.Inputs[0].ConnectorReset();
            var deleteConnection = SecurityConnections.Where(x => x == parameter).FirstOrDefault();
            SecurityConnections.Remove(deleteConnection);
        }

        private DelegateCommand _DeleteSelectionCommand;
        public DelegateCommand DeleteSelectionCommand =>
            _DeleteSelectionCommand ?? (_DeleteSelectionCommand = new DelegateCommand(ExecuteDeleteSelectionCommand));

        void ExecuteDeleteSelectionCommand()
        {
            if (CurrentSelectedNode == null) return;
            if (CurrentSelectedNode.Inputs[0].ConnectorNode != default)
            {
                var deleteConnection = SecurityConnections
                    .Where(x => x.Source == CurrentSelectedNode.Inputs[0].ConnectorNode.Outputs[0] && x.Target == CurrentSelectedNode.Inputs[0])
                    .FirstOrDefault();
                SecurityConnections.Remove(deleteConnection);
                CurrentSelectedNode.Inputs[0].ConnectorNode.Outputs[0].ConnectorReset();
            }
            if (CurrentSelectedNode.Outputs[0].ConnectorNode != default)
            {
                var deleteConnection = SecurityConnections
                    .Where(x => x.Source == CurrentSelectedNode.Outputs[0] && x.Target == CurrentSelectedNode.Outputs[0].ConnectorNode.Inputs[0])
                    .FirstOrDefault();
                SecurityConnections.Remove(deleteConnection);
                CurrentSelectedNode.Outputs[0].ConnectorNode.Inputs[0].ConnectorReset();
            }
            SecurityNodes.Remove(CurrentSelectedNode);
            CurrentSelectedNode = null;
        }

        private DelegateCommand _ConfirmCommand;
        public DelegateCommand ConfirmCommand =>
            _ConfirmCommand ?? (_ConfirmCommand = new DelegateCommand(ExecuteConfirmCommand));

        void ExecuteConfirmCommand()
        {
            if (SecurityNodes.Count == 0)
            {
                MovementPoint.MovementPointSecuritiesCollection.Clear();
                RequestClose?.Invoke(null);
                return;
            }

            if (SecurityNodes.Where(x => x.Inputs[0].ConnectorNode == default).Count() > 1)
            {
                GraphMessage = "动作图有多个起点,请检查!";
                return;
            }

            int sequence = 0;
            //FirstNode
            var node = SecurityNodes.Where(x => x.Inputs[0].ConnectorNode == default).FirstOrDefault();
            MovementPoint.MovementPointSecuritiesCollection.Clear();
            MovementPoint.MovementPointSecuritiesCollection.Add(new MovementPointSecurityModel()
            {
                Sequence = sequence,
                SecurityTypes = node.NodeType,
                BoolSecurityTypeValue = node.BoolValue,
                Name = node.NodeName,
                IOOutputSecurityTypeDelayValue = node.IntDelayTime,
            });
            while (node.Outputs[0].ConnectorNode != default)
            {
                sequence++;
                node = SecurityNodes.Where(x => x == node.Outputs[0].ConnectorNode).FirstOrDefault();
                MovementPoint.MovementPointSecuritiesCollection.Add(new MovementPointSecurityModel()
                {
                    Sequence = sequence,
                    SecurityTypes = node.NodeType,
                    BoolSecurityTypeValue = node.BoolValue,
                    Name = node.NodeName,
                    IOOutputSecurityTypeDelayValue = node.IntDelayTime,
                });
            }
            RequestClose?.Invoke(null);
        }

        private DelegateCommand _ResetGraphCommand;
        public DelegateCommand ResetGraphCommand =>
            _ResetGraphCommand ?? (_ResetGraphCommand = new DelegateCommand(ExecuteResetGraphCommand));

        void ExecuteResetGraphCommand()
        {
            SecurityNodes.Clear();
            SecurityConnections.Clear();
            CreateGraph();
        }

        private DelegateCommand _ClearGraphCommand;
        public DelegateCommand ClearGraphCommand =>
            _ClearGraphCommand ?? (_ClearGraphCommand = new DelegateCommand(ExecuteClearGraphCommand));

        void ExecuteClearGraphCommand()
        {
            SecurityNodes.Clear();
            SecurityConnections.Clear();
        }
        public string Title { get; set; } = "MovementPointSecurity".TryFindResourceEx();

        public event Action<IDialogResult> RequestClose;

        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed()
        {

        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            MovementPoint = parameters.GetValue<MovementPointInfoModel>("MovementPointModel");

            var movementpoints = dataBaseService.Db.Queryable<MovementPointInfoEntity>().Where(x => x.ProductInfo.Select == true).ToList();
            mapper.Map(movementpoints, MovementPointCollection);
            var cylinders = dataBaseService.Db.Queryable<CylinderInfoEntity>()
                .Includes(x => x.SingleValveOutputInfo)
                .Includes(x => x.DualValveOriginOutputInfo)
                .Includes(x => x.DualValveMovingOutputInfo)
                .Includes(x => x.SensorOriginInputInfo)
                .Includes(x => x.SensorMovingInputInfo)
                .OrderBy(x => x.Number)
                .OrderBy(x => x.Id)
                .ToList();
            mapper.Map(cylinders, CylinderCollection);
            var outputs = dataBaseService.Db.Queryable<IOOutputInfoEntity>().OrderBy(x => x.Number).OrderBy(x => x.Number).ToList();
            mapper.Map(outputs, OutputCollection);
            var axes = dataBaseService.Db.Queryable<AxisInfoEntity>().OrderBy(x => x.Number).OrderBy(x => x.Number).ToList();
            mapper.Map(axes, AxisCollection);
            var safeAxes = dataBaseService.Db.Queryable<AxisInfoEntity>().Where(x => x.SafeAxisEnable == true).OrderBy(x => x.Number).ToList();
            mapper.Map(safeAxes, SafeAxisCollection);
            CreateGraph();
        }

        private void CreateGraph()
        {
            SecurityNodeModel previousNode = null;
            for (int i = 0; i < MovementPoint.MovementPointSecuritiesCollection.Count; i++)
            {
                var node = new SecurityNodeModel()
                {
                    NodeType = MovementPoint.MovementPointSecuritiesCollection[i].SecurityTypes,
                    NodeName = MovementPoint.MovementPointSecuritiesCollection[i].Name,
                    Location = new Point(i * colSpacing, rowSpacing),
                    BoolValue = MovementPoint.MovementPointSecuritiesCollection[i].BoolSecurityTypeValue,
                    IntDelayTime = MovementPoint.MovementPointSecuritiesCollection[i].IOOutputSecurityTypeDelayValue,
                };
                var inputConnector = new SecurityConnectorModel() { IsConnected = true, IsInput = true, ParentNode = node };
                node.Inputs.Add(inputConnector);
                var outputConnector = new SecurityConnectorModel() { IsConnected = true, IsInput = false, ParentNode = node };
                node.Outputs.Add(outputConnector);
                SecurityNodes.Add(node);
                if (previousNode != null)
                {
                    SecurityConnectorModel source = previousNode.Outputs[0];
                    SecurityConnectorModel target = node.Inputs[0];
                    source.HasConnected = true;
                    target.HasConnected = true;
                    source.ConnectorNode = target.ParentNode;
                    target.ConnectorNode = source.ParentNode;
                    SecurityConnections.Add(new SecurityConnectionModel()
                    {
                        Source = source,
                        Target = target,
                    });
                }
                previousNode = node;
            }
        }
    }
}
