﻿using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using Telegram.Td.Api;
using Unigram.Common;
using Unigram.Controls.Messages.Content;
using Unigram.Converters;
using Unigram.Native.Composition;
using Unigram.Services;
using Unigram.ViewModels;
using Windows.Foundation;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Text;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Core.Direct;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;

namespace Unigram.Controls.Messages
{
    public sealed class MessageBubble : Control, IPlayerView
    {
        private MessageViewModel _message;

        private readonly List<EmojiPosition> _positions = new();

        private string _query;
        private long? _photoId;

        private bool _ignoreSizeChanged = true;
        private bool _ignoreSpoilers = false;
        private bool _ignoreLayoutUpdated = true;

        private DirectRectangleClip _cornerRadius;

        public MessageBubble()
        {
            DefaultStyleKey = typeof(MessageBubble);
        }

        public void UpdateQuery(string text)
        {
            _query = text;
        }

        #region InitializeComponent

        private ColumnDefinition PhotoColumn;

        private Grid ContentPanel;
        private Grid Header;
        private TextBlock HeaderLabel;
        private TextBlock AdminLabel;
        private MessageBubblePanel Panel;
        private RichTextBlock Message;
        private Border Media;
        private MessageFooter Footer;
        private ReactionsPanel Reactions;

        // Lazy loaded
        private CustomEmojiCanvas CustomEmoji;

        private ProfilePicture Photo;

        private Border BackgroundPanel;
        private Border CrossPanel;

        private GlyphButton PsaInfo;

        private MessageReference Reply;

        private HyperlinkButton Thread;
        private StackPanel RecentRepliers;
        private TextBlock ThreadGlyph;
        private TextBlock ThreadLabel;

        private ReactionsPanel MediaReactions;
        private ReplyMarkupPanel Markup;

        private Border Action;
        private GlyphButton ActionButton;

        private bool _templateApplied;

        protected override void OnApplyTemplate()
        {
            PhotoColumn = GetTemplateChild(nameof(PhotoColumn)) as ColumnDefinition;
            ContentPanel = GetTemplateChild(nameof(ContentPanel)) as Grid;
            Header = GetTemplateChild(nameof(Header)) as Grid;
            HeaderLabel = GetTemplateChild(nameof(HeaderLabel)) as TextBlock;
            AdminLabel = GetTemplateChild(nameof(AdminLabel)) as TextBlock;
            Panel = GetTemplateChild(nameof(Panel)) as MessageBubblePanel;
            Message = GetTemplateChild(nameof(Message)) as RichTextBlock;
            Media = GetTemplateChild(nameof(Media)) as Border;
            Footer = GetTemplateChild(nameof(Footer)) as MessageFooter;
            Reactions = GetTemplateChild(nameof(Reactions)) as ReactionsPanel;

            //ContentPanel.CanDrag = true;
            //ContentPanel.DragStarting += OnDragStarting;
            ContentPanel.SizeChanged += OnSizeChanged;
            Message.ContextMenuOpening += Message_ContextMenuOpening;
            Footer.SizeChanged += Footer_SizeChanged;

            ElementCompositionPreview.SetIsTranslationEnabled(Header, true);
            ElementCompositionPreview.SetIsTranslationEnabled(Message, true);
            ElementCompositionPreview.SetIsTranslationEnabled(Media, true);

            _cornerRadius = CompositionDevice.CreateRectangleClip(ContentPanel);

            _templateApplied = true;

            if (_message != null)
            {
                UpdateMessage(_message);
            }
        }

        private async void OnDragStarting(UIElement sender, DragStartingEventArgs args)
        {
            var deferral = args.GetDeferral();

            if (AllowDrag(_message, out string path))
            {
                var file = await _message.ProtoService.GetFileAsync(path);
                if (file != null)
                {
                    using (var stream = new InMemoryRandomAccessStream())
                    {
                        using (var writer = new DataWriter(stream.GetOutputStreamAt(0)))
                        {
                            writer.WriteInt64(_message.ChatId);
                            writer.WriteInt64(_message.Id);

                            await writer.FlushAsync();
                            await writer.StoreAsync();
                        }

                        stream.Seek(0);

                        args.Data.SetData("application/x-tl-message", stream.CloneStream());
                        args.Data.SetStorageItems(new[] { file }, true);

                        args.Data.RequestedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;

                        // Exception in some condition
                        args.DragUI.SetContentFromDataPackage();
                    }
                }
                else
                {
                    args.Cancel = true;
                }
            }
            else
            {
                args.Cancel = true;
            }

            deferral.Complete();
        }

        private bool AllowDrag(MessageViewModel message, out string path)
        {
            if (message == null || message.Ttl > 0 || !message.CanBeSaved)
            {
                path = null;
                return false;
            }

            var file = message.GetFile();
            if (file != null && file.Local.IsFileExisting())
            {
                path = file.Local.Path;
                return true;
            }

            path = null;
            return false;
        }

        #endregion

        public UIElement MediaTemplateRoot => Media.Child;

        public void UpdateMessage(MessageViewModel message)
        {
            if (_message?.Id != message?.Id)
            {
                _ignoreSpoilers = false;
            }

            _message = message;
            Tag = message;

            if (!_templateApplied)
            {
                return;
            }

            if (message != null)
            {
                UpdateMessageHeader(message);
                UpdateMessageReply(message);
                UpdateMessageContent(message);
                UpdateMessageInteractionInfo(message);

                Footer.UpdateMessage(message);
                UpdateMessageReplyMarkup(message);

                UpdateAttach(message);

                if (PhotoColumn.Width.IsAuto && message.HasSenderPhoto)
                {
                    PhotoColumn.Width = new GridLength(38, GridUnitType.Pixel);
                }
                else if (PhotoColumn.Width.IsAbsolute && !message.HasSenderPhoto)
                {
                    PhotoColumn.Width = new GridLength(0, GridUnitType.Auto);
                }
            }
            else
            {
                Message.Blocks.Clear();
                Media.Child = null;
                Reactions.UpdateMessageReactions(null);

                if (CustomEmoji != null)
                {
                    XamlMarkupHelper.UnloadObject(CustomEmoji);
                    CustomEmoji = null;
                }

                if (MediaReactions != null)
                {
                    XamlMarkupHelper.UnloadObject(MediaReactions);
                    MediaReactions = null;
                }
            }

            if (_highlight != null)
            {
                _highlight.StopAnimation("Opacity");
                _highlight.Opacity = 0;
            }
        }

        public string GetAutomationName()
        {
            if (_message == null)
            {
                return null;
            }

            return UpdateAutomation(_message);
        }

        public string UpdateAutomation(MessageViewModel message)
        {
            var chat = message.GetChat();

            var title = string.Empty;
            var senderBot = false;

            if (message.IsSaved)
            {
                title = message.ProtoService.GetTitle(message.ForwardInfo);
            }
            else if (chat.Type is ChatTypeBasicGroup || chat.Type is ChatTypeSupergroup supergroup && !supergroup.IsChannel)
            {
                if (message.IsOutgoing)
                {
                    title = null;
                }
                else if (message.ProtoService.TryGetUser(message.SenderId, out User senderUser))
                {
                    senderBot = senderUser.Type is UserTypeBot;
                    title = senderUser.GetFullName();
                }
                else if (message.ProtoService.TryGetChat(message.SenderId, out Chat senderChat))
                {
                    title = message.ProtoService.GetTitle(senderChat);
                }
            }

            var builder = new StringBuilder();
            if (title?.Length > 0)
            {
                builder.AppendLine($"{title}. ");
            }

            if (message.ReplyToMessage != null)
            {
                if (message.ProtoService.TryGetUser(message.ReplyToMessage.SenderId, out User replyUser))
                {
                    builder.AppendLine($"{Strings.Resources.AccDescrReplying} {replyUser.GetFullName()}. ");
                }
                else if (message.ProtoService.TryGetChat(message.ReplyToMessage.SenderId, out Chat replyChat))
                {
                    builder.AppendLine($"{Strings.Resources.AccDescrReplying} {message.ProtoService.GetTitle(replyChat)}. ");
                }
            }

            if (message.ForwardInfo != null)
            {
                if (message.ForwardInfo?.Origin is MessageForwardOriginUser fromUser)
                {
                    title = message.ProtoService.GetUser(fromUser.SenderUserId)?.GetFullName();
                    builder.AppendLine($"{Strings.Resources.AccDescrForwarding} {title}. ");
                }
                if (message.ForwardInfo?.Origin is MessageForwardOriginChat fromChat)
                {
                    title = message.ProtoService.GetTitle(message.ProtoService.GetChat(fromChat.SenderChatId));
                    builder.AppendLine($"{Strings.Resources.AccDescrForwarding} {title}. ");
                }
                else if (message.ForwardInfo?.Origin is MessageForwardOriginChannel fromChannel)
                {
                    title = message.ProtoService.GetTitle(message.ProtoService.GetChat(fromChannel.ChatId));
                    builder.AppendLine($"{Strings.Resources.AccDescrForwarding} {title}. ");
                }
            }

            builder.Append(Automation.GetSummary(message.ProtoService, message.Get(), true));

            if (message.AuthorSignature.Length > 0)
            {
                builder.Append($"{message.AuthorSignature}, ");
            }

            if (message.EditDate != 0 && message.ViaBotUserId == 0 && !senderBot && message.ReplyMarkup is not ReplyMarkupInlineKeyboard)
            {
                builder.Append($"{Strings.Resources.EditedMessage}, ");
            }

            var date = string.Format(Strings.Resources.TodayAtFormatted, Converter.ShortTime.Format(Utils.UnixTimestampToDateTime(message.Date)));
            if (message.IsOutgoing)
            {
                builder.Append(string.Format(Strings.Resources.AccDescrSentDate, date));
            }
            else
            {
                builder.Append(string.Format(Strings.Resources.AccDescrReceivedDate, date));
            }

            builder.Append(". ");

            var maxId = 0L;
            if (chat != null)
            {
                maxId = chat.LastReadOutboxMessageId;
            }

            if (message.SendingState is MessageSendingStateFailed)
            {
            }
            else if (message.SendingState is MessageSendingStatePending)
            {
            }
            else if (message.Id <= maxId)
            {
                builder.Append(Strings.Resources.AccDescrMsgRead);
            }
            else
            {
                builder.Append(Strings.Resources.AccDescrMsgUnread);
            }

            if (message.InteractionInfo?.ViewCount > 0)
            {
                builder.Append(". ");
                builder.Append(Locale.Declension("AccDescrNumberOfViews", message.InteractionInfo.ViewCount));
            }

            builder.Append(".");

            return builder.ToString();
        }

        public void UpdateAttach(MessageViewModel message, bool wide = false)
        {
            if (!_templateApplied)
            {
                return;
            }

            //var topLeft = 15d;
            //var topRight = 15d;
            //var bottomRight = 15d;
            //var bottomLeft = 15d;
            var radius = SettingsService.Current.Appearance.BubbleRadius;
            var small = radius < 4 ? radius : 4;

            var topLeft = radius;
            var topRight = radius;
            var bottomRight = radius;
            var bottomLeft = radius;

            if (message.IsOutgoing && !wide)
            {
                if (message.IsFirst && message.IsLast)
                {
                }
                else if (message.IsFirst)
                {
                    bottomRight = small;
                }
                else if (message.IsLast)
                {
                    topRight = small;
                }
                else
                {
                    topRight = small;
                    bottomRight = small;
                }
            }
            else
            {
                if (message.IsFirst && message.IsLast)
                {
                }
                else if (message.IsFirst)
                {
                    bottomLeft = small;
                }
                else if (message.IsLast)
                {
                    topLeft = small;
                }
                else
                {
                    topLeft = small;
                    bottomLeft = small;
                }
            }

            var content = message.GeneratedContent ?? message.Content;
            if (message.ReplyMarkup is ReplyMarkupInlineKeyboard)
            {
                if (content is MessageSticker or MessageDice or MessageVideoNote or MessageBigEmoji)
                {
                    _cornerRadius.Set(0);
                }
                else
                {
                    _cornerRadius.Set(topLeft, topRight, small, small);
                }

                if (Markup != null)
                {
                    Markup.CornerRadius = new CornerRadius(small, small, bottomRight, bottomLeft);
                }
            }
            else if (content is MessageSticker or MessageDice or MessageVideoNote or MessageBigEmoji)
            {
                _cornerRadius.Set(0);
            }
            else
            {
                _cornerRadius.Set(topLeft, topRight, bottomRight, bottomLeft);
            }

            Margin = new Thickness(0, message.IsFirst ? 4 : 2, 0, 0);

            UpdatePhoto(message);
        }

