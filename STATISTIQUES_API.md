# 📊 API des Statistiques - Documentation Complète

## 🎯 Vue d'ensemble

L'API Kenergie propose 5 endpoints de statistiques pour une analyse complète des données par société.

---

## 📈 Endpoints des Statistiques

### 1. Statistiques Générales
```
GET /api/Statistiques/generales/{idSociete}
```

**Rôles autorisés**: Super-Admin, Admin, Financier

**Données retournées**:

`totalPaiements` et `totalPaiementsCount` : uniquement les paiements validés dont `DatePaiement` est dans le **mois calendaire en cours** (même périmètre que la collecte utilisée pour `tauxRecouvrement`). `totalArrieres` reste le cumul des soldes dus sur toutes les factures actives.

```json
{
  "totalClients": 1072,
  "totalFactures": 47,
  "totalArrieres": 2611000.00,
  "totalPaiements": 26000.00,
  "tauxRecouvrement": 99.00,
  "totalPaiementsCount": 2,
  "dateGeneration": "2026-02-15T22:45:27.96525+02:00"
}
```

### 2. Statistiques Financières
```
GET /api/Statistiques/financieres/{idSociete}?debut=2026-01-01&fin=2026-02-28
```

**Rôles autorisés**: Super-Admin, Admin, Financier

**Périodes (sans query `debut` / `fin`)** :
- `chiffreAffaires`, `montantPaye`, `repartitionPaiements` : **mois calendaire en cours** uniquement.
- `evolutionMensuelle` : **année en cours** (1er janvier → aujourd’hui).

Avec `?debut=` et/ou `?fin=`, `montantPaye` et `repartitionPaiements` suivent cette période ; `chiffreAffaires` reste la collecte du mois en cours.

**Données retournées**:
```json
{
  "chiffreAffaires": 2637000.00,
  "montantArrieres": 2611000.00,
  "montantPaye": 26000.00,
  "montantDu": 2611000.00,
  "evolutionMensuelle": [
    {
      "mois": "janvier 2026",
      "montantFactures": 476000.00,
      "montantPaiements": 0.00,
      "montantArrieres": 476000.00,
      "nombreFactures": 37,
      "nombrePaiements": 0
    },
    {
      "mois": "février 2026",
      "montantFactures": 2135000.00,
      "montantPaiements": 26000.00,
      "montantArrieres": 2611000.00,
      "nombreFactures": 10,
      "nombrePaiements": 2
    }
  ],
  "repartitionPaiements": [
    {
      "methodePaiement": "Espace",
      "montantTotal": 26000.00,
      "nombrePaiements": 2,
      "pourcentage": 100
    }
  ],
  "dateGeneration": "2026-02-15T22:45:39.352638+02:00"
}
```

### 3. Statistiques Opérationnelles
```
GET /api/Statistiques/operationnelles/{idSociete}
```

**Rôles autorisés**: Super-Admin, Admin, Financier

**Données retournées**:
```json
{
  "repartitionClientsParCategorie": [
    {
      "idCategorie": 5,
      "nomCategorie": "DOMESTIQUE",
      "nombreClients": 934,
      "pourcentage": 86.96,
      "montantTotal": 673000.00
    },
    {
      "idCategorie": 3,
      "nomCategorie": "COMMERCIAL",
      "nombreClients": 124,
      "pourcentage": 11.55,
      "montantTotal": 1815000.00
    }
  ],
  "repartitionClientsParAxe": [
    {
      "idAxe": 26,
      "nomAxe": "E4",
      "nomCabine": "CABINE E",
      "nombreClients": 277,
      "pourcentage": 25.84
    }
  ],
  "statistiquesFacturesMois": [
    {
      "mois": "janvier 2026",
      "montantTotal": 494000.00,
      "nombreFactures": 37,
      "montantMoyen": 13351.35
    }
  ],
  "clientActivite": {
    "nombreClientsActifs": 1072,
    "nombreClientsInactifs": 0,
    "totalClients": 1072,
    "pourcentageActifs": 100,
    "pourcentageInactifs": 0
  },
  "dateGeneration": "2026-02-15T22:45:51.830389+02:00"
}
```

### 4. Statistiques de Performance
```
GET /api/Statistiques/performance/{idSociete}
```

**Rôles autorisés**: Super-Admin, Admin, Financier

**Données retournées**:

`topAgents` : classement des agents caissiers par **montant collecté sur le mois calendaire en cours** (`DatePaiement` du 1er au dernier jour du mois). Les paiements des mois précédents ne sont pas inclus. Seuls les agents avec une collecte strictement positive du mois apparaissent (maximum 10).

