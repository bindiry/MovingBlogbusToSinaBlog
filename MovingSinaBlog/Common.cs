using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace MovingSinaBlog
{
	class Common
	{

		// 处理dataset数据
		public static DataSet handleDataSet(DataSet ds)
		{
			DataTable dt = ds.Tables["Log"];
			DataRow[] rows = dt.Select("Status='0'");
			for (int i = 0; i < rows.Length; i++)
			{
				dt.Rows.Remove(rows[i]);
			}
			return ds;
		}
	}
}
