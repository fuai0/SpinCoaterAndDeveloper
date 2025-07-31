using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpinCoaterAndDeveloper.App.Common.Models
{
    public class ProductShowInfoModel : BindableBase
    {
        private long id;

        public long Id
        {
            get { return id; }
            set { SetProperty(ref id, value); }
        }
        private string productCode;

        public string ProductCode
        {
            get { return productCode; }
            set { SetProperty(ref productCode, value); }
        }

        private bool productResult;

        public bool ProductResult
        {
            get { return productResult; }
            set { SetProperty(ref productResult, value); }
        }

        private DateTime productStartTime;

        public DateTime ProductStartTime
        {
            get { return productStartTime; }
            set { SetProperty(ref productStartTime, value); }
        }
        private DateTime prodcutEndTime;

        public DateTime ProductEndTime
        {
            get { return prodcutEndTime; }
            set { SetProperty(ref prodcutEndTime, value); }
        }
        private double productCT;

        public double ProductCT
        {
            get { return productCT; }
            set { SetProperty(ref productCT, value); }
        }
    }
}
