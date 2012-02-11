using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Xml;
using System.Threading;
using System.Diagnostics;

namespace MovingSinaBlog
{
    public partial class Main : Form
    {
		// 主页地址
		private string indexUrl = "http://login.sina.com.cn/";
		// 发表新日志地址
		private string addArticleUrl = "http://control.blog.sina.com.cn/admin/article/article_add.php?index";
		// LoginDoneUrl
        private string LoginDoneUrl = "http://login.sina.com.cn/member/my.php?entry=sso";
		// 设置每次发表完日志的等待时间（秒）
		private int intTotalCounter = 65;
		// xml file stream
		private Stream streamXmlFile;
        // 是否载入完成
        private Boolean isComplete = false;
		// dataset
		private DataSet ds;
		// 待导入日志总数
		private int intLogCount = 0;
		// 当前待导入的日志序号
		private int intCurrentLogNum = 0;
		// 是否检查当前页是否是发表新日志页开关
		private bool isCheckAddUrl = true;
		// 待添加日志内容设置完毕
		private bool isCompleteForAddLog = false;

		// 线程
		//private Thread thread;
		private System.Timers.Timer timer;
		private System.Timers.Timer timer2;
		private int intCurrentCounter = 0;

		private delegate HtmlDocument delGetDoc();

        public Main()
        {
            InitializeComponent();
        }

        private void Main_Load(object sender, EventArgs e)
        {
			// 转向主页
			webBrowser1.Navigate(this.indexUrl);
			// 为浏览器添加监听事件
			this.webBrowser1.DocumentCompleted += new WebBrowserDocumentCompletedEventHandler(webBrowser_DocumentCompleted);
			// 为登录按钮添加单击事件
			btnLogin.Click += new EventHandler(btnLogin_Click);
        }

		// 登录单击事件
		void btnLogin_Click(object sender, EventArgs e)
		{
			if (txtUserName.Text != null && txtUserPwd != null)
			{
				// 设置登录用户名
                HtmlElement loginNameInput = webBrowser1.Document.GetElementById("username");
				loginNameInput.InnerText = txtUserName.Text;
				// 设置密码
                HtmlElement loginPwdInput = webBrowser1.Document.GetElementById("password");
				loginPwdInput.InnerText = txtUserPwd.Text;
				// 提交表单
                //HtmlElement loginSubmin = webBrowser1.Document.get("smb_btn").GetElementsByTagName("input")[0];
                //loginSubmin.InvokeMember("click");

                HtmlElementCollection submit = webBrowser1.Document.All;
                foreach (HtmlElement element in submit)
                {
                    if (element.GetAttribute("type") == "submit")
                    {
                        element.InvokeMember("click");
                    }
                }
			}
		}

        // 网页载入完成事件
        void webBrowser_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
			
            if (webBrowser1.ReadyState == WebBrowserReadyState.Complete)
            {
				if (webBrowser1.Url.ToString() == indexUrl)
				{
					// 设置读取完成
					isComplete = true;
					btnLogin.Enabled = (this.streamXmlFile != null) ? true : false;
				}

				// 如果当前页为登录成功页
				if (webBrowser1.Url.ToString() == LoginDoneUrl)
				{
					// 登录成功，获取发表日志链接
					webBrowser1.Navigate(addArticleUrl);
				}

				// 如果当前页为发表新日志页
				if (webBrowser1.Url.ToString() == addArticleUrl && isCheckAddUrl)
				{
					// 设置控件状态
					txtUserName.Enabled = false;
					txtUserPwd.Enabled = false;
					btnLogin.Enabled = false;
					btnStartMoving.Enabled = true;
					pictureBox2.Visible = true;
					btnSelectFile.Enabled = false;
					isCheckAddUrl = false;
				}

				// 日志添加完成
				if (webBrowser1.Url.ToString() == addArticleUrl && isCompleteForAddLog)
				{
					//删除行
					ds.Tables["Log"].Rows.RemoveAt(0);
					dataGridView1.Update();
					// 设置导入状态
					labImported.Text = (int.Parse(labImported.Text) + 1).ToString();
					labImporting.Text = (int.Parse(labImporting.Text) - 1).ToString();

					// 重置内容准备完成状态
					isCompleteForAddLog = false;
					// 设置dataset表中的行数索引+1
					intCurrentLogNum += 1;
					// 设置本载入完成事件中不检查当前页是否是发表新日志页
					isCheckAddUrl = false;
					// 转向发表新日志页
					webBrowser1.Navigate(addArticleUrl);
					// 解决"从不是创建控件的线程访问它"
					Control.CheckForIllegalCrossThreadCalls = false;
					if (int.Parse(labImporting.Text) == 0)
					{
						MessageBox.Show("搬家完成！");
						this.Close();
					}
					else
					{
						// 等待
						timer = new System.Timers.Timer(intTotalCounter * 1000);
						timer.AutoReset = false;
						timer.Enabled = true;
						timer.Elapsed += new System.Timers.ElapsedEventHandler(timer_Elapsed);
						timer.Start();

						timer2 = new System.Timers.Timer(1000);
						timer2.AutoReset = true;
						timer2.Enabled = true;
						timer2.Elapsed += new System.Timers.ElapsedEventHandler(timer2_Elapsed);
						timer2.Start();
						intCurrentCounter = 1;
					}
				}

            }


        }

