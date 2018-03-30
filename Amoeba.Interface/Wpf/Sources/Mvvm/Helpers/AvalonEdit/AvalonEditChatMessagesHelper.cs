﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Amoeba.Messages;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Rendering;
using Omnius.Base;
using Omnius.Security;
using Omnius.Utils;

namespace Amoeba.Interface
{
    class AvalonEditChatMessagesInfo
    {
        public AvalonEditChatMessagesInfo(IEnumerable<ChatMessageInfo> chatMessageInfos, IEnumerable<Signature> trustSignatures)
        {
            if (chatMessageInfos != null) this.ChatMessageInfos = new ReadOnlyCollection<ChatMessageInfo>(chatMessageInfos.ToList());
            if (trustSignatures != null) this.TrustSignatures = new ReadOnlyCollection<Signature>(trustSignatures.ToList());
        }

        public IEnumerable<ChatMessageInfo> ChatMessageInfos { get; }
        public IEnumerable<Signature> TrustSignatures { get; }
    }

    class AvalonEditChatMessagesHelper : CustomAvalonEditHelperBase
    {
        public static ICommand GetCommand(DependencyObject obj)
        {
            return (ICommand)obj.GetValue(CommandProperty);
        }

        public static void SetCommand(DependencyObject obj, ICommand value)
        {
            obj.SetValue(CommandProperty, value);
        }

        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.RegisterAttached("Command", typeof(ICommand), typeof(AvalonEditChatMessagesHelper), new PropertyMetadata(null));

        public static int GetInfo(DependencyObject obj)
        {
            return (int)obj.GetValue(InfoProperty);
        }

        public static void SetInfo(DependencyObject obj, int value)
        {
            obj.SetValue(InfoProperty, value);
        }

        public static readonly DependencyProperty InfoProperty =
            DependencyProperty.RegisterAttached("Info", typeof(AvalonEditChatMessagesInfo), typeof(AvalonEditChatMessagesHelper),
                new UIPropertyMetadata(
                    null,
                    (o, e) =>
                    {
                        var textEditor = o as TextEditor;
                        if (textEditor == null) return;

                        Clear(textEditor);
                        Setup(textEditor);

                        var info = e.NewValue as AvalonEditChatMessagesInfo;
                        if (info == null) return;

                        Set(textEditor, info);
                    }
                )
            );

        private static void Setup(TextEditor textEditor)
        {
            textEditor.TextArea.TextView.Options.EnableEmailHyperlinks = false;
            textEditor.TextArea.TextView.Options.EnableHyperlinks = false;

            textEditor.TextArea.TextView.LineTransformers.Clear();

            textEditor.TextArea.Caret.CaretBrush = Brushes.Transparent;
        }

        private static void Clear(TextEditor textEditor)
        {
            textEditor.Document.BeginUpdate();

            textEditor.Document.Text = "";
            textEditor.CaretOffset = 0;
            textEditor.SelectionLength = 0;
            textEditor.TextArea.TextView.ElementGenerators.Clear();
            textEditor.ScrollToHome();

            textEditor.Document.EndUpdate();
        }

        private static void Set(TextEditor textEditor, AvalonEditChatMessagesInfo info)
        {
            textEditor.FontFamily = new FontFamily(SettingsManager.Instance.ViewSetting.Fonts.Chat_Message.FontFamily);
            textEditor.FontSize = (double)new FontSizeConverter().ConvertFromString(SettingsManager.Instance.ViewSetting.Fonts.Chat_Message.FontSize + "pt");

            var document = new StringBuilder();
            var settings = new List<CustomElementSetting>();

            var trustSignatures = new HashSet<Signature>(info.TrustSignatures);
            var infos = info.ChatMessageInfos.ToList();
            infos.Sort((x, y) => x.Message.CreationTime.CompareTo(y.Message.CreationTime));

            foreach (var target in infos)
            {
                int startOffset = document.Length;

                {
                    string stateText = target.State.HasFlag(ChatMessageState.New) ? "!" : "#";
                    string creationTimeText = target.Message.CreationTime.ToLocalTime().ToString(LanguagesManager.Instance.Global_DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo);
                    string SignatureText = target.Message.AuthorSignature.ToString();

                    {
                        if (stateText != null)
                        {
                            settings.Add(new CustomElementSetting("State", document.Length, stateText.Length));
                            document.Append(stateText);
                            document.Append(" ");
                        }

                        settings.Add(new CustomElementSetting("Signature", document.Length, SignatureText.Length));
                        document.Append(SignatureText);
                        document.Append(" - ");

                        settings.Add(new CustomElementSetting("CreationTime", document.Length, creationTimeText.Length));
                        document.Append(creationTimeText);

                        if (!trustSignatures.Contains(target.Message.AuthorSignature) && target.Message.Cost != null)
                        {
                            document.Append(" +");
                            document.Append(target.Message.Cost.Value);
                        }
                    }

                    document.AppendLine();
                }

                {
                    document.AppendLine();
                }

                {
                    foreach (string line in (target.Message.Value.Comment ?? "")
                        .Trim('\r', '\n')
                        .Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None)
                        .Take(128)
                        .Select(n => StringUtils.Normalize(n)))
                    {
                        foreach (var match in _uriRegexes.Select(n => n.Matches(line)).SelectMany(n => n.OfType<Match>()))
                        {
                            settings.Add(new CustomElementSetting("Uri", document.Length + match.Index, match.Length));
                        }

                        document.AppendLine(line);
                    }
                }

                {
                    document.AppendLine();
                }
            }

