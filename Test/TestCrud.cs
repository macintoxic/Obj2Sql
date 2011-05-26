using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework.Constraints;

using System.Data.SqlClient;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Diagnostics;
using Obj2Sql;


namespace NUnit.Framework.Tests
{
    
    public class TestOtherTypes
    {
        private IList<string> _lString;

        public IList<string> LString
        {
            get { return _lString; }
            set { _lString = value; }
        }


        private int _id;

        public int Id
        {
            get { return _id; }
            set { _id = value; }
        }

    }
    
    public class TestOtherTypes2
    {
        private IList<string> _lString;
        
        public IList<string> LString
        {
            get { return _lString; }
            set { _lString = value; }
        }


        private int _id;
        
        public int Id
        {
            get { return _id; }
            set { _id = value; }
        }


        
        private TestOtherTypes myVar;
        
        public TestOtherTypes MyProperty
        {
            get { return myVar; }
            set { myVar = value; }
        }

    }



    [TestFixture(Category = "Reflection crud")]
    public class TestCrud : AssertionHelper
    {
        SQLiteConnection conn = null;
        SampleObj sample = new SampleObj();

        [Test]
        public void TestGetInsert()
        {
            int prop = sample.GetType().GetProperties().Length;

            sample.Name = "inserted";
            sample.Id = 2;
            sample.Dec = 999;

            string[] identity_fields = new string[] { "id" };
            IDbCommand command = GenStatement<SampleObj>.GetInsertCommand(sample, conn, identity_fields);
            Assert.NotNull(command, command.CommandText);
            Assert.AreEqual(prop - identity_fields.Length, command.Parameters.Count, "Number of SqlParameters is wrong.");
            Assert.AreEqual(1, command.ExecuteNonQuery(), "Record not inserted");
        }


        [Test]
        public void TestGetUpdate()
        {
            string[] identity_fields = new string[] { "id" };
            int prop = sample.GetType().GetProperties().Length;

            sample.Name = "updated";
            sample.Id = 33;
            sample.Dec = 666;

            IDbCommand command = GenStatement<SampleObj>.GetInsertCommand(sample, conn, identity_fields);
            command.ExecuteNonQuery();

            command.Parameters.Clear();
            command.CommandText = "select last_insert_rowid()";
            sample.Id = Convert.ToInt32(command.ExecuteScalar() ?? 0);


            Console.WriteLine(string.Format("insert record {0} for deletion", sample.Id));
            command = GenStatement<SampleObj>.GetUpdateCommand(sample, conn, identity_fields);
            Assert.NotNull(command);
            Assert.NotNull(command, command.CommandText);
            Assert.AreEqual(prop, command.Parameters.Count, "Number of SqlParameters is wrong.");
            Assert.AreEqual(1, command.ExecuteNonQuery(), "Record not updated");

            GenStatement<SampleObj>.RemoveFromCache();
            identity_fields = new string[] { "id", "name" };

            Console.WriteLine(string.Format("insert record {0} for deletion", sample.Id));
            command = GenStatement<SampleObj>.GetUpdateCommand(sample, conn, identity_fields);
            Assert.NotNull(command);
            Assert.NotNull(command, command.CommandText);
            Assert.AreEqual(prop, command.Parameters.Count, "Number of SqlParameters is wrong.");
            Assert.AreEqual(1, command.ExecuteNonQuery(), "Record not updated");
        }

        [Test]
        public void TestGetDelete()
        {
            string[] identity_fields = new string[] { "id" };

            //Debugger.Launch();
            IDbCommand command = GenStatement<SampleObj>.GetInsertCommand(sample, conn, identity_fields);
            Console.WriteLine(command.CommandText);
            Console.WriteLine(command.Parameters.Count);
            command.ExecuteNonQuery();

            command.Parameters.Clear();
            command.CommandText = "select last_insert_rowid()";
            sample.Id = Convert.ToInt32(command.ExecuteScalar() ?? 0);
            Console.WriteLine(string.Format("insert record {0} for update", sample.Id));

            command = GenStatement<SampleObj>.GetDeleteCommand(sample, conn, identity_fields);
            Console.WriteLine(command.Parameters.Count);
            Assert.NotNull(command);
            Assert.NotNull(command, command.CommandText);
            Assert.AreEqual(identity_fields.Length, command.Parameters.Count, "Number of SqlParameters is wrong.");
            Assert.AreEqual(1, command.ExecuteNonQuery(), "Record not deleted");
        }

