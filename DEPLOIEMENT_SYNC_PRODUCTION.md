# 🚀 **Déploiement Synchronisation Offline - Production**

## 📋 **Vue d'ensemble**

**Étape 4:** Déploiement de la synchronisation offline en production avec stratégie zéro-risque et monitoring complet.

---

## 🔴 **Prérequis Critiques (Obligatoires)**

### **1. Backup Complet**
```bash
# Backup de la base de données complète
mysqldump --single-transaction --routines --triggers kenergie > backup_pre_sync_$(date +%Y%m%d_%H%M%S).sql

# Backup du code actuel
git tag backup_before_sync_v1.0
git push origin backup_before_sync_v1.0
```

### **2. Environnement de Staging**
```bash
# Vérifier que staging est à jour
git checkout main
git pull origin main
dotnet build --configuration Release

# Déployer en staging pour validation
docker-compose -f docker-compose.staging.yml up -d
```

### **3. Clés de Sécurité**
```json
// appsettings.Production.json
{
  "Sync": {
    "WatermarkKey": "VOTRE_CLE_HMAC_WATERMARK_256_BITS_MINIMUM",
    "CursorKey": "VOTRE_CLE_HMAC_CURSOR_256_BITS_MINIMUM"
  }
}
```

---

## 🟢 **Étape 4.1: Migration Database**

### **🔧 Script de Migration Sécurisé**
```bash
#!/bin/bash
# migration_sync_production.sh

set -e  # Arrêter en cas d'erreur

echo "🔴 DÉBUT MIGRATION SYNCHRONISATION"
echo "⏰ $(date)"

# 1. Vérifier la connexion
echo "📡 Test connexion base de données..."
dotnet ef database update --no-build --verbose

# 2. Vérifier les nouvelles colonnes
echo "🔍 Vérification des colonnes ajoutées..."
mysql -h $DB_HOST -u $DB_USER -p$DB_PASSWORD $DB_NAME -e "
    SELECT COLUMN_NAME 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = '$DB_NAME' 
    AND TABLE_NAME IN ('Clients', 'Paiements')
    AND COLUMN_NAME IN ('UpdatedAt', 'IsDeleted', 'ClientRequestId')
    ORDER BY TABLE_NAME, COLUMN_NAME;
"

# 3. Vérifier les index
echo "📊 Vérification des index créés..."
mysql -h $DB_HOST -u $DB_USER -p$DB_PASSWORD $DB_NAME -e "
    SHOW INDEX FROM Clients WHERE Key_name LIKE '%Sync%';
    SHOW INDEX FROM Paiements WHERE Key_name LIKE '%Idempotent%';
    SHOW INDEX FROM ClientFactures WHERE Key_name LIKE '%Sync%';
"

# 4. Validation des données
echo "✅ Validation des données..."
mysql -h $DB_HOST -u $DB_USER -p$DB_PASSWORD $DB_NAME -e "
    SELECT COUNT(*) as total_clients FROM Clients;
    SELECT COUNT(*) as clients_with_updatedat FROM Clients WHERE UpdatedAt IS NOT NULL;
    SELECT COUNT(*) as clients_not_deleted FROM Clients WHERE IsDeleted = 0;
    SELECT COUNT(*) as paiements_with_requestid FROM Paiements WHERE ClientRequestId IS NOT NULL;
"

echo "✅ MIGRATION TERMINÉE"
echo "⏰ $(date)"
```

### **🚀 Exécution de la Migration**
```bash
# Exécuter la migration
chmod +x migration_sync_production.sh
./migration_sync_production.sh

# Vérifier le succès
if [ $? -eq 0 ]; then
    echo "✅ Migration réussie"
else
    echo "❌ Migration échouée - Annulation"
    # Rollback si nécessaire
    git checkout backup_before_sync_v1.0
    exit 1
fi
```

---

## 🟡 **Étape 4.2: Déploiement Blue-Green**

