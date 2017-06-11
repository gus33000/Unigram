//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

// --------------------------------------------------------------------------------------------------
// <auto-generatedInfo>
// 	This code was generated by ResW File Code Generator (http://reswcodegen.codeplex.com)
// 	ResW File Code Generator was written by Christian Resma Helle
// 	and is under GNU General Public License version 2 (GPLv2)
// 
// 	This code contains a helper class exposing property representations
// 	of the string resources defined in the specified .ResW file
// 
// 	Generated: 06/03/2017 15:33:57
// </auto-generatedInfo>
// --------------------------------------------------------------------------------------------------
namespace Unigram.Strings
{
    using Windows.ApplicationModel.Resources;
    
    
    public partial class AppResources
    {
        
        private static ResourceLoader resourceLoader;
        
        static AppResources()
        {
            string executingAssemblyName;
            executingAssemblyName = Windows.UI.Xaml.Application.Current.GetType().AssemblyQualifiedName;
            string[] executingAssemblySplit;
            executingAssemblySplit = executingAssemblyName.Split(',');
            executingAssemblyName = executingAssemblySplit[1];
            string currentAssemblyName;
            currentAssemblyName = typeof(AppResources).AssemblyQualifiedName;
            string[] currentAssemblySplit;
            currentAssemblySplit = currentAssemblyName.Split(',');
            currentAssemblyName = currentAssemblySplit[1];
            if (executingAssemblyName.Equals(currentAssemblyName))
            {
                resourceLoader = ResourceLoader.GetForViewIndependentUse("Resources");
            }
            else
            {
                resourceLoader = ResourceLoader.GetForViewIndependentUse(currentAssemblyName + "/Resources");
            }
        }
        
        public static ResourceLoader Loader
        {
            get
            {
                return resourceLoader;
            }
        }
        
