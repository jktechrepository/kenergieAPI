# Documentation d'Intégration Frontend
## Rôles : Agent Direction Commercial & Responsable Commercial

---

## Table des matières

1. [Vue d'ensemble des rôles](#vue-densemble-des-rôles)
2. [Permissions par rôle](#permissions-par-rôle)
3. [Authentification](#authentification)
4. [Endpoints disponibles](#endpoints-disponibles)
5. [Intégration Flutter (Mobile)](#intégration-flutter-mobile)
6. [Intégration Vue.js (Web)](#intégration-vuejs-web)
7. [Exemples d'utilisation](#exemples-dutilisation)
8. [Gestion des erreurs](#gestion-des-erreurs)
9. [Bonnes pratiques](#bonnes-pratiques)

---

## Vue d'ensemble des rôles

### Agent Direction Commercial
- **Rôle** : Agent de terrain
- **Focus** : Prospection et gestion client
- **Permissions** : Clients (CRUD), Dashboard, Statistiques personnelles
- **Endpoints** : 15+ endpoints disponibles

### Responsable Commercial  
- **Rôle** : Superviseur d'équipe
- **Focus** : Gestion d'équipe et aspects financiers
- **Permissions** : Tout l'Agent + Paiements + Gestion agents + Organisation
- **Endpoints** : 25+ endpoints disponibles

---

## Permissions par rôle

### Agent Direction Commercial

| Catégorie | Actions | Endpoints |
|-----------|---------|-----------|
| **Dashboard** | Read | `/api/AgentDirectionCommercialDashboard/*` |
| **Client** | Create, Read, ReadAll, Update | `/api/Client/*` |
| **Commercial** | Read | `/api/Commercial/*` |
| **CategorieClient** | Read, ReadAll | `/api/CategorieClient/*` |
| **Axe** | CRUD complet | `/api/Axe/*` |
| **Cabine** | CRUD complet | `/api/Cabine/*` |
| **Usage** | CRUD complet | `/api/Usage/*` |
| **TypeDeCourant** | CRUD complet | `/api/TypeDeCourant/*` |
| **PlainteClient** | CRUD complet | `/api/PlainteClient/*` |
| **CommunicationCampaign** | CRUD complet | `/api/CommunicationCampaign/*` |

### Responsable Commercial

| Catégorie | Actions | Endpoints |
|-----------|---------|-----------|
| **Dashboard** | Read | `/api/ResponsableCommercialDashboard/*` |
| **Client** | CRUD complet | `/api/Client/*` |
| **Paiement** | Create, Read, ReadAll | `/api/Paiement/*` |
| **Agent** | Read, ReadAll, Manage | `/api/Agent/*` |
| **Commercial** | Read | `/api/Commercial/*` |
| **Utilisateur** | Read, ReadAll, Create | `/api/Utilisateur/*` |
| **CategorieClient** | Read, ReadAll | `/api/CategorieClient/*` |
| **Axe** | CRUD complet | `/api/Axe/*` |
| **Cabine** | CRUD complet | `/api/Cabine/*` |
| **Usage** | CRUD complet | `/api/Usage/*` |
| **TypeDeCourant** | CRUD complet | `/api/TypeDeCourant/*` |
| **PlainteClient** | CRUD complet | `/api/PlainteClient/*` |
| **CommunicationCampaign** | CRUD complet | `/api/CommunicationCampaign/*` |

---

## Authentification

### Flow d'authentification

1. **Login** avec email/password
2. **Réception du JWT** avec permissions
3. **Stockage sécurisé** du token
4. **Utilisation** dans les requêtes API

### Structure du JWT

```json
{
  "sub": "user_id",
  "email": "user@example.com",
  "roles": ["Agent Direction Commercial"],
  "permissions": [
    "Client.Create",
    "Client.Read", 
    "Client.ReadAll",
    "Client.Update",
    "Dashboard.Read",
    "Commercial.Read"
  ],
  "exp": 1640995200,
  "iat": 1640908800
}
```

---

## Endpoints disponibles

### Dashboard

#### Agent Direction Commercial
```
GET  /api/AgentDirectionCommercialDashboard
GET  /api/AgentDirectionCommercialDashboard/stats
GET  /api/AgentDirectionCommercialDashboard/performance
GET  /api/AgentDirectionCommercialDashboard/clients
GET  /api/AgentDirectionCommercialDashboard/prospects
GET  /api/AgentDirectionCommercialDashboard/tasks
GET  /api/AgentDirectionCommercialDashboard/objectives
GET  /api/AgentDirectionCommercialDashboard/activities
```

#### Responsable Commercial
```
GET  /api/ResponsableCommercialDashboard
GET  /api/ResponsableCommercialDashboard/stats
GET  /api/ResponsableCommercialDashboard/team-performance
GET  /api/ResponsableCommercialDashboard/clients
GET  /api/ResponsableCommercialDashboard/prospects
GET  /api/ResponsableCommercialDashboard/tasks
GET  /api/ResponsableCommercialDashboard/objectives
GET  /api/ResponsableCommercialDashboard/activities
```

### Clients
```
GET    /api/Client/{id}
GET    /api/Client
POST   /api/Client
PUT    /api/Client/{id}
DELETE /api/Client/{id}
```

### Paiements (Responsable Commercial uniquement)
```
GET    /api/Paiement/{id}
GET    /api/Paiement
POST   /api/Paiement
PUT    /api/Paiement/{id}
DELETE /api/Paiement/{id}
```

---

## Intégration Flutter (Mobile)

### Configuration

```yaml
# pubspec.yaml
dependencies:
  flutter:
    sdk: flutter
  http: ^1.1.0
  flutter_secure_storage: ^8.0.0
  jwt_decoder: ^2.0.1
  dio: ^5.3.2
```

### Service d'authentification

```dart
// services/auth_service.dart
import 'package:dio/dio.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:jwt_decoder/jwt_decoder.dart';

class AuthService {
  final Dio _dio;
  final FlutterSecureStorage _storage;

  AuthService() 
    : _dio = Dio(BaseOptions(baseUrl: 'https://api.kenergie.com')),
      _storage = const FlutterSecureStorage();

  Future<AuthResult> login(String email, String password) async {
    try {
      final response = await _dio.post('/api/auth/login', data: {
        'email': email,
        'password': password,
      });

      final token = response.data['token'];
      await _storage.write(key: 'jwt_token', value: token);
      
      return AuthResult.success(
        token: token,
        user: User.fromToken(token),
      );
    } on DioException catch (e) {
      return AuthResult.error(e.response?.data['message'] ?? 'Login failed');
    }
  }

  Future<User?> getCurrentUser() async {
    final token = await _storage.read(key: 'jwt_token');
    if (token == null) return null;

    if (JwtDecoder.isExpired(token)) {
      await logout();
      return null;
    }

    return User.fromToken(token);
  }

  Future<void> logout() async {
    await _storage.delete(key: 'jwt_token');
  }

  Future<String?> getToken() async {
    return await _storage.read(key: 'jwt_token');
  }

  bool hasPermission(String permission) {
    final user = getCurrentUser();
    return user?.permissions.contains(permission) ?? false;
  }
}

class User {
  final String id;
  final String email;
  final List<String> roles;
  final List<String> permissions;

  User({
    required this.id,
    required this.email,
    required this.roles,
    required this.permissions,
  });

  factory User.fromToken(String token) {
    final decodedToken = JwtDecoder.decode(token);
    return User(
      id: decodedToken['sub'],
      email: decodedToken['email'],
      roles: List<String>.from(decodedToken['roles'] ?? []),
      permissions: List<String>.from(decodedToken['permissions'] ?? []),
    );
  }

  bool isAgentDirectionCommercial() => roles.contains('Agent Direction Commercial');
  bool isResponsableCommercial() => roles.contains('Responsable Commercial');
}

class AuthResult {
  final bool success;
  final String? token;
  final User? user;
  final String? error;

  AuthResult({required this.success, this.token, this.user, this.error});

  factory AuthResult.success({required String token, required User user}) {
    return AuthResult(success: true, token: token, user: user);
  }

  factory AuthResult.error(String error) {
    return AuthResult(success: false, error: error);
  }
}
```

### Service API avec intercepteur

```dart
// services/api_service.dart
import 'package:dio/dio.dart';

class ApiService {
  late Dio _dio;
  final AuthService _authService;

  ApiService(this._authService) {
    _dio = Dio(BaseOptions(
      baseUrl: 'https://api.kenergie.com',
      connectTimeout: const Duration(seconds: 10),
      receiveTimeout: const Duration(seconds: 10),
    ));

    // Ajout de l'intercepteur pour le JWT
    _dio.interceptors.add(
      InterceptorsWrapper(
        onRequest: (options, handler) async {
          final token = await _authService.getToken();
          if (token != null) {
            options.headers['Authorization'] = 'Bearer $token';
          }
          handler.next(options);
        },
        onError: (error, handler) async {
          if (error.response?.statusCode == 401) {
            await _authService.logout();
            // Rediriger vers la page de login
          }
          handler.next(error);
        },
      ),
    );
  }

  // Dashboard Agent Direction Commercial
  Future<AgentDashboard> getAgentDashboard() async {
    try {
      final response = await _dio.get('/api/AgentDirectionCommercialDashboard');
      return AgentDashboard.fromJson(response.data);
    } on DioException catch (e) {
      throw ApiException.fromDioError(e);
    }
  }

  // Dashboard Responsable Commercial
  Future<ResponsableDashboard> getResponsableDashboard() async {
    try {
      final response = await _dio.get('/api/ResponsableCommercialDashboard');
      return ResponsableDashboard.fromJson(response.data);
    } on DioException catch (e) {
      throw ApiException.fromDioError(e);
    }
  }

  // CRUD Clients
  Future<List<Client>> getClients() async {
    try {
      final response = await _dio.get('/api/Client');
      return (response.data as List).map((json) => Client.fromJson(json)).toList();
    } on DioException catch (e) {
      throw ApiException.fromDioError(e);
    }
  }

  Future<Client> createClient(CreateClientDto clientDto) async {
    try {
      final response = await _dio.post('/api/Client', data: clientDto.toJson());
      return Client.fromJson(response.data);
    } on DioException catch (e) {
      throw ApiException.fromDioError(e);
    }
  }

  Future<Client> updateClient(int id, UpdateClientDto clientDto) async {
    try {
      final response = await _dio.put('/api/Client/$id', data: clientDto.toJson());
      return Client.fromJson(response.data);
    } on DioException catch (e) {
      throw ApiException.fromDioError(e);
    }
  }

  // Paiements (Responsable Commercial uniquement)
  Future<List<Paiement>> getPaiements() async {
    if (!_authService.hasPermission('Paiement.ReadAll')) {
      throw PermissionDeniedException('Permission Paiement.ReadAll requise');
    }

    try {
      final response = await _dio.get('/api/Paiement');
      return (response.data as List).map((json) => Paiement.fromJson(json)).toList();
    } on DioException catch (e) {
      throw ApiException.fromDioError(e);
    }
  }
}

class ApiException implements Exception {
  final String message;
  final int? statusCode;

  ApiException(this.message, this.statusCode);

  factory ApiException.fromDioError(DioException error) {
    return ApiException(
      error.response?.data['message'] ?? 'Une erreur est survenue',
      error.response?.statusCode,
    );
  }
}

class PermissionDeniedException implements Exception {
  final String message;
  PermissionDeniedException(this.message);
}
```

### Models Flutter

```dart
// models/dashboard_models.dart
class AgentDashboard {
  final AgentStatsDto stats;
  final AgentPerformancePersonnelDto performance;
  final List<ClientAgentDto> clients;
  final List<ProspectAgentDto> prospects;
  final List<TacheDto> tasks;
  final ObjectifsMoisDto objectives;
  final List<ActiviteRecenteDto> activities;

  AgentDashboard({
    required this.stats,
    required this.performance,
    required this.clients,
    required this.prospects,
    required this.tasks,
    required this.objectives,
    required this.activities,
  });

  factory AgentDashboard.fromJson(Map<String, dynamic> json) {
    return AgentDashboard(
      stats: AgentStatsDto.fromJson(json['stats']),
      performance: AgentPerformancePersonnelDto.fromJson(json['performance']),
      clients: (json['clients'] as List)
          .map((c) => ClientAgentDto.fromJson(c))
          .toList(),
      prospects: (json['prospects'] as List)
          .map((p) => ProspectAgentDto.fromJson(p))
          .toList(),
      tasks: (json['tasks'] as List)
          .map((t) => TacheDto.fromJson(t))
          .toList(),
      objectives: ObjectifsMoisDto.fromJson(json['objectives']),
      activities: (json['activities'] as List)
          .map((a) => ActiviteRecenteDto.fromJson(a))
          .toList(),
    );
  }
}

class AgentStatsDto {
  final int totalClients;
  final int totalProspects;
  final int paiementsMois;
  final double chiffreAffairesMois;
  final int tachesEnCours;

  AgentStatsDto({
    required this.totalClients,
    required this.totalProspects,
    required this.paiementsMois,
    required this.chiffreAffairesMois,
    required this.tachesEnCours,
  });

  factory AgentStatsDto.fromJson(Map<String, dynamic> json) {
    return AgentStatsDto(
      totalClients: json['totalClients'] ?? 0,
      totalProspects: json['totalProspects'] ?? 0,
      paiementsMois: json['paiementsMois'] ?? 0,
      chiffreAffairesMois: (json['chiffreAffairesMois'] ?? 0).toDouble(),
      tachesEnCours: json['tachesEnCours'] ?? 0,
    );
  }
}

class Client {
  final int id;
  final String nom;
  final String prenom;
  final String email;
  final String telephone;
  final String adresse;
  final String? photoUrl;

  Client({
    required this.id,
    required this.nom,
    required this.prenom,
    required this.email,
    required this.telephone,
    required this.adresse,
    this.photoUrl,
  });

  factory Client.fromJson(Map<String, dynamic> json) {
    return Client(
      id: json['id'],
      nom: json['nom'] ?? '',
      prenom: json['prenom'] ?? '',
      email: json['email'] ?? '',
      telephone: json['telephone'] ?? '',
      adresse: json['adresse'] ?? '',
      photoUrl: json['photoUrl'],
    );
  }
}

class CreateClientDto {
  final String nom;
  final String prenom;
  final String email;
  final String telephone;
  final String adresse;

  CreateClientDto({
    required this.nom,
    required this.prenom,
    required this.email,
    required this.telephone,
    required this.adresse,
  });

  Map<String, dynamic> toJson() {
    return {
      'nom': nom,
      'prenom': prenom,
      'email': email,
      'telephone': telephone,
      'adresse': adresse,
    };
  }
}
```

### Widget Flutter - Dashboard Agent

```dart
// screens/agent_dashboard_screen.dart
import 'package:flutter/material.dart';

class AgentDashboardScreen extends StatefulWidget {
  const AgentDashboardScreen({Key? key}) : super(key: key);

  @override
  State<AgentDashboardScreen> createState() => _AgentDashboardScreenState();
}

class _AgentDashboardScreenState extends State<AgentDashboardScreen> {
  final ApiService _apiService = ApiService(AuthService());
  AgentDashboard? _dashboard;
  bool _isLoading = true;

  @override
  void initState() {
    super.initState();
    _loadDashboard();
  }

  Future<void> _loadDashboard() async {
    try {
      final dashboard = await _apiService.getAgentDashboard();
      setState(() {
        _dashboard = dashboard;
        _isLoading = false;
      });
    } catch (e) {
      setState(() => _isLoading = false);
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Erreur: ${e.toString()}')),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Dashboard Agent'),
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh),
            onPressed: _loadDashboard,
          ),
        ],
      ),
      body: _isLoading
          ? const Center(child: CircularProgressIndicator())
          : _dashboard != null
              ? _buildDashboardContent()
              : const Center(child: Text('Impossible de charger le dashboard')),
    );
  }

  Widget _buildDashboardContent() {
    return SingleChildScrollView(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // Stats principales
          _buildStatsCard(),
          const SizedBox(height: 16),
          
          // Performance
          _buildPerformanceCard(),
          const SizedBox(height: 16),
          
          // Clients récents
          _buildRecentClientsCard(),
          const SizedBox(height: 16),
          
          // Tâches en cours
          _buildTasksCard(),
        ],
      ),
    );
  }

  Widget _buildStatsCard() {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text(
              'Statistiques',
              style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
            ),
            const SizedBox(height: 12),
            Row(
              children: [
                Expanded(
                  child: _buildStatItem(
                    'Clients',
                    _dashboard!.stats.totalClients.toString(),
                    Icons.people,
                    Colors.blue,
                  ),
                ),
                Expanded(
                  child: _buildStatItem(
                    'Prospects',
                    _dashboard!.stats.totalProspects.toString(),
                    Icons.person_search,
                    Colors.green,
                  ),
                ),
              ],
            ),
            const SizedBox(height: 12),
            Row(
              children: [
                Expanded(
                  child: _buildStatItem(
                    'Paiements/Mois',
                    _dashboard!.stats.paiementsMois.toString(),
                    Icons.payment,
                    Colors.orange,
                  ),
                ),
                Expanded(
                  child: _buildStatItem(
                    'CA/Mois',
                    '${_dashboard!.stats.chiffreAffairesMois.toStringAsFixed(0)} FCFA',
                    Icons.trending_up,
                    Colors.purple,
                  ),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildStatItem(String title, String value, IconData icon, Color color) {
    return Container(
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: color.withOpacity(0.1),
        borderRadius: BorderRadius.circular(8),
      ),
      child: Column(
        children: [
          Icon(icon, color: color, size: 24),
          const SizedBox(height: 8),
          Text(
            value,
            style: TextStyle(
              fontSize: 20,
              fontWeight: FontWeight.bold,
              color: color,
            ),
          ),
          const SizedBox(height: 4),
          Text(
            title,
            style: TextStyle(fontSize: 12, color: Colors.grey[600]),
          ),
        ],
      ),
    );
  }

  Widget _buildPerformanceCard() {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text(
              'Performance',
              style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
            ),
            const SizedBox(height: 12),
            LinearProgressIndicator(
              value: _dashboard!.performance.tauxConversion,
              backgroundColor: Colors.grey[300],
              valueColor: AlwaysStoppedAnimation<Color>(
                _dashboard!.performance.tauxConversion > 0.7
                    ? Colors.green
                    : Colors.orange,
              ),
            ),
            const SizedBox(height: 8),
            Text(
              'Taux de conversion: ${(_dashboard!.performance.tauxConversion * 100).toStringAsFixed(1)}%',
              style: TextStyle(color: Colors.grey[600]),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildRecentClientsCard() {
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
                  'Clients récents',
                  style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
                ),
                TextButton(
                  onPressed: () {
                    // Naviguer vers la liste complète des clients
                  },
                  child: const Text('Voir tout'),
                ),
              ],
            ),
            const SizedBox(height: 12),
            ..._dashboard!.clients.take(3).map((client) => ListTile(
              leading: CircleAvatar(
                child: Text(client.nom[0]),
              ),
              title: Text('${client.prenom} ${client.nom}'),
              subtitle: Text(client.email),
              trailing: const Icon(Icons.chevron_right),
              onTap: () {
                // Naviguer vers les détails du client
              },
            )),
          ],
        ),
      ),
    );
  }

  Widget _buildTasksCard() {
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
                  'Tâches en cours',
                  style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
                ),
                Chip(
                  label: Text('${_dashboard!.tasks.length}'),
                  backgroundColor: Colors.blue.withOpacity(0.1),
                ),
              ],
            ),
            const SizedBox(height: 12),
            ..._dashboard!.tasks.take(3).map((task) => ListTile(
              leading: Checkbox(
                value: task.completed,
                onChanged: (value) {
                  // Marquer la tâche comme complétée
                },
              ),
              title: Text(task.titre),
              subtitle: Text(task.description),
              trailing: Text(
                task.dateEcheance,
                style: TextStyle(
                  fontSize: 12,
                  color: task.isUrgent ? Colors.red : Colors.grey,
                ),
              ),
            )),
          ],
        ),
      ),
    );
  }
}
```

---

## Intégration Vue.js (Web)

### Configuration

```bash
# Installation des dépendances
npm install axios vue-router vuex
npm install @vue/composition-api
```

### Service d'authentification

```javascript
// services/auth.service.js
import axios from 'axios';

const API_BASE_URL = process.env.VUE_APP_API_URL || 'https://api.kenergie.com';

class AuthService {
  constructor() {
    this.axios = axios.create({
      baseURL: API_BASE_URL,
      timeout: 10000,
    });

    // Intercepteur pour ajouter le JWT
    this.axios.interceptors.request.use(
      (config) => {
        const token = localStorage.getItem('jwt_token');
        if (token) {
          config.headers.Authorization = `Bearer ${token}`;
        }
        return config;
      },
      (error) => Promise.reject(error)
    );

    // Intercepteur pour gérer les erreurs 401
    this.axios.interceptors.response.use(
      (response) => response,
      (error) => {
        if (error.response?.status === 401) {
          this.logout();
          router.push('/login');
        }
        return Promise.reject(error);
      }
    );
  }

  async login(email, password) {
    try {
      const response = await this.axios.post('/api/auth/login', {
        email,
        password,
      });

      const token = response.data.token;
      localStorage.setItem('jwt_token', token);
      
      return {
        success: true,
        token,
        user: this.parseToken(token),
      };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data?.message || 'Login failed',
      };
    }
  }

  getCurrentUser() {
    const token = localStorage.getItem('jwt_token');
    if (!token) return null;

    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      
      // Vérifier si le token est expiré
      if (payload.exp * 1000 < Date.now()) {
        this.logout();
        return null;
      }

      return this.parseToken(token);
    } catch (error) {
      this.logout();
      return null;
    }
  }

  parseToken(token) {
    const payload = JSON.parse(atob(token.split('.')[1]));
    return {
      id: payload.sub,
      email: payload.email,
      roles: payload.roles || [],
      permissions: payload.permissions || [],
    };
  }

  logout() {
    localStorage.removeItem('jwt_token');
  }

  hasPermission(permission) {
    const user = this.getCurrentUser();
    return user?.permissions.includes(permission) || false;
  }

  isAgentDirectionCommercial() {
    const user = this.getCurrentUser();
    return user?.roles.includes('Agent Direction Commercial') || false;
  }

  isResponsableCommercial() {
    const user = this.getCurrentUser();
    return user?.roles.includes('Responsable Commercial') || false;
  }
}

export default new AuthService();
```

### Store Vuex pour la gestion d'état

```javascript
// store/index.js
import { createStore } from 'vuex';
import AuthService from '@/services/auth.service';
import ApiService from '@/services/api.service';

export default createStore({
  state: {
    user: null,
    isAuthenticated: false,
    dashboard: null,
    clients: [],
    paiements: [],
    loading: false,
    error: null,
  },

  getters: {
    currentUser: (state) => state.user,
    isAuthenticated: (state) => state.isAuthenticated,
    isAgentDirectionCommercial: (state) => 
      state.user?.roles.includes('Agent Direction Commercial') || false,
    isResponsableCommercial: (state) => 
      state.user?.roles.includes('Responsable Commercial') || false,
    hasPermission: (state) => (permission) => 
      state.user?.permissions.includes(permission) || false,
    dashboardStats: (state) => state.dashboard?.stats || null,
  },

  mutations: {
    SET_USER(state, user) {
      state.user = user;
      state.isAuthenticated = !!user;
    },
    SET_DASHBOARD(state, dashboard) {
      state.dashboard = dashboard;
    },
    SET_CLIENTS(state, clients) {
      state.clients = clients;
    },
    SET_PAIEMENTS(state, paiements) {
      state.paiements = paiements;
    },
    SET_LOADING(state, loading) {
      state.loading = loading;
    },
    SET_ERROR(state, error) {
      state.error = error;
    },
    ADD_CLIENT(state, client) {
      state.clients.push(client);
    },
    UPDATE_CLIENT(state, updatedClient) {
      const index = state.clients.findIndex(c => c.id === updatedClient.id);
      if (index !== -1) {
        state.clients.splice(index, 1, updatedClient);
      }
    },
  },

  actions: {
    async initAuth({ commit }) {
      const user = AuthService.getCurrentUser();
      if (user) {
        commit('SET_USER', user);
      }
    },

    async login({ commit }, { email, password }) {
      try {
        commit('SET_LOADING', true);
        const result = await AuthService.login(email, password);
        
        if (result.success) {
          commit('SET_USER', result.user);
          return { success: true };
        } else {
          commit('SET_ERROR', result.error);
          return { success: false, error: result.error };
        }
      } catch (error) {
        commit('SET_ERROR', 'Login failed');
        return { success: false, error: 'Login failed' };
      } finally {
        commit('SET_LOADING', false);
      }
    },

    async logout({ commit }) {
      AuthService.logout();
      commit('SET_USER', null);
      commit('SET_DASHBOARD', null);
      commit('SET_CLIENTS', []);
      commit('SET_PAIEMENTS', []);
    },

    async loadDashboard({ commit, state }) {
      if (!state.user) return;

      try {
        commit('SET_LOADING', true);
        
        let dashboard;
        if (state.user.roles.includes('Agent Direction Commercial')) {
          dashboard = await ApiService.getAgentDashboard();
        } else if (state.user.roles.includes('Responsable Commercial')) {
          dashboard = await ApiService.getResponsableDashboard();
        }

        commit('SET_DASHBOARD', dashboard);
      } catch (error) {
        commit('SET_ERROR', error.message);
      } finally {
        commit('SET_LOADING', false);
      }
    },

    async loadClients({ commit }) {
      try {
        commit('SET_LOADING', true);
        const clients = await ApiService.getClients();
        commit('SET_CLIENTS', clients);
      } catch (error) {
        commit('SET_ERROR', error.message);
      } finally {
        commit('SET_LOADING', false);
      }
    },

    async createClient({ commit }, clientData) {
      try {
        const client = await ApiService.createClient(clientData);
        commit('ADD_CLIENT', client);
        return { success: true, client };
      } catch (error) {
        commit('SET_ERROR', error.message);
        return { success: false, error: error.message };
      }
    },

    async updateClient({ commit }, { id, clientData }) {
      try {
        const updatedClient = await ApiService.updateClient(id, clientData);
        commit('UPDATE_CLIENT', updatedClient);
        return { success: true, client: updatedClient };
      } catch (error) {
        commit('SET_ERROR', error.message);
        return { success: false, error: error.message };
      }
    },

    async loadPaiements({ commit, state }) {
      if (!AuthService.hasPermission('Paiement.ReadAll')) {
        commit('SET_ERROR', 'Permission denied');
        return;
      }

      try {
        commit('SET_LOADING', true);
        const paiements = await ApiService.getPaiements();
        commit('SET_PAIEMENTS', paiements);
      } catch (error) {
        commit('SET_ERROR', error.message);
      } finally {
        commit('SET_LOADING', false);
      }
    },
  },
});
```

### Service API

```javascript
// services/api.service.js
import axios from 'axios';
import AuthService from './auth.service';

const API_BASE_URL = process.env.VUE_APP_API_URL || 'https://api.kenergie.com';

class ApiService {
  constructor() {
    this.axios = axios.create({
      baseURL: API_BASE_URL,
      timeout: 10000,
    });

    this.axios.interceptors.request.use(
      (config) => {
        const token = localStorage.getItem('jwt_token');
        if (token) {
          config.headers.Authorization = `Bearer ${token}`;
        }
        return config;
      },
      (error) => Promise.reject(error)
    );
  }

  // Dashboard
  async getAgentDashboard() {
    const response = await this.axios.get('/api/AgentDirectionCommercialDashboard');
    return response.data;
  }

  async getResponsableDashboard() {
    const response = await this.axios.get('/api/ResponsableCommercialDashboard');
    return response.data;
  }

  // Clients
  async getClients() {
    const response = await this.axios.get('/api/Client');
    return response.data;
  }

  async getClient(id) {
    const response = await this.axios.get(`/api/Client/${id}`);
    return response.data;
  }

  async createClient(clientData) {
    const response = await this.axios.post('/api/Client', clientData);
    return response.data;
  }

  async updateClient(id, clientData) {
    const response = await this.axios.put(`/api/Client/${id}`, clientData);
    return response.data;
  }

  async deleteClient(id) {
    await this.axios.delete(`/api/Client/${id}`);
  }

  // Paiements (Responsable Commercial uniquement)
  async getPaiements() {
    if (!AuthService.hasPermission('Paiement.ReadAll')) {
      throw new Error('Permission Paiement.ReadAll requise');
    }
    const response = await this.axios.get('/api/Paiement');
    return response.data;
  }

  async createPaiement(paiementData) {
    if (!AuthService.hasPermission('Paiement.Create')) {
      throw new Error('Permission Paiement.Create requise');
    }
    const response = await this.axios.post('/api/Paiement', paiementData);
    return response.data;
  }

  // Agents (Responsable Commercial uniquement)
  async getAgents() {
    if (!AuthService.hasPermission('Agent.ReadAll')) {
      throw new Error('Permission Agent.ReadAll requise');
    }
    const response = await this.axios.get('/api/Agent');
    return response.data;
  }
}

export default new ApiService();
```

### Composant Vue.js - Dashboard Agent

```vue
<!-- components/AgentDashboard.vue -->
<template>
  <div class="agent-dashboard">
    <div class="dashboard-header">
      <h1>Dashboard Agent</h1>
      <button @click="refreshDashboard" class="btn-refresh">
        <i class="fas fa-sync-alt"></i> Actualiser
      </button>
    </div>

    <div v-if="loading" class="loading">
      <i class="fas fa-spinner fa-spin"></i> Chargement...
    </div>

    <div v-else-if="error" class="error">
      <i class="fas fa-exclamation-triangle"></i>
      {{ error }}
    </div>

    <div v-else-if="dashboard" class="dashboard-content">
      <!-- Statistiques principales -->
      <div class="stats-grid">
        <div class="stat-card">
          <div class="stat-icon clients">
            <i class="fas fa-users"></i>
          </div>
          <div class="stat-content">
            <h3>{{ dashboard.stats.totalClients }}</h3>
            <p>Clients</p>
          </div>
        </div>

        <div class="stat-card">
          <div class="stat-icon prospects">
            <i class="fas fa-user-search"></i>
          </div>
          <div class="stat-content">
            <h3>{{ dashboard.stats.totalProspects }}</h3>
            <p>Prospects</p>
          </div>
        </div>

        <div class="stat-card">
          <div class="stat-icon payments">
            <i class="fas fa-payment"></i>
          </div>
          <div class="stat-content">
            <h3>{{ dashboard.stats.paiementsMois }}</h3>
            <p>Paiements/Mois</p>
          </div>
        </div>

        <div class="stat-card">
          <div class="stat-icon revenue">
            <i class="fas fa-chart-line"></i>
          </div>
          <div class="stat-content">
            <h3>{{ formatCurrency(dashboard.stats.chiffreAffairesMois) }}</h3>
            <p>CA/Mois</p>
          </div>
        </div>
      </div>

      <!-- Performance -->
      <div class="performance-card">
        <h2>Performance</h2>
        <div class="progress-bar">
          <div 
            class="progress-fill"
            :style="{ width: `${dashboard.performance.tauxConversion * 100}%` }"
          ></div>
        </div>
        <p>Taux de conversion: {{ (dashboard.performance.tauxConversion * 100).toFixed(1) }}%</p>
      </div>

      <!-- Clients récents -->
      <div class="recent-clients">
        <div class="section-header">
          <h2>Clients récents</h2>
          <router-link to="/clients" class="btn-view-all">
            Voir tout <i class="fas fa-arrow-right"></i>
          </router-link>
        </div>
        
        <div class="clients-list">
          <div 
            v-for="client in dashboard.clients.slice(0, 3)" 
            :key="client.id"
            class="client-item"
            @click="goToClient(client.id)"
          >
            <div class="client-avatar">
              {{ client.nom.charAt(0) }}
            </div>
            <div class="client-info">
              <h4>{{ client.prenom }} {{ client.nom }}</h4>
              <p>{{ client.email }}</p>
            </div>
            <i class="fas fa-chevron-right"></i>
          </div>
        </div>
      </div>

      <!-- Tâches en cours -->
      <div class="tasks-section">
        <div class="section-header">
          <h2>Tâches en cours</h2>
          <span class="task-count">{{ dashboard.tasks.length }}</span>
        </div>
        
        <div class="tasks-list">
          <div 
            v-for="task in dashboard.tasks.slice(0, 3)" 
            :key="task.id"
            class="task-item"
            :class="{ urgent: task.isUrgent }"
          >
            <input 
              type="checkbox" 
              :checked="task.completed"
              @change="toggleTask(task)"
            />
            <div class="task-content">
              <h4>{{ task.titre }}</h4>
              <p>{{ task.description }}</p>
              <span class="task-date">{{ task.dateEcheance }}</span>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import { mapState, mapGetters, mapActions } from 'vuex';

export default {
  name: 'AgentDashboard',
  
  computed: {
    ...mapState(['dashboard', 'loading', 'error']),
    ...mapGetters(['isAgentDirectionCommercial']),
  },

  created() {
    if (this.isAgentDirectionCommercial) {
      this.loadDashboard();
    }
  },

  methods: {
    ...mapActions(['loadDashboard']),

    async refreshDashboard() {
      await this.loadDashboard();
    },

    formatCurrency(amount) {
      return new Intl.NumberFormat('fr-FR', {
        style: 'currency',
        currency: 'XOF',
      }).format(amount);
    },

    goToClient(clientId) {
      this.$router.push(`/clients/${clientId}`);
    },

    async toggleTask(task) {
      try {
        // Appeler l'API pour marquer la tâche comme complétée
        await this.$store.dispatch('toggleTask', task.id);
      } catch (error) {
        console.error('Erreur lors de la mise à jour de la tâche:', error);
      }
    },
  },
};
</script>

<style scoped>
.agent-dashboard {
  padding: 20px;
  max-width: 1200px;
  margin: 0 auto;
}

.dashboard-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 30px;
}

.dashboard-header h1 {
  color: #2c3e50;
  font-size: 28px;
}

.btn-refresh {
  background: #3498db;
  color: white;
  border: none;
  padding: 10px 20px;
  border-radius: 5px;
  cursor: pointer;
  display: flex;
  align-items: center;
  gap: 8px;
}

.loading, .error {
  text-align: center;
  padding: 40px;
  font-size: 18px;
}

.error {
  color: #e74c3c;
}

.stats-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 20px;
  margin-bottom: 30px;
}

.stat-card {
  background: white;
  border-radius: 10px;
  padding: 20px;
  box-shadow: 0 2px 10px rgba(0,0,0,0.1);
  display: flex;
  align-items: center;
  gap: 15px;
}

.stat-icon {
  width: 50px;
  height: 50px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  color: white;
  font-size: 20px;
}

.stat-icon.clients { background: #3498db; }
.stat-icon.prospects { background: #2ecc71; }
.stat-icon.payments { background: #f39c12; }
.stat-icon.revenue { background: #9b59b6; }

.stat-content h3 {
  margin: 0;
  font-size: 24px;
  font-weight: bold;
  color: #2c3e50;
}

.stat-content p {
  margin: 5px 0 0 0;
  color: #7f8c8d;
}

.performance-card {
  background: white;
  border-radius: 10px;
  padding: 25px;
  box-shadow: 0 2px 10px rgba(0,0,0,0.1);
  margin-bottom: 30px;
}

.performance-card h2 {
  margin: 0 0 15px 0;
  color: #2c3e50;
}

.progress-bar {
  width: 100%;
  height: 10px;
  background: #ecf0f1;
  border-radius: 5px;
  overflow: hidden;
  margin-bottom: 10px;
}

.progress-fill {
  height: 100%;
  background: linear-gradient(90deg, #2ecc71, #27ae60);
  transition: width 0.3s ease;
}

.recent-clients, .tasks-section {
  background: white;
  border-radius: 10px;
  padding: 25px;
  box-shadow: 0 2px 10px rgba(0,0,0,0.1);
  margin-bottom: 30px;
}

.section-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;
}

.section-header h2 {
  margin: 0;
  color: #2c3e50;
}

.btn-view-all {
  color: #3498db;
  text-decoration: none;
  display: flex;
  align-items: center;
  gap: 5px;
}

.clients-list, .tasks-list {
  display: flex;
  flex-direction: column;
  gap: 15px;
}

.client-item, .task-item {
  display: flex;
  align-items: center;
  gap: 15px;
  padding: 15px;
  border-radius: 8px;
  background: #f8f9fa;
  cursor: pointer;
  transition: background 0.2s;
}

.client-item:hover, .task-item:hover {
  background: #e9ecef;
}

.client-avatar {
  width: 40px;
  height: 40px;
  border-radius: 50%;
  background: #3498db;
  color: white;
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: bold;
}

.client-info {
  flex: 1;
}

.client-info h4 {
  margin: 0 0 5px 0;
  color: #2c3e50;
}

.client-info p {
  margin: 0;
  color: #7f8c8d;
  font-size: 14px;
}

.task-item.urgent {
  border-left: 4px solid #e74c3c;
}

.task-content {
  flex: 1;
}

.task-content h4 {
  margin: 0 0 5px 0;
  color: #2c3e50;
}

.task-content p {
  margin: 0 0 5px 0;
  color: #7f8c8d;
  font-size: 14px;
}

.task-date {
  font-size: 12px;
  color: #e74c3c;
}

.task-count {
  background: #3498db;
  color: white;
  padding: 5px 10px;
  border-radius: 15px;
  font-size: 12px;
}

@media (max-width: 768px) {
  .stats-grid {
    grid-template-columns: 1fr;
  }
  
  .dashboard-header {
    flex-direction: column;
    gap: 15px;
    text-align: center;
  }
}
</style>
```

### Composant Vue.js - Gestion des clients

```vue
<!-- components/ClientForm.vue -->
<template>
  <div class="client-form">
    <h2>{{ isEditing ? 'Modifier le client' : 'Nouveau client' }}</h2>
    
    <form @submit.prevent="handleSubmit">
      <div class="form-row">
        <div class="form-group">
          <label for="nom">Nom *</label>
          <input 
            type="text" 
            id="nom" 
            v-model="form.nom" 
            required
            :class="{ error: errors.nom }"
          />
          <span v-if="errors.nom" class="error-message">{{ errors.nom }}</span>
        </div>
        
        <div class="form-group">
          <label for="prenom">Prénom *</label>
          <input 
            type="text" 
            id="prenom" 
            v-model="form.prenom" 
            required
            :class="{ error: errors.prenom }"
          />
          <span v-if="errors.prenom" class="error-message">{{ errors.prenom }}</span>
        </div>
      </div>
      
      <div class="form-group">
        <label for="email">Email *</label>
        <input 
          type="email" 
          id="email" 
          v-model="form.email" 
          required
          :class="{ error: errors.email }"
        />
        <span v-if="errors.email" class="error-message">{{ errors.email }}</span>
      </div>
      
      <div class="form-group">
        <label for="telephone">Téléphone *</label>
        <input 
          type="tel" 
          id="telephone" 
          v-model="form.telephone" 
          required
          :class="{ error: errors.telephone }"
        />
        <span v-if="errors.telephone" class="error-message">{{ errors.telephone }}</span>
      </div>
      
      <div class="form-group">
        <label for="adresse">Adresse *</label>
        <textarea 
          id="adresse" 
          v-model="form.adresse" 
          required
          rows="3"
          :class="{ error: errors.adresse }"
        ></textarea>
        <span v-if="errors.adresse" class="error-message">{{ errors.adresse }}</span>
      </div>
      
      <div class="form-actions">
        <button type="button" @click="cancel" class="btn-cancel">
          Annuler
        </button>
        <button type="submit" class="btn-submit" :disabled="submitting">
          <i v-if="submitting" class="fas fa-spinner fa-spin"></i>
          {{ isEditing ? 'Mettre à jour' : 'Créer' }}
        </button>
      </div>
    </form>
  </div>
</template>

<script>
import { mapActions } from 'vuex';

export default {
  name: 'ClientForm',
  
  props: {
    client: {
      type: Object,
      default: null,
    },
  },
  
  data() {
    return {
      form: {
        nom: '',
        prenom: '',
        email: '',
        telephone: '',
        adresse: '',
      },
      errors: {},
      submitting: false,
    };
  },
  
  computed: {
    isEditing() {
      return !!this.client;
    },
  },
  
  created() {
    if (this.client) {
      this.form = { ...this.client };
    }
  },
  
  methods: {
    ...mapActions(['createClient', 'updateClient']),
    
    validateForm() {
      this.errors = {};
      
      if (!this.form.nom.trim()) {
        this.errors.nom = 'Le nom est requis';
      }
      
      if (!this.form.prenom.trim()) {
        this.errors.prenom = 'Le prénom est requis';
      }
      
      if (!this.form.email.trim()) {
        this.errors.email = 'L\'email est requis';
      } else if (!this.isValidEmail(this.form.email)) {
        this.errors.email = 'L\'email n\'est pas valide';
      }
      
      if (!this.form.telephone.trim()) {
        this.errors.telephone = 'Le téléphone est requis';
      }
      
      if (!this.form.adresse.trim()) {
        this.errors.adresse = 'L\'adresse est requise';
      }
      
      return Object.keys(this.errors).length === 0;
    },
    
    isValidEmail(email) {
      const re = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
      return re.test(email);
    },
    
    async handleSubmit() {
      if (!this.validateForm()) {
        return;
      }
      
      this.submitting = true;
      
      try {
        let result;
        
        if (this.isEditing) {
          result = await this.updateClient({
            id: this.client.id,
            clientData: this.form,
          });
        } else {
          result = await this.createClient(this.form);
        }
        
        if (result.success) {
          this.$emit('success', result.client);
          this.$toast.success(
            this.isEditing ? 'Client mis à jour avec succès' : 'Client créé avec succès'
          );
        } else {
          this.$toast.error(result.error);
        }
      } catch (error) {
        this.$toast.error('Une erreur est survenue');
      } finally {
        this.submitting = false;
      }
    },
    
    cancel() {
      this.$emit('cancel');
    },
  },
};
</script>

<style scoped>
.client-form {
  background: white;
  border-radius: 10px;
  padding: 30px;
  box-shadow: 0 2px 10px rgba(0,0,0,0.1);
}

.client-form h2 {
  margin: 0 0 25px 0;
  color: #2c3e50;
}

.form-row {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 20px;
}

.form-group {
  margin-bottom: 20px;
}

.form-group label {
  display: block;
  margin-bottom: 8px;
  color: #2c3e50;
  font-weight: 500;
}

.form-group input,
.form-group textarea {
  width: 100%;
  padding: 12px;
  border: 1px solid #ddd;
  border-radius: 5px;
  font-size: 14px;
  transition: border-color 0.2s;
}

.form-group input:focus,
.form-group textarea:focus {
  outline: none;
  border-color: #3498db;
}

.form-group input.error,
.form-group textarea.error {
  border-color: #e74c3c;
}

.error-message {
  color: #e74c3c;
  font-size: 12px;
  margin-top: 5px;
  display: block;
}

.form-actions {
  display: flex;
  gap: 15px;
  justify-content: flex-end;
  margin-top: 30px;
}

.btn-cancel,
.btn-submit {
  padding: 12px 24px;
  border: none;
  border-radius: 5px;
  font-size: 14px;
  cursor: pointer;
  transition: background-color 0.2s;
}

.btn-cancel {
  background: #95a5a6;
  color: white;
}

.btn-cancel:hover {
  background: #7f8c8d;
}

.btn-submit {
  background: #3498db;
  color: white;
  display: flex;
  align-items: center;
  gap: 8px;
}

.btn-submit:hover:not(:disabled) {
  background: #2980b9;
}

.btn-submit:disabled {
  opacity: 0.7;
  cursor: not-allowed;
}

@media (max-width: 768px) {
  .form-row {
    grid-template-columns: 1fr;
  }
  
  .form-actions {
    flex-direction: column;
  }
  
  .btn-cancel,
  .btn-submit {
    width: 100%;
  }
}
</style>
```

---

## Exemples d'utilisation

### Flutter - Création d'un client

```dart
// Exemple d'utilisation dans Flutter
class CreateClientScreen extends StatefulWidget {
  @override
  _CreateClientScreenState createState() => _CreateClientScreenState();
}

class _CreateClientScreenState extends State<CreateClientScreen> {
  final _formKey = GlobalKey<FormState>();
  final _nameController = TextEditingController();
  final _emailController = TextEditingController();
  final _phoneController = TextEditingController();
  final _addressController = TextEditingController();
  
  final ApiService _apiService = ApiService(AuthService());
  bool _isLoading = false;

  Future<void> _createClient() async {
    if (!_formKey.currentState!.validate()) return;

    setState(() => _isLoading = true);

    try {
      final clientDto = CreateClientDto(
        nom: _nameController.text,
        prenom: _nameController.text, // Adapter selon vos besoins
        email: _emailController.text,
        telephone: _phoneController.text,
        adresse: _addressController.text,
      );

      final client = await _apiService.createClient(clientDto);
      
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Client créé avec succès')),
      );
      
      Navigator.pop(context, client);
    } catch (e) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Erreur: ${e.toString()}')),
      );
    } finally {
      setState(() => _isLoading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text('Nouveau client')),
      body: Padding(
        padding: EdgeInsets.all(16),
        child: Form(
          key: _formKey,
          child: Column(
            children: [
              TextFormField(
                controller: _nameController,
                decoration: InputDecoration(labelText: 'Nom'),
                validator: (value) => value?.isEmpty ?? true ? 'Requis' : null,
              ),
              TextFormField(
                controller: _emailController,
                decoration: InputDecoration(labelText: 'Email'),
                keyboardType: TextInputType.emailAddress,
                validator: (value) {
                  if (value?.isEmpty ?? true) return 'Requis';
                  if (!RegExp(r'^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$').hasMatch(value!)) {
                    return 'Email invalide';
                  }
                  return null;
                },
              ),
              TextFormField(
                controller: _phoneController,
                decoration: InputDecoration(labelText: 'Téléphone'),
                keyboardType: TextInputType.phone,
                validator: (value) => value?.isEmpty ?? true ? 'Requis' : null,
              ),
              TextFormField(
                controller: _addressController,
                decoration: InputDecoration(labelText: 'Adresse'),
                maxLines: 3,
                validator: (value) => value?.isEmpty ?? true ? 'Requis' : null,
              ),
              SizedBox(height: 20),
              _isLoading
                  ? CircularProgressIndicator()
                  : ElevatedButton(
                      onPressed: _createClient,
                      child: Text('Créer le client'),
                    ),
            ],
          ),
        ),
      ),
    );
  }
}
```

### Vue.js - Création d'un client

```javascript
// Exemple d'utilisation dans Vue.js
export default {
  name: 'CreateClient',
  
  data() {
    return {
      form: {
        nom: '',
        prenom: '',
        email: '',
        telephone: '',
        adresse: '',
      },
      errors: {},
      submitting: false,
    };
  },
  
  methods: {
    async createClient() {
      if (!this.validateForm()) return;
      
      this.submitting = true;
      
      try {
        const result = await this.$store.dispatch('createClient', this.form);
        
        if (result.success) {
          this.$toast.success('Client créé avec succès');
          this.$router.push('/clients');
        } else {
          this.$toast.error(result.error);
        }
      } catch (error) {
        this.$toast.error('Une erreur est survenue');
      } finally {
        this.submitting = false;
      }
    },
    
    validateForm() {
      this.errors = {};
      
      if (!this.form.nom.trim()) {
        this.errors.nom = 'Le nom est requis';
      }
      
      if (!this.form.email.trim()) {
        this.errors.email = 'L\'email est requis';
      } else if (!this.isValidEmail(this.form.email)) {
        this.errors.email = 'L\'email n\'est pas valide';
      }
      
      return Object.keys(this.errors).length === 0;
    },
  },
};
```

---

## Gestion des erreurs

### Flutter - Gestion centralisée des erreurs

```dart
// utils/error_handler.dart
class ErrorHandler {
  static void handleApiError(BuildContext context, ApiException error) {
    String message;
    
    switch (error.statusCode) {
      case 401:
        message = 'Session expirée. Veuillez vous reconnecter.';
        // Rediriger vers la page de login
        Navigator.of(context).pushNamedAndRemoveUntil(
          '/login',
          (route) => false,
        );
        break;
      case 403:
        message = 'Permission refusée';
        break;
      case 404:
        message = 'Ressource non trouvée';
        break;
      case 500:
        message = 'Erreur serveur. Veuillez réessayer plus tard.';
        break;
      default:
        message = error.message;
    }
    
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text(message),
        backgroundColor: Colors.red,
      ),
    );
  }
}
```

### Vue.js - Gestion centralisée des erreurs

```javascript
// utils/error.handler.js
export class ErrorHandler {
  static handleApiError(error, context) {
    let message;
    
    switch (error.response?.status) {
      case 401:
        message = 'Session expirée. Veuillez vous reconnecter.';
        // Rediriger vers la page de login
        context.$router.push('/login');
        break;
      case 403:
        message = 'Permission refusée';
        break;
      case 404:
        message = 'Ressource non trouvée';
        break;
      case 500:
        message = 'Erreur serveur. Veuillez réessayer plus tard.';
        break;
      default:
        message = error.response?.data?.message || 'Une erreur est survenue';
    }
    
    context.$toast.error(message);
  }
}
```

---

## Bonnes pratiques

### Sécurité
1. **Toujours vérifier les permissions** côté client avant d'appeler les endpoints
2. **Utiliser HTTPS** pour toutes les communications
3. **Stocker le JWT** de manière sécurisée (Flutter Secure Storage, localStorage)
4. **Implémenter le logout automatique** sur expiration du token

### Performance
1. **Utiliser le cache** pour les données qui changent peu
2. **Implémenter le lazy loading** pour les grandes listes
3. **Optimiser les images** et les assets
4. **Utiliser la pagination** pour les endpoints list

### UX/UI
1. **Afficher des indicateurs de chargement** pendant les appels API
2. **Gérer les erreurs** de manière conviviale
3. **Implémenter le mode hors ligne** pour les fonctionnalités critiques
4. **Utiliser des notifications** pour informer l'utilisateur des succès/erreurs

### Code
1. **Organiser le code** en services, composants, utilitaires
2. **Utiliser TypeScript** pour Vue.js si possible
3. **Documenter les fonctions** et les composants
4. **Écrire des tests** unitaires et d'intégration

---

## Conclusion

Cette documentation fournit une base complète pour l'intégration frontend des nouveaux rôles "Agent Direction Commercial" et "Responsable Commercial". Les exemples sont fonctionnels et peuvent être adaptés selon les besoins spécifiques de votre application.

Pour toute question supplémentaire ou pour des besoins d'adaptation spécifiques, n'hésitez pas à demander !
