using System;
using System.Collections;
using UnityEngine;
using System.Net.Sockets;
using System.IO;
using System.Text.RegularExpressions;
using TwitchChatConnect.Data;
using TwitchChatConnect.Manager;

namespace TwitchChatConnect.Client
{
    public class TwitchChatClient : MonoBehaviour
    {
        [Header("config.json file with 'username', 'userToken' and 'channelName'")] 
        [SerializeField]
        string configurationPath = "";

        [Header("Command prefix, by default is '!' (only 1 character)")] 
        [SerializeField]
        string commandPrefix = "!";

        TcpClient twitchClient;
        StreamReader reader;
        StreamWriter writer;
        TwitchConnectData twitchConnectData;

        static string COMMAND_JOIN = "JOIN";
        static string COMMAND_PART = "PART";
        static string COMMAND_MESSAGE = "PRIVMSG";
        static string CUSTOM_REWARD_TEXT = "custom-reward-id";

        Regex joinRegexp = new Regex(@":(.+)!.*JOIN"); // :<user>!<user>@<user>.tmi.twitch.tv JOIN #<channel>
        Regex partRegexp = new Regex(@":(.+)!.*PART"); // :<user>!<user>@<user>.tmi.twitch.tv PART #<channel>

        Regex messageRegexp =
            new Regex(@"display\-name=(.+);emotes.*subscriber=(.+);tmi.*user\-id=(.+);.*:(.*)!.*PRIVMSG.+:(.*)");

        Regex messageRegexp2 =
            new Regex(@"color=(.+);display");

        Regex rewardRegexp =
            new Regex(
                @"custom\-reward\-id=(.+);display\-name=(.+);emotes.*subscriber=(.+);tmi.*user\-id=(.+);.*:(.*)!.*PRIVMSG.+:(.*)");

        public Action<TwitchChatMessage> onChatMessageReceived;
        public Action<TwitchChatCommand> onChatCommandReceived;
        public Action<TwitchChatReward> onChatRewardReceived;

        public delegate void OnError(string errorMessage);

        public delegate void OnSuccess();

        #region Singleton

        public static TwitchChatClient instance { get; private set; }

        void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        #endregion

        void FixedUpdate()
        {
            if (!IsConnected()) return;
            ReadChatLine();
        }

        public void Init(OnSuccess onSuccess, OnError onError)
        {
            if (IsConnected())
            {
                onSuccess();
                return;
            }

            // Checks
            if (configurationPath == "") configurationPath = Application.persistentDataPath + "/config.json";
            if (String.IsNullOrEmpty(commandPrefix)) commandPrefix = "!";

            if (commandPrefix.Length > 1)
            {
                string errorMessage =
                    $"TwitchChatClient.Init :: Command prefix length should contain only 1 character. Command prefix: {commandPrefix}";
                onError(errorMessage);
                return;
            }

            TwitchConfiguration.Load(configurationPath, (data) =>
            {
                twitchConnectData = data;
                Login();
                onSuccess();
            }, message => onError(message));
        }

        void Login()
        {
            twitchClient = new TcpClient("irc.chat.twitch.tv", 6667);
            reader = new StreamReader(twitchClient.GetStream());
            writer = new StreamWriter(twitchClient.GetStream());

            writer.WriteLine($"PASS {twitchConnectData.UserToken}");
            writer.WriteLine($"NICK {twitchConnectData.Username}");
            writer.WriteLine($"USER {twitchConnectData.Username} 8 * :{twitchConnectData.Username}");
            writer.WriteLine($"JOIN #{twitchConnectData.ChannelName}");

            writer.WriteLine("CAP REQ :twitch.tv/tags");
            writer.WriteLine("CAP REQ :twitch.tv/commands");
            writer.WriteLine("CAP REQ :twitch.tv/membership");

            writer.Flush();
        }

