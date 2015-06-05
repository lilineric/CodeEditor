using CodeEditor.Models;
using Newtonsoft.Json;
using SharpSvn;
using SharpSvn.Security;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

namespace CodeEditor.Biz
{
    public class CodeEditorBiz
    {
        public static CodeEditorBiz Instance
        {
            get
            {
                return new CodeEditorBiz();
            }
        }

        private const string SourceCodePath = @"D:\RuralSourceCode";

        private const string MiscCodePath = @"D:\Web.Misc\WebUI";

        static CodeEditorType JudgeCodeEditorType(string extension)
        {
            switch (extension)
            {
                case ".css":
                    return CodeEditorType.Css;
                case ".js":
                    return CodeEditorType.Js;
                case ".config":
                case ".xml":
                    return CodeEditorType.Xml;
                case ".cs":
                    return CodeEditorType.CSharp;
                case ".html":
                case ".htm":
                case ".aspx":
                case ".cshtml":
                    return CodeEditorType.Html;
                default:
                    return CodeEditorType.Non;
            }
        }

        public object GetCodeDirectories(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                var rootNode = new List<CodeFileTreeNode>()
                {
                    new CodeFileTreeNode()
                    {
                        state = "closed",
                        attributes = new CodeFile
                        {
                            EditorType = CodeEditorType.Non,
                            FileName = "RuralSourceCode",
                            IsDirectory = true,
                            Path = SourceCodePath,
                        }
                    },
                    new CodeFileTreeNode()
                    {
                        state = "closed",
                        attributes = new CodeFile
                        {
                            EditorType = CodeEditorType.Non,
                            FileName = "Web.Misc",
                            IsDirectory = true,
                            Path = MiscCodePath,
                        }
                    }
                };
                return rootNode;
            }

            var codeTreeNodes = new List<CodeFileTreeNode>();
            var path = id;
            if (!Directory.Exists(path))
            {
                throw new Exception("目录不存在");
            }
            var directoryInfo = new DirectoryInfo(path);
            directoryInfo.GetFileSystemInfos()
                .ToList()
                .ForEach(x =>
                {
                    var editorType = JudgeCodeEditorType(x.Extension);
                    var isDirectory = x is DirectoryInfo;
                    if (editorType == CodeEditorType.Non && !isDirectory)
                    {
                        return;
                    }
                    if (x.Attributes.ToString().Contains(FileAttributes.Hidden.ToString()))
                    {
                        return;
                    }
                    codeTreeNodes.Add(new CodeFileTreeNode
                    {
                        attributes = new CodeFile
                        {
                            EditorType = editorType,
                            FileName = x.Name,
                            Path = x.FullName,
                            UpdatdeTime = x.LastWriteTime,
                            IsDirectory = isDirectory,
                            IsChange = IsChanged(x.FullName)
                        },
                        state = isDirectory ? "closed" : "open",
                    });
                });
            return codeTreeNodes;
        }

        static bool IsChanged(string path)
        {
            using (var client = new SvnClient())
            {
                Collection<SvnStatusEventArgs> status;
                client.GetStatus(path, out status);

                return status.Any();
            }
        }

        public string GetScripts(string path)
        {
            if (!File.Exists(path))
            {
                throw new Exception("文件不存在");
            }
            return File.ReadAllText(path);
        }

        public void SaveScripts(string path, string scripts)
        {
            File.WriteAllText(path, scripts);
        }

        public void CommitCode(string filePath, string username, string password)
        {
            using (var client = new SvnClient())
            {
                CreateSvnClient(client, username, password).Commit(filePath, new SvnCommitArgs { LogMessage = username + " commit" });
            }
        }

        public void RevertCode(string filePath, string username, string password)
        {
            using (var client = new SvnClient())
            {
                CreateSvnClient(client, username, password).Revert(filePath);
            }
        }

        public void UpdateCode(string filePath, string username, string password)
        {
            using (var client = new SvnClient())
            {
                CreateSvnClient(client, username, password).Update(filePath);
            }
        }

        public void CleanupCode(string filePath, string username, string password)
        {
            using (var client = new SvnClient())
            {
                CreateSvnClient(client, username, password).CleanUp(filePath);
            }
        }


        static SvnClient CreateSvnClient(SvnClient client, string username, string password)
        {
            client.Authentication.UserNamePasswordHandlers += delegate(Object s, SvnUserNamePasswordEventArgs ee)
            {
                ee.UserName = username;
                ee.Password = password;
            };

            client.Authentication.SslServerTrustHandlers += delegate(Object ssender, SvnSslServerTrustEventArgs se)
            {

                se.AcceptedFailures = se.Failures;
                se.Save = true; // Save acceptance to authentication store
            };
            return client;
        }

    }
}
