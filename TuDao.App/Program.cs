using CsQuery;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;

namespace TuDao.App
{
    class Program
    {
        static string basePath;
        static string baseListUrl;
        static string baseItemUrl = "https://detail.tmall.com/item.htm?id=";
        static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("本程序分三步完成采集工作（每完成一部需要重启程序）：");
            Console.WriteLine("第1步：根据商户商品列表页面地址，采集商品编号");
            Console.WriteLine("第2步：根据商品编号，采集商品图片地址");
            Console.WriteLine("第3步：根据图片地址，下载图片");
            Console.WriteLine("请问您现在需要执行第几步操作：（请输入1、2或3然后按任意键开始）");
            var key = Console.ReadLine();
            if(key == "1")
            {
                Console.WriteLine("请先输入目标商户的商品列表页面地址：");
                baseListUrl = Console.ReadLine();
                getId();
                Console.WriteLine("第1步操作执行完毕，按任意键退出程序");
            }
            else if(key == "2")
            {
                Console.WriteLine("开始执行第2步操作：");
                prepareData();
                Console.WriteLine("第2步操作执行完毕，按任意键退出程序");
            }
            else if(key == "3")
            {
                Console.WriteLine("开始执行第3步操作：");
                downloadPic();
                Console.WriteLine("第3步操作执行完毕，按任意键退出程序");
            }
            Console.ReadKey();
        }

        static List<string> idlist;
        