            if (document.Length >= 2) document.Remove(document.Length - 2, 2);

            textEditor.Document.BeginUpdate();

            textEditor.Document.Text = "";
            textEditor.CaretOffset = 0;
            textEditor.SelectionLength = 0;
            textEditor.TextArea.TextView.ElementGenerators.Clear();
            textEditor.ScrollToHome();

            textEditor.Document.Text = document.ToString();

            var elementGenerator = new CustomElementGenerator(settings, trustSignatures);
            elementGenerator.SelectEvent += (CustomElementRange range) => textEditor.Select(range.Start, range.End - range.Start);
            elementGenerator.ClickEvent += (string text) =>
            {
                var command = GetCommand(textEditor);
                if (command != null)
                {
                    if (command.CanExecute(text))
                    {
                        command.Execute(text);
                    }
                }
            };
            textEditor.TextArea.TextView.ElementGenerators.Add(elementGenerator);

            textEditor.Document.EndUpdate();

            textEditor.CaretOffset = textEditor.Document.Text.Length;
            textEditor.TextArea.Caret.BringCaretToView();
            textEditor.ScrollToEnd();
        }

        class CustomElementGenerator : AbstractCustomElementGenerator
        {
            private HashSet<Signature> _trustSignatures = new HashSet<Signature>();

            public CustomElementGenerator(IEnumerable<CustomElementSetting> settings, IEnumerable<Signature> trustSignatures)
                : base(settings)
            {
                _trustSignatures.UnionWith(trustSignatures);
            }

            public override VisualLineElement ConstructElement(int offset)
            {

                var result = this.FindMatch(offset);

                try
                {
                    if (result != null)
                    {
                        if (result.Type == "State")
                        {
                            double size = (double)new FontSizeConverter().ConvertFromString(SettingsManager.Instance.ViewSetting.Fonts.Chat_Message.FontSize + "pt");

                            var image = new Image() { Height = (size - 3), Width = (size - 3), Margin = new Thickness(1.5, 1.5, 0, 0) };
                            if (result.Value == "!") image.Source = AmoebaEnvironment.Images.YelloBall;
                            else if (result.Value == "#") image.Source = AmoebaEnvironment.Images.GreenBall;

                            var element = new CustomObjectElement(result.Value, image);

                            element.ClickEvent += (string text) =>
                            {
                                this.OnSelectEvent(new CustomElementRange(offset, offset + result.Value.Length));
                            };

                            return element;
                        }
                        else if (result.Type == "Signature")
                        {
                            Brush brush;

                            if (_trustSignatures.Contains(Signature.Parse(result.Value)))
                            {
                                brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(SettingsManager.Instance.ViewSetting.Colors.Message_Trust));
                            }
                            else
                            {
                                brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(SettingsManager.Instance.ViewSetting.Colors.Message_Untrust));
                            }

                            var element = new CustomTextElement(result.Value);
                            element.Foreground = brush;

                            element.ClickEvent += (string text) =>
                            {
                                this.OnSelectEvent(new CustomElementRange(offset, offset + result.Value.Length));
                            };

                            return element;
                        }
                        else if (result.Type == "Uri")
                        {
                            string uri = result.Value;

                            CustomObjectElement element = null;

                            if (uri.StartsWith("http:") | uri.StartsWith("https:"))
                            {
                                var textBlock = new TextBlock();
                                textBlock.Text = uri.Substring(0, Math.Min(64, uri.Length)) + ((uri.Length > 64) ? "..." : "");
                                textBlock.ToolTip = HttpUtility.UrlDecode(uri);

                                element = new CustomObjectElement(uri, textBlock);
                            }
                            else if (uri.StartsWith("Tag:"))
                            {
                                var tag = AmoebaConverter.FromTagString(uri);

                                var textBlock = new TextBlock();
                                textBlock.Text = uri.Substring(0, Math.Min(64, uri.Length)) + ((uri.Length > 64) ? "..." : "");

                                element = new CustomObjectElement(uri, textBlock);
                            }
                            else if (uri.StartsWith("Seed:"))
                            {
                                var seed = AmoebaConverter.FromSeedString(uri);

                                var textBlock = new TextBlock();
                                textBlock.Text = uri.Substring(0, Math.Min(64, uri.Length)) + ((uri.Length > 64) ? "..." : "");

                                element = new CustomObjectElement(uri, textBlock);
                            }

                            if (element != null)
                            {
                                element.ClickEvent += (string text) =>
                                {
                                    this.OnSelectEvent(new CustomElementRange(offset, offset + result.Value.Length));
                                    this.OnClickEvent(text);
                                };

                                return element;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }

                return null;
            }
        }
    }
}