```json
{
  "tauxRecouvrementGlobal": 1.00,
  "tauxRecouvrementParCategorie": [
    {
      "idCategorie": 5,
      "nomCategorie": "DOMESTIQUE",
      "tauxRecouvrement": 4.02,
      "montantDu": 647000.00,
      "montantPaye": 26000.00
    }
  ],
  "topAgents": [
    {
      "idAgent": 1,
      "nomAgent": "Administrateur Super Admin",
      "montantCollecte": 26000.00,
      "nombrePaiements": 2,
      "tauxConversion": 50
    }
  ],
  "performanceMensuelle": [
    {
      "mois": "février 2026",
      "tauxRecouvrement": 1.22,
      "montantCollecte": 26000.00,
      "nombrePaiements": 2,
      "ticketMoyen": 13000.00
    }
  ],
  "dateGeneration": "2026-02-15T22:46:03.263618+02:00"
}
```

### 5. Statistiques Consolidées
```
GET /api/Statistiques/consolidees/{idSociete}?debut=2026-01-01&fin=2026-02-28
```

**Rôles autorisés**: Super-Admin, Admin, Financier

**Périodes** : agrège les 4 blocs ci-dessus. Sans query `debut` / `fin` :
- `generales.totalPaiements`, `financieres.montantPaye`, `financieres.chiffreAffaires` et `financieres.repartitionPaiements[].montantTotal` = **collecte du mois en cours** ;
- `financieres.evolutionMensuelle` = année en cours.

Avec `?debut=` / `?fin=`, seul `financieres.evolutionMensuelle` est recalculé sur cette fenêtre ; les totaux de paiements restent sur le **mois courant** (évite un cumul annuel type ~307 M sur le tableau de bord).

**Données retournées**: Contient toutes les statistiques précédentes dans un seul objet:
```json
{
  "generales": { /* Statistiques générales */ },
  "financieres": { /* Statistiques financières */ },
  "operationnelles": { /* Statistiques opérationnelles */ },
  "performance": { /* Statistiques de performance */ },
  "periode": {
    "dateDebut": "2026-01-01T00:00:00",
    "dateFin": "2026-02-28T00:00:00",
    "libellePeriode": "Période personnalisée"
  },
  "dateGeneration": "2026-02-15T22:46:17.011633+02:00"
}
```

---

## 🎨 Composant Chart.js pour Vue.js

### Installation
```bash
npm install chart.js vue-chartjs
```

### Composant TrendChart
```vue
<!-- src/components/TrendChart.vue -->
<template>
  <div class="trend-chart">
    <canvas ref="chartCanvas"></canvas>
  </div>
</template>

<script>
import { ref, onMounted, watch } from 'vue';
import {
  Chart,
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Title,
  Tooltip,
  Legend,
} from 'chart.js';

Chart.register(
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Title,
  Tooltip,
  Legend
);

export default {
  name: 'TrendChart',
  props: {
    data: {
      type: Array,
      required: true,
    },
    title: {
      type: String,
      default: 'Tendances',
    },
    color: {
      type: String,
      default: '#3B82F6',
    },
  },
  setup(props) {
    const chartCanvas = ref(null);
    let chartInstance = null;

    const createChart = () => {
      if (!chartCanvas.value) return;

      const ctx = chartCanvas.value.getContext('2d');
      
      chartInstance = new Chart(ctx, {
        type: 'line',
        data: {
          labels: props.data.map(item => item.mois),
          datasets: [
            {
              label: props.title,
              data: props.data.map(item => item.valeur),
              borderColor: props.color,
              backgroundColor: `${props.color}20`,
              borderWidth: 2,
              fill: true,
              tension: 0.4,
              pointBackgroundColor: props.color,
              pointBorderColor: '#fff',
              pointBorderWidth: 2,
              pointRadius: 4,
              pointHoverRadius: 6,
            },
          ],
        },
        options: {
          responsive: true,
          maintainAspectRatio: false,
          plugins: {
            legend: {
              display: false,
            },
            tooltip: {
              backgroundColor: 'rgba(0, 0, 0, 0.8)',
              titleColor: '#fff',
              bodyColor: '#fff',
              padding: 12,
              displayColors: false,
              callbacks: {
                label: function(context) {
                  const value = context.parsed.y;
                  return `${props.title}: ${value.toLocaleString('fr-FR')}`;
                },
              },
            },
          },
          scales: {
            x: {
              grid: {
                display: false,
              },
              ticks: {
                color: '#6B7280',
                font: {
                  size: 12,
                },
              },
            },
            y: {
              beginAtZero: true,
              grid: {
                color: '#E5E7EB',
              },
              ticks: {
                color: '#6B7280',
                font: {
                  size: 12,
                },
                callback: function(value) {
                  return value.toLocaleString('fr-FR');
                },
              },
            },
          },
          interaction: {
            intersect: false,
            mode: 'index',
          },
        },
      });
    };

    const updateChart = () => {
      if (!chartInstance) return;

      chartInstance.data.labels = props.data.map(item => item.mois);
      chartInstance.data.datasets[0].data = props.data.map(item => item.valeur);
      chartInstance.update();
    };

    onMounted(() => {
      createChart();
    });

    watch(() => props.data, () => {
      updateChart();
    }, { deep: true });

    return {
      chartCanvas,
    };
  },
};
</script>

<style scoped>
.trend-chart {
  position: relative;
  height: 300px;
  width: 100%;
}

canvas {
  max-height: 100%;
  max-width: 100%;
}
</style>
```