        [Test]
        public void TestNullConnection()
        {
            string[] identity_fields = new string[] { "id" };
            Assert.Catch((delegate()
            {
                IDbCommand command = GenStatement<SampleObj>.GetDeleteCommand(sample, null, null);
            }));


            Assert.Catch((delegate()
           {
               IDbCommand command = GenStatement<SampleObj>.GetInsertCommand(sample, null, null);
           }));


            Assert.Catch((delegate()
           {
               IDbCommand command = GenStatement<SampleObj>.GetUpdateCommand(sample, null, null);
           }));
        }


        [Test]
        public void TestNullIdentifiers()
        {
            string[] identity_fields = new string[] { "id" };
            Assert.Catch((delegate()
            {
                IDbCommand command = GenStatement<SampleObj>.GetDeleteCommand(sample, conn, null);
            }));


            Assert.Catch((delegate()
            {
                IDbCommand command = GenStatement<SampleObj>.GetUpdateCommand(sample, conn, null);
            }));

            Assert.Catch((delegate()
            {
                IDbCommand command = GenStatement<SampleObj>.GetInsertCommand(sample, conn, null);
            }));


        }


        [Test]
        public void TestRemoveFromCache()
        {
            string[] identity_fields = new string[] { "id" };
            Assert.AreEqual(true, GenStatement<SampleObj>.RemoveFromCache());
            GenStatement<SampleObj>.GetInsertCommand(sample, conn, identity_fields);

            Assert.AreEqual(true, GenStatement<SampleObj>.RemoveFromCache());
            GenStatement<SampleObj>.GetDeleteCommand(sample, conn, identity_fields);


            Assert.AreEqual(true, GenStatement<SampleObj>.RemoveFromCache());
            GenStatement<SampleObj>.GetUpdateCommand(sample, conn, identity_fields);

            Assert.AreEqual(true, GenStatement<SampleObj>.RemoveFromCache());
            Assert.AreEqual(false, GenStatement<SampleObj>.RemoveFromCache());
        }



        [Test]
        public void TestOtherTypes()
        {
            string[] identity_fields = new string[] { "id" };
            TestOtherTypes tot = new TestOtherTypes();
            tot.LString = new List<string>();
            tot.LString.Add("teste");
            tot.Id = 999;
            identity_fields[0] = tot.LString[0];
            GenStatement<TestOtherTypes>.GetInsertCommand(tot, conn, identity_fields);


            TestOtherTypes2 tot2 = new TestOtherTypes2();
            tot2.LString = new List<string>();
            tot2.LString.Add("teste");
            tot2.Id = 999;
            tot2.MyProperty = new TestOtherTypes();
            tot2.MyProperty.Id = tot2.Id;
            identity_fields[0] = tot.LString[0];
            Assert.Catch((delegate()
            {
                GenStatement<TestOtherTypes2>.GetInsertCommand(tot2, conn, identity_fields);
            }));
        }



        [SetUp]
        public void SetupTest()
        {
            //conn = new SqlConnection("Data Source=esaomdbdev01\\brzsqld1;Initial Catalog=MLIBO; User Id=mlibo_user; Pwd=Mlibopwd01;Integrated security=true;");



            Console.WriteLine(string.Format("sqlite db: {0}\\{1}", Path.GetTempPath(), "Test.db3"));
            conn = new SQLiteConnection(string.Format("Data Source={0}Test.db3;Pooling=true;FailIfMissing=false", Path.GetTempPath()));
            conn.Open();


            sample.Name = "teste";
            sample.Id = 2;
            sample.Dec = 666;
            string sql = @"CREATE TABLE if not exists  [SampleObj] (
                [Id] INTEGER  NOT NULL PRIMARY KEY AUTOINCREMENT,
                [Name] varchar(35)  NULL,
                [Dec] decimal  NULL
                )";
            IDbCommand comm = conn.CreateCommand();
            comm.CommandText = sql;
            comm.ExecuteNonQuery();

        }


        [TearDown]
        public void TearDownTest()
        {
            //conn.Close();
            //conn.Dispose();


        }

    }
}
