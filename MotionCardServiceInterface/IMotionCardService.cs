using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotionCardServiceInterface
{
    /// <summary>
    /// 运动控制卡服务接口
    /// </summary>
    public interface IMotionCardService
    {
        /// <summary>
        /// 运动控制卡初始化成功标志
        /// </summary>
        bool InitSuccess { get; set; }
        /// <summary>
        /// 初始化运动控制卡
        /// </summary>
        /// <returns></returns>
        bool Init();
        /// <summary>
        /// 启动回原点
        /// </summary>
        /// <param name="homingAxNo"></param>
        /// <param name="homeMethod"></param>
        /// <param name="offset"></param>
        /// <param name="highVel"></param>
        /// <param name="lowVel"></param>
        /// <param name="acc"></param>
        /// <param name="overtime"></param>
        /// <param name="posSrc"></param>
        /// <returns></returns>
        bool StartHoming(short homingAxNo, short homeMethod, int offset, uint highVel, uint lowVel, uint acc, uint overtime, short posSrc);
        /// <summary>
        /// 获取回原状态
        /// </summary>
        /// <param name="homingAxNo"></param>
        /// <returns></returns>
        bool GetHomingSts(short homingAxNo);
        /// <summary>
        /// 停止回原
        /// </summary>
        /// <param name="homingAxNo"></param>
        bool StopHoming(short homingAxNo);
        /// <summary>
        /// 结束回原
        /// </summary>
        /// <param name="homingAxNo"></param>
        bool FinishHoming(short homingAxNo);
        /// <summary>
        /// 关闭板卡不复位
        /// </summary>
        bool Close();
        /// <summary>
        /// 关闭并复位板卡
        /// </summary>
        bool CloseWithReset();
        /// <summary>
        /// Jog运动启动
        /// </summary>
        /// <param name="axNo"></param>
        /// <param name="tgVel"></param>
        /// <param name="dir"></param>
        /// <param name="acc"></param>
        /// <param name="dec"></param>
        /// <param name="dsParaLTDMC">雷赛板卡S段速度参数</param>
        /// <returns></returns>
        bool JogMoveStart(short axNo, double tgVel, Direction dir, double acc, double dec, double dsParaLTDMC = double.NaN);
        /// <summary>
        /// Jog运动停止
        /// </summary>
        /// <param name="axNo"></param>
        bool JogMoveStop(short axNo);
        /// <summary>
        /// 轴上/下使能
        /// </summary>
        /// <param name="axNo"></param>
        /// <param name="enable"></param>
        /// <returns></returns>
        bool AxisServo(short axNo, bool enable);
        /// <summary>
        /// 获取轴规划模式
        /// </summary>
        /// <param name="axNo">起始轴号</param>
        /// <param name="count">需要获取的轴数量</param>
        /// <returns></returns>
        Int16[] GetAxPrfMode(short axNo, Int16 count);
        /// <summary>
        /// 获取轴状态
        /// </summary>
        /// <param name="axNo">起始轴号</param>
        /// <param name="count">需要获取的轴数量</param>
        /// <returns></returns>
        int[] GetAxSts(short axNo, Int16 count);
        //Int16 GetMultiAxArrivalSts(Int32 axMask);   //多轴同时到位检查

        /// <summary>
        /// 获取规划位置
        /// </summary>
        /// <param name="axNo">起始轴号</param>
        /// <param name="count">需要获取的轴数量</param>
        /// <returns></returns>
        double[] GetAxPrfPos(short axNo, Int16 count);
        /// <summary>
        /// 获取规划速度
        /// </summary>
        /// <param name="axNo">起始轴号</param>
        /// <param name="count">需要获取的轴数量</param>
        /// <returns></returns>
        double[] GetAxPrfVel(short axNo, Int16 count);
        /// <summary>
        /// 获取规划加速度
        /// </summary>
        /// <param name="axNo">起始轴号</param>
        /// <param name="count">需要获取的轴数量</param>
        /// <returns></returns>
        double[] GetAxPrfAcc(short axNo, Int16 count);
        /// <summary>
        /// 获取当前位置
        /// </summary>
        /// <param name="axNo">起始轴号</param>
        /// <param name="count">需要获取的轴数量</param>
        /// <returns></returns>
        double[] GetAxEncPos(short axNo, Int16 count);
        /// <summary>
        /// 获取当前速度
        /// </summary>
        /// <param name="axNo">起始轴号</param>
        /// <param name="count">需要获取的轴数量</param>
        /// <returns></returns>
        double[] GetAxEncVel(short axNo, Int16 count);
        /// <summary>
        /// 获取当前加速度
        /// </summary>
        /// <param name="axNo">起始轴号</param>
        /// <param name="count">需要获取的轴数量</param>
        /// <returns></returns>
        double[] GetAxEncAcc(short axNo, Int16 count);
        /// <summary>
        /// 按Bit获取Di
        /// </summary>
        /// <param name="diNo"></param>
        /// <returns></returns>
        bool? GetEcatDiBit(short diNo);
        /// <summary>
        /// 按Bit获取Do
        /// </summary>
        /// <param name="doNo"></param>
        /// <returns></returns>
        bool? GetEcatDoBit(short doNo);
        /// <summary>
        /// 按组获取Di
        /// </summary>
        /// <param name="groupNo"></param>
        /// <returns></returns>
        short? GetEcatGrpDi(short groupNo);
        /// <summary>
        /// 按组获取Do
        /// </summary>
        /// <param name="groupNo"></param>
        /// <returns></returns>
        short? GetEcatGrpDo(short groupNo);
        /// <summary>
        /// 按Bit设定Do
        /// </summary>
        /// <param name="doNo"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        bool SetEcatDoBit(short doNo, bool value);
        /// <summary>
        /// 按组设定Do
        /// </summary>
        /// <param name="groupNo"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        bool SetEcatGrpDo(short groupNo, short value);
        /// <summary>
        /// 轴绝对运动
        /// </summary>
        /// <param name="axNo"></param>
        /// <param name="velType">0:T形速度</param>
        /// <param name="ratio">0</param>
        /// <param name="vel"></param>
        /// <param name="acc"></param>
        /// <param name="dec"></param>
        /// <param name="tgtPos"></param>
        /// <param name="dsParaLTDMC">雷赛板卡S段速度参数</param>
        /// <returns></returns>
        bool StartMoveAbs(short axNo, short velType, double ratio, double vel, double acc, double dec, double tgtPos, double dsParaLTDMC = double.NaN);
        /// <summary>
        /// 轴相对运动
        /// </summary>
        /// <param name="axNo"></param>
        /// <param name="velType">0:T形速度</param>
        /// <param name="ratio">0</param>
        /// <param name="vel"></param>
        /// <param name="acc"></param>
        /// <param name="dec"></param>
        /// <param name="tgtPos"></param>
        /// <param name="dsParaLTDMC">雷赛板卡S段速度参数</param>
        /// <returns></returns>
        bool StartMoveRel(short axNo, short velType, double ratio, double vel, double acc, double dec, double tgtPos, double dsParaLTDMC = double.NaN);
        /// <summary>
        /// 停止轴运动
        /// </summary>
        /// <param name="axNo"></param>
        /// <param name="stopType"></param>
        /// <returns></returns>
        bool StopMove(short axNo, short stopType);
        /// <summary>
        /// 暂停轴运动
        /// </summary>
        /// <param name="axNo"></param>
        /// <returns></returns>
        bool PauseMove(short axNo);
        /// <summary>
        /// 恢复轴运动
        /// </summary>
        /// <param name="axNo"></param>
        /// <returns></returns>
        bool ResumeMove(short axNo);
        /// <summary>
        /// 清除轴错误
        /// </summary>
        /// <param name="axNo"></param>
        /// <returns></returns>
        bool ClearAxSts(short axNo);
        /// <summary>
        /// 板卡急停
        /// </summary>
        /// <returns></returns>
        bool EmgStop();
        /// <summary>
        /// 板卡急停取消
        /// </summary>
        /// <returns></returns>
        bool EmgStopCancel();
        /// <summary>
        /// 获取轴报错码
        /// </summary>
        /// <param name="axNo"></param>
        /// <returns></returns>
        short? GetAxErrorCode(short axNo);
    }
}