### **🐳 Docker Compose Production**
```yaml
# docker-compose.prod.yml
version: '3.8'

services:
  # Blue (Version actuelle)
  kenergie-api-blue:
    image: kenergie-api:${VERSION_BLUE}
    container_name: kenergie-api-blue
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=${DB_CONNECTION}
      - Sync__WatermarkKey=${SYNC_WATERMARK_KEY}
      - Sync__CursorKey=${SYNC_CURSOR_KEY}
    ports:
      - "5001:80"
    volumes:
      - ./logs:/app/logs
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost/health"]
      interval: 30s
      timeout: 10s
      retries: 3

  # Green (Nouvelle version avec sync)
  kenergie-api-green:
    image: kenergie-api:${VERSION_GREEN}
    container_name: kenergie-api-green
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=${DB_CONNECTION}
      - Sync__WatermarkKey=${SYNC_WATERMARK_KEY}
      - Sync__CursorKey=${SYNC_CURSOR_KEY}
    ports:
      - "5002:80"
    volumes:
      - ./logs:/app/logs
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost/health"]
      interval: 30s
      timeout: 10s
      retries: 3

  # Load Balancer
  nginx-lb:
    image: nginx:alpine
    container_name: kenergie-lb
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./nginx.conf:/etc/nginx/nginx.conf
    depends_on:
      - kenergie-api-blue
      - kenergie-api-green
```

### **🔄 Script de Déploiement**
```bash
#!/bin/bash
# deploy_sync_production.sh

set -e

VERSION_BLUE=$(git rev-parse HEAD~1)
VERSION_GREEN=$(git rev-parse HEAD)
DB_CONNECTION="$1"
SYNC_WATERMARK_KEY="$2"
SYNC_CURSOR_KEY="$3"

echo "🚀 DÉPLOIEMENT SYNCHRONISATION PRODUCTION"
echo "🔵 Version Blue (actuelle): $VERSION_BLUE"
echo "🟢 Version Green (nouvelle): $VERSION_GREEN"

# 1. Builder les images
echo "🔨 Build des images Docker..."
docker build -t kenergie-api:$VERSION_BLUE .
docker build -t kenergie-api:$VERSION_GREEN .

# 2. Déployer la version Green
echo "🟢 Déploiement version Green..."
export VERSION_GREEN=$VERSION_GREEN DB_CONNECTION=$DB_CONNECTION SYNC_WATERMARK_KEY=$SYNC_WATERMARK_KEY SYNC_CURSOR_KEY=$SYNC_CURSOR_KEY
docker-compose -f docker-compose.prod.yml up -d kenergie-api-green

# 3. Attendre le health check
echo "⏳ Attente health check Green..."
timeout 300 bash -c 'until curl -f http://localhost:5002/health; do sleep 5; done'

# 4. Tests de validation
echo "🧪 Tests de validation..."
./tests_sync_validation.sh http://localhost:5002

# 5. Switch du load balancer
echo "🔄 Switch vers Green..."
sed -i 's/server 5001/server 5002/g' nginx.conf
docker-compose -f docker-compose.prod.yml restart nginx-lb

# 6. Tests finaux
echo "✅ Tests finaux..."
./tests_sync_validation.sh http://localhost

# 7. Nettoyage
echo "🧹 Nettoyage version Blue..."
docker-compose -f docker-compose.prod.yml stop kenergie-api-blue
docker rmi kenergie-api:$VERSION_BLUE

echo "✅ DÉPLOIEMENT TERMINÉ"
echo "🟢 Version active: $VERSION_GREEN"
```

---

## 🟣 **Étape 4.3: Tests de Validation**

### **🧪 Script de Tests Automatisés**
```bash
#!/bin/bash
# tests_sync_validation.sh

API_URL="$1"
echo "🧪 TESTS DE VALIDATION - $API_URL"

# 1. Test de santé
echo "🏥 Test santé..."
curl -f "$API_URL/health" || exit 1

# 2. Test bootstrap
echo "🔄 Test bootstrap..."
curl -X GET "$API_URL/api/sync/bootstrap" \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -H "Content-Type: application/json" | jq '.supportsDelta' | grep true || exit 1

# 3. Test pagination clients
echo "👥 Test pagination clients..."
RESPONSE=$(curl -X GET "$API_URL/api/sync/clients?pageSize=10" \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -H "Content-Type: application/json")

echo "$RESPONSE" | jq '.items | length' | grep 10 || exit 1
echo "$RESPONSE" | jq '.hasMore' || exit 1

# 4. Test paiements batch
echo "💰 Test paiements batch..."
curl -X POST "$API_URL/api/sync/payments/batch" \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "items": [{
      "clientRequestId": "test-' $(date +%s) '",
      "idClient": 1,
      "montantPaye": 100,
      "datePaiementUtc": "'$(date -u +%Y-%m-%dT%H:%M:%SZ)'",
      "methodePaiement": "Test"
    }]
  }' | jq '.summary.created' | grep 1 || exit 1

# 5. Test de charge léger
echo "⚡ Test charge légère..."
for i in {1..10}; do
  curl -X GET "$API_URL/api/sync/clients?pageSize=100" \
    -H "Authorization: Bearer $JWT_TOKEN" \
    -H "Content-Type: application/json" > /dev/null &
done

wait
echo "✅ TOUS LES TESTS RÉUSSIS"
```