        static void downloadPic()
        {
            DirectoryInfo basedi = new DirectoryInfo("data");
            foreach(var di in basedi.EnumerateDirectories())
            {
                var jsonstr = File.ReadAllText(Path.Combine(di.FullName, "config.txt"));
                var obj = JObject.Parse(jsonstr);
                var dic1 = JsonConvert.DeserializeObject<Dictionary<string, string>>(obj["TiTu"].ToString());
                var dic2 = JsonConvert.DeserializeObject<Dictionary<string, string>>(obj["SeTu"].ToString());
                var dic3 = JsonConvert.DeserializeObject<Dictionary<string, string>>(obj["SeTu"].ToString());
                var dic4 = JsonConvert.DeserializeObject<Dictionary<string, string>>(obj["NeiRongTu"].ToString());
                eachPic(di.FullName, dic1);
                eachPic(di.FullName, dic2);
                eachPic(di.FullName, dic3);
                eachPic(di.FullName, dic4);
            }
        }
        static void eachPic(string dicName,Dictionary<string,string> dic1)
        {
            foreach (var key in dic1.Keys)
            {
                var name = Path.Combine(dicName, key + dic1[key].Substring(dic1[key].LastIndexOf('.')));
                try
                {
                    getPic(dic1[key], name);
                    Console.WriteLine("图片下载:" + name);
                }
                catch (Exception ex)
                {
                    File.AppendAllText("err2.txt", dic1[key] + Environment.NewLine);
                }
            }
        }
        static void getPic(string url,string name)
        {
            ServicePointManager.ServerCertificateValidationCallback = ValidateServerCertificate;
            HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
            request.UseDefaultCredentials = true;
            HttpWebResponse response = request.GetResponse() as HttpWebResponse;
            Stream stream = response.GetResponseStream();
            var fileStream = new FileStream(name, FileMode.Create, FileAccess.Write);
            stream.CopyTo(fileStream);
            fileStream.Dispose();
            stream.Close();
        }
        static string getHtml(string url)
        {
            ServicePointManager.ServerCertificateValidationCallback = ValidateServerCertificate;
            HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
            request.UseDefaultCredentials = true;
            HttpWebResponse response = request.GetResponse() as HttpWebResponse;
            Stream stream = response.GetResponseStream();
            StreamReader reader = new StreamReader(stream, Encoding.Default);
            string html = reader.ReadToEnd();
            stream.Close();
            return html;
        }
        static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }
        static void getId()
        {
            idlist = new List<string>();
            CQ doc = getHtml(baseListUrl);
            var pageCount = Convert.ToInt32(doc[".ui-page-s-len"].Text().Split('/')[1]);
            var liList = doc[".item"].ToList().Take(60);
            foreach (var obj in liList)
            {
                var id = obj.GetAttribute("data-id");
                idlist.Add(id);
                Console.WriteLine("采集到id:{0}", id);
            }
            for (var i = 2; i <= pageCount; i++)
            {
                CQ doc1 = getHtml(baseListUrl + "&pageNo=" + i.ToString());
                var liList1 = doc1[".item"].ToList().Take(60);
                foreach (var obj in liList1)
                {
                    var id = obj.GetAttribute("data-id");
                    idlist.Add(id);
                    Console.WriteLine("采集到id:{0}", id);
                }
            }
            var sb = new StringBuilder();
            foreach (var id in idlist)
            {
                sb.AppendLine(id);
            }
            File.WriteAllText("ids.txt", sb.ToString());
        }
        static ShangPin getShangPin(string id)
        {
            //id = "522670612044";
            var sp = new ShangPin();
            sp.Id = id;
            var html = getHtml(baseItemUrl + id);
            var indexHH = html.IndexOf("货号");
            if(indexHH < 1)
            {
                indexHH = html.IndexOf("款号");
                if(indexHH < 1)
                {
                    indexHH = html.IndexOf("型号");
                    if (indexHH < 1)
                    {
                        File.AppendAllText("err.txt", id + Environment.NewLine);
                        return null;
                    }
                    
                }
            }
            if (html.Substring(indexHH - 7, 12).StartsWith("name"))
            {
                sp.HuoHao = html.Substring(indexHH + 13,60);
                sp.HuoHao = sp.HuoHao.Substring(0, sp.HuoHao.IndexOf('"'));
            }
            else
            {
                sp.HuoHao = html.Substring(indexHH, 60);
                sp.HuoHao = sp.HuoHao.Replace("&nbsp;", "").Substring(3);
                sp.HuoHao = sp.HuoHao.Substring(0, sp.HuoHao.IndexOf('<'));
            }
            
            sp.DetailJsonUrl = html.Substring(html.IndexOf("descUrl") + 10);
            sp.DetailJsonUrl = "https:" + sp.DetailJsonUrl.Substring(0, sp.DetailJsonUrl.IndexOf('"'));
            CQ doc = html;
            var shoutulist = doc["#J_UlThumb img"].ToList();
            var i = 1;
            foreach (var st in shoutulist)
            {
                var src = "https:" + st.GetAttribute("src");
                src = src.Substring(0, src.LastIndexOf('_'));
                sp.TiTu.Add("题图" + i, src);
                Console.WriteLine("采集到题图:{0}", src);
                i += 1;
            }
            var setuList = doc[".tb-sku .J_TSaleProp a"].ToList();
            i = 1;
            foreach (var st in setuList)
            {
                var style = st.GetAttribute("style");
                if (string.IsNullOrEmpty(style))
                {
                    continue;
                }
                style = style.Substring(style.IndexOf("(") + 1);
                style = style.Substring(0, style.IndexOf(")"));
                style = "http:" + style;
                style = style.Substring(0, style.LastIndexOf('_'));
                sp.SeTu.Add(st.InnerText.Trim() + i, style);
                Console.WriteLine("采集到颜色图:{0}", style);
                i += 1;
            }
            var neirongJsonStr = getHtml(sp.DetailJsonUrl);
            var neirongArr = Regex.Split(neirongJsonStr, @"<img\b[^<>]*?\bsrc[\s\t\r\n]*=[\s\t\r\n]*[""']?[\s\t\r\n]*(?<imgUrl>[^\s\t\r\n""'<>]*)[^<>]*?/?[\s\t\r\n]*>", RegexOptions.IgnoreCase);

            i = 1;
            foreach (var nrt in neirongArr)
            {
                if (!nrt.StartsWith("http") || nrt.EndsWith("spaceball.gif"))
                {
                    continue;
                }
                sp.NeiRongTu.Add("内容" + i, nrt);
                Console.WriteLine("采集到内容图:{0}", nrt);
                i += 1;
            }
            return sp;
        }
        static void prepareData()
        {
            basePath = Directory.CreateDirectory("data").FullName;
            idlist = File.ReadLines("ids.txt").ToList();
            foreach (var id in idlist)
            {
                var sp = getShangPin(id);
                if(sp == null)
                {
                    continue;
                }
                var curP = Path.Combine(basePath, sp.HuoHao);
                Directory.CreateDirectory(curP);
                var jsonStr = JsonConvert.SerializeObject(sp);
                File.WriteAllText(Path.Combine(curP, "config.txt"), jsonStr);
            }
        }
    }
}
