using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpinCoaterAndDeveloper.Shared.DataEntity
{
    [SugarTable("interpolation_path_coordinate")]
    public class InterpolationPathCoordinateEntity
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int Id { get; set; }

        [SugarColumn(ColumnDescription = "插补路径名称")]
        public string PathName { get; set; }

        [SugarColumn(IsNullable = true)]
        public int InterpolationCoordinateID { get; set; }

        public bool EnableAxisX { get; set; }

        [SugarColumn(IsNullable = true)]
        public int AxisXId { get; set; }

        [Navigate(NavigateType.OneToOne, nameof(AxisXId))]
        public AxisInfoEntity AxisX { get; set; }

        [SugarColumn(DefaultValue = "0.000", ColumnDescription = "起点X", Length = 18, DecimalDigits = 3)]
        public double BeginningX { get; set; }

        [SugarColumn(DefaultValue = "0.000", Length = 18, DecimalDigits = 3)]
        public double BeginningXVel { get; set; }

        [SugarColumn(DefaultValue = "0.000", Length = 18, DecimalDigits = 3)]
        public double BeginningXAcc { get; set; }

        [SugarColumn(DefaultValue = "0.000", Length = 18, DecimalDigits = 3)]
        public double BeginningXDec { get; set; }

        public bool EnableAxisY { get; set; }

        [SugarColumn(IsNullable = true)]
        public int AxisYId { get; set; }

        [Navigate(NavigateType.OneToOne, nameof(AxisYId))]
        public AxisInfoEntity AxisY { get; set; }

        [SugarColumn(DefaultValue = "0.000", ColumnDescription = "起点Y", Length = 18, DecimalDigits = 3)]
        public double BeginningY { get; set; }

        [SugarColumn(DefaultValue = "0.000", Length = 18, DecimalDigits = 3)]
        public double BeginningYVel { get; set; }

        [SugarColumn(DefaultValue = "0.000", Length = 18, DecimalDigits = 3)]
        public double BeginningYAcc { get; set; }

        [SugarColumn(DefaultValue = "0.000", Length = 18, DecimalDigits = 3)]
        public double BeginningYDec { get; set; }
        public bool EnableAxisZ { get; set; }

        [SugarColumn(IsNullable = true)]
        public int AxisZId { get; set; }

        [Navigate(NavigateType.OneToOne, nameof(AxisZId))]
        public AxisInfoEntity AxisZ { get; set; }

        [SugarColumn(DefaultValue = "0.000", ColumnDescription = "起点Z", Length = 18, DecimalDigits = 3)]
        public double BeginningZ { get; set; }

        [SugarColumn(DefaultValue = "0.000", Length = 18, DecimalDigits = 3)]
        public double BeginningZVel { get; set; }

        [SugarColumn(DefaultValue = "0.000", Length = 18, DecimalDigits = 3)]
        public double BeginningZAcc { get; set; }

        [SugarColumn(DefaultValue = "0.000", Length = 18, DecimalDigits = 3)]
        public double BeginningZDec { get; set; }
        public bool EnableAxisR { get; set; }

        [SugarColumn(IsNullable = true)]
        public int AxisRId { get; set; }

        [Navigate(NavigateType.OneToOne, nameof(AxisRId))]
        public AxisInfoEntity AxisR { get; set; }

        [SugarColumn(DefaultValue = "0.000", ColumnDescription = "起点R", Length = 18, DecimalDigits = 3)]
        public double BeginningR { get; set; }

        [SugarColumn(DefaultValue = "0.000", Length = 18, DecimalDigits = 3)]
        public double BeginningRVel { get; set; }

        [SugarColumn(DefaultValue = "0.000", Length = 18, DecimalDigits = 3)]
        public double BeginningRAcc { get; set; }

        [SugarColumn(DefaultValue = "0.000", Length = 18, DecimalDigits = 3)]
        public double BeginningRDec { get; set; }
        public bool EnableAxisA { get; set; }

        [SugarColumn(IsNullable = true)]
        public int AxisAId { get; set; }

        [Navigate(NavigateType.OneToOne, nameof(AxisAId))]
        public AxisInfoEntity AxisA { get; set; }

        [SugarColumn(DefaultValue = "0.000", ColumnDescription = "起点A", Length = 18, DecimalDigits = 3)]
        public double BeginningA { get; set; }

        [SugarColumn(DefaultValue = "0.000", Length = 18, DecimalDigits = 3)]
        public double BeginningAVel { get; set; }

        [SugarColumn(DefaultValue = "0.000", Length = 18, DecimalDigits = 3)]
        public double BeginningAAcc { get; set; }

        [SugarColumn(DefaultValue = "0.000", Length = 18, DecimalDigits = 3)]
        public double BeginningADec { get; set; }

        [Navigate(NavigateType.OneToMany, nameof(InterpolationPathEditEntity.CoordinateId))]
        public List<InterpolationPathEditEntity> InterpolationPaths { get; set; }

        [SugarColumn(ColumnDescription = "product外键")]
        public int ProductId { get; set; }

        [Navigate(NavigateType.OneToOne, nameof(ProductId))]
        public ProductInfoEntity ProductInfo { get; set; }
    }
}
