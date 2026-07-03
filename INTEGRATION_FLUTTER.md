# 📱 Intégration Flutter - Dashboards Kenergie API

## 📦 Dépendances requises

Ajoutez ces dépendances à votre `pubspec.yaml`:

```yaml
dependencies:
  flutter:
    sdk: flutter
  http: ^1.1.0
  dio: ^5.3.2
  shared_preferences: ^2.2.2
  flutter_secure_storage: ^8.0.0
  json_annotation: ^4.8.1
  intl: ^0.18.1

dev_dependencies:
  flutter_test:
    sdk: flutter
  json_serializable: ^6.7.1
  build_runner: ^2.4.7
```

---

## 🔐 Service d'Authentification

### Modèle Authentification
```dart
// models/auth_response.dart
import 'package:json_annotation/json_annotation.dart';

part 'auth_response.g.dart';

@JsonSerializable()
class AuthResponse {
  final bool success;
  final String accessToken;
  final String tokenType;
  final int expiresIn;
  final UserInfo utilisateur;

  AuthResponse({
    required this.success,
    required this.accessToken,
    required this.tokenType,
    required this.expiresIn,
    required this.utilisateur,
  });

  factory AuthResponse.fromJson(Map<String, dynamic> json) =>
      _$AuthResponseFromJson(json);
  Map<String, dynamic> toJson() => _$AuthResponseToJson(this);
}

@JsonSerializable()
class UserInfo {
  final int idUtilisateur;
  final String nomComplet;
  final String email;
  final int idSociete;
  final List<String> roles;

  UserInfo({
    required this.idUtilisateur,
    required this.nomComplet,
    required this.email,
    required this.idSociete,
    required this.roles,
  });

  factory UserInfo.fromJson(Map<String, dynamic> json) =>
      _$UserInfoFromJson(json);
  Map<String, dynamic> toJson() => _$UserInfoToJson(this);
}
```

### Service Auth
```dart
// services/auth_service.dart
import 'package:dio/dio.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:kenergie_app/models/auth_response.dart';

class AuthService {
  final Dio _dio;
  final FlutterSecureStorage _storage;

  AuthService(this._dio, this._storage);

  Future<AuthResponse> login(String email, String password) async {
    try {
      final response = await _dio.post(
        '/api/Utilisateur/authentifier',
        data: {
          'emailOuTelephone': email,
          'motDePasse': password,
          'fcmToken': 'string',
          'deviceType': 'mobile',
          'deviceModel': 'Flutter App',
          'osVersion': '1.0.0',
        },
      );

      if (response.statusCode == 200) {
        final authResponse = AuthResponse.fromJson(response.data);
        
        // Sauvegarder le token
        await _storage.write(
          key: 'access_token',
          value: authResponse.accessToken,
        );
        
        // Configurer le token pour les requêtes futures
        _dio.options.headers['Authorization'] = 
            'Bearer ${authResponse.accessToken}';
        
        return authResponse;
      }
      throw Exception('Login failed');
    } on DioException catch (e) {
      throw Exception(e.response?.data['message'] ?? 'Login error');
    }
  }

  Future<void> logout() async {
    await _storage.delete(key: 'access_token');
    _dio.options.headers.remove('Authorization');
  }

  Future<String?> getToken() async {
    return await _storage.read(key: 'access_token');
  }

  Future<bool> isLoggedIn() async {
    final token = await getToken();
    return token != null;
  }
}
```

---

## 🏢 Dashboard Super-Admin - Flutter

