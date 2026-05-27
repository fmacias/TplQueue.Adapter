# Cross-Queue Ownership

Die erste Queue, die eine geteilte Job-Instanz enqueuet, übernimmt die Ausführungs-Ownership für genau diese Instanz.

Spätere Queues dürfen diesen Job weiterhin als Abhängigkeit referenzieren, ihn aber nicht erneut ausführen. `CrossQueueId` ist beobachtbarer Runtime-State und keine vom Benutzer konfigurierbare Setup-Option.
