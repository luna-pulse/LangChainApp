using LangChain.Chains;
using LangChain.Chains.StackableChains.Agents;

namespace PersonAgent.Agent;

public static class LangChainAgentExecutor
{
    public static ReActAgentExecutorChain Create(IChatClient chatClient, string prompt, int maxActions) =>
        Chain.ReActAgentExecutor(chatClient, prompt, maxActions: maxActions);
}