        void ReadChatLine()
        {
            if (twitchClient.Available <= 0) return;
            string message = reader.ReadLine();
            
            Debug.Log(message);

            if (message == null) return;
            if (message.Length == 0) return;

            if (message.Contains("PING"))
            {
                writer.WriteLine($"PONG #{twitchConnectData.ChannelName}");
                writer.Flush();
                return;
            }

            if (message.Contains(COMMAND_MESSAGE))
            {
                if (message.Contains(CUSTOM_REWARD_TEXT))
                {
                    ReadChatReward(message);
                }
                else
                {
                    ReadChatMessage(message);
                }
            }
            else if (message.Contains(COMMAND_JOIN))
            {
                string username = joinRegexp.Match(message).Groups[1].Value;
                TwitchUserManager.AddUser(username);
            }
            else if (message.Contains(COMMAND_PART))
            {
                string username = partRegexp.Match(message).Groups[1].Value;
                TwitchUserManager.RemoveUser(username);
            }
        }

        void ReadChatMessage(string message)
        {
            string displayName = messageRegexp.Match(message).Groups[1].Value;
            bool isSub = messageRegexp.Match(message).Groups[2].Value == "1";
            string idUser = messageRegexp.Match(message).Groups[3].Value;
            string username = messageRegexp.Match(message).Groups[4].Value;
            string messageSent = messageRegexp.Match(message).Groups[5].Value;
            
            var result = messageRegexp2.Match(message).Groups[0].Value;
            string color = result.Replace("color=","").Replace(";display","");
            if (string.IsNullOrEmpty(color))
            {
                color = "#FFFFFF";
            }

            TwitchUser twitchUser = TwitchUserManager.AddUser(username);
            twitchUser.SetData(idUser, displayName, isSub, color);

            if (messageSent.Length == 0) return;

            if (messageSent[0] == commandPrefix[0])
            {
                TwitchChatCommand chatCommand = new TwitchChatCommand(twitchUser, messageSent);
                onChatCommandReceived?.Invoke(chatCommand);
            }
            else
            {
                TwitchChatMessage chatMessage = new TwitchChatMessage(twitchUser, messageSent);
                onChatMessageReceived?.Invoke(chatMessage);
            }
        }

        void ReadChatReward(string message)
        {
            string customRewardId = rewardRegexp.Match(message).Groups[1].Value;
            string displayName = rewardRegexp.Match(message).Groups[2].Value;
            bool isSub = rewardRegexp.Match(message).Groups[3].Value == "1";
            string idUser = rewardRegexp.Match(message).Groups[4].Value;
            string username = rewardRegexp.Match(message).Groups[5].Value;
            string messageSent = rewardRegexp.Match(message).Groups[6].Value;
            
            var result = messageRegexp2.Match(message).Groups[0].Value;
            string color = result.Replace("color=","").Replace(";display","");
            if (string.IsNullOrEmpty(color))
            {
                color = "#FFFFFF";
            }
            
            TwitchUser twitchUser = TwitchUserManager.AddUser(username);
            twitchUser.SetData(idUser, displayName, isSub, color);
            
            TwitchChatReward chatReward = new TwitchChatReward(twitchUser, messageSent, customRewardId);
            onChatRewardReceived?.Invoke(chatReward);
        }

        public bool SendChatMessage(string message)
        {
            if (!IsConnected()) return false;
            SendTwitchMessage(message);
            return true;
        }

        public bool SendChatMessage(string message, float seconds)
        {
            if (!IsConnected()) return false;
            StartCoroutine(SendTwitchChatMessageWithDelay(message, seconds));
            return true;
        }

        IEnumerator SendTwitchChatMessageWithDelay(string message, float seconds)
        {
            yield return new WaitForSeconds(seconds);
            SendTwitchMessage(message);
        }

        void SendTwitchMessage(string message)
        {
            writer.WriteLine($"PRIVMSG #{twitchConnectData.ChannelName} :/me {message}");
            writer.Flush();
        }

        bool IsConnected()
        {
            return twitchClient != null && twitchClient.Connected;
        }

        void OnDestroy()
        {
            twitchClient.Dispose();
        }
    }
}