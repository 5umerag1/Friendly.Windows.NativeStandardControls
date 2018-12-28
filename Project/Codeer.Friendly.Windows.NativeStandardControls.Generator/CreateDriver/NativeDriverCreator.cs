﻿using Codeer.TestAssistant.GeneratorToolKit;
using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;

namespace Codeer.Friendly.Windows.NativeStandardControls.Generator.CreateDriver
{
    internal class NativeDriverCreator
    {
        private const string TODO_COMMENT = "// TODO It is not the best way to identify. Please change to a better method.";
        private const string INDENT = "    ";
        private const string PROPERTY_TEXT = "Text";
        private const string PROPERTY_HANDLE = "Handle";
        private const string PROPERTY_CHILDREN = "Children";
        private const string PROPERTY_DIALOGID = "DialogId";
        private const string PROPERTY_ZINDEX = "ZIndex";
        private const string PROPERTY_CLASSNAME = "ClassName";

        private readonly CodeDomProvider _dom;
        private readonly DriverElementNameGeneratorAdaptor _customNameGenerator;

        public NativeDriverCreator(CodeDomProvider dom)
        {
            _dom = dom;
            _customNameGenerator = new DriverElementNameGeneratorAdaptor(dom);
        }

        public void CreateDriver(object windowInfoSrc)
        {
            CreateDriver(-1, string.Empty, windowInfoSrc, new List<string>());
        }

        private void CreateDriver(int index, string rootDriver, object windowInfoSrc, List<string> driverNames)
        {
            //WindowInfoから情報を取り出す
            var windowInfo = new ReflectionAccessor(windowInfoSrc);
            var windowText = windowInfo.GetProperty<string>(PROPERTY_TEXT);
            var windowHandle = windowInfo.GetProperty<IntPtr>(PROPERTY_HANDLE);

            var isTopLevel = NativeMethods.GetAncestor(windowHandle, NativeMethods.Ancestor_Root) == windowHandle;

            var driverName = MakeDriverName(windowText, driverNames);
            var dialogIds = new List<int>();
            foreach (var c in windowInfo.GetProperty<IEnumerable>(PROPERTY_CHILDREN))
            {
                dialogIds.Add(new ReflectionAccessor(c).GetProperty<int>(PROPERTY_DIALOGID));
            }
            var zIndexes = windowInfo.GetProperty<int[]>(PROPERTY_ZINDEX);

            //子を調べる
            var fileName = $"{driverName}.cs";
            var members = new List<string>();
            var usings = new List<string>();
            var childrenZIndexTargetIndex = index + 1;
            var childNames = new List<string>();
            foreach (var childWindowInfo in OrderByTabOrder(windowInfo.GetProperty<IEnumerable>(PROPERTY_CHILDREN)))
            {
                //ドライバに合致するコントロールであるか
                string className = childWindowInfo.GetProperty<string>(PROPERTY_CLASSNAME);
                var handle = childWindowInfo.GetProperty<IntPtr>(PROPERTY_HANDLE);
                var searchDescendantUserControls = true;
                if (DriverCreatorAdapter.WindowClassNameAndControlDriver.TryGetValue(className, out var ctrlDriver) && ctrlDriver.DriverMappingEnabled)
                {
                    searchDescendantUserControls = ctrlDriver.SearchDescendantUserControls;
                    var typeName = DriverCreatorUtils.GetTypeName(ctrlDriver.ControlDriverTypeFullName);
                    var ns = DriverCreatorUtils.GetTypeNamespace(ctrlDriver.ControlDriverTypeFullName);
                    if (!usings.Contains(ns)) usings.Add(ns);

                    var ctrlDriverPropName = _customNameGenerator.MakeDriverPropName(childWindowInfo.GetProperty<IntPtr>(PROPERTY_HANDLE), typeName, childNames);
                    var dialogId = childWindowInfo.GetProperty<int>(PROPERTY_DIALOGID);
                    var cZIndexes = childWindowInfo.GetProperty<int[]>(PROPERTY_ZINDEX);
                    string key;
                    if (1 < CollectionUtility.Where(dialogIds, x => x == dialogId).Count)
                    {
                        key = $"Core.IdentifyFromZIndex({cZIndexes[childrenZIndexTargetIndex]})";
                        members.Add($"public {typeName} {ctrlDriverPropName} => new {typeName}({key}); {TODO_COMMENT}");
                    }
                    else
                    {
                        key = $"Core.IdentifyFromDialogId({dialogId})";
                        members.Add($"public {typeName} {ctrlDriverPropName} => new {typeName}({key});");
                    }
                    DriverCreatorAdapter.AddCodeLineSelectInfo(fileName, key, handle);
                }

                //さらに子を持っているならUserControlDriverを作成する
                if (searchDescendantUserControls && (0 < childWindowInfo.GetProperty<Array>(PROPERTY_CHILDREN).Length))
                {
                    CreateDriver(childrenZIndexTargetIndex, driverName, childWindowInfo.Object, driverNames);
                }
            }

            //コード生成
            var zIndex = index == -1 ? -1 : zIndexes[index];
            DriverCreatorAdapter.AddCode(fileName, GenerateCode(isTopLevel, rootDriver, DriverCreatorAdapter.SelectedNamespace, driverName, windowText, zIndex, usings, members), windowHandle);
        }

