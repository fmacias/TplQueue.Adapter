# Job-Graphen

TplQueue-Graphen sind per Design abhängigkeitssensitiv und root-terminal.

## Root-Terminal-Regel

Die aktuelle Runtime verhindert, dass Non-Root-Jobs von einem `IJobRoot` abhängen. Praktisch bedeutet das: `job.After(root)` ist ungültig, weil die enqueuebare Root der letzte Knoten im Graphen bleiben muss.

Verwenden Sie stattdessen eine dieser gültigen Formen:

```csharp
extract.Then(transform);
transform.Then(root);
```
