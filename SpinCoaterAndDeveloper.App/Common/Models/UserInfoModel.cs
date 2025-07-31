using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpinCoaterAndDeveloper.App.Common.Models
{
    public class UserInfoModel
    {
        private string userName;

        public string UserName
        {
            get { return userName; }
            set { userName = value; }
        }

        private string password;

        public string Password
        {
            get { return password; }
            set { password = value; }
        }
        private string authority;

        private string passwordSec;

        public string PasswordSec
        {
            get { return passwordSec; }
            set { passwordSec = value; }
        }
        private string passwordOld;

        public string PasswordOld
        {
            get { return passwordOld; }
            set { passwordOld = value; }
        }

        public string Authority
        {
            get { return authority; }
            set { authority = value; }
        }

    }
}
