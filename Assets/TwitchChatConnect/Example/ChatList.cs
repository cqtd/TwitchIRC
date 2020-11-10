using System.Collections.Generic;
using System.Linq;
using TMPro;
using TwitchChatConnect.Client;
using TwitchChatConnect.Data;
using UnityEngine;
using UnityEngine.UI;

public class ChatList : MonoBehaviour
{
	[SerializeField] private Transform panel;
	[SerializeField] private TextMeshProUGUI textPrefab;

	[SerializeField] TextMeshProUGUI[] texts;

	[SerializeField] string[] ignoreUsers;

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
			message =>
			{
				// Error when initializing.
				Debug.LogError(message);
			});
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

		// TwitchChatClient.instance.SendChatMessage($"Hello {chatCommand.User.DisplayName}! I received your message.");
		// TwitchChatClient.instance.SendChatMessage($"Hello {chatCommand.User.DisplayName}! This message will be sent in 5 seconds.", 5);

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
		
		string message = $"Message by {chatMessage.User.DisplayName} - Message: {chatMessage.Message}";
		// AddText(message);
		AddText($"<color={chatMessage.User.Color}>{chatMessage.User.DisplayName}({chatMessage.User.Id})</color>: {chatMessage.Message}");
	}

	Queue<string> messages;
	int capacity;

	private void AddText(string message)
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
			
			// texts[i].SetText(messages.ElementAt(i));

		}
		
		// TextMeshProUGUI newText = Instantiate(textPrefab, panel);
		// newText.SetText(message);
	}
}