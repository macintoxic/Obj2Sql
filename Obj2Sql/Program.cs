using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Data.SQLite;
using System.Data;


namespace Obj2Sql
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            SampleObj obj = new SampleObj();

            obj.Dec = 66.6M;
            obj.Name = "obj de teste";
            obj.Id = 999;

            Console.WriteLine(string.Format("sqlite db: {0}\\{1}", Path.GetTempPath(), "Test.db3"));
            SQLiteConnection conn = new SQLiteConnection(string.Format("Data Source={0}Test.db3;Pooling=true;FailIfMissing=false", Path.GetTempPath()));
            conn.Open();


            
            string sql = @"CREATE TABLE if not exists  [SampleObj] (
                [Id] INTEGER  NOT NULL PRIMARY KEY AUTOINCREMENT,
                [Name] varchar(35)  NULL,
                [Dec] decimal  NULL
                )";
            IDbCommand comm = conn.CreateCommand();
            comm.CommandText = sql;
            comm.ExecuteNonQuery();

            Debug.WriteLine(GenStatement<SampleObj>.GetDeleteCommand(obj, conn, new string[] { "id" }).CommandText);
            Debug.WriteLine(GenStatement<SampleObj>.GetInsertCommand(obj, conn, new string[] { "id" }).CommandText);
            Debug.WriteLine(GenStatement<SampleObj>.GetUpdateCommand(obj, conn, new string[] { "id" }).CommandText);
            GenStatement<SampleObj>.RemoveFromCache();
            
        }
    }
}