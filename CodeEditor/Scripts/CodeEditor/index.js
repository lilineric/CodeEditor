$(function () {
    var $fileTree = $('#codeDirectory');
    $('#loginSVN').dialog('close');
    $('#message').dialog('close');

    $fileTree.tree({
        onClick: function (node) {
            var attributes = node.attributes;
            if (attributes.IsDirectory) {
                return;
            }
            openTab(node.attributes);
        },
        onContextMenu: function (e, node) {
            selectedNodeAttribute = node.attributes;
            e.preventDefault();
            $(this).tree('select', node.target);
            $('#mm').menu('show', {
                left: e.pageX,
                top: e.pageY
            });
        }
    });

    var $script = $('#script');
    $script.tabs({
        onSelect: function (title, index) {
            var tab = $script.tabs('getTab', index);
            var id = tab.attr('id');
            if (!tabs[id]) {
                return;
            }
            currEditTabId = id;
            $('#filePathText').html(tabs[currEditTabId].path);
        },
        onBeforeClose: function (title, index) {
            var target = this;
            if (title.indexOf('Unsaved') < 0) {
                closeTab(index, target);
                return true;
            } else {
                $.messager.confirm(title, 'File is not saved, are sure you want to close it?', function (r) {
                    if (r) {
                        closeTab(index, target, function () {
                            $(target).tabs('close', index);
                        });
                    }
                });
                return false; // prevent from closing
            }
        }
    });
    $(document).keydown(function (event) {
        if ((event.ctrlKey || event.metaKey) && event.which == 83) {
            event.preventDefault();
            save();
            return false;
        };
        return true;
    }
    );
});

var filePath = '';
var defaultTheme = 'visual-studio-light';
var tabs = [];

var currEditTabId = '';

var selectedNodeAttribute = {};

var loginBeforeFunc;

function closeTab(index, target, closeFunc) {
    var tab = $('#script').tabs('getTab', index);
    var id = tab.attr('id');
    tabs[id] = undefined;
    currEditTabId = '';

    var opts = $(target).tabs('options');
    var bc = opts.onBeforeClose;
    opts.onBeforeClose = function () { };  // allowed to close now
    if (closeFunc && $.isFunction(closeFunc)) {
        closeFunc();
    }
    opts.onBeforeClose = bc;  // restore the event function
}

function selectTheme() {
    var input = document.getElementById('selectTheme');
    defaultTheme = input.options[input.selectedIndex].innerHTML;
    for (var key in tabs) {
        var tab = tabs[key];
        if (tab && tab.editor) {
            tab.editor.setOption('theme', defaultTheme);
        }
    }
}

function restore() {
    var currTab = tabs[currEditTabId];
    currTab.editor.getDoc().setValue(currTab.text);
    setTabTitle(currTab.tab, currTab.title);
    currTab.isChange = false;
}