---

## 📱 Widget Flutter pour Graphiques

### Dépendances
```yaml
dependencies:
  fl_chart: ^0.63.0
```

### Widget TrendChart
```dart
// widgets/trend_chart.dart
import 'package:flutter/material.dart';
import 'package:fl_chart/fl_chart.dart';

class TrendChart extends StatelessWidget {
  final List<TrendData> data;
  final String title;
  final Color color;

  const TrendChart({
    Key? key,
    required this.data,
    required this.title,
    this.color = Colors.blue,
  }) : super(key: key);

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          title,
          style: const TextStyle(
            fontSize: 16,
            fontWeight: FontWeight.w600,
          ),
        ),
        const SizedBox(height: 16),
        SizedBox(
          height: 200,
          child: LineChart(
            LineChartData(
              gridData: FlGridData(
                show: true,
                drawVerticalLine: false,
                horizontalInterval: 1000,
                getDrawingHorizontalLine: (value) {
                  return FlLine(
                    color: Colors.grey.withOpacity(0.3),
                    strokeWidth: 1,
                  );
                },
              ),
              titlesData: FlTitlesData(
                show: true,
                rightTitles: AxisTitles(sideTitles: SideTitles(showTitles: false)),
                topTitles: AxisTitles(sideTitles: SideTitles(showTitles: false)),
                bottomTitles: AxisTitles(
                  sideTitles: SideTitles(
                    showTitles: true,
                    reservedSize: 30,
                    interval: 1,
                    getTitlesWidget: (value, meta) {
                      final index = value.toInt();
                      if (index < 0 || index >= data.length) {
                        return const Text('');
                      }
                      return SideTitleWidget(
                        axisSide: meta.axisSide,
                        child: Text(
                          data[index].mois.substring(0, 3),
                          style: const TextStyle(fontSize: 10),
                        ),
                      );
                    },
                  ),
                ),
                leftTitles: AxisTitles(
                  sideTitles: SideTitles(
                    showTitles: true,
                    reservedSize: 42,
                    getTitlesWidget: (value, meta) {
                      return SideTitleWidget(
                        axisSide: meta.axisSide,
                        child: Text(
                          value.toInt().toString(),
                          style: const TextStyle(fontSize: 10),
                        ),
                      );
                    },
                  ),
                ),
              ),
              borderData: FlBorderData(show: false),
              minX: 0,
              maxX: (data.length - 1).toDouble(),
              minY: 0,
              maxY: data.isNotEmpty 
                  ? data.map((e) => e.valeur).reduce((a, b) => a > b ? a : b) * 1.2
                  : 100,
              lineBarsData: [
                LineChartBarData(
                  spots: data.asMap().entries.map((e) {
                    return FlSpot(e.key.toDouble(), e.value.valeur);
                  }).toList(),
                  isCurved: true,
                  gradient: LinearGradient(
                    colors: [
                      color.withOpacity(0.8),
                      color.withOpacity(0.4),
                    ],
                  ),
                  barWidth: 3,
                  isStrokeCapRound: true,
                  dotData: FlDotData(
                    show: true,
                    getDotPainter: (spot, percent, barData, index) {
                      return FlDotCirclePainter(
                        radius: 4,
                        color: color,
                        strokeWidth: 2,
                        strokeColor: Colors.white,
                      );
                    },
                  ),
                  belowBarData: BarAreaData(
                    show: true,
                    gradient: LinearGradient(
                      colors: [
                        color.withOpacity(0.3),
                        color.withOpacity(0.1),
                      ],
                      begin: Alignment.topCenter,
                      end: Alignment.bottomCenter,
                    ),
                  ),
                ),
              ],
              lineTouchData: LineTouchData(
                touchTooltipData: LineTouchTooltipData(
                  tooltipBgColor: Colors.black87,
                  tooltipRoundedRadius: 8,
                  getTooltipItems: (touchedSpots) {
                    return touchedSpots.map((spot) {
                      final index = spot.x.toInt();
                      if (index < 0 || index >= data.length) {
                        return null;
                      }
                      return LineTooltipItem(
                        '${data[index].mois}\n${data[index].valeur.toInt()}',
                        const TextStyle(color: Colors.white, fontSize: 12),
                      );
                    }).toList();
                  },
                ),
                handleBuiltInTouches: true,
              ),
            ),
          ),
        ),
      ],
    );
  }
}

class TrendData {
  final String mois;
  final double valeur;

  TrendData({required this.mois, required this.valeur});
}
```