### Modèles
```dart
// models/super_admin_dashboard.dart
import 'package:json_annotation/json_annotation.dart';

part 'super_admin_dashboard.g.dart';

@JsonSerializable()
class SuperAdminDashboard {
  final GlobalStatistiques globalStatistiques;
  final List<SocieteSummary> societes;
  final List<SocieteCA> top5SocietesCA;
  final List<SocieteRecouvrement> top5SocietesRecouvrement;
  final List<AlerteCritique> alertesCritiques;
  final Tendances tendances;
  final UtilisateursStatistiques utilisateursStatistiques;
  final DateTime dateGeneration;

  SuperAdminDashboard({
    required this.globalStatistiques,
    required this.societes,
    required this.top5SocietesCA,
    required this.top5SocietesRecouvrement,
    required this.alertesCritiques,
    required this.tendances,
    required this.utilisateursStatistiques,
    required this.dateGeneration,
  });

  factory SuperAdminDashboard.fromJson(Map<String, dynamic> json) =>
      _$SuperAdminDashboardFromJson(json);
  Map<String, dynamic> toJson() => _$SuperAdminDashboardToJson(json);
}

@JsonSerializable()
class GlobalStatistiques {
  final int totalSocietes;
  final int totalClients;
  final int totalAgents;
  final int totalUtilisateurs;
  final double caMoisEnCours;
  final double caMoisDernier;
  final double tauxRecouvrementMoisEnCours;
  final double tauxRecouvrementMoisDernier;
  final double croissanceCA;
  final double croissanceRecouvrement;

  GlobalStatistiques({
    required this.totalSocietes,
    required this.totalClients,
    required this.totalAgents,
    required this.totalUtilisateurs,
    required this.caMoisEnCours,
    required this.caMoisDernier,
    required this.tauxRecouvrementMoisEnCours,
    required this.tauxRecouvrementMoisDernier,
    required this.croissanceCA,
    required this.croissanceRecouvrement,
  });

  factory GlobalStatistiques.fromJson(Map<String, dynamic> json) =>
      _$GlobalStatistiquesFromJson(json);
  Map<String, dynamic> toJson() => _$GlobalStatistiquesToJson(json);
}

@JsonSerializable()
class Tendances {
  final List<TendanceMois> chiffreAffaires;
  final List<TendanceMois> tauxRecouvrement;

  Tendances({
    required this.chiffreAffaires,
    required this.tauxRecouvrement,
  });

  factory Tendances.fromJson(Map<String, dynamic> json) =>
      _$TendancesFromJson(json);
  Map<String, dynamic> toJson() => _$TendancesToJson(json);
}

@JsonSerializable()
class TendanceMois {
  final String mois;
  final double valeur;

  TendanceMois({
    required this.mois,
    required this.valeur,
  });

  factory TendanceMois.fromJson(Map<String, dynamic> json) =>
      _$TendanceMoisFromJson(json);
  Map<String, dynamic> toJson() => _$TendanceMoisToJson(json);
}
```

### Service Dashboard
```dart
// services/dashboard_service.dart
import 'package:dio/dio.dart';
import 'package:kenergie_app/models/super_admin_dashboard.dart';

class DashboardService {
  final Dio _dio;

  DashboardService(this._dio);

  Future<SuperAdminDashboard> getSuperAdminDashboard() async {
    try {
      final response = await _dio.get('/api/SuperAdmin/dashboard');
      
      if (response.statusCode == 200) {
        return SuperAdminDashboard.fromJson(response.data);
      }
      throw Exception('Failed to load dashboard');
    } on DioException catch (e) {
      throw Exception(e.response?.data['message'] ?? 'Dashboard error');
    }
  }

  Future<GerantDashboard> getGerantDashboard(int idSociete) async {
    try {
      final response = await _dio.get('/api/Gerant/dashboard/$idSociete');
      
      if (response.statusCode == 200) {
        return GerantDashboard.fromJson(response.data);
      }
      throw Exception('Failed to load dashboard');
    } on DioException catch (e) {
      throw Exception(e.response?.data['message'] ?? 'Dashboard error');
    }
  }

  Future<ClientDashboard> getClientDashboard() async {
    try {
      final response = await _dio.get('/api/ClientDashboard');
      
      if (response.statusCode == 200) {
        return ClientDashboard.fromJson(response.data);
      }
      throw Exception('Failed to load dashboard');
    } on DioException catch (e) {
      throw Exception(e.response?.data['message'] ?? 'Dashboard error');
    }
  }
}
```

