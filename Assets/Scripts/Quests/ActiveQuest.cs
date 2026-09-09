using UnityEngine;

public enum QuestStatus
{
    Active,
    ReadyToTurnIn,
    Completed
}

public class ActiveQuest
{
    public QuestDefinition Definition;
    public QuestStatus Status;

    public ActiveQuest(QuestDefinition definition)
    {
        Definition = definition;
        Status = QuestStatus.Active;
    }
}