---

## 🟠 **Étape 4.4: Monitoring & Alertes**

### **📊 Configuration Monitoring**
```yaml
# prometheus.yml
global:
  scrape_interval: 15s

scrape_configs:
  - job_name: 'kenergie-api'
    static_configs:
      - targets: ['localhost:5000']
    metrics_path: '/metrics'
    scrape_interval: 5s

rule_files:
  - "alert_rules.yml"
```

```yaml
# alert_rules.yml
groups:
  - name: kenergie_sync_alerts
    rules:
      - alert: SyncAPIHighErrorRate
        expr: rate(http_requests_total{status=~"5.."}[5m]) > 0.1
        for: 2m
        labels:
          severity: critical
        annotations:
          summary: "Taux d'erreur élevé sur API Sync"
          description: "Taux d'erreur de {{ $value }} erreurs/secondes"

      - alert: SyncAPISlowResponse
        expr: histogram_quantile(0.95, rate(http_request_duration_seconds_bucket[5m])) > 2
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: "Réponses lentes sur API Sync"
          description: "95ème percentile à {{ $value }} secondes"

      - alert: SyncDatabaseConnections
        expr: mysql_global_status_threads_connected > 80
        for: 1m
        labels:
          severity: warning
        annotations:
          summary: "Connexions database élevées"
          description: "{{ $value }} connexions actives"
```

### **📈 Dashboard Grafana**
```json
{
  "dashboard": {
    "title": "Kenergie Sync Monitoring",
    "panels": [
      {
        "title": "Requêtes Sync/minute",
        "type": "graph",
        "targets": [
          {
            "expr": "rate(http_requests_total{job=\"kenergie-api\"}[5m])",
            "legendFormat": "{{method}} {{endpoint}}"
          }
        ]
      },
      {
        "title": "Taux d'erreur",
        "type": "singlestat",
        "targets": [
          {
            "expr": "rate(http_requests_total{job=\"kenergie-api\",status=~\"5..\"}[5m]) * 100",
            "legendFormat": "Erreur %"
          }
        ]
      },
      {
        "title": "Temps réponse moyen",
        "type": "graph",
        "targets": [
          {
            "expr": "histogram_quantile(0.50, rate(http_request_duration_seconds_bucket[5m]))",
            "legendFormat": "50ème percentile"
          }
        ]
      }
    ]
  }
}
```

---

## 🟢 **Étape 4.5: Documentation Mobile**

### **📱 Guide Flutter Intégration**
```markdown
# Guide Intégration Flutter - Sync Offline

## Configuration
```dart
class SyncConfig {
  static const String baseUrl = 'https://api.kenergie.com';
  static const int pageSize = 1000;
  static const Duration timeout = Duration(seconds: 30);
}
```

## Bootstrap
```dart
Future<SyncBootstrap> bootstrap() async {
  final response = await http.get(
    Uri.parse('${SyncConfig.baseUrl}/api/sync/bootstrap'),
    headers: {'Authorization': 'Bearer $token'},
  );
  
  if (response.statusCode == 200) {
    return SyncBootstrap.fromJson(json.decode(response.body));
  }
  throw Exception('Bootstrap failed');
}
```

## Synchronisation Complète
```dart
Future<void> fullSync() async {
  final bootstrap = await SyncService.bootstrap();
  
  // Sync clients
  await _syncEntities<ClientSyncDto>(
    '/api/sync/clients',
    bootstrap.snapshot,
  );
  
  // Sync arrears
  await _syncEntities<ArrearSyncDto>(
    '/api/sync/arrears',
    bootstrap.snapshot,
  );
}
```

## Delta Sync Quotidien
```dart
Future<void> deltaSync() async {
  final bootstrap = await SyncService.bootstrap();
  
  // Sync clients modifiés
  await _syncEntitiesDelta<ClientSyncDto>(
    '/api/sync/clients',
    bootstrap.snapshot,
    _lastSyncWatermark,
  );
  
  // Sync arrears modifiés
  await _syncEntitiesDelta<ArrearSyncDto>(
    '/api/sync/arrears',
    bootstrap.snapshot,
    _lastSyncWatermark,
  );
  
  // Nettoyer les suppressions
  await _syncDeletions(bootstrap.snapshot, _lastSyncWatermark);
}
```
```

