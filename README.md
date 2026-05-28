# ReadMe — Bibliothèque EPUB

Application de bibliothèque de livres numériques au format EPUB, composée d'un backend Node.js et d'une application mobile/desktop .NET MAUI.

---

## Prérequis

| Outil | Version minimale |
|---|---|
| Node.js | 18+ |
| .NET SDK | 8.0 |
| Docker Desktop | toute version récente |
| Visual Studio / Rider | avec workload MAUI installé |

---

## Installation

### 1. Base de données

Démarrer MySQL et phpMyAdmin via Docker :

```bash
cd backend
docker-compose up -d
```

Puis importer le schéma dans phpMyAdmin (`http://localhost:8080`) ou via la CLI :

```bash
mysql -u root -proot p_web < backend/p_web.sql
```

---

### 2. Backend

```bash
cd backend
```

Créer le fichier `.env` :

```env
DATABASE_NAME=p_web
DATABASE_USERNAME=root
DATABASE_PASSWORD=root
DATABASE_HOST=localhost
PORT=3002
JWT_SECRET=votre_secret
PEPPER=votre_pepper
```

Installer les dépendances et démarrer :

```bash
npm install
npm run dev
```

Le serveur écoute sur **http://localhost:3002**.  
La documentation Swagger est accessible sur **http://localhost:3002/api-docs**.

---

### 3. Application MAUI

Ouvrir `P_335_ReadMe/P_335_ReadMe.sln` dans Visual Studio.

Sélectionner la cible souhaitée (`Windows Machine`, `Android Emulator`, etc.) et lancer le projet.

> **Android** : le backend doit être accessible depuis l'émulateur. L'adresse `http://10.0.2.2:3002` est utilisée automatiquement à la place de `localhost`.

---

## Fonctionnalités

### Importer un livre
Appuyer sur le bouton **+** en bas à droite, sélectionner un fichier `.epub`. Le livre est envoyé au serveur, les métadonnées sont extraites automatiquement depuis le fichier EPUB.

### Lire un livre
Appuyer sur la zone titre/auteur d'une carte pour ouvrir le lecteur. Utiliser **Suivant** / **Précédent** pour naviguer entre les chapitres. La progression est sauvegardée automatiquement.

### Gérer les tags
Appuyer sur le bouton **[tag]** en bas à droite d'une carte pour ouvrir le gestionnaire de tags. Il est possible d'ajouter un nouveau tag ou de supprimer un tag existant.

### Filtrer par tag
La barre de filtres apparaît automatiquement au-dessus de la bibliothèque dès qu'au moins un tag existe. Appuyer sur un chip pour filtrer ; appuyer de nouveau pour désactiver le filtre.

### Trier la bibliothèque
Appuyer sur le bouton **⇅** dans la barre inférieure pour choisir parmi :
- Date ↓ (récent en premier) — par défaut
- Date ↑ (ancien en premier)
- Titre A → Z
- Titre Z → A

### Rechercher
Le champ de recherche filtre en temps réel sur le titre et la description des livres.

---

## Structure du projet

```
P_335/
├── backend/                  # API Node.js / Express
│   ├── models/               # Modèles Sequelize (Book, User, Category, Comment)
│   ├── routes/               # Routes Express (books, users, categories, comments)
│   ├── middleware/           # Middleware d'authentification (currentUser)
│   ├── public/books/         # Fichiers EPUB uploadés
│   ├── docker-compose.yml    # MySQL + phpMyAdmin
│   ├── p_web.sql             # Schéma de la base de données
│   └── swagger.json          # Documentation API
│
├── P_335_ReadMe/             # Application .NET MAUI
│   ├── Models/               # Book.cs (modèle local SQLite + désérialisation API)
│   ├── Services/             # ApiService.cs (communication avec le backend)
│   ├── MainPage.xaml         # Interface principale (bibliothèque + lecteur)
│   └── MainPage.xaml.cs      # Logique : sync, lecture, tags, tri, filtres
│
└── docs/
    └── scenarios_tests.pdf   # Scénarios de tests fonctionnels
```

---

## API — Endpoints principaux

| Méthode | Route | Description |
|---|---|---|
| `GET` | `/books/` | Lister les livres (avec tags) |
| `POST` | `/books/` | Importer un livre EPUB (multipart) |
| `PUT` | `/books/:id` | Modifier les métadonnées d'un livre |
| `DELETE` | `/books/:id` | Supprimer un livre |
| `POST` | `/categories/:book_id` | Ajouter un tag à un livre |
| `DELETE` | `/categories/:book_id/remove` | Retirer un tag d'un livre |
| `GET` | `/api-docs` | Documentation Swagger interactive |
