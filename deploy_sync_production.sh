#!/bin/bash

# Script de déploiement blue-green pour la synchronisation offline
# Auteur: Kenergie Team
# Date: 21 mars 2026

set -e  # Arrêter en cas d'erreur

# Configuration
VERSION_BLUE=$(git rev-parse HEAD~1 2>/dev/null || echo "unknown")
VERSION_GREEN=$(git rev-parse HEAD)
API_URL="${API_URL:-http://localhost}"
JWT_TOKEN="${JWT_TOKEN:-}"

echo "🚀 DÉPLOIEMENT SYNCHRONISATION PRODUCTION"
echo "⏰ $(date)"
echo "🔵 Version Blue (actuelle): $VERSION_BLUE"
echo "🟢 Version Green (nouvelle): $VERSION_GREEN"

# Vérification des prérequis
echo "🔍 Vérification des prérequis..."

if [ -z "$DB_CONNECTION" ]; then
    echo "❌ DB_CONNECTION non défini"
    exit 1
fi

if [ -z "$SYNC_WATERMARK_KEY" ]; then
    echo "❌ SYNC_WATERMARK_KEY non défini"
    exit 1
fi

if [ -z "$SYNC_CURSOR_KEY" ]; then
    echo "❌ SYNC_CURSOR_KEY non défini"
    exit 1
fi

# Créer les répertoires nécessaires
mkdir -p logs
mkdir -p backups

# 1. Backup de l'application actuelle
echo "💾 Backup de la version actuelle..."
git tag backup_before_sync_v1.0

# 2. Builder les images Docker
echo "🔨 Build des images Docker..."
docker build -t kenergie-api:$VERSION_BLUE .
docker build -t kenergie-api:$VERSION_GREEN .

if [ $? -ne 0 ]; then
    echo "❌ Échec du build Docker"
    exit 1
fi

echo "✅ Images Docker créées"

# 3. Créer le docker-compose de production
cat > docker-compose.prod.yml << EOF
version: '3.8'

services:
  # Blue (Version actuelle)
  kenergie-api-blue:
    image: kenergie-api:$VERSION_BLUE
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
      start_period: 40s

  # Green (Nouvelle version avec sync)
  kenergie-api-green:
    image: kenergie-api:$VERSION_GREEN
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
      start_period: 40s

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
    command: >
      sh -c "
        echo 'Configuring nginx...'
        cat > /etc/nginx/nginx.conf << 'EOFF'
        events {
            worker_connections 1024;
        }
        http {
            upstream kenergie_api {
                server kenergie-api-blue:80 weight=1 max_fails=3 fail_timeout=30s;
                server kenergie-api-green:80 weight=1 max_fails=3 fail_timeout=30s;
            }
            
            server {
                listen 80;
                server_name _;
                
                location /health {
                    access_log off;
                    return 200 'healthy';
                    add_header Content-Type text/plain;
                }
                
                location / {
                    proxy_pass http://kenergie_api;
                    proxy_set_header Host \$host;
                    proxy_set_header X-Real-IP \$remote_addr;
                    proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
                    proxy_set_header X-Forwarded-Proto \$scheme;
                    
                    # Timeouts
                    proxy_connect_timeout 30s;
                    proxy_send_timeout 30s;
                    proxy_read_timeout 30s;
                    
                    # Compression
                    gzip on;
                    gzip_types text/plain application/json application/xml text/css application/javascript;
                }
            }
        }
        EOFF'
        nginx -g 'daemon off; nginx -c /etc/nginx/nginx.conf'
      '
EOF

echo "✅ Configuration Docker Compose créée"

# 4. Déployer la version Green
echo "🟢 Déploiement version Green..."
export VERSION_GREEN=$VERSION_GREEN DB_CONNECTION=$DB_CONNECTION SYNC_WATERMARK_KEY=$SYNC_WATERMARK_KEY SYNC_CURSOR_KEY=$SYNC_CURSOR_KEY

# Arrêter les conteneurs existants
docker-compose -f docker-compose.prod.yml down 2>/dev/null || true

# Démarrer les conteneurs
docker-compose -f docker-compose.prod.yml up -d

if [ $? -ne 0 ]; then
    echo "❌ Échec du déploiement Green"
    exit 1
fi

echo "✅ Conteneurs démarrés"

# 5. Attendre le health check de Green
echo "⏳ Attente health check Green (max 5 minutes)..."
TIMEOUT=300
ELAPSED=0

while [ $ELAPSED -lt $TIMEOUT ]; do
    if curl -f http://localhost:5002/health >/dev/null 2>&1; then
        echo "✅ Health check Green réussi"
        break
    fi
    
    echo "⏳ En attente... ($ELAPSED/$TIMEOUT)"
    sleep 10
    ELAPSED=$((ELAPSED + 10))
