using SpinCoaterAndDeveloper.Shared.Models.InterpolationModels;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpinCoaterAndDeveloper.Shared.DataEntity
{
    [SugarTable("interpolation_path_edit")]
    public class InterpolationPathEditEntity
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int Id { get; set; }

        public int CoordinateId { get; set; }

        [SugarColumn(ColumnDescription = "顺序")]
        public int Sequence { get; set; }

        [SugarColumn(IsNullable = true)]
        public InterpolationPathMode PathMode { get; set; }

        [SugarColumn(DefaultValue = "0.000", Length = 18, DecimalDigits = 3)]
        public double MX { get; set; }

        [SugarColumn(DefaultValue = "0.000", Length = 18, DecimalDigits = 3)]
        public double MY { get; set; }

        [SugarColumn(DefaultValue = "0.000", Length = 18, DecimalDigits = 3)]
        public double MZ { get; set; }

        [SugarColumn(DefaultValue = "0.000", Length = 18, DecimalDigits = 3)]
        public double TX { get; set; }

        [SugarColumn(DefaultValue = "0.000", Length = 18, DecimalDigits = 3)]
        public double TY { get; set; }

        [SugarColumn(DefaultValue = "0.000", Length = 18, DecimalDigits = 3)]
        public double TZ { get; set; }

        [SugarColumn(DefaultValue = "0.000", Length = 18, DecimalDigits = 3)]
        public double TR { get; set; }

        [SugarColumn(DefaultValue = "0.000", Length = 18, DecimalDigits = 3)]
        public double TA { get; set; }

        [SugarColumn(DefaultValue = "0.000", Length = 18, DecimalDigits = 3)]
        public double Speed { get; set; }

        [SugarColumn(DefaultValue = "0.000", Length = 18, DecimalDigits = 3)]
        public double AccSpeed { get; set; }
        public bool IOEnable { get; set; }

        public bool StartDelayIOEnable { get; set; }

        [SugarColumn(DefaultValue = "0.000", Length = 18, DecimalDigits = 3)]
        public double StartDelayTime { get; set; }

        public bool EndDelayIOEnable { get; set; }

        [SugarColumn(DefaultValue = "0.000", Length = 18, DecimalDigits = 3)]
        public double EndDelayTime { get; set; }
    }
}
