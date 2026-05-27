# Cancellation

Cancellation pro Enqueue wird sowohl vor dem Dispatch als auch während der Ausführung verfolgt.

- Wenn ein Token bereits abgebrochen ist, wenn der Scheduler den Job dispatchen möchte, wird der Job als `Canceled` markiert, nicht ausgeführt und der Dispatcher-Slot sofort freigegeben.
- Wenn die Cancellation nach dem Start der Ausführung eintritt, beobachtet der laufende Job das Token, die Queue veröffentlicht `Canceled` und der Slot wird während der Finalisierung freigegeben.
