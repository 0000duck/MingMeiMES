﻿using System;
using System.Data;
using System.Collections.Generic;
using DBAccess.Model;
namespace DBAccess.BLL
{
    /// <summary>
    /// BatteryModuleModel
    /// </summary>
    public partial class BatteryModuleBll
    {
        private readonly DBAccess.DAL.BatteryModuleDal dal = new DBAccess.DAL.BatteryModuleDal();
        public BatteryModuleBll()
        { }
        #region  BasicMethod
        /// <summary>
        /// 是否存在该记录
        /// </summary>
        public bool Exists(string batModuleID)
        {
            return dal.Exists(batModuleID);
        }

        /// <summary>
        /// 增加一条数据
        /// </summary>
        public bool Add(DBAccess.Model.BatteryModuleModel model)
        {
            return dal.Add(model);
        }

        /// <summary>
        /// 更新一条数据
        /// </summary>
        public bool Update(DBAccess.Model.BatteryModuleModel model)
        {
            return dal.Update(model);
        }

        /// <summary>
        /// 删除一条数据
        /// </summary>
        public bool Delete(string batModuleID)
        {

            return dal.Delete(batModuleID);
        }
        /// <summary>
        /// 删除一条数据
        /// </summary>
        public bool DeleteList(string batModuleIDlist)
        {
            return dal.DeleteList(batModuleIDlist);
        }

        /// <summary>
        /// 得到一个对象实体
        /// </summary>
        public DBAccess.Model.BatteryModuleModel GetModel(string batModuleID)
        {

            return dal.GetModel(batModuleID);
        }

        /// <summary>
        /// 获得数据列表
        /// </summary>
        public DataSet GetList(string strWhere)
        {
            return dal.GetList(strWhere);
        }
        /// <summary>
        /// 获得前几行数据
        /// </summary>
        public DataSet GetList(int Top, string strWhere, string filedOrder)
        {
            return dal.GetList(Top, strWhere, filedOrder);
        }
        /// <summary>
        /// 获得数据列表
        /// </summary>
        public List<DBAccess.Model.BatteryModuleModel> GetModelList(string strWhere)
        {
            DataSet ds = dal.GetList(0,strWhere,"asmTime");
            return DataTableToList(ds.Tables[0]);
        }
        /// <summary>
        /// 获得数据列表
        /// </summary>
        public List<DBAccess.Model.BatteryModuleModel> DataTableToList(DataTable dt)
        {
            List<DBAccess.Model.BatteryModuleModel> modelList = new List<DBAccess.Model.BatteryModuleModel>();
            int rowsCount = dt.Rows.Count;
            if (rowsCount > 0)
            {
                DBAccess.Model.BatteryModuleModel model;
                for (int n = 0; n < rowsCount; n++)
                {
                    model = dal.DataRowToModel(dt.Rows[n]);
                    if (model != null)
                    {
                        modelList.Add(model);
                    }
                }
            }
            return modelList;
        }

        /// <summary>
        /// 获得数据列表
        /// </summary>
        public DataSet GetAllList()
        {
            return GetList("");
        }

        /// <summary>
        /// 分页获取数据列表
        /// </summary>
        public int GetRecordCount(string strWhere)
        {
            return dal.GetRecordCount(strWhere);
        }
        /// <summary>
        /// 分页获取数据列表
        /// </summary>
        public DataSet GetListByPage(string strWhere, string orderby, int startIndex, int endIndex)
        {
            return dal.GetListByPage(strWhere, orderby, startIndex, endIndex);
        }
        /// <summary>
        /// 分页获取数据列表
        /// </summary>
        //public DataSet GetList(int PageSize,int PageIndex,string strWhere)
        //{
        //return dal.GetList(PageSize,PageIndex,strWhere);
        //}

        #endregion  BasicMethod
        #region  ExtensionMethod
        public List<string> GetBatchList()
        {
            DataSet ds = dal.GetBatchList();
            DataTable dt = ds.Tables[0];
            int rowsCount = dt.Rows.Count;
            List<string> batchList = new List<string>();
            if (rowsCount > 0)
            {
               
                for (int n = 0; n < rowsCount; n++)
                {
                    string batch = dt.Rows[n]["batchName"].ToString();
                    if(!string.IsNullOrWhiteSpace(batch))
                    {
                        batchList.Add(batch);
                    }
                }
            }
            batchList.Sort();
            return batchList;
        }

        public DBAccess.Model.BatteryModuleModel GetModelByPalletIDAndTag2(string palletID,string tag2,string curProcessStage)
        {
            string strWhere = "palletID = '" + palletID + "' and tag2 = '" + tag2 + "' and curProcessStage = '" + curProcessStage + "'";
            List<DBAccess.Model.BatteryModuleModel> modelList = GetModelList(strWhere);
            if(modelList == null || modelList.Count == 0)
            {
                return null;
            }
            return modelList[modelList.Count-1];
        }

        public List<DBAccess.Model.BatteryModuleModel> GetModelByPalletID(string palletID,string curProcessStage)
        {
            string strWhere = "palletID = '" + palletID + "' and curProcessStage = '" + curProcessStage + "'";
            List<DBAccess.Model.BatteryModuleModel> modelList = GetModelList(strWhere);
            if (modelList == null || modelList.Count == 0)
            {
                return null;
            }
            return modelList;
        }
        public List<DBAccess.Model.BatteryModuleModel> GetBindedMods(string palletID)
        {
            string strWhere = string.Format("palletID='{0}' and palletBinded=1", palletID);
            return GetModelList(strWhere);
        }
        #endregion  ExtensionMethod
    }
}

