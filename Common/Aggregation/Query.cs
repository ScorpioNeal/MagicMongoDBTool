﻿using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MagicMongoDBTool.Module;
namespace Common.Aggregation
{
    public static class QueryHelper
    {
        /// <summary>
        ///     获得输出字段名称
        /// </summary>
        /// <returns></returns>
        public static String[] GetOutputFields(List<DataFilter.QueryFieldItem> FieldItemLst)
        {
            var outputFieldLst = new List<String>();
            foreach (DataFilter.QueryFieldItem item in FieldItemLst)
            {
                if (item.IsShow)
                {
                    outputFieldLst.Add(item.ColName);
                }
            }
            return outputFieldLst.ToArray();
        }

        /// <summary>
        ///     获得排序
        /// </summary>
        /// <returns></returns>
        public static SortByBuilder GetSort(List<DataFilter.QueryFieldItem> FieldItemLst)
        {
            var sort = new SortByBuilder();
            var ascendingList = new List<String>();
            var descendingList = new List<String>();
            //_id将以文字的形式排序，所以不要排序_id!!
            foreach (DataFilter.QueryFieldItem item in FieldItemLst)
            {
                switch (item.sortType)
                {
                    case DataFilter.SortType.NoSort:
                        break;
                    case DataFilter.SortType.Ascending:
                        ascendingList.Add(item.ColName);
                        break;
                    case DataFilter.SortType.Descending:
                        descendingList.Add(item.ColName);
                        break;
                    default:
                        break;
                }
            }
            sort.Ascending(ascendingList.ToArray());
            sort.Descending(descendingList.ToArray());
            return sort;
        }

        /// <summary>
        ///     检索过滤器
        /// </summary>
        /// <returns></returns>
        public static IMongoQuery GetQuery(List<DataFilter.QueryConditionInputItem> QueryCompareList)
        {
            //遍历所有条件，分组
            var conditiongrpList = new List<List<DataFilter.QueryConditionInputItem>>();
            List<DataFilter.QueryConditionInputItem> currGrp = null;
            for (int i = 0; i < QueryCompareList.Count; i++)
            {
                if (i == 0 || QueryCompareList[i].StartMark == "(" || QueryCompareList[i - 1].EndMark.StartsWith(")"))
                {
                    var newGroup = new List<DataFilter.QueryConditionInputItem>();
                    conditiongrpList.Add(newGroup);
                    currGrp = newGroup;
                    currGrp.Add(QueryCompareList[i]);
                }
                else
                {
                    currGrp.Add(QueryCompareList[i]);
                }
            }
            //将每个分组总结为1个IMongoQuery和1个连接符号
            IMongoQuery rtnQuery = null;
            if (conditiongrpList.Count == 1)
            {
                return GetGroupQuery(conditiongrpList[0]);
            }
            for (int i = 0; i < conditiongrpList.Count - 1; i++)
            {
                String joinMark = conditiongrpList[i][conditiongrpList[i].Count() - 1].EndMark;
                if (joinMark == MongoDbHelper.EndMark_AND_T)
                {
                    rtnQuery =
                        Query.And(i == 0
                            ? new[] {GetGroupQuery(conditiongrpList[i]), GetGroupQuery(conditiongrpList[i + 1])}
                            : new[] {rtnQuery, GetGroupQuery(conditiongrpList[i + 1])});
                }

                if (joinMark == MongoDbHelper.EndMark_OR_T)
                {
                    rtnQuery =
                        Query.Or(i == 0
                            ? new[] {GetGroupQuery(conditiongrpList[i]), GetGroupQuery(conditiongrpList[i + 1])}
                            : new[] {rtnQuery, GetGroupQuery(conditiongrpList[i + 1])});
                }
            }
            return rtnQuery;
        }

        /// <summary>
        ///     将每个分组合并为一个IMongoQuery
        /// </summary>
        /// <param name="conditionGroup"></param>
        /// <returns></returns>
        private static IMongoQuery GetGroupQuery(List<DataFilter.QueryConditionInputItem> conditionGroup)
        {
            IMongoQuery rtnQuery = Query.Or(GetQuery(conditionGroup[0]));
            for (int i = 1; i < conditionGroup.Count; i++)
            {
                if (conditionGroup[i - 1].EndMark == MongoDbHelper.EndMark_AND)
                {
                    rtnQuery = Query.And(new[] {rtnQuery, GetQuery(conditionGroup[i])});
                }
                if (conditionGroup[i - 1].EndMark == MongoDbHelper.EndMark_OR)
                {
                    rtnQuery = Query.Or(new[] {rtnQuery, GetQuery(conditionGroup[i])});
                }
            }
            return rtnQuery;
        }

        /// <summary>
        ///     将And和Or组里面的最基本条件转化为一个IMongoQuery
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private static IMongoQuery GetQuery(DataFilter.QueryConditionInputItem item)
        {
            IMongoQuery query;
            BsonValue queryvalue = item.Value.GetBsonValue();
            switch (item.Compare)
            {
                case DataFilter.CompareEnum.EQ:
                    query = Query.EQ(item.ColName, queryvalue);
                    break;
                case DataFilter.CompareEnum.GT:
                    query = Query.GT(item.ColName, queryvalue);
                    break;
                case DataFilter.CompareEnum.GTE:
                    query = Query.GTE(item.ColName, queryvalue);
                    break;
                case DataFilter.CompareEnum.LT:
                    query = Query.LT(item.ColName, queryvalue);
                    break;
                case DataFilter.CompareEnum.LTE:
                    query = Query.LTE(item.ColName, queryvalue);
                    break;
                case DataFilter.CompareEnum.NE:
                    query = Query.NE(item.ColName, queryvalue);
                    break;
                default:
                    query = Query.EQ(item.ColName, queryvalue);
                    break;
            }
            return query;
        }

        /// <summary>
        ///     Is Exist by Key
        /// </summary>
        /// <param name="mongoCol">Collection</param>
        /// <param name="KeyValue">KeyValue</param>
        /// <returns></returns>
        public static Boolean IsExistByKey(MongoCollection mongoCol, BsonValue KeyValue)
        {
            return mongoCol.FindAs<BsonDocument>(Query.EQ(MongoDbHelper.KEY_ID, KeyValue)).Count() > 0;
        }
    }
}