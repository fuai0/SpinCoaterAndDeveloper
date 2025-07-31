using DataBaseServiceInterface;
using FSM;
using LogServiceInterface;
using MotionCardServiceInterface;
using MotionControlActuation;
using Prism.Events;
using SpinCoaterAndDeveloper.Shared.DataEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;

namespace SpinCoaterAndDeveloper.Shared.Extensions
{
    public static class MotionControlResourceExtensions
    {
        /// <summary>
        /// 设定输出点位输出
        /// 如果输出点位屏蔽打开,则始终输出屏蔽时默认值.屏蔽值不参与取反计算.如果未屏蔽且取反Enable,则输出value的取反值
        /// </summary>
        /// <param name="motionCard"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool SetOuputStsEx(this IMotionCardService motionCard, string name, bool value)
        {
#if DEBUG
            if (!MotionControlResource.IOOutputResource.ContainsKey(name))
                throw new Exception($"设置输出点位状态时,IOOutputResource中不包含输出点位{name}");
#endif
            //如果输出点位屏蔽打开,则始终输出屏蔽时默认值.屏蔽值不参与取反计算.如果未屏蔽且取反Enable,则输出value取反值
            return motionCard.SetEcatDoBit((short)(MotionControlResource.IOOutputResource[name].ProgramAddressGroup * 8 + MotionControlResource.IOOutputResource[name].ProgramAddressPosition),
                MotionControlResource.IOOutputResource[name].ShieldEnable ? MotionControlResource.IOOutputResource[name].GetShiedlEnableDefaultValue() :
                    MotionControlResource.IOOutputResource[name].ReverseEnable ? !value : value);
        }
        /// <summary>
        /// 获取输入点位状态
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static bool GetInputStsEx(this IMotionCardService motionCard, string name)
        {
#if DEBUG
            if (!MotionControlResource.IOInputResource.ContainsKey(name))
                throw new Exception($"获取输入点位状态时,IOInputResource中不包含输入点位{name}");
#endif
            return MotionControlResource.IOInputResource[name].Status;
        }
        /// <summary>
        /// 获取输入点位状态,静态函数
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static bool GetInputStsEx(string name)
        {
#if DEBUG
            if (!MotionControlResource.IOInputResource.ContainsKey(name))
                throw new Exception($"获取输入点位状态时,IOInputResource中不包含输入点位{name}");
#endif
            return MotionControlResource.IOInputResource[name].Status;
        }
        /// <summary>
        /// 实时获取输入点位状态
        /// </summary>
        /// <param name="motionCard"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static bool? RealTimeGetInputStsEx(this IMotionCardService motionCard, string name)
        {
#if DEBUG
            if (!MotionControlResource.IOInputResource.ContainsKey(name))
                throw new Exception($"获取输入点位状态时,IOInputResource中不包含输入点位{name}");
#endif
            return motionCard.GetEcatDiBit((short)(MotionControlResource.IOInputResource[name].ProgramAddressGroup * 8 + MotionControlResource.IOInputResource[name].ProgramAddressPosition));
        }
        /// <summary>
        /// 获取输出点位状态
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static bool GetOutputStsEx(this IMotionCardService motionCard, string name)
        {
#if DEBUG
            if (!MotionControlResource.IOOutputResource.ContainsKey(name))
                throw new Exception($"获取输出点位状态时,IOOutputResource中不包含输出点位{name}");
#endif
            return MotionControlResource.IOOutputResource[name].Status;
        }
        /// <summary>
        /// 获取输出点位状态,静态函数
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static bool GetOutputStsEx(string name)
        {
#if DEBUG
            if (!MotionControlResource.IOOutputResource.ContainsKey(name))
                throw new Exception($"获取输出点位状态时,IOOutputResource中不包含输出点位{name}");
#endif
            return MotionControlResource.IOOutputResource[name].Status;
        }
        /// <summary>
        /// 实时获取输出点位状态
        /// </summary>
        /// <param name="motionCard"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static bool? RealTimeGetOutputStsEx(this IMotionCardService motionCard, string name)
        {
#if DEBUG
            if (!MotionControlResource.IOOutputResource.ContainsKey(name))
                throw new Exception($"获取输出点位状态时,IOOutputResource中不包含输出点位{name}");
#endif
            return motionCard.GetEcatDoBit((short)(MotionControlResource.IOOutputResource[name].ProgramAddressGroup * 8 + MotionControlResource.IOOutputResource[name].ProgramAddressPosition));
        }
        /// <summary>
        /// 实时检测轴运动是否到位,不从框架全局表中读取,避免全局表更新不及时造成误判
        /// </summary>
        /// <param name="motionCardService"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static bool RealTimeCheckAxisArrivedEx(this IMotionCardService motionCardService, string name)
        {
#if DEBUG
            if (!MotionControlResource.AxisResource.ContainsKey(name))
                throw new Exception($"检查轴是否到达时,AxisStsResource中不包含轴{name}");
#endif
            //var status = motionCardService.GetAxSts((short)MotionControlResource.AxisResource[name].AxisIdOnCard, 1);
            ////轴到位状态需要轴不忙且轴到位(PTP启动后立即获取轴状态,此时轴状态显示到位,轴忙及到位一起判断)
            ////return (MotionControlResource.AxisStsResource[name].AxSts & 0x4) != 0x4 && (MotionControlResource.AxisStsResource[name].AxSts & 0x8) == 0x8;
            //if (status == null) return false;
            //return (status[0] & 0x0C) == 0x8;
            return motionCardService.CheckAxisArrived((short)MotionControlResource.AxisResource[name].AxisIdOnCard, (short)(MotionControlResource.AxisResource[name].TargetLocationGap * MotionControlResource.AxisResource[name].Proportion));
        }
        /// <summary>
        /// 轴回原点扩展函数
        /// </summary>
        /// <param name="motionCardService"></param>
        /// <param name="axName"></param>
        /// <returns></returns>
        public static bool StartHomingEx(this IMotionCardService motionCardService, string axName) =>
            motionCardService.StartHoming((short)MotionControlResource.AxisResource[axName].AxisIdOnCard,
            (short)MotionControlResource.AxisResource[axName].GetHomeMethod(),
            (int)(MotionControlResource.AxisResource[axName].GetHomeOffset() * MotionControlResource.AxisResource[axName].Proportion),
            (uint)(MotionControlResource.AxisResource[axName].GetHomeHighVel() * MotionControlResource.AxisResource[axName].Proportion),
            (uint)(MotionControlResource.AxisResource[axName].GetHomeLowVel() * MotionControlResource.AxisResource[axName].Proportion),
            (uint)(MotionControlResource.AxisResource[axName].GetHomeAcc() * MotionControlResource.AxisResource[axName].Proportion),
            (uint)MotionControlResource.AxisResource[axName].GetHomeTimeout(),
            0);
        /// <summary>
        /// 轴使能扩展函数
        /// </summary>
        /// <param name="motionCardService"></param>
        /// <param name="axName"></param>
        /// <param name="enable"></param>
        /// <returns></returns>
        public static bool AxisServoEx(this IMotionCardService motionCardService, string axName, bool enable) =>
            motionCardService.AxisServo((short)MotionControlResource.AxisResource[axName].AxisIdOnCard, enable);
        /// <summary>
        /// 轴清除轴错误扩展函数
        /// </summary>
        /// <param name="motionCardService"></param>
        /// <param name="axName"></param>
        /// <returns></returns>
        public static bool ClearAxStsEx(this IMotionCardService motionCardService, string axName) =>
            motionCardService.ClearAxSts((short)MotionControlResource.AxisResource[axName].AxisIdOnCard);
        /// <summary>
        /// 停止回原扩展函数
        /// </summary>
        /// <param name="motionCardService"></param>
        /// <param name="axName"></param>
        public static void StopHomingEx(this IMotionCardService motionCardService, string axName) =>
            motionCardService.StopHoming((short)MotionControlResource.AxisResource[axName].AxisIdOnCard);
        /// <summary>
        /// 获取轴回原状态扩展函数
        /// </summary>
        /// <param name="motionCardService"></param>
        /// <param name="axName"></param>
        /// <returns></returns>
        public static bool GetHomingStsEx(this IMotionCardService motionCardService, string axName) =>
            motionCardService.GetHomingSts((short)MotionControlResource.AxisResource[axName].AxisIdOnCard);
        /// <summary>
        /// 轴结束回原扩展函数
        /// </summary>
        /// <param name="motionCardService"></param>
        /// <param name="axName"></param>
        public static void FinishHomingEx(this IMotionCardService motionCardService, string axName) =>
            motionCardService.FinishHoming((short)MotionControlResource.AxisResource[axName].AxisIdOnCard);
        /// <summary>
        /// 获取轴复位超时时间
        /// </summary>
        /// <param name="axName"></param>
        /// <returns></returns>
        public static int GetAxTimeoutEx(this IMotionCardService motionCard, string axName) => MotionControlResource.AxisResource[axName].GetHomeTimeout() + 1000;
        /// <summary>
        /// 获取轴复位超时时间,共类中直接初始化属性时使用
        /// </summary>
        /// <param name="axName"></param>
        /// <returns></returns>
        public static int GetAxTimeoutEx(string axName) => MotionControlResource.AxisResource[axName].GetHomeTimeout() + 1000;
        /// <summary>
        /// 移动到指定点位,安全轴不参与运行!
        /// </summary>
        /// <param name="motionCardService"></param>
        /// <param name="pointName"></param>
        /// <returns></returns>
        public static bool MoveToPointEx(this IMotionCardService motionCardService, string pointName)
        {
#if DEBUG
            if (!GlobalValues.MCPointDicCollection.ContainsKey(pointName))
                throw new Exception($"移动到点位时,不存在运动点位{pointName}");
            if (GlobalValues.MCPointDicCollection[pointName].MovementPointPositions.Count == 0)
                throw new Exception($"移动到点位时,点位{pointName}中没有设定轴");
#endif
            bool ret = true;
            foreach (var item in GlobalValues.MCPointDicCollection[pointName].MovementPointPositions)
            {
                switch (item.GetMovementPointType())
                {
                    case Models.MotionControlModels.MovementType.Abs:
                        //绝对运动位置由绝对位置+Offset组成
                        ret &= motionCardService.StartMoveAbs((short)item.AxisInfo.AxisIdOnCard,
                                                              0,
                                                              0,
                                                              item.Vel * item.AxisInfo.Proportion * GlobalValues.GlobalVelPercentage / 100,
                                                              item.Acc * item.AxisInfo.Proportion,
                                                              item.Dec * item.AxisInfo.Proportion,
                                                              (item.AbsValue + item.Offset) * item.AxisInfo.Proportion);
                        break;
                    case Models.MotionControlModels.MovementType.Rel:
                        //相对运动位置由相对位置+Offset组成
                        ret &= motionCardService.StartMoveRel((short)item.AxisInfo.AxisIdOnCard,
                                                              0,
                                                              0,
                                                              item.Vel * item.AxisInfo.Proportion * GlobalValues.GlobalVelPercentage / 100,
                                                              item.Acc * item.AxisInfo.Proportion,
                                                              item.Dec * item.AxisInfo.Proportion,
                                                              (item.RelValue + item.Offset) * item.AxisInfo.Proportion);
                        break;
                    case Models.MotionControlModels.MovementType.Jog:
                        ret &= motionCardService.JogMoveStart((short)item.AxisInfo.AxisIdOnCard,
                                                              item.Vel * item.AxisInfo.Proportion * GlobalValues.GlobalVelPercentage / 100,
                                                              item.JogDirection,
                                                              item.Acc * item.AxisInfo.Proportion,
                                                              item.Dec * item.AxisInfo.Proportion);
                        break;
                    default:
                        throw new Exception("MoveToPointEx中不应该有Null出现");
                }
            }
            return ret;
        }
        /// <summary>
        /// 运动点位中只有绝对位移时可使用此函数进行运动,安全轴不参与运行!
        /// </summary>
        /// <param name="motionCardService"></param>
        /// <param name="pointName"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static bool AbsMoveToPointEx(IMotionCardService motionCardService, string pointName)
        {
#if DEBUG
            if (!GlobalValues.MCPointDicCollection.ContainsKey(pointName))
                throw new Exception($"移动到点位时,不存在运动点位{pointName}");
            if (GlobalValues.MCPointDicCollection[pointName].MovementPointPositions.Count == 0)
                throw new Exception($"移动到点位时,点位{pointName}中没有设定轴");
#endif
            bool ret = true;
            foreach (var item in GlobalValues.MCPointDicCollection[pointName].MovementPointPositions)
            {
                switch (item.GetMovementPointType())
                {
                    case Models.MotionControlModels.MovementType.Abs:
                        //绝对运动位置由绝对位置+Offset组成
                        ret &= motionCardService.StartMoveAbs((short)item.AxisInfo.AxisIdOnCard,
                                                              0,
                                                              0,
                                                              item.Vel * item.AxisInfo.Proportion * GlobalValues.GlobalVelPercentage / 100,
                                                              item.Acc * item.AxisInfo.Proportion,
                                                              item.Dec * item.AxisInfo.Proportion,
                                                              (item.AbsValue + item.Offset) * item.AxisInfo.Proportion);
                        break;
                    case Models.MotionControlModels.MovementType.Rel:
                        throw new Exception("AbsMoveToPointEx中不应该有相对点位出现");
                    case Models.MotionControlModels.MovementType.Jog:
                        throw new Exception("RelMoveToPointEx中不应该有Jog出现");
                    default:
                        throw new Exception("AbsMoveToPointEx中不应该有Null出现");
                }
            }
            return ret;
        }
        /// <summary>
        /// 运动点位中只有相对位移时可使用此函数进行运动,安全轴不参与运行!
        /// </summary>
        /// <param name="motionCardService"></param>
        /// <param name="pointName"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static bool RelMoveToPointEx(IMotionCardService motionCardService, string pointName)
        {
#if DEBUG
            if (!GlobalValues.MCPointDicCollection.ContainsKey(pointName))
                throw new Exception($"移动到点位时,不存在运动点位{pointName}");
            if (GlobalValues.MCPointDicCollection[pointName].MovementPointPositions.Count == 0)
                throw new Exception($"移动到点位时,点位{pointName}中没有设定轴");
#endif
            bool ret = true;
            foreach (var item in GlobalValues.MCPointDicCollection[pointName].MovementPointPositions)
            {
                switch (item.GetMovementPointType())
                {
                    case Models.MotionControlModels.MovementType.Abs:
                        throw new Exception("RelMoveToPointEx中不应该有Abs出现");
                    case Models.MotionControlModels.MovementType.Rel:
                        ret &= motionCardService.StartMoveRel((short)item.AxisInfo.AxisIdOnCard,
                                                              0,
                                                              0,
                                                              item.Vel * item.AxisInfo.Proportion * GlobalValues.GlobalVelPercentage / 100,
                                                              item.Acc * item.AxisInfo.Proportion,
                                                              item.Dec * item.AxisInfo.Proportion,
                                                              (item.RelValue + item.Offset) * item.AxisInfo.Proportion);
                        break;
                    case Models.MotionControlModels.MovementType.Jog:
                        throw new Exception("RelMoveToPointEx中不应该有Jog出现");
                    default:
                        throw new Exception("AbsMoveToPointEx中不应该有Null出现");
                }
            }
            return ret;
        }
        /// <summary>
        /// 运动点位中只有Jog时可使用此函数进行运动,安全轴不参与运行!
        /// </summary>
        /// <param name="motionCardService"></param>
        /// <param name="pointName"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static bool JogMoveToPointEx(IMotionCardService motionCardService, string pointName)
        {
#if DEBUG
            if (!GlobalValues.MCPointDicCollection.ContainsKey(pointName))
                throw new Exception($"移动到点位时,不存在运动点位{pointName}");
            if (GlobalValues.MCPointDicCollection[pointName].MovementPointPositions.Count == 0)
                throw new Exception($"移动到点位时,点位{pointName}中没有设定轴");
#endif
            bool ret = true;
            foreach (var item in GlobalValues.MCPointDicCollection[pointName].MovementPointPositions)
            {
                switch (item.GetMovementPointType())
                {
                    case Models.MotionControlModels.MovementType.Abs:
                        throw new Exception("JogMoveToPointEx中不应该有Abs出现");
                    case Models.MotionControlModels.MovementType.Rel:
                        throw new Exception("JogMoveToPointEx中不应该有Rel出现");
                    case Models.MotionControlModels.MovementType.Jog:
                        ret &= motionCardService.JogMoveStart((short)item.AxisInfo.AxisIdOnCard,
                                                              item.Vel * item.AxisInfo.Proportion * GlobalValues.GlobalVelPercentage / 100,
                                                              item.JogDirection,
                                                              item.Acc * item.AxisInfo.Proportion,
                                                              item.Dec * item.AxisInfo.Proportion);
                        break;
                    default:
                        throw new Exception("JogMoveToPointEx中不应该有Null出现");
                }
            }
            return ret;
        }
        /// <summary>
        /// 用于组装时纠偏位移,位移位绝对移动
        /// 轴排序为轴Number(若Number相同,按照Id)从小到大.对应补偿应依照此顺序
        /// 调用前最好对Offset数量进行判断,否则容易产生异常
        /// </summary>
        /// <param name="motionCardService"></param>
        /// <param name="pointName"></param>
        /// <param name="ccdOffset"></param>
        /// <returns></returns>
        public static bool MoveToPointWithCCDOffsetEx(this IMotionCardService motionCardService, string pointName, List<double> ccdOffset)
        {
#if DEBUG
            if (!GlobalValues.MCPointDicCollection.ContainsKey(pointName))
                throw new Exception($"CCDOffeset不存在运动点位{pointName}");
            if (GlobalValues.MCPointDicCollection[pointName].MovementPointPositions.Count == 0)
                throw new Exception($"移动到点位时,点位{pointName}中没有设定轴");
#endif
            bool ret = true;
            int i = 0;
            foreach (var item in GlobalValues.MCPointDicCollection[pointName].MovementPointPositions)
            {
                switch (item.GetMovementPointType())
                {
                    case Models.MotionControlModels.MovementType.Abs:
                        //绝对运动位置由绝对位置+Offset+CCD Offset组成
                        ret &= motionCardService.StartMoveAbs((short)item.AxisInfo.AxisIdOnCard,
                                                              0,
                                                              0,
                                                              item.Vel * item.AxisInfo.Proportion * GlobalValues.GlobalVelPercentage / 100,
                                                              item.Acc * item.AxisInfo.Proportion,
                                                              item.Dec * item.AxisInfo.Proportion,
                                                              (item.AbsValue + item.Offset + ccdOffset[i]) * item.AxisInfo.Proportion);
                        break;
                    case Models.MotionControlModels.MovementType.Rel:
                        ret &= motionCardService.StartMoveRel((short)item.AxisInfo.AxisIdOnCard,
                                                              0,
                                                              0,
                                                              item.Vel * item.AxisInfo.Proportion * GlobalValues.GlobalVelPercentage / 100,
                                                              item.Acc * item.AxisInfo.Proportion,
                                                              item.Dec * item.AxisInfo.Proportion,
                                                              (item.RelValue + item.Offset + ccdOffset[i]) * item.AxisInfo.Proportion);
                        break;
                    case Models.MotionControlModels.MovementType.Jog:
                        throw new Exception("不支持Jog运动中进行Offset补偿");
                    default:
                        throw new Exception("MoveToPointWithCCDOffsetEx中不应该有Null出现");
                }
                i++;
            }
            return ret;
        }
        /// <summary>
        /// 停止运动点位参与运动的轴
        /// </summary>
        /// <param name="motionCardService"></param>
        /// <param name="pointName"></param>
        /// <returns></returns>
        public static bool StopMoveToPointEx(this IMotionCardService motionCardService, string pointName)
        {
#if DEBUG
            if (!GlobalValues.MCPointDicCollection.ContainsKey(pointName))
                throw new Exception($"停止移动到点位时,不存在运动点位{pointName}");
            if (GlobalValues.MCPointDicCollection[pointName].MovementPointPositions.Count == 0)
                throw new Exception($"移动到点位时,点位{pointName}中没有设定轴");
#endif
            bool ret = true;
            foreach (var item in GlobalValues.MCPointDicCollection[pointName].MovementPointPositions)
            {
                ret &= motionCardService.StopMove((short)item.AxisInfo.AxisIdOnCard, 1);
            }
            return ret;
        }
        /// <summary>
        /// 实时检测点位运动是否到位,不从框架全局表中读取,避免全局表更新不及时造成误判
        /// </summary>
        /// <param name="motionCardService"></param>
        /// <param name="pointName"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static bool RealTimeCheckMoveToPointArrivedEx(this IMotionCardService motionCardService, string pointName)
        {
#if DEBUG
            if (!GlobalValues.MCPointDicCollection.ContainsKey(pointName))
                throw new Exception($"检查是否到位时,不存在运动点位{pointName}");
            if (GlobalValues.MCPointDicCollection[pointName].MovementPointPositions.Count == 0)
                throw new Exception($"移动到点位时,点位{pointName}中没有设定轴");
#endif
            bool ret = true;
            foreach (var item in GlobalValues.MCPointDicCollection[pointName].MovementPointPositions)
            {
                switch (item.GetMovementPointType())
                {
                    case Models.MotionControlModels.MovementType.Abs:
                    case Models.MotionControlModels.MovementType.Rel:
                        ret &= motionCardService.CheckAxisArrived((short)item.AxisInfo.AxisIdOnCard, (short)(item.AxisInfo.TargetLocationGap * item.AxisInfo.Proportion));
                        break;
                    case Models.MotionControlModels.MovementType.Jog:
                        if (MotionControlResource.IOInputResource[item.JogIOInputInfo.Name].Status == (item.JogArrivedCondition == Models.MotionControlModels.JogArrivedType.Input ? true : false))
                        {
                            motionCardService.JogMoveStop((short)item.AxisInfo.AxisIdOnCard);
                            ret &= true;
                        }
                        else
                            ret &= false;
                        break;
                    default:
                        break;
                }
            }
            return ret;
        }
        /// <summary>
        /// 获取运动参数
        /// </summary>
        /// <param name="parmeterName"></param>
        /// <returns></returns>
        public static object GetMCParmeterEx(this IMotionCardService motionCardService, string parmeterName)
        {
#if DEBUG
            if (!GlobalValues.MCParmeterDicCollection.ContainsKey(parmeterName))
                throw new Exception($"运动控制参数中不存在{parmeterName}");
#endif
            return GlobalValues.MCParmeterDicCollection[parmeterName].Data;
        }
        /// <summary>
        /// 设置参数
        /// </summary>
        /// <param name="motionCardService"></param>
        /// <param name="dataBaseService"></param>
        /// <param name="parmeterName"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static bool SetMCParmeterEx(this IMotionCardService motionCardService, IDataBaseService dataBaseService, string parmeterName, string data)
        {
#if DEBUG
            if (!GlobalValues.MCParmeterDicCollection.ContainsKey(parmeterName))
                throw new Exception($"运动控制参数中不存在{parmeterName}");
#endif
            var par = dataBaseService.Db.Queryable<ParmeterInfoEntity>().Where(x => x.Name == parmeterName && x.ProductInfo.Select == true).First();
            if (par != null)
            {
                par.Data = data;
                var rows = dataBaseService.Db.Updateable(par).ExecuteCommand();
                if (rows > 0)
                    GlobalValues.MCParmeterDicCollection[parmeterName].Data = data;
                return rows > 0;
            }
            return false;
        }
        /// <summary>
        /// 获取运动参数,供类中直接初始化属性时使用
        /// </summary>
        /// <param name="parmeterName"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static object GetMCParmeterEx(string parmeterName)
        {
#if DEBUG
            if (!GlobalValues.MCParmeterDicCollection.ContainsKey(parmeterName))
                throw new Exception($"运动控制参数中不存在{parmeterName}");
#endif
            return GlobalValues.MCParmeterDicCollection[parmeterName].Data;
        }
        /// <summary>
        /// 设置参数
        /// </summary>
        /// <param name="dataBaseService"></param>
        /// <param name="parmeterName"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static bool SetMCParmeterEx(IDataBaseService dataBaseService, string parmeterName, string data)
        {
#if DEBUG
            if (!GlobalValues.MCParmeterDicCollection.ContainsKey(parmeterName))
                throw new Exception($"运动控制参数中不存在{parmeterName}");
#endif
            var par = dataBaseService.Db.Queryable<ParmeterInfoEntity>().Where(x => x.Name == parmeterName && x.ProductInfo.Select == true).First();
            if (par != null)
            {
                par.Data = data;
                var rows = dataBaseService.Db.Updateable(par).ExecuteCommand();
                if (rows > 0)
                    GlobalValues.MCParmeterDicCollection[parmeterName].Data = data;
                return rows > 0;
            }
            return false;
        }
        /// <summary>
        /// 获取运动参数泛型版本
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parmeterName"></param>
        /// <returns></returns>
        public static T GetMCParmeterEx<T>(this IMotionCardService motionCardService, string parmeterName)
        {
#if DEBUG
            if (!GlobalValues.MCParmeterDicCollection.ContainsKey(parmeterName))
                throw new Exception($"运动控制参数中不存在{parmeterName}");
#endif
            return (T)Convert.ChangeType(GlobalValues.MCParmeterDicCollection[parmeterName].Data, typeof(T));
        }
        /// <summary>
        /// 设置参数
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="motionCardService"></param>
        /// <param name="dataBaseService"></param>
        /// <param name="parmeterName"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static bool SetMCParmeterEx<T>(this IMotionCardService motionCardService, IDataBaseService dataBaseService, string parmeterName, T data)
        {
#if DEBUG
            if (!GlobalValues.MCParmeterDicCollection.ContainsKey(parmeterName))
                throw new Exception($"运动控制参数中不存在{parmeterName}");
#endif
            var par = dataBaseService.Db.Queryable<ParmeterInfoEntity>().Where(x => x.Name == parmeterName && x.ProductInfo.Select == true).First();
            if (par != null)
            {
                par.Data = data.ToString();
                var rows = dataBaseService.Db.Updateable(par).ExecuteCommand();
                if (rows > 0)
                    GlobalValues.MCParmeterDicCollection[parmeterName].Data = data.ToString();
                return rows > 0;
            }
            return false;
        }
        /// <summary>
        /// 获取运动参数泛型版本,供类中直接初始化属性时使用
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parmeterName"></param>
        /// <returns></returns>
        public static T GetMCParmeterEx<T>(string parmeterName)
        {
#if DEBUG
            if (!GlobalValues.MCParmeterDicCollection.ContainsKey(parmeterName))
                throw new Exception($"运动控制参数中不存在{parmeterName}");
#endif
            return (T)Convert.ChangeType(GlobalValues.MCParmeterDicCollection[parmeterName].Data, typeof(T));
        }
        /// <summary>
        /// 设置参数
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dataBaseService"></param>
        /// <param name="parmeterName"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static bool SetMCParmeterEx<T>(IDataBaseService dataBaseService, string parmeterName, T data)
        {
#if DEBUG
            if (!GlobalValues.MCParmeterDicCollection.ContainsKey(parmeterName))
                throw new Exception($"运动控制参数中不存在{parmeterName}");
#endif
            var par = dataBaseService.Db.Queryable<ParmeterInfoEntity>().Where(x => x.Name == parmeterName && x.ProductInfo.Select == true).First();
            if (par != null)
            {
                par.Data = data.ToString();
                var rows = dataBaseService.Db.Updateable(par).ExecuteCommand();
                if (rows > 0)
                    GlobalValues.MCParmeterDicCollection[parmeterName].Data = data.ToString();
                return rows > 0;
            }
            return false;
        }
        /// <summary>
        /// 获取所有轴状态是否报警
        /// </summary>
        /// <returns></returns>
        public static bool GetAxesErrorEx(this IMotionCardService motionCardService, ILogService logService)
        {
            bool result = false;
            foreach (var axis in MotionControlResource.AxisResource)
            {
                var sts = (axis.Value.Status & 0x1) == 0x1;
                if (sts == true)
                {
                    short? errorCode = motionCardService.GetAxErrorCode((short)axis.Value.AxisIdOnCard);
                    if (errorCode.HasValue)
                    {
                        logService.WriteLog(LogTypes.DB.ToString(), $"监视到轴{axis.Value.Name}错误,错误代码为0x{(short)errorCode:x}", MessageDegree.FATAL);
                    }
                }
                result |= sts;
            }
            return result;
        }
        /// <summary>
        /// Jog移动启动
        /// </summary>
        /// <param name="motionCardService"></param>
        /// <param name="axisName"></param>
        /// <param name="tgVel"></param>
        /// <param name="dir"></param>
        /// <returns></returns>
        public static bool JogMoveStartEx(this IMotionCardService motionCardService, string axisName, double tgVel, double acc, double dec, Direction dir)
        {
#if DEBUG
            if (!MotionControlResource.AxisResource.ContainsKey(axisName))
                throw new Exception($"Jog移动停止时,轴字典中未包含{axisName}");
#endif
            return motionCardService.JogMoveStart((short)MotionControlResource.AxisResource[axisName].AxisIdOnCard,
                                                  tgVel * MotionControlResource.AxisResource[axisName].Proportion * GlobalValues.GlobalVelPercentage / 100,
                                                  dir,
                                                  acc * MotionControlResource.AxisResource[axisName].Proportion,
                                                  dec * MotionControlResource.AxisResource[axisName].Proportion);
        }
        /// <summary>
        /// Jog移动停止
        /// </summary>
        /// <param name="motionCardService"></param>
        /// <param name="axName"></param>
        /// <returns></returns>
        public static bool JogMoveStopEx(this IMotionCardService motionCardService, string axName)
        {
#if DEBUG
            if (!MotionControlResource.AxisResource.ContainsKey(axName))
                throw new Exception($"Jog移动停止时,轴字典中未包含{axName}");
#endif
            return motionCardService.JogMoveStop((short)MotionControlResource.AxisResource[axName].AxisIdOnCard);
        }
        /// <summary>
        /// 暂停点位移动
        /// </summary>
        /// <param name="motionCardService"></param>
        /// <param name="pointName"></param>
        /// <returns></returns>
        public static bool PauseMoveToPointEx(this IMotionCardService motionCardService, string pointName)
        {
#if DEBUG
            if (!GlobalValues.MCPointDicCollection.ContainsKey(pointName))
                throw new Exception($"暂停移动时,不存在运动点位{pointName}");
            if (GlobalValues.MCPointDicCollection[pointName].MovementPointPositions.Count == 0)
                throw new Exception($"移动到点位时,点位{pointName}中没有设定轴");
#endif
            bool ret = true;
            foreach (var item in GlobalValues.MCPointDicCollection[pointName].MovementPointPositions)
            {
                ret &= motionCardService.PauseMove((short)item.AxisInfo.AxisIdOnCard);
            }
            return ret;
        }
        /// <summary>
        /// 恢复点位移动
        /// </summary>
        /// <param name="motionCardService"></param>
        /// <param name="pointName"></param>
        /// <returns></returns>
        public static bool ResumeMoveToPointEx(this IMotionCardService motionCardService, string pointName)
        {
#if DEBUG
            if (!GlobalValues.MCPointDicCollection.ContainsKey(pointName))
                throw new Exception($"恢复移动时,不存在运动点位{pointName}");
            if (GlobalValues.MCPointDicCollection[pointName].MovementPointPositions.Count == 0)
                throw new Exception($"移动到点位时,点位{pointName}中没有设定轴");
#endif
            bool ret = true;
            foreach (var item in GlobalValues.MCPointDicCollection[pointName].MovementPointPositions)
            {
                ret &= motionCardService.ResumeMove((short)item.AxisInfo.AxisIdOnCard);
            }
            return ret;
        }
        /// <summary>
        /// 气缸控制
        /// </summary>
        /// <param name="motionCardService"></param>
        /// <param name="cylinderName"></param>
        /// <param name="value">True:气缸伸出,False:气缸缩回</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static bool CylinderControlEx(this IMotionCardService motionCardService, string cylinderName, bool value)
        {
#if DEBUG
            if (!GlobalValues.CylinderDicCollection.ContainsKey(cylinderName))
            {
                throw new Exception($"不存在气缸{cylinderName}");
            }
#endif
            bool ret = true;
            switch (GlobalValues.CylinderDicCollection[cylinderName].ValveType)
            {
                case Models.CylinderModels.ValveType.SingleHeader:
                    ret &= motionCardService.SetOuputStsEx(GlobalValues.CylinderDicCollection[cylinderName].SingleValveOutputInfo.Name, value);
                    break;
                case Models.CylinderModels.ValveType.DualHeader:
                    //伸出
                    if (value)
                    {
                        ret &= motionCardService.SetOuputStsEx(GlobalValues.CylinderDicCollection[cylinderName].DualValveMovingOutputInfo.Name, false);
                        ret &= motionCardService.SetOuputStsEx(GlobalValues.CylinderDicCollection[cylinderName].DualValveOriginOutputInfo.Name, true);
                    }
                    //缩回
                    if (!value)
                    {
                        ret &= motionCardService.SetOuputStsEx(GlobalValues.CylinderDicCollection[cylinderName].DualValveMovingOutputInfo.Name, true);
                        ret &= motionCardService.SetOuputStsEx(GlobalValues.CylinderDicCollection[cylinderName].DualValveOriginOutputInfo.Name, false);
                    }
                    break;
                default:
                    break;
            }
            return ret;
        }
        /// <summary>
        /// 流程中气缸到位检查,需要响应流程取消
        /// </summary>
        /// <param name="motionCardService"></param>
        /// <param name="actuationManager">用于取消指令,防止阻塞</param>
        /// <param name="cylinderName"></param>
        /// <param name="value">True:气缸伸出,False:气缸缩回</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static bool CylinderArrivedCheckEx(this IMotionCardService motionCardService, ActuationManagerAbs actuationManager, string cylinderName, bool value)
        {
#if DEBUG
            if (!GlobalValues.CylinderDicCollection.ContainsKey(cylinderName))
            {
                throw new Exception($"不存在气缸{cylinderName}");
            }
#endif
            switch (GlobalValues.CylinderDicCollection[cylinderName].SensorType)
            {
                case Models.CylinderModels.SensorType.None:
                    //气缸无传感器,使用DelayTime.DelayTime需要小于指令超时时间
                    Task.Delay(Convert.ToInt16(GlobalValues.CylinderDicCollection[cylinderName].DelayTime), actuationManager.GetCancellationTokenSource().Token).GetAwaiter().GetResult();
                    return true;
                case Models.CylinderModels.SensorType.SingleOrigin:
                    //气缸伸出,只有原点传感器则使用延时时间
                    if (value)
                    {
                        Task.Delay(Convert.ToInt16(GlobalValues.CylinderDicCollection[cylinderName].DelayTime), actuationManager.GetCancellationTokenSource().Token).GetAwaiter().GetResult();
                        return true;
                    }
                    //气缸缩回,未屏蔽原点传感器时,判断原点传感器是否亮
                    if (!value && !GlobalValues.CylinderDicCollection[cylinderName].ShiedSensorOriginInput)
                    {
                        return motionCardService.GetInputStsEx(GlobalValues.CylinderDicCollection[cylinderName].SensorOriginInputInfo.Name) == true;
                    }
                    //气缸缩回,屏蔽原点传感器时,使用屏蔽原点传感器延时
                    if (!value && GlobalValues.CylinderDicCollection[cylinderName].ShiedSensorOriginInput)
                    {
                        Task.Delay(Convert.ToInt16(GlobalValues.CylinderDicCollection[cylinderName].ShiedSensorOriginInputDelayTime), actuationManager.GetCancellationTokenSource().Token).GetAwaiter().GetResult();
                        return true;
                    }
                    break;
                case Models.CylinderModels.SensorType.SingleMoving:
                    //气缸伸出,未屏蔽动点传感器时,判断动点传感器是否亮
                    if (value && !GlobalValues.CylinderDicCollection[cylinderName].ShiedSensorMovingInput)
                    {
                        return motionCardService.GetInputStsEx(GlobalValues.CylinderDicCollection[cylinderName].SensorMovingInputInfo.Name) == true;
                    }
                    //气缸伸出,屏蔽动点传感器时,使用屏蔽动点传感器延时时间
                    if (value && GlobalValues.CylinderDicCollection[cylinderName].ShiedSensorMovingInput)
                    {
                        Task.Delay(Convert.ToInt16(GlobalValues.CylinderDicCollection[cylinderName].ShiedSensorMovingInputDelayTime), actuationManager.GetCancellationTokenSource().Token).GetAwaiter().GetResult();
                        return true;
                    }
                    //气缸缩回,只有动点传感器则使用延时时间
                    if (!value)
                    {
                        Task.Delay(Convert.ToInt16(GlobalValues.CylinderDicCollection[cylinderName].DelayTime), actuationManager.GetCancellationTokenSource().Token).GetAwaiter().GetResult();
                        return true;
                    }
                    break;
                case Models.CylinderModels.SensorType.Dual:
                    //气缸伸出,未屏蔽动点传感器时,判断动点传感器是否亮
                    if (value && !GlobalValues.CylinderDicCollection[cylinderName].ShiedSensorMovingInput)
                    {
                        return motionCardService.GetInputStsEx(GlobalValues.CylinderDicCollection[cylinderName].SensorMovingInputInfo.Name) == true;
                    }
                    //气缸伸出,屏蔽动点传感器时,使用屏蔽动点传感器延时时间
                    if (value && GlobalValues.CylinderDicCollection[cylinderName].ShiedSensorMovingInput)
                    {
                        Task.Delay(Convert.ToInt16(GlobalValues.CylinderDicCollection[cylinderName].ShiedSensorMovingInputDelayTime), actuationManager.GetCancellationTokenSource().Token).GetAwaiter().GetResult();
                        return true;
                    }
                    //气缸缩回,未屏蔽原点传感器时,判断原点传感器是否亮
                    if (!value && !GlobalValues.CylinderDicCollection[cylinderName].ShiedSensorOriginInput)
                    {
                        return motionCardService.GetInputStsEx(GlobalValues.CylinderDicCollection[cylinderName].SensorOriginInputInfo.Name) == true;
                    }
                    //气缸缩回,屏蔽原点传感器时,使用屏蔽原点传感器延时时间
                    if (!value && GlobalValues.CylinderDicCollection[cylinderName].ShiedSensorOriginInput)
                    {
                        Task.Delay(Convert.ToInt16(GlobalValues.CylinderDicCollection[cylinderName].ShiedSensorOriginInputDelayTime), actuationManager.GetCancellationTokenSource().Token).GetAwaiter().GetResult();
                        return true;
                    }
                    break;
                default:
                    throw new Exception("无此气缸传感器类型");
            }
            return false;
        }
        /// <summary>
        /// 检查气缸是否到位,自定义取消时使用
        /// </summary>
        /// <param name="motionCardService"></param>
        /// <param name="cancellationTokenSource"></param>
        /// <param name="cylinderName"></param>
        /// <param name="value"></param>
        /// <returns>True:气缸伸出,False:气缸缩回</returns>
        /// <exception cref="Exception"></exception>
        public static bool CylinderArrviedCheckWithCancellationEx(this IMotionCardService motionCardService, CancellationTokenSource cancellationTokenSource, string cylinderName, bool value)
        {
#if DEBUG
            if (!GlobalValues.CylinderDicCollection.ContainsKey(cylinderName))
            {
                throw new Exception($"不存在气缸{cylinderName}");
            }
#endif
            switch (GlobalValues.CylinderDicCollection[cylinderName].SensorType)
            {
                case Models.CylinderModels.SensorType.None:
                    //气缸无传感器,使用DelayTime.DelayTime需要小于指令超时时间
                    Task.Delay(Convert.ToInt16(GlobalValues.CylinderDicCollection[cylinderName].DelayTime), cancellationTokenSource.Token).GetAwaiter().GetResult();
                    return true;
                case Models.CylinderModels.SensorType.SingleOrigin:
                    //气缸伸出,只有原点传感器则使用延时时间
                    if (value)
                    {
                        Task.Delay(Convert.ToInt16(GlobalValues.CylinderDicCollection[cylinderName].DelayTime), cancellationTokenSource.Token).GetAwaiter().GetResult();
                        return true;
                    }
                    //气缸缩回,未屏蔽原点传感器时,判断原点传感器是否亮
                    if (!value && !GlobalValues.CylinderDicCollection[cylinderName].ShiedSensorOriginInput)
                    {
                        return motionCardService.GetInputStsEx(GlobalValues.CylinderDicCollection[cylinderName].SensorOriginInputInfo.Name) == true;
                    }
                    //气缸缩回,屏蔽原点传感器时,使用屏蔽原点传感器延时
                    if (!value && GlobalValues.CylinderDicCollection[cylinderName].ShiedSensorOriginInput)
                    {
                        Task.Delay(Convert.ToInt16(GlobalValues.CylinderDicCollection[cylinderName].ShiedSensorOriginInputDelayTime), cancellationTokenSource.Token).GetAwaiter().GetResult();
                        return true;
                    }
                    break;
                case Models.CylinderModels.SensorType.SingleMoving:
                    //气缸伸出,未屏蔽动点传感器时,判断动点传感器是否亮
                    if (value && !GlobalValues.CylinderDicCollection[cylinderName].ShiedSensorMovingInput)
                    {
                        return motionCardService.GetInputStsEx(GlobalValues.CylinderDicCollection[cylinderName].SensorMovingInputInfo.Name) == true;
                    }
                    //气缸伸出,屏蔽动点传感器时,使用屏蔽动点传感器延时时间
                    if (value && GlobalValues.CylinderDicCollection[cylinderName].ShiedSensorMovingInput)
                    {
                        Task.Delay(Convert.ToInt16(GlobalValues.CylinderDicCollection[cylinderName].ShiedSensorMovingInputDelayTime), cancellationTokenSource.Token).GetAwaiter().GetResult();
                        return true;
                    }
                    //气缸缩回,只有动点传感器则使用延时时间
                    if (!value)
                    {
                        Task.Delay(Convert.ToInt16(GlobalValues.CylinderDicCollection[cylinderName].DelayTime), cancellationTokenSource.Token).GetAwaiter().GetResult();
                        return true;
                    }
                    break;
                case Models.CylinderModels.SensorType.Dual:
                    //气缸伸出,未屏蔽动点传感器时,判断动点传感器是否亮
                    if (value && !GlobalValues.CylinderDicCollection[cylinderName].ShiedSensorMovingInput)
                    {
                        return motionCardService.GetInputStsEx(GlobalValues.CylinderDicCollection[cylinderName].SensorMovingInputInfo.Name) == true;
                    }
                    //气缸伸出,屏蔽动点传感器时,使用屏蔽动点传感器延时时间
                    if (value && GlobalValues.CylinderDicCollection[cylinderName].ShiedSensorMovingInput)
                    {
                        Task.Delay(Convert.ToInt16(GlobalValues.CylinderDicCollection[cylinderName].ShiedSensorMovingInputDelayTime), cancellationTokenSource.Token).GetAwaiter().GetResult();
                        return true;
                    }
                    //气缸缩回,未屏蔽原点传感器时,判断原点传感器是否亮
                    if (!value && !GlobalValues.CylinderDicCollection[cylinderName].ShiedSensorOriginInput)
                    {
                        return motionCardService.GetInputStsEx(GlobalValues.CylinderDicCollection[cylinderName].SensorOriginInputInfo.Name) == true;
                    }
                    //气缸缩回,屏蔽原点传感器时,使用屏蔽原点传感器延时时间
                    if (!value && GlobalValues.CylinderDicCollection[cylinderName].ShiedSensorOriginInput)
                    {
                        Task.Delay(Convert.ToInt16(GlobalValues.CylinderDicCollection[cylinderName].ShiedSensorOriginInputDelayTime), cancellationTokenSource.Token).GetAwaiter().GetResult();
                        return true;
                    }
                    break;
                default:
                    throw new Exception("无此气缸传感器类型");
            }
            return false;
        }
        /// <summary>
        /// 获取轴当前位置
        /// </summary>
        /// <param name="motionCardService"></param>
        /// <param name="axisName"></param>
        /// <returns></returns>
        public static double GetAxesCurrentPosEx(this IMotionCardService motionCardService, string axisName)
        {
            return motionCardService.GetAxEncPos((short)MotionControlResource.AxisResource[axisName].AxisIdOnCard, 1)[0] / MotionControlResource.AxisResource[axisName].Proportion;
        }
        /// <summary>
        /// 获取功能屏蔽状态
        /// </summary>
        /// <param name="motionCardService"></param>
        /// <param name="functionShieldName"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static bool GetFunctionShieldStsEx(this IMotionCardService motionCardService, string functionShieldName)
        {
#if DEBUG
            if (!GlobalValues.MCFunctionShieldDicCollection.ContainsKey(functionShieldName))
                throw new Exception($"功能屏蔽中没有此功能名称{functionShieldName}");
#endif
            return GlobalValues.MCFunctionShieldDicCollection[functionShieldName].IsActive;
        }
        /// <summary>
        /// 获取功能屏蔽状态
        /// </summary>
        /// <param name="functionShieldName"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static bool GetFunctionShieldStsEx(string functionShieldName)
        {
#if DEBUG
            if (!GlobalValues.MCFunctionShieldDicCollection.ContainsKey(functionShieldName))
                throw new Exception($"功能屏蔽中没有此功能名称{functionShieldName}");
#endif
            return GlobalValues.MCFunctionShieldDicCollection[functionShieldName].IsActive;
        }
    }
}
