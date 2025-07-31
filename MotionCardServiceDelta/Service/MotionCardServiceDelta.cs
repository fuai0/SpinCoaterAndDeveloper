using ImTools;
using LogServiceInterface;
using MotionCardServiceDelta.Core;
using MotionCardServiceInterface;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MotionCardServiceDelta.Service
{
    public class MotionCardServiceDeltaMC : IMotionCardService
    {
        private readonly ILogService logService;

        public MotionCardServiceDeltaMC(ILogService logService = null)
        {
            this.logService = logService;
            if (logService != null && logService.InitSuccess == false) logService.Init();

        }

        /// <summary>
        /// 初始话是否成功的标识
        /// </summary>
        public bool InitSuccess { get; set; } = false;


        /// <summary>
        /// 多张轴卡卡号的数组
        /// </summary>
        ushort[] ExistCardNoList = new ushort[32];

        /// <summary>
        /// 第一张轴卡的卡号
        /// </summary>
        ushort CardNo = 0;

        /// <summary>
        /// 回原时设置的加减速
        /// </summary>
        double Dec = 0;

        /// <summary>
        /// 通过EtherCAT连接轴卡的数量
        /// </summary>
        ushort ExistCards = 0;

        /// <summary>
        /// 槽号
        /// </summary>
        public ushort SlotId { get; internal set; } = 0;



        /// <summary>
        /// 初始化运动控制卡
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public bool Init()
        {
            try
            {
                ushort retCode = 0; //方法调用后的返回值
                ushort currExistCardsCount = 0;//当前第几张轴卡
                ushort curruCardNo = 0; //当前的轴卡卡号

                // step1  OpenCard 打开板卡，回传EtherCAT的轴卡数量
                retCode = DeltaMcApi.CS_ECAT_Master_Open(ref ExistCards);
                InitSuccess = false;
                if (ExistCards == 0)
                {
                    logService.WriteLog("DB", $"通过Ethercat连接的轴卡数量为{ExistCards},请进行硬件检查", MessageDegree.ERROR);
                    return InitSuccess;
                }
                else
                {
                    for (currExistCardsCount = 0; currExistCardsCount < 32; currExistCardsCount++)
                    {
                        ExistCardNoList[currExistCardsCount] = 99;
                    }

                    //step 2 设置CardNo 轴卡卡号
                    for (currExistCardsCount = 0; currExistCardsCount < ExistCards; currExistCardsCount++)
                    {
                        //返回轴卡的卡号
                        retCode = DeltaMcApi.CS_ECAT_Master_Get_CardSeq(currExistCardsCount, ref curruCardNo);
                        //初始化对应轴卡卡号的运动控制卡
                        retCode = DeltaMcApi.CS_ECAT_Master_Initial(curruCardNo);

                        if (retCode != DeltaMcErr.ERR_ECAT_NO_ERROR)
                        {
                            logService.WriteLog("DB", $"轴卡初始化失败 ErrorCode = {retCode}", MessageDegree.ERROR);
                        }
                        else
                        {
                            ExistCardNoList[currExistCardsCount] = curruCardNo;
                            InitSuccess = true;
                        }
                    }
                    if (InitSuccess == true)
                    {
                        CardNo = ExistCardNoList[0];
                        logService.WriteLog("DB", "初始化卡成功！", MessageDegree.INFO);
                    }

                    //step 3根据卡号得出从站数量，进行校验从站数量，方法暂时不提供此功能

                    return InitSuccess;
                }
            }
            catch (Exception ex)
            {
                logService.WriteLog("DB", $"初始化运动控制卡异常:{ex.Message}", MessageDegree.WARN);
                return false;
            }
        }

        /// <summary>
        /// 启动回原点
        /// </summary>
        /// <param name="homingAxNo">轴号</param>
        /// <param name="homeMethod">回原方法</param>
        /// <param name="offset">回原后零点偏执(将当前原点位置设置成此值,即编码器值为非0)</param>
        /// <param name="highVel">回原搜索高速度</param>
        /// <param name="lowVel">回原搜索低速度</param>
        /// <param name="acc">加速度</param>
        /// <param name="overtime">超时时间</param>
        /// <param name="posSrc">仅对端子板轴回零有效,EtherCat轴无效(0表示内部计数,1表示外部编码)</param>
        /// <returns></returns>
        public bool StartHoming(short homingAxNo, short homeMethod, int offset, uint highVel, uint lowVel, uint acc, uint overtime, short posSrc)
        {
            try
            {
                ushort retCode = 0;//方法调用后的返回值
                ushort axisNo = (ushort)homingAxNo; //轴号
                ushort homeMode = (ushort)homeMethod;//回原方式
                Dec = acc;//回原时候的加减速，回原停止的需要使用

                retCode = DeltaMcApi.CS_ECAT_Slave_Home_Config(CardNo, axisNo, SlotId, homeMode
                    , offset, lowVel, highVel, acc);
                if (retCode != DeltaMcErr.ERR_ECAT_NO_ERROR)
                {
                    logService.WriteLog("DB", "回零参数配置失败, ErrorCode = " + retCode.ToString(), MessageDegree.ERROR);
                    return false;
                }
                else
                {
                    retCode = DeltaMcApi.CS_ECAT_Slave_Home_Move(CardNo, axisNo, SlotId);
                    if (retCode != DeltaMcErr.ERR_ECAT_NO_ERROR)
                    {
                        logService.WriteLog("DB", "启动回零失败, ErrorCode = " + retCode.ToString(), MessageDegree.ERROR);
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                logService.WriteLog("DB", $"启动回原点异常:{ex.Message}", MessageDegree.WARN);
                return false;
            }
        }


        /// <summary>
        /// 停止回原
        /// </summary>
        /// <param name="homingAxNo"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public bool StopHoming(short homingAxNo)
        {
            try
            {
                ushort retCode = 0;//方法调用后的返回值
                ushort axisNo = (ushort)homingAxNo; //轴号 

                retCode = DeltaMcApi.CS_ECAT_Slave_Motion_Sd_Stop(CardNo, axisNo, SlotId, Dec);

                if (retCode != DeltaMcErr.ERR_ECAT_NO_ERROR)
                {
                    logService.WriteLog("DB", "停止回原失败, ErrorCode = " + retCode.ToString(), MessageDegree.ERROR);
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                logService.WriteLog("DB", $"停止回原异常:{ex.Message}", MessageDegree.WARN);
                return false;
            }
        }

        /// <summary>
        /// 结束回原 结束回原和停止回原调用的Api方法一致，书写只是为了适应统一的接口
        /// </summary>
        /// <param name="homingAxNo"></param>
        public bool FinishHoming(short homingAxNo)
        {
            try
            {
                ushort retCode = 0;//方法调用后的返回值
                ushort axisNo = (ushort)homingAxNo; //轴号 

                retCode = DeltaMcApi.CS_ECAT_Slave_Motion_Sd_Stop(CardNo, axisNo, SlotId, Dec);

                if (retCode != DeltaMcErr.ERR_ECAT_NO_ERROR)
                {
                    logService.WriteLog("DB", "结束回原失败, ErrorCode = " + retCode.ToString(), MessageDegree.ERROR);
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                logService.WriteLog("DB", $"结束回原异常:{ex.Message}", MessageDegree.WARN);
                return false;
            }
        }


        /// <summary>
        /// 获取回原状态
        /// </summary>
        /// <param name="homingAxNo"></param>
        /// <returns></returns>
        public bool GetHomingSts(short homingAxNo)
        {
            try
            {
                ushort retCode = 0;
                ushort axisHomingState = 4;
                ushort axisNo = (ushort)homingAxNo;
                retCode = DeltaMcApi.CS_ECAT_Slave_Home_Status(CardNo, axisNo, SlotId, ref axisHomingState);
                if (axisHomingState == 0)
                {
                    return true;
                }
                else 
                {
                    logService.WriteLog("DB", $"轴{homingAxNo}回零失败,回零状态为:{axisHomingState:x8}", MessageDegree.ERROR);
                    return false;
                }
            }
            catch (Exception ex)
            {
                logService.WriteLog("DB", $"轴{homingAxNo}获取回零状态异常:{ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 关闭板卡不复位
        /// </summary>
        public bool Close()
        {
            try
            {
                if (ExistCards > 0)
                {
                    ushort retCode = 0;//方法调用后的返回值
                    retCode = DeltaMcApi.CS_ECAT_Master_Close();
                    if (retCode == 0)
                    {
                        InitSuccess = false;
                        return true;
                    }
                    logService.WriteLog("DB", $"关闭卡失败,错误码为:{retCode}", MessageDegree.ERROR);
                    return false;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                logService.WriteLog("DB", $"关闭板卡异常:{ex.Message}", MessageDegree.WARN);
                return false;
            }
        }

        /// <summary>
        /// 关闭并复位板卡
        /// </summary>
        public bool CloseWithReset()
        {
            try
            {
                if (ExistCards > 0)
                {
                    for (int nSeq = 0; nSeq < 32; nSeq++)
                    {
                        if (ExistCardNoList[nSeq] == 99) continue;
                        DeltaMcApi.CS_ECAT_Master_Reset(ExistCardNoList[nSeq]);
                    }
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                logService.WriteLog("DB", $"关闭卡异常(复位):{ex.Message}", MessageDegree.WARN);
                return false;
            }
        }

        /// <summary>
        /// Jog移动开始  PV定速运动控制
        /// </summary>
        /// <param name="axNo"></param>
        /// <param name="tgVel"></param>
        /// <param name="dir"></param>
        public bool JogMoveStart(short axNo, double tgVel, Direction dir, double acc, double dec, double dsParaLTDMC = double.NaN)
        {
            try
            {
                if (ExistCards == 0)
                {
                    logService.WriteLog("DB", "运动控制卡未初始化", MessageDegree.ERROR);
                    return false;
                }
                double vel = 0;
                ushort retRtn = 0; //方法调用后的返回值
                ushort axisNo = (ushort)axNo; //轴号  
                switch (dir)
                {
                    case Direction.Positive:
                        vel = Math.Abs(tgVel);
                        break;
                    case Direction.Negative:
                        vel = Math.Abs(tgVel) * (-1);
                        break;
                    default:
                        vel = Math.Abs(tgVel);
                        break;
                }
                retRtn = DeltaMcApi.CS_ECAT_Slave_PV_Start_Move(CardNo, axisNo,SlotId, (int)vel, (uint)(acc), (uint)dec);
                if (retRtn != DeltaMcErr.ERR_ECAT_NO_ERROR)
                {
                    logService.WriteLog("DB", $"轴{axNo}启动Jog失败,错误码为:{retRtn:x8}", MessageDegree.ERROR);
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                logService.WriteLog("DB", $"启动Jog运动异常:{ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Jog移动停止 PV定速运动控制停止
        /// </summary>
        /// <param name="axNo"></param>
        public bool JogMoveStop(short axNo)
        {
            try
            {
                ushort retCode = 0;//方法调用后的返回值
                ushort axisNo = (ushort)axNo; //轴号 

                retCode = DeltaMcApi.CS_ECAT_Slave_Motion_Sd_Stop(CardNo, axisNo, SlotId, Dec);

                if (retCode != DeltaMcErr.ERR_ECAT_NO_ERROR)
                {
                    logService.WriteLog("DB", $"停止Jog错误,错误码为:{retCode:x8}", MessageDegree.ERROR);
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                logService.WriteLog("DB", $"停止Jog异常:{ex.Message}", ex);
                return false;
            }
        }


        /// <summary>
        /// 轴使能
        /// </summary>
        /// <param name="axNo"></param>
        /// <param name="enable"></param>
        /// <returns></returns>
        public bool AxisServo(short axNo, bool enable)
        {
            try
            {
                ushort retCode = 0;//方法调用后的返回值
                ushort AxisNo = (ushort)axNo;
                switch (enable)
                {
                    case true:
                        retCode = DeltaMcApi.CS_ECAT_Slave_Motion_Set_Svon(CardNo, AxisNo, SlotId,1);
                        if (retCode != DeltaMcErr.ERR_ECAT_NO_ERROR)
                        {
                            logService.WriteLog("DB", $"轴:{axNo}上使能错误,错误码为:0x{retCode:x8}", MessageDegree.ERROR);
                            return false;
                        }
                        break;
                    case false:
                        retCode = DeltaMcApi.CS_ECAT_Slave_Motion_Set_Svon(CardNo, AxisNo, SlotId, 0);
                        if (retCode != DeltaMcErr.ERR_ECAT_NO_ERROR)
                        {
                            logService.WriteLog("DB", $"轴:{axNo}下使能错误,错误码为:0x{retCode:x8}", MessageDegree.ERROR);
                            return false;
                        }
                        break;
                    default:
                        return false;
                }
                //使能后延迟150待使能 生效.(松下100ms测试未通过)依照伺服时序来决定Sleep时间
                Thread.Sleep(150);
                return true;
            }
            catch (Exception ex)
            {
                logService.WriteLog("DB", $"轴{axNo}使能异常:{ex.Message}", ex);
                return false;
            }
        }


        /// <summary>
        /// 清除轴报警
        /// </summary>
        /// <param name="axNo"></param>
        /// <returns></returns>
        public bool ClearAxSts(short axNo)
        {
            try
            {
                ushort retRtn = 0; //方法调用后的返回值
                ushort NodeId = (ushort)axNo; //轴号
                retRtn = DeltaMcApi.CS_ECAT_Slave_Motion_Ralm(CardNo, NodeId, SlotId);
                if (retRtn != DeltaMcErr.ERR_ECAT_NO_ERROR)
                {
                    logService.WriteLog("DB", $"清除轴{axNo} 报警出错,错误码为:{retRtn:x8}", MessageDegree.ERROR);
                    return false;
                }
                Thread.Sleep(200);
                return true;
            }
            catch (Exception ex)
            {
                logService.WriteLog("DB", $"清除轴{axNo}错误异常:{ex.Message}", ex);
                return false;
            }
        }


        /// <summary>
        /// 急停
        /// </summary>
        /// <returns></returns>
        public bool EmgStop()
        {
            try
            {
                //retRtn = DeltaMcApi.CS_ECAT_Slave_Motion_Emg_Stop(CardNo, 2);
                //if (retRtn != ImcApi.EXE_SUCCESS)
                //{
                //    logService.WriteLog("DB", $"设定急停模式错误,错误码为:{retRtn:x8}", MessageDegree.ERROR);
                //}
                //retRtn = ImcApi.IMC_SetEmgTrigLevelInv(cardHandle, 0);
                //if (retRtn != ImcApi.EXE_SUCCESS)
                //{
                //    logService.WriteLog("DB", $"设定急停错误,错误码为:{retRtn:x8}", MessageDegree.ERROR);
                //    return false;
                //}
                return true;
            }
            catch (Exception ex)
            {
                logService.WriteLog("DB", $"设定急停异常.", ex);
                return false;
            }
        }

        public bool EmgStopCancel()
        {
            throw new NotImplementedException();
        }



        public double[] GetAxEncAcc(short axNo, short count)
        {
            throw new NotImplementedException();
        }

        public double[] GetAxEncPos(short axNo, short count)
        {
            throw new NotImplementedException();
        }

        public double[] GetAxEncVel(short axNo, short count)
        {
            throw new NotImplementedException();
        }

        public short? GetAxErrorCode(short axNo)
        {
            throw new NotImplementedException();
        }

        public double[] GetAxPrfAcc(short axNo, short count)
        {
            throw new NotImplementedException();
        }

        public short[] GetAxPrfMode(short axNo, short count)
        {
            throw new NotImplementedException();
        }

        public double[] GetAxPrfPos(short axNo, short count)
        {
            throw new NotImplementedException();
        }

        public double[] GetAxPrfVel(short axNo, short count)
        {
            throw new NotImplementedException();
        }

        public int[] GetAxSts(short axNo, short count)
        {
            throw new NotImplementedException();
        }

        public bool? GetEcatDiBit(short diNo)
        {
            throw new NotImplementedException();
        }

        public bool? GetEcatDoBit(short doNo)
        {
            throw new NotImplementedException();
        }

        public short? GetEcatGrpDi(short groupNo)
        {
            throw new NotImplementedException();
        }

        public short? GetEcatGrpDo(short groupNo)
        {
            throw new NotImplementedException();
        }

      

        public bool PauseMove(short axNo)
        {
            throw new NotImplementedException();
        }

        public bool ResumeMove(short axNo)
        {
            throw new NotImplementedException();
        }

        public bool SetEcatDoBit(short doNo, bool value)
        {
            throw new NotImplementedException();
        }

        public bool SetEcatGrpDo(short groupNo, short value)
        {
            throw new NotImplementedException();
        }



        public bool StartMoveAbs(short axNo, short velType, double ratio, double vel, double acc, double dec, double tgtPos, double dsParaLTDMC = double.NaN)
        {
            throw new NotImplementedException();
        }

        public bool StartMoveRel(short axNo, short velType, double ratio, double vel, double acc, double dec, double tgtPos, double dsParaLTDMC = double.NaN)
        {
            throw new NotImplementedException();
        }



        public bool StopMove(short axNo, short stopType)
        {
            throw new NotImplementedException();
        }

    }
}
