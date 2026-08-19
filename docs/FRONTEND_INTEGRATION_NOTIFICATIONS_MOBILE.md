# Intégration mobile — Notifications in-app (SignalR)

Guide Flutter / Dart pour recevoir les notifications **in-app en temps réel** via le hub Kenergie, et les synchroniser avec l’API REST.

**Prérequis backend (phases 1–2) :**
- Événement canonique : `ReceiveNotification`
- `MarkNotificationAsRead` persiste `EstLue` / `DateLecture` en base
- Hub : `[Authorize]` JWT

---

## 1. Prérequis

| Élément | Valeur |
|--------|--------|
| Hub | `{BASE_URL}/hubs/notifications` |
| Ex. dev | `https://dev-kenergie.asdc-rdc.org.asdc-rdc.org/hubs/notifications` |
| Auth | Même JWT Bearer que l’API REST (`IdUtilisateur` = claim NameIdentifier / `sub`) |
| Package Dart | [`signalr_netcore`](https://pub.dev/packages/signalr_netcore) |

```yaml
# pubspec.yaml
dependencies:
  signalr_netcore: ^1.3.7   # vérifier la dernière version sur pub.dev
```

**Rôle des canaux :**

| Canal | Quand |
|-------|--------|
| **SignalR** | App au premier plan, connectée au hub |
| **Firebase Push** | App en arrière-plan / tuée (déjà envoyé côté API si device enregistré) |
| **REST** | Historique au cold start, rattrapage hors ligne |

---

## 2. Authentification SignalR

Le hub exige un JWT valide. Les clients WebSocket passent souvent le token en query `?access_token=…` (l’API lit ce paramètre pour `/hubs/*`).

```dart
import 'package:signalr_netcore/signalr_client.dart';

HubConnection buildNotificationHub({
  required String baseUrl,
  required Future<String?> Function() getAccessToken,
}) {
  final hubUrl = '$baseUrl/hubs/notifications';

  return HubConnectionBuilder()
      .withUrl(
        hubUrl,
        options: HttpConnectionOptions(
          accessTokenFactory: () async {
            final token = await getAccessToken();
            if (token == null || token.isEmpty) {
              throw StateError('JWT manquant pour SignalR');
            }
            // Sans préfixe "Bearer" dans accessTokenFactory
            return token.startsWith('Bearer ')
                ? token.substring(7)
                : token;
          },
        ),
      )
      .withAutomaticReconnect()
      .build();
}
```

---

## 3. Cycle de vie

1. **Après login** → `hub.start()`
2. **Sur logout / token expiré** → `hub.stop()` puis reconnexion après refresh token
3. **Reconnexion auto** → ré-enregistrer les handlers **avant** `start`, une seule fois (éviter les doublons)

```dart
class NotificationRealtimeService {
  HubConnection? _hub;
  final _items = <InAppNotification>[]; // état local / Riverpod / Bloc

  Future<void> connect({
    required String baseUrl,
    required Future<String?> Function() getAccessToken,
  }) async {
    await disconnect();

    _hub = buildNotificationHub(baseUrl: baseUrl, getAccessToken: getAccessToken);

    _hub!.on('ReceiveNotification', _onReceiveNotification);
    _hub!.on('NotificationMarkedAsRead', _onMarkedAsRead);
    _hub!.on('NotificationMarkFailed', _onMarkFailed);

    await _hub!.start();
  }

  Future<void> disconnect() async {
    final hub = _hub;
    _hub = null;
    if (hub != null) {
      await hub.stop();
    }
  }
}
```

---

## 4. Contrat des événements

### 4.1 `ReceiveNotification` (à écouter — canonique)

Payload JSON camelCase aligné sur le modèle `Notification` :

```json
{
  "idNotification": 123,
  "titre": "Paiement confirmé",
  "contenu": "Votre paiement de 15 000 CDF a été validé.",
  "typeNotification": "Paiement",
  "estLue": false,
  "dateCreation": "2026-08-15T14:30:00",
  "priorite": "INFO",
  "icone": null,
  "lienAction": null,
  "idDestinataire": 7,
  "payloadJson": "{\"idPaiement\":\"456\"}"
}
```

- `idNotification > 0` : notification persistée → utilisable pour `MarkNotificationAsRead`
- `idNotification == 0` : envoi sans persist (rare / plainte sans enregistrement) → affichage local seulement

### 4.2 `ReceiveCustomNotification` (déprécié)

Ancien `{ title, message, type }`. **Ne pas** brancher les nouvelles apps.

### 4.3 Marquer comme lue

```dart
Future<void> markAsRead(int idNotification) async {
  if (idNotification <= 0) return;
  await _hub!.invoke('MarkNotificationAsRead', args: <Object>[idNotification]);
}
```

| Événement serveur | Signification |
|-------------------|---------------|
| `NotificationMarkedAsRead` | `id` (int) — succès / déjà lue |
| `NotificationMarkFailed` | `{ notificationId, reason }` — `not_found` \| `forbidden` \| `unauthorized` |

---

## 5. Snippet handlers + modèle

```dart
class InAppNotification {
  InAppNotification({
    required this.idNotification,
    required this.titre,
    required this.contenu,
    required this.typeNotification,
    required this.estLue,
    required this.dateCreation,
    this.priorite,
    this.payloadJson,
  });

  final int idNotification;
  final String titre;
  final String contenu;
  final String typeNotification;
  final bool estLue;
  final DateTime? dateCreation;
  final String? priorite;
  final String? payloadJson;

  factory InAppNotification.fromJson(Map<String, dynamic> json) {
    return InAppNotification(
      idNotification: (json['idNotification'] as num?)?.toInt() ?? 0,
      titre: json['titre'] as String? ?? '',
      contenu: json['contenu'] as String? ?? '',
      typeNotification: json['typeNotification'] as String? ?? 'INFO',
      estLue: json['estLue'] as bool? ?? false,
      dateCreation: json['dateCreation'] != null
          ? DateTime.tryParse(json['dateCreation'].toString())
          : null,
      priorite: json['priorite'] as String?,
      payloadJson: json['payloadJson'] as String?,
    );
  }
}

void _onReceiveNotification(List<Object?>? args) {
  if (args == null || args.isEmpty) return;
  final raw = args.first;
  final map = raw is Map
      ? Map<String, dynamic>.from(raw as Map)
      : <String, dynamic>{};
  final n = InAppNotification.fromJson(map);
  // Insérer en tête de liste + snackbar / badge
}

void _onMarkedAsRead(List<Object?>? args) {
  final id = (args?.first as num?)?.toInt();
  if (id == null) return;
  // Mettre estLue = true dans l’état local
}

void _onMarkFailed(List<Object?>? args) {
  // Logger reason ; éventuellement toast
}
```

> Selon la version de `signalr_netcore`, le callback peut recevoir un `Object` unique plutôt qu’une `List`. Adapter le parsing (`args` vs map directe).

---

## 6. API REST complémentaire

Hydrater au démarrage (cold start), puis laisser SignalR alimenter le live.

| Méthode | Route | Usage |
|---------|-------|--------|
| GET | `/api/Notification/destinataire/{idUtilisateur}` | Historique |
| GET | `/api/Notification/destinataire/{idUtilisateur}/non-lues` | Badge non lues |
| PUT | `/api/Notification/{id}/marquer-lue` | Marquer lue **sans** SignalR (fallback) |
| PUT | `/api/Notification/destinataire/{idUtilisateur}/marquer-toutes-lues` | Tout marquer |

Pattern recommandé :

1. Login → REST non-lues / liste
2. `hub.start()` → `ReceiveNotification`
3. Tap notification → `invoke('MarkNotificationAsRead', id)` **ou** `PUT …/marquer-lue` si hub down
4. Logout → `hub.stop()`

`idUtilisateur` = id du compte connecté (même que le JWT), pas l’`IdClient`.

---

## 7. Checklist QA mobile

- [ ] Connexion hub OK avec JWT valide (pas de 401 sur negotiate)
- [ ] Réception `ReceiveNotification` après un paiement / diffusion facture / campagne in-app
- [ ] `idNotification` > 0 pour les flux via `NotificationSender`
- [ ] `MarkNotificationAsRead` → badge diminue ; refresh REST confirme `estLue: true`
- [ ] Autre utilisateur ne peut pas marquer (raison `forbidden`)
- [ ] Reconnect après coupure réseau reprend les handlers
- [ ] Logout coupe le hub (pas de fuite de connexion)

---

## 8. Hors scope

- Hub `/hubs/dashboard` (dashboards staff, `PaiementElectroniqueStatusChanged` par société)
- Enregistrement device FCM (voir module push existant)
- Guide web Vue/React : [`SIGNALR_FRONTEND_GUIDE.md`](./SIGNALR_FRONTEND_GUIDE.md)

---

## 9. Liens

- Guide SignalR général : [`SIGNALR_FRONTEND_GUIDE.md`](./SIGNALR_FRONTEND_GUIDE.md)
- Index modules : [`INDEX_MODULES_FRONTEND.md`](./INDEX_MODULES_FRONTEND.md)