        private List<ReflectionAccessor> OrderByTabOrder(IEnumerable enumerable)
        {
            var list = new List<ReflectionAccessor>();
            foreach (var e in enumerable)
            {
                list.Add(new ReflectionAccessor(e));
            }
            list.Sort((l, r) =>
            {
                var la = l.GetProperty<int[]>(PROPERTY_ZINDEX);
                var ra = r.GetProperty<int[]>(PROPERTY_ZINDEX);
                var lIndex = la.Length == 0 ? -1 : la[la.Length - 1];
                var rIndex = ra.Length == 0 ? -1 : ra[ra.Length - 1];
                return rIndex - lIndex;
            });
            return list;
        }

        private string MakeDriverName(string text, List<string> names)
        {
            foreach (var err in "(){}<>+-=*/%!\"#$&'^~\\|@;:,.?")
            {
                text = text.Replace(err, '_');
            }

            var nameSrc = _dom.IsValidIdentifier(text) ? text : "Window";
            var name = nameSrc;
            for (int i = 0;; i++)
            {
                if (!names.Contains(name))
                {
                    names.Add(name);
                    return name + DriverCreatorUtils.Suffix;
                }
                name = nameSrc + i;
            }
        }

        private string GenerateCode(bool isTopLevel, string rootDriver, string nameSpace, string driverClassName, string windowText, int zIndex, List<string> usings, List<string> members)
        {
            var code = new List<string>
            {
                "using Codeer.Friendly.Dynamic;",
                "using Codeer.Friendly.Windows;",
                "using Codeer.Friendly.Windows.Grasp;",
                "using Codeer.TestAssistant.GeneratorToolKit;"
            };
            foreach (var e in usings)
            {
                code.Add($"using {e};");
            }

            var attr = string.IsNullOrEmpty(rootDriver) ? "WindowDriver" : "UserControlDriver";

            code.Add(string.Empty);
            code.Add($"namespace {nameSpace}");
            code.Add("{");
            code.Add($"{INDENT}[{attr}]");
            code.Add($"{INDENT}public class {driverClassName}");
            code.Add($"{INDENT}{{");
            code.Add($"{INDENT}{INDENT}public WindowControl Core {{ get; }}");
            foreach (var e in members)
            {
                code.Add($"{INDENT}{INDENT}{e}");
            }
            code.Add(string.Empty);
            code.Add($"{INDENT}{INDENT}public {driverClassName}(WindowControl core)");
            code.Add($"{INDENT}{INDENT}{{");
            code.Add($"{INDENT}{INDENT}{INDENT}Core = core;");
            code.Add($"{INDENT}{INDENT}}}");
            code.Add($"{INDENT}}}");

            if (isTopLevel)
            {
                code.Add(string.Empty);
                code.Add($"{INDENT}public static class {driverClassName}_Extensions");
                code.Add($"{INDENT}{{");
                code.Add($"{INDENT}{INDENT}[WindowDriverIdentify(WindowText = \"{windowText}\")]");
                code.Add($"{INDENT}{INDENT}public static {driverClassName} {GetFuncName(driverClassName)}(this WindowsAppFriend app)");
                code.Add($"{INDENT}{INDENT}{INDENT}=> new {driverClassName}(app.WaitForIdentifyFromWindowText(\"{windowText}\"));");
                code.Add($"{INDENT}}}");
            }
            else if (!string.IsNullOrEmpty(rootDriver))
            {
                code.Add(string.Empty);
                code.Add($"{INDENT}public static class {driverClassName}_Extensions");
                code.Add($"{INDENT}{{");
                code.Add($"{INDENT}{INDENT}{TODO_COMMENT}");
                code.Add($"{INDENT}{INDENT}[UserControlDriverIdentify]");
                code.Add($"{INDENT}{INDENT}public static {driverClassName} {GetFuncName(driverClassName)}(this {rootDriver} window)");
                code.Add($"{INDENT}{INDENT}{INDENT}=> new {driverClassName}(window.Core.IdentifyFromZIndex({zIndex}));");
                code.Add($"{INDENT}}}");
            }
            code.Add("}");
            return string.Join(Environment.NewLine, code.ToArray());
        }

        private string GetFuncName(string driverClassName)
        {
            return $"Attach_{driverClassName.Substring(0, driverClassName.Length - DriverCreatorUtils.Suffix.Length)}";
        }
    }
}