### Widget Dashboard Super-Admin
```dart
// pages/super_admin_dashboard_page.dart
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:kenergie_app/services/dashboard_service.dart';
import 'package:kenergie_app/models/super_admin_dashboard.dart';

class SuperAdminDashboardPage extends StatefulWidget {
  const SuperAdminDashboardPage({Key? key}) : super(key: key);

  @override
  _SuperAdminDashboardPageState createState() => _SuperAdminDashboardPageState();
}

class _SuperAdminDashboardPageState extends State<SuperAdminDashboardPage> {
  Future<SuperAdminDashboard>? _dashboardFuture;

  @override
  void initState() {
    super.initState();
    _dashboardFuture = _loadDashboard();
  }

  Future<SuperAdminDashboard> _loadDashboard() async {
    final dashboardService = 
        Provider.of<DashboardService>(context, listen: false);
    return await dashboardService.getSuperAdminDashboard();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Dashboard Super-Admin'),
        backgroundColor: Colors.blue[800],
      ),
      body: RefreshIndicator(
        onRefresh: () async {
          setState(() {
            _dashboardFuture = _loadDashboard();
          });
        },
        child: FutureBuilder<SuperAdminDashboard>(
          future: _dashboardFuture,
          builder: (context, snapshot) {
            if (snapshot.connectionState == ConnectionState.waiting) {
              return const Center(child: CircularProgressIndicator());
            }

            if (snapshot.hasError) {
              return Center(
                child: Column(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    Icon(Icons.error, size: 64, color: Colors.red),
                    const SizedBox(height: 16),
                    Text(
                      'Erreur: ${snapshot.error}',
                      textAlign: TextAlign.center,
                    ),
                    const SizedBox(height: 16),
                    ElevatedButton(
                      onPressed: () {
                        setState(() {
                          _dashboardFuture = _loadDashboard();
                        });
                      },
                      child: const Text('Réessayer'),
                    ),
                  ],
                ),
              );
            }

            final dashboard = snapshot.data!;
            return _buildDashboardContent(dashboard);
          },
        ),
      ),
    );
  }

  Widget _buildDashboardContent(SuperAdminDashboard dashboard) {
    return SingleChildScrollView(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          _buildGlobalStats(dashboard.globalStatistiques),
          const SizedBox(height: 24),
          _buildTrendsSection(dashboard.tendances),
          const SizedBox(height: 24),
          _buildTopSocietes(dashboard.top5SocietesCA),
          const SizedBox(height: 24),
          _buildAlertsSection(dashboard.alertesCritiques),
        ],
      ),
    );
  }

  Widget _buildGlobalStats(GlobalStatistiques stats) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text(
              'Statistiques Globales',
              style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold),
            ),
            const SizedBox(height: 16),
            GridView.count(
              shrinkWrap: true,
              physics: const NeverScrollableScrollPhysics(),
              crossAxisCount: 2,
              childAspectRatio: 1.5,
              children: [
                _buildStatCard(
                  'Total Sociétés',
                  stats.totalSocietes.toString(),
                  Icons.business,
                  Colors.blue,
                ),
                _buildStatCard(
                  'Total Clients',
                  '${stats.totalClients}',
                  Icons.people,
                  Colors.green,
                ),
                _buildStatCard(
                  'CA Mois',
                  '${stats.caMoisEnCours.toInt()} FC',
                  Icons.attach_money,
                  Colors.orange,
                ),
                _buildStatCard(
                  'Taux Recouvrement',
                  '${stats.tauxRecouvrementMoisEnCours.toStringAsFixed(1)}%',
                  Icons.trending_up,
                  Colors.purple,
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildStatCard(String title, String value, IconData icon, Color color) {
    return Card(
      elevation: 2,
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(icon, size: 32, color: color),
            const SizedBox(height: 8),
            Text(
              title,
              style: const TextStyle(fontSize: 12, fontWeight: FontWeight.w500),
              textAlign: TextAlign.center,
            ),
            const SizedBox(height: 4),
            Text(
              value,
              style: const TextStyle(
                fontSize: 16,
                fontWeight: FontWeight.bold,
              ),
              textAlign: TextAlign.center,
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildTrendsSection(Tendances tendances) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text(
              'Tendances Mensuelles',
              style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold),
            ),
            const SizedBox(height: 16),
            SizedBox(
              height: 200,
              child: _buildTrendChart(tendances.chiffreAffaires, 'Chiffre d\'Affaires'),
            ),
            const SizedBox(height: 16),
            SizedBox(
              height: 200,
              child: _buildTrendChart(tendances.tauxRecouvrement, 'Taux de Recouvrement'),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildTrendChart(List<TendanceMois> data, String title) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(title, style: const TextStyle(fontWeight: FontWeight.w600)),
        const SizedBox(height: 8),
        Expanded(
          child: ListView.builder(
            scrollDirection: Axis.horizontal,
            itemCount: data.length,
            itemBuilder: (context, index) {
              final item = data[index];
              return Container(
                width: 80,
                margin: const EdgeInsets.only(right: 8),
                decoration: BoxDecoration(
                  color: Colors.blue.withOpacity(0.1),
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Padding(
                  padding: const EdgeInsets.all(8),
                  child: Column(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      Text(
                        item.mois,
                        style: const TextStyle(fontSize: 10),
                        textAlign: TextAlign.center,
                      ),
                      const SizedBox(height: 4),
                      Text(
                        '${item.valeur.toInt()}',
                        style: const TextStyle(
                          fontSize: 12,
                          fontWeight: FontWeight.bold,
                        ),
                        textAlign: TextAlign.center,
                      ),
                    ],
                  ),
                ),
              );
            },
          ),
        ),
      ],
    );
  }

  Widget _buildTopSocietes(List<SocieteCA> societes) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text(
              'Top 5 Sociétés (CA)',
              style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold),
            ),
            const SizedBox(height: 16),
            ListView.builder(
              shrinkWrap: true,
              physics: const NeverScrollableScrollPhysics(),
              itemCount: societes.length,
              itemBuilder: (context, index) {
                final societe = societes[index];
                return ListTile(
                  leading: CircleAvatar(
                    backgroundColor: Colors.blue,
                    child: Text('${index + 1}'),
                  ),
                  title: Text(societe.nomSociete),
                  subtitle: Text('${societe.nombreClients} clients'),
                  trailing: Text(
                    '${societe.chiffreAffaires.toInt()} FC',
                    style: const TextStyle(
                      fontWeight: FontWeight.bold,
                      color: Colors.green,
                    ),
                  ),
                );
              },
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildAlertsSection(List<AlerteCritique> alertes) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text(
              'Alertes Critiques',
              style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold),
            ),
            const SizedBox(height: 16),
            if (alertes.isEmpty)
              const Center(
                child: Text('Aucune alerte critique'),
              )
            else
              ListView.builder(
                shrinkWrap: true,
                physics: const NeverScrollableScrollPhysics(),
                itemCount: alertes.length,
                itemBuilder: (context, index) {
                  final alerte = alertes[index];
                  return ListTile(
                    leading: Icon(
                      Icons.warning,
                      color: _getAlertColor(alerte.gravite),
                    ),
                    title: Text(alerte.message),
                    subtitle: Text(alerte.dateAlerte.toString()),
                    trailing: alerte.estLue 
                        ? const Icon(Icons.check_circle, color: Colors.green)
                        : const Icon(Icons.circle, color: Colors.red),
                  );
                },
              ),
          ],
        ),
      ),
    );
  }

  Color _getAlertColor(String gravite) {
    switch (gravite.toLowerCase()) {
      case 'élevée':
        return Colors.red;
      case 'moyenne':
        return Colors.orange;
      case 'faible':
        return Colors.yellow;
      default:
        return Colors.grey;
    }
  }
}
```

