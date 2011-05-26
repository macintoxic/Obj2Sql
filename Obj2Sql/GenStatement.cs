using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Data;
using System.Collections;

namespace Obj2Sql
{
    /// <summary>
    /// Holds the information about the object in cache
    /// </summary>
    struct ObjectInfo
    {
        public string Name;
        public string FullName;
        public string InsertQuery;
        public string UpdateQuery;
        public string DeleteQuery;
        public ParameterInfo[] ParametersInfo;
    }

    /// <summary>
    /// Holds the information about the properties (parameters) in the cache
    /// </summary>
    struct ParameterInfo
    {
        public bool IsPk;
        public string Name;
        public DbType DbType;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="isPk">Indicates if the field is primary key/identity field</param>
        /// <param name="name">Name of the member</param>
        /// <param name="dbType">The DbType of the parameter </param>
        public ParameterInfo(bool isPk, string name, DbType dbType)
        {
            IsPk = isPk;
            Name = name;
            DbType = dbType;
        }
    }

    /// <summary>
    /// Generics class that generates the sql statements from
    /// the informed object. All public properties will be used to extract fields and values    
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public static class GenStatement<T>
    {
        /// <summary>
        /// Internal dictionary that maps System types to DbTypes
        /// </summary>
        private static Dictionary<RuntimeTypeHandle, DbType> typeMap
             = new Dictionary<RuntimeTypeHandle, DbType>();


        /// <summary>
        /// Holds cache of objects in order to bypass the reflection overload.        
        /// </summary>
        private static Dictionary<string, ObjectInfo> parameterCache =
            new Dictionary<string, ObjectInfo>();


        private static string _parameterIdentifier;

        /// <summary>
        /// Holds the identifier of parameters. The default is '@'.
        /// </summary>
        public static string ParameterIdentifier
        {
            get { return _parameterIdentifier; }
            set { _parameterIdentifier = value; }
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        static GenStatement()
        {
            ParameterIdentifier = "@";

            typeMap = new Dictionary<RuntimeTypeHandle, DbType>();
            typeMap[typeof(byte).TypeHandle] = DbType.Byte;
            typeMap[typeof(sbyte).TypeHandle] = DbType.SByte;
            typeMap[typeof(short).TypeHandle] = DbType.Int16;
            typeMap[typeof(ushort).TypeHandle] = DbType.UInt16;
            typeMap[typeof(int).TypeHandle] = DbType.Int32;
            typeMap[typeof(uint).TypeHandle] = DbType.UInt32;
            typeMap[typeof(long).TypeHandle] = DbType.Int64;
            typeMap[typeof(ulong).TypeHandle] = DbType.UInt64;
            typeMap[typeof(float).TypeHandle] = DbType.Single;
            typeMap[typeof(double).TypeHandle] = DbType.Double;
            typeMap[typeof(decimal).TypeHandle] = DbType.Decimal;
            typeMap[typeof(bool).TypeHandle] = DbType.Boolean;
            typeMap[typeof(string).TypeHandle] = DbType.String;
            typeMap[typeof(char).TypeHandle] = DbType.StringFixedLength;
            typeMap[typeof(Guid).TypeHandle] = DbType.Guid;
            typeMap[typeof(DateTime).TypeHandle] = DbType.DateTime;
            typeMap[typeof(DateTimeOffset).TypeHandle] = DbType.DateTimeOffset;
            typeMap[typeof(byte[]).TypeHandle] = DbType.Binary;
            typeMap[typeof(byte?).TypeHandle] = DbType.Byte;
            typeMap[typeof(sbyte?).TypeHandle] = DbType.SByte;
            typeMap[typeof(short?).TypeHandle] = DbType.Int16;
            typeMap[typeof(ushort?).TypeHandle] = DbType.UInt16;
            typeMap[typeof(int?).TypeHandle] = DbType.Int32;
            typeMap[typeof(uint?).TypeHandle] = DbType.UInt32;
            typeMap[typeof(long?).TypeHandle] = DbType.Int64;
            typeMap[typeof(ulong?).TypeHandle] = DbType.UInt64;
            typeMap[typeof(float?).TypeHandle] = DbType.Single;
            typeMap[typeof(double?).TypeHandle] = DbType.Double;
            typeMap[typeof(decimal?).TypeHandle] = DbType.Decimal;
            typeMap[typeof(bool?).TypeHandle] = DbType.Boolean;
            typeMap[typeof(char?).TypeHandle] = DbType.StringFixedLength;
            typeMap[typeof(Guid?).TypeHandle] = DbType.Guid;
            typeMap[typeof(DateTime?).TypeHandle] = DbType.DateTime;
            typeMap[typeof(DateTimeOffset?).TypeHandle] = DbType.DateTimeOffset;
        }


        /// <summary>
        /// Looks up the equivalent DbType for the property type
        /// </summary>        
        /// <param name="type"></param>
        /// <returns></returns>
        private static DbType LookupDbType(Type type)
        {
            DbType dbType;
            if (typeMap.TryGetValue(type.TypeHandle, out dbType))
            {
                return dbType;
            }
            if (typeof(IEnumerable).IsAssignableFrom(type))
            {
                // use xml to denote its a list, hacky but will work on any DB
                return DbType.Xml;
            }
            throw new NotSupportedException(
                string.Format("The type : {0} is not supported by this generator", type)
                );
        }

        /// <summary>
        /// Removes an object from cache.
        /// </summary>
        /// <returns>true if object removed</returns>
        public static bool RemoveFromCache()
        {
            string key = typeof(T).FullName;
            bool removed = false;
            if (parameterCache.ContainsKey(key))
            {
                removed = parameterCache.Remove(key);
            }
            return removed;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="identityFields"></param>
        private static void CacheParameters(T obj, params string[] identityFields)
        {
            PropertyInfo[] properties = obj.GetType().GetProperties();
            List<ParameterInfo> parameters = new List<ParameterInfo>();
            string fullName = obj.GetType().FullName;
            string name = obj.GetType().Name;


            for (int i = 0; i < properties.Length; i++)
            {
                ParameterInfo parameterInfo = new ParameterInfo();
                parameterInfo.DbType = LookupDbType(properties[i].PropertyType);
                parameterInfo.Name = properties[i].Name;

                parameterInfo.IsPk = null != Array.Find<string>(identityFields, delegate(string s)
                {
                    return s.ToLower() == parameterInfo.Name.ToLower();
                });

                parameters.Add(parameterInfo);
            }

            ObjectInfo objectInfo = new ObjectInfo();
            objectInfo.FullName = fullName;
            objectInfo.Name = name;
            objectInfo.ParametersInfo = parameters.ToArray();

            objectInfo.InsertQuery = CreateInsertQuery(objectInfo);
            objectInfo.DeleteQuery = CreateDeleteQuery(objectInfo);
            objectInfo.UpdateQuery = CreateUpdateQuery(objectInfo);

            parameterCache.Add(obj.GetType().FullName, objectInfo);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        private static string CreateInsertQuery(ObjectInfo obj)
        {
            string query = string.Format("INSERT INTO  {0} (", obj.Name);
            string parameters = string.Empty;

            ParameterInfo[] param =
                Array.FindAll<ParameterInfo>(obj.ParametersInfo, delegate(ParameterInfo p)
                {
                    return !p.IsPk;
                });


            for (int i = 0; i < param.Length; i++)
            {
                query += param[i].Name + ", ";
                parameters += string.Format("{0}{1}, ", ParameterIdentifier, param[i].Name);
            }

            return string.Format("{0} ) values ( {1} )", query.Substring(0, query.Length - 2),
                parameters.Substring(0, parameters.Length - 2));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        private static string CreateUpdateQuery(ObjectInfo obj)
        {
            string query = string.Format("UPDATE {0} SET ", obj.Name);
            string where = string.Empty;
            Array.ForEach<ParameterInfo>(obj.ParametersInfo, delegate(ParameterInfo p)
                {
                    if (p.IsPk)
                    {
                        if (string.IsNullOrEmpty(where))
                            where += string.Format("{0} = @{0} ", p.Name);
                        else
                            where += string.Format("and {0} = @{0} ", p.Name);

                    }
                    else
                    {
                        query += string.Format("{0} = @{0}, ", p.Name);
                    }

                });

            return string.Format("{0} where {1}", query.Substring(0, query.Length - 2), where);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        private static string CreateDeleteQuery(ObjectInfo obj)
        {
            string query = string.Format("delete from {0} ", obj.Name);
            string where = string.Empty;
            Array.ForEach<ParameterInfo>(obj.ParametersInfo,
                delegate(ParameterInfo p)
                {
                    if (p.IsPk)
                    {
                        if (string.IsNullOrEmpty(where))
                        {
                            where += string.Format("{0} = @{0} ", p.Name);
                        }
                        else
                        {
                            where += string.Format("and {0} = @{0} ", p.Name);
                        }
                    }

                });

            return string.Format("{0} where {1}", query, where);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="connection"></param>
        /// <param name="identityFields"></param>
        /// <returns></returns>
        public static IDbCommand GetDeleteCommand(T obj, IDbConnection connection, 
            params string[] identityFields)
        {
            string objKey = obj.GetType().FullName;
            if (connection == null)
                throw new NullReferenceException("Connection cannot be null.");

            if (identityFields == null || identityFields.Length == 0)
                throw new NullReferenceException("At least one primary key or identity  field  must be informed.");

            if (!parameterCache.ContainsKey(objKey))
            {
                ;
                CacheParameters(obj, identityFields);
            }
            ObjectInfo objInfo = parameterCache[objKey];

            IDbCommand command = connection.CreateCommand();
            command.CommandText = objInfo.DeleteQuery;

            Array.ForEach<ParameterInfo>(objInfo.ParametersInfo,
                delegate(ParameterInfo p)
                {
                    if (p.IsPk)
                    {
                        IDbDataParameter parameter = command.CreateParameter();
                        parameter.ParameterName = ParameterIdentifier + p.Name;
                        parameter.Value = obj.GetType().GetProperty(p.Name).GetValue(obj, null);
                        parameter.DbType = p.DbType;
                        command.Parameters.Add(parameter);
                    }

                });

            return command;
        }

        public static IDbCommand GetUpdateCommand(T obj, IDbConnection connection, params string[] identityFields)
        {
            string objKey = obj.GetType().FullName;
            if (connection == null)
                throw new NullReferenceException("Connection cannot be null.");

            if (identityFields == null || identityFields.Length == 0)
                throw new NullReferenceException("At least one primary key or identity  field  must be informed.");

            if (!parameterCache.ContainsKey(objKey))
            {
                CacheParameters(obj, identityFields);
            }

            ObjectInfo objInfo = parameterCache[objKey];
            IDbCommand command = connection.CreateCommand();
            command.CommandText = objInfo.UpdateQuery;

            Array.ForEach<ParameterInfo>(objInfo.ParametersInfo,
                delegate(ParameterInfo p)
                {
                    IDbDataParameter parameter = command.CreateParameter();
                    parameter.ParameterName = ParameterIdentifier + p.Name;
                    parameter.Value = obj.GetType().GetProperty(p.Name).GetValue(obj, null);
                    parameter.DbType = p.DbType;
                    command.Parameters.Add(parameter);

                });


            return command;
        }

        /// <summary>
        /// Creates insert comand basede on properties of the Object
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="connection"></param>
        /// <param name="identityFields"></param>
        /// <returns></returns>
        public static IDbCommand GetInsertCommand(T obj, IDbConnection connection, params string[] identityFields)
        {
            string objKey = obj.GetType().FullName;

            if (connection == null)
                throw new NullReferenceException("Connection cannot be null");

            if (identityFields == null || identityFields.Length == 0)
                throw new NullReferenceException("At least one primary key or identity  field  must be informed");
            
            if (!parameterCache.ContainsKey(objKey))
            {
                CacheParameters(obj, identityFields);
            }

            ObjectInfo objInfo = parameterCache[objKey];

            IDbCommand command = connection.CreateCommand();
            command.CommandText = objInfo.InsertQuery;

            Array.ForEach<ParameterInfo>(objInfo.ParametersInfo,
                delegate(ParameterInfo p)
                {
                    if (!p.IsPk)
                    {
                        IDbDataParameter parameter = command.CreateParameter();
                        parameter.ParameterName = ParameterIdentifier + p.Name;
                        parameter.Value = obj.GetType().GetProperty(p.Name).GetValue(obj, null);
                        parameter.DbType = p.DbType;
                        command.Parameters.Add(parameter);
                    }

                });

            return command;
        }
    }
}
