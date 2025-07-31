using AutoMapper;
using DataBaseServiceInterface;
using LogServiceInterface;
using Prism.Commands;
using Prism.Ioc;
using Prism.Mvvm;
using SpinCoaterAndDeveloper.App.Common.Models;
using SpinCoaterAndDeveloper.Shared;
using SpinCoaterAndDeveloper.Shared.DataEntity;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace SpinCoaterAndDeveloper.App.ViewModels
{
    public class OptionFunctionShieldViewModel : BindableBase
    {
        private CancellationTokenSource cancellationTokenSource;
        private readonly IDataBaseService dataBaseService;
        private readonly ILogService logService;
        private readonly IMapper mapper;
        public ObservableCollection<FunctionShieldMonitorModel> FunctionShieldStatusCollection { get; set; } = new ObservableCollection<FunctionShieldMonitorModel>();
        public OptionFunctionShieldViewModel(IContainerProvider containerProvider)
        {
            this.dataBaseService = containerProvider.Resolve<IDataBaseService>("MainDb");
            this.logService = containerProvider.Resolve<ILogService>();
            this.mapper = containerProvider.Resolve<IMapper>();

            cancellationTokenSource = new CancellationTokenSource();
            Task.Run(async () =>
            {
                //不支持功能新增或删除动态切换,不知道显示到UI动态切换,重启程序即可
                try
                {
                    var functionShields = dataBaseService.Db.Queryable<FunctionShieldEntity>().Includes(x => x.ProductInfo).Where(x => x.ProductInfo.Select == true && x.EnableOnUI == true).OrderBy(x => x.Id).ToList();
                    Application.Current.Dispatcher.Invoke(() => { mapper.Map(functionShields, FunctionShieldStatusCollection); });


                    while (!cancellationTokenSource.IsCancellationRequested)
                    {
                        FunctionShieldStatusCollection.ToList().ForEach(fs =>
                        {
                            if (GlobalValues.MCFunctionShieldDicCollection.ContainsKey(fs.Name))
                                fs.IsActive = GlobalValues.MCFunctionShieldDicCollection[fs.Name].IsActive;
                        });
                        await Task.Delay(10_000, cancellationTokenSource.Token);
                    }
                }
                catch (Exception ex)
                {
                    logService.WriteLog(LogTypes.DB.ToString(), $@"刷新功能屏蔽线程异常:{ex.Message}", ex);
                }
            }, cancellationTokenSource.Token);
        }
        ~OptionFunctionShieldViewModel()
        {
            cancellationTokenSource?.Cancel();
        }
    }
}
