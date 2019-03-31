using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

using Betty.utilities;

namespace Betty.UnitTest
{
    [TestClass]
    public class PermissionHelperTest
    {
        [TestMethod]
        public void StringToPermission_Admin_ReturnsCorrectPermission()
        {
            string Input = "AdMIn";

            byte Result = PermissionHelper.StringToPermission(Input);

            byte Expected = PermissionHelper.Admin;

            Assert.AreEqual(Result, Expected);
        }

        [TestMethod]
        public void StringToPermission_Owner_ReturnsCorrectPermission()
        {
            string Input = "Owner";

            byte Result = PermissionHelper.StringToPermission(Input);

            byte Expected = PermissionHelper.Owner;

            Assert.AreEqual(Result, Expected);
        }

        [TestMethod]
        public void StringToPermission_Member_ReturnsCorrectPermission()
        {
            string Input = "Member";

            byte Result = PermissionHelper.StringToPermission(Input);

            byte Expected = PermissionHelper.Member;

            Assert.AreEqual(Result, Expected);
        }

        [TestMethod]
        public void StringToPermission_Public_ReturnsCorrectPermission()
        {
            string Input = "PUBLIC";

            byte Result = PermissionHelper.StringToPermission(Input);

            byte Expected = PermissionHelper.Public;

            Assert.AreEqual(Result, Expected);
        }

        [TestMethod]
        public void StringToPermission_InvalidInput_ThrowsArgumentException()
        {
            string Input = "Superuser";

            try
            {
                byte Result = PermissionHelper.StringToPermission(Input);
            }
            catch (ArgumentException)
            {
                return;
            }

            Assert.Fail();
        }
    }
}
