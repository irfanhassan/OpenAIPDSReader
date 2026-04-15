using OpenAI.Chat;
using System.ClientModel;

namespace OPenAIPDSQandA.Services;

public class OpenAIService
{
    private readonly ChatClient _chatClient;
    private readonly string _systemPrompt;

    public OpenAIService(string apiKey, string model, string systemPrompt)
    {
        _chatClient = new ChatClient(model, new ApiKeyCredential(apiKey));
        _systemPrompt = systemPrompt;
    }

    /// <summary>
    /// RAG path: answer using only the retrieved chunks as context.
    /// </summary>
    public async Task AskWithRagAsync(
        string[] retrievedChunks,
        string question,
        List<ConversationTurn> history,
        TextWriter output,
        CancellationToken cancellationToken = default)
    {
        var context = string.Join("\n\n---\n\n", retrievedChunks);
        var systemMessage = _systemPrompt.Replace("{context}", context);

        var messages = new List<ChatMessage>
        {
            ChatMessage.CreateSystemMessage(systemMessage)
        };

        foreach (var turn in history)
        {
            messages.Add(ChatMessage.CreateUserMessage(turn.UserMessage));
            messages.Add(ChatMessage.CreateAssistantMessage(turn.AssistantMessage));
        }

        messages.Add(ChatMessage.CreateUserMessage(question));

        var assistantResponse = new System.Text.StringBuilder();

        await foreach (var update in _chatClient.CompleteChatStreamingAsync(messages, cancellationToken: cancellationToken))
        {
            foreach (var part in update.ContentUpdate)
            {
                await output.WriteAsync(part.Text);
                assistantResponse.Append(part.Text);
            }
        }

        history.Add(new ConversationTurn(question, assistantResponse.ToString()));
    }
}

public record ConversationTurn(string UserMessage, string AssistantMessage);
