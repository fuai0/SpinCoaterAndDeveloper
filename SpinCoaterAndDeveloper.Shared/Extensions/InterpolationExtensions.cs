using LogServiceInterface;
using MotionCardServiceInterface;
using MotionControlActuation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SpinCoaterAndDeveloper.Shared.Extensions
{
    public static class InterpolationExtensions
    {
        /// <summary>
        /// 编译插补路径并启动插补
        /// IO如果屏蔽,则直接使用屏蔽默认值.屏蔽值不参与取反计算.如果未屏蔽且取反Enable,则输出取反值
        /// </summary>
        /// <param name="motionCardService"></param>
        /// <param name="logService"></param>
        /// <param name="pathName">插补路径名称</param>
        /// <param name="deltaX">引导偏差X</param>
        /// <param name="deltaY">引导偏差Y</param>
        /// <param name="deltaZ">引导偏差Z</param>
        /// <param name="IOPortName">IO名称</param>
        /// <param name="error">错误信息</param>
        /// <returns></returns>
        public static bool InterpolationPathComplierAndStartEx(this IMotionCardService motionCardService, ILogService logService, string pathName, double deltaX, double deltaY, double deltaZ, string IOPortName, ref string error)
        {
            try
            {
                if (!GlobalValues.InterpolationPaths.ContainsKey(pathName))
                {
                    error = "插补路径名称不存在";
                    logService.WriteLog(LogTypes.DB.ToString(), error, MessageDegree.ERROR);
                    return false;
                }
                //路径
                var coordinatePath = GlobalValues.InterpolationPaths[pathName];
                //排序
                var sortList = coordinatePath.InterpolationPaths.OrderBy(t => t.Sequence).ToList();
                //插补坐标系
                short[] axMapping = new short[3];
                axMapping[0] = (short)GlobalValues.InterpolationPaths[pathName].AxisX.AxisIdOnCard;
                axMapping[1] = (short)GlobalValues.InterpolationPaths[pathName].AxisY.AxisIdOnCard;
                axMapping[2] = GlobalValues.InterpolationPaths[pathName].EnableAxisZ ? (short)GlobalValues.InterpolationPaths[pathName].AxisZ.AxisIdOnCard : (short)-1;   //如果Z不启用写入-1到插补坐标系

                if (!motionCardService.InterpolationPrepare((short)coordinatePath.InterpolationCoordinateID, axMapping))
                {
                    error = "插补准备失败";
                    logService.WriteLog(LogTypes.DB.ToString(), error, MessageDegree.ERROR);
                    return false;
                }
                //遍历
                foreach (var path in sortList)
                {
                    //检测StartDelayIO指令
                    if (path.IOEnable)
                    {
                        //设置IO打开
                        if (!motionCardService.InterpolationDO((short)coordinatePath.InterpolationCoordinateID, (short)(MotionControlResource.IOOutputResource[IOPortName].ProgramAddressGroup * 8 + MotionControlResource.IOOutputResource[IOPortName].ProgramAddressPosition),
                            MotionControlResource.IOOutputResource[IOPortName].ShieldEnable ?
                                (MotionControlResource.IOOutputResource[IOPortName].GetShiedlEnableDefaultValue() ? (short)1 : (short)0) :
                                    MotionControlResource.IOOutputResource[IOPortName].ReverseEnable ? (short)0 : (short)1))
                        {
                            error = "设置插补IO打开失败";
                            logService.WriteLog(LogTypes.DB.ToString(), error, MessageDegree.ERROR);
                            return false;
                        }
                        //IO打开并且StartDleayIO打开
                        if (path.IOEnable && path.StartDelayIOEnable)
                        {
                            if (!motionCardService.InterpolationWaitTime((short)coordinatePath.InterpolationCoordinateID, (int)path.StartDelayTime))
                            {
                                error = "设置插补等待时间失败";
                                logService.WriteLog(LogTypes.DB.ToString(), error, MessageDegree.ERROR);
                                return false;
                            }
                        }
                    }
                    else
                    {
                        // 设置IO关闭
                        if (!motionCardService.InterpolationDO((short)coordinatePath.InterpolationCoordinateID, (short)(MotionControlResource.IOOutputResource[IOPortName].ProgramAddressGroup * 8 + MotionControlResource.IOOutputResource[IOPortName].ProgramAddressPosition),
                            MotionControlResource.IOOutputResource[IOPortName].ShieldEnable ?
                                (MotionControlResource.IOOutputResource[IOPortName].GetShiedlEnableDefaultValue() ? (short)1 : (short)0) :
                                    MotionControlResource.IOOutputResource[IOPortName].ReverseEnable ? (short)1 : (short)0))
                        {
                            error = "设置插补IO关闭失败";
                            logService.WriteLog(LogTypes.DB.ToString(), error, MessageDegree.ERROR);
                            return false;
                        }
                    }
                    //直线插补类型
                    if (path.PathMode == Models.InterpolationModels.InterpolationPathMode.Line)
                    {

                        if (coordinatePath.EnableAxisR == false && coordinatePath.EnableAxisA == false)
                        {
                            //R轴不启用,A轴不启用
                            var result = motionCardService.InterpolationLineXYZ(
                                            (short)coordinatePath.InterpolationCoordinateID,                                                //坐标系ID
                                            (coordinatePath.BeginningX + path.TX + deltaX) * coordinatePath.AxisX.Proportion,           //目标X,起点+目标点+引导X
                                            (coordinatePath.BeginningY + path.TY + deltaY) * coordinatePath.AxisY.Proportion,           //目标Y,起点+目标点+引导Y
                                            GlobalValues.InterpolationPaths[pathName].EnableAxisZ ? (coordinatePath.BeginningZ + path.TZ + deltaZ) * coordinatePath.AxisZ.Proportion : 0,           //目标Z,起点+目标点+引导Z
                                            path.Speed * coordinatePath.AxisX.Proportion * GlobalValues.GlobalVelPercentage / 100,      //运动速度,以X为轴当量为标准(插补轴当量必须相同)
                                            path.AccSpeed * coordinatePath.AxisX.Proportion * GlobalValues.GlobalVelPercentage / 100,   //加速度
                                            path.AccSpeed * coordinatePath.AxisX.Proportion * GlobalValues.GlobalVelPercentage / 100);  //减速度
                            if (result != true)
                            {
                                error = "设定XYZ直线插补失败";
                                logService.WriteLog(LogTypes.DB.ToString(), error, MessageDegree.ERROR);
                                return false;
                            }
                        }
                        else if (coordinatePath.EnableAxisR == true && coordinatePath.EnableAxisA == false)
                        {
                            //R轴启用,A轴不启用(启用了R,Z必须启用)
                            var result = motionCardService.InterpolationLineXYZR(
                                            (short)coordinatePath.InterpolationCoordinateID,                                                //坐标系ID
                                            (coordinatePath.BeginningX + path.TX + deltaX) * coordinatePath.AxisX.Proportion,           //目标X,起点+目标点+引导X
                                            (coordinatePath.BeginningY + path.TY + deltaY) * coordinatePath.AxisY.Proportion,           //目标Y,起点+目标点+引导Y
                                            (coordinatePath.BeginningZ + path.TZ + deltaZ) * coordinatePath.AxisZ.Proportion,           //目标Z,起点+目标点+引导Z
                                            (short)coordinatePath.AxisR.AxisIdOnCard,                                                             //R轴映射轴号
                                            (coordinatePath.BeginningR + path.TR) * coordinatePath.AxisR.Proportion,                    //R轴移动位置,无补偿
                                            path.Speed * coordinatePath.AxisX.Proportion * GlobalValues.GlobalVelPercentage / 100,      //运动速度,以X为轴当量为标准(插补轴当量必须相同)
                                            path.AccSpeed * coordinatePath.AxisX.Proportion * GlobalValues.GlobalVelPercentage / 100,   //加速度
                                            path.AccSpeed * coordinatePath.AxisX.Proportion * GlobalValues.GlobalVelPercentage / 100);  //减速度
                            if (result != true)
                            {
                                error = "设定XYZR直线插补失败";
                                logService.WriteLog(LogTypes.DB.ToString(), error, MessageDegree.ERROR);
                                return false;
                            }
                        }
                        else if (coordinatePath.EnableAxisA == true && coordinatePath.EnableAxisA == true)
                        {
                            //R轴启用,A轴启用(启用了R,A,Z必须启用)
                            var result = motionCardService.InterpolationLineXYZRA(
                                            (short)coordinatePath.InterpolationCoordinateID,                                                //坐标系ID
                                            (coordinatePath.BeginningX + path.TX + deltaX) * coordinatePath.AxisX.Proportion,           //目标X,起点+目标点+引导X
                                            (coordinatePath.BeginningY + path.TY + deltaY) * coordinatePath.AxisY.Proportion,           //目标Y,起点+目标点+引导Y
                                            (coordinatePath.BeginningZ + path.TZ + deltaZ) * coordinatePath.AxisZ.Proportion,           //目标Z,起点+目标点+引导Z
                                            (short)coordinatePath.AxisR.AxisIdOnCard,                                                             //R轴映射轴号
                                            (coordinatePath.BeginningR + path.TR) * coordinatePath.AxisR.Proportion,                    //R轴移动位置,无补偿
                                            (short)coordinatePath.AxisA.AxisIdOnCard,                                                             //A轴映射轴号
                                            (coordinatePath.BeginningA + path.TA) * coordinatePath.AxisA.Proportion,                    //A轴移动位置,无补偿
                                            path.Speed * coordinatePath.AxisX.Proportion * GlobalValues.GlobalVelPercentage / 100,      //运动速度,以X为轴当量为标准(插补轴当量必须相同)
                                            path.AccSpeed * coordinatePath.AxisX.Proportion * GlobalValues.GlobalVelPercentage / 100,   //加速度
                                            path.AccSpeed * coordinatePath.AxisX.Proportion * GlobalValues.GlobalVelPercentage / 100);  //减速度
                            if (result != true)
                            {
                                error = "设定XYZRA直线插补失败";
                                logService.WriteLog(LogTypes.DB.ToString(), error, MessageDegree.ERROR);
                                return false;
                            }
                        }
                        else if (coordinatePath.EnableAxisR == false && coordinatePath.EnableAxisA == true)
                        {
                            //R轴不启用,A轴启用
                            error = "Line不支持R轴不启用,A轴启用模式";
                            logService.WriteLog(LogTypes.DB.ToString(), error, MessageDegree.ERROR);
                            return false;
                        }
                        else
                        {
                            error = "Line未知异常情况,理论不会发生!";
                            logService.WriteLog(LogTypes.DB.ToString(), error, MessageDegree.ERROR);
                            return false;
                        }

                    }
                    //圆弧插补类型
                    if (path.PathMode == Models.InterpolationModels.InterpolationPathMode.Arc)
                    {
                        if (coordinatePath.EnableAxisR == false && coordinatePath.EnableAxisA == false)
                        {
                            //R轴不启用,A轴不启用
                            var result = motionCardService.InterpolationArcThreePointXYZ(
                                            (short)coordinatePath.InterpolationCoordinateID,                                                //坐标系ID
                                            (coordinatePath.BeginningX + path.MX + deltaX) * coordinatePath.AxisX.Proportion,           //中间点X,起点+目标点+引导X
                                            (coordinatePath.BeginningY + path.MY + deltaY) * coordinatePath.AxisY.Proportion,           //中间点Y,起点+目标点+引导Y
                                            GlobalValues.InterpolationPaths[pathName].EnableAxisZ ? (coordinatePath.BeginningZ + path.MZ + deltaZ) * coordinatePath.AxisZ.Proportion : 0,           //中间点Z,起点+目标点+引导Z
                                            (coordinatePath.BeginningX + path.TX + deltaX) * coordinatePath.AxisX.Proportion,           //终点X,起点+目标点+引导X
                                            (coordinatePath.BeginningY + path.TY + deltaY) * coordinatePath.AxisY.Proportion,           //终点Y,起点+目标点+引导Y
                                            GlobalValues.InterpolationPaths[pathName].EnableAxisZ ? (coordinatePath.BeginningZ + path.TZ + deltaZ) * coordinatePath.AxisZ.Proportion : 0,           //终点Z,起点+目标点+引导Z
                                            path.Speed * coordinatePath.AxisX.Proportion * GlobalValues.GlobalVelPercentage / 100,      //运动速度,以X为轴当量为标准(插补轴当量必须相同)
                                            path.AccSpeed * coordinatePath.AxisX.Proportion * GlobalValues.GlobalVelPercentage / 100,   //加速度
                                            path.AccSpeed * coordinatePath.AxisX.Proportion * GlobalValues.GlobalVelPercentage / 100);  //减速度
                            if (result != true)
                            {
                                error = "设定XYZ圆弧插补失败";
                                logService.WriteLog(LogTypes.DB.ToString(), error, MessageDegree.ERROR);
                                return false;
                            }
                        }
                        else if (coordinatePath.EnableAxisR == true && coordinatePath.EnableAxisA == false)
                        {
                            //R轴启用,A轴不启用
                            var result = motionCardService.InterpolationArcThreePointXYZR(
                                            (short)coordinatePath.InterpolationCoordinateID,                                                //坐标系ID
                                            (coordinatePath.BeginningX + path.MX + deltaX) * coordinatePath.AxisX.Proportion,           //中间点X,起点+目标点+引导X
                                            (coordinatePath.BeginningY + path.MY + deltaY) * coordinatePath.AxisY.Proportion,           //中间点Y,起点+目标点+引导Y
                                            (coordinatePath.BeginningZ + path.MZ + deltaZ) * coordinatePath.AxisZ.Proportion,           //中间点Z,起点+目标点+引导Z
                                            (coordinatePath.BeginningX + path.TX + deltaX) * coordinatePath.AxisX.Proportion,           //终点X,起点+目标点+引导X
                                            (coordinatePath.BeginningY + path.TY + deltaY) * coordinatePath.AxisY.Proportion,           //终点Y,起点+目标点+引导Y
                                            (coordinatePath.BeginningZ + path.TZ + deltaZ) * coordinatePath.AxisZ.Proportion,           //终点Z,起点+目标点+引导Z
                                            (short)coordinatePath.AxisR.AxisIdOnCard,                                                             //R轴映射轴号
                                            (coordinatePath.BeginningR + path.TR) * coordinatePath.AxisR.Proportion,                    //R轴移动位置,无补偿
                                            path.Speed * coordinatePath.AxisX.Proportion * GlobalValues.GlobalVelPercentage / 100,      //运动速度,以X为轴当量为标准(插补轴当量必须相同)
                                            path.AccSpeed * coordinatePath.AxisX.Proportion * GlobalValues.GlobalVelPercentage / 100,   //加速度
                                            path.AccSpeed * coordinatePath.AxisX.Proportion * GlobalValues.GlobalVelPercentage / 100);  //减速度
                            if (result != true)
                            {
                                error = "设定XYZR圆弧插补失败";
                                logService.WriteLog(LogTypes.DB.ToString(), error, MessageDegree.ERROR);
                                return false;
                            }
                        }
                        else if (coordinatePath.EnableAxisA == true && coordinatePath.EnableAxisA == true)
                        {
                            //R轴启用,A轴启用
                            var result = motionCardService.InterpolationArcThreePointXYZRA(
                                            (short)coordinatePath.InterpolationCoordinateID,                                                //坐标系ID
                                            (coordinatePath.BeginningX + path.MX + deltaX) * coordinatePath.AxisX.Proportion,           //中间点X,起点+目标点+引导X
                                            (coordinatePath.BeginningY + path.MY + deltaY) * coordinatePath.AxisY.Proportion,           //中间点Y,起点+目标点+引导Y
                                            (coordinatePath.BeginningZ + path.MZ + deltaZ) * coordinatePath.AxisZ.Proportion,           //中间点Z,起点+目标点+引导Z
                                            (coordinatePath.BeginningX + path.TX + deltaX) * coordinatePath.AxisX.Proportion,           //终点X,起点+目标点+引导X
                                            (coordinatePath.BeginningY + path.TY + deltaY) * coordinatePath.AxisY.Proportion,           //终点Y,起点+目标点+引导Y
                                            (coordinatePath.BeginningZ + path.TZ + deltaZ) * coordinatePath.AxisZ.Proportion,           //终点Z,起点+目标点+引导Z
                                            (short)coordinatePath.AxisR.AxisIdOnCard,                                                             //R轴映射轴号
                                            (coordinatePath.BeginningR + path.TR) * coordinatePath.AxisR.Proportion,                    //R轴移动位置,无补偿
                                            (short)coordinatePath.AxisA.AxisIdOnCard,                                                             //A轴映射轴号
                                            (coordinatePath.BeginningA + path.TA) * coordinatePath.AxisA.Proportion,                    //A轴移动位置,无补偿
                                            path.Speed * coordinatePath.AxisX.Proportion * GlobalValues.GlobalVelPercentage / 100,      //运动速度,以X为轴当量为标准(插补轴当量必须相同)
                                            path.AccSpeed * coordinatePath.AxisX.Proportion * GlobalValues.GlobalVelPercentage / 100,   //加速度
                                            path.AccSpeed * coordinatePath.AxisX.Proportion * GlobalValues.GlobalVelPercentage / 100);  //减速度
                            if (result != true)
                            {
                                error = "设定XYZRA圆弧插补失败";
                                logService.WriteLog(LogTypes.DB.ToString(), error, MessageDegree.ERROR);
                                return false;
                            }
                        }
                        else if (coordinatePath.EnableAxisR == false && coordinatePath.EnableAxisA == true)
                        {
                            //R轴不启用,A轴启用
                            error = "ARC不支持R轴不启用,A轴启用模式";
                            logService.WriteLog(LogTypes.DB.ToString(), error, MessageDegree.ERROR);
                            return false;
                        }
                        else
                        {
                            error = "Arc未知异常情况,理论不会发生!";
                            logService.WriteLog(LogTypes.DB.ToString(), error, MessageDegree.ERROR);
                            return false;
                        }
                    }
                    //检测EndDelayIO指令
                    if (path.IOEnable)
                    {
                        if (path.IOEnable && path.EndDelayIOEnable == true)
                        {
                            //IO打开且关闭延迟打开
                            if (motionCardService.InterpolationWaitTime((short)coordinatePath.InterpolationCoordinateID, (int)path.EndDelayTime))
                            {
                                error = "设定延时关闭IO等待时间失败";
                                logService.WriteLog(LogTypes.DB.ToString(), error, MessageDegree.ERROR);
                                return false;
                            }
                            //当前逻辑为,如果启用了EndDelayIO,认为会关闭IO
                            if (motionCardService.InterpolationDO((short)coordinatePath.InterpolationCoordinateID, (short)(MotionControlResource.IOOutputResource[IOPortName].ProgramAddressGroup * 8 + MotionControlResource.IOOutputResource[IOPortName].ProgramAddressPosition),
                                MotionControlResource.IOOutputResource[IOPortName].ShieldEnable ?
                                    (MotionControlResource.IOOutputResource[IOPortName].GetShiedlEnableDefaultValue() ? (short)1 : (short)0) :
                                        MotionControlResource.IOOutputResource[IOPortName].ReverseEnable ? (short)1 : (short)0))
                            {
                                error = "设定延时关闭IO失败";
                                logService.WriteLog(LogTypes.DB.ToString(), error, MessageDegree.ERROR);
                                return false;
                            }
                        }
                    }
                }
                if (!motionCardService.InterpolationSendData((short)coordinatePath.InterpolationCoordinateID))
                {
                    error = "压入插补数据失败";
                    logService.WriteLog(LogTypes.DB.ToString(), error, MessageDegree.ERROR);
                    return false;
                }
                if (!motionCardService.InterpolationStart((short)coordinatePath.InterpolationCoordinateID))
                {
                    error = "启动插补失败";
                    logService.WriteLog(LogTypes.DB.ToString(), error, MessageDegree.ERROR);
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $"插补路径生成异常{ex.Message}", ex);
                return false;
            }
        }
        /// <summary>
        /// 获取插补运行中状态
        /// </summary>
        /// <param name="motionCardService"></param>
        /// <param name="logService"></param>
        /// <param name="pathName"></param>
        /// <returns>true:无错误,false:插补出错(自动清理插补数据及坐标系)</returns>
        public static bool InterpolationRunningStsEx(this IMotionCardService motionCardService, ILogService logService, string pathName)
        {
            try
            {
#if DEBUG
                if (!GlobalValues.InterpolationPaths.ContainsKey(pathName))
                    throw new Exception($"不存在插补路径{pathName}");
#endif
                return motionCardService.InterpolationRunSts((short)GlobalValues.InterpolationPaths[pathName].InterpolationCoordinateID);
            }
            catch (Exception ex)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $"获取插补运行状态异常{ex.Message}", ex);
                return false;
            }
        }
        /// <summary>
        /// 获取插补是否执行结束状态
        /// </summary>
        /// <param name="motionCardService"></param>
        /// <param name="logService"></param>
        /// <param name="pathName"></param>
        /// <returns>true:插补运行到位(清理插补数据及坐标系),false:插补未到位</returns>
        public static bool InterpolationFinishStsEx(this IMotionCardService motionCardService, ILogService logService, string pathName)
        {
            try
            {
#if DEBUG
                if (!GlobalValues.InterpolationPaths.ContainsKey(pathName))
                    throw new Exception($"不存在插补路径{pathName}");
#endif
                if (motionCardService.InterpolationFinishSts((short)GlobalValues.InterpolationPaths[pathName].InterpolationCoordinateID))
                {
                    //插补运行结束,正常停止,清理队列,销毁坐标系
                    motionCardService.InterpolationStop((short)GlobalValues.InterpolationPaths[pathName].InterpolationCoordinateID);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $"获取插补结束状态异常{ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 插补运行结束,正常停止,清理队列,销毁坐标系
        /// </summary>
        /// <param name="motionCardService"></param>
        /// <param name="logService"></param>
        /// <param name="pathName"></param>
        /// <returns></returns>
        public static bool InterpolationStopEx(this IMotionCardService motionCardService, ILogService logService, string pathName)
        {
            try
            {
#if DEBUG
                if (!GlobalValues.InterpolationPaths.ContainsKey(pathName))
                    throw new Exception($"不存在插补路径{pathName}");
#endif
                //插补运行结束,正常停止,清理队列,销毁坐标系
                return motionCardService.InterpolationStop((short)GlobalValues.InterpolationPaths[pathName].InterpolationCoordinateID);
            }
            catch (Exception ex)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $"停止插补运动异常,{ex.Message}", ex);
                return false;
            }
        }
        /// <summary>
        /// 暂停插补运动
        /// </summary>
        /// <param name="motionCardService"></param>
        /// <param name="logService"></param>
        /// <param name="pathName"></param>
        /// <returns></returns>
        public static bool InterpolationPauseEx(this IMotionCardService motionCardService, ILogService logService, string pathName)
        {
            try
            {
#if DEBUG
                if (!GlobalValues.InterpolationPaths.ContainsKey(pathName))
                    throw new Exception($"不存在插补路径{pathName}");
#endif
                return motionCardService.InterpolationPause((short)GlobalValues.InterpolationPaths[pathName].InterpolationCoordinateID);
            }
            catch (Exception ex)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $"暂停插补运动异常,{ex.Message}", ex);
                return false;
            }
        }
        /// <summary>
        /// 恢复插补运动
        /// </summary>
        /// <param name="motionCardService"></param>
        /// <param name="logService"></param>
        /// <param name="pathName"></param>
        /// <returns></returns>
        public static bool InterpolationResumeEx(this IMotionCardService motionCardService, ILogService logService, string pathName)
        {
            try
            {
#if DEBUG
                if (!GlobalValues.InterpolationPaths.ContainsKey(pathName))
                    throw new Exception($"不存在插补路径{pathName}");
#endif
                return motionCardService.InterpolationResume((short)GlobalValues.InterpolationPaths[pathName].InterpolationCoordinateID);
            }
            catch (Exception ex)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $"恢复插补运动异常,{ex.Message}", ex);
                return false;
            }
        }
        /// <summary>
        /// 运动到插补起点.多轴一起运动,请确保安全的情况下调用此函数
        /// </summary>
        /// <param name="motionCardService"></param>
        /// <param name="logService"></param>
        /// <param name="pathName"></param>
        /// <param name="deltaX"></param>
        /// <param name="deltaY"></param>
        /// <param name="deltaZ"></param>
        /// <returns></returns>
        public static bool InterpolationMoveToStartPointEx(this IMotionCardService motionCardService, ILogService logService, string pathName, double deltaX, double deltaY, double deltaZ)
        {
            try
            {
#if DEBUG
                if (!GlobalValues.InterpolationPaths.ContainsKey(pathName))
                    throw new Exception($"不存在插补路径{pathName}");
#endif
                bool ret = true;
                ret &= motionCardService.StartMoveAbs((short)GlobalValues.InterpolationPaths[pathName].AxisX.AxisIdOnCard,
                                                        0,
                                                        0,
                                                        GlobalValues.InterpolationPaths[pathName].BeginningXVel * GlobalValues.InterpolationPaths[pathName].AxisX.Proportion * GlobalValues.GlobalVelPercentage / 100,
                                                        GlobalValues.InterpolationPaths[pathName].BeginningXAcc * GlobalValues.InterpolationPaths[pathName].AxisX.Proportion * GlobalValues.GlobalVelPercentage / 100,
                                                        GlobalValues.InterpolationPaths[pathName].BeginningXDec * GlobalValues.InterpolationPaths[pathName].AxisX.Proportion * GlobalValues.GlobalVelPercentage / 100,
                                                        (GlobalValues.InterpolationPaths[pathName].BeginningX + deltaX) * GlobalValues.InterpolationPaths[pathName].AxisX.Proportion);
                ret &= motionCardService.StartMoveAbs((short)GlobalValues.InterpolationPaths[pathName].AxisY.AxisIdOnCard,
                                                        0,
                                                        0,
                                                        GlobalValues.InterpolationPaths[pathName].BeginningYVel * GlobalValues.InterpolationPaths[pathName].AxisY.Proportion * GlobalValues.GlobalVelPercentage / 100,
                                                        GlobalValues.InterpolationPaths[pathName].BeginningYAcc * GlobalValues.InterpolationPaths[pathName].AxisY.Proportion * GlobalValues.GlobalVelPercentage / 100,
                                                        GlobalValues.InterpolationPaths[pathName].BeginningYDec * GlobalValues.InterpolationPaths[pathName].AxisY.Proportion * GlobalValues.GlobalVelPercentage / 100,
                                                        (GlobalValues.InterpolationPaths[pathName].BeginningY + deltaY) * GlobalValues.InterpolationPaths[pathName].AxisY.Proportion);
                if (GlobalValues.InterpolationPaths[pathName].EnableAxisZ)
                {
                    ret &= motionCardService.StartMoveAbs((short)GlobalValues.InterpolationPaths[pathName].AxisZ.AxisIdOnCard,
                                                        0,
                                                        0,
                                                        GlobalValues.InterpolationPaths[pathName].BeginningZVel * GlobalValues.InterpolationPaths[pathName].AxisZ.Proportion * GlobalValues.GlobalVelPercentage / 100,
                                                        GlobalValues.InterpolationPaths[pathName].BeginningZAcc * GlobalValues.InterpolationPaths[pathName].AxisZ.Proportion * GlobalValues.GlobalVelPercentage / 100,
                                                        GlobalValues.InterpolationPaths[pathName].BeginningZDec * GlobalValues.InterpolationPaths[pathName].AxisZ.Proportion * GlobalValues.GlobalVelPercentage / 100,
                                                        (GlobalValues.InterpolationPaths[pathName].BeginningZ + deltaZ) * GlobalValues.InterpolationPaths[pathName].AxisZ.Proportion);
                }
                if (GlobalValues.InterpolationPaths[pathName].EnableAxisR)
                {
                    ret &= motionCardService.StartMoveAbs((short)GlobalValues.InterpolationPaths[pathName].AxisR.AxisIdOnCard,
                                                        0,
                                                        0,
                                                        GlobalValues.InterpolationPaths[pathName].BeginningRVel * GlobalValues.InterpolationPaths[pathName].AxisR.Proportion * GlobalValues.GlobalVelPercentage / 100,
                                                        GlobalValues.InterpolationPaths[pathName].BeginningRAcc * GlobalValues.InterpolationPaths[pathName].AxisR.Proportion * GlobalValues.GlobalVelPercentage / 100,
                                                        GlobalValues.InterpolationPaths[pathName].BeginningRDec * GlobalValues.InterpolationPaths[pathName].AxisR.Proportion * GlobalValues.GlobalVelPercentage / 100,
                                                        GlobalValues.InterpolationPaths[pathName].BeginningR * GlobalValues.InterpolationPaths[pathName].AxisR.Proportion);
                }
                if (GlobalValues.InterpolationPaths[pathName].EnableAxisA)
                {
                    ret &= motionCardService.StartMoveAbs((short)GlobalValues.InterpolationPaths[pathName].AxisA.AxisIdOnCard,
                                                        0,
                                                        0,
                                                        GlobalValues.InterpolationPaths[pathName].BeginningAVel * GlobalValues.InterpolationPaths[pathName].AxisA.Proportion * GlobalValues.GlobalVelPercentage / 100,
                                                        GlobalValues.InterpolationPaths[pathName].BeginningAAcc * GlobalValues.InterpolationPaths[pathName].AxisA.Proportion * GlobalValues.GlobalVelPercentage / 100,
                                                        GlobalValues.InterpolationPaths[pathName].BeginningADec * GlobalValues.InterpolationPaths[pathName].AxisA.Proportion * GlobalValues.GlobalVelPercentage / 100,
                                                        GlobalValues.InterpolationPaths[pathName].BeginningA * GlobalValues.InterpolationPaths[pathName].AxisA.Proportion);
                }
                return ret;
            }
            catch (Exception ex)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $"插补运动到起点异常,{ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 停止运动到插补起点
        /// </summary>
        /// <param name="motionCardService"></param>
        /// <param name="logService"></param>
        /// <param name="pathName"></param>
        /// <returns></returns>
        public static bool InterpolationStopMoveToStartPointEx(this IMotionCardService motionCardService, ILogService logService, string pathName)
        {
            try
            {
#if DEBUG
                if (!GlobalValues.InterpolationPaths.ContainsKey(pathName))
                    throw new Exception($"不存在插补路径{pathName}");
#endif
                bool ret = true;
                ret &= motionCardService.StopMove((short)GlobalValues.InterpolationPaths[pathName].AxisX.AxisIdOnCard, 1);
                ret &= motionCardService.StopMove((short)GlobalValues.InterpolationPaths[pathName].AxisY.AxisIdOnCard, 1);
                if (GlobalValues.InterpolationPaths[pathName].EnableAxisZ)
                {
                    ret &= motionCardService.StopMove((short)GlobalValues.InterpolationPaths[pathName].AxisZ.AxisIdOnCard, 1);
                }
                if (GlobalValues.InterpolationPaths[pathName].EnableAxisR)
                {
                    ret &= motionCardService.StopMove((short)GlobalValues.InterpolationPaths[pathName].AxisR.AxisIdOnCard, 1);
                }
                if (GlobalValues.InterpolationPaths[pathName].EnableAxisA)
                {
                    ret &= motionCardService.StopMove((short)GlobalValues.InterpolationPaths[pathName].AxisA.AxisIdOnCard, 1);
                }
                return ret;
            }
            catch (Exception ex)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $"插补运动到起点停止异常,{ex.Message}", ex);
                return false;
            }
        }
        /// <summary>
        /// 插补运动到起点是否到达
        /// </summary>
        /// <param name="motionCardService"></param>
        /// <param name="logService"></param>
        /// <param name="pathName"></param>
        /// <returns></returns>
        public static bool InterpolationMoveToStartPointArrived(this IMotionCardService motionCardService, ILogService logService, string pathName)
        {
            try
            {
#if DEBUG
                if (!GlobalValues.InterpolationPaths.ContainsKey(pathName))
                    throw new Exception($"不存在插补路径{pathName}");
#endif
                bool ret = true;
                ret &= motionCardService.RealTimeCheckAxisArrivedEx(GlobalValues.InterpolationPaths[pathName].AxisX.Name);
                ret &= motionCardService.RealTimeCheckAxisArrivedEx(GlobalValues.InterpolationPaths[pathName].AxisY.Name);
                if (GlobalValues.InterpolationPaths[pathName].EnableAxisZ)
                {
                    ret &= motionCardService.RealTimeCheckAxisArrivedEx(GlobalValues.InterpolationPaths[pathName].AxisZ.Name);
                }
                if (GlobalValues.InterpolationPaths[pathName].EnableAxisR)
                {
                    ret &= motionCardService.RealTimeCheckAxisArrivedEx(GlobalValues.InterpolationPaths[pathName].AxisR.Name);
                }
                if (GlobalValues.InterpolationPaths[pathName].EnableAxisA)
                {
                    ret &= motionCardService.RealTimeCheckAxisArrivedEx(GlobalValues.InterpolationPaths[pathName].AxisA.Name);
                }

                return ret;
            }
            catch (Exception ex)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $"插补运动到起点是否到达异常,{ex.Message}", ex);
                return false;
            }
        }
    }
}