		void timer2_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			if ((intTotalCounter - intCurrentCounter) > 0)
			{
				labOneMinute.Text = (intTotalCounter - intCurrentCounter).ToString();
				intCurrentCounter += 1;
			}
			else
				timer2.Stop();
		}

		void timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			// 继续搬家
			startMoving();
			timer.Stop();
		}

		// 选择文件按钮单击事件
		private void btnSelectFile_Click(object sender, EventArgs e)
		{
			this.openFileDialog1.RestoreDirectory = true;
			if (openFileDialog1.ShowDialog() == DialogResult.OK)
			{
				if ((streamXmlFile = openFileDialog1.OpenFile()) != null)
				{
					// 设置控件状态
					pictureBox1.Visible = true;
					txtUserName.Enabled = true;
					txtUserPwd.Enabled = true;
					btnLogin.Enabled = this.isComplete ? true : false;

					// 读取xml文件并显示
					ds = new DataSet("blogbus");
					ds.ReadXml(streamXmlFile);
					ds = Common.handleDataSet(ds);
					dataGridView1.DefaultCellStyle.Font = new Font("Tahoma", 9);
					dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
					dataGridView1.DataSource = ds.DefaultViewManager;
					dataGridView1.DataMember = "Log";

					// 设置待导入数目
					intLogCount = ds.Tables["Log"].Rows.Count;
					labImporting.Text = intLogCount.ToString();

					// 隐藏一些没用的字段
					dataGridView1.Columns["Status"].Visible = false;
					dataGridView1.Columns["AllowComment"].Visible = false;
					//dataGridView1.Columns["AllowPing"].Visible = false;
					//dataGridView1.Columns["AllowLinks"].Visible = false;
					dataGridView1.Columns["Writer"].Visible = false;
					dataGridView1.Columns["Sort"].Visible = false;
					dataGridView1.Columns["Excerpt"].Visible = false;
					dataGridView1.Columns["Content"].Visible = false;
					dataGridView1.Columns["Tags"].Visible = false;
					dataGridView1.ColumnHeadersVisible = false;

					streamXmlFile.Close();
				}
			}
			
		}

		// 开始搬家按钮单击事件
		private void btnStartMoving_Click(object sender, EventArgs e)
		{
			pictureBox3.Visible = true;
			btnStartMoving.Text = "搬家中...";
			btnStartMoving.Enabled = false;
			startMoving();
		}

		// ======================================
		// 开始搬家
		// ======================================

		// 声明添加日志页的控件
		private HtmlElement heCategory;
		private string heCategoryValue;

		void startMoving()
		{
			DataTable dt = ds.Tables["Log"];
			// 获取控件
			getHtmlDoc().GetElementById("articleTitle").InnerText = dt.Rows[0]["Title"].ToString();
			getHtmlDoc().GetElementById("SinaEditor_59_viewcodecheckbox").InvokeMember("click");
			getHtmlDoc().All["SinaEditorTextarea"].InnerText = dt.Rows[0]["Content"].ToString() + "<br />" + dt.Rows[0]["LogDate"].ToString();
			getHtmlDoc().GetElementById("SinaEditor_59_viewcodecheckbox").InvokeMember("click");
			heCategory = getHtmlDoc().GetElementById("componentSelect");
			foreach (HtmlElement heCategoryOption in heCategory.GetElementsByTagName("option"))
			{
				if (heCategoryOption.InnerText == dt.Rows[0]["Sort"].ToString())
					heCategoryValue = heCategoryOption.GetAttribute("value");
			}
			heCategory.SetAttribute("value", heCategoryValue);
			if (dt.Rows[0]["Tags"].ToString().Trim() != "")
				getHtmlDoc().GetElementById("articleTagInput").InnerText = dt.Rows[0]["Tags"].ToString(); ;
			//getHtmlDoc().GetElementById("input1").InvokeMember("click");
			//getHtmlDoc().GetElementById("input2").InvokeMember("click");
			getHtmlDoc().GetElementById("articlePostBtn").InvokeMember("click");
			isCompleteForAddLog = true;
		}

		private HtmlDocument getHtmlDoc()
		{
			HtmlDocument hd;
			if (webBrowser1.InvokeRequired)
			{
				delGetDoc handler = new delGetDoc(getHtmlDoc);
				hd = (HtmlDocument)this.Invoke(handler);
			}
			else
			{
				hd = webBrowser1.Document;
			}
			return hd;
		}

		// 单击了junnan.org
		private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			//ProcessStartInfo sInfo = new ProcessStartInfo(e.Link.LinkData.ToString());
			Process.Start("http://junnan.org");
        }


    }
}