---

## 🔄 Gestion des Erreurs

### Vue.js - Composant ErrorBoundary
```vue
<!-- src/components/ErrorBoundary.vue -->
<template>
  <div v-if="error" class="error-boundary">
    <div class="error-content">
      <i class="fas fa-exclamation-triangle"></i>
      <h2>Une erreur est survenue</h2>
      <p>{{ error.message }}</p>
      <button @click="retry" class="retry-button">Réessayer</button>
    </div>
  </div>
  <slot v-else />
</template>

<script>
import { ref, onErrorCaptured } from 'vue';

export default {
  name: 'ErrorBoundary',
  setup(_, { slots }) {
    const error = ref(null);

    onErrorCaptured((err) => {
      error.value = err;
      console.error('Error caught by boundary:', err);
      return false;
    });

    const retry = () => {
      error.value = null;
    };

    return {
      error,
      retry,
    };
  },
};
</script>

<style scoped>
.error-boundary {
  display: flex;
  justify-content: center;
  align-items: center;
  min-height: 200px;
  padding: 20px;
}

.error-content {
  text-align: center;
  color: #ef4444;
}

.error-content i {
  font-size: 3rem;
  margin-bottom: 16px;
}

.error-content h2 {
  margin: 0 0 8px 0;
}

.retry-button {
  background: #ef4444;
  color: white;
  border: none;
  padding: 10px 20px;
  border-radius: 8px;
  cursor: pointer;
  margin-top: 16px;
}
</style>
```

### Flutter - Error Widget
```dart
// widgets/error_widget.dart
import 'package:flutter/material.dart';

class ErrorWidget extends StatelessWidget {
  final String error;
  final VoidCallback onRetry;

  const ErrorWidget({
    Key? key,
    required this.error,
    required this.onRetry,
  }) : super(key: key);

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(
              Icons.error_outline,
              size: 64,
              color: Colors.red[400],
            ),
            const SizedBox(height: 16),
            Text(
              'Une erreur est survenue',
              style: Theme.of(context).textTheme.headlineSmall,
              textAlign: TextAlign.center,
            ),
            const SizedBox(height: 8),
            Text(
              error,
              style: Theme.of(context).textTheme.bodyMedium,
              textAlign: TextAlign.center,
            ),
            const SizedBox(height: 24),
            ElevatedButton.icon(
              onPressed: onRetry,
              icon: const Icon(Icons.refresh),
              label: const Text('Réessayer'),
              style: ElevatedButton.styleFrom(
                backgroundColor: Colors.red,
                foregroundColor: Colors.white,
              ),
            ),
          ],
        ),
      ),
    );
  }
}
```

---

## 📝 Résumé des Points Clés

### 🔐 Sécurité
- Tous les endpoints nécessitent une authentification JWT
- Rôles spécifiques requis pour chaque type de statistique
- Tokens expirant après 2 heures (7200 secondes)

### 📊 Types de Données
- **Financières**: CA, arriérés, paiements, taux de recouvrement
- **Opérationnelles**: Répartition clients, factures par mois, activité
- **Performance**: Taux par catégorie, top agents, tendances mensuelles

### 🎯 Cas d'Usage
- **Dashboard Super-Admin**: Vue globale de toutes les sociétés
- **Dashboard Gérant**: Focus sur une société spécifique
- **Dashboard Client**: Vue personnelle du client
- **Dashboard Technicien**: Suivi des interventions

### 📱 Formats Supportés
- **Flutter**: Widgets natifs avec Material Design
- **Vue.js**: Composants réactifs avec Chart.js
- **Responsive**: Adaptation mobile et desktop

### 🔄 Mises à Jour en Temps Réel
- SignalR intégré pour les notifications
- Rafraîchissement automatique des données
- Gestion des erreurs robuste

Cette documentation complète facilitera l'intégration frontend avec des exemples concrets et réutilisables.