        private void UpdatePhoto(MessageViewModel message)
        {
            if (message.HasSenderPhoto)
            {
                if (message.IsLast)
                {
                    if (message.Id != _photoId || Photo == null || Photo.Visibility == Visibility.Collapsed)
                    {
                        if (Photo == null)
                        {
                            Photo = GetTemplateChild(nameof(Photo)) as ProfilePicture;
                            Photo.Click += Photo_Click;
                        }

                        if (message.IsSaved)
                        {
                            if (message.ForwardInfo?.Origin is MessageForwardOriginUser fromUser && message.ProtoService.TryGetUser(fromUser.SenderUserId, out User fromUserUser))
                            {
                                Photo.SetUser(message.ProtoService, fromUserUser, 30);
                            }
                            else if (message.ForwardInfo?.Origin is MessageForwardOriginChat fromChat && message.ProtoService.TryGetChat(fromChat.SenderChatId, out Chat fromChatChat))
                            {
                                Photo.SetChat(message.ProtoService, fromChatChat, 30);
                            }
                            else if (message.ForwardInfo?.Origin is MessageForwardOriginChannel fromChannel && message.ProtoService.TryGetChat(fromChannel.ChatId, out Chat fromChannelChat))
                            {
                                Photo.SetChat(message.ProtoService, fromChannelChat, 30);
                            }
                            else if (message.ForwardInfo?.Origin is MessageForwardOriginMessageImport fromImport)
                            {
                                Photo.Source = PlaceholderHelper.GetNameForUser(fromImport.SenderName, 30);
                            }
                            else if (message.ForwardInfo?.Origin is MessageForwardOriginHiddenUser fromHiddenUser)
                            {
                                Photo.Source = PlaceholderHelper.GetNameForUser(fromHiddenUser.SenderName, 30);
                            }
                        }
                        else if (message.ProtoService.TryGetUser(message.SenderId, out User senderUser))
                        {
                            Photo.SetUser(message.ProtoService, senderUser, 30);
                        }
                        else if (message.ProtoService.TryGetChat(message.SenderId, out Chat senderChat))
                        {
                            Photo.SetChat(message.ProtoService, senderChat, 30);
                        }

                        _photoId = message.Id;
                        Photo.Visibility = Visibility.Visible;
                    }
                }
                else if (Photo != null)
                {
                    _photoId = null;

                    Photo.Visibility = Visibility.Collapsed;
                    Photo.Clear();
                }
            }
            else if (Photo != null)
            {
                _photoId = null;

                XamlMarkupHelper.UnloadObject(Photo);
                Photo = null;
            }
        }

        private void Photo_Click(object sender, RoutedEventArgs e)
        {
            var message = _message;
            if (message == null)
            {
                return;
            }

            if (message.IsSaved)
            {
                if (message.ForwardInfo?.Origin is MessageForwardOriginUser fromUser)
                {
                    message.Delegate.OpenUser(fromUser.SenderUserId);
                }
                else if (message.ForwardInfo?.Origin is MessageForwardOriginChat fromChat)
                {
                    message.Delegate.OpenChat(fromChat.SenderChatId, true);
                }
                else if (message.ForwardInfo?.Origin is MessageForwardOriginChannel fromChannel)
                {
                    // TODO: verify if this is sufficient
                    message.Delegate.OpenChat(fromChannel.ChatId);
                }
                else if (message.ForwardInfo?.Origin is MessageForwardOriginHiddenUser)
                {
                    Window.Current.ShowTeachingTip(sender as FrameworkElement, Strings.Resources.HidAccount);
                    //await MessagePopup.ShowAsync(Strings.Resources.HidAccount, Strings.Resources.AppName, Strings.Resources.OK);
                }
            }
            else if (message.ProtoService.TryGetChat(message.SenderId, out Chat senderChat))
            {
                if (senderChat.Type is ChatTypeSupergroup supergroup && supergroup.IsChannel)
                {
                    message.Delegate.OpenChat(senderChat.Id);
                }
                else
                {
                    message.Delegate.OpenChat(senderChat.Id, true);
                }
            }
            else if (message.SenderId is MessageSenderUser senderUser)
            {
                message.Delegate.OpenUser(senderUser.UserId);
            }
        }

        private void UpdateAction(MessageViewModel message)
        {
            var chat = message?.GetChat();
            if (chat == null)
            {
                return;
            }

            var content = message.GeneratedContent ?? message.Content;
            var light = content is MessageSticker or MessageDice or MessageVideoNote or MessageBigEmoji;

            var info = message.InteractionInfo?.ReplyInfo;
            if (info != null && light && message.IsChannelPost && message.CanGetMessageThread)
            {
                if (Action == null)
                {
                    Action = GetTemplateChild(nameof(Action)) as Border;
                    ActionButton = GetTemplateChild(nameof(ActionButton)) as GlyphButton;

                    ActionButton.Click += Action_Click;
                }

                ActionButton.Glyph = Icons.Comment;
                Action.Visibility = Visibility.Visible;

                Automation.SetToolTip(ActionButton, info.ReplyCount > 0
                    ? Locale.Declension("Comments", info.ReplyCount)
                    : Strings.Resources.LeaveAComment);
            }
            else if (message.ChatId == message.ProtoService.Options.RepliesBotChatId && Action != null)
            {
                Action.Visibility = Visibility.Collapsed;
            }
            else if (message.IsSaved)
            {
                if (message.ForwardInfo?.Origin is MessageForwardOriginMessageImport or MessageForwardOriginHiddenUser && Action != null)
                {
                    Action.Visibility = Visibility.Collapsed;
                }
                else
                {
                    if (Action == null)
                    {
                        Action = GetTemplateChild(nameof(Action)) as Border;
                        ActionButton = GetTemplateChild(nameof(ActionButton)) as GlyphButton;

                        ActionButton.Click += Action_Click;
                    }

                    ActionButton.Glyph = Icons.ArrowRight;
                    Action.Visibility = Visibility.Visible;

                    Automation.SetToolTip(ActionButton, Strings.Resources.AccDescrOpenChat);
                }
            }
            else if (message.IsShareable)
            {
                if (Action == null)
                {
                    Action = GetTemplateChild(nameof(Action)) as Border;
                    ActionButton = GetTemplateChild(nameof(ActionButton)) as GlyphButton;

                    ActionButton.Click += Action_Click;
                }

                ActionButton.Glyph = Icons.Share;
                Action.Visibility = Visibility.Visible;

                Automation.SetToolTip(ActionButton, Strings.Resources.ShareFile);
            }
            else if (Action != null)
            {
                Action.Visibility = Visibility.Collapsed;
            }
        }

        private void Action_Click(object sender, RoutedEventArgs e)
        {
            var message = _message;
            if (message == null)
            {
                return;
            }

            var content = message.GeneratedContent ?? message.Content;
            var light = content is MessageSticker or MessageDice or MessageVideoNote or MessageBigEmoji;

            var info = message.InteractionInfo?.ReplyInfo;
            if (info != null && light && message.IsChannelPost && message.CanGetMessageThread)
            {
                message.Delegate.OpenThread(message);
            }
            else if (message.IsSaved)
            {
                if (message.ForwardInfo?.Origin is MessageForwardOriginUser or MessageForwardOriginChat)
                {
                    message.Delegate.OpenChat(message.ForwardInfo.FromChatId, message.ForwardInfo.FromMessageId);
                }
                else if (message.ForwardInfo?.Origin is MessageForwardOriginChannel fromChannel)
                {
                    message.Delegate.OpenChat(fromChannel.ChatId, fromChannel.MessageId);
                }
            }
            else
            {
                message.Delegate.ForwardMessage(message);
            }
        }

        public void UpdateMessageReply(MessageViewModel message)
        {
            if (!_templateApplied)
            {
                return;
            }

            if (Reply == null && message.ReplyToMessageId != 0 && message.ReplyToMessageState != ReplyToMessageState.Hidden)
            {
                Reply = GetTemplateChild(nameof(Reply)) as MessageReference;
                Reply.Click += Reply_Click;
            }

            if (Reply != null)
            {
                Reply.UpdateMessageReply(message);

                if (_playing)
                {
                    Reply.Play();
                }
            }
        }

