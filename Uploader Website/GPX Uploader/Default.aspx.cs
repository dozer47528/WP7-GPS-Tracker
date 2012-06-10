using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace GPX_Uploader
{
    public partial class _Default : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Request.HttpMethod == "GET") return;
            Delete();
            if (string.IsNullOrEmpty(Request["content"])) return;
            var content = Request["content"];
            var paths = GetFilePath();
            FileStream fs = new FileStream(paths[0], FileMode.Create);
            StreamWriter writer = new StreamWriter(fs, Encoding.UTF8);
            writer.Write(Encoding.Default.GetString(Convert.FromBase64String(Uri.UnescapeDataString(content))));
            writer.Close();
            fs.Close();
            Response.Write(paths[1]);
        }

        private string[] GetFilePath()
        {
            var url = string.Format("~/file/{0}/{1}.gpx", DateTime.Now.ToString("yyyy-MM-dd"), Guid.NewGuid().ToString());
            var path = Server.MapPath(url);
            var dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            return new string[] { path, string.Concat("http://", Request.Url.Authority, ResolveUrl(url)) };
        }

        private void Delete()
        {
            var path = Server.MapPath("~/file");
            var dirs = Directory.GetDirectories(path);
            foreach (var dir in dirs)
            {
                if (DateTime.Now.AddDays(-7) > DateTime.Parse(Path.GetFileName(dir)))
                {
                    Directory.Delete(dir, true);
                }
            }
        }
    }
}