---

## 🔵 **Étape 4.6: Rollback Plan**

### **🚨 Script de Rollback**
```bash
#!/bin/bash
# rollback_sync_production.sh

set -e

echo "🚨 ROLLBACK SYNCHRONISATION"

# 1. Arrêter le nouveau conteneur
docker-compose -f docker-compose.prod.yml stop kenergie-api-green

# 2. Restaurer l'ancienne version
git checkout backup_before_sync_v1.0
docker build -t kenergie-api:rollback .

# 3. Redémarrer l'ancienne version
docker-compose -f docker-compose.prod.yml up -d kenergie-api-blue

# 4. Switch du load balancer
sed -i 's/server 5002/server 5001/g' nginx.conf
docker-compose -f docker-compose.prod.yml restart nginx-lb

# 5. Nettoyer
docker rmi kenergie-api:$VERSION_GREEN
docker-compose -f docker-compose.prod.yml rm -f kenergie-api-green

echo "✅ ROLLBACK TERMINÉ"
echo "🔵 Version active: backup_before_sync_v1.0"
```

### **📋 Checklist Rollback**
- [ ] Backup database disponible
- [ ] Git tag de backup créé
- [ ] Script de rollback testé
- [ ] Équipe d'astreinte prévenue
- [ ] Communication utilisateurs préparée

---

## 🎯 **Étape 4.7: Go/No-Go Decision**

### **✅ Critères de Go:**
1. **Migration réussie** sans erreur
2. **Tests validation** 100% passants
3. **Performance** < 2 secondes par requête
4. **Monitoring** vert depuis 5 minutes
5. **Documentation** mobile disponible

### **❌ Critères de No-Go:**
1. **Erreur migration** ou corruption données
2. **Tests validation** échoués
3. **Performance** > 5 secondes par requête
4. **Alertes monitoring** critiques
5. **Régression** fonctionnelle détectée

### **🎛️ Gates de Déploiement:**
```bash
# Automated gates
./tests_sync_validation.sh $API_URL
./performance_tests.sh $API_URL
./security_scan.sh $API_URL

# Manual gates
echo "✅ Migration validée ? (y/n)"
read -r migration_ok

echo "✅ Tests passants ? (y/n)"
read -r tests_ok

if [[ "$migration_ok" == "y" && "$tests_ok" == "y" ]]; then
    echo "🟢 GO - Déploiement autorisé"
    ./deploy_sync_production.sh $DB_CONNECTION $SYNC_WATERMARK_KEY $SYNC_CURSOR_KEY
else
    echo "🔴 NO-GO - Déploiement annulé"
    exit 1
fi
```

---

## 📞 **Support & Monitoring**

### **📊 Métriques Clés:**
- **Taux de succès sync:** > 99.5%
- **Temps moyen sync:** < 30 secondes
- **Volume données transférées:** < 10 MB/jour/agent
- **Taux d'erreur API:** < 0.1%
- **Connexions concurrentes:** < 80% du pool

### **🚨 Alertes Critiques:**
- **API down** > 30 secondes
- **Taux d'erreur** > 1%
- **Database timeout** > 10 secondes
- **Mémoire > 80%** utilisation
- **CPU > 90%** pendant > 5 minutes

### **📞 Équipe d'Astreinte:**
- **Développeur Senior:** +243123456789
- **DevOps:** +243123456788
- **DBA:** +243123456789
- **Product Owner:** +243123456790

---

## 🎉 **Conclusion**

Le déploiement de la synchronisation offline nécessite une **stratégie prudente** avec:

1. **Backup complet** pré-déploiement
2. **Migration sécurisée** avec validation
3. **Déploiement blue-green** pour zero downtime
4. **Tests automatisés** pour validation
5. **Monitoring complet** avec alertes
6. **Rollback rapide** en cas de problème
7. **Documentation mobile** pour intégration

**Avec cette approche, le déploiement peut se faire en toute sécurité!** 🚀✨

---

*Guide de déploiement créé le 21 mars 2026 - Stratégie zéro-risque*