function save() {
    if (currEditTabId === "" || tabs[currEditTabId].path === '') {
        return;
    }

    var currTab = tabs[currEditTabId];

    var script = currTab.editor.getDoc().getValue();

    $.ajax({
        url: '/CodeEditor/SaveScript',
        type: 'POST',
        data: { path: tabs[currEditTabId].path, scripts: escape(script) },
        dataType: 'text',
        success: function () {
            setTabTitle(currTab.tab, currTab.title);
            currTab.text = script;
            currTab.isChange = false;
            var $span = $('#span' + currEditTabId.substring(3));

            $span.css('color', 'red');
        }
    });
}
function openTab(attributes) {
    if (!attributes) {
        return;
    }
    var path = attributes.Path;

    currEditTabId = 'tab' + attributes.Id;
    $('#filePathText').html(path);

    var title = attributes.FileName;

    var $script = $('#script');
    var existTab = tabs[currEditTabId];

    if (existTab) {
        var displayTitle = existTab.isChange ? getTabUnSaveTitle(title) : title;
        $script.tabs('select', displayTitle);
        return;
    }

    $script.tabs('add', {
        title: title,
        content: '<textarea id="' + attributes.Id + '" name="code"></textarea>',
        closable: true,
        id: currEditTabId,
    });
    filePath = path;
    var editor = CodeMirror.fromTextArea(document.getElementById(attributes.Id),
                {
                    lineNumbers: true,
                    styleActiveLine: true,
                    matchBrackets: true,
                    selectionPointer: true,
                    extraKeys:
                    {
                        'Ctrl-J': 'autocomplete',
                        'F11': function (cm) {
                            cm.setOption('fullScreen', !cm.getOption('fullScreen'));
                        },
                        'Esc': function (cm) {
                            if (cm.getOption('fullScreen')) cm.setOption('fullScreen', false);
                        }
                    }
                });
    editor.setOption('theme', defaultTheme);

    editor.on('change', function () {
        var tab = $script.tabs('getSelected');
        var tabArray = tabs[tab.attr('id')];
        if (!tabArray) {
            return;
        }
        if (!tabArray.isChange) {
            tabArray.isChange = true;
            setTabTitle(tab, getTabUnSaveTitle(title));
        }
    });

    $.ajax({
        url: '/CodeEditor/GetText',
        type: 'POST',
        data: { path: path },
        dataType: 'text',
        success: function (result) {
            editor.getDoc().setValue(result);
            tabs['tab' + attributes.Id] = { path: path, text: result, editor: editor, tab: $script.tabs('getSelected'), isChange: false, title: title };
        }
    });

    if (attributes.EditorType == '0') {
        editor.setOption('mode', 'text/x-csharp');
    } else if (attributes.EditorType == '1') {
        editor.setOption('mode', 'javascript');
    } else if (attributes.EditorType == '2') {
        var mixedMode = {
            name: 'htmlmixed',
            scriptTypes: [{
                matches: /\/x-handlebars-template|\/x-mustache/i,
                mode: null
            },
                          {
                              matches: /(text|application)\/(x-)?vb(a|script)/i,
                              mode: 'vbscript'
                          }]
        };
        editor.setOption('mode', mixedMode);
    } else if (attributes.EditorType == '3') {
        editor.setOption('mode', 'xml');
    } else if (attributes.EditorType == '4') {
        editor.setOption('mode', 'css');
    }
}

function setTabTitle(currTab, title) {
    var options = currTab.panel('options');
    var tab = options.tab;
    options.title = title;
    var $title = tab.find('span.tabs-title');
    $title.html(title);
}

function getTabUnSaveTitle(title) {
    return '*' + title + ' —— Unsaved';
}

function svnOperate(url, message, needUpdateCss) {
    showMessage(selectedNodeAttribute.Path, message + selectedNodeAttribute.Path);
    var updateCss;
    if (needUpdateCss) {
        updateCss = function () {
            $('#span' + selectedNodeAttribute.Id).css('color', 'black');
        };
    }
    loginBeforeFunc = function () {
        postSVN(url, updateCss);
    };
    loginBeforeFunc();
}

function hideMessage(content) {
    var html = $('#messageContent').html();
    $('#messageContent').html(html + '<br/>' + content);
    $('#message').dialog('close');
}
function showMessage(title, content) {
    var $messsage = $('#message');
    $messsage.panel('setTitle', title);
    $('#messageContent').html(content);
    $messsage.dialog('open');
}

function postSVN(url, success) {
    $.ajax({
        url: url,
        type: 'POST',
        data: { filePath: selectedNodeAttribute.Path },
        dataType: 'json',
        success: function (result) {
            if (result.Code == -1) {
                $('#loginSVN').dialog('open');
            }

            if (result.Code == -1 || result.Code == 0) {
                var html = $('#messageContent').html();
                $('#messageContent').html(html + '<br/><span style="color: red;">' + result.Msg + '</span>');
                return;
            }

            if (success && $.isFunction(success)) {
                success();
            }
            hideMessage(result.Msg);
        },
        error: function (result) {
            var html = $('#messageContent').html();
            $('#messageContent').html(html + '<br/><span style="color: red;">' + result.Msg + '</span');
        }
    });
}

function login() {
    var name = $('#username').val();
    var pwd = $('#password').val();
    if (name == '' || pwd == '') {
        return;
    }
    $.ajax({
        url: '/CodeEditor/LoginSVN',
        type: 'POST',
        data: { username: name, password: pwd },
        dataType: 'text',
        success: function () {
            $('#loginSVN').dialog('close');
            if (loginBeforeFunc && $.isFunction(loginBeforeFunc)) {
                loginBeforeFunc();
            }
        }
    });
}