---

## 👤 Dashboard Client - Flutter

### Widget Dashboard Client
```dart
// pages/client_dashboard_page.dart
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:kenergie_app/services/dashboard_service.dart';
import 'package:kenergie_app/models/client_dashboard.dart';

class ClientDashboardPage extends StatefulWidget {
  const ClientDashboardPage({Key? key}) : super(key: key);

  @override
  _ClientDashboardPageState createState() => _ClientDashboardPageState();
}

class _ClientDashboardPageState extends State<ClientDashboardPage> {
  Future<ClientDashboard>? _dashboardFuture;

  @override
  void initState() {
    super.initState();
    _dashboardFuture = _loadDashboard();
  }

  Future<ClientDashboard> _loadDashboard() async {
    final dashboardService = 
        Provider.of<DashboardService>(context, listen: false);
    return await dashboardService.getClientDashboard();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Mon Dashboard'),
        backgroundColor: Colors.green[800],
      ),
      body: FutureBuilder<ClientDashboard>(
        future: _dashboardFuture,
        builder: (context, snapshot) {
          if (snapshot.connectionState == ConnectionState.waiting) {
            return const Center(child: CircularProgressIndicator());
          }

          if (snapshot.hasError) {
            return Center(
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Icon(Icons.error, size: 64, color: Colors.red),
                  const SizedBox(height: 16),
                  Text('Erreur: ${snapshot.error}'),
                  const SizedBox(height: 16),
                  ElevatedButton(
                    onPressed: () {
                      setState(() {
                        _dashboardFuture = _loadDashboard();
                      });
                    },
                    child: const Text('Réessayer'),
                  ),
                ],
              ),
            );
          }

          final dashboard = snapshot.data!;
          return _buildDashboardContent(dashboard);
        },
      ),
    );
  }

  Widget _buildDashboardContent(ClientDashboard dashboard) {
    return SingleChildScrollView(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          _buildClientInfo(dashboard.resumeClient),
          const SizedBox(height: 24),
          _buildStatsCards(dashboard.statistiques),
          const SizedBox(height: 24),
          _buildRecentInvoices(dashboard.facturesRecentes),
          const SizedBox(height: 24),
          _buildRecentPayments(dashboard.paiementsRecents),
          const SizedBox(height: 24),
          _buildAlerts(dashboard.alertesClient),
        ],
      ),
    );
  }

  Widget _buildClientInfo(ResumeClient resume) {
    return Card(
      color: Colors.green[50],
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Row(
          children: [
            CircleAvatar(
              radius: 30,
              backgroundColor: Colors.green[800],
              child: Text(
                resume.nomClient.substring(0, 2).toUpperCase(),
                style: const TextStyle(
                  color: Colors.white,
                  fontWeight: FontWeight.bold,
                ),
              ),
            ),
            const SizedBox(width: 16),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    resume.nomClient,
                    style: const TextStyle(
                      fontSize: 18,
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                  Text(
                    'Réf: ${resume.referenceClient}',
                    style: TextStyle(color: Colors.grey[600]),
                  ),
                  Text(
                    resume.categorieClient,
                    style: TextStyle(
                      color: Colors.green[800],
                      fontWeight: FontWeight.w500,
                    ),
                  ),
                ],
              ),
            ),
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
              decoration: BoxDecoration(
                color: resume.statutCompte == 'Actif' 
                    ? Colors.green 
                    : Colors.red,
                borderRadius: BorderRadius.circular(12),
              ),
              child: Text(
                resume.statutCompte,
                style: const TextStyle(
                  color: Colors.white,
                  fontSize: 12,
                  fontWeight: FontWeight.bold,
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildStatsCards(ClientStatistiques stats) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const Text(
          'Mes Statistiques',
          style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold),
        ),
        const SizedBox(height: 16),
        GridView.count(
          shrinkWrap: true,
          physics: const NeverScrollableScrollPhysics(),
          crossAxisCount: 2,
          childAspectRatio: 1.5,
          children: [
            _buildStatCard(
              'Total Factures',
              '${stats.montantTotalFactures.toInt()} FC',
              Icons.receipt_long,
              Colors.blue,
            ),
            _buildStatCard(
              'Montant Payé',
              '${stats.montantTotalPaye.toInt()} FC',
              Icons.check_circle,
              Colors.green,
            ),
            _buildStatCard(
              'Montant Dû',
              '${stats.montantTotalDu.toInt()} FC',
              Icons.money_off,
              Colors.red,
            ),
            _buildStatCard(
              'Taux Recouvrement',
              '${stats.tauxRecouvrement.toStringAsFixed(1)}%',
              Icons.trending_up,
              Colors.purple,
            ),
          ],
        ),
      ],
    );
  }

  Widget _buildRecentInvoices(List<FactureRecente> factures) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                const Text(
                  'Factures Récentes',
                  style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
                ),
                TextButton(
                  onPressed: () {
                    // Naviguer vers la liste complète des factures
                  },
                  child: const Text('Voir tout'),
                ),
              ],
            ),
            const SizedBox(height: 16),
            if (factures.isEmpty)
              const Center(child: Text('Aucune facture récente'))
            else
              ListView.builder(
                shrinkWrap: true,
                physics: const NeverScrollableScrollPhysics(),
                itemCount: factures.length,
                itemBuilder: (context, index) {
                  final facture = factures[index];
                  return ListTile(
                    leading: Icon(
                      Icons.receipt,
                      color: facture.statut == 'Payée' 
                          ? Colors.green 
                          : Colors.orange,
                    ),
                    title: Text(facture.reference),
                    subtitle: Text(facture.moisAnnee),
                    trailing: Column(
                      mainAxisAlignment: MainAxisAlignment.center,
                      crossAxisAlignment: CrossAxisAlignment.end,
                      children: [
                        Text(
                          '${facture.montantTotal.toInt()} FC',
                          style: const TextStyle(fontWeight: FontWeight.bold),
                        ),
                        Text(
                          facture.statut,
                          style: TextStyle(
                            fontSize: 12,
                            color: facture.statut == 'Payée' 
                                ? Colors.green 
                                : Colors.orange,
                            fontWeight: FontWeight.w500,
                          ),
                        ),
                      ],
                    ),
                  );
                },
              ),
          ],
        ),
      ),
    );
  }

  Widget _buildRecentPayments(List<PaiementRecent> paiements) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text(
              'Paiements Récents',
              style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
            ),
            const SizedBox(height: 16),
            if (paiements.isEmpty)
              const Center(child: Text('Aucun paiement récent'))
            else
              ListView.builder(
                shrinkWrap: true,
                physics: const NeverScrollableScrollPhysics(),
                itemCount: paiements.length,
                itemBuilder: (context, index) {
                  final paiement = paiements[index];
                  return ListTile(
                    leading: Icon(
                      Icons.payment,
                      color: Colors.green,
                    ),
                    title: Text(paiement.reference),
                    subtitle: Text(
                      'Méthode: ${paiement.methodePaiement}',
                    ),
                    trailing: Column(
                      mainAxisAlignment: MainAxisAlignment.center,
                      crossAxisAlignment: CrossAxisAlignment.end,
                      children: [
                        Text(
                          '${paiement.montantPaye.toInt()} FC',
                          style: const TextStyle(fontWeight: FontWeight.bold),
                        ),
                        Text(
                          DateFormat('dd/MM/yyyy').format(paiement.datePaiement),
                          style: const TextStyle(fontSize: 12),
                        ),
                      ],
                    ),
                  );
                },
              ),
          ],
        ),
      ),
    );
  }

  Widget _buildAlerts(List<AlerteClient> alertes) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text(
              'Mes Alertes',
              style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
            ),
            const SizedBox(height: 16),
            if (alertes.isEmpty)
              const Center(child: Text('Aucune alerte'))
            else
              ListView.builder(
                shrinkWrap: true,
                physics: const NeverScrollableScrollPhysics(),
                itemCount: alertes.length,
                itemBuilder: (context, index) {
                  final alerte = alertes[index];
                  return ListTile(
                    leading: Icon(
                      Icons.warning,
                      color: _getAlertColor(alerte.niveauUrgence),
                    ),
                    title: Text(alerte.message),
                    subtitle: Text(
                      DateFormat('dd/MM/yyyy HH:mm').format(alerte.dateAlerte),
                    ),
                    trailing: alerte.estLue 
                        ? const Icon(Icons.check_circle, color: Colors.green)
                        : const Icon(Icons.circle, color: Colors.red),
                  );
                },
              ),
          ],
        ),
      ),
    );
  }

  Color _getAlertColor(String niveau) {
    switch (niveau.toLowerCase()) {
      case 'élevée':
        return Colors.red;
      case 'moyenne':
        return Colors.orange;
      case 'faible':
        return Colors.yellow;
      default:
        return Colors.grey;
    }
  }
}
```
