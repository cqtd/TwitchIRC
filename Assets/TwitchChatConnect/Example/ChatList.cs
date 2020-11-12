using System.Collections.Generic;
using System.Linq;
using TMPro;
using TwitchChatConnect.Client;
using TwitchChatConnect.Data;
using UnityEngine;

public class ChatList : MonoBehaviour
{
	[SerializeField] TextMeshProUGUI[] texts;
	[SerializeField] string[] ignoreUsers;

	public string msg;

	[ContextMenu("Send")]
	void SendMessage()
	{
		if (!string.IsNullOrEmpty(msg))
		{
			TwitchChatClient.instance.SendChatMessage(msg);
			msg = string.Empty;
		}
	}
	
	void Start()
	{
		messages = new Queue<string>();
		capacity = texts.Length;
		
		TwitchChatClient.instance.Init(() =>
			{
				TwitchChatClient.instance.onChatMessageReceived += ShowMessage;
				TwitchChatClient.instance.onChatCommandReceived += ShowCommand;
				TwitchChatClient.instance.onChatRewardReceived += ShowReward;
                
			},
			Debug.LogError);
	}

	void ShowCommand(TwitchChatCommand chatCommand)
	{
		if (ignoreUsers.Contains(chatCommand.User.DisplayName))
		{
			Debug.LogWarning($"Ignored - [{chatCommand.User.DisplayName}]: {chatCommand.Command} ");
			return;
		}
		
		string parameters = string.Join(" - ", chatCommand.Parameters);
		string message =
			$"Command: '{chatCommand.Command}' - Username: {chatCommand.User.DisplayName} - Sub: {chatCommand.User.IsSub} - Parameters: {parameters}";

		AddText(message);
	}

	void ShowReward(TwitchChatReward chatReward)
	{
		if (ignoreUsers.Contains(chatReward.User.DisplayName))
		{
			Debug.LogWarning($"Ignored - [{chatReward.User.DisplayName}]: {chatReward.Message} ");
			return;
		}
		
		string message = $"Reward unlocked by {chatReward.User.DisplayName} - Reward ID: {chatReward.CustomRewardId} - Message: {chatReward.Message}";
		AddText(message);
	}
    
	void ShowMessage(TwitchChatMessage chatMessage)
	{
		if (ignoreUsers.Contains(chatMessage.User.DisplayName))
		{
			Debug.LogWarning($"Ignored - [{chatMessage.User.DisplayName}]: {chatMessage.Message} ");
			return;
		}
		
		AddText($"<color={chatMessage.User.Color}>{chatMessage.User.DisplayName}({chatMessage.User.Id})</color>: {chatMessage.Message}");
	}

	Queue<string> messages;
	int capacity;

	void AddText(string message)
	{
		if (messages.Count >= capacity)
		{
			messages.Dequeue();
		}
		
		messages.Enqueue(message);

		for (int i = 0; i < capacity; i++)
		{
			if (messages.Count <= i)
			{
				texts[i].SetText("");
			}
			else
			{
				texts[i].SetText(messages.ElementAt(i));
			}
		}
	}
}