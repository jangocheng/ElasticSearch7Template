using ElasticSearch7Template.Core;
using ElasticSearch7Template.Entity;
using ElasticSearch7Template.Utility;
using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ElasticSearch7Template.DAL
{
    internal abstract partial class AbstractRepository<TEntity, TCondition> where TEntity : class where TCondition : ESBaseCondition
    {
        public abstract SearchRequest<T> TrunConditionToSql<T>(TCondition condition);

        /// <summary>
        /// 查看请求/响应日志
        /// </summary>
        /// <param name="searchResponse"></param>
        protected void GetDebugInfo(IResponse searchResponse)
        {
            if (ElasticSearchConfig.IsOpenDebugger.HasValue)
            {
                if (ElasticSearchConfig.IsOpenDebugger.Value)
                {
                    string requestStr = searchResponse.ApiCall.RequestBodyInBytes == null ? "" : Encoding.Default.GetString(searchResponse.ApiCall.RequestBodyInBytes);
                    string responseStr = searchResponse.ApiCall.ResponseBodyInBytes == null ? "" : Encoding.Default.GetString(searchResponse.ApiCall.ResponseBodyInBytes);
                }
            }
        }

        /// <summary>
        /// 根据id查询单条数据
        /// </summary>
        /// <param name="id"></param>
        /// <param name="routing"></param>
        /// <returns></returns>
        public async Task<T> GetAsync<T, TPrimaryKeyType>(TPrimaryKeyType id, string routing = "") where T : class
        {
            SearchRequest<T> searchRequest = new SearchRequest<T>();
            if (!string.IsNullOrEmpty(routing))
            {
                searchRequest.Routing = new Routing(routing);
            }
            var termQuery = new TermQuery { Field = PocoData.PrimaryKey, Value = id.ToString() };
            var query = new ConstantScoreQuery() { Filter = termQuery };
            searchRequest.Query = query;
            var response = await client.SearchAsync<T>(searchRequest).ConfigureAwait(false);
            GetDebugInfo(response);
            if (response.IsValid)
            {
                if (response.Documents.Any())
                {
                    return response.Documents.First();
                }
            }
            return default(T);
        }


        /// <summary>
        /// 根据id查询单条数据(知道具体索引)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TPrimaryKeyType"></typeparam>
        /// <param name="id"></param>
        /// <param name="indexName"></param>
        /// <param name="routing"></param>
        /// <returns></returns>
        public async Task<T> GetAsync<T, TPrimaryKeyType>(QueryModel<TPrimaryKeyType> queryModel) where T : class
        {
            string indexName = queryModel.IndexName?.ToLower();
            if (!string.IsNullOrEmpty(indexName))
            {
                GetDescriptor<T> descriptor = new GetDescriptor<T>(indexName, queryModel.Id.ToString()); ;
                if (!string.IsNullOrEmpty(queryModel.Routing))
                {
                    descriptor.Routing(queryModel.Routing);
                }
                var response = await client.GetAsync<T>(descriptor).ConfigureAwait(false);

                if (response.Found)
                {
                    return response.Source;
                }
                return default(T);
            }
            else
            {
                //不知道indexName 另外一种方式查询
                return await GetAsync<T, TPrimaryKeyType>(queryModel.Id, queryModel.Routing);
            }
        }

        #region  SQL QUERY

        public async Task<List<T>> SimpleSQLQueryAsync<T>(SimpleSQLQueryModel queryModel) where T : class, new()
        {
            var result = await client.Sql.QueryAsync(t => t.FetchSize(queryModel.FetchSize).Format(queryModel.Format.ToString()).Query(queryModel.Sql)).ConfigureAwait(false);
            var rows = result.Rows.ToList();
            var colunms = result.Columns.ToList();
            List<T> entityList = new List<T>();
            GetDebugInfo(result);
            foreach (var row in rows)
            {
                T entity = new T();
                int index = 0;
                foreach (var column in colunms)
                {
                    if (!column.Name.Contains("."))
                    {

                        var val = row[index];
                        var emitSetter = EmitHelper.EmitSetter<T>(column.Name);
                        if (column.Type == "text" || column.Type == "keyword")
                        {
                            emitSetter(entity, val.As<string>());
                        }
                        else if (column.Type == "datetime")
                        {
                            emitSetter(entity, val.As<DateTime>());
                        }
                        else if (column.Type == "long")
                        {
                            emitSetter(entity, val.As<long>());
                        }
                        else if (column.Type == "integer")
                        {
                            emitSetter(entity, val.As<int>());
                        }
                        else if (column.Type == "float")
                        {
                            emitSetter(entity, val.As<float>());
                        }
                        else if (column.Type == "double")
                        {
                            emitSetter(entity, val.As<double>());
                        }
                        else if (column.Type == "boolean")
                        {
                            emitSetter(entity, val.As<bool>());
                        }
                    }
                    index++;
                }
                entityList.Add(entity);
            }
            return entityList;
        }
        #endregion




        #region  分页查询
        public async Task<QueryPagedResponseModel<T>> GetListPagedAsync<T>(int page, int size, TCondition condition, string field = null, string orderBy = null) where T : class, new()
        {
            var searchRequest = TrunConditionToSql<T>(condition);
            int fromToSearch = (page - 1) * size;
            searchRequest.From = fromToSearch;
            searchRequest.Size = size;
            if (!string.IsNullOrEmpty(condition.KeyRouting))
            {
                searchRequest.Routing = new Routing(condition.KeyRouting);
            }
            if (!string.IsNullOrEmpty(orderBy))
            {
                searchRequest.Sort = GetSortList(orderBy).ToArray();
            }
            if (!string.IsNullOrEmpty(field))
            {
                Field[] fields = GetSourceField(field).ToArray();
                searchRequest.Source = new SourceFilter() { Includes = fields };
            }
            var searchResponse = await client.SearchAsync<T>(searchRequest);
            GetDebugInfo(searchResponse);
            var entities = searchResponse.Documents;
            List<T> list = new List<T>();
            if (!searchResponse.TimedOut)
            {
                foreach (var entity in entities)
                {
                    list.Add(entity);
                }
            }
            var isSuccess = !searchResponse.TimedOut;
            var total = searchResponse.Total;
            return new QueryPagedResponseModel<T> { Data = list, Total = total, IsSuccess = isSuccess };
        }
        #region
        /// <summary>
        /// 获取排序规则  orderBy 模式   a desc,b asc
        /// </summary>
        /// <param name="orderBy"></param>
        /// <returns></returns>
        protected List<FieldSort> GetSortList(string orderBy)
        {
            var sorts = GetSortInfoArray(orderBy);
            List<FieldSort> sortFieldList = new List<FieldSort>();
            foreach (var singleOrderInfo in sorts)
            {
                string[] singleField = GetSingleSortFieldAndOrderTypeArray(singleOrderInfo);
                string field = singleField[0];
                string orderType = singleField[1];
                SortOrder sortOrder = SortOrder.Descending;
                if (orderType.ToLower() == "asc")
                {
                    sortOrder = SortOrder.Ascending;
                }
                FieldSort sortField = new FieldSort() { Field = field, Order = sortOrder };
                sortFieldList.Add(sortField);
            }
            return sortFieldList;
        }

        private string[] GetSortInfoArray(string orderBy)
        {
            string[] sorts = orderBy.Split(',');
            return sorts;
        }

        private string[] GetSingleSortFieldAndOrderTypeArray(string singleOrderInfo)
        {
            string[] singleFieldOrderInfo = singleOrderInfo.Trim().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return singleFieldOrderInfo;
        }

        protected IEnumerable<Field> GetSourceField(string needField)
        {

            var fields = needField.Trim().Split(',');
            foreach (var field in fields)
            {
                if (!string.IsNullOrEmpty(field))
                {
                    yield return new Field(field); ;
                }
            }
        }

        #endregion

        #endregion

    }
}
