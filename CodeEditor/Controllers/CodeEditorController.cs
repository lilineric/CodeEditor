using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CodeEditor.Biz;
using Microsoft.JScript;
using Newtonsoft.Json;
using SharpSvn;
using System.Threading;

namespace CodeEditor.Controllers
{
    public class CodeEditorController : Controller
    {
        static object syncObj = new object();

        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public ActionResult GetCodeDirectories(string id)
        {
            return ConvertToContent(CodeEditorBiz.Instance.GetCodeDirectories(id));
        }

        [HttpPost]
        public string GetText(string path)
        {
            return CodeEditorBiz.Instance.GetScripts(path);
        }

        [HttpPost]
        public void SaveScript(string path, string scripts)
        {
            CodeEditorBiz.Instance.SaveScripts(path, GlobalObject.unescape(scripts));
        }

        [HttpPost]
        public void LoginSVN(string username, string password)
        {
            Session["username"] = username;
            Session["password"] = password;
        }

        public ActionResult CommitCode(string filePath)
        {
            return SvnExcute((x, y) => CodeEditorBiz.Instance.CommitCode(filePath, x, y), "提交成功");
        }

        public ActionResult RevertCode(string filePath)
        {
            return SvnExcute((x, y) => CodeEditorBiz.Instance.RevertCode(filePath, x, y), "还原成功");
        }

        public ActionResult UpdateCode(string filePath)
        {
            return SvnExcute((x, y) => CodeEditorBiz.Instance.UpdateCode(filePath, x, y), "更新成功");
        }

        public ActionResult CleanupCode(string filePath)
        {
            return SvnExcute((x, y) => CodeEditorBiz.Instance.CleanupCode(filePath, x, y), "清理成功");
        }

        ContentResult SvnExcute(Action<string, string> act, string msg)
        {
            var username = Session["username"];
            var password = Session["password"];

            if (username == null || password == null)
            {
                return ConvertToContent(new
                {
                    Code = -1,
                    Msg = "未登录！"
                });
            }
            if (!Monitor.TryEnter(syncObj))
                return ConvertToContent(new
                {
                    Code = -2,
                    Msg = "现在有人在发布，或者操作SVN，清稍后再试！"
                });
            try
            {
                act(username.ToString(), password.ToString());
                return ConvertToContent(new
                {
                    Code = 1,
                    Msg = msg
                });
            }
            catch (SvnException ex)
            {
                if (ex.SvnErrorCode == SvnErrorCode.SVN_ERR_ILLEGAL_TARGET)
                {
                    return ConvertToContent(new
                    {
                        Code = 0,
                        Msg = "文件未在SVN目录下！"
                    });
                }
                return ConvertToContent(new
                {
                    Code = 0,
                    Msg = "提交失败！" + ex.Message
                });
            }
            catch (Exception ex)
            {
                return ConvertToContent(new
                {
                    Code = 0,
                    Msg = "提交失败！" + ex.Message
                });
            }
            finally
            {
                Monitor.Exit(syncObj);
            }
        }

        ContentResult ConvertToContent(object obj)
        {
            return Content(JsonConvert.SerializeObject(obj), "application/json");
        }
    }
}
