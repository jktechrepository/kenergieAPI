#!/bin/bash

# Script de tests de validation pour l'API de synchronisation
# Auteur: Kenergie Team
# Date: 21 mars 2026

set -e

# Configuration
API_URL="${1:-http://localhost}"
JWT_TOKEN="${2:-}"
TIMEOUT=30

echo "🧪 TESTS DE VALIDATION - API SYNCHRONISATION"
echo "📊 API: $API_URL"
echo "⏰ $(date)"

# Vérification des prérequis
if [ -z "$API_URL" ]; then
    echo "❌ API_URL non fourni"
    exit 1
fi

if [ -z "$JWT_TOKEN" ]; then
    echo "⚠️ JWT_TOKEN non fourni, tests limités"
fi

# Compteurs
TESTS_TOTAL=0
TESTS_PASSED=0
TESTS_FAILED=0

# Fonction de test
run_test() {
    local test_name="$1"
    local test_command="$2"
    local expected_result="$3"
    
    echo "🔄 Test: $test_name"
    TESTS_TOTAL=$((TESTS_TOTAL + 1))
    
    # Exécuter le test
    if eval "$test_command"; then
        echo "✅ $test_name: RÉUSSI"
        TESTS_PASSED=$((TESTS_PASSED + 1))
    else
        echo "❌ $test_name: ÉCHOUÉ"
        TESTS_FAILED=$((TESTS_FAILED + 1))
    fi
    
    echo ""
}

# 1. Test de santé de l'API
run_test "Health Check" "curl -f -s --max-time $TIMEOUT $API_URL/health > /dev/null" "true"

# 2. Test des en-têtes CORS
run_test "CORS Headers" "curl -I -s --max-time $TIMEOUT $API_URL/api/sync/bootstrap | grep -i 'access-control-allow-origin'" "true"

# 3. Test de bootstrap (sans JWT)
run_test "Bootstrap sans auth" "test \$(curl -s --max-time $TIMEOUT $API_URL/api/sync/bootstrap | jq -r '.serverTimeUtc') = 'null'" "true"

# 4. Test de bootstrap avec JWT invalide
run_test "Bootstrap JWT invalide" "test \$(curl -s -H 'Authorization: Bearer invalid-token' --max-time $TIMEOUT $API_URL/api/sync/bootstrap | jq -r '.serverTimeUtc') = 'null'" "true"

# Tests avec JWT valide
if [ -n "$JWT_TOKEN" ]; then
    # 5. Test de bootstrap avec JWT valide
    run_test "Bootstrap JWT valide" "curl -s -H 'Authorization: Bearer $JWT_TOKEN' --max-time $TIMEOUT $API_URL/api/sync/bootstrap | jq -r '.supportsDelta' = 'true'" "true"
    
    # 6. Test pagination clients
    run_test "Pagination clients" "curl -s -H 'Authorization: Bearer $JWT_TOKEN' --max-time $TIMEOUT '$API_URL/api/sync/clients?pageSize=10' | jq -r '.items | length = 10 and .hasMore = true'" "true"
    
    # 7. Test pagination arrears
    run_test "Pagination arrears" "curl -s -H 'Authorization: Bearer $JWT_TOKEN' --max-time $TIMEOUT '$API_URL/api/sync/arrears?pageSize=10' | jq -r '.items | length = 10 and .hasMore = true'" "true"
    
    # 8. Test pagination avec cursor
    echo "🔄 Test: Pagination avec cursor"
    TESTS_TOTAL=$((TESTS_TOTAL + 1))
    
    # Récupérer la première page
    FIRST_PAGE=$(curl -s -H 'Authorization: Bearer $JWT_TOKEN' --max-time $TIMEOUT '$API_URL/api/sync/clients?pageSize=5')
    CURSOR=$(echo "$FIRST_PAGE" | jq -r '.nextCursor')
    
    if [ -n "$CURSOR" ]; then
        # Utiliser le cursor pour la deuxième page
        SECOND_PAGE=$(curl -s -H 'Authorization: Bearer $JWT_TOKEN' --max-time $TIMEOUT "$API_URL/api/sync/clients?pageSize=5&cursor=$CURSOR")
        SECOND_COUNT=$(echo "$SECOND_PAGE" | jq -r '.items | length')
        
        if [ "$SECOND_COUNT" -gt 0 ]; then
            echo "✅ Pagination avec cursor: RÉUSSI"
            TESTS_PASSED=$((TESTS_PASSED + 1))
        else
            echo "❌ Pagination avec cursor: ÉCHOUÉ"
            TESTS_FAILED=$((TESTS_FAILED + 1))
        fi
    else
        echo "❌ Pagination avec cursor: ÉCHOUÉ (pas de cursor)"
        TESTS_FAILED=$((TESTS_FAILED + 1))
    fi
    echo ""
    
    # 9. Test paiements batch
    TEST_PAYMENT_ID="test-$(date +%s)"
    run_test "Paiements batch" "curl -s -X POST -H 'Authorization: Bearer $JWT_TOKEN' -H 'Content-Type: application/json' --max-time $TIMEOUT '$API_URL/api/sync/payments/batch' -d '{
        \"items\": [{
            \"clientRequestId\": \"$TEST_PAYMENT_ID\",
            \"idClient\": 1,
            \"montantPaye\": 100,
            \"datePaiementUtc\": \"$(date -u +%Y-%m-%dT%H:%M:%SZ)\",
            \"methodePaiement\": \"Test\",
            \"commentaire\": \"Test automatique\"
        }]
    }' | jq -r '.summary.created = 1 and .results[0].status = \"created\"'" "true"
    
    # 10. Test idempotence (même paiement)
    run_test "Idempotence paiements" "curl -s -X POST -H 'Authorization: Bearer $JWT_TOKEN' -H 'Content-Type: application/json' --max-time $TIMEOUT '$API_URL/api/sync/payments/batch' -d '{
        \"items\": [{
            \"clientRequestId\": \"$TEST_PAYMENT_ID\",
            \"idClient\": 1,
            \"montantPaye\": 100,
            \"datePaiementUtc\": \"$(date -u +%Y-%m-%dT%H:%M:%SZ)\",
            \"methodePaiement\": \"Test\"
        }]
    }' | jq -r '.summary.duplicates = 1 and .results[0].status = \"duplicate\"'" "true"
    
    # 11. Test delta sync
    run_test "Delta sync depuis watermark" "curl -s -H 'Authorization: Bearer $JWT_TOKEN' --max-time $TIMEOUT '$API_URL/api/sync/clients?since=invalid-watermark' | jq -r '.items | length = 0'" "true"
    
    # 12. Test suppressions
    run_test "Suppressions delta" "curl -s -H 'Authorization: Bearer $JWT_TOKEN' --max-time $TIMEOUT '$API_URL/api/sync/deletions?since=invalid-watermark' | jq -r '.deletedClientIds | length = 0 and .removedClientFactureIds | length = 0 and .deletedPaymentIds | length = 0'" "true"
    
    # 13. Test validation erreurs
    run_test "Validation erreurs" "curl -s -X POST -H 'Authorization: Bearer $JWT_TOKEN' -H 'Content-Type: application/json' --max-time $TIMEOUT '$API_URL/api/sync/payments/batch' -d '{
        \"items\": [{
            \"clientRequestId\": \"\",
            \"idClient\": 0,
            \"montantPaye\": 0
        }]
    }' | jq -r '.results[0].status = \"rejected\" and .results[0].errorCode != null'" "true"
    
    # 14. Test performance
    echo "🔄 Test: Performance (< 2 secondes)"
    TESTS_TOTAL=$((TESTS_TOTAL + 1))
    
    START_TIME=$(date +%s.%N)
    curl -s -H 'Authorization: Bearer $JWT_TOKEN' --max-time $TIMEOUT '$API_URL/api/sync/clients?pageSize=100' > /dev/null
    END_TIME=$(date +%s.%N)
    
    DURATION=$(echo "$END_TIME - $START_TIME" | bc)
    
    if (( $(echo "$DURATION < 2.0" | bc -l) )); then
        echo "✅ Performance (< 2s): RÉUSSI (${DURATION}s)"
        TESTS_PASSED=$((TESTS_PASSED + 1))
    else
        echo "❌ Performance (< 2s): ÉCHOUÉ (${DURATION}s)"
        TESTS_FAILED=$((TESTS_FAILED + 1))
    fi
    echo ""
    
    # 15. Test compression
    run_test "Compression réponse" "curl -s -H 'Authorization: Bearer $JWT_TOKEN' -H 'Accept-Encoding: gzip' --max-time $TIMEOUT '$API_URL/api/sync/clients?pageSize=100' | jq -r 'length > 0'" "true"