        public void UpdateMessageHeader(MessageViewModel message)
        {
            if (!_templateApplied)
            {
                return;
            }

            var paragraph = HeaderLabel;
            var admin = AdminLabel;
            var parent = Header;

            paragraph.Inlines.Clear();

            var chat = message?.GetChat();
            if (chat == null)
            {
                return;
            }

            var content = message.GeneratedContent ?? message.Content;

            var light = content is MessageSticker or MessageDice or MessageVideoNote or MessageBigEmoji;
            var shown = false;

            if (!light && message.IsFirst && !message.IsOutgoing && !message.IsChannelPost && (chat.Type is ChatTypeBasicGroup || chat.Type is ChatTypeSupergroup))
            {
                if (message.IsSaved)
                {
                    var title = string.Empty;
                    var foreground = default(SolidColorBrush);

                    if (message.ForwardInfo?.Origin is MessageForwardOriginUser fromUser)
                    {
                        title = message.ProtoService.GetUser(fromUser.SenderUserId)?.GetFullName();
                        foreground = PlaceholderHelper.GetBrush(fromUser.SenderUserId);
                    }
                    else if (message.ForwardInfo?.Origin is MessageForwardOriginChat fromChat)
                    {
                        title = message.ProtoService.GetTitle(message.ProtoService.GetChat(fromChat.SenderChatId));
                    }
                    else if (message.ForwardInfo?.Origin is MessageForwardOriginChannel fromChannel)
                    {
                        title = message.ProtoService.GetTitle(message.ProtoService.GetChat(fromChannel.ChatId));
                    }
                    else if (message.ForwardInfo?.Origin is MessageForwardOriginMessageImport fromImport)
                    {
                        title = fromImport.SenderName;
                    }
                    else if (message.ForwardInfo?.Origin is MessageForwardOriginHiddenUser fromHiddenUser)
                    {
                        title = fromHiddenUser.SenderName;
                    }

                    var hyperlink = new Hyperlink();
                    hyperlink.Inlines.Add(CreateRun(title ?? string.Empty));
                    hyperlink.UnderlineStyle = UnderlineStyle.None;
                    hyperlink.Click += (s, args) => FwdFrom_Click(message);

                    if (foreground != null)
                    {
                        hyperlink.Foreground = foreground;
                    }

                    paragraph.Inlines.Add(hyperlink);
                    shown = true;
                }
                else if (message.ProtoService.TryGetUser(message.SenderId, out User senderUser))
                {
                    var hyperlink = new Hyperlink();
                    hyperlink.Inlines.Add(CreateRun(senderUser.GetFullName()));
                    hyperlink.UnderlineStyle = UnderlineStyle.None;
                    hyperlink.Foreground = PlaceholderHelper.GetBrush(senderUser.Id);
                    hyperlink.Click += (s, args) => From_Click(message);

                    paragraph.Inlines.Add(hyperlink);
                    shown = true;
                }
                else if (message.ProtoService.TryGetChat(message.SenderId, out Chat senderChat))
                {
                    var hyperlink = new Hyperlink();
                    hyperlink.Inlines.Add(CreateRun(senderChat.Title));
                    hyperlink.UnderlineStyle = UnderlineStyle.None;
                    hyperlink.Foreground = PlaceholderHelper.GetBrush(senderChat.Id);
                    hyperlink.Click += (s, args) => From_Click(message);

                    paragraph.Inlines.Add(hyperlink);
                    shown = true;
                }
            }
            else if (!light && message.IsChannelPost && chat.Type is ChatTypeSupergroup && string.IsNullOrEmpty(message.ForwardInfo?.PublicServiceAnnouncementType))
            {
                var hyperlink = new Hyperlink();
                hyperlink.Inlines.Add(CreateRun(message.ProtoService.GetTitle(chat)));
                hyperlink.UnderlineStyle = UnderlineStyle.None;
                //hyperlink.Foreground = Convert.Bubble(message.ChatId);
                hyperlink.Click += (s, args) => From_Click(message);

                paragraph.Inlines.Add(hyperlink);
                shown = false;
            }
            else if (!light && message.IsFirst && message.IsSaved)
            {
                var title = string.Empty;
                var foreground = default(SolidColorBrush);

                if (message.ForwardInfo?.Origin is MessageForwardOriginUser fromUser)
                {
                    title = message.ProtoService.GetUser(fromUser.SenderUserId)?.GetFullName();
                    foreground = PlaceholderHelper.GetBrush(fromUser.SenderUserId);
                }
                else if (message.ForwardInfo?.Origin is MessageForwardOriginChat fromChat)
                {
                    title = message.ProtoService.GetTitle(message.ProtoService.GetChat(fromChat.SenderChatId));
                }
                else if (message.ForwardInfo?.Origin is MessageForwardOriginChannel fromChannel)
                {
                    title = message.ProtoService.GetTitle(message.ProtoService.GetChat(fromChannel.ChatId));
                }
                else if (message.ForwardInfo?.Origin is MessageForwardOriginMessageImport fromImport)
                {
                    title = fromImport.SenderName;
                }
                else if (message.ForwardInfo?.Origin is MessageForwardOriginHiddenUser fromHiddenUser)
                {
                    title = fromHiddenUser.SenderName;
                }

                var hyperlink = new Hyperlink();
                hyperlink.Inlines.Add(CreateRun(title ?? string.Empty));
                hyperlink.UnderlineStyle = UnderlineStyle.None;
                hyperlink.Click += (s, args) => FwdFrom_Click(message);

                if (foreground != null)
                {
                    hyperlink.Foreground = foreground;
                }

                paragraph.Inlines.Add(hyperlink);
                shown = true;
            }

            if (shown)
            {
                var title = message.Delegate.GetAdminTitle(message);
                if (title != null)
                {
                    if (admin != null && !message.IsOutgoing)
                    {
                        paragraph.Inlines.Add(new Run { Text = " " + title, Foreground = null });
                    }
                }
                else if (message.ForwardInfo != null && !message.IsChannelPost)
                {
                    paragraph.Inlines.Add(new Run { Text = " " + Strings.Resources.DiscussChannel, Foreground = null });
                }
            }

            var forward = false;

            if (message.ForwardInfo != null && !message.IsSaved)
            {
                if (paragraph.Inlines.Count > 0)
                {
                    paragraph.Inlines.Add(new LineBreak());
                }

                if (message.ForwardInfo.PublicServiceAnnouncementType.Length > 0)
                {
                    var type = LocaleService.Current.GetString("PsaMessage_" + message.ForwardInfo.PublicServiceAnnouncementType);
                    if (type.Length > 0)
                    {
                        paragraph.Inlines.Add(CreateRun(type, FontWeights.Normal));
                    }
                    else
                    {
                        paragraph.Inlines.Add(CreateRun(Strings.Resources.PsaMessageDefault, FontWeights.Normal));
                    }

                    if (PsaInfo == null)
                    {
                        PsaInfo = GetTemplateChild(nameof(PsaInfo)) as GlyphButton;
                        PsaInfo.Click += PsaInfo_Click;
                    }

                    PsaInfo.Visibility = Visibility.Visible;
                }
                else
                {
                    paragraph.Inlines.Add(CreateRun(Strings.Resources.ForwardedMessage, FontWeights.Normal));

                    if (PsaInfo != null)
                    {
                        PsaInfo.Visibility = Visibility.Collapsed;
                    }
                }

                paragraph.Inlines.Add(new LineBreak());
                paragraph.Inlines.Add(CreateRun(Strings.Resources.From + " ", FontWeights.Normal));

                var title = string.Empty;
                var bold = true;

                if (message.ForwardInfo?.Origin is MessageForwardOriginUser fromUser)
                {
                    title = message.ProtoService.GetUser(fromUser.SenderUserId)?.GetFullName();
                }
                else if (message.ForwardInfo?.Origin is MessageForwardOriginChat fromChat)
                {
                    title = message.ProtoService.GetTitle(message.ProtoService.GetChat(fromChat.SenderChatId));
                }
                else if (message.ForwardInfo?.Origin is MessageForwardOriginChannel fromChannel)
                {
                    title = message.ProtoService.GetTitle(message.ProtoService.GetChat(fromChannel.ChatId));
                }
                else if (message.ForwardInfo?.Origin is MessageForwardOriginMessageImport fromImport)
                {
                    title = fromImport.SenderName;
                }
                else if (message.ForwardInfo?.Origin is MessageForwardOriginHiddenUser fromHiddenUser)
                {
                    title = fromHiddenUser.SenderName;
                    bold = false;
                }

                var hyperlink = new Hyperlink();
                hyperlink.Inlines.Add(CreateRun(title, bold ? FontWeights.SemiBold : FontWeights.Normal));
                hyperlink.UnderlineStyle = UnderlineStyle.None;
                hyperlink.Foreground = light ? new SolidColorBrush(Colors.White) : GetBrush("MessageHeaderForegroundBrush");
                hyperlink.Click += (s, args) => FwdFrom_Click(message);

                paragraph.Inlines.Add(hyperlink);
                forward = true;
            }
            else if (PsaInfo != null)
            {
                PsaInfo.Visibility = Visibility.Collapsed;
            }

            //if (message.HasViaBotId && message.ViaBot != null && !message.ViaBot.IsDeleted && message.ViaBot.HasUsername)
            var viaBot = message.ProtoService.GetUser(message.ViaBotUserId);
            if (viaBot != null && viaBot.Type is UserTypeBot && !string.IsNullOrEmpty(viaBot.Username))
            {
                var hyperlink = new Hyperlink();
                hyperlink.Inlines.Add(CreateRun(paragraph.Inlines.Count > 0 ? " via @" : "via @", FontWeights.Normal));
                hyperlink.Inlines.Add(CreateRun(viaBot.Username));
                hyperlink.UnderlineStyle = UnderlineStyle.None;
                hyperlink.Foreground = light ? new SolidColorBrush(Colors.White) : GetBrush("MessageHeaderForegroundBrush");
                hyperlink.Click += (s, args) => ViaBot_Click(message);

                if (paragraph.Inlines.Count > 0 && !forward)
                {
                    paragraph.Inlines.Insert(1, hyperlink);
                }
                else
                {
                    paragraph.Inlines.Add(hyperlink);
                }
            }

            if (paragraph.Inlines.Count > 0)
            {
                var title = message.Delegate.GetAdminTitle(message);
                if (admin != null && shown && !message.IsOutgoing && message.Delegate != null && !string.IsNullOrEmpty(title))
                {
                    admin.Visibility = Visibility.Visible;
                    admin.Text = title;
                }
                else if (admin != null && shown && !message.IsChannelPost && message.SenderId is MessageSenderChat && message.ForwardInfo != null)
                {
                    admin.Visibility = Visibility.Visible;
                    admin.Text = Strings.Resources.DiscussChannel;
                }
                else if (admin != null)
                {
                    admin.Visibility = Visibility.Collapsed;
                }

                paragraph.Inlines.Add(CreateRun(" "));
                paragraph.Visibility = Visibility.Visible;
                parent.Visibility = Visibility.Visible;
            }
            else
            {
                if (admin != null)
                {
                    admin.Visibility = Visibility.Collapsed;
                }

                paragraph.Visibility = Visibility.Collapsed;
                parent.Visibility = (message.ReplyToMessageId != 0 && message.ReplyToMessageState != ReplyToMessageState.Hidden) ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void ViaBot_Click(MessageViewModel message)
        {
            message.Delegate.OpenViaBot(message.ViaBotUserId);
        }

        private void FwdFrom_Click(MessageViewModel message)
        {
            if (message.ForwardInfo?.Origin is MessageForwardOriginUser fromUser)
            {
                message.Delegate.OpenUser(fromUser.SenderUserId);
            }
            else if (message.ForwardInfo?.Origin is MessageForwardOriginChat fromChat)
            {
                message.Delegate.OpenChat(fromChat.SenderChatId, true);
            }
            else if (message.ForwardInfo?.Origin is MessageForwardOriginChannel fromChannel)
            {
                message.Delegate.OpenChat(fromChannel.ChatId, fromChannel.MessageId);
            }
            else if (message.ForwardInfo?.Origin is MessageForwardOriginHiddenUser)
            {
                Window.Current.ShowTeachingTip(HeaderLabel, Strings.Resources.HidAccount);
            }
        }

        private void From_Click(MessageViewModel message)
        {
            if (message.ProtoService.TryGetChat(message.SenderId, out Chat senderChat))
            {
                if (senderChat.Type is ChatTypeSupergroup supergroup && supergroup.IsChannel)
                {
                    message.Delegate.OpenChat(senderChat.Id);
                }
                else
                {
                    message.Delegate.OpenChat(senderChat.Id, true);
                }
            }
            else if (message.SenderId is MessageSenderUser senderUser)
            {
                message.Delegate.OpenUser(senderUser.UserId);
            }
        }

        public void UpdateMessageState(MessageViewModel message)
        {
            if (!_templateApplied)
            {
                return;
            }

            Footer.UpdateMessageState(message);
        }

        public void UpdateMessageEdited(MessageViewModel message)
        {
            if (!_templateApplied)
            {
                return;
            }

            Footer.UpdateMessageEdited(message);
            UpdateMessageReplyMarkup(message);
        }

        private void UpdateMessageReplyMarkup(MessageViewModel message)
        {
            if (message.ReplyMarkup is ReplyMarkupInlineKeyboard)
            {
                if (Markup == null)
                {
                    Markup = GetTemplateChild(nameof(Markup)) as ReplyMarkupPanel;
                    Markup.InlineButtonClick += ReplyMarkup_ButtonClick;
                }

                Markup.Visibility = Visibility.Visible;
                Markup.Update(message, message.ReplyMarkup);
            }
            else
            {
                if (Markup != null)
                {
                    Markup.Visibility = Visibility.Collapsed;
                    Markup.Children.Clear();
                }
            }
        }

        public void UpdateMessageIsPinned(MessageViewModel message)
        {
            if (!_templateApplied)
            {
                return;
            }

            Footer.UpdateMessageIsPinned(message);
        }

        public void UpdateMessageInteractionInfo(MessageViewModel message)
        {
            if (!_templateApplied)
            {
                return;
            }


            Footer.UpdateMessageInteractionInfo(message);
            UpdateMessageReactions(message, false);

            UpdateAction(message);

            var content = message.GeneratedContent ?? message.Content;
            if (content is MessageSticker or MessageDice or MessageVideoNote or MessageBigEmoji)
            {
                if (Thread != null)
                {
                    Thread.Visibility = Visibility.Collapsed;
                }

                return;
            }

            var info = message.InteractionInfo?.ReplyInfo;
            if (info == null || !message.IsChannelPost || !message.CanGetMessageThread)
            {
                if (message.ChatId == message.ProtoService.Options.RepliesBotChatId)
                {
                    if (Thread == null)
                    {
                        Thread = GetTemplateChild(nameof(Thread)) as HyperlinkButton;
                        RecentRepliers = GetTemplateChild(nameof(RecentRepliers)) as StackPanel;
                        ThreadGlyph = GetTemplateChild(nameof(ThreadGlyph)) as TextBlock;
                        ThreadLabel = GetTemplateChild(nameof(ThreadLabel)) as TextBlock;

                        Thread.Click += Thread_Click;
                    }

                    RecentRepliers.Children.Clear();
                    ThreadGlyph.Visibility = Visibility.Visible;
                    ThreadLabel.Text = Strings.Resources.ViewInChat;

                    AutomationProperties.SetName(Thread, Strings.Resources.ViewInChat);

                    Thread.Visibility = Visibility.Visible;
                }
                else if (Thread != null)
                {
                    Thread.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                if (Thread == null)
                {
                    Thread = GetTemplateChild(nameof(Thread)) as HyperlinkButton;
                    RecentRepliers = GetTemplateChild(nameof(RecentRepliers)) as StackPanel;
                    ThreadGlyph = GetTemplateChild(nameof(ThreadGlyph)) as TextBlock;
                    ThreadLabel = GetTemplateChild(nameof(ThreadLabel)) as TextBlock;

                    Thread.Click += Thread_Click;
                }

                RecentRepliers.Children.Clear();

                foreach (var sender in info.RecentReplierIds)
                {
                    var picture = new ProfilePicture();
                    picture.Width = 24;
                    picture.Height = 24;
                    picture.IsEnabled = false;

                    if (message.ProtoService.TryGetUser(sender, out User user))
                    {
                        picture.SetUser(message.ProtoService, user, 24);
                    }
                    else if (message.ProtoService.TryGetChat(sender, out Chat chat))
                    {
                        picture.SetChat(message.ProtoService, chat, 24);
                    }

                    if (RecentRepliers.Children.Count > 0)
                    {
                        picture.Margin = new Thickness(-10, 0, 0, 0);
                    }

                    Canvas.SetZIndex(picture, -RecentRepliers.Children.Count);
                    RecentRepliers.Children.Add(picture);
                }

                ThreadGlyph.Visibility = RecentRepliers.Children.Count > 0
                    ? Visibility.Collapsed
                    : Visibility.Visible;

                ThreadLabel.Text = info.ReplyCount > 0
                    ? Locale.Declension("Comments", info.ReplyCount)
                    : Strings.Resources.LeaveAComment;

                AutomationProperties.SetName(Thread, info.ReplyCount > 0
                    ? Locale.Declension("Comments", info.ReplyCount)
                    : Strings.Resources.LeaveAComment);

                Thread.Visibility = Visibility.Visible;
            }
        }

        public void UpdateMessageReactions(MessageViewModel message, bool? animate)
        {
            var media = Grid.GetRow(Media);
            var footer = Grid.GetRow(Footer);

            var content = message.GeneratedContent ?? message.Content;
            if (content is MessageSticker or MessageDice or MessageVideoNote or MessageBigEmoji || (media == footer && IsFullMedia(content)))
            {
                Reactions.UpdateMessageReactions(null);

                MediaReactions ??= GetTemplateChild(nameof(MediaReactions)) as ReactionsPanel;
                MediaReactions.UpdateMessageReactions(message, animate);
            }
            else
            {
                Reactions.UpdateMessageReactions(message, animate);

                if (MediaReactions != null)
                {
                    XamlMarkupHelper.UnloadObject(MediaReactions);
                    MediaReactions = null;
                }
            }
        }

        public void UpdateMessageContentOpened(MessageViewModel message)
        {
            if (!_templateApplied)
            {
                return;
            }

            if (Media.Child is IContentWithFile content && content.IsValid(message.GeneratedContent ?? message.Content, true))
            {
                content.UpdateMessageContentOpened(message);
            }
        }

        public void UpdateMessageContent(MessageViewModel message)
        {
            if (!_templateApplied)
            {
                return;
            }

            Panel.Content = message?.GeneratedContent ?? message?.Content;

            var content = message.GeneratedContent ?? message.Content;
            if (content is MessageText text && text.WebPage == null)
            {
                ContentPanel.Padding = new Thickness(0, 4, 0, 0);
                Media.Margin = new Thickness(0);
                FooterToNormal();
                Grid.SetRow(Footer, 2);
                Grid.SetRow(Message, 2);
                Panel.Placeholder = true;
            }
            else if (IsFullMedia(content))
            {
                var top = 0;
                var bottom = 0;

                var chat = message.GetChat();
                if (message.IsFirst && !message.IsOutgoing && !message.IsChannelPost && (chat.Type is ChatTypeBasicGroup || chat.Type is ChatTypeSupergroup))
                {
                    top = 4;
                }
                if (message.IsFirst && message.IsSaved)
                {
                    top = 4;
                }
                if ((message.ForwardInfo != null && !message.IsSaved) || message.ViaBotUserId != 0 || (message.ReplyToMessageId != 0 && message.ReplyToMessageState != ReplyToMessageState.Hidden) || message.IsChannelPost)
                {
                    top = 4;
                }

                var caption = content is MessageVenue || content.HasCaption();
                if (caption)
                {
                    FooterToNormal();
                    bottom = 4;
                }
                else if (content is MessageCall || (content is MessageLocation location && location.LivePeriod > 0 && Converter.DateTime(message.Date + location.LivePeriod) > DateTime.Now))
                {
                    FooterToHidden();
                }
                else
                {
                    FooterToMedia();
                }

                ContentPanel.Padding = new Thickness(0, top, 0, 0);
                Media.Margin = new Thickness(0, top, 0, bottom);
                Grid.SetRow(Footer, caption ? 4 : 3);
                Grid.SetRow(Message, caption ? 4 : 2);
                Panel.Placeholder = caption;
            }
            else if (content is MessageSticker or MessageDice or MessageVideoNote or MessageBigEmoji)
            {
                ContentPanel.Padding = new Thickness(0);
                Media.Margin = new Thickness(0);

                if (message.IsOutgoing && !message.IsChannelPost)
                {
                    FooterToLightMedia(true);
                    Grid.SetRow(Footer, 3);
                    Grid.SetRow(Message, 2);
                    Panel.Placeholder = false;
                }
                else
                {
                    FooterToLightMedia(false);
                    Grid.SetRow(Footer, content is MessageBigEmoji ? 2 : 3);
                    Grid.SetRow(Message, 2);
                    Panel.Placeholder = content is MessageBigEmoji;
                }
            }
            else if ((content is MessageText webPage && webPage.WebPage != null) || content is MessageGame)
            {
                ContentPanel.Padding = new Thickness(0, 4, 0, 0);
                Media.Margin = new Thickness(10, -6, 10, 0);
                FooterToNormal();
                Grid.SetRow(Footer, 4);
                Grid.SetRow(Message, 2);
                Panel.Placeholder = false;
            }
            else if (content is MessagePoll)
            {
                ContentPanel.Padding = new Thickness(0, 4, 0, 0);
                Media.Margin = new Thickness(0);
                FooterToNormal();
                Grid.SetRow(Footer, 4);
                Grid.SetRow(Message, 2);
                Panel.Placeholder = false;
            }
            else if (content is MessageInvoice invoice)
            {
                var caption = invoice.Photo == null;

                ContentPanel.Padding = new Thickness(0, 4, 0, 0);
                Media.Margin = new Thickness(10, 0, 10, 6);
                FooterToNormal();
                Grid.SetRow(Footer, caption ? 3 : 4);
                Grid.SetRow(Message, 2);
                Panel.Placeholder = caption;
            }
            else if (content is MessageContact)
            {
                ContentPanel.Padding = new Thickness(0, 4, 0, 0);
                Media.Margin = new Thickness(10, 4, 10, 0);
                FooterToNormal();
                Grid.SetRow(Footer, 4);
                Grid.SetRow(Message, 2);
                Panel.Placeholder = false;
            }
            else
            {
                var caption = content.HasCaption();
                if (content is MessageCall)
                {
                    FooterToHidden();
                }
                else
                {
                    FooterToNormal();
                }

                ContentPanel.Padding = new Thickness(0, 4, 0, 0);
                Media.Margin = new Thickness(10, 4, 10, 8);
                Grid.SetRow(Footer, caption ? 4 : 3);
                Grid.SetRow(Message, caption ? 4 : 2);
                Panel.Placeholder = caption;
            }

            UpdateMessageText(message);

            if (Media.Child is IContent media && media.IsValid(content, true))
            {
                media.UpdateMessage(message);
            }
            else
            {
                if (content is MessageText textMessage && textMessage.WebPage != null)
                {
                    if (textMessage.WebPage.IsSmallPhoto())
                    {
                        Media.Child = new WebPageSmallPhotoContent(message);
                    }
                    else
                    {
                        Media.Child = new WebPageContent(message);
                    }
                }
                else if (content is MessageAlbum)
                {
                    Media.Child = new AlbumContent(message);
                }
                else if (content is MessageAnimation)
                {
                    Media.Child = new AnimationContent(message);
                }
                else if (content is MessageAudio)
                {
                    Media.Child = new AudioContent(message);
                }
                else if (content is MessageCall)
                {
                    Media.Child = new CallContent(message);
                }
                else if (content is MessageContact)
                {
                    Media.Child = new ContactContent(message);
                }
                else if (content is MessageDice)
                {
                    Media.Child = new DiceContent(message);
                }
                else if (content is MessageDocument)
                {
                    Media.Child = new DocumentContent(message);
                }
                else if (content is MessageGame)
                {
                    Media.Child = new GameContent(message);
                }
                else if (content is MessageInvoice invoice)
                {
                    if (invoice.Photo == null)
                    {
                        Media.Child = new InvoiceContent(message);
                    }
                    else
                    {
                        Media.Child = new InvoicePhotoContent(message);
                    }
                }
                else if (content is MessageLocation)
                {
                    Media.Child = new LocationContent(message);
                }
                else if (content is MessagePhoto)
                {
                    Media.Child = new PhotoContent(message);
                }
                else if (content is MessagePoll)
                {
                    Media.Child = new PollContent(message);
                }
                else if (content is MessageSticker sticker)
                {
                    if (sticker.Sticker.Format is StickerFormatTgs)
                    {
                        Media.Child = new AnimatedStickerContent(message);
                    }
                    else if (sticker.Sticker.Format is StickerFormatWebm)
                    {
                        Media.Child = new VideoStickerContent(message);
                    }
                    else
                    {
                        Media.Child = new StickerContent(message);
                    }
                }
                else if (content is MessageVenue)
                {
                    Media.Child = new VenueContent(message);
                }
                else if (content is MessageVideo)
                {
                    Media.Child = new VideoContent(message);
                }
                else if (content is MessageVideoNote)
                {
                    Media.Child = new VideoNoteContent(message);
                }
                else if (content is MessageVoiceNote)
                {
                    Media.Child = new VoiceNoteContent(message);
                }
                else if (content is MessageAnimatedEmoji)
                {
                    Media.Child = new Border
                    {
                        Width = 180 * message.ProtoService.Config.GetNamedNumber("emojies_animated_zoom", 0.625f),
                        Height = 180 * message.ProtoService.Config.GetNamedNumber("emojies_animated_zoom", 0.625f)
                    };
                }
                else
                {
                    Media.Child = null;
                }
            }
        }

        public IPlayerView GetPlaybackElement()
        {
            if (Media?.Child is IContentWithPlayback content)
            {
                return content.GetPlaybackElement();
            }
            else if (Media?.Child is IPlayerView playback)
            {
                return playback;
            }

            return null;
        }

        public IPlayerView GetCustomEmoji() => CustomEmoji;

        private void UpdateMessageText(MessageViewModel message)
        {
            Message.Blocks.Clear();

            var result = false;
            var adjust = false;
            var cleanup = false;

            var content = message.GeneratedContent ?? message.Content;
            if (content is MessageText text)
            {
                result = ReplaceEntities(message, Message, text.Text, out adjust);
            }
            else if (content is MessageAlbum album)
            {
                result = ReplaceEntities(message, Message, album.Caption, out adjust);
            }
            else if (content is MessageAnimation animation)
            {
                result = ReplaceEntities(message, Message, animation.Caption, out adjust);
            }
            else if (content is MessageAudio audio)
            {
                result = ReplaceEntities(message, Message, audio.Caption, out adjust);
            }
            else if (content is MessageDocument document)
            {
                result = ReplaceEntities(message, Message, document.Caption, out adjust);
            }
            else if (content is MessagePhoto photo)
            {
                result = ReplaceEntities(message, Message, photo.Caption, out adjust);
            }
            else if (content is MessageVideo video)
            {
                result = ReplaceEntities(message, Message, video.Caption, out adjust);
            }
            else if (content is MessageVoiceNote voiceNote)
            {
                result = ReplaceEntities(message, Message, voiceNote.Caption, out adjust);
            }
            else if (content is MessageUnsupported)
            {
                result = GetEntities(message, Message, Strings.Resources.UnsupportedMedia, out adjust);
                cleanup = true;
            }
            else if (content is MessageVenue venue)
            {
                var paragraph = new Paragraph();
                paragraph.Inlines.Add(CreateRun(venue.Venue.Title, FontWeights.SemiBold));
                paragraph.Inlines.Add(new LineBreak());
                paragraph.Inlines.Add(CreateRun(venue.Venue.Address));

                Message.Blocks.Add(paragraph);
                result = true;
                cleanup = true;
            }
            else if (content is MessageBigEmoji bigEmoji)
            {
                var paragraph = new Paragraph();
                paragraph.Inlines.Add(new Run { Text = bigEmoji.Text.Text, FontSize = 32 });

                Message.Blocks.Clear();
                Message.Blocks.Add(paragraph);
                //result = ReplaceEntities(message, Message, bigEmoji.Text, out adjust, 32);
                result = true;
                cleanup = true;
            }

            Message.Visibility = result ? Visibility.Visible : Visibility.Collapsed;
            //Footer.HorizontalAlignment = adjust ? HorizontalAlignment.Left : HorizontalAlignment.Right;

            if (adjust && Message.Blocks.Count > 0 && Message.Blocks[0] is Paragraph existing)
            {
                existing.Inlines.Add(new LineBreak());
            }

            if (cleanup && CustomEmoji != null)
            {
                _positions.Clear();

                _ignoreLayoutUpdated = true;
                Message.LayoutUpdated -= OnLayoutUpdated;

                XamlMarkupHelper.UnloadObject(CustomEmoji);
                CustomEmoji = null;
            }
        }

        private bool GetEntities(MessageViewModel message, RichTextBlock textBlock, string text, out bool adjust)
        {
            if (string.IsNullOrEmpty(text))
            {
                //Message.Visibility = Visibility.Collapsed;
                adjust = false;
                return false;
            }
            else
            {
                //Message.Visibility = Visibility.Visible;

                var response = message.ProtoService.Execute(new GetTextEntities(text));
                if (response is TextEntities entities)
                {
                    return ReplaceEntities(message, textBlock, text, entities.Entities, out adjust);
                }

                var paragraph = new Paragraph();
                paragraph.Inlines.Add(CreateRun(text));

                textBlock.Blocks.Add(paragraph);

                adjust = false;
                return true;
            }
        }

        private bool ReplaceEntities(MessageViewModel message, RichTextBlock textBlock, FormattedText text, out bool adjust, double fontSize = 0)
        {
            if (text == null)
            {
                adjust = false;
                return false;
            }

            return ReplaceEntities(message, textBlock, text.Text, text.Entities, out adjust, fontSize);
        }

        private bool ReplaceEntities(MessageViewModel message, RichTextBlock textBlock, string text, IList<TextEntity> entities, out bool adjust, double fontSize = 0)
        {
            if (string.IsNullOrEmpty(text))
            {
                adjust = false;
                return false;
            }

            _positions.Clear();
            textBlock.TextHighlighters.Clear();
            TextHighlighter spoiler = null;

            var preformatted = false;

            var runs = TextStyleRun.GetRuns(text, entities);
            var previous = 0;

            var shift = 1;
            var close = false;

            var direct = XamlDirect.GetDefault();
            var paragraph = direct.CreateInstance(XamlTypeIndex.Paragraph);
            var inlines = direct.GetXamlDirectObjectProperty(paragraph, XamlPropertyIndex.Paragraph_Inlines);

            if (fontSize > 0)
            {
                direct.SetDoubleProperty(paragraph, XamlPropertyIndex.TextElement_FontSize, fontSize);
            }

            var emojis = new HashSet<long>();

            foreach (var entity in runs)
            {
                if (entity.Offset > previous)
                {
                    direct.AddToCollection(inlines, CreateDirectRun(text.Substring(previous, entity.Offset - previous)));

                    // Run
                    shift++;
                    shift += entity.Offset - previous;

                    shift++;
                }

                if (entity.Length + entity.Offset > text.Length)
                {
                    previous = entity.Offset + entity.Length;
                    continue;
                }

                if (entity.HasFlag(TextStyle.Monospace))
                {
                    var data = text.Substring(entity.Offset, entity.Length);

                    if (message.Delegate.Settings.Diagnostics.CopyFormattedCode && entity.Type is TextEntityTypeCode)
                    {
                        var hyperlink = new Hyperlink();
                        hyperlink.Click += (s, args) => Entity_Click(message, entity.Type, data);
                        hyperlink.Foreground = Message.Foreground;
                        hyperlink.UnderlineStyle = UnderlineStyle.None;

                        hyperlink.Inlines.Add(CreateRun(data, fontFamily: new FontFamily("Consolas")));
                        direct.AddToCollection(inlines, direct.GetXamlDirectObject(hyperlink));

                        // Hyperlink
                        shift++;
                        close = true;

                        // Run
                        shift++;
                        shift += entity.Length;
                    }
                    else
                    {
                        direct.AddToCollection(inlines, CreateDirectRun(data, fontFamily: new FontFamily("Consolas")));
                        preformatted = entity.Type is TextEntityTypePre or TextEntityTypePreCode;

                        // Run
                        shift++;
                        shift += entity.Length;
                    }
                }
                else
                {
                    var local = inlines;

                    if (_ignoreSpoilers is false && entity.HasFlag(TextStyle.Spoiler))
                    {
                        var hyperlink = new Hyperlink();
                        hyperlink.Click += (s, args) => Entity_Click(message, new TextEntityTypeSpoiler(), null);
                        hyperlink.Foreground = Message.Foreground;
                        hyperlink.UnderlineStyle = UnderlineStyle.None;
                        hyperlink.FontFamily = App.Current.Resources["SpoilerFontFamily"] as FontFamily;
                        //hyperlink.Foreground = foreground;

                        spoiler ??= new TextHighlighter();
                        spoiler.Ranges.Add(new TextRange { StartIndex = entity.Offset, Length = entity.Length });

                        var temp = direct.GetXamlDirectObject(hyperlink);

                        direct.AddToCollection(inlines, temp);
                        local = direct.GetXamlDirectObjectProperty(temp, XamlPropertyIndex.Span_Inlines);

                        // Hyperlink
                        shift++;
                        close = true;
                    }
                    else if (entity.HasFlag(TextStyle.Mention) || entity.HasFlag(TextStyle.Url))
                    {
                        if (entity.Type is TextEntityTypeMentionName or TextEntityTypeTextUrl)
                        {
                            var hyperlink = new Hyperlink();
                            object data;
                            if (entity.Type is TextEntityTypeTextUrl textUrl)
                            {
                                data = textUrl.Url;
                                MessageHelper.SetEntityData(hyperlink, textUrl.Url);
                                MessageHelper.SetEntityType(hyperlink, entity.Type);

                                ToolTipService.SetToolTip(hyperlink, textUrl.Url);
                            }
                            else if (entity.Type is TextEntityTypeMentionName mentionName)
                            {
                                data = mentionName.UserId;
                            }

                            hyperlink.Click += (s, args) => Entity_Click(message, entity.Type, null);
                            hyperlink.Foreground = GetBrush("MessageForegroundLinkBrush");
                            //hyperlink.Foreground = foreground;

                            var temp = direct.GetXamlDirectObject(hyperlink);

                            direct.AddToCollection(inlines, temp);
                            local = direct.GetXamlDirectObjectProperty(temp, XamlPropertyIndex.Span_Inlines);
                        }
                        else
                        {
                            var hyperlink = new Hyperlink();
                            var original = entities.FirstOrDefault(x => x.Offset <= entity.Offset && x.Offset + x.Length >= entity.End);

                            var data = text.Substring(entity.Offset, entity.Length);

                            if (original != null)
                            {
                                data = text.Substring(original.Offset, original.Length);
                            }

                            hyperlink.Click += (s, args) => Entity_Click(message, entity.Type, data);
                            hyperlink.Foreground = GetBrush("MessageForegroundLinkBrush");
                            //hyperlink.Foreground = foreground;

                            //if (entity.Type is TextEntityTypeUrl || entity.Type is TextEntityTypeEmailAddress || entity.Type is TextEntityTypeBankCardNumber)
                            {
                                MessageHelper.SetEntityData(hyperlink, data);
                                MessageHelper.SetEntityType(hyperlink, entity.Type);
                            }

                            var temp = direct.GetXamlDirectObject(hyperlink);

                            direct.AddToCollection(inlines, temp);
                            local = direct.GetXamlDirectObjectProperty(temp, XamlPropertyIndex.Span_Inlines);
                        }

                        // Hyperlink
                        shift++;
                        close = true;
                    }

                    if (entity.Type is TextEntityTypeCustomEmoji customEmoji)
                    {
                        // Run
                        shift++;

                        _positions.Add(new EmojiPosition { X = shift, CustomEmojiId = customEmoji.CustomEmojiId });

                        direct.AddToCollection(inlines, CreateDirectRun(text.Substring(entity.Offset, entity.Length), fontFamily: App.Current.Resources["SpoilerFontFamily"] as FontFamily));
                        emojis.Add(customEmoji.CustomEmojiId);

                        shift += entity.Length;
                    }
                    else
                    {
                        var run = CreateDirectRun(text.Substring(entity.Offset, entity.Length));
                        var decorations = TextDecorations.None;

                        if (entity.HasFlag(TextStyle.Underline))
                        {
                            decorations |= TextDecorations.Underline;
                        }
                        if (entity.HasFlag(TextStyle.Strikethrough))
                        {
                            decorations |= TextDecorations.Strikethrough;
                        }

                        if (decorations != TextDecorations.None)
                        {
                            direct.SetEnumProperty(run, XamlPropertyIndex.TextElement_TextDecorations, (uint)decorations);
                        }

                        if (entity.HasFlag(TextStyle.Bold))
                        {
                            direct.SetObjectProperty(run, XamlPropertyIndex.TextElement_FontWeight, FontWeights.SemiBold);
                        }
                        if (entity.HasFlag(TextStyle.Italic))
                        {
                            direct.SetEnumProperty(run, XamlPropertyIndex.TextElement_FontStyle, (uint)FontStyle.Italic);
                        }

                        direct.AddToCollection(local, run);

                        // Run
                        shift++;
                        shift += entity.Length;
                    }
                }

                previous = entity.Offset + entity.Length;
                shift++;

                if (close)
                {
                    shift++;
                    close = false;
                }
            }

            ContentPanel.MaxWidth = preformatted ? double.PositiveInfinity : 432;

            if (text.Length > previous)
            {
                direct.AddToCollection(inlines, CreateDirectRun(text.Substring(previous)));
            }

            if (string.IsNullOrWhiteSpace(_query))
            {
                Message.TextHighlighters.Clear();
            }
            else
            {
                var find = text.IndexOf(_query, StringComparison.OrdinalIgnoreCase);
                if (find != -1)
                {
                    var highligher = new TextHighlighter();
                    highligher.Foreground = new SolidColorBrush(Colors.White);
                    highligher.Background = new SolidColorBrush(Colors.Orange);
                    highligher.Ranges.Add(new TextRange { StartIndex = find, Length = _query.Length });

                    Message.TextHighlighters.Add(highligher);
                }
                else
                {
                    Message.TextHighlighters.Clear();
                }
            }

            if (spoiler?.Ranges.Count > 0)
            {
                spoiler.Foreground = new SolidColorBrush(Colors.Black);
                spoiler.Background = new SolidColorBrush(Colors.Black);

                Message.TextHighlighters.Add(spoiler);
            }

            direct.SetDoubleProperty(paragraph, XamlPropertyIndex.TextElement_FontSize, Theme.Current.MessageFontSize);

            textBlock.Blocks.Clear();
            textBlock.Blocks.Add(direct.GetObject(paragraph) as Paragraph);

            if (LocaleService.Current.FlowDirection == FlowDirection.LeftToRight && MessageHelper.IsAnyCharacterRightToLeft(text))
            {
                Message.FlowDirection = FlowDirection.RightToLeft;
                adjust = true;
            }
            else if (LocaleService.Current.FlowDirection == FlowDirection.RightToLeft && !MessageHelper.IsAnyCharacterRightToLeft(text))
            {
                Message.FlowDirection = FlowDirection.LeftToRight;
                adjust = true;
            }
            else
            {
                Message.FlowDirection = LocaleService.Current.FlowDirection;
                adjust = false;
            }

            Message.LayoutUpdated -= OnLayoutUpdated;

            if (emojis.Count > 0)
            {
                CustomEmoji ??= GetTemplateChild(nameof(CustomEmoji)) as CustomEmojiCanvas;
                CustomEmoji.UpdateEntities(message.ProtoService, emojis);

                if (_playing)
                {
                    CustomEmoji.Play();
                }

                _ignoreLayoutUpdated = false;
                Message.LayoutUpdated += OnLayoutUpdated;
            }
            else if (CustomEmoji != null)
            {
                XamlMarkupHelper.UnloadObject(CustomEmoji);
                CustomEmoji = null;
            }

            return true;
        }

        private void OnLayoutUpdated(object sender, object e)
        {
            if (_ignoreLayoutUpdated)
            {
                return;
            }

            if (_positions.Count > 0)
            {
                _ignoreLayoutUpdated = true;
                LoadCustomEmoji();
            }
            else
            {
                Message.LayoutUpdated -= OnLayoutUpdated;

                if (CustomEmoji != null)
                {
                    XamlMarkupHelper.UnloadObject(CustomEmoji);
                    CustomEmoji = null;
                }
            }
        }

        private void LoadCustomEmoji()
        {
            var positions = new List<EmojiPosition>();

            foreach (var item in _positions)
            {
                var pointer = Message.ContentStart.GetPositionAtOffset(item.X, LogicalDirection.Forward);
                if (pointer == null)
                {
                    continue;
                }

                var rect = pointer.GetCharacterRect(LogicalDirection.Forward);

                positions.Add(new EmojiPosition
                {
                    CustomEmojiId = item.CustomEmojiId,
                    X = (int)rect.X,
                    Y = (int)rect.Y
                });
            }

            if (positions.Count < 1)
            {
                Message.LayoutUpdated -= OnLayoutUpdated;

                if (CustomEmoji != null)
                {
                    XamlMarkupHelper.UnloadObject(CustomEmoji);
                    CustomEmoji = null;
                }
            }
            else
            {
                CustomEmoji ??= GetTemplateChild(nameof(CustomEmoji)) as CustomEmojiCanvas;
                CustomEmoji.UpdatePositions(positions);

                if (_playing)
                {
                    CustomEmoji.Play();
                }
            }
        }

        private Run CreateRun(string text, FontWeight? fontWeight = null, FontFamily fontFamily = null)
        {
            var direct = XamlDirect.GetDefault();
            var run = direct.CreateInstance(XamlTypeIndex.Run);
            direct.SetStringProperty(run, XamlPropertyIndex.Run_Text, text);

            if (fontWeight != null)
            {
                direct.SetObjectProperty(run, XamlPropertyIndex.TextElement_FontWeight, fontWeight.Value);
            }

            if (fontFamily != null)
            {
                direct.SetObjectProperty(run, XamlPropertyIndex.TextElement_FontFamily, fontFamily);
            }

            return direct.GetObject(run) as Run;
        }

        private IXamlDirectObject CreateDirectRun(string text, FontWeight? fontWeight = null, FontFamily fontFamily = null)
        {
            var direct = XamlDirect.GetDefault();
            var run = direct.CreateInstance(XamlTypeIndex.Run);
            direct.SetStringProperty(run, XamlPropertyIndex.Run_Text, text);

            if (fontWeight != null)
            {
                direct.SetObjectProperty(run, XamlPropertyIndex.TextElement_FontWeight, fontWeight.Value);
            }

            if (fontFamily != null)
            {
                direct.SetObjectProperty(run, XamlPropertyIndex.TextElement_FontFamily, fontFamily);
            }

            return run;
        }

        private Brush GetBrush(string key)
        {
            var message = _message;
            if (message == null)
            {
                return null;
            }

            if (message.IsOutgoing && !message.IsChannelPost)
            {
                if (ActualTheme == ElementTheme.Light)
                {
                    return ThemeOutgoing.Light[key].Brush;
                }
                else
                {
                    return ThemeOutgoing.Dark[key].Brush;
                }
            }
            else if (ActualTheme == ElementTheme.Light)
            {
                return ThemeIncoming.Light[key].Brush;
            }
            else
            {
                return ThemeIncoming.Dark[key].Brush;
            }
        }

        private void Entity_Click(MessageViewModel message, TextEntityType type, object data)
        {
            foreach (Paragraph block in Message.Blocks)
            {
                foreach (var element in block.Inlines)
                {
                    if (element is Hyperlink)
                    {
                        ToolTipService.SetToolTip(element, null);
                    }
                }
            }

            if (type is TextEntityTypeBotCommand && data is string command)
            {
                message.Delegate.SendBotCommand(command);
            }
            else if (type is TextEntityTypeEmailAddress)
            {
                message.Delegate.OpenUrl("mailto:" + data, false);
            }
            else if (type is TextEntityTypePhoneNumber)
            {
                message.Delegate.OpenUrl("tel:" + data, false);
            }
            else if (type is TextEntityTypeHashtag or TextEntityTypeCashtag && data is string hashtag)
            {
                message.Delegate.OpenHashtag(hashtag);
            }
            else if (type is TextEntityTypeMention && data is string username)
            {
                message.Delegate.OpenUsername(username);
            }
            else if (type is TextEntityTypeMentionName mentionName)
            {
                message.Delegate.OpenUser(mentionName.UserId);
            }
            else if (type is TextEntityTypeTextUrl textUrl)
            {
                message.Delegate.OpenUrl(textUrl.Url, true);
            }
            else if (type is TextEntityTypeUrl && data is string url)
            {
                message.Delegate.OpenUrl(url, false);
            }
            else if (type is TextEntityTypeBankCardNumber && data is string cardNumber)
            {
                message.Delegate.OpenBankCardNumber(cardNumber);
            }
            else if (type is TextEntityTypeMediaTimestamp mediaTimestamp && message.ReplyToMessage != null)
            {
                message.Delegate.OpenMedia(message.ReplyToMessage, null, mediaTimestamp.MediaTimestamp);
            }
            else if (type is TextEntityTypeCode or TextEntityTypePre or TextEntityTypePreCode && data is string code)
            {
                MessageHelper.CopyText(code);
            }
            else if (type is TextEntityTypeSpoiler)
            {
                _ignoreSpoilers = true;
                UpdateMessageText(message);
            }
        }

        private void FooterToLightMedia(bool isOut)
        {
            VisualStateManager.GoToState(this, "LightState" + (isOut ? "Out" : string.Empty), false);

            if (Reply != null)
            {
                Reply.ToLightState();
            }

            if (BackgroundPanel != null)
            {
                BackgroundPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void FooterToMedia()
        {
            VisualStateManager.GoToState(this, "MediaState", false);

            if (Reply != null)
            {
                Reply.ToNormalState();
            }
        }

        private void FooterToHidden()
        {
            VisualStateManager.GoToState(this, "HiddenState", false);

            if (Reply != null)
            {
                Reply.ToNormalState();
            }
        }

        private void FooterToNormal()
        {
            VisualStateManager.GoToState(this, "Normal", false);

            if (Reply != null)
            {
                Reply.ToNormalState();
            }
        }

        public void RegisterEvents()
        {
            _ignoreSizeChanged = false;
        }

        public void UnregisterEvents()
        {
            _ignoreSizeChanged = true;
        }

        private void UpdateClip()
        {
            if (_cornerRadius.TopLeft == 0 && _cornerRadius.BottomRight == 0)
            {
                _cornerRadius.Left = -float.MaxValue;
                _cornerRadius.Top = -float.MaxValue;
                _cornerRadius.Right = float.MaxValue;
                _cornerRadius.Bottom = float.MaxValue;
            }
            else
            {
                _cornerRadius.Left = 0;
                _cornerRadius.Top = 0;
                _cornerRadius.Right = (float)Math.Truncate(ContentPanel.ActualWidth);
                _cornerRadius.Bottom = (float)Math.Truncate(ContentPanel.ActualHeight);
            }
        }

        public void AnimateSendout(float xScale, float yScale, float fontScale, double outer, double inner, double delay, bool reply)
        {
            if (!_templateApplied)
            {
                return;
            }

            var content = _message?.GeneratedContent ?? _message?.Content;
            var panel = ElementCompositionPreview.GetElementVisual(ContentPanel);

            if (content is MessageText)
            {
                var crossScale = Window.Current.Compositor.CreateVector3KeyFrameAnimation();
                crossScale.InsertKeyFrame(0, new Vector3(1, yScale, 1));
                crossScale.InsertKeyFrame(1, new Vector3(1));
                crossScale.Duration = TimeSpan.FromMilliseconds(outer);
                crossScale.DelayTime = TimeSpan.FromMilliseconds(delay);
                crossScale.DelayBehavior = AnimationDelayBehavior.SetInitialValueBeforeDelay;

                var outOpacity = Window.Current.Compositor.CreateScalarKeyFrameAnimation();
                outOpacity.InsertKeyFrame(0, 1);
                outOpacity.InsertKeyFrame(1, 0);
                outOpacity.Duration = TimeSpan.FromMilliseconds(outer);
                outOpacity.DelayTime = TimeSpan.FromMilliseconds(delay);
                outOpacity.DelayBehavior = AnimationDelayBehavior.SetInitialValueBeforeDelay;

                if (BackgroundPanel == null)
                {
                    BackgroundPanel = GetTemplateChild(nameof(BackgroundPanel)) as Border;
                    CrossPanel = GetTemplateChild(nameof(CrossPanel)) as Border;
                }

                var cross = ElementCompositionPreview.GetElementVisual(CrossPanel);
                cross.StartAnimation("Opacity", outOpacity);

                var background = ElementCompositionPreview.GetElementVisual(BackgroundPanel);
                background.CenterPoint = new Vector3(0, reply ? 0 : ContentPanel.ActualSize.Y / 2, 0);
                background.StartAnimation("Scale", crossScale);

                if (reply)
                {
                    _cornerRadius.AnimateBottom(Window.Current.Compositor, ContentPanel.ActualSize.Y * yScale, ContentPanel.ActualSize.Y, outer / 1000);
                }
                else
                {
                    var scaled = ContentPanel.ActualSize.Y * yScale;
                    var diff = (scaled - ContentPanel.ActualSize.Y) / 2;

                    _cornerRadius.AnimateTop(Window.Current.Compositor, -diff, 0, outer / 1000);
                    _cornerRadius.AnimateBottom(Window.Current.Compositor, ContentPanel.ActualSize.Y + diff, ContentPanel.ActualSize.Y, outer / 1000);
                }
            }

            var header = ElementCompositionPreview.GetElementVisual(Header);
            var text = ElementCompositionPreview.GetElementVisual(Message);
            var media = ElementCompositionPreview.GetElementVisual(Media);
            var footer = ElementCompositionPreview.GetElementVisual(Footer);

            var scale = Window.Current.Compositor.CreateVector3KeyFrameAnimation();
            scale.InsertKeyFrame(0, new Vector3(xScale, 1, 1));
            scale.InsertKeyFrame(1, new Vector3(1));
            scale.Duration = TimeSpan.FromMilliseconds(inner);
            scale.DelayTime = TimeSpan.FromMilliseconds(delay);
            scale.DelayBehavior = AnimationDelayBehavior.SetInitialValueBeforeDelay;

            var factor = Window.Current.Compositor.CreateExpressionAnimation("Vector3(1 / content.Scale.X, 1, 1)");
            factor.SetReferenceParameter("content", panel);

            CompositionAnimation textScale = factor;
            if (fontScale != 1)
            {
                var textScaleImpl = Window.Current.Compositor.CreateScalarKeyFrameAnimation();
                textScaleImpl.InsertKeyFrame(0, fontScale);
                textScaleImpl.InsertKeyFrame(1, 1);
                textScaleImpl.Duration = TimeSpan.FromMilliseconds(outer);
                textScaleImpl.DelayTime = TimeSpan.FromMilliseconds(delay);
                textScaleImpl.DelayBehavior = AnimationDelayBehavior.SetInitialValueBeforeDelay;

                textScale = Window.Current.Compositor.CreateExpressionAnimation("Vector3(this.Scale * (1 / content.Scale.X), this.Scale, 1)");
                textScale.SetReferenceParameter("content", panel);
                textScale.Properties.InsertScalar("Scale", fontScale);
                textScale.Properties.StartAnimation("Scale", textScaleImpl);

                Message.Tag = textScaleImpl;
                Media.Tag = textScale;
            }

            var inOpacity = Window.Current.Compositor.CreateScalarKeyFrameAnimation();
            inOpacity.InsertKeyFrame(0, 0);
            inOpacity.InsertKeyFrame(1, 1);
            inOpacity.Duration = TimeSpan.FromMilliseconds(outer / 3 * 2);
            inOpacity.DelayTime = TimeSpan.FromMilliseconds(outer / 3);
            inOpacity.DelayBehavior = AnimationDelayBehavior.SetInitialValueBeforeDelay;

            var headerLeft = (float)Header.Margin.Left;
            var textLeft = (float)Message.Margin.Left;

            var mediaLeft = (float)Media.Margin.Left;
            var mediaBottom = (float)Media.Margin.Bottom;

            var footerRight = (float)Footer.Margin.Right;
            var footerBottom = (float)Footer.Margin.Bottom;

            header.CenterPoint = new Vector3(-headerLeft, 0, 0);
            text.CenterPoint = new Vector3(-textLeft, Message.ActualSize.Y, 0);
            media.CenterPoint = new Vector3(-mediaLeft, Media.ActualSize.Y + mediaBottom, 0);
            footer.CenterPoint = new Vector3(Footer.ActualSize.X + footerRight, Footer.ActualSize.Y + footerBottom, 0);

            header.StartAnimation("Scale", factor);
            text.StartAnimation("Scale", textScale);
            media.StartAnimation("Scale", textScale);
            footer.StartAnimation("Scale", factor);
            footer.StartAnimation("Opacity", inOpacity);

            var headerOffsetX = content is MessageText ? 10 : 14;
            var headerOffsetY = 0f;

            var textOffsetX = 0f;
            var textOffsetY = 0f;

            if (content is MessageSticker or MessageDice)
            {
                headerOffsetY = reply ? 46 : 0;
                textOffsetX = ContentPanel.ActualSize.X - Media.ActualSize.X; // - 10;
            }
            if (content is MessageBigEmoji)
            {
                headerOffsetY = reply ? -36 : 0;
                textOffsetX = ContentPanel.ActualSize.X - Message.ActualSize.X; //- 10;
            }
            else if (content is MessageText)
            {
                textOffsetY = reply ? 16 : 0;
            }

            var headerOffset = Window.Current.Compositor.CreateVector3KeyFrameAnimation();
            headerOffset.InsertKeyFrame(0, new Vector3(-(headerOffsetX * (1 / xScale)), headerOffsetY, 0));
            headerOffset.InsertKeyFrame(1, new Vector3(0));
            headerOffset.Duration = TimeSpan.FromMilliseconds(headerOffsetY > 0 ? outer : inner);
            headerOffset.DelayTime = TimeSpan.FromMilliseconds(delay);
            headerOffset.DelayBehavior = AnimationDelayBehavior.SetInitialValueBeforeDelay;
            header.StartAnimation("Translation", headerOffset);

            var textOffset = Window.Current.Compositor.CreateVector3KeyFrameAnimation();
            textOffset.InsertKeyFrame(0, new Vector3(-textOffsetX, textOffsetY, 0));
            textOffset.InsertKeyFrame(1, new Vector3());
            textOffset.Duration = TimeSpan.FromMilliseconds(textOffsetY > 0 ? outer : inner);
            textOffset.DelayTime = TimeSpan.FromMilliseconds(delay);
            textOffset.DelayBehavior = AnimationDelayBehavior.SetInitialValueBeforeDelay;

            if (content is MessageSticker or MessageDice)
            {
                media.StartAnimation("Translation", textOffset);
            }
            else
            {
                text.StartAnimation("Translation", textOffset);
            }

            panel.CenterPoint = new Vector3(ContentPanel.ActualSize, 0);
            panel.StartAnimation("Scale", scale);
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            _ignoreLayoutUpdated = false;
            UpdateClip();

            var message = _message;
            if (message == null || e.PreviousSize.Width < 1 || e.PreviousSize.Height < 1 || _ignoreSizeChanged)
            {
                return;
            }

            var content = message.GeneratedContent ?? message.Content;
            if (content is MessageSticker or MessageDice or MessageVideoNote or MessageBigEmoji)
            {
                return;
            }

            var prev = e.PreviousSize.ToVector2();
            var next = e.NewSize.ToVector2();

            var outgoing = message.IsOutgoing && !message.IsChannelPost;

            var anim = Window.Current.Compositor.CreateVector3KeyFrameAnimation();
            anim.InsertKeyFrame(0, new Vector3(prev / next, 1));
            anim.InsertKeyFrame(1, Vector3.One);

            var panel = ElementCompositionPreview.GetElementVisual(ContentPanel);
            panel.CenterPoint = new Vector3(outgoing ? next.X : 0, 0, 0);
            panel.StartAnimation("Scale", anim);

            var factor = Window.Current.Compositor.CreateExpressionAnimation("Vector3(1 / content.Scale.X, 1 / content.Scale.Y, 1)");
            factor.SetReferenceParameter("content", panel);

            var header = ElementCompositionPreview.GetElementVisual(Header);
            var text = ElementCompositionPreview.GetElementVisual(Message);
            var media = ElementCompositionPreview.GetElementVisual(Media);
            var footer = ElementCompositionPreview.GetElementVisual(Footer);
            var reactions = ElementCompositionPreview.GetElementVisual(Reactions);

            var headerLeft = (float)Header.Margin.Left;
            var textLeft = (float)Message.Margin.Left;
            var mediaLeft = (float)Media.Margin.Left;

            var footerRight = (float)Footer.Margin.Right;
            var footerBottom = (float)Footer.Margin.Bottom;

            header.CenterPoint = new Vector3(-headerLeft, 0, 0);
            text.CenterPoint = new Vector3(-textLeft, 0, 0);
            media.CenterPoint = new Vector3(-mediaLeft, 0, 0);
            footer.CenterPoint = new Vector3(Footer.ActualSize.X + footerRight, Footer.ActualSize.Y + footerBottom, 0);
            reactions.CenterPoint = new Vector3(0, Reactions.ActualSize.Y, 0);

            header.StartAnimation("Scale", factor);
            text.StartAnimation("Scale", factor);
            media.StartAnimation("Scale", factor);
            footer.StartAnimation("Scale", factor);
            reactions.StartAnimation("Scale", factor);
        }

        private void Footer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.PreviousSize.Width > 0 && e.NewSize.Width != e.PreviousSize.Width)
            {
                Panel.InvalidateMeasure();
            }
        }

        private SpriteVisual _highlight;

        public void Highlight()
        {
            var message = _message;
            if (message == null)
            {
                return;
            }

            var overlay = _highlight;
            if (overlay == null)
            {
                _highlight = overlay = Window.Current.Compositor.CreateSpriteVisual();
            }

            FrameworkElement target;
            if (Media.Child is IContentWithMask)
            {
                ElementCompositionPreview.SetElementChildVisual(ContentPanel, null);
                ElementCompositionPreview.SetElementChildVisual(Media, overlay);
                target = Media;
            }
            else
            {
                ElementCompositionPreview.SetElementChildVisual(Media, null);
                ElementCompositionPreview.SetElementChildVisual(ContentPanel, overlay);
                target = ContentPanel;
            }

            //Media.Content is IContentWithMask ? Media : (FrameworkElement)ContentPanel;

            //var overlay = ElementCompositionPreview.GetElementChildVisual(target) as SpriteVisual;
            //if (overlay == null)
            //{
            //    overlay = ElementCompositionPreview.GetElementVisual(this).Compositor.CreateSpriteVisual();
            //    ElementCompositionPreview.SetElementChildVisual(target, overlay);
            //}

            var settings = new UISettings();
            var fill = overlay.Compositor.CreateColorBrush(settings.GetColorValue(UIColorType.Accent));
            var brush = (CompositionBrush)fill;

            if (Media.Child is IContentWithMask withMask)
            {
                var alpha = withMask.GetAlphaMask();
                if (alpha != null)
                {
                    var mask = overlay.Compositor.CreateMaskBrush();
                    mask.Source = brush;
                    mask.Mask = alpha;

                    brush = mask;
                }
            }

            overlay.Size = target.ActualSize;
            overlay.Opacity = 0f;
            overlay.Brush = brush;

            var animation = overlay.Compositor.CreateScalarKeyFrameAnimation();
            animation.Duration = TimeSpan.FromSeconds(2);
            animation.InsertKeyFrame(300f / 2000f, 0.4f);
            animation.InsertKeyFrame(1700f / 2000f, 0.4f);
            animation.InsertKeyFrame(1, 0);

            overlay.StartAnimation("Opacity", animation);
        }

        #region Actions

        private void PsaInfo_Click(object sender, RoutedEventArgs e)
        {
            var message = _message;
            if (message == null)
            {
                return;
            }

            var type = LocaleService.Current.GetString("PsaMessageInfo_" + message.ForwardInfo.PublicServiceAnnouncementType);
            if (string.IsNullOrEmpty(type))
            {
                type = Strings.Resources.PsaMessageInfoDefault;
            }

            var entities = message.ProtoService.Execute(new GetTextEntities(type)) as TextEntities;
            Window.Current.ShowTeachingTip(PsaInfo, new FormattedText(type, entities.Entities), TeachingTipPlacementMode.TopLeft);
        }

        private void Thread_Click(object sender, RoutedEventArgs e)
        {
            var message = _message;
            if (message == null)
            {
                return;
            }

            message.Delegate.OpenThread(message);
        }

        private void Reply_Click(object sender, RoutedEventArgs e)
        {
            var message = _message;
            if (message == null)
            {
                return;
            }

            message.Delegate.OpenReply(message);
        }

        private void ReplyMarkup_ButtonClick(object sender, ReplyMarkupInlineButtonClickEventArgs e)
        {
            var message = _message;
            if (message == null)
            {
                return;
            }

            message.Delegate.OpenInlineButton(message, e.Button);
        }

        #endregion

        public void Mockup(string message, bool outgoing, DateTime date, bool first = true, bool last = true)
        {
            if (!_templateApplied)
            {
                void loaded(object o, RoutedEventArgs e)
                {
                    Loaded -= loaded;
                    Mockup(message, outgoing, date, first, last);
                }

                Loaded += loaded;
                return;
            }

            UpdateMockup(outgoing, first, last);

            Header.Visibility = Visibility.Collapsed;

            Footer.Mockup(outgoing, date);
            Panel.Content = new MessageText { Text = new FormattedText(message, new TextEntity[0]) };

            Media.Margin = new Thickness(0);
            FooterToNormal();
            Grid.SetRow(Footer, 2);
            Grid.SetRow(Message, 2);
            Panel.Placeholder = true;

            var paragraph = new Paragraph();
            paragraph.Inlines.Add(CreateRun(message));

            Message.Blocks.Clear();
            Message.Blocks.Add(paragraph);

            if (LocaleService.Current.FlowDirection == FlowDirection.LeftToRight && MessageHelper.IsAnyCharacterRightToLeft(message))
            {
                paragraph.Inlines.Add(new LineBreak());
            }
            else if (LocaleService.Current.FlowDirection == FlowDirection.RightToLeft && !MessageHelper.IsAnyCharacterRightToLeft(message))
            {
                paragraph.Inlines.Add(new LineBreak());
            }

            UpdateMockup();
        }

        public void Mockup(string message, string forwarded, bool link, bool outgoing, DateTime date, bool first = true, bool last = true)
        {
            if (!_templateApplied)
            {
                void loaded(object o, RoutedEventArgs e)
                {
                    Loaded -= loaded;
                    Mockup(message, forwarded, link, outgoing, date, first, last);
                }

                Loaded += loaded;
                return;
            }

            UpdateMockup(outgoing, first, last);

            Header.Visibility = Visibility.Collapsed;

            Footer.Mockup(outgoing, date);
            Panel.Content = new MessageText { Text = new FormattedText(message, new TextEntity[0]) };

            Media.Margin = new Thickness(0);
            FooterToNormal();
            Grid.SetRow(Footer, 2);
            Grid.SetRow(Message, 2);
            Panel.Placeholder = true;

            var paragraph = new Paragraph();
            paragraph.Inlines.Add(CreateRun(message));

            Message.Blocks.Clear();
            Message.Blocks.Add(paragraph);

            if (LocaleService.Current.FlowDirection == FlowDirection.LeftToRight && MessageHelper.IsAnyCharacterRightToLeft(message))
            {
                paragraph.Inlines.Add(new LineBreak());
            }
            else if (LocaleService.Current.FlowDirection == FlowDirection.RightToLeft && !MessageHelper.IsAnyCharacterRightToLeft(message))
            {
                paragraph.Inlines.Add(new LineBreak());
            }

            HeaderLabel.Inlines.Add(new Run { Text = Strings.Resources.ForwardedMessage, FontWeight = FontWeights.Normal });
            HeaderLabel.Inlines.Add(new LineBreak());
            HeaderLabel.Inlines.Add(new Run { Text = Strings.Resources.From + " ", FontWeight = FontWeights.Normal });

            var hyperlink = new Hyperlink();
            hyperlink.Inlines.Add(new Run { Text = forwarded });
            hyperlink.UnderlineStyle = UnderlineStyle.None;
            hyperlink.Foreground = GetBrush("MessageHeaderForegroundBrush");
            //hyperlink.Click += (s, args) => FwdFrom_Click(message);

            HeaderLabel.Inlines.Add(hyperlink);

            Header.Visibility = Visibility.Visible;
            HeaderLabel.Visibility = Visibility.Visible;

            UpdateMockup();
        }

        public void Mockup(string message, string sender, string reply, bool outgoing, DateTime date, bool first = true, bool last = true)
        {
            if (!_templateApplied)
            {
                void loaded(object o, RoutedEventArgs e)
                {
                    Loaded -= loaded;
                    Mockup(message, sender, reply, outgoing, date, first, last);
                }

                Loaded += loaded;
                return;
            }

            UpdateMockup(outgoing, first, last);

            Header.Visibility = Visibility.Visible;
            HeaderLabel.Visibility = Visibility.Collapsed;
            AdminLabel.Visibility = Visibility.Collapsed;

            if (Reply == null)
            {
                void layoutUpdated(object o, object e)
                {
                    Reply.LayoutUpdated -= layoutUpdated;
                    Reply.Mockup(sender, reply);
                }

                Reply = GetTemplateChild(nameof(Reply)) as MessageReference;
                Reply.LayoutUpdated += layoutUpdated;
            }
            else
            {
                Reply.Visibility = Visibility.Visible;
                Reply.Mockup(sender, reply);
            }

            Footer.Mockup(outgoing, date);
            Panel.Content = new MessageText { Text = new FormattedText(message, new TextEntity[0]) };

            Media.Margin = new Thickness(0);
            FooterToNormal();
            Grid.SetRow(Footer, 2);
            Grid.SetRow(Message, 2);
            Panel.Placeholder = true;

            var paragraph = new Paragraph();
            paragraph.Inlines.Add(CreateRun(message));

            Message.Blocks.Clear();
            Message.Blocks.Add(paragraph);

            if (LocaleService.Current.FlowDirection == FlowDirection.LeftToRight && MessageHelper.IsAnyCharacterRightToLeft(message))
            {
                paragraph.Inlines.Add(new LineBreak());
            }
            else if (LocaleService.Current.FlowDirection == FlowDirection.RightToLeft && !MessageHelper.IsAnyCharacterRightToLeft(message))
            {
                paragraph.Inlines.Add(new LineBreak());
            }

            UpdateMockup();
        }

        public void Mockup(MessageContent content, bool outgoing, DateTime date, bool first = true, bool last = true)
        {
            if (!_templateApplied)
            {
                void loaded(object o, RoutedEventArgs e)
                {
                    Loaded -= loaded;
                    Mockup(content, outgoing, date, first, last);
                }

                Loaded += loaded;
                return;
            }

            UpdateMockup(outgoing, first, last);

            Header.Visibility = Visibility.Collapsed;
            Message.Visibility = Visibility.Collapsed;

            Footer.Mockup(outgoing, date);
            Panel.Content = content;

            Media.Margin = new Thickness(10, 4, 10, 8);
            FooterToNormal();
            Grid.SetRow(Footer, 3);
            Grid.SetRow(Message, 2);
            Panel.Placeholder = false;

            if (content is MessageVoiceNote voiceNote)
            {
                var presenter = new VoiceNoteContent();

                void layoutUpdated(object o, object e)
                {
                    presenter.LayoutUpdated -= layoutUpdated;
                    presenter.Mockup(voiceNote);
                }

                presenter.LayoutUpdated += layoutUpdated;
                Media.Child = presenter;
            }
            else if (content is MessageAudio audio)
            {
                var presenter = new AudioContent();

                void layoutUpdated(object o, object e)
                {
                    presenter.LayoutUpdated -= layoutUpdated;
                    presenter.Mockup(audio);
                }

                presenter.LayoutUpdated += layoutUpdated;
                Media.Child = presenter;
            }

            Message.Blocks.Clear();

            UpdateMockup();
        }

        public void Mockup(MessageContent content, string caption, bool outgoing, DateTime date, bool first = true, bool last = true)
        {
            if (!_templateApplied)
            {
                void loaded(object o, RoutedEventArgs e)
                {
                    Loaded -= loaded;
                    Mockup(content, caption, outgoing, date, first, last);
                }

                Loaded += loaded;
                return;
            }

            UpdateMockup(outgoing, first, last);

            Header.Visibility = Visibility.Collapsed;

            Footer.Mockup(outgoing, date);
            Panel.Content = content;

            Media.Margin = new Thickness(0, 0, 0, 4);
            FooterToNormal();
            Grid.SetRow(Footer, 4);
            Grid.SetRow(Message, 4);
            Panel.Placeholder = true;

            if (content is MessagePhoto photo)
            {
                var presenter = new PhotoContent();

                void layoutUpdated(object o, object e)
                {
                    presenter.LayoutUpdated -= layoutUpdated;
                    presenter.Mockup(photo);
                }

                presenter.LayoutUpdated += layoutUpdated;
                Media.Child = presenter;
            }

            var paragraph = new Paragraph();
            paragraph.Inlines.Add(CreateRun(caption));

            Message.Blocks.Clear();
            Message.Blocks.Add(paragraph);

            if (LocaleService.Current.FlowDirection == FlowDirection.LeftToRight && MessageHelper.IsAnyCharacterRightToLeft(caption))
            {
                paragraph.Inlines.Add(new LineBreak());
            }
            else if (LocaleService.Current.FlowDirection == FlowDirection.RightToLeft && !MessageHelper.IsAnyCharacterRightToLeft(caption))
            {
                paragraph.Inlines.Add(new LineBreak());
            }

            UpdateMockup();
        }

        public void UpdateMockup()
        {
            if (Message.Blocks.Count > 0 && Message.Blocks[0] is Paragraph existing)
            {
                existing.FontSize = (double)Navigation.BootStrapper.Current.Resources["MessageFontSize"];
            }

            ContentPanel.CornerRadius = new CornerRadius(SettingsService.Current.Appearance.BubbleRadius);
        }

        private void UpdateMockup(bool outgoing, bool first, bool last)
        {
            var topLeft = 15d;
            var topRight = 15d;
            var bottomRight = 15d;
            var bottomLeft = 15d;

            if (outgoing)
            {
                if (first && last)
                {
                }
                else if (first)
                {
                    bottomRight = 4;
                }
                else if (last)
                {
                    topRight = 4;
                }
                else
                {
                    topRight = 4;
                    bottomRight = 4;
                }
            }
            else
            {
                if (first && last)
                {
                }
                else if (first)
                {
                    bottomLeft = 4;
                }
                else if (last)
                {
                    topLeft = 4;
                }
                else
                {
                    topLeft = 4;
                    bottomLeft = 4;
                }
            }

            ContentPanel.CornerRadius = new CornerRadius(topLeft, topRight, bottomRight, bottomLeft);
            Margin = new Thickness(outgoing ? 50 : 12, first ? 2 : 1, outgoing ? 12 : 50, last ? 2 : 1);
        }





        protected override Size MeasureOverride(Size availableSize)
        {
            //return base.MeasureOverride(availableSize);

            var availableWidth = Math.Min(availableSize.Width, Math.Min(double.IsNaN(Width) ? double.PositiveInfinity : Width, 320));
            var availableHeight = Math.Min(availableSize.Height, Math.Min(double.IsNaN(Height) ? double.PositiveInfinity : Height, 420));

            var ttl = false;
            var width = 0.0;
            var height = 0.0;

            var constraint = Tag;
            if (constraint is MessageViewModel viewModel)
            {
                ttl = viewModel.IsSecret();
                constraint = viewModel.GeneratedContent ?? viewModel.Content;
            }
            else if (constraint is Message message)
            {
                ttl = message.Ttl > 0;
                constraint = message.Content;
            }

            if (constraint is MessageAnimation animationMessage)
            {
                constraint = animationMessage.Animation;
            }
            else if (constraint is MessageInvoice invoiceMessage)
            {
                constraint = invoiceMessage.Photo;
            }
            else if (constraint is MessageLocation locationMessage)
            {
                constraint = locationMessage.Location;
            }
            else if (constraint is MessagePhoto photoMessage)
            {
                constraint = photoMessage.Photo;
            }
            else if (constraint is MessageSticker stickerMessage)
            {
                constraint = stickerMessage.Sticker;
            }
            else if (constraint is MessageVenue venueMessage)
            {
                constraint = venueMessage.Venue;
            }
            else if (constraint is MessageVideo videoMessage)
            {
                constraint = videoMessage.Video;
            }
            else if (constraint is MessageVideoNote videoNoteMessage)
            {
                constraint = videoNoteMessage.VideoNote;
            }
            else if (constraint is MessageVoiceNote voiceNoteMessage)
            {
                constraint = voiceNoteMessage.VoiceNote;
            }
            else if (constraint is MessageChatChangePhoto chatChangePhoto)
            {
                constraint = chatChangePhoto.Photo;
            }
            else if (constraint is MessageAlbum album)
            {
                if (album.Messages.Count == 1)
                {
                    if (album.Messages[0].Content is MessagePhoto photoContent)
                    {
                        constraint = photoContent.Photo;
                    }
                    else if (album.Messages[0].Content is MessageVideo videoContent)
                    {
                        constraint = videoContent.Video;
                    }
                }
                else if (album.IsMedia)
                {
                    var positions = album.GetPositionsForWidth(availableWidth + MessageAlbum.ITEM_MARGIN);
                    width = positions.Item2.Width - MessageAlbum.ITEM_MARGIN;
                    height = positions.Item2.Height;

                    goto Calculate;
                }
            }

            if (constraint is Animation animation)
            {
                width = animation.Width;
                height = animation.Height;

                goto Calculate;
            }
            else if (constraint is Location)
            {
                width = 320;
                height = 200;

                goto Calculate;
            }
            else if (constraint is Photo photo)
            {
                if (ttl)
                {
                    width = 240;
                    height = 240;
                }
                else if (photo.Sizes.Count > 0)
                {
                    width = photo.Sizes[photo.Sizes.Count - 1].Width;
                    height = photo.Sizes[photo.Sizes.Count - 1].Height;
                }

                goto Calculate;
            }
            else if (constraint is Sticker)
            {
                // We actually don't have to calculate bubble width for stickers,
                // As it might be wider due to reply
                //width = sticker.Width;
                //height = sticker.Height;

                //goto Calculate;
            }
            else if (constraint is Venue)
            {
                width = 320;
                height = 200;

                goto Calculate;
            }
            else if (constraint is Video video)
            {
                if (ttl)
                {
                    width = 240;
                    height = 240;
                }
                else
                {
                    width = video.Width;
                    height = video.Height;
                }

                goto Calculate;
            }
            else if (constraint is VideoNote)
            {
                // We actually don't have to calculate bubble width for video notes,
                // As it might be wider due to reply/forward
                //width = 200;
                //height = 200;

                //goto Calculate;
            }
            else if (constraint is VoiceNote voiceNote)
            {
                width = Math.Min(Math.Max(4, voiceNote.Duration), 30) / 30d * availableSize.Width;

                //return base.MeasureOverride(new Size(width, availableSize.Height));
            }

            return base.MeasureOverride(availableSize);

        Calculate:

            if (Footer.DesiredSize.IsEmpty)
            {
                Footer.Measure(availableSize);
            }

            var additional = 0d;

            if (PhotoColumn.Width.IsAbsolute)
            {
                additional += 38;
            }

            if (Action != null)
            {
                additional += 38;
            }

            width = Math.Max(Footer.DesiredSize.Width + /*margin left*/ 8 + /*padding right*/ 6 + /*margin right*/ 6, Math.Max(width, 96));

            if (width > availableWidth + additional || height > availableHeight)
            {
                var ratioX = availableWidth / width;
                var ratioY = availableHeight / height;
                var ratio = Math.Min(ratioX, ratioY);

                return base.MeasureOverride(new Size(Math.Max(96, width * ratio) + additional, availableSize.Height));
            }
            else
            {
                return base.MeasureOverride(new Size(Math.Max(96, width) + additional, availableSize.Height));
            }
        }

        private void Message_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            e.Handled = true;
        }

        private static bool IsFullMedia(MessageContent content, bool width = false)
        {
            switch (content)
            {
                case MessageLocation:
                case MessageVenue:
                case MessagePhoto:
                case MessageVideo:
                case MessageAnimation:
                    return true;
                case MessageAlbum album:
                    return album.IsMedia;
                case MessageInvoice invoice:
                    return width && invoice.Photo != null;
                default:
                    return false;
            }
        }

        #region IPlayerView

        public bool IsAnimatable => CustomEmoji != null || (Reply?.IsAnimatable ?? false);

        public bool IsLoopingEnabled => true;

        private bool _playing;

        public bool Play()
        {
            CustomEmoji?.Play();
            Reply?.Play();

            _playing = true;
            return true;
        }

        public void Pause()
        {
            CustomEmoji?.Pause();
            Reply?.Pause();

            _playing = false;
        }

        public void Unload()
        {
            CustomEmoji?.Unload();
            Reply?.Unload();

            _playing = false;
        }

        #endregion
    }
}
