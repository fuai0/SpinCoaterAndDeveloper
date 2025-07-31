using AutoMapper;
using MotionControlActuation;
using SpinCoaterAndDeveloper.App.Common.Models;
using SpinCoaterAndDeveloper.Shared.DataEntity;
using SpinCoaterAndDeveloper.Shared.Models.CylinderModels;
using SpinCoaterAndDeveloper.Shared.Models.InterpolationModels;
using SpinCoaterAndDeveloper.Shared.Models.MotionControlModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpinCoaterAndDeveloper.App.Common
{
    public class MapperProfile : Profile
    {
        public MapperProfile()
        {
            CreateMap<UserInfoEntity, UserInfoModel>().ReverseMap();
            CreateMap<LogInfoEntity, LogShowInfoModel>();
            CreateMap<ProduceInfoEntity, ProductShowInfoModel>().ReverseMap();

            CreateMap<AxisInfoEntity, AxisInfoModel>().ReverseMap();
            CreateMap<AxisInfoEntity, AxisInfo>().ReverseMap();
            CreateMap<AxisInfoEntity, KeyValuePair<string, AxisInfo>>()
                .ConstructUsing((t, c) => new KeyValuePair<string, AxisInfo>(t.Name, c.Mapper.Map<AxisInfo>(t)));
            CreateMap<AxisInfoEntity, AxisMonitorModel>()
                .ForMember(dest => dest.ShowOnUIName, opt => opt.MapFrom<AxisMonitorLanguageResolver>())
                .ReverseMap();

            CreateMap<IOInputInfoEntity, IOInputInfoModel>().ReverseMap();
            CreateMap<IOInputInfoEntity, IOInputInfo>().ReverseMap();
            CreateMap<IOInputInfoEntity, KeyValuePair<string, IOInputInfo>>()
                .ConstructUsing((t, c) => new KeyValuePair<string, IOInputInfo>(t.Name, c.Mapper.Map<IOInputInfo>(t)));
            CreateMap<IOInputInfoEntity, IOInputMonitorModel>()
                .ForMember(dest => dest.ShowOnUIName, opt => opt.MapFrom<IOInputMonitorLanguageResolver>())
                .ReverseMap();

            CreateMap<IOOutputInfoEntity, IOOutputInfoModel>().ReverseMap();
            CreateMap<IOOutputInfoEntity, IOOutputInfo>().ReverseMap();
            CreateMap<IOOutputInfoEntity, KeyValuePair<string, IOOutputInfo>>()
                .ConstructUsing((t, c) => new KeyValuePair<string, IOOutputInfo>(t.Name, c.Mapper.Map<IOOutputInfo>(t)));
            CreateMap<IOOutputInfoEntity, IOOutputMonitorModel>()
                .ForMember(dest => dest.ShowOnUIName, opt => opt.MapFrom<IOOutputMonitorLanguageResolver>())
                .ReverseMap();

            CreateMap<CylinderInfoEntity, CylinderInfoModel>().ReverseMap();
            CreateMap<CylinderInfoEntity, CylinderInfo>().ReverseMap();
            CreateMap<CylinderInfoEntity, KeyValuePair<string, CylinderInfo>>()
                .ConstructUsing((t, c) => new KeyValuePair<string, CylinderInfo>(t.Name, c.Mapper.Map<CylinderInfo>(t)));
            CreateMap<CylinderInfoEntity, CylinderMonitorModel>()
                .ForMember(dest => dest.ShowOnUIName, opt => opt.MapFrom<CylinderMonitorLanguageResolver>())
                .ReverseMap();

            CreateMap<ParmeterInfoEntity, ParmeterInfoModel>().ReverseMap();
            CreateMap<ParmeterInfoEntity, ParmeterInfo>().ReverseMap();
            CreateMap<ParmeterInfoEntity, KeyValuePair<string, ParmeterInfo>>()
                .ConstructUsing((t, c) => new KeyValuePair<string, ParmeterInfo>(t.Name, c.Mapper.Map<ParmeterInfo>(t)));
            CreateMap<ParmeterInfoEntity, ParmeterMonitorModel>()
                .ForMember(dest => dest.ShowOnUIName, opt => opt.MapFrom<ParmeterMonitorLanguageResolver>())
                .ForMember(dest => dest.ErrorMark, opt => opt.MapFrom<ParmeterMonitorErrorMarkResolver>())
                .ReverseMap();

            CreateMap<MovementPointPositionEntity, MovementPointPositionModel>().ReverseMap();
            CreateMap<MovementPointSecurityEntity, MovementPointSecurityModel>().ReverseMap();
            CreateMap<MovementPointInfoEntity, MovementPointInfoModel>()
                .ForMember(dest => dest.MovementPointPositionsCollection, opt => opt.MapFrom(src => src.MovementPointPositions))
                .ForMember(dest => dest.MovementPointSecuritiesCollection, opt => opt.MapFrom(src => src.MovementPointSecurities))
                .ReverseMap();
            CreateMap<MovementPointInfoEntity, MovementPointInfo>().ReverseMap();
            CreateMap<MovementPointPositionEntity, MovementPointPosition>().ReverseMap();
            CreateMap<MovementPointSecurityEntity, MovementPointSercurity>().ReverseMap();
            CreateMap<MovementPointInfoEntity, KeyValuePair<string, MovementPointInfo>>()
                .ConstructUsing((t, c) => new KeyValuePair<string, MovementPointInfo>(t.Name, c.Mapper.Map<MovementPointInfo>(t)));
            CreateMap<MovementPointPositionEntity, MovementPointPositionMonitorModel>().ReverseMap();
            CreateMap<MovementPointSecurityEntity, MovementPointSecurityMonitorModel>().ReverseMap();
            CreateMap<MovementPointInfoEntity, MovementPointMonitorModel>()
                .ForMember(dest => dest.MovementPointPositionsMonitorCollection, opt => opt.MapFrom(src => src.MovementPointPositions))
                .ForMember(dest => dest.MovementPointSecuritiesMonitorCollection, opt => opt.MapFrom(src => src.MovementPointSecurities))
                .ForMember(dest => dest.ShowOnUIName, opt => opt.MapFrom<MovementPointMonitorLanguageResolver>())
                .ReverseMap();

            CreateMap<InterpolationPathCoordinateEntity, InterpolationPathCoordinateModel>().ReverseMap();
            CreateMap<InterpolationPathEditEntity, InterpolationPathEditModel>().ReverseMap();
            CreateMap<InterpolationPathCoordinateEntity, KeyValuePair<string, InterpolationPathCoordinateEntity>>()
                .ConstructUsing(t => new KeyValuePair<string, InterpolationPathCoordinateEntity>(t.PathName, t));

            CreateMap<FunctionShieldEntity, FunctionShieldInfoModel>().ReverseMap();
            CreateMap<FunctionShieldEntity, FunctionShieldInfo>().ReverseMap();
            CreateMap<FunctionShieldEntity, KeyValuePair<string, FunctionShieldInfo>>()
                .ConstructUsing((t, c) => new KeyValuePair<string, FunctionShieldInfo>(t.Name, c.Mapper.Map<FunctionShieldInfo>(t)));
            CreateMap<FunctionShieldEntity, FunctionShieldMonitorModel>()
                .ForMember(dest => dest.ShowOnUIName, opt => opt.MapFrom<FuncionShieldMonitorLanguageResolver>())
                .ReverseMap();
        }
    }

    public class AxisMonitorLanguageResolver : IValueResolver<AxisInfoEntity, AxisMonitorModel, string>
    {
        public string Resolve(AxisInfoEntity source, AxisMonitorModel destination, string destMember, ResolutionContext context)
        {
            switch (System.Threading.Thread.CurrentThread.CurrentUICulture.Name)
            {
                case "zh-CN":
                    return !string.IsNullOrWhiteSpace(source.CNName) ? source.CNName : source.Name;
                case "en-US":
                    return !string.IsNullOrWhiteSpace(source.ENName) ? source.ENName : source.Name;
                case "vi-VN":
                    return !string.IsNullOrWhiteSpace(source.VNName) ? source.VNName : source.Name;
                default:
                    return source.Name;
            }
        }
    }

    public class IOInputMonitorLanguageResolver : IValueResolver<IOInputInfoEntity, IOInputMonitorModel, string>
    {
        public string Resolve(IOInputInfoEntity source, IOInputMonitorModel destination, string destMember, ResolutionContext context)
        {
            switch (System.Threading.Thread.CurrentThread.CurrentUICulture.Name)
            {
                case "zh-CN":
                    return !string.IsNullOrWhiteSpace(source.CNName) ? source.CNName : source.Name;
                case "en-US":
                    return !string.IsNullOrWhiteSpace(source.ENName) ? source.ENName : source.Name;
                case "vi-VN":
                    return !string.IsNullOrWhiteSpace(source.VNName) ? source.VNName : source.Name;
                default:
                    return source.Name;
            }
        }
    }

    public class IOOutputMonitorLanguageResolver : IValueResolver<IOOutputInfoEntity, IOOutputMonitorModel, string>
    {
        public string Resolve(IOOutputInfoEntity source, IOOutputMonitorModel destination, string destMember, ResolutionContext context)
        {
            switch (System.Threading.Thread.CurrentThread.CurrentUICulture.Name)
            {
                case "zh-CN":
                    return !string.IsNullOrWhiteSpace(source.CNName) ? source.CNName : source.Name;
                case "en-US":
                    return !string.IsNullOrWhiteSpace(source.ENName) ? source.ENName : source.Name;
                case "vi-VN":
                    return !string.IsNullOrWhiteSpace(source.VNName) ? source.VNName : source.Name;
                default:
                    return source.Name;
            }
        }
    }

    public class CylinderMonitorLanguageResolver : IValueResolver<CylinderInfoEntity, CylinderMonitorModel, string>
    {
        public string Resolve(CylinderInfoEntity source, CylinderMonitorModel destination, string destMember, ResolutionContext context)
        {
            switch (System.Threading.Thread.CurrentThread.CurrentUICulture.Name)
            {
                case "zh-CN":
                    return !string.IsNullOrWhiteSpace(source.CNName) ? source.CNName : source.Name;
                case "en-US":
                    return !string.IsNullOrWhiteSpace(source.ENName) ? source.ENName : source.Name;
                case "vi-VN":
                    return !string.IsNullOrWhiteSpace(source.VNName) ? source.VNName : source.Name;
                default:
                    return source.Name;
            }
        }
    }

    public class ParmeterMonitorLanguageResolver : IValueResolver<ParmeterInfoEntity, ParmeterMonitorModel, string>
    {
        public string Resolve(ParmeterInfoEntity source, ParmeterMonitorModel destination, string destMember, ResolutionContext context)
        {
            switch (System.Threading.Thread.CurrentThread.CurrentUICulture.Name)
            {
                case "zh-CN":
                    return !string.IsNullOrWhiteSpace(source.CNName) ? source.CNName : source.Name;
                case "en-US":
                    return !string.IsNullOrWhiteSpace(source.ENName) ? source.ENName : source.Name;
                case "vi-VN":
                    return !string.IsNullOrWhiteSpace(source.VNName) ? source.VNName : source.Name;
                default:
                    return source.Name;
            }
        }
    }

    public class ParmeterMonitorErrorMarkResolver : IValueResolver<ParmeterInfoEntity, ParmeterMonitorModel, bool>
    {
        public bool Resolve(ParmeterInfoEntity source, ParmeterMonitorModel destination, bool destMember, ResolutionContext context)
        {
            switch (source.DataType)
            {
                case ParmeterType.String:
                    return true;
                case ParmeterType.Bool:
                    return bool.TryParse(source.Data, out _);
                case ParmeterType.Byte:
                    return byte.TryParse(source.Data, out _);
                case ParmeterType.Short:
                    return short.TryParse(source.Data, out _);
                case ParmeterType.Int:
                    return int.TryParse(source.Data, out _);
                case ParmeterType.Double:
                    return double.TryParse(source.Data, out _);
                case ParmeterType.Float:
                    return float.TryParse(source.Data, out _);
                case ParmeterType.DateTime:
                    return DateTime.TryParse(source.Data, out _);
                default:
                    return true;
            }
        }
    }

    public class MovementPointMonitorLanguageResolver : IValueResolver<MovementPointInfoEntity, MovementPointMonitorModel, string>
    {
        public string Resolve(MovementPointInfoEntity source, MovementPointMonitorModel destination, string destMember, ResolutionContext context)
        {
            switch (System.Threading.Thread.CurrentThread.CurrentUICulture.Name)
            {
                case "zh-CN":
                    return !string.IsNullOrWhiteSpace(source.CNName) ? source.CNName : source.Name;
                case "en-US":
                    return !string.IsNullOrWhiteSpace(source.ENName) ? source.ENName : source.Name;
                case "vi-VN":
                    return !string.IsNullOrWhiteSpace(source.VNName) ? source.VNName : source.Name;
                default:
                    return source.Name;
            }
        }
    }

    public class FuncionShieldMonitorLanguageResolver : IValueResolver<FunctionShieldEntity, FunctionShieldMonitorModel, string>
    {
        public string Resolve(FunctionShieldEntity source, FunctionShieldMonitorModel destination, string destMember, ResolutionContext context)
        {
            switch (System.Threading.Thread.CurrentThread.CurrentUICulture.Name)
            {
                case "zh-CN":
                    return !string.IsNullOrWhiteSpace(source.CNName) ? source.CNName : source.Name;
                case "en-US":
                    return !string.IsNullOrWhiteSpace(source.ENName) ? source.ENName : source.Name;
                case "vi-VN":
                    return !string.IsNullOrWhiteSpace(source.VNName) ? source.VNName : source.Name;
                default:
                    return source.Name;
            }
        }
    }
}