        /// <summary>
        /// Localized resource similar to "{0} created the broadcast "{1}""
        /// </summary>
        public static string MessageActionBroadcastCreate
        {
            get
            {
                return resourceLoader.GetString("MessageActionBroadcastCreate");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "Channel created"
        /// </summary>
        public static string MessageActionChannelCreate
        {
            get
            {
                return resourceLoader.GetString("MessageActionChannelCreate");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "Channel photo removed"
        /// </summary>
        public static string MessageActionChannelDeletePhoto
        {
            get
            {
                return resourceLoader.GetString("MessageActionChannelDeletePhoto");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "Channel photo updated"
        /// </summary>
        public static string MessageActionChannelEditPhoto
        {
            get
            {
                return resourceLoader.GetString("MessageActionChannelEditPhoto");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "Channel name changed to "{0}""
        /// </summary>
        public static string MessageActionChannelEditTitle
        {
            get
            {
                return resourceLoader.GetString("MessageActionChannelEditTitle");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "This group was upgraded to a supergroup."
        /// </summary>
        public static string MessageActionChannelMigrateFrom
        {
            get
            {
                return resourceLoader.GetString("MessageActionChannelMigrateFrom");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "Chat was activated"
        /// </summary>
        public static string MessageActionChatActivate
        {
            get
            {
                return resourceLoader.GetString("MessageActionChatActivate");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "{0} joined the group"
        /// </summary>
        public static string MessageActionChatAddSelf
        {
            get
            {
                return resourceLoader.GetString("MessageActionChatAddSelf");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "{0} added {1}"
        /// </summary>
        public static string MessageActionChatAddUser
        {
            get
            {
                return resourceLoader.GetString("MessageActionChatAddUser");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "{0} created the group "{1}""
        /// </summary>
        public static string MessageActionChatCreate
        {
            get
            {
                return resourceLoader.GetString("MessageActionChatCreate");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "Chat was deactivated"
        /// </summary>
        public static string MessageActionChatDeactivate
        {
            get
            {
                return resourceLoader.GetString("MessageActionChatDeactivate");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "{0} removed group photo"
        /// </summary>
        public static string MessageActionChatDeletePhoto
        {
            get
            {
                return resourceLoader.GetString("MessageActionChatDeletePhoto");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "{0} removed {1}"
        /// </summary>
        public static string MessageActionChatDeleteUser
        {
            get
            {
                return resourceLoader.GetString("MessageActionChatDeleteUser");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "{0} updated group photo"
        /// </summary>
        public static string MessageActionChatEditPhoto
        {
            get
            {
                return resourceLoader.GetString("MessageActionChatEditPhoto");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "{0} changed group name to "{1}""
        /// </summary>
        public static string MessageActionChatEditTitle
        {
            get
            {
                return resourceLoader.GetString("MessageActionChatEditTitle");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "{0} joined the group"
        /// </summary>
        public static string MessageActionChatJoin
        {
            get
            {
                return resourceLoader.GetString("MessageActionChatJoin");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "{0} joined the group via invite link"
        /// </summary>
        public static string MessageActionChatJoinedByLink
        {
            get
            {
                return resourceLoader.GetString("MessageActionChatJoinedByLink");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "You joined the group"
        /// </summary>
        public static string MessageActionChatJoinSelf
        {
            get
            {
                return resourceLoader.GetString("MessageActionChatJoinSelf");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "Chat migrated to supergroup "{0}""
        /// </summary>
        public static string MessageActionChatMigrateTo
        {
            get
            {
                return resourceLoader.GetString("MessageActionChatMigrateTo");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "{0} disabled the self-destruct timer"
        /// </summary>
        public static string MessageActionDisableMessageTTL
        {
            get
            {
                return resourceLoader.GetString("MessageActionDisableMessageTTL");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "Empty service message"
        /// </summary>
        public static string MessageActionEmpty
        {
            get
            {
                return resourceLoader.GetString("MessageActionEmpty");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "You left the group"
        /// </summary>
        public static string MessageActionLeftGroupSelf
        {
            get
            {
                return resourceLoader.GetString("MessageActionLeftGroupSelf");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "{0} pinned a contact"
        /// </summary>
        public static string MessageActionPinContact
        {
            get
            {
                return resourceLoader.GetString("MessageActionPinContact");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "{0} pinned a file"
        /// </summary>
        public static string MessageActionPinFile
        {
            get
            {
                return resourceLoader.GetString("MessageActionPinFile");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "{0} pinned a game"
        /// </summary>
        public static string MessageActionPinGame
        {
            get
            {
                return resourceLoader.GetString("MessageActionPinGame");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "{0} pinned a GIF"
        /// </summary>
        public static string MessageActionPinGif
        {
            get
            {
                return resourceLoader.GetString("MessageActionPinGif");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "{0} pinned a map"
        /// </summary>
        public static string MessageActionPinMap
        {
            get
            {
                return resourceLoader.GetString("MessageActionPinMap");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "{0} pinned a message"
        /// </summary>
        public static string MessageActionPinMessage
        {
            get
            {
                return resourceLoader.GetString("MessageActionPinMessage");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "{0} pinned a photo"
        /// </summary>
        public static string MessageActionPinPhoto
        {
            get
            {
                return resourceLoader.GetString("MessageActionPinPhoto");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "{0} pinned a {1}sticker"
        /// </summary>
        public static string MessageActionPinSticker
        {
            get
            {
                return resourceLoader.GetString("MessageActionPinSticker");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "{0} pinned "{1}""
        /// </summary>
        public static string MessageActionPinText
        {
            get
            {
                return resourceLoader.GetString("MessageActionPinText");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "{0} pinned a track"
        /// </summary>
        public static string MessageActionPinTrack
        {
            get
            {
                return resourceLoader.GetString("MessageActionPinTrack");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "{0} pinned a video"
        /// </summary>
        public static string MessageActionPinVideo
        {
            get
            {
                return resourceLoader.GetString("MessageActionPinVideo");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "{0} pinned a voice message"
        /// </summary>
        public static string MessageActionPinVoiceMessage
        {
            get
            {
                return resourceLoader.GetString("MessageActionPinVoiceMessage");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "{0} took a screenshot!"
        /// </summary>
        public static string MessageActionScreenshotMessages
        {
            get
            {
                return resourceLoader.GetString("MessageActionScreenshotMessages");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "{0} set the self-destruct timer to {1}"
        /// </summary>
        public static string MessageActionSetMessageTTL
        {
            get
            {
                return resourceLoader.GetString("MessageActionSetMessageTTL");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "{0} left the group"
        /// </summary>
        public static string MessageActionUserLeftGroup
        {
            get
            {
                return resourceLoader.GetString("MessageActionUserLeftGroup");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "You disabled the self-destruct timer"
        /// </summary>
        public static string MessageActionYouDisableMessageTTL
        {
            get
            {
                return resourceLoader.GetString("MessageActionYouDisableMessageTTL");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "You took a screenshot!"
        /// </summary>
        public static string MessageActionYouScreenshotMessages
        {
            get
            {
                return resourceLoader.GetString("MessageActionYouScreenshotMessages");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "You set the self-destruct timer to {0}"
        /// </summary>
        public static string MessageActionYouSetMessageTTL
        {
            get
            {
                return resourceLoader.GetString("MessageActionYouSetMessageTTL");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "{0} scored {1}"
        /// </summary>
        public static string UserScored
        {
            get
            {
                return resourceLoader.GetString("UserScored");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "{0} scored {1} at {2}"
        /// </summary>
        public static string UserScoredAtGame
        {
            get
            {
                return resourceLoader.GetString("UserScoredAtGame");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "{0} scored {1} at {2}"
        /// </summary>
        public static string UserScoredAtGamePlural
        {
            get
            {
                return resourceLoader.GetString("UserScoredAtGamePlural");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "{0} scored {1}"
        /// </summary>
        public static string UserScoredPlural
        {
            get
            {
                return resourceLoader.GetString("UserScoredPlural");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "Users"
        /// </summary>
        public static string UserNominativePlural
        {
            get
            {
                return resourceLoader.GetString("UserNominativePlural");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "User"
        /// </summary>
        public static string UserNominativeSingular
        {
            get
            {
                return resourceLoader.GetString("UserNominativeSingular");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "You scored {0}"
        /// </summary>
        public static string YourScored
        {
            get
            {
                return resourceLoader.GetString("YourScored");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "You scored {0} at {1}"
        /// </summary>
        public static string YourScoredAtGame
        {
            get
            {
                return resourceLoader.GetString("YourScoredAtGame");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "You scored {0} at {1}"
        /// </summary>
        public static string YourScoredAtGamePlural
        {
            get
            {
                return resourceLoader.GetString("YourScoredAtGamePlural");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "You scored {0}"
        /// </summary>
        public static string YourScoredPlural
        {
            get
            {
                return resourceLoader.GetString("YourScoredPlural");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "{0} are typing"
        /// </summary>
        public static string AreTyping
        {
            get
            {
                return resourceLoader.GetString("AreTyping");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "{0} is playing a game"
        /// </summary>
        public static string IsPlayingGame
        {
            get
            {
                return resourceLoader.GetString("IsPlayingGame");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "{0} is recording a voice message"
        /// </summary>
        public static string IsRecordingAudio
        {
            get
            {
                return resourceLoader.GetString("IsRecordingAudio");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "{0} is recording a video"
        /// </summary>
        public static string IsRecordingVideo
        {
            get
            {
                return resourceLoader.GetString("IsRecordingVideo");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "{0} is sending a audio"
        /// </summary>
        public static string IsSendingAudio
        {
            get
            {
                return resourceLoader.GetString("IsSendingAudio");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "{0} is sending a file"
        /// </summary>
        public static string IsSendingFile
        {
            get
            {
                return resourceLoader.GetString("IsSendingFile");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "{0} is sending a photo"
        /// </summary>
        public static string IsSendingPhoto
        {
            get
            {
                return resourceLoader.GetString("IsSendingPhoto");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "{0} is sending a video"
        /// </summary>
        public static string IsSendingVideo
        {
            get
            {
                return resourceLoader.GetString("IsSendingVideo");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "{0} is typing"
        /// </summary>
        public static string IsTyping
        {
            get
            {
                return resourceLoader.GetString("IsTyping");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "Playing a game"
        /// </summary>
        public static string PlayingGame
        {
            get
            {
                return resourceLoader.GetString("PlayingGame");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "Recording a video"
        /// </summary>
        public static string RecordingVideo
        {
            get
            {
                return resourceLoader.GetString("RecordingVideo");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "Recording a voice message"
        /// </summary>
        public static string RecordingVoiceMessage
        {
            get
            {
                return resourceLoader.GetString("RecordingVoiceMessage");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "Sending an audio"
        /// </summary>
        public static string SendingAudio
        {
            get
            {
                return resourceLoader.GetString("SendingAudio");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "Sending a file"
        /// </summary>
        public static string SendingFile
        {
            get
            {
                return resourceLoader.GetString("SendingFile");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "Sending a photo"
        /// </summary>
        public static string SendingPhoto
        {
            get
            {
                return resourceLoader.GetString("SendingPhoto");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "Sending a video"
        /// </summary>
        public static string SendingVideo
        {
            get
            {
                return resourceLoader.GetString("SendingVideo");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "Typing"
        /// </summary>
        public static string Typing
        {
            get
            {
                return resourceLoader.GetString("Typing");
            }
        }
        
        /// <summary>
        /// Localized resource similar to ""
        /// </summary>
        public static string CompanyGenitivePlural
        {
            get
            {
                return resourceLoader.GetString("CompanyGenitivePlural");
            }
        }
        
        /// <summary>
        /// Localized resource similar to ""
        /// </summary>
        public static string CompanyGenitiveSingular
        {
            get
            {
                return resourceLoader.GetString("CompanyGenitiveSingular");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "Members"
        /// </summary>
        public static string CompanyNominativePlural
        {
            get
            {
                return resourceLoader.GetString("CompanyNominativePlural");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "Member"
        /// </summary>
        public static string CompanyNominativeSingular
        {
            get
            {
                return resourceLoader.GetString("CompanyNominativeSingular");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "{0} minute"
        /// </summary>
        public static string CallMinutes_1
        {
            get
            {
                return resourceLoader.GetString("CallMinutes_1");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "{0} minutes"
        /// </summary>
        public static string CallMinutes_2
        {
            get
            {
                return resourceLoader.GetString("CallMinutes_2");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "{0} minutes"
        /// </summary>
        public static string CallMinutes_3_10
        {
            get
            {
                return resourceLoader.GetString("CallMinutes_3_10");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "{0} minutes"
        /// </summary>
        public static string CallMinutes_any
        {
            get
            {
                return resourceLoader.GetString("CallMinutes_any");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "{0} second"
        /// </summary>
        public static string CallSeconds_1
        {
            get
            {
                return resourceLoader.GetString("CallSeconds_1");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "{0} seconds"
        /// </summary>
        public static string CallSeconds_2
        {
            get
            {
                return resourceLoader.GetString("CallSeconds_2");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "{0} seconds"
        /// </summary>
        public static string CallSeconds_3_10
        {
            get
            {
                return resourceLoader.GetString("CallSeconds_3_10");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "{0} seconds"
        /// </summary>
        public static string CallSeconds_any
        {
            get
            {
                return resourceLoader.GetString("CallSeconds_any");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "{0} min"
        /// </summary>
        public static string CallShortMinutes_1
        {
            get
            {
                return resourceLoader.GetString("CallShortMinutes_1");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "{0} min"
        /// </summary>
        public static string CallShortMinutes_2
        {
            get
            {
                return resourceLoader.GetString("CallShortMinutes_2");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "{0} min"
        /// </summary>
        public static string CallShortMinutes_3_10
        {
            get
            {
                return resourceLoader.GetString("CallShortMinutes_3_10");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "{0} min"
        /// </summary>
        public static string CallShortMinutes_any
        {
            get
            {
                return resourceLoader.GetString("CallShortMinutes_any");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "{0} sec"
        /// </summary>
        public static string CallShortSeconds_1
        {
            get
            {
                return resourceLoader.GetString("CallShortSeconds_1");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "{0} sec"
        /// </summary>
        public static string CallShortSeconds_2
        {
            get
            {
                return resourceLoader.GetString("CallShortSeconds_2");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "{0} sec"
        /// </summary>
        public static string CallShortSeconds_3_10
        {
            get
            {
                return resourceLoader.GetString("CallShortSeconds_3_10");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "{0} sec"
        /// </summary>
        public static string CallShortSeconds_any
        {
            get
            {
                return resourceLoader.GetString("CallShortSeconds_any");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "Cancelled Call"
        /// </summary>
        public static string CallCanceled
        {
            get
            {
                return resourceLoader.GetString("CallCanceled");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "Incoming Call"
        /// </summary>
        public static string CallIncoming
        {
            get
            {
                return resourceLoader.GetString("CallIncoming");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "Missed Call"
        /// </summary>
        public static string CallMissed
        {
            get
            {
                return resourceLoader.GetString("CallMissed");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "Outgoing Call"
        /// </summary>
        public static string CallOutgoing
        {
            get
            {
                return resourceLoader.GetString("CallOutgoing");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "{0} ({1})"
        /// </summary>
        public static string CallTimeFormat
        {
            get
            {
                return resourceLoader.GetString("CallTimeFormat");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "{0} pinned an invoice"
        /// </summary>
        public static string MessageActionPinInvoice
        {
            get
            {
                return resourceLoader.GetString("MessageActionPinInvoice");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "Cancelled"
        /// </summary>
        public static string CallCanceledShort
        {
            get
            {
                return resourceLoader.GetString("CallCanceledShort");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "Incoming"
        /// </summary>
        public static string CallIncomingShort
        {
            get
            {
                return resourceLoader.GetString("CallIncomingShort");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "Missed"
        /// </summary>
        public static string CallMissedShort
        {
            get
            {
                return resourceLoader.GetString("CallMissedShort");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "Outgoing"
        /// </summary>
        public static string CallOutgoingShort
        {
            get
            {
                return resourceLoader.GetString("CallOutgoingShort");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "Chat migrated to supergroup"
        /// </summary>
        public static string MessageActionChatMigrateToGeneric
        {
            get
            {
                return resourceLoader.GetString("MessageActionChatMigrateToGeneric");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "Recording a video message"
        /// </summary>
        public static string RecordingVideoMessage
        {
            get
            {
                return resourceLoader.GetString("RecordingVideoMessage");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "Sending a video message"
        /// </summary>
        public static string SendingVideoMessage
        {
            get
            {
                return resourceLoader.GetString("SendingVideoMessage");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "{0} is recording a video message"
        /// </summary>
        public static string IsRecordingVideoMessage
        {
            get
            {
                return resourceLoader.GetString("IsRecordingVideoMessage");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "{0} is sending a video message"
        /// </summary>
        public static string IsSendingVideoMessage
        {
            get
            {
                return resourceLoader.GetString("IsSendingVideoMessage");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "Months"
        /// </summary>
        public static string MonthGenitivePlural
        {
            get
            {
                return resourceLoader.GetString("MonthGenitivePlural");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "Month"
        /// </summary>
        public static string MonthGenitiveSingular
        {
            get
            {
                return resourceLoader.GetString("MonthGenitiveSingular");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "Months"
        /// </summary>
        public static string MonthNominativePlural
        {
            get
            {
                return resourceLoader.GetString("MonthNominativePlural");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "Month"
        /// </summary>
        public static string MonthNominativeSingular
        {
            get
            {
                return resourceLoader.GetString("MonthNominativeSingular");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "Years"
        /// </summary>
        public static string YearGenitivePlural
        {
            get
            {
                return resourceLoader.GetString("YearGenitivePlural");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "Year"
        /// </summary>
        public static string YearGenitiveSingular
        {
            get
            {
                return resourceLoader.GetString("YearGenitiveSingular");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "Years"
        /// </summary>
        public static string YearNominativePlural
        {
            get
            {
                return resourceLoader.GetString("YearNominativePlural");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "Year"
        /// </summary>
        public static string YearNominativeSingular
        {
            get
            {
                return resourceLoader.GetString("YearNominativeSingular");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "Users"
        /// </summary>
        public static string UserGenitivePlural
        {
            get
            {
                return resourceLoader.GetString("UserGenitivePlural");
            }
        }
        
        /// <summary>
        /// Localized resource similar to "User"
        /// </summary>
        public static string UserGenitiveSingular
        {
            get
            {
                return resourceLoader.GetString("UserGenitiveSingular");
            }
        }
    }
}