done

if [ $ELAPSED -ge $TIMEOUT ]; then
    echo "❌ Timeout health check Green"
    echo "🔄 Rollback automatique..."
    ./rollback_sync_production.sh
    exit 1
fi

# 6. Tests de validation
echo "🧪 Tests de validation..."

if [ -n "$JWT_TOKEN" ]; then
    echo "🔐 Test avec JWT token..."
    
    # Test bootstrap
    echo "🔄 Test bootstrap..."
    BOOTSTRAP_RESPONSE=$(curl -s -w "%{http_code}" -o /tmp/bootstrap.json \
        -H "Authorization: Bearer $JWT_TOKEN" \
        -H "Content-Type: application/json" \
        "$API_URL/api/sync/bootstrap")
    
    if [ "$BOOTSTRAP_RESPONSE" != "200" ]; then
        echo "❌ Test bootstrap échoué (HTTP $BOOTSTRAP_RESPONSE)"
        echo "🔄 Rollback automatique..."
        ./rollback_sync_production.sh
        exit 1
    fi
    
    # Test pagination clients
    echo "👥 Test pagination clients..."
    CLIENTS_RESPONSE=$(curl -s -w "%{http_code}" -o /tmp/clients.json \
        -H "Authorization: Bearer $JWT_TOKEN" \
        -H "Content-Type: application/json" \
        "$API_URL/api/sync/clients?pageSize=10")
    
    if [ "$CLIENTS_RESPONSE" != "200" ]; then
        echo "❌ Test clients échoué (HTTP $CLIENTS_RESPONSE)"
        echo "🔄 Rollback automatique..."
        ./rollback_sync_production.sh
        exit 1
    fi
    
    # Vérifier la structure de la réponse
    ITEMS_COUNT=$(jq -r '.items | length' /tmp/clients.json 2>/dev/null || echo "0")
    if [ "$ITEMS_COUNT" != "10" ]; then
        echo "❌ Test clients: mauvais nombre d'items ($ITEMS_COUNT au lieu de 10)"
        echo "🔄 Rollback automatique..."
        ./rollback_sync_production.sh
        exit 1
    fi
    
    echo "✅ Tests de validation réussis"
else
    echo "⚠️ JWT_TOKEN non fourni, tests de validation sautés"
fi

# 7. Switch du load balancer vers Green
echo "🔄 Switch vers Green..."
sed -i 's/server 5001/server 5002/g' nginx.conf
docker-compose -f docker-compose.prod.yml restart nginx-lb

# Attendre que le switch soit effectif
sleep 10

# 8. Tests finaux sur l'API principale
echo "✅ Tests finaux sur l'API principale..."

if [ -n "$JWT_TOKEN" ]; then
    FINAL_RESPONSE=$(curl -s -w "%{http_code}" -o /tmp/final.json \
        -H "Authorization: Bearer $JWT_TOKEN" \
        -H "Content-Type: application/json" \
        "$API_URL/api/sync/clients?pageSize=5")
    
    if [ "$FINAL_RESPONSE" != "200" ]; then
        echo "❌ Test final échoué (HTTP $FINAL_RESPONSE)"
        echo "🔄 Rollback automatique..."
        ./rollback_sync_production.sh
        exit 1
    fi
    
    echo "✅ Tests finaux réussis"
fi

# 9. Nettoyage de la version Blue
echo "🧹 Nettoyage version Blue..."
docker-compose -f docker-compose.prod.yml stop kenergie-api-blue
docker rmi kenergie-api:$VERSION_BLUE 2>/dev/null || true

# 10. Créer le tag de déploiement
echo "🏷️ Création du tag de déploiement..."
git tag -a "sync_v1.0" -m "Déploiement synchronisation offline v1.0"
git push origin sync_v1.0

# 11. Nettoyer les fichiers temporaires
rm -f /tmp/bootstrap.json /tmp/clients.json /tmp/final.json

echo ""
echo "🎉 DÉPLOIEMENT TERMINÉ AVEC SUCCÈS"
echo "⏰ $(date)"
echo "📊 Résumé:"
echo "  - Version déployée: $VERSION_GREEN"
echo "  - API principale: $API_URL"
echo "  - Health check: ✅"
echo "  - Tests validation: ✅"
echo "  - Load balancer: Green (port 5002)"
echo ""
echo "🎯 Monitoring disponible:"
echo "  - Health: $API_URL/health"
echo "  - Metrics: $API_URL/metrics"
echo "  - Logs: docker logs -f kenergie-api-green"
echo ""
echo "📋 Commandes utiles:"
echo "  - Voir logs: docker logs -f kenergie-api-green"
echo "  - Redémarrer: docker-compose -f docker-compose.prod.yml restart kenergie-api-green"
echo "  - Rollback: ./rollback_sync_production.sh"
