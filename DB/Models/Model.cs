﻿namespace HSA.RehaGame.DB.Models
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using Mono.Data.Sqlite;

    public abstract class Model : IModel
    {
        protected IList<PropertyInfo> columns;

        private PropertyInfo primaryKey;
        private IDatabase database;
        private Type type;
        private bool isInstance;

        public Model()
        {
            this.database = Database.Instance();
            this.primaryKey = GetFieldOfType(typeof(PrimaryKey));
            this.type = this.GetType();
            this.columns = GetColumns(type);
            this.isInstance = false;
        }

        public bool IsInstance
        {
            get
            {
                return isInstance;
            }
        }

        public object PrimaryKeyValue
        {
            get
            {
                return primaryKey.GetGetMethod().Invoke(this, null);
            }
        }

        private IList<PropertyInfo> GetColumns(Type model)
        {
            IList<PropertyInfo> columns = new List<PropertyInfo>();
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            foreach (PropertyInfo property in this.GetType().GetProperties(flags))
            {
                var attr = property.GetCustomAttributes(typeof(TableColumn), true);

                if (attr.Length == 1)
                    columns.Add(property);
            }

            if (columns.Count == 0)
                throw new Exception("No columns found");

            return columns;
        }

        private PropertyInfo GetFieldOfType(Type type)
        {
            foreach (PropertyInfo property in this.GetType().GetProperties())
            {
                var attr = property.GetCustomAttributes(type, true);

                if (attr.Length == 1)
                {
                    return property;
                }
            }

            if(type.Equals(typeof(PrimaryKey)))
                throw new Exception(string.Format("No Field of type ({0}) found", type.Name));

            return null;
        }

        private IList<object> GetValues()
        {
            IList<object> values = new List<object>();

            foreach (var column in columns)
            {
                if (column == primaryKey)
                    continue;

                var get = column.GetGetMethod(true);
                values.Add(get.Invoke(this, null));
            }

            return values;
        }

        private string GetFieldColumnName(PropertyInfo field)
        {
            var attr = field.GetCustomAttributes(typeof(TableColumn), true);

            if(attr.Length == 1)
            {
                var attribute = (TableColumn)attr[0];

                if (attribute.NameInTable == null)
                    return field.Name.ToLower();

                return attribute.NameInTable;
            }

            throw new Exception(string.Format("Field ({0}) is not a table column", field.Name));
        }

        private object GetFieldValue(PropertyInfo field)
        {
            var get = field.GetGetMethod(true);
            return get.Invoke(this, null);
        }

        public SQLiteErrorCode Save()
        {
            if (primaryKey == null)
                throw new Exception(string.Format("Primary key '{0}' not set", primaryKey.Name));

            foreach (var column in this.columns)
            {
                var attr = column.GetCustomAttributes(typeof(TableColumn), true);

                if (attr.Length == 1)
                {
                    try
                    {
                        SQLiteErrorCode errorCode;
                        var get = column.GetGetMethod(true);
                        var value = get.Invoke(this, null);
                        var attribute = ((TableColumn)attr[0]);


                        if (attribute.NotNull && value == null)
                            throw new Exception(string.Format("Column '{0}' can not be null", column.Name));

                        if (this.isInstance)
                            errorCode = database.UpdateTable(attribute, type, column, value, primaryKey.Name, GetFieldValue(primaryKey));
                        else
                            errorCode = database.Save(type, GetFieldValue(primaryKey), GetValues());

                        if(errorCode == SQLiteErrorCode.Ok)
                            this.isInstance = true;
                        else
                            return errorCode;
                    }

                    catch (Exception e)
                    {
                        throw e;
                    }
                }
            }

            return SQLiteErrorCode.Ok;
        }

        private object ManyToManyQuery(TableColumn attribute, PropertyInfo column)
        {
            var manyToMany = ((ManyToManyRelation)attribute);
            var genericDict = typeof(Dictionary<,>);
            var genericArgs = column.PropertyType.GetGenericArguments();

            var keyType = genericArgs[0];
            var valType = genericArgs[1];

            var values = database.Join(manyToMany, GetFieldValue(primaryKey));
            var dict = genericDict.MakeGenericType(genericArgs);
            var dictInstance = Activator.CreateInstance(dict) as IDictionary;

            for (int i = 0; i < values.Length; i++)
            {
                var value = values[i];

                if (value.ToString() != "" && value != null)
                {
                    var model = Activator.CreateInstance(valType, value) as Model;

                    if (model != null)
                    {
                        model.SetData();
                        dictInstance.Add(model.PrimaryKeyValue, model);
                    }
                }
            }

            return dictInstance;
        }

        private object ForeignKeyQuery(TableColumn attribute, PropertyInfo column)
        {
            var value = database.Get(attribute, column, type, primaryKey.Name, GetFieldValue(primaryKey))[0];

            if (value.ToString() != "" && value != null)
            {
                var model = Activator.CreateInstance(column.PropertyType, value) as IModel;

                if (model != null)
                    value = model;
            }
            else
                value = null;

            return value;
        }

        protected IDictionary<PropertyInfo, object> GetData()
        {
            IDictionary<PropertyInfo, object> data = new Dictionary<PropertyInfo, object>();

            foreach (var column in this.columns)
            {
                var attr = column.GetCustomAttributes(typeof(TableColumn), true);
                var attribute = ((TableColumn)attr[0]);

                if (attr.Length == 1)
                {
                    object value;

                    if (attribute.GetType() == typeof(TranslationColumn))
                        ((TranslationColumn)attribute).SetColumn(column.Name, "de_de"); //ToDo: Sprache aus Settings dynamisch setzen

                    if (attribute.GetType() == typeof(ManyToManyRelation))
                        value = ManyToManyQuery(attribute, column);

                    else if (attribute.GetType() == typeof(ForeignKey))
                        value = ForeignKeyQuery(attribute, column);

                    else
                        value = database.Get(attribute, column, type, primaryKey.Name, PrimaryKeyValue)[0];

                    data.Add(column, value);
                }
            }

            return data;
        }

        public virtual void SetData()
        {
            try
            {
                if (primaryKey == null)
                    throw new Exception("Primary key not set");

                var data = GetData();

                foreach(var d in data)
                    d.Key.GetSetMethod(true).Invoke(this, new object[] { d.Value });

                this.isInstance = true;
            }

            catch (Exception e)
            {
                throw e;
            }
        }

        public override string ToString()
        {
            string values = string.Format("{0}: {1} ({2})\n", primaryKey.Name, primaryKey.GetGetMethod(true).Invoke(this, null), primaryKey.PropertyType.Name);

            foreach(var column in columns)
            {
                var value = column.GetGetMethod(true).Invoke(this, null);

                if(value != null)
                    values += string.Format("{0}: {1} ({2})\n", column.Name, value, column.PropertyType.Name);
            }

            return values;
        }

        private static object[] GetPrimaryKeys(Type type)
        {
            var columns = type.GetProperties();

            foreach (var column in columns)
            {
                var attr = column.GetCustomAttributes(typeof(PrimaryKey), true);

                if (attr.Length == 1)
                {
                    return Database.Instance().All(column.Name, type.Name);
                }
            }

            throw new Exception(string.Format("No primary key found in model of type {0}", type.Name));
        }

        public static IDictionary<object, T> All<T>() where T : Model
        {
            Type type = typeof(T);
            IDictionary<object, T> models = new Dictionary<object, T>();

            var primaryKeys = GetPrimaryKeys(typeof(T));

            foreach(var primaryKey in primaryKeys)
            {
                var model = GetModel<T>(primaryKey);

                model.SetData();
                models.Add(model.PrimaryKeyValue, model);
            }

            return models;
        }

        public static T GetModel<T>(object primaryKey) where T : Model
        {
            IModel model = Activator.CreateInstance(typeof(T), primaryKey) as IModel;

            if (model != null)
                model.SetData();

            return model as T;
        }
    }
}
