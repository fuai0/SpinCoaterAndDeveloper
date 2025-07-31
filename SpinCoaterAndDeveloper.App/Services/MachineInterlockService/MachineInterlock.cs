using LogServiceInterface;
using MotionCardServiceInterface;
using MotionControlActuation;
using Prism.Ioc;
using SpinCoaterAndDeveloper.Actuation.Actuation;
using SpinCoaterAndDeveloper.Shared;
using SpinCoaterAndDeveloper.Shared.Extensions;
using SpinCoaterAndDeveloper.Shared.Services.MachineInterlockService;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SpinCoaterAndDeveloper.App.Services.MachineInterlockService
{
    public class MachineInterlock : IMachineInterlock
    {
        //轴允许偏差±0.5mm
        private double axisOffset = 0.5;
        private Guid guid = Guid.Empty;
        private Task interlockTask;
        private bool interlockServiceEnable;

        private readonly ILogService logService;
        private readonly ActuationRunningGroupManager actuationRunningGroupManager;
        private readonly IMotionCardService motionCardService;
        private CancellationTokenSource cancellationTokenSource;
        private Dictionary<string, bool?> ouputStatus = new Dictionary<string, bool?>();
        private Dictionary<string, double> axisPosition = new Dictionary<string, double>();

        public InterlockStatus Status { get; private set; } = InterlockStatus.Default;
        public bool ForceCloseInterlockCheck { get; private set; } = false;
        public MachineInterlock(IContainerProvider containerProvider)
        {
            this.logService = containerProvider.Resolve<ILogService>();
            this.actuationRunningGroupManager = containerProvider.Resolve<ActuationRunningGroupManager>();
            this.motionCardService = containerProvider.Resolve<IMotionCardService>();
            this.interlockServiceEnable = Convert.ToBoolean(ConfigurationManager.AppSettings["InterlockServiceEnable"]);
        }

        /// <summary>
        /// 联锁开始锁定,默认最多等待5s
        /// </summary>
        /// <param name="maxWaitTime"></param>
        public void InterlockRecord(double maxWaitTime = 5000)
        {
            Status = InterlockStatus.Locking;
            ouputStatus.Clear();
            axisPosition.Clear();
            cancellationTokenSource = new CancellationTokenSource();
            interlockTask = Task.Run(() =>
            {
                //如果没有启用联锁功能
                if (!interlockServiceEnable)
                {
                    Status = InterlockStatus.LockFinish;
                    return;
                }

                DateTime startTime = DateTime.Now;
                try
                {
                    do
                    {
                        Thread.Sleep(20);
                        //获取动作流程是否程序意义上暂停(包含管理线程及动作线程)
                        bool processTaskStatus = actuationRunningGroupManager.GetProcessTaskPauseStatus();

                        //获取轴的到位状态是否到位停止
                        bool axesStatus = true;
                        MotionControlResource.AxisResource.Values.ToList().ForEach(x =>
                        {
                            axesStatus &= motionCardService.RealTimeCheckAxisArrivedEx(x.Name);
                        });

                        if (processTaskStatus && axesStatus) break;

                        //超时
                        if ((DateTime.Now - startTime).TotalMilliseconds > maxWaitTime)
                        {
                            logService.WriteLog(LogTypes.DB.ToString(), $@"联锁记录超时", MessageDegree.ERROR);
                            Status = InterlockStatus.LockTimeout;
                            return;
                        }
                        //取消
                        if (cancellationTokenSource.IsCancellationRequested)
                        {
                            logService.WriteLog(LogTypes.DB.ToString(), $@"联锁取消", MessageDegree.ERROR);
                            Status = InterlockStatus.LockException;
                            return;
                        }
                    } while (true);
                    //记录轴位置
                    MotionControlResource.AxisResource.Values.ToList().ForEach(x => { axisPosition.Add(x.Name, motionCardService.GetAxEncPos((short)MotionControlResource.AxisResource[x.Name].AxisIdOnCard, 1)[0] / MotionControlResource.AxisResource[x.Name].Proportion); });
                    //记录输出IO状态
                    MotionControlResource.IOOutputResource.Values.ToList().ForEach(x =>
                    {
                        if (x.Name != "三色灯_红灯" && x.Name != "三色灯_黄灯" && x.Name != "三色灯_绿灯" && x.Name != "三色灯_蜂鸣" && x.Name != "启动灯" && x.Name != "停止灯" && x.Name != "复位灯")
                            ouputStatus.Add(x.Name, motionCardService.RealTimeGetOutputStsEx(x.Name));
                    });

                    ouputStatus.Values.ToList().ForEach(x => { if (x.HasValue == false) { throw new Exception("IO获取到值为Null,请检查"); } });

                    logService.WriteLog(LogTypes.DB.ToString(), $@"联锁记录完成", MessageDegree.INFO);
                    Status = InterlockStatus.LockFinish;
                }
                catch (Exception ex)
                {
                    Status = InterlockStatus.LockException;
                    logService.WriteLog(LogTypes.DB.ToString(), $@"连锁记录异常:{ex.Message}", ex);
                }
            }, cancellationTokenSource.Token);
        }

        public (bool outputCheckResult, Dictionary<string, bool> differentOutput, bool axisCheckResult, Dictionary<string, double> differentAxis, Guid guid) InterlockCheck()
        {
            if (Status != InterlockStatus.LockFinish)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $@"联锁状态不在结束状态,请检查流程", MessageDegree.FATAL);
                throw new Exception("联锁状态不在结束状态,请检查流程");
            }
            guid = Guid.NewGuid();

            Dictionary<string, bool> differentOutput = new Dictionary<string, bool>();
            Dictionary<string, double> differentAxis = new Dictionary<string, double>();

            bool ret_Output = true;
            foreach (var item in ouputStatus)
            {
                if (item.Value != motionCardService.RealTimeGetOutputStsEx(item.Key))
                {
                    differentOutput.Add(item.Key, item.Value ?? false);
                    ret_Output &= false;
                }
            }
            bool ret_Axis = true;
            foreach (var item in axisPosition)
            {
                double currentPos = motionCardService.GetAxEncPos((short)MotionControlResource.AxisResource[item.Key].AxisIdOnCard, 1)[0] / MotionControlResource.AxisResource[item.Key].Proportion;
                if ((Math.Abs(item.Value - currentPos) >= axisOffset))
                {
                    differentAxis.Add(item.Key, item.Value);
                    ret_Axis &= false;
                }
            }
            return (ret_Output, differentOutput, ret_Axis, differentAxis, guid);
        }

        public Guid InterlockRecordTimeoutOrExceptionGuid()
        {
            guid = Guid.NewGuid();
            return guid;
        }

        public void InterlockStatusReset()
        {
            Status = InterlockStatus.Default;
            ForceCloseInterlockCheck = false;
        }

        public bool InterlockOneCycleForceColse(Guid guid)
        {
            if (guid != this.guid)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $@"传入GUID:{guid}与联锁要求GUID:{this.guid}不符,强制关闭联锁失败", MessageDegree.ERROR);
                return false;
            }
            cancellationTokenSource?.Cancel();
            if (interlockTask.Wait(100))
                logService.WriteLog(LogTypes.DB.ToString(), $@"联锁取消成功", MessageDegree.INFO);
            else
                logService.WriteLog(LogTypes.DB.ToString(), $@"联锁取消失败", MessageDegree.WARN);
            ForceCloseInterlockCheck = true;
            return true;
        }
    }
}
