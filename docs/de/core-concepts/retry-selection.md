# Retry Selection

Die Retry-Selection folgt einem einfachen Vorrangmodell.

1. Retry-Policy auf Root-Ebene
2. Retry-Policy auf Queue-Ebene
3. Standard-`NoRetryPolicy`

Dadurch kann eine Queue die allgemeine betriebliche Policy definieren, während ein bestimmter Graph sie bei Bedarf weiterhin überschreiben kann.