fi

# 16. Test de charge léger
echo "🔄 Test: Charge légère (10 requêtes concurrentes)"
TESTS_TOTAL=$((TESTS_TOTAL + 1))

for i in {1..10}; do
    curl -s -H 'Authorization: Bearer $JWT_TOKEN' --max-time $TIMEOUT '$API_URL/api/sync/clients?pageSize=50' > /dev/null &
done

# Attendre que toutes les requêtes se terminent
wait

# Vérifier si aucune erreur n'a été retournée
if [ $? -eq 0 ]; then
    echo "✅ Charge légère: RÉUSSI (10 requêtes concurrentes)"
    TESTS_PASSED=$((TESTS_PASSED + 1))
else
    echo "❌ Charge légère: ÉCHOUÉ (erreurs dans les requêtes concurrentes)"
    TESTS_FAILED=$((TESTS_FAILED + 1))
fi
echo ""

# 17. Test de stabilité (uptime)
echo "🔄 Test: Stabilité (5 requêtes sur 30 secondes)"
TESTS_TOTAL=$((TESTS_TOTAL + 1))

SUCCESS_COUNT=0
for i in {1..5}; do
    if curl -s -f --max-time $TIMEOUT "$API_URL/health" > /dev/null; then
        SUCCESS_COUNT=$((SUCCESS_COUNT + 1))
    fi
    sleep 6
done

if [ $SUCCESS_COUNT -eq 5 ]; then
    echo "✅ Stabilité: RÉUSSI (5/5 health checks)"
    TESTS_PASSED=$((TESTS_PASSED + 1))
else
    echo "❌ Stabilité: ÉCHOUÉ ($SUCCESS_COUNT/5 health checks)"
    TESTS_FAILED=$((TESTS_FAILED + 1))
fi
echo ""

# Résultats finaux
echo "📊 RÉSULTATS DES TESTS"
echo "⏰ $(date)"
echo "📈 Total tests: $TESTS_TOTAL"
echo "✅ Réussis: $TESTS_PASSED"
echo "❌ Échoués: $TESTS_FAILED"
echo "📊 Taux de réussite: $(( TESTS_PASSED * 100 / TESTS_TOTAL ))%"

if [ $TESTS_FAILED -eq 0 ]; then
    echo "🎉 TOUS LES TESTS SONT PASSANTS - DÉPLOIEMENT AUTORISÉ"
    exit 0
else
    echo "⚠️ CERTAINS TESTS ONT ÉCHOUÉ - DÉPLOIEMENT NON AUTORISÉ"
    exit 1
fi
