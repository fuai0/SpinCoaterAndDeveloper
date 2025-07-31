using AutoMapper;
using DataBaseServiceInterface;
using MotionCardServiceInterface;
using MotionControlActuation;
using Prism.Ioc;
using SpinCoaterAndDeveloper.Shared;
using SpinCoaterAndDeveloper.Shared.DataEntity;
using SpinCoaterAndDeveloper.Shared.Models.MotionControlModels;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpinCoaterAndDeveloper.Shared.Services.MotionResourceInitService
{
    public class MotionResourceInit : IMotionResourceInit
    {
        private readonly IDataBaseService dataBaseService;
        private readonly IMapper mapper;
        private readonly IMotionCardService motionCardService;
        public MotionResourceInit(IContainerProvider containerProvider)
        {
            this.dataBaseService = containerProvider.Resolve<IDataBaseService>("MainDb");
            this.mapper = containerProvider.Resolve<IMapper>();
            this.motionCardService = containerProvider.Resolve<IMotionCardService>();
        }
        /// <summary>
        /// 初始化轴资源
        /// </summary>
        public void InitAxisResourceDicCollection()
        {
            if (MotionControlResource.AxisResource.Count() != 0)
                MotionControlResource.AxisResource.Clear();
            var axes = dataBaseService.Db.Queryable<AxisInfoEntity>().OrderBy(x => x.Number).OrderBy(x => x.Id).ToList();
            mapper.Map(axes, MotionControlResource.AxisResource);
            axes.ForEach(x => { motionCardService.SetAxisSoftLimit((short)x.AxisIdOnCard, x.SoftLimitEnable, (int)(x.SoftPositiveLimitPos * x.Proportion), (int)(x.SoftNegativeLimitPos * x.Proportion)); });
        }
        /// <summary>
        /// 初始化输入IO资源
        /// </summary>
        public void InitIOInputResourceDicCollection()
        {
            if (MotionControlResource.IOInputResource.Count() != 0)
                MotionControlResource.IOInputResource.Clear();
            var inputs = dataBaseService.Db.Queryable<IOInputInfoEntity>().OrderBy(x => x.Number).OrderBy(x => x.Id).ToList();
            mapper.Map(inputs, MotionControlResource.IOInputResource);
        }
        /// <summary>
        /// 初始化输出IO资源
        /// </summary>
        public void InitIOOutputResourceDicCollection()
        {
            if (MotionControlResource.IOOutputResource.Count() != 0)
                MotionControlResource.IOOutputResource.Clear();
            var outputs = dataBaseService.Db.Queryable<IOOutputInfoEntity>().OrderBy(x => x.Number).OrderBy(x => x.Id).ToList();
            mapper.Map(outputs, MotionControlResource.IOOutputResource);
        }
        /// <summary>
        /// 初始化运动参数资源
        /// </summary>
        public void InitMCParmeterDicCollection()
        {
            if (GlobalValues.MCParmeterDicCollection.Count() != 0)
                GlobalValues.MCParmeterDicCollection.Clear();
            var pars = dataBaseService.Db.Queryable<ParmeterInfoEntity>().Includes(x => x.ProductInfo).Where(x => x.ProductInfo.Select == true).OrderBy(x => x.Number).OrderBy(x => x.Id).ToList();
            mapper.Map(pars, GlobalValues.MCParmeterDicCollection);
        }
        /// <summary>
        /// 初始化全局变量MCPointCollection
        /// </summary>
        public void InitMCPointDicCollection()
        {
            //运动点位参与轴集合生成,数据库中运动点位对应了所有的轴,但全局点位集合中,仅将参与轴保存在运动点位集合中.参与轴按照在运动控制卡上的Number排序(若Number相同,按照Id),运动时按照排序的顺序进行启动
            if (GlobalValues.MCPointDicCollection.Count() != 0)
                GlobalValues.MCPointDicCollection.Clear();
            var mPoints = dataBaseService.Db.Queryable<MovementPointInfoEntity>()
                .Includes(x => x.MovementPointPositions.Where(m => m.InvolveAxis == true).OrderBy(n => n.AxisInfo.Number).OrderBy(n => n.Id).ToList(), y => y.AxisInfo)
                .Includes(x => x.MovementPointPositions.Where(m => m.InvolveAxis == true).OrderBy(n => n.AxisInfo.Number).OrderBy(n => n.Id).ToList(), y => y.JogIOInputInfo)
                .Includes(x => x.MovementPointSecurities.OrderBy(n => n.Sequence).ToList())
                .Where(x => x.ProductInfo.Select == true)
                .ToList();
            mapper.Map(mPoints, GlobalValues.MCPointDicCollection);
        }
        /// <summary>
        /// 初始化全局变量插补路径集合
        /// </summary>
        public void InitInterpolationPaths()
        {
            if (GlobalValues.InterpolationPaths.Count() != 0)
                GlobalValues.InterpolationPaths.Clear();
            var interpolationPaths = dataBaseService.Db.Queryable<InterpolationPathCoordinateEntity>()
                   .Includes(x1 => x1.AxisX)
                   .Includes(x2 => x2.AxisY)
                   .Includes(x3 => x3.AxisZ)
                   .Includes(x4 => x4.AxisR)
                   .Includes(x5 => x5.AxisA)
                   .Includes(x => x.ProductInfo)
                   .Where(x => x.ProductInfo.Select == true)
                   .OrderBy(it => it.Id)
                   .Includes(p => p.InterpolationPaths.OrderBy(s => s.Sequence).ToList())
                   .ToList();
            mapper.Map(interpolationPaths, GlobalValues.InterpolationPaths);
        }
        /// <summary>
        /// 初始化全局气缸集合
        /// </summary>
        public void InitCylinderDicCollection()
        {
            if (GlobalValues.CylinderDicCollection.Count() != 0)
                GlobalValues.CylinderDicCollection.Clear();
            var cylinders = dataBaseService.Db.Queryable<CylinderInfoEntity>()
                .Includes(x => x.SingleValveOutputInfo)
                .Includes(x => x.DualValveOriginOutputInfo)
                .Includes(x => x.DualValveMovingOutputInfo)
                .Includes(x => x.SensorOriginInputInfo)
                .Includes(x => x.SensorMovingInputInfo)
                .OrderBy(x => x.Number)
                .OrderBy(x => x.Id)
                .ToList();
            mapper.Map(cylinders, GlobalValues.CylinderDicCollection);
        }

        public void InitFunctionShieldDicCollection()
        {
            if (GlobalValues.MCFunctionShieldDicCollection.Count() != 0)
            {
                GlobalValues.MCFunctionShieldDicCollection.Clear();
            }
            var functionShields = dataBaseService.Db.Queryable<FunctionShieldEntity>()
                .Includes(x => x.ProductInfo)
                .Where(x => x.ProductInfo.Select == true)
                .OrderBy(x => x.Id)
                .ToList();
            mapper.Map(functionShields, GlobalValues.MCFunctionShieldDicCollection);
        }
    }
}
