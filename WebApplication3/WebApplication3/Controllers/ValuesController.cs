using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace WebApplication3.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        // GET api/values
        [HttpGet]
        public ActionResult<IEnumerable<string>> Get()
        {
            //return new string[] { "value1", "value2" };
            using (DataTable table = new DataTable())
            {
                //DBHelper.FillTable("SELECT TOP 1 * FROM [test].[dbo].[testtable]", table);
                DBHelper.FillTable("SELECT * FROM [test].[dbo].[testtable]", table);
                return new string[] { DataTable2Json(table) };
            }           
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public ActionResult<string> Get(int id)
        {
            return "value";
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }

        public static string DataTable2Json(DataTable dt)
        {
            //StringBuilder jsonBuilder = new StringBuilder();
            //jsonBuilder.Append("{\"");
            //jsonBuilder.Append(dt.TableName);
            //jsonBuilder.Append("\":[");
            //jsonBuilder.Append("[");
            //for (int i = 0; i < dt.Rows.Count; i++)
            //{
            //    jsonBuilder.Append("{");
            //    for (int j = 0; j < dt.Columns.Count; j++)
            //    {
            //        jsonBuilder.Append("\"");
            //        jsonBuilder.Append(dt.Columns[j].ColumnName);
            //        jsonBuilder.Append("\":\"");
            //        jsonBuilder.Append(dt.Rows[i][j].ToString());
            //        jsonBuilder.Append("\",");
            //    }
            //    jsonBuilder.Remove(jsonBuilder.Length - 1, 1);
            //    jsonBuilder.Append("},");
            //}
            //jsonBuilder.Remove(jsonBuilder.Length - 1, 1);
            //jsonBuilder.Append("]");
            //jsonBuilder.Append("}");
            //return jsonBuilder.ToString();
            StringBuilder Json = new StringBuilder();
            Json.Append("{\"Test\":[");
            if (dt.Rows.Count > 0)
            {
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    Json.Append("{");
                    for (int j = 0; j < dt.Columns.Count; j++)
                    {
                        Json.Append("\"" + dt.Columns[j].ColumnName.ToString() + "\":\"" + dt.Rows[i][j].ToString() + "\"");
                        if (j < dt.Columns.Count - 1)
                        {
                            Json.Append(",");
                        }
                    }
                    Json.Append("}");
                    if (i < dt.Rows.Count - 1)
                    {
                        Json.Append(",");
                    }
                }
            }
            Json.Append("]}");
            return Json.ToString();
        }
    }
}
