using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CodeEditor.Models
{
    public class CodeFile
    {
        public bool IsDirectory { get; set; }
        public string FileName { get; set; }
        public string Path { get; set; }
        public CodeEditorType EditorType { get; set; }
        public DateTime UpdatdeTime { get; set; }
        public bool IsChange { get; set; }
        public string Id { get { return Path.Replace("\\", "").Replace(".", "").Replace(":", ""); } }
    }

    public class CodeFileTreeNode
    {
        public string id
        {
            get { return attributes.Path; }
        }

        public string state
        {
            get;
            set;
        }

        public string text
        {
            get
            {
                var color = attributes.IsChange ? "red" : "black";

                return "<span id='span" + attributes.Id + "' style='color: " + color + "'>" + attributes.FileName + "</span>";
            }
        }
        public CodeFile attributes { get; set; }
    }

    public enum CodeEditorType
    {
        CSharp = 0,
        Js = 1,
        Html = 2,
        Xml = 3,
        Css = 4,
        Non = 5,
    }
